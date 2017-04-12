using Qoollo.Turbo.Queues.DiskQueueComponents;
using Qoollo.Turbo.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Qoollo.Turbo.Collections;
using System.IO;
using System.Diagnostics;
using Qoollo.Turbo.Threading.ThreadManagement;
using Qoollo.Turbo.Threading.ServiceStuff;

namespace Qoollo.Turbo.Queues
{
    /// <summary>
    /// Queue that stores items on disk
    /// </summary>
    /// <typeparam name="T">Type of items stored inside queue</typeparam>
    public class DiskQueue<T>: Common.CommonQueueImpl<T>
    {
        private const int CompactionPeriodMs = 2 * 60 * 1000;
        private static readonly int NonFairItemThreshold = 16 * Environment.ProcessorCount;
        private const int NonFairSegmentThreshold = 4;

        private readonly DiskQueueSegmentFactory<T> _segmentFactory;
        private readonly object _segmentOperationsLock;
        private readonly int _maxSegmentCount;
        private readonly string _segmentsPath;

        private readonly CircularList<DiskQueueSegmentWrapper<T>> _segments;
        private volatile DiskQueueSegmentWrapper<T> _tailSegment;
        private volatile DiskQueueSegmentWrapper<T> _headSegment;

        private long _lastSegmentNumber;

        private long _itemCount;
        private readonly long _boundedCapacity;

        private readonly MonitorObject _addMonitor;
        private readonly MonitorObject _takeMonitor;
        private readonly MonitorObject _peekMonitor;

        private readonly int _compactionPeriod;
        private readonly DelegateThreadSetManager _backgroundCompactionThread;

        private volatile bool _isDisposed;

