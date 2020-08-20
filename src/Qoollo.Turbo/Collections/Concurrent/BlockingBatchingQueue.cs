using Qoollo.Turbo.Threading;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Collections.Concurrent
{
    /// <summary>
    /// Batching queue with blocking and bounding capabilities. Items enqueued one-by-one, but dequeued in batches
    /// </summary>
    /// <typeparam name="T">The type of elements in collection</typeparam>
    [DebuggerDisplay("Count = {Count}")]
    public class BlockingBatchingQueue<T>: ICollection, IEnumerable<T>
    {
        private readonly ConcurrentBatchingQueue<T> _innerQueue;
        private readonly int _boundedCapacityInBatches;
        private readonly SemaphoreLight _freeNodes;
        private readonly SemaphoreLight _occupiedBatches;

        /// <summary>
        /// <see cref="BlockingBatchingQueue{T}"/> constructor
        /// </summary>
        /// <param name="batchSize">Size of the batch</param>
        /// <param name="boundedCapacityInBatches">Maximum number of batches in queue (if less or equal to 0 then no limitation)</param>
        public BlockingBatchingQueue(int batchSize, int boundedCapacityInBatches)
        {
            if (batchSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(batchSize), $"'{nameof(batchSize)}' should be positive");
            if (boundedCapacityInBatches > 0 && ((long)boundedCapacityInBatches * batchSize) > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(boundedCapacityInBatches), $"Max capacity value is depends on '{nameof(batchSize)}' value and equal to {int.MaxValue / batchSize}");

            _innerQueue = new ConcurrentBatchingQueue<T>(batchSize);
            _boundedCapacityInBatches = boundedCapacityInBatches >= 0 ? boundedCapacityInBatches : -1;

            if (boundedCapacityInBatches > 0)
                _freeNodes = new SemaphoreLight(boundedCapacityInBatches * batchSize);
            _occupiedBatches = new SemaphoreLight(0);
        }
        /// <summary>
        /// <see cref="BlockingBatchingQueue{T}"/> constructor
        /// </summary>
        /// <param name="batchSize">Size of the batch</param>
        public BlockingBatchingQueue(int batchSize)
            : this(batchSize, -1)
        {
        }

        /// <summary>
        /// Maximum number of batches in queue (if less or equal to 0 then no limitation)
        /// </summary>
        public int BoundedCapacityInBatches { get { return _boundedCapacityInBatches; } }
        /// <summary>
        /// Number of items inside the queue
        /// </summary>
        public int Count { get { return _innerQueue.Count; } }
        /// <summary>
        /// Number of completed batches inside the queue (these batches can be dequeued)
        /// </summary>
        public int CompletedBatchCount { get { return _innerQueue.CompletedBatchCount; } }


        /// <summary>
        /// Adds new item to the tail of the queue (inner core method)
        /// </summary>
        /// <param name="item">New item</param>
        /// <param name="timeout">Adding timeout</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>Was added sucessufully</returns>
        /// <exception cref="OperationCanceledException">Cancellation was requested by token</exception>
        private bool TryAddInner(T item, int timeout, CancellationToken token)
        {
            if (token.IsCancellationRequested)
                throw new OperationCanceledException(token);

            bool elementAdded = false;
            int batchCountIncreased = 0;
            bool entered = false;

            try
            {
                if (_freeNodes == null || _freeNodes.Wait(timeout, token))
                {
                    entered = true;
                    _innerQueue.Enqueue(item, out batchCountIncreased);
                    elementAdded = true;
                }
            }
            finally
            {
                if (!elementAdded && entered && _freeNodes != null)
                {
                    _freeNodes.Release();
                }
                if (elementAdded && batchCountIncreased > 0)
                    _occupiedBatches.Release(batchCountIncreased);
            }

            return elementAdded;
        }


        /// <summary>
        /// Adds the item to the tail of the queue
        /// </summary>
        /// <param name="item">New item</param>
        public void Add(T item)
        {
            bool addResult = TryAddInner(item, Timeout.Infinite, new CancellationToken());
            TurboContract.Assume(addResult, "addResult is false when timeout is Infinite");
        }
        /// <summary>
        /// Adds the item to the tail of the queue
        /// </summary>
        /// <param name="item">New item</param>
        /// <param name="token">Cancellation token</param>
        /// <exception cref="OperationCanceledException">Cancellation was requested by token</exception>
        public void Add(T item, CancellationToken token)
        {
            bool addResult = TryAddInner(item, Timeout.Infinite, token);
            TurboContract.Assume(addResult, "addResult is false when timeout is Infinite");
        }

        /// <summary>
        /// Attempts to add new item to tail of the the queue
        /// </summary>
        /// <param name="item">New item</param>
        /// <returns>True if item was added, otherwise false</returns>
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
        public bool TryAdd(T item, TimeSpan timeout)
        {
            long timeoutMs = (long)timeout.TotalMilliseconds;
            if (timeoutMs > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(timeout));
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
        public bool TryAdd(T item, int timeout, CancellationToken token)
        {
            if (timeout < 0)
                timeout = Timeout.Infinite;
            return TryAddInner(item, timeout, token);
        }




        /// <summary>
        /// Removes completed batch from the head of the queue (inner core method)
        /// </summary>
        /// <param name="batch">The batch removed from queue</param>
        /// <param name="timeout">Removing timeout</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>True if the batch was removed</returns>
        /// <exception cref="OperationCanceledException">Cancellation was requested by token</exception>
        private bool TryTakeInner(out T[] batch, int timeout, CancellationToken token)
        {
            if (token.IsCancellationRequested)
                throw new OperationCanceledException(token);

            batch = null;

            bool batchTaken = false;
            bool entered = false;

            try
            {
                if (_occupiedBatches.Wait(timeout, token))
                {
                    entered = true;

                    batchTaken = _innerQueue.TryDequeue(out batch);
                    Debug.Assert(batchTaken == true, "Collection is inconsistent");
                }
            }
            finally
            {
                if (!batchTaken && entered)
                {
                    _occupiedBatches.Release();
                }
                if (batchTaken && _freeNodes != null)
                    _freeNodes.Release(batch.Length);
            }

            return batchTaken;
        }


        /// <summary>
        /// Removes completed batch from the head of the queue
        /// </summary>
        /// <returns>The batch removed from queue</returns>
        public T[] Take()
        {
            bool takeResult = TryTakeInner(out T[] result, Timeout.Infinite, new CancellationToken());
            TurboContract.Assume(takeResult, "takeResult is false when timeout is Infinite");

            return result;
        }
        /// <summary>
        /// Removes completed batch from the head of the queue
        /// </summary>
        /// <param name="token">Cancellation token</param>
        /// <returns>The batch removed from queue</returns>
        /// <exception cref="OperationCanceledException">Cancellation was requested by token</exception>
        public T[] Take(CancellationToken token)
        {
            bool takeResult = TryTakeInner(out T[] result, Timeout.Infinite, token);
            TurboContract.Assume(takeResult, "takeResult is false when timeout is Infinite");

            return result;
        }

        /// <summary>
        /// Attempts to remove completed batch from the head of the queue
        /// </summary>
        /// <param name="batch">The batch removed from queue</param>
        /// <returns>True if the batch was removed</returns>
        public bool TryTake(out T[] batch)
        {
            return TryTakeInner(out batch, 0, CancellationToken.None);
        }
        /// <summary>
        /// Attempts to remove completed batch from the head of the queue
        /// </summary>
        /// <param name="batch">The batch removed from queue</param>
        /// <param name="timeout">Removing timeout</param>
        /// <returns>True if the batch was removed</returns>
        public bool TryTake(out T[] batch, TimeSpan timeout)
        {
            long timeoutMs = (long)timeout.TotalMilliseconds;
            if (timeoutMs > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(timeout));
            if (timeoutMs < 0)
                timeoutMs = Timeout.Infinite;

            return TryTakeInner(out batch, (int)timeoutMs, new CancellationToken());
        }
        /// <summary>
        /// Attempts to remove completed batch from the head of the queue
        /// </summary>
        /// <param name="batch">The batch removed from queue</param>
        /// <param name="timeout">Removing timeout in milliseconds</param>
        /// <returns>True if the batch was removed</returns>
        public bool TryTake(out T[] batch, int timeout)
        {
            if (timeout < 0)
                timeout = Timeout.Infinite;
            return TryTakeInner(out batch, timeout, new CancellationToken());
        }
        /// <summary>
        /// Attempts to remove completed batch from the head of the queue
        /// </summary>
        /// <param name="batch">The batch removed from queue</param>
        /// <param name="timeout">Removing timeout in milliseconds</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>True if the item was removed</returns>
        /// <exception cref="OperationCanceledException">Cancellation was requested by token</exception>
        public bool TryTake(out T[] batch, int timeout, CancellationToken token)
        {
            if (timeout < 0)
                timeout = Timeout.Infinite;
            return TryTakeInner(out batch, timeout, token);
        }


        /// <summary>
        /// Mark active batch as completed so that it can be removed from the queue even if it is not full
        /// </summary>
        /// <returns>True when active batch is not empty, otherwise false</returns>
        public bool CompleteCurrentBatch()
        {
            bool bucketCountIncreased = false;

            try
            {
                bucketCountIncreased = _innerQueue.CompleteCurrentBatch();
            }
            finally
            {
                if (bucketCountIncreased)
                    _occupiedBatches.Release();
            }

            return bucketCountIncreased;
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
            TurboContract.Assert(array != null, conditionString: "array != null");
            TurboContract.Assert(index >= 0 && index < array.Length, conditionString: "index >= 0 && index < array.Length");

            _innerQueue.CopyTo(array, index);
        }


        /// <summary>
        /// Returns Enumerator
        /// </summary>
        /// <returns>Enumerator</returns>
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
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
            ((ICollection)_innerQueue).CopyTo(array, index);
        }
    }
}
