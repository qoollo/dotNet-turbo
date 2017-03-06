using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly IQueue<T> _highLevelQueue;
        private readonly IQueue<T> _lowLevelQueue;

        private readonly LevelingQueueAddingMode _addingMode;

        private volatile bool _isDisposed;

        public LevelingQueue(IQueue<T> highLevelQueue, IQueue<T> lowLevelQueue, LevelingQueueAddingMode addingMode)
        {
            if (highLevelQueue == null)
                throw new ArgumentNullException(nameof(highLevelQueue));
            if (lowLevelQueue == null)
                throw new ArgumentNullException(nameof(lowLevelQueue));

            _highLevelQueue = highLevelQueue;
            _lowLevelQueue = lowLevelQueue;

            _addingMode = addingMode;

            _isDisposed = false;
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
        public sealed override bool IsEmpty { get { return _highLevelQueue.IsEmpty && _lowLevelQueue.IsEmpty; } }


        /// <summary>
        /// Checks if queue is disposed
        /// </summary>
        private void CheckDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(LevelingQueue<T>));
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
                if (_highLevelQueue.TryAdd(item, 0, default(CancellationToken)))
                    return;

                _lowLevelQueue.AddForced(item);
            }
            else
            {
                if (_lowLevelQueue.IsEmpty && _highLevelQueue.TryAdd(item, 0, default(CancellationToken)))
                    return;

                _lowLevelQueue.AddForced(item);
            }
        }

        /// <summary>
        /// Adds new item to the high level queue, even when the bounded capacity reached
        /// </summary>
        /// <param name="item">New item</param>
        public void AddForcedToHighLevelQueue(T item)
        {
            CheckDisposed();
            _highLevelQueue.AddForced(item);
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
            CheckDisposed();

            if (token.IsCancellationRequested)
                throw new OperationCanceledException(token);

            if (_addingMode == LevelingQueueAddingMode.PreferLiveData)
            {
                if (_highLevelQueue.TryAdd(item, 0, default(CancellationToken)))
                    return true;

                return _lowLevelQueue.TryAdd(item, timeout, token);
            }
            else
            {
                if (_lowLevelQueue.IsEmpty && _highLevelQueue.TryAdd(item, 0, default(CancellationToken)))
                    return true;

                return _lowLevelQueue.TryAdd(item, timeout, token);
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
            CheckDisposed();

            throw new NotImplementedException();
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



        /// <summary>
        /// Clean-up all resources
        /// </summary>
        /// <param name="isUserCall">Was called by user</param>
        protected override void Dispose(bool isUserCall)
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                _lowLevelQueue.Dispose();
                _highLevelQueue.Dispose();
            }
        }
    }
}