        /// <summary>
        /// DiskQueue constructor
        /// </summary>
        /// <param name="path">Path to the folder on the disk to store queue segments</param>
        /// <param name="segmentFactory">Factory to create DiskQueueSegments</param>
        /// <param name="maxSegmentCount">Maximum number of simultaniously active segments</param>
        /// <param name="backgroundCompaction">Is background compaction allowed (if not then compaction happens synchronously within the Take operation)</param>
        /// <param name="compactionPeriod">Compaction period in milliseconds</param>
        internal DiskQueue(string path, DiskQueueSegmentFactory<T> segmentFactory, int maxSegmentCount, bool backgroundCompaction, int compactionPeriod)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));
            if (segmentFactory == null)
                throw new ArgumentNullException(nameof(segmentFactory));
            if (compactionPeriod <= 0)
                throw new ArgumentOutOfRangeException(nameof(compactionPeriod), "Compaction period should be positive");
            if (maxSegmentCount == 0 || maxSegmentCount == 1)
                throw new ArgumentOutOfRangeException(nameof(maxSegmentCount), "At least two segments should be available");
            if (maxSegmentCount > int.MaxValue / 4)
                throw new ArgumentOutOfRangeException(nameof(maxSegmentCount), "Segment count is too large");

            if (maxSegmentCount <= 0 || maxSegmentCount > int.MaxValue / 4)
                maxSegmentCount = int.MaxValue / 4;

            _addMonitor = new MonitorObject("DiskQueue.AddMonitor");
            _takeMonitor = new MonitorObject("DiskQueue.TakeMonitor");
            _peekMonitor = new MonitorObject("DiskQueue.PeekMonitor");

            _segmentFactory = segmentFactory;
            _segmentOperationsLock = new object();
            _maxSegmentCount = maxSegmentCount;
            _segmentsPath = path;

            _itemCount = 0;
            _boundedCapacity = -1;
            if (_segmentFactory.SegmentCapacity > 0)
                _boundedCapacity = (long)_segmentFactory.SegmentCapacity * maxSegmentCount;


            var discoveredSegments = segmentFactory.DiscoverSegmentsWrapped(path);
            _segments = new CircularList<DiskQueueSegmentWrapper<T>>(discoveredSegments.OrderBy(o => o.Number));
            if (_segments.Count > 0)
            {
                for (int i = 0; i < _segments.Count; i++)
                {
                    if (i + 1 < _segments.Count)
                    {
                        _segments[i].NextSegment = _segments[i + 1]; // Build linked-list
                        if (_segments[i].Number == _segments[i + 1].Number)
                            throw new InvalidOperationException("DiscoverSegments returned duplicated segment numbers");
                    }
                    _itemCount += _segments[i].Count;
                }

                _headSegment = _segments[0];
                _tailSegment = _segments[_segments.Count - 1];
                _lastSegmentNumber = _tailSegment.Number;
            }
            else
            {
                // Allocate new segment
                lock (_segmentOperationsLock)
                {
                    _headSegment = _tailSegment = segmentFactory.CreateSegmentWrapped(path, ++_lastSegmentNumber);
                    _segments.Add(_tailSegment);
                }
            }

            _compactionPeriod = compactionPeriod;
            if (backgroundCompaction)
            {
                _backgroundCompactionThread = new DelegateThreadSetManager(1, this.GetType().GetCSName() + "_" + this.GetHashCode().ToString() + " Background compaction", BackgroundCompactionProc);
                _backgroundCompactionThread.IsBackground = true;
                _backgroundCompactionThread.Start();
            }

            _isDisposed = false;
        }
        /// <summary>
        /// DiskQueue constructor
        /// </summary>
        /// <param name="path">Path to the folder on the disk to store queue segments</param>
        /// <param name="segmentFactory">Factory to create DiskQueueSegments</param>
        /// <param name="maxSegmentCount">Maximum number of simultaniously active segments</param>
        /// <param name="backgroundCompaction">Is background compaction allowed (if not then compaction happens synchronously within the Take operation)</param>
        public DiskQueue(string path, DiskQueueSegmentFactory<T> segmentFactory, int maxSegmentCount, bool backgroundCompaction)
            : this(path, segmentFactory, maxSegmentCount, backgroundCompaction, CompactionPeriodMs)
        {
        }
        /// <summary>
        /// DiskQueue constructor
        /// </summary>
        /// <param name="path">Path to the folder on the disk to store queue segments</param>
        /// <param name="segmentFactory">Factory to create DiskQueueSegments</param>
        public DiskQueue(string path, DiskQueueSegmentFactory<T> segmentFactory)
            : this(path, segmentFactory, -1, false)
        {
        }



        /// <summary>
        /// The bounded size of the queue (-1 means not bounded)
        /// </summary>
        public override long BoundedCapacity { get { return _boundedCapacity; } }
        /// <summary>
        /// Number of items inside the queue
        /// </summary>
        public override long Count { get { return Math.Max(Volatile.Read(ref _itemCount), 0); } }
        /// <summary>
        /// Indicates whether the queue is empty
        /// </summary>
        public override bool IsEmpty { get { return Volatile.Read(ref _itemCount) == 0; } }
        /// <summary>
        /// Is background compaction enabled
        /// </summary>
        public bool IsBackgroundCompactionEnabled { get { return _backgroundCompactionThread != null; } }
        /// <summary>
        /// Path to the folder on the disk to store queue segments
        /// </summary>
        public string Path { get { return _segmentsPath; } }
        /// <summary>
        /// Queue segments count
        /// </summary>
        internal int SegmentCount { get { return _segments.Count; } }

        /// <summary>
        /// Checks if queue is disposed
        /// </summary>
        private void CheckDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(this.GetType().Name);
        }

        /// <summary>
        /// Checks that the queue in the consistent state (should be called inside lock)
        /// </summary>
        [Conditional("DEBUG")]
        private void VerifyConsistency()
        {
            Debug.Assert(Monitor.IsEntered(_segmentOperationsLock), "segment lock is not acquired");

            Debug.Assert(_headSegment != null, "_headSegment != null");
            Debug.Assert(_tailSegment != null, "_tailSegment != null");
            Debug.Assert(_segments.Count > 0, "_segments.Count > 0");
            Debug.Assert(_tailSegment == _segments[_segments.Count - 1], "_tailSegment == _segments[_segments.Count - 1]");

            bool beforeHead = true;
            DiskQueueSegmentWrapper<T> prevSegment = null;
            _segments.ForEach(curSegment =>
            {
                if (prevSegment != null)
                    Debug.Assert(prevSegment.NextSegment == curSegment, "Linked-list of segments is broken");

                if (curSegment == _headSegment)
                {
                    Debug.Assert(beforeHead, "Head segments met twice");
                    beforeHead = false;
                }

                if (beforeHead)
                    Debug.Assert(curSegment.IsCompleted, "All segements before head should be in IsCompleted state");
                if (curSegment != _tailSegment)
                    Debug.Assert(curSegment.IsFull, "All segements before tail should be in IsFull state");

                prevSegment = curSegment;
            });
            Debug.Assert(!beforeHead, "HeadSegment is not found in the list of segments");
        }


        /// <summary>
        /// Notifies about item addition
        /// </summary>
        private void NotifyItemAdded()
        {
            Interlocked.Increment(ref _itemCount);
            _takeMonitor.Pulse();
            _peekMonitor.PulseAll();
        }
        /// <summary>
        /// Notifies that item was taken
        /// </summary>
        private void NotifyItemTaken()
        {
            Interlocked.Decrement(ref _itemCount);
            _addMonitor.Pulse();
        }


        /// <summary>
        /// Removes completed segments
        /// </summary>
        /// <param name="allowNotification">Allows to send notification for adders (only when called from background thread)</param>
        private void Compact(bool allowNotification)
        {
            if (_isDisposed)
                return;

            List<DiskQueueSegmentWrapper<T>> _segmentsToCleanUp = null;
            bool shouldNotifyWaiters = false;
            lock (_segmentOperationsLock)
            {
                if (_isDisposed)
                    return;

                VerifyConsistency();

                shouldNotifyWaiters = allowNotification && _segments.Count >= _maxSegmentCount;

                MoveHeadToNonCompletedSegment(); // Correct head segment
                while (_segments.Count > 1 && _headSegment != _segments[0] && _segments[0].IsCompleted)
                {
                    _segmentsToCleanUp = _segmentsToCleanUp ?? new List<DiskQueueSegmentWrapper<T>>();
                    _segmentsToCleanUp.Add(_segments.RemoveFirst());
                }
            }

            if (_segmentsToCleanUp != null)
            {
                if (shouldNotifyWaiters)
                    _addMonitor.PulseAll(); // Segment allocation is now possible

                foreach (var segment in _segmentsToCleanUp)
                    segment.Dispose(DiskQueueSegmentDisposeBehaviour.Delete);
            }
        }

        /// <summary>
        /// Creates a new segment.
        /// Attention: waiters notification is required after the segment allocation (<see cref="_addMonitor"/>)
        /// </summary>
        /// <returns>Created segment</returns>
        private DiskQueueSegmentWrapper<T> AllocateNewSegment()
        {
            CheckDisposed();

            Debug.Assert(Monitor.IsEntered(_segmentOperationsLock));
            Debug.Assert(!Monitor.IsEntered(_takeMonitor));
            Debug.Assert(!Monitor.IsEntered(_peekMonitor));

            var result = _segmentFactory.CreateSegmentWrapped(_segmentsPath, checked(++_lastSegmentNumber));
            if (result == null)
                throw new InvalidOperationException("CreateSegment returned null");
            Debug.Assert(result.Number == _lastSegmentNumber);

            _segments.Add(result);
            _tailSegment.NextSegment = result;
            _tailSegment = result;

            VerifyConsistency();
            return result;
        }


        /// <summary>
        /// Moves head to the next non-completed segment
        /// </summary>
        /// <returns>New head segment</returns>
        private DiskQueueSegmentWrapper<T> MoveHeadToNonCompletedSegment()
        {
            Debug.Assert(Monitor.IsEntered(_segmentOperationsLock));

            DiskQueueSegmentWrapper<T> curHeadSegment = _headSegment;

            if (curHeadSegment.NextSegment == null || !curHeadSegment.IsCompleted)
                return curHeadSegment;

            curHeadSegment = curHeadSegment.NextSegment;
            while (curHeadSegment.NextSegment != null && curHeadSegment.IsCompleted)
                curHeadSegment = curHeadSegment.NextSegment;

            _headSegment = curHeadSegment;

            VerifyConsistency();
            return curHeadSegment;
        }


        /// <summary>
        /// Attempts to get non-full segment
        /// </summary>
        /// <returns>Non-full tail segment if found (null otherwise)</returns>
        private DiskQueueSegmentWrapper<T> TryGetNonFullTailSegment()
        {
            DiskQueueSegmentWrapper<T> result = _tailSegment;
            if (!result.IsFull)
                return result;

            bool lockTaken = false;
            bool segmentAllocated = false;

            try
            {
                Monitor.Enter(_segmentOperationsLock, ref lockTaken);

                result = _tailSegment; // Should retry inside lock
                if (!result.IsFull)
                    return result;

                if (_segments.Count < _maxSegmentCount)
                {
                    result = AllocateNewSegment();
                    segmentAllocated = true;
                    return result;
                }
            }
            finally
            {
                if (lockTaken)
                    Monitor.Exit(_segmentOperationsLock);

                if (segmentAllocated) // Segment was allocated => should notify all waiters outside the lock
                    _addMonitor.PulseAll();
            }

            return null;
        }


        /// <summary>
        /// Attempts to find non-completed head segment
        /// </summary>
        /// <returns>Non-completed head segment if found (null otherwise)</returns>
        private DiskQueueSegmentWrapper<T> TryGetNonCompletedHeadSegment()
        {
            DiskQueueSegmentWrapper<T> result = _headSegment;
            if (!result.IsCompleted)
                return result;

            if (_headSegment.NextSegment != null)
            {
                lock (_segmentOperationsLock)
                {
                    result = _headSegment; // Retry inside lock
                    if (!result.IsCompleted)
                        return result;

                    // Search for not completed segment
                    result = MoveHeadToNonCompletedSegment();
                    if (!result.IsCompleted)
                    {
                        // Perform compaction 
                        // Force compaction when we reach limit of available segments
                        if ((!IsBackgroundCompactionEnabled || _segments.Count == _maxSegmentCount) && _headSegment != _segments[0])
                            Compact(allowNotification: false);

                        return result;
                    }
                }
            }

            return null;
        }


        // ================ Add ===================

        /// <summary>
        /// Adds new item to the queue, even when the bounded capacity reached (slow path)
        /// </summary>
        /// <param name="item">New item</param>
        private void AddForcedSlow(T item)
        {
            lock (_addMonitor)
            {
                while (!_isDisposed)
                {
                    var tailSegment = TryGetNonFullTailSegment();
                    if (tailSegment == null)
                    {
                        tailSegment = _tailSegment;
                        tailSegment.AddForced(item);
                        return;
                    }
                    if (tailSegment.TryAdd(item))
                        return;

                    Debug.Assert(tailSegment.IsFull);
                }

                if (_isDisposed)
                    throw new ObjectDisposedException(this.GetType().Name);
            }

            Debug.Fail("AddForcedSlow: operation should be competed earlier");
        }


        /// <summary>
        /// Adds new item to the queue, even when the bounded capacity reached
        /// </summary>
        /// <param name="item">New item</param>
        public override void AddForced(T item)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(this.GetType().Name);

            // Fast path
            var tailSegment = _tailSegment;
            if (!tailSegment.TryAdd(item))
            {
                Debug.Assert(tailSegment.IsFull);
                AddForcedSlow(item); // Enter slow path (should be rare with reasonable size of the segment)
            }

            NotifyItemAdded();
        }



        /// <summary>
        /// Adds new item to the tail of the queue (slow path)
        /// </summary>
        /// <returns>Was added sucessufully</returns>
        private bool TryAddSlow(T item, int timeout, CancellationToken token)
        {
            TimeoutTracker timeoutTracker = new TimeoutTracker(timeout);

            if (_addMonitor.WaiterCount > 0 && timeout != 0)
                Thread.Yield();

            // Check WaiterCount to preserve fairness
            if (_addMonitor.WaiterCount == 0)
            {
                var tailSegment = TryGetNonFullTailSegment();
                if (tailSegment != null && tailSegment.TryAdd(item))
                    return true;
                if (timeout == 0 || timeoutTracker.IsTimeouted)
                    return false;
            }

            // Use waiting scheme
            using (var waiter = _addMonitor.Enter(timeoutTracker.RemainingMilliseconds, token))
            {
                do
                {
                    var tailSegment = TryGetNonFullTailSegment();
                    if (tailSegment != null && tailSegment.TryAdd(item))
                        return true;

                    Debug.Assert(tailSegment == null || tailSegment.IsFull);
                }
                while (waiter.Wait());
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

            if (_addMonitor.WaiterCount == 0) // Check WaiterCount to preserve fairness
            {
                var tailSegment = _tailSegment;
                Debug.Assert(tailSegment != null);
                result = tailSegment.TryAdd(item);
                Debug.Assert(result || tailSegment.IsFull);
            }

            if (!result)
                result = TryAddSlow(item, timeout, token); // Enter slow path (should be rare with reasonable size of the segment)

            if (result)
                NotifyItemAdded();

            return result;
        }

        // ============ Take ============



        /// <summary>
        /// Removes item from the head of the queue (slow path)
        /// </summary>
        /// <returns>Was taken sucessufully</returns>
        private bool TryTakeSlow(out T item, int timeout, CancellationToken token)
        {
            TimeoutTracker timeoutTracker = new TimeoutTracker(timeout);

            if (_takeMonitor.WaiterCount > 0 && timeout != 0)
                Thread.Yield();

            // Check WaiterCount to preserve fairness
            // Check ItemCount to prevent stucking inside lock on _takeMonitor
            if (_takeMonitor.WaiterCount == 0 || Volatile.Read(ref _itemCount) > NonFairItemThreshold)
            {
                var headSegment = TryGetNonCompletedHeadSegment();
                if (headSegment != null && headSegment.TryTake(out item))
                    return true;

                if (timeout == 0 || timeoutTracker.IsTimeouted)
                {
                    item = default(T);
                    return false;
                }
            }

            // Use waiting scheme
            using (var waiter = _takeMonitor.Enter(timeoutTracker.RemainingMilliseconds, token))
            {
                do
                {
                    var headSegment = TryGetNonCompletedHeadSegment();
                    if (headSegment != null && headSegment.TryTake(out item))
                        return true;
                }
                while (waiter.Wait());
            }

            item = default(T);
            return false;
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

            if (_takeMonitor.WaiterCount == 0) // Check WaiterCount to preserve fairness
            {
                var headSegment = _headSegment;
                Debug.Assert(headSegment != null);
                result = headSegment.TryTake(out item);
            }

            if (!result)
                result = TryTakeSlow(out item, timeout, token);

            if (result)
                NotifyItemTaken();

            return result;
        }


        // =============== Peek ===============


        /// <summary>
        /// Returns the item at the head of the queue without removing it (slow path)
        /// </summary>
        /// <returns>Was sucessufully</returns>
        private bool TryPeekSlow(out T item, int timeout, CancellationToken token)
        {
            TimeoutTracker timeoutTracker = new TimeoutTracker(timeout);

            if (_peekMonitor.WaiterCount > 0 && timeout != 0)
                Thread.Yield();

            // Check WaiterCount to preserve fairness
            // Check ItemCount to prevent stucking inside lock on _peekMonitor
            if (_peekMonitor.WaiterCount == 0 || Volatile.Read(ref _itemCount) > NonFairItemThreshold)
            {
                var headSegment = TryGetNonCompletedHeadSegment();
                if (headSegment != null && headSegment.TryPeek(out item))
                    return true;

                if (timeout == 0 || timeoutTracker.IsTimeouted)
                {
                    item = default(T);
                    return false;
                }
            }

            // Use waiting scheme
            using (var waiter = _peekMonitor.Enter(timeoutTracker.RemainingMilliseconds, token))
            {
                do
                {
                    var headSegment = TryGetNonCompletedHeadSegment();
                    if (headSegment != null && headSegment.TryPeek(out item))
                        return true;
                }
                while (waiter.Wait());
            }

            item = default(T);
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
            if (_isDisposed)
                throw new ObjectDisposedException(this.GetType().Name);
            if (token.IsCancellationRequested)
                throw new OperationCanceledException(token);

            if (_peekMonitor.WaiterCount == 0) // Preserve fairness
            {
                var headSegment = _headSegment;
                Debug.Assert(headSegment != null);
                if (headSegment.TryPeek(out item))
                    return true;
            }

            return TryPeekSlow(out item, timeout, token);
        }


        // ============== Background compaction ===============

        /// <summary>
        /// Background compaction logic
        /// </summary>
        /// <param name="token">Cancellation Token</param>
        private void BackgroundCompactionProc(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (!token.WaitHandle.WaitOne(_compactionPeriod))
                    Compact(allowNotification: true);
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

                if (_backgroundCompactionThread != null)
                    _backgroundCompactionThread.Stop(true);

                _addMonitor.Dispose();
                _takeMonitor.Dispose();
                _peekMonitor.Dispose();

                lock (_segmentOperationsLock)
                {
                    foreach (var segment in _segments)
                        segment.Dispose();
                }
            }
        }
    }
}
