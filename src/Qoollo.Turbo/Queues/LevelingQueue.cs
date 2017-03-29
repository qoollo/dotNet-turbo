using Qoollo.Turbo.Queues.ServiceStuff;
using Qoollo.Turbo.Threading;
using Qoollo.Turbo.Threading.ServiceStuff;
using Qoollo.Turbo.Threading.ThreadManagement;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Queues
{
    /// <summary>
    /// Adding modes for LevelingQueue
    /// </summary>
    public enum LevelingQueueAddingMode
    {
        /// <summary>
        /// Indicates that the ordering of data is critical
        /// </summary>
        PreserveOrder = 0,
        /// <summary>
        /// Indicates that data should be first added to the HighLevelQueue if possible
        /// </summary>
        PreferLiveData
    }


    /// <summary>
    /// Queue that controls two queues. 
    /// One of the higher level and another as the backing storage when first is full.
    /// </summary>
    /// <example>
    /// Helps to combine small and fast queue in memory with large and slow queue on disk
    /// </example>
    /// <typeparam name="T">The type of elements in queue</typeparam>
    public class LevelingQueue<T> : Common.CommonQueueImpl<T>, IDisposable
    {
        /// <summary>
        /// As long as the inner queue possibly can be changed outside we use Polling on MonitorObject with reasonable WaitPollingTimeout
        /// </summary>
        private const int WaitPollingTimeout = 5000;

        private static readonly int ProcessorCount = Environment.ProcessorCount;

        private readonly IQueue<T> _highLevelQueue;
        private readonly IQueue<T> _lowLevelQueue;

        private readonly LevelingQueueAddingMode _addingMode;
        private readonly bool _isBackgroundTransferingEnabled;

        private readonly MonitorObject _addMonitor;
        private readonly MonitorObject _takeMonitor;

        private readonly DelegateThreadSetManager _backgroundTransferer;
        private readonly MutuallyExclusivePrimitive _bacgoundTransfererExclusive;

        private long _itemCount; // Required when background transfering enabled

        private volatile bool _isDisposed;

        /// <summary>
        /// LevelingQueue constructor
        /// </summary>
        /// <param name="highLevelQueue">High level queue (queue with higher priority)</param>
        /// <param name="lowLevelQueue">Low level queue (queue with lower priority)</param>
        /// <param name="addingMode">Adding mode of the queue</param>
        /// <param name="isBackgroundTransferingEnabled">Is background transfering items from LowLevelQueue to HighLevelQueue enabled</param>
        public LevelingQueue(IQueue<T> highLevelQueue, IQueue<T> lowLevelQueue, LevelingQueueAddingMode addingMode, bool isBackgroundTransferingEnabled)
        {
            if (highLevelQueue == null)
                throw new ArgumentNullException(nameof(highLevelQueue));
            if (lowLevelQueue == null)
                throw new ArgumentNullException(nameof(lowLevelQueue));

            _highLevelQueue = highLevelQueue;
            _lowLevelQueue = lowLevelQueue;

            _addingMode = addingMode;
            _isBackgroundTransferingEnabled = isBackgroundTransferingEnabled;

            _addMonitor = new MonitorObject("AddMonitor");
            _takeMonitor = new MonitorObject("TakeMonitor");

            _itemCount = highLevelQueue.Count + lowLevelQueue.Count;
            _isDisposed = false;

            if (isBackgroundTransferingEnabled)
            {
                _bacgoundTransfererExclusive = new MutuallyExclusivePrimitive();
                if (addingMode == LevelingQueueAddingMode.PreferLiveData)
                    _bacgoundTransfererExclusive.AllowBackgroundGate(); // Allow background transfering from the start

                _backgroundTransferer = new DelegateThreadSetManager(1, this.GetType().GetCSName() + "_" + this.GetHashCode().ToString(), BackgroundTransferProc);
                _backgroundTransferer.IsBackground = true;
                _backgroundTransferer.Start();
            }
        }
        /// <summary>
        /// LevelingQueue constructor
        /// </summary>
        /// <param name="highLevelQueue">High level queue (queue with higher priority)</param>
        /// <param name="lowLevelQueue">Low level queue (queue with lower priority)</param>
        public LevelingQueue(IQueue<T> highLevelQueue, IQueue<T> lowLevelQueue)
            : this(highLevelQueue, lowLevelQueue, LevelingQueueAddingMode.PreserveOrder, false)
        {
        }

        /// <summary>
        /// Direct access to high level queue
        /// </summary>
        protected IQueue<T> HighLevelQueue { get { return _highLevelQueue; } }
        /// <summary>
        /// Direct access to low level queue
        /// </summary>
        protected IQueue<T> LowLevelQueue { get { return _lowLevelQueue; } }
        /// <summary>
        /// Adding mode of the queue
        /// </summary>
        public LevelingQueueAddingMode AddingMode { get { return _addingMode; } }
        /// <summary>
        /// Is transfering items from LowLevelQueue to HighLevelQueue in background enabled
        /// </summary>
        public bool IsBackgroundTransferingEnabled { get { return _isBackgroundTransferingEnabled; } }

        /// <summary>
        /// The bounded size of the queue (-1 means not bounded)
        /// </summary>
        public sealed override long BoundedCapacity
        {
            get
            {
                long lowLevelBoundedCapacity = _lowLevelQueue.BoundedCapacity;
                if (lowLevelBoundedCapacity < 0)
                    return -1;
                long highLevelBoundedCapacity = _highLevelQueue.BoundedCapacity;
                if (highLevelBoundedCapacity < 0)
                    return -1;
                return highLevelBoundedCapacity + lowLevelBoundedCapacity;
            }
        }

        /// <summary>
        /// Number of items inside the queue
        /// </summary>
        public sealed override long Count
        {
            get
            {
                if (_isBackgroundTransferingEnabled)
                    return Volatile.Read(ref _itemCount);

                long highLevelCount = _highLevelQueue.Count;
                if (highLevelCount < 0)
                    return -1;
                long lowLevelCount = _lowLevelQueue.Count;
                if (lowLevelCount < 0)
                    return -1;
                return highLevelCount + lowLevelCount;
            }
        }

        /// <summary>
        /// Indicates whether the queue is empty
        /// </summary>
        public sealed override bool IsEmpty
        {
            get
            {
                if (_isBackgroundTransferingEnabled)
                    return Volatile.Read(ref _itemCount) == 0;

                return _highLevelQueue.IsEmpty && _lowLevelQueue.IsEmpty;
            }
        }


        /// <summary>
        /// Checks if queue is disposed
        /// </summary>
        private void CheckDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(this.GetType().Name);
        }

        /// <summary>
        /// Adds new item to the queue, even when the bounded capacity reached
        /// </summary>
        /// <param name="item">New item</param>
        public override void AddForced(T item)
        {
            CheckDisposed();

            if (_addingMode == LevelingQueueAddingMode.PreferLiveData)
            {
                if (!_highLevelQueue.TryAdd(item, 0, default(CancellationToken)))
                {
                    _lowLevelQueue.AddForced(item);

                    if (_isBackgroundTransferingEnabled)
                        _bacgoundTransfererExclusive.AllowBackgroundGate(); // Allow background transfering
                }
            }
            else
            {
                bool addedToHighLevelQueue = false;
                if (_isBackgroundTransferingEnabled)
                {
                    if (_lowLevelQueue.IsEmpty)
                    {
                        // Only in exclusive mode
                        using (var gateGuard = _bacgoundTransfererExclusive.EnterMain(Timeout.Infinite, default(CancellationToken))) // This should happen fast
                        {
                            Debug.Assert(gateGuard.IsAcquired);
                            addedToHighLevelQueue = _lowLevelQueue.IsEmpty && _highLevelQueue.TryAdd(item, 0, default(CancellationToken));
                        }
                    }
                }
                else
                {
                    addedToHighLevelQueue = _lowLevelQueue.IsEmpty && _highLevelQueue.TryAdd(item, 0, default(CancellationToken));
                }

                if (!addedToHighLevelQueue)
                {
                    _lowLevelQueue.AddForced(item);
                    if (_isBackgroundTransferingEnabled)
                        _bacgoundTransfererExclusive.AllowBackgroundGate(); // Allow background transfering
                }
            }

            Interlocked.Increment(ref _itemCount);
            _takeMonitor.Pulse();
        }

        /// <summary>
        /// Adds new item to the high level queue, even when the bounded capacity reached
        /// </summary>
        /// <param name="item">New item</param>
        public void AddForcedToHighLevelQueue(T item)
        {
            CheckDisposed();
            _highLevelQueue.AddForced(item);
            Interlocked.Increment(ref _itemCount);
            _takeMonitor.Pulse();
        }


        // ====================== Add ==================

        /// <summary>
        /// Check wheter the value inside specified interval (inclusively)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsInsideInterval(long value, long min, long max)
        {
            return value >= min && value <= max;
        }

        /// <summary>
        /// Fast path to add the item (with zero timeout)
        /// </summary>
        /// <param name="item">New item</param>
        /// <returns>Was added sucessufully</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryAddFast(T item)
        {
            Debug.Assert(_addingMode == LevelingQueueAddingMode.PreferLiveData, "Only PreferLiveData supported");

            if (_highLevelQueue.TryAdd(item, 0, default(CancellationToken)))
                return true;
            if (_lowLevelQueue.TryAdd(item, 0, default(CancellationToken)))
                return true;

            return false;
        }

        /// <summary>
        /// Slow path to add the item (waits on <see cref="_addMonitor"/>)
        /// </summary>
        /// <param name="item">New item</param>
        /// <param name="startTime">Start time of the whole operation</param>
        /// <param name="timeout">Timeout</param>
        /// <param name="token">Token</param>
        /// <returns>Was added sucessufully</returns>
        private bool TryAddSlow(T item, int timeout, uint startTime, CancellationToken token)
        {
            if (timeout != 0 && _addMonitor.WaiterCount >= 0)
                Thread.Yield();

            int restTimeout = TimeoutHelper.RecalculateTimeout(startTime, timeout);
            using (var waiter = _addMonitor.Enter(restTimeout, token))
            {
                if (TryAddFast(item))
                    return true;

                while (!waiter.IsTimeouted)
                {
                    waiter.Wait(WaitPollingTimeout);
                    if (TryAddFast(item))
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Adds new item to the tail of the queue (core method)
        /// </summary>
        /// <param name="item">New item</param>
        /// <param name="timeout">Adding timeout</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>Was added sucessufully</returns>
        protected override bool TryAddCore(T item, int timeout, CancellationToken token)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(this.GetType().Name);
            if (token.IsCancellationRequested)
                throw new OperationCanceledException(token);

            bool result = false;
            uint startTime = 0;
            if (timeout > 0)
                startTime = TimeoutHelper.GetTimestamp();
            else if (timeout < -1)
                timeout = Timeout.Infinite;

            if (_addingMode == LevelingQueueAddingMode.PreferLiveData)
            {
                if (_addMonitor.WaiterCount == 0)
                {
                    result = TryAddFast(item);
                    if (!result && timeout != 0)
                        result = TryAddSlow(item, timeout, startTime, token); // Use slow path to add to any queue
                }
                else
                {
                    // Enter slow path if any waiter presented
                    result = TryAddSlow(item, timeout, startTime, token);
                }

                if (_isBackgroundTransferingEnabled && !_lowLevelQueue.IsEmpty)
                    _bacgoundTransfererExclusive.AllowBackgroundGate(); // Allow background transfering
            }
            else
            {
                if (_isBackgroundTransferingEnabled && timeout != 0 && !_lowLevelQueue.IsEmpty && Volatile.Read(ref _itemCount) <= ProcessorCount)
                {
                    // Attempt to wait for lowLevelQueue to become empty
                    SpinWait sw = new SpinWait();
                    while (!sw.NextSpinWillYield && !_lowLevelQueue.IsEmpty)
                        sw.SpinOnce();
                    if (sw.NextSpinWillYield && timeout != 0 && !_lowLevelQueue.IsEmpty)
                        sw.SpinOnce();
                }

                if (_isBackgroundTransferingEnabled)
                {
                    if (_lowLevelQueue.IsEmpty)
                    {
                        // Only in exclusive mode
                        using (var gateGuard = _bacgoundTransfererExclusive.EnterMain(Timeout.Infinite, token)) // This should happen fast
                        {
                            Debug.Assert(gateGuard.IsAcquired);
                            result = _lowLevelQueue.IsEmpty && _highLevelQueue.TryAdd(item, 0, default(CancellationToken));
                        }
                    }
                }
                else
                {
                    result = _lowLevelQueue.IsEmpty && _highLevelQueue.TryAdd(item, 0, default(CancellationToken));
                }

                if (!result)
                {
                    result = _lowLevelQueue.TryAdd(item, timeout, token); // To preserve order we try to add only to the lower queue
                    if (result && _isBackgroundTransferingEnabled)
                        _bacgoundTransfererExclusive.AllowBackgroundGate(); // Allow background transfering
                }
            }

            if (result)
            {
                Interlocked.Increment(ref _itemCount);
                _takeMonitor.Pulse();
            }

            return result;
        }


        // ========================== Take ==================

        /// <summary>
        /// Fast path to take the item from any queue (with zero timeout)
        /// </summary>
        /// <param name="item">The item removed from queue</param>
        /// <returns>True if the item was removed</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryTakeFast(out T item)
        {
            long itemCount = Volatile.Read(ref _itemCount);
            if (_highLevelQueue.TryTake(out item, 0, default(CancellationToken)))
                return true;
            if (itemCount == 0) // Prevent ordering problem
                return false;
            if (_lowLevelQueue.TryTake(out item, 0, default(CancellationToken)))
                return true;

            return false;
        }
        /// <summary>
        /// Slow path to take the item from queue (uses <see cref="_takeMonitor"/>)
        /// </summary>
        /// <param name="item">The item removed from queue</param>
        /// <param name="timeout">Timeout</param>
        /// <param name="startTime">Start time of the operation</param>
        /// <param name="token">Token</param>
        /// <returns>True if the item was removed</returns>
        private bool TryTakeSlow(out T item, int timeout, uint startTime, CancellationToken token)
        {
            if (timeout != 0 && _takeMonitor.WaiterCount >= 0)
                Thread.Yield();

            int restTimeout = TimeoutHelper.RecalculateTimeout(startTime, timeout);
            using (var waiter = _takeMonitor.Enter(restTimeout, token))
            {
                if (TryTakeFast(out item))
                    return true;

                while (!waiter.IsTimeouted)
                {
                    waiter.Wait(WaitPollingTimeout);
                    if (TryTakeFast(out item))
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Blocks background transferer and attempts to take the item
        /// </summary>
        private bool TryTakeExclusively(out T item, int timeout, uint startTime, CancellationToken token)
        {
            Debug.Assert(_isBackgroundTransferingEnabled);

            using (var gateGuard = _bacgoundTransfererExclusive.EnterMain(Timeout.Infinite, token)) // This should happen fast
            {
                Debug.Assert(gateGuard.IsAcquired);

                if (_takeMonitor.WaiterCount == 0)
                {
                    if (TryTakeFast(out item))
                        return true;
                    if (timeout == 0)
                        return false;
                }

                return TryTakeSlow(out item, timeout, startTime, token);
            }
        }

        /// <summary>
        /// Removes item from the head of the queue (core method)
        /// </summary>
        /// <param name="item">The item removed from queue</param>
        /// <param name="timeout">Removing timeout</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>True if the item was removed</returns>
        protected override bool TryTakeCore(out T item, int timeout, CancellationToken token)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(this.GetType().Name);
            if (token.IsCancellationRequested)
                throw new OperationCanceledException(token);

            bool result = false;
            item = default(T);

            uint startTime = 0;
            if (timeout > 0)
                startTime = TimeoutHelper.GetTimestamp();
            else if (timeout < -1)
                timeout = Timeout.Infinite;

            if (_isBackgroundTransferingEnabled)
            {
                result = _highLevelQueue.TryTake(out item, 0, default(CancellationToken));
                if (!result && _addingMode == LevelingQueueAddingMode.PreferLiveData)
                    result = _lowLevelQueue.TryTake(out item, 0, default(CancellationToken)); // Can take from lower queue only when ordering is not required

                if (!result)
                    result = TryTakeExclusively(out item, timeout, startTime, token); // Should be mutually exclusive with background transferer to prevent item lost or reordering
                else if (!_lowLevelQueue.IsEmpty)
                    _bacgoundTransfererExclusive.AllowBackgroundGate(); // allow Background transfering
            }
            else
            {
                if (_takeMonitor.WaiterCount == 0)
                {
                    result = TryTakeFast(out item);
                    if (!result && timeout != 0)
                        result = TryTakeSlow(out item, timeout, startTime, token);
                }
                else
                {
                    // Preserve fairness
                    result = TryTakeSlow(out item, timeout, startTime, token);
                }
            }

            if (result)
            {
                Interlocked.Decrement(ref _itemCount);
                _addMonitor.Pulse();
            }

            return result;
        }


        // ===================== Peek ===============

        /// <summary>
        /// Fast path to peek the item (zero timeout)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryPeekFast(out T item)
        {
            if (_highLevelQueue.TryPeek(out item, 0, default(CancellationToken)))
                return true;
            if (_lowLevelQueue.TryPeek(out item, 0, default(CancellationToken)))
                return true;

            return false;
        }

        /// <summary>
        /// Returns the item at the head of the queue without removing it (core method)
        /// </summary>
        /// <param name="item">The item at the head of the queue</param>
        /// <param name="timeout">Peeking timeout</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>True if the item was read</returns>
        protected override bool TryPeekCore(out T item, int timeout, CancellationToken token)
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        // ====================== Background ===============


        /// <summary>
        /// Transfers data from LowLevelQueue to HighLevelQueue in background
        /// </summary>
        private void BackgroundTransferProc(object state, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                using (var gateGuard = _bacgoundTransfererExclusive.EnterBackground(Timeout.Infinite, token)) // Background is on Gate2
                using (var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(token, gateGuard.Token))
                {
                    Debug.Assert(gateGuard.IsAcquired);

                    T item = default(T);
                    bool itemTaken = false;

                    try
                    {
                        while (!linkedCancellation.IsCancellationRequested)
                        {
                            item = default(T);
                            itemTaken = false;

                            if (!_lowLevelQueue.TryTake(out item, 0, default(CancellationToken)))
                            {
                                _bacgoundTransfererExclusive.DisallowBackgroundGate(); // Nothing to do. Stop attempts
                                break;
                            }

                            itemTaken = true;
                            if (!_highLevelQueue.TryAdd(item, 0, default(CancellationToken))) // Fast path to ignore cancellation
                            {
                                bool itemAdded = _highLevelQueue.TryAdd(item, Timeout.Infinite, linkedCancellation.Token);
                                Debug.Assert(itemAdded);
                            }

                            itemTaken = false;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        if (itemTaken)
                        {
                            _highLevelQueue.AddForced(item); // Prevent item lost
                            itemTaken = false;
                        }

                        if (!linkedCancellation.IsCancellationRequested)
                            throw;
                    }
                }
            }
        }


        /// <summary>
        /// Clean-up all resources
        /// </summary>
        /// <param name="isUserCall">Was called by user</param>
        protected override void Dispose(bool isUserCall)
        {
            if (!_isDisposed)
            {
                _isDisposed = true;

                if (_backgroundTransferer != null)
                    _backgroundTransferer.Stop(waitForStop: true);

                _addMonitor.Dispose();
                _takeMonitor.Dispose();

                _lowLevelQueue.Dispose();
                _highLevelQueue.Dispose();

                if (_bacgoundTransfererExclusive != null)
                    _bacgoundTransfererExclusive.Dispose();
            }
        }
    }
}
