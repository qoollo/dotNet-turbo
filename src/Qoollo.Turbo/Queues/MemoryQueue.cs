using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Queues
{
    /// <summary>
    /// Queue that stores items in memory
    /// </summary>
    /// <typeparam name="T">The type of elements in queue</typeparam>
    [System.Diagnostics.DebuggerDisplay("Count = {Count}")]
    public class MemoryQueue<T>: Common.CommonQueueImpl<T>
    {
        private readonly ConcurrentQueue<T> _innerQueue;
        private readonly int _boundedCapacity;
        private volatile int _delayedBoundedCapacityDecrease;

        private readonly SemaphoreSlim _freeNodes;
        private readonly SemaphoreSlim _occupiedNodes;

        private volatile bool _isDisposed;

        /// <summary>
        /// MemoryQueue constructor
        /// </summary>
        /// <param name="boundedCapacity">The bounded size of the queue (if less or equeal to 0 then no limitation)</param>
        public MemoryQueue(int boundedCapacity)
        {
            _innerQueue = new ConcurrentQueue<T>();
            _boundedCapacity = boundedCapacity >= 0 ? boundedCapacity : -1;
            _delayedBoundedCapacityDecrease = 0;
            _isDisposed = false;

            if (boundedCapacity > 0)
                _freeNodes = new SemaphoreSlim(boundedCapacity);

            _occupiedNodes = new SemaphoreSlim(0);
        }
        /// <summary>
        /// MemoryQueue constructor
        /// </summary>
        public MemoryQueue()
            : this(-1)
        {
        }


        /// <summary>
        /// The bounded size of the queue (-1 means not bounded)
        /// </summary>
        public sealed override long BoundedCapacity { get { return _boundedCapacity; } }
        /// <summary>
        /// Number of items inside the queue
        /// </summary>
        public sealed override long Count { get { return _occupiedNodes.CurrentCount; } }
        /// <summary>
        /// Indicates whether the queue is empty
        /// </summary>
        public sealed override bool IsEmpty { get { return _occupiedNodes.CurrentCount == 0; } }

        /// <summary>
        /// Wait handle that notifies about items presence
        /// </summary>
        protected sealed override WaitHandle HasItemsWaitHandle { get { return _occupiedNodes.AvailableWaitHandle; } }
        /// <summary>
        /// Wait handle that notifies about space availability for new items
        /// </summary>
        protected sealed override WaitHandle HasSpaceWaitHandle { get { return _freeNodes != null ? _freeNodes.AvailableWaitHandle : AlwaysSettedWaitHandle; } }


        /// <summary>
        /// Checks if queue is disposed
        /// </summary>
        private void CheckDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(this.GetType().Name);
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
        /// Adds new item to the queue, even when the bounded capacity reached
        /// </summary>
        /// <param name="item">New item</param>
        public sealed override void AddForced(T item)
        {
            CheckDisposed();

            try { }
            finally
            {
                _innerQueue.Enqueue(item);
                if (_freeNodes != null)
                    UpdateDelayedBoundedCapacityDecreaseField(1);
                _occupiedNodes.Release();
            }
        }

        /// <summary>
        /// Adds new item to the tail of the queue (core method)
        /// </summary>
        /// <param name="item">New item</param>
        /// <param name="timeout">Adding timeout</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>Was added sucessufully</returns>
        protected sealed override bool TryAddCore(T item, int timeout, CancellationToken token)
        {
            CheckDisposed();

            if (token.IsCancellationRequested)
                throw new OperationCanceledException(token);

            bool waitForSemaphoreWasSuccessful = true;

            if (_freeNodes != null)
            {
                waitForSemaphoreWasSuccessful = _freeNodes.Wait(0);
                if (waitForSemaphoreWasSuccessful == false && timeout != 0)
                    waitForSemaphoreWasSuccessful = _freeNodes.Wait(timeout, token);
            }

            if (!waitForSemaphoreWasSuccessful)
                return false;


            bool elementWasTaken = false;
            try
            {
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
            }

            return true;
        }

        /// <summary>
        /// Removes item from the head of the queue (core method)
        /// </summary>
        /// <param name="item">The item removed from queue</param>
        /// <param name="timeout">Removing timeout</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>True if the item was removed</returns>
        protected sealed override bool TryTakeCore(out T item, int timeout, CancellationToken token)
        {
            CheckDisposed();
            item = default(T);

            if (token.IsCancellationRequested)
                throw new OperationCanceledException(token);

            bool waitForSemaphoreWasSuccessful = _occupiedNodes.Wait(0);
            if (waitForSemaphoreWasSuccessful == false && timeout != 0)
                waitForSemaphoreWasSuccessful = _occupiedNodes.Wait(timeout, token);

            if (!waitForSemaphoreWasSuccessful)
                return false;

            bool removeSucceeded = false;
            bool removeFaulted = true;
            try
            {
                token.ThrowIfCancellationRequested();
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
            }

            return removeSucceeded;
        }

        /// <summary>
        /// Returns the item at the head of the queue without removing it (core method)
        /// </summary>
        /// <param name="item">The item at the head of the queue</param>
        /// <param name="timeout">Peeking timeout</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>True if the item was read</returns>
        protected sealed override bool TryPeekCore(out T item, int timeout, CancellationToken token)
        {
            CheckDisposed();
            item = default(T);

            if (token.IsCancellationRequested)
                throw new OperationCanceledException(token);

            if (_innerQueue.TryPeek(out item))
                return true;


            bool waitForSemaphoreWasSuccessful = _occupiedNodes.Wait(0);
            if (waitForSemaphoreWasSuccessful == false && timeout != 0)
                waitForSemaphoreWasSuccessful = _occupiedNodes.Wait(timeout, token);

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
        /// Clean-up all resources
        /// </summary>
        /// <param name="isUserCall">Was called by user</param>
        protected override void Dispose(bool isUserCall)
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
            }
        }
    }
}
