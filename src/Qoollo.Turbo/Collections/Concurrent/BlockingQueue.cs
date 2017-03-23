using Qoollo.Turbo.Threading;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Collections.Concurrent
{
#pragma warning disable 0420
    /// <summary>
    /// Queue with blocking and bounding capabilities
    /// </summary>
    /// <remarks>
    /// Faster than BlockingCollection
    /// </remarks>
    /// <typeparam name="T">The type of elements in collection</typeparam>
    [DebuggerDisplay("Count = {Count}")]
    public class BlockingQueue<T>: ICollection, IEnumerable<T>, IDisposable
    {
        private const int COMPLETE_ADDING_ON_MASK = 1 << 31;

        private readonly ConcurrentQueue<T> _innerQueue;
        private volatile int _boundedCapacity;
        private volatile int _delayedBoundedCapacityDecrease;
        
        private readonly SemaphoreLight _freeNodes;
        private readonly SemaphoreLight _occupiedNodes;
        
        private readonly CancellationTokenSource _consumersCancellationTokenSource;
        private readonly CancellationTokenSource _producersCancellationTokenSource;
 
        private volatile int _currentAdders;

        private volatile bool _isDisposed;


        /// <summary>
        /// BlockingQueue constructor
        /// </summary>
        /// <param name="boundedCapacity">The bounded size of the queue (if less or equeal to 0 then no limitation)</param>
        public BlockingQueue(int boundedCapacity)
        {
            _innerQueue = new ConcurrentQueue<T>();
            _boundedCapacity = boundedCapacity >= 0 ? boundedCapacity : -1;
            _delayedBoundedCapacityDecrease = 0;
            _consumersCancellationTokenSource = new CancellationTokenSource();
            _producersCancellationTokenSource = new CancellationTokenSource();
            _isDisposed = false;

            if (boundedCapacity > 0)
                _freeNodes = new SemaphoreLight(boundedCapacity);

            _occupiedNodes = new SemaphoreLight(0);
        }
        /// <summary>
        /// BlockingQueue constructor
        /// </summary>
        public BlockingQueue()
            : this(-1)
        {
        }


        /// <summary>
        /// The bounded size of the queue
        /// </summary>
        public int BoundedCapacity { get { return _boundedCapacity; } }
        /// <summary>
        /// Is queue marked as Completed for Adding
        /// </summary>
        public bool IsAddingCompleted { get { return _currentAdders == COMPLETE_ADDING_ON_MASK; } }
        /// <summary>
        /// Is queue is empty and Adding completed
        /// </summary>
        public bool IsCompleted { get { return (IsAddingCompleted && (_occupiedNodes.CurrentCount == 0)); } }
        /// <summary>
        /// Number of items inside the queue
        /// </summary>
        public int Count { get { return _occupiedNodes.CurrentCount; } }


        /// <summary>
        /// Checks if queue is disposed
        /// </summary>
        private void CheckDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException("BlockingQueue");
        }

        /// <summary>
        /// Increase queue bounded capacity
        /// </summary>
        /// <param name="increaseValue">The number of items by which the bounded capacity should be increased</param>
        public void IncreaseBoundedCapacity(int increaseValue)
        {
            CheckDisposed();
            if (increaseValue < 0)
                throw new ArgumentException(nameof(increaseValue), "increaseValue should be positive");

            if (_freeNodes == null)
                return;

            if (increaseValue == 0)
                return;

            try { }
            finally
            {
                _freeNodes.Release(increaseValue);
                Interlocked.Add(ref _boundedCapacity, increaseValue);
            }
        }

        /// <summary>
        /// Updates the inner filed to track the value of delayed capacity decreasing
        /// </summary>
        /// <param name="decreaseValue">The number of items by which the bounded capacity should be decreased</param>
        private void UpdateDelayedBoundedCapacityDecreaseField(int decreaseValue)
        {
            Debug.Assert(decreaseValue >= 0);

            SpinWait sw = new SpinWait();
            int delayedBoundedCapacityDecrease = _delayedBoundedCapacityDecrease;
            while (Interlocked.CompareExchange(ref _delayedBoundedCapacityDecrease, Math.Max(0, delayedBoundedCapacityDecrease) + decreaseValue, delayedBoundedCapacityDecrease) != delayedBoundedCapacityDecrease)
            {
                sw.SpinOnce();
                delayedBoundedCapacityDecrease = _delayedBoundedCapacityDecrease;
            }
        }
        /// <summary>
        /// Decrease queue bounded capacity
        /// </summary>
        /// <param name="decreaseValue">The number of items by which the bounded capacity should be decreased</param>
        public void DecreaseBoundedCapacity(int decreaseValue)
        {
            CheckDisposed();
            if (decreaseValue < 0)
                throw new ArgumentException(nameof(decreaseValue), "decreaseValue should be positive");

            if (_freeNodes == null)
                return;

            if (decreaseValue == 0)
                return;

            try { }
            finally
            {
                SpinWait sw = new SpinWait();
                int boundedCapacity = _boundedCapacity;
                int newBoundedCapacity = Math.Max(0, boundedCapacity - decreaseValue);
                while (Interlocked.CompareExchange(ref _boundedCapacity, newBoundedCapacity, boundedCapacity) != boundedCapacity)
                {
                    sw.SpinOnce();
                    boundedCapacity = _boundedCapacity;
                    newBoundedCapacity = Math.Max(0, boundedCapacity - decreaseValue);
                }

                int realDiff = boundedCapacity - newBoundedCapacity;
                UpdateDelayedBoundedCapacityDecreaseField(realDiff);


                while (_delayedBoundedCapacityDecrease > 0 && _freeNodes.Wait(0))
                {
                    if (Interlocked.Decrement(ref _delayedBoundedCapacityDecrease) < 0)
                    {
                        _freeNodes.Release();
                        break;
                    }
                }
            }
        }
        /// <summary>
        /// Sets new bounded capacity for the queue
        /// </summary>
        /// <param name="newBoundedCapacity">New bounded size of the queue</param>
        public void SetBoundedCapacity(int newBoundedCapacity)
        {
            CheckDisposed();
            if (newBoundedCapacity < 0)
                throw new ArgumentException(nameof(newBoundedCapacity), "newBoundedCapacity should be positive");

            if (_freeNodes == null)
                return;

            int boundedCapacity = _boundedCapacity;
            if (boundedCapacity < newBoundedCapacity)
                IncreaseBoundedCapacity(newBoundedCapacity - boundedCapacity);
            else if (boundedCapacity > newBoundedCapacity)
                DecreaseBoundedCapacity(boundedCapacity - newBoundedCapacity);
        }








        /// <summary>
        /// Adds new item to the queue, even when the bounded capacity reached
        /// </summary>
        /// <param name="item">New item</param>
        public void AddForced(T item)
        {
            CheckDisposed();

            if (IsAddingCompleted)
                throw new InvalidOperationException("Adding was completed for BlockingQueue");

            try { }
            finally
            {
                _innerQueue.Enqueue(item);
                if (_freeNodes != null && !_freeNodes.Wait(0)) // Attempt to fill _freeNodes
                    UpdateDelayedBoundedCapacityDecreaseField(1);
                _occupiedNodes.Release();
            }
        }
        /// <summary>
        /// Adds new item to the queue, even when the bounded capacity reached
        /// </summary>
        /// <param name="item">New item</param>
        [Obsolete("Use AddForced")]
        public void EnqueueForced(T item)
        {
            this.AddForced(item);
        }




        /// <summary>
        /// Add new item to queue (simple and fast version)
        /// </summary>
        /// <param name="item">New item</param>
        /// <returns>True if item was added, otherwise false</returns>
        internal bool TryAddFast(T item)
        {
            CheckDisposed();

            if (IsAddingCompleted)
                throw new InvalidOperationException("Adding was completed for BlockingQueue");


            if (_freeNodes != null && (_freeNodes.CurrentCount == 0 || !_freeNodes.Wait(0)))
                return false;

            try
            {
                _innerQueue.Enqueue(item);
            }
            catch
            {
                if (_freeNodes != null)
                    _freeNodes.Release();
                throw;
            }
            _occupiedNodes.Release();

            return true;
        }


        /// <summary>
        /// Adds new item to the tail of the queue (inner core method)
        /// </summary>
        /// <param name="item">New item</param>
        /// <param name="timeout">Adding timeout</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>Was added sucessufully</returns>
        /// <exception cref="OperationCanceledException">Cancellation was requested by token</exception>
        /// <exception cref="OperationInterruptedException">Operation was interrupted by disposing</exception>
        /// <exception cref="InvalidOperationException">Adding was completed</exception>
        /// <exception cref="ObjectDisposedException">Queue was disposed</exception>
        private bool TryAddInner(T item, int timeout, CancellationToken token)
        {
            CheckDisposed();

            if (token.IsCancellationRequested)
                throw new OperationCanceledException(token);

            if (IsAddingCompleted)
                throw new InvalidOperationException("Adding was completed for BlockingQueue");

            bool waitForSemaphoreWasSuccessful = true;

            if (_freeNodes != null)
            {
                CancellationTokenSource linkedTokenSource = null;
                try
                {
                    waitForSemaphoreWasSuccessful = _freeNodes.Wait(0);
                    if (waitForSemaphoreWasSuccessful == false && timeout != 0)
                    {
                        if (token.CanBeCanceled)
                        {
                            linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token, _producersCancellationTokenSource.Token);
                            waitForSemaphoreWasSuccessful = _freeNodes.Wait(timeout, linkedTokenSource.Token);
                        }
                        else
                        {
                            waitForSemaphoreWasSuccessful = _freeNodes.Wait(timeout, _producersCancellationTokenSource.Token);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    if (token.IsCancellationRequested)
                        throw new OperationCanceledException(token);

                    throw new OperationInterruptedException("Add operation in BlockingQueue was interrupted by CompleteAdd");
                }
                finally
                {
                    if (linkedTokenSource != null)
                        linkedTokenSource.Dispose();
                }
            }

            if (!waitForSemaphoreWasSuccessful)
                return false;


            bool currentAddersUpdated = false;
            bool elementWasTaken = false;
            try
            {
                int currentAdders = _currentAdders;
                if ((currentAdders & COMPLETE_ADDING_ON_MASK) != 0)
                {
                    SpinWait completeSw = new SpinWait();
                    while (_currentAdders != COMPLETE_ADDING_ON_MASK)
                        completeSw.SpinOnce();

                    throw new OperationInterruptedException("Add operation in BlockingQueue was interrupted by CompleteAdd");
                }

                currentAdders = Interlocked.Increment(ref _currentAdders);
                currentAddersUpdated = true;

                if ((currentAdders & COMPLETE_ADDING_ON_MASK) != 0)
                {
                    SpinWait completeSw = new SpinWait();
                    while (_currentAdders != COMPLETE_ADDING_ON_MASK)
                        completeSw.SpinOnce();

                    throw new OperationInterruptedException("Add operation in BlockingQueue was interrupted by CompleteAdd");
                }


                token.ThrowIfCancellationRequested();
                _innerQueue.Enqueue(item);
                elementWasTaken = true;

                _occupiedNodes.Release();
            }
            finally
            {
                if (!elementWasTaken && _freeNodes != null)
                {
                    _freeNodes.Release();
                }

                if (currentAddersUpdated)
                {
                    Debug.Assert((_currentAdders & ~COMPLETE_ADDING_ON_MASK) > 0);
                    Interlocked.Decrement(ref _currentAdders);
                }
            }

            return true;
        }


        /// <summary>
        /// Adds the item to the tail of the queue
        /// </summary>
        /// <param name="item">New item</param>
        /// <exception cref="OperationInterruptedException">Operation was interrupted by disposing</exception>
        /// <exception cref="InvalidOperationException">Adding was completed</exception>
        /// <exception cref="ObjectDisposedException">Queue was disposed</exception>
        [Obsolete("Method was renamed. Consider to use 'Add' instead")]
        public void Enqueue(T item)
        {
            bool addResult = TryAddInner(item, Timeout.Infinite, new CancellationToken());
            Debug.Assert(addResult);
        }
        /// <summary>
        /// Adds the item to the tail of the queue
        /// </summary>
        /// <param name="item">New item</param>
        /// <param name="token">Cancellation token</param>
        /// <exception cref="OperationCanceledException">Cancellation was requested by token</exception>
        /// <exception cref="OperationInterruptedException">Operation was interrupted by disposing</exception>
        /// <exception cref="InvalidOperationException">Adding was completed</exception>
        /// <exception cref="ObjectDisposedException">Queue was disposed</exception>
        [Obsolete("Method was renamed. Consider to use 'Add' instead")]
        public void Enqueue(T item, CancellationToken token)
        {
            bool addResult = TryAddInner(item, Timeout.Infinite, token);
            Debug.Assert(addResult);
        }

        /// <summary>
        /// Attempts to add new item to tail of the the queue
        /// </summary>
        /// <param name="item">New item</param>
        /// <returns>True if item was added, otherwise false</returns>
        /// <exception cref="InvalidOperationException">Adding was completed</exception>
        /// <exception cref="ObjectDisposedException">Queue was disposed</exception>
        [Obsolete("Method was renamed. Consider to use 'TryAdd' instead")]
        public bool TryEnqueue(T item)
        {
            return TryAddInner(item, 0, new CancellationToken());
        }
        /// <summary>
        /// Attempts to add new item to tail of the the queue
        /// </summary>
        /// <param name="item">New item</param>
        /// <param name="timeout">Adding timeout</param>
        /// <returns>True if item was added, otherwise false</returns>
        /// <exception cref="OperationInterruptedException">Operation was interrupted by disposing</exception>
        /// <exception cref="InvalidOperationException">Adding was completed</exception>
        /// <exception cref="ObjectDisposedException">Queue was disposed</exception>
        [Obsolete("Method was renamed. Consider to use 'TryAdd' instead")]
        public bool TryEnqueue(T item, TimeSpan timeout)
        {
            long timeoutMs = (long)timeout.TotalMilliseconds;
            if (timeoutMs > int.MaxValue)
                throw new ArgumentOutOfRangeException("timeout");
            if (timeoutMs < 0)
                timeoutMs = Timeout.Infinite;

            return TryAddInner(item, (int)timeoutMs, new CancellationToken());
        }
        /// <summary>
        /// Attempts to add new item to tail of the the queue
        /// </summary>
        /// <param name="item">New item</param>
        /// <param name="timeout">Adding timeout</param>
        /// <returns>True if item was added, otherwise false</returns>
        /// <exception cref="OperationInterruptedException">Operation was interrupted by disposing</exception>
        /// <exception cref="InvalidOperationException">Adding was completed</exception>
        /// <exception cref="ObjectDisposedException">Queue was disposed</exception>
        [Obsolete("Method was renamed. Consider to use 'TryAdd' instead")]
        public bool TryEnqueue(T item, int timeout)
        {
            if (timeout < 0)
                timeout = Timeout.Infinite;
            return TryAddInner(item, timeout, new CancellationToken());
        }
        /// <summary>
        /// Attempts to add new item to the tail of the queue
        /// </summary>
        /// <param name="item">New item</param>
        /// <param name="timeout">Adding timeout</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>True if item was added, otherwise false</returns>
        /// <exception cref="OperationCanceledException">Cancellation was requested by token</exception>
        /// <exception cref="OperationInterruptedException">Operation was interrupted by disposing</exception>
        /// <exception cref="InvalidOperationException">Adding was completed</exception>
        /// <exception cref="ObjectDisposedException">Queue was disposed</exception>
        [Obsolete("Method was renamed. Consider to use 'TryAdd' instead")]
        public bool TryEnqueue(T item, int timeout, CancellationToken token)
        {
            if (timeout < 0)
                timeout = Timeout.Infinite;
            return TryAddInner(item, timeout, token);
        }

        /// <summary>
        /// Adds the item to the tail of the queue
        /// </summary>
        /// <param name="item">New item</param>
        /// <exception cref="OperationInterruptedException">Operation was interrupted by disposing</exception>
        /// <exception cref="InvalidOperationException">Adding was completed</exception>
        /// <exception cref="ObjectDisposedException">Queue was disposed</exception>
        public void Add(T item)
        {
            bool addResult = TryAddInner(item, Timeout.Infinite, new CancellationToken());
            Debug.Assert(addResult);
        }
        /// <summary>
        /// Adds the item to the tail of the queue
        /// </summary>
        /// <param name="item">New item</param>
        /// <param name="token">Cancellation token</param>
        /// <exception cref="OperationCanceledException">Cancellation was requested by token</exception>
        /// <exception cref="OperationInterruptedException">Operation was interrupted by disposing</exception>
        /// <exception cref="InvalidOperationException">Adding was completed</exception>
        /// <exception cref="ObjectDisposedException">Queue was disposed</exception>
        public void Add(T item, CancellationToken token)
        {
            bool addResult = TryAddInner(item, Timeout.Infinite, token);
            Debug.Assert(addResult);
        }

        /// <summary>
        /// Attempts to add new item to tail of the the queue
        /// </summary>
        /// <param name="item">New item</param>
        /// <returns>True if item was added, otherwise false</returns>
        /// <exception cref="InvalidOperationException">Adding was completed</exception>
        /// <exception cref="ObjectDisposedException">Queue was disposed</exception>
        public bool TryAdd(T item)
        {
            return TryAddInner(item, 0, new CancellationToken());
        }
        /// <summary>
        /// Attempts to add new item to tail of the the queue
        /// </summary>
        /// <param name="item">New item</param>
        /// <param name="timeout">Adding timeout</param>
        /// <returns>True if item was added, otherwise false</returns>
        /// <exception cref="OperationInterruptedException">Operation was interrupted by disposing</exception>
        /// <exception cref="InvalidOperationException">Adding was completed</exception>
        /// <exception cref="ObjectDisposedException">Queue was disposed</exception>
        public bool TryAdd(T item, TimeSpan timeout)
        {
            long timeoutMs = (long)timeout.TotalMilliseconds;
            if (timeoutMs > int.MaxValue)
                throw new ArgumentOutOfRangeException("timeout");
            if (timeoutMs < 0)
                timeoutMs = Timeout.Infinite;

            return TryAddInner(item, (int)timeoutMs, new CancellationToken());
        }
        /// <summary>
        /// Attempts to add new item to tail of the the queue
        /// </summary>
        /// <param name="item">New item</param>
        /// <param name="timeout">Adding timeout</param>
        /// <returns>True if item was added, otherwise false</returns>
        /// <exception cref="OperationInterruptedException">Operation was interrupted by disposing</exception>
        /// <exception cref="InvalidOperationException">Adding was completed</exception>
        /// <exception cref="ObjectDisposedException">Queue was disposed</exception>
        public bool TryAdd(T item, int timeout)
        {
            if (timeout < 0)
                timeout = Timeout.Infinite;
            return TryAddInner(item, timeout, new CancellationToken());
        }
        /// <summary>
        /// Attempts to add new item to the tail of the queue
        /// </summary>
        /// <param name="item">New item</param>
        /// <param name="timeout">Adding timeout</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>True if item was added, otherwise false</returns>
        /// <exception cref="OperationCanceledException">Cancellation was requested by token</exception>
        /// <exception cref="OperationInterruptedException">Operation was interrupted by disposing</exception>
        /// <exception cref="InvalidOperationException">Adding was completed</exception>
        /// <exception cref="ObjectDisposedException">Queue was disposed</exception>
        public bool TryAdd(T item, int timeout, CancellationToken token)
        {
            if (timeout < 0)
                timeout = Timeout.Infinite;
            return TryAddInner(item, timeout, token);
        }



        /// <summary>
        /// Attempts to remove item from queue (simple and fast version)
        /// </summary>
        /// <param name="item">The item removed from queue</param>
        /// <returns>True if the item was removed</returns>
        internal bool TryTakeFast(out T item)
        {
            CheckDisposed();
            item = default(T);

            if (_occupiedNodes.CurrentCount == 0)
                return false;

            if (!_occupiedNodes.Wait(0))
                return false;

            bool removeSucceeded = false;
            bool removeFaulted = true;
            try
            {
                removeSucceeded = _innerQueue.TryDequeue(out item);
                Debug.Assert(removeSucceeded, "Take from underlying collection return false");
                removeFaulted = false;
            }
            finally
            {
                if (removeSucceeded)
                {
                    if (_freeNodes != null)
                    {
                        if (_delayedBoundedCapacityDecrease <= 0 || Interlocked.Decrement(ref _delayedBoundedCapacityDecrease) < 0)
                            _freeNodes.Release();
                    }
                }
                else if (removeFaulted)
                {
                    _occupiedNodes.Release();
                }

                if (IsCompleted)
                    _consumersCancellationTokenSource.Cancel();
            }

            return removeSucceeded;
        }


        /// <summary>
        /// Removes item from the head of the queue (inner core method)
        /// </summary>
        /// <param name="item">The item removed from queue</param>
        /// <param name="timeout">Removing timeout</param>
        /// <param name="token">Cancellation token</param>
        /// <param name="throwOnCancellation">Should throw OperationCancelledException when Cancellation requested or just return false</param>
        /// <returns>True if the item was removed</returns>
        /// <exception cref="OperationCanceledException">Cancellation was requested by token</exception>
        /// <exception cref="ObjectDisposedException">Queue was disposed</exception>
        private bool TryTakeInner(out T item, int timeout, CancellationToken token, bool throwOnCancellation)
        {
            CheckDisposed();
            item = default(T);

            if (token.IsCancellationRequested)
            {
                if (!throwOnCancellation)
                    return false;

                throw new OperationCanceledException(token);
            }

            if (IsCompleted)
                return false;


            bool waitForSemaphoreWasSuccessful = _occupiedNodes.Wait(0);
            if (waitForSemaphoreWasSuccessful == false && timeout != 0)
            {
                if (token.CanBeCanceled)
                {
                    using (var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token, _consumersCancellationTokenSource.Token))
                    {
                        waitForSemaphoreWasSuccessful = _occupiedNodes.Wait(timeout, linkedTokenSource.Token, throwOnCancellation);
                    }            
                }
                else
                {
                    waitForSemaphoreWasSuccessful = _occupiedNodes.Wait(timeout, _consumersCancellationTokenSource.Token, throwOnCancellation);
                }
            }

            if (!waitForSemaphoreWasSuccessful)
                return false;

            bool removeSucceeded = false;
            bool removeFaulted = true;
            try
            {
                if (token.IsCancellationRequested)
                {
                    if (!throwOnCancellation)
                        return false;

                    throw new OperationCanceledException(token);
                }
                removeSucceeded = _innerQueue.TryDequeue(out item);
                Debug.Assert(removeSucceeded, "Take from underlying collection return false");
                removeFaulted = false;
            }
            finally
            {
                if (removeSucceeded)
                {
                    if (_freeNodes != null)
                    {
                        if (_delayedBoundedCapacityDecrease <= 0 || Interlocked.Decrement(ref _delayedBoundedCapacityDecrease) < 0)
                            _freeNodes.Release();
                    }
                }
                else if (removeFaulted && waitForSemaphoreWasSuccessful)
                {
                    _occupiedNodes.Release();
                }

                if (IsCompleted)
                    _consumersCancellationTokenSource.Cancel();
            }

            return removeSucceeded;
        }




        /// <summary>
        /// Removes item from the head of the queue
        /// </summary>
        /// <returns>The item removed from queue</returns>
        /// <exception cref="ObjectDisposedException">Queue was disposed</exception>
        [Obsolete("Method was renamed. Consider to use 'Take' instead")]
        public T Dequeue()
        {
            T result;
            bool takeResult = TryTakeInner(out result, Timeout.Infinite, new CancellationToken(), true);
            Debug.Assert(takeResult);
 
            return result;
        }
        /// <summary>
        /// Removes item from the head of the queue
        /// </summary>
        /// <param name="token">Cancellation token</param>
        /// <returns>The item removed from queue</returns>
        /// <exception cref="OperationCanceledException">Cancellation was requested by token</exception>
        /// <exception cref="ObjectDisposedException">Queue was disposed</exception>
        [Obsolete("Method was renamed. Consider to use 'Take' instead")]
        public T Dequeue(CancellationToken token)
        {
            T result;
            bool takeResult = TryTakeInner(out result, Timeout.Infinite, token, true);
            Debug.Assert(takeResult);

            return result;
        }
 
        /// <summary>
        /// Attempts to remove item from the head of the queue
        /// </summary>
        /// <param name="item">The item removed from queue</param>
        /// <returns>True if the item was removed</returns>
        /// <exception cref="ObjectDisposedException">Queue was disposed</exception>
        [Obsolete("Method was renamed. Consider to use 'TryTake' instead")]
        public bool TryDequeue(out T item)
        {
            return TryTakeInner(out item, 0, CancellationToken.None, true);
        }
        /// <summary>
        /// Attempts to remove item from the head of the queue
        /// </summary>
        /// <param name="item">The item removed from queue</param>
        /// <param name="timeout">Removing timeout</param>
        /// <returns>True if the item was removed</returns>
        /// <exception cref="ObjectDisposedException">Queue was disposed</exception>
        [Obsolete("Method was renamed. Consider to use 'TryTake' instead")]
        public bool TryDequeue(out T item, TimeSpan timeout)
        {
            long timeoutMs = (long)timeout.TotalMilliseconds;
            if (timeoutMs > int.MaxValue)
                throw new ArgumentOutOfRangeException("timeout");
            if (timeoutMs < 0)
                timeoutMs = Timeout.Infinite;

            return TryTakeInner(out item, (int)timeoutMs, new CancellationToken(), true);
        }
        /// <summary>
        /// Attempts to remove item from the head of the queue
        /// </summary>
        /// <param name="item">The item removed from queue</param>
        /// <param name="timeout">Removing timeout in milliseconds</param>
        /// <returns>True if the item was removed</returns>
        /// <exception cref="ObjectDisposedException">Queue was disposed</exception>
        [Obsolete("Method was renamed. Consider to use 'TryTake' instead")]
        public bool TryDequeue(out T item, int timeout)
        {
            if (timeout < 0)
                timeout = Timeout.Infinite;
            return TryTakeInner(out item, timeout, new CancellationToken(), true);
        }
        /// <summary>
        /// Attempts to remove item from the head of the queue
        /// </summary>
        /// <param name="item">The item removed from queue</param>
        /// <param name="timeout">Removing timeout in milliseconds</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>True if the item was removed</returns>
        [Obsolete("Method was renamed. Consider to use 'TryTake' instead")]
        public bool TryDequeue(out T item, int timeout, CancellationToken token)
        {
            if (timeout < 0)
                timeout = Timeout.Infinite;
            return TryTakeInner(out item, timeout, token, true);
        }
        /// <summary>
        /// Attempts to remove item from the head of the queue
        /// </summary>
        /// <param name="item">The item removed from queue</param>
        /// <param name="timeout">Removing timeout in milliseconds</param>
        /// <param name="token">Cancellation token</param>
        /// <param name="throwOnCancellation">Should throw OperationCancelledException when Cancellation requested or just return false</param>
        /// <returns>True if the item was removed</returns>
        /// <exception cref="OperationCanceledException">Cancellation was requested by token</exception>
        /// <exception cref="ObjectDisposedException">Queue was disposed</exception>
        [Obsolete("Method was renamed. Consider to use 'TryTake' instead")]
        internal bool TryDequeue(out T item, int timeout, CancellationToken token, bool throwOnCancellation)
        {
            if (timeout < 0)
                timeout = Timeout.Infinite;
            return TryTakeInner(out item, timeout, token, throwOnCancellation);
        }




        /// <summary>
        /// Removes item from the head of the queue
        /// </summary>
        /// <returns>The item removed from queue</returns>
        /// <exception cref="ObjectDisposedException">Queue was disposed</exception>
        public T Take()
        {
            T result;
            bool takeResult = TryTakeInner(out result, Timeout.Infinite, new CancellationToken(), true);
            Debug.Assert(takeResult);

            return result;
        }
        /// <summary>
        /// Removes item from the head of the queue
        /// </summary>
        /// <param name="token">Cancellation token</param>
        /// <returns>The item removed from queue</returns>
        /// <exception cref="OperationCanceledException">Cancellation was requested by token</exception>
        /// <exception cref="ObjectDisposedException">Queue was disposed</exception>
        public T Take(CancellationToken token)
        {
            T result;
            bool takeResult = TryTakeInner(out result, Timeout.Infinite, token, true);
            Debug.Assert(takeResult);

            return result;
        }

        /// <summary>
        /// Attempts to remove item from the head of the queue
        /// </summary>
        /// <param name="item">The item removed from queue</param>
        /// <returns>True if the item was removed</returns>
        /// <exception cref="ObjectDisposedException">Queue was disposed</exception>
        public bool TryTake(out T item)
        {
            return TryTakeInner(out item, 0, CancellationToken.None, true);
        }
        /// <summary>
        /// Attempts to remove item from the head of the queue
        /// </summary>
        /// <param name="item">The item removed from queue</param>
        /// <param name="timeout">Removing timeout</param>
        /// <returns>True if the item was removed</returns>
        /// <exception cref="ObjectDisposedException">Queue was disposed</exception>
        public bool TryTake(out T item, TimeSpan timeout)
        {
            long timeoutMs = (long)timeout.TotalMilliseconds;
            if (timeoutMs > int.MaxValue)
                throw new ArgumentOutOfRangeException("timeout");
            if (timeoutMs < 0)
                timeoutMs = Timeout.Infinite;

            return TryTakeInner(out item, (int)timeoutMs, new CancellationToken(), true);
        }
        /// <summary>
        /// Attempts to remove item from the head of the queue
        /// </summary>
        /// <param name="item">The item removed from queue</param>
        /// <param name="timeout">Removing timeout in milliseconds</param>
        /// <returns>True if the item was removed</returns>
        /// <exception cref="ObjectDisposedException">Queue was disposed</exception>
        public bool TryTake(out T item, int timeout)
        {
            if (timeout < 0)
                timeout = Timeout.Infinite;
            return TryTakeInner(out item, timeout, new CancellationToken(), true);
        }
        /// <summary>
        /// Attempts to remove item from the head of the queue
        /// </summary>
        /// <param name="item">The item removed from queue</param>
        /// <param name="timeout">Removing timeout in milliseconds</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>True if the item was removed</returns>
        public bool TryTake(out T item, int timeout, CancellationToken token)
        {
            if (timeout < 0)
                timeout = Timeout.Infinite;
            return TryTakeInner(out item, timeout, token, true);
        }
        /// <summary>
        /// Attempts to remove item from the head of the queue
        /// </summary>
        /// <param name="item">The item removed from queue</param>
        /// <param name="timeout">Removing timeout in milliseconds</param>
        /// <param name="token">Cancellation token</param>
        /// <param name="throwOnCancellation">Should throw OperationCancelledException when Cancellation requested or just return false</param>
        /// <returns>True if the item was removed</returns>
        /// <exception cref="OperationCanceledException">Cancellation was requested by token</exception>
        /// <exception cref="ObjectDisposedException">Queue was disposed</exception>
        internal bool TryTake(out T item, int timeout, CancellationToken token, bool throwOnCancellation)
        {
            if (timeout < 0)
                timeout = Timeout.Infinite;
            return TryTakeInner(out item, timeout, token, throwOnCancellation);
        }




        /// <summary>
        /// Returns the item at the head of the queue without removing it (inner core method)
        /// </summary>
        /// <param name="item">The item at the head of the queue</param>
        /// <param name="timeout">Peeking timeout</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>True if the item was read</returns>
        /// <exception cref="OperationCanceledException">Cancellation was requested by token</exception>
        /// <exception cref="ObjectDisposedException">Queue was disposed</exception>
        private bool TryPeekInner(out T item, int timeout, CancellationToken token)
        {
            CheckDisposed();
            item = default(T);

            if (token.IsCancellationRequested)
                throw new OperationCanceledException(token);

            if (IsCompleted)
                return false;

            if (_innerQueue.TryPeek(out item))
                return true;


            bool waitForSemaphoreWasSuccessful = false;

            CancellationTokenSource linkedTokenSource = null;
            try
            {
                waitForSemaphoreWasSuccessful = _occupiedNodes.Wait(0);
                if (waitForSemaphoreWasSuccessful == false && timeout != 0)
                {
                    if (token.CanBeCanceled)
                    {
                        linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token, _consumersCancellationTokenSource.Token);
                        waitForSemaphoreWasSuccessful = _occupiedNodes.Wait(timeout, linkedTokenSource.Token);
                    }
                    else
                    {
                        waitForSemaphoreWasSuccessful = _occupiedNodes.Wait(timeout, _consumersCancellationTokenSource.Token);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                if (token.IsCancellationRequested)
                    throw new OperationCanceledException(token);

                return false;
            }
            finally
            {
                if (linkedTokenSource != null)
                    linkedTokenSource.Dispose();
            }

            if (!waitForSemaphoreWasSuccessful)
                return false;

            try
            {
                token.ThrowIfCancellationRequested();
                return _innerQueue.TryPeek(out item);
            }
            finally
            {
                _occupiedNodes.Release();
            }
        }




        /// <summary>
        /// Returns the item at the head of the queue without removing it (blocks the Thread when queue is empty)
        /// </summary>
        /// <returns>The item at the head of the queue</returns>
        /// <exception cref="ObjectDisposedException">Queue was disposed</exception>
        public T Peek()
        {
            T result;
            bool takeResult = TryPeekInner(out result, Timeout.Infinite, new CancellationToken());
            Debug.Assert(takeResult);

            return result;
        }
        /// <summary>
        /// Returns the item at the head of the queue without removing it (blocks the Thread when queue is empty)
        /// </summary>
        /// <param name="token">Cancellation token</param>
        /// <returns>The item at the head of the queue</returns>
        /// <exception cref="OperationCanceledException">Cancellation was requested by token</exception>
        /// <exception cref="ObjectDisposedException">Queue was disposed</exception>
        public T Peek(CancellationToken token)
        {
            T result;
            bool takeResult = TryPeekInner(out result, Timeout.Infinite, token);
            Debug.Assert(takeResult);

            return result;
        }

        /// <summary>
        /// Attempts to read the item at the head of the queue without removing it
        /// </summary>
        /// <param name="item">The item at the head of the queue</param>
        /// <returns>True if the item was read</returns>
        /// <exception cref="ObjectDisposedException">Queue was disposed</exception>
        public bool TryPeek(out T item)
        {
            return _innerQueue.TryPeek(out item);
        }
        /// <summary>
        /// Attempts to read the item at the head of the queue without removing it
        /// </summary>
        /// <param name="item">The item at the head of the queue</param>
        /// <param name="timeout">Peeking timeout</param>
        /// <returns>True if the item was read</returns>
        /// <exception cref="ObjectDisposedException">Queue was disposed</exception>
        public bool TryPeek(out T item, TimeSpan timeout)
        {
            long timeoutMs = (long)timeout.TotalMilliseconds;
            if (timeoutMs > int.MaxValue)
                throw new ArgumentOutOfRangeException("timeout");
            if (timeoutMs < 0)
                timeoutMs = Timeout.Infinite;

            if (timeoutMs == 0)
                return _innerQueue.TryPeek(out item);

            return TryPeekInner(out item, (int)timeoutMs, new CancellationToken());
        }
        /// <summary>
        /// Attempts to read the item at the head of the queue without removing it
        /// </summary>
        /// <param name="item">The item at the head of the queue</param>
        /// <param name="timeout">Peeking timeout in milliseconds</param>
        /// <returns>True if the item was read</returns>
        /// <exception cref="ObjectDisposedException">Queue was disposed</exception>
        public bool TryPeek(out T item, int timeout)
        {
            if (timeout < 0)
                timeout = Timeout.Infinite;

            if (timeout == 0)
                return _innerQueue.TryPeek(out item);

            return TryPeekInner(out item, timeout, new CancellationToken());
        }
        /// <summary>
        /// Attempts to read the item at the head of the queue without removing it
        /// </summary>
        /// <param name="item">The item at the head of the queue</param>
        /// <param name="timeout">Peeking timeout in milliseconds</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>True if the item was read</returns>
        /// <exception cref="OperationCanceledException">Cancellation was requested by token</exception>
        /// <exception cref="ObjectDisposedException">Queue was disposed</exception>
        public bool TryPeek(out T item, int timeout, CancellationToken token)
        {
            if (timeout < 0)
                timeout = Timeout.Infinite;

            if (timeout == 0 && !token.IsCancellationRequested)
                return _innerQueue.TryPeek(out item);

            return TryPeekInner(out item, timeout, token);
        }


 
 
        /// <summary>
        /// Marks queue as completed for adding
        /// </summary>
        public void CompleteAdding()
        {
            CheckDisposed();
 
            if (IsAddingCompleted)
                return;
 
            SpinWait sw = new SpinWait();
            int currentAdders = _currentAdders;

            while (true)
            {
                if ((currentAdders & COMPLETE_ADDING_ON_MASK) != 0)
                {
                    SpinWait completeSw = new SpinWait();
                    while (_currentAdders != COMPLETE_ADDING_ON_MASK) 
                        completeSw.SpinOnce();
                    return;
                }

                if (Interlocked.CompareExchange(ref _currentAdders, currentAdders | COMPLETE_ADDING_ON_MASK, currentAdders) == currentAdders)
                {
                    SpinWait completeSw = new SpinWait();
                    while (_currentAdders != COMPLETE_ADDING_ON_MASK)
                        completeSw.SpinOnce();

                    if (Count == 0)
                        _consumersCancellationTokenSource.Cancel();

                    _producersCancellationTokenSource.Cancel();
                    return;
 
                }

                sw.SpinOnce();
                currentAdders = _currentAdders;
            }
        } 
 
 
        /// <summary>
        /// Copies all items from queue into a new array
        /// </summary>
        /// <returns>An array</returns>
        public T[] ToArray()
        {
            return _innerQueue.ToArray();
        }
 
        /// <summary>
        /// Copies all items from queue into a specified array
        /// </summary>
        /// <param name="array">Array that is the destination of the elements copied</param>
        /// <param name="index">Index in array at which copying begins</param>
        public void CopyTo(T[] array, int index)
        {
            Debug.Assert(array != null);
            Debug.Assert(index >= 0 && index < array.Length);

            _innerQueue.CopyTo(array, index);
        }
 

        /// <summary>
        /// Returns Enumerator
        /// </summary>
        /// <returns>Enumerator</returns>
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            CheckDisposed();
            return _innerQueue.GetEnumerator();
 
        }
        /// <summary>
        /// Returns Enumerator
        /// </summary>
        /// <returns>Enumerator</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<T>)this).GetEnumerator();
        }

        /// <summary>
        /// Is synchronized collection
        /// </summary>
        bool ICollection.IsSynchronized { get { return false; } }
        /// <summary>
        /// Sychronization object (not supported)
        /// </summary>
        object ICollection.SyncRoot
        {
            get
            {
                throw new NotSupportedException("SyncRoot is not supported for BlockingQueue");
            }
        }

        /// <summary>
        /// Copy queue items to the array
        /// </summary>
        /// <param name="array">Target array</param>
        /// <param name="index">Start index</param>
        void ICollection.CopyTo(Array array, int index)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (index < 0 || index >= array.Length)
                throw new ArgumentOutOfRangeException(nameof(index));

            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (index < 0 || index >= array.Length)
                throw new ArgumentOutOfRangeException(nameof(index));

 
            T[] localArray = _innerQueue.ToArray();
            if (array.Length - index < localArray.Length)
                throw new ArgumentException("Not enough space in target array");


            Array.Copy(localArray, 0, array, index, localArray.Length);
        }



        /// <summary>
        /// Clean-up all resources
        /// </summary>
        /// <param name="isUserCall">Was called by user</param>
        protected virtual void Dispose(bool isUserCall)
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
            }
        }

        /// <summary>
        /// Clean-up all resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

#pragma warning restore 0420


}
