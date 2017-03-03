using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Queues.Common
{
    /// <summary>
    /// Common overloads implementation for queue
    /// </summary>
    /// <typeparam name="T">Type of items stored inside queue</typeparam>
    public abstract class CommonQueueImpl<T> : IQueue<T>
    {
        /// <summary>
        /// The bounded size of the queue (-1 means not bounded)
        /// </summary>
        public abstract long BoundedCapacity { get; }
        /// <summary>
        /// Number of items inside the queue
        /// </summary>
        public abstract long Count { get; }

        /// <summary>
        /// Adds new item to the queue, even when the bounded capacity reached
        /// </summary>
        /// <param name="item">New item</param>
        public abstract void AddForced(T item);

        /// <summary>
        /// Adds new item to the tail of the queue (core method)
        /// </summary>
        /// <param name="item">New item</param>
        /// <param name="timeout">Adding timeout</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>Was added sucessufully</returns>
        protected abstract bool TryAddCore(T item, int timeout, CancellationToken token);

        /// <summary>
        /// Removes item from the head of the queue (core method)
        /// </summary>
        /// <param name="item">The item removed from queue</param>
        /// <param name="timeout">Removing timeout</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>True if the item was removed</returns>
        protected abstract bool TryTakeCore(out T item, int timeout, CancellationToken token);

        /// <summary>
        /// Returns the item at the head of the queue without removing it (core method)
        /// </summary>
        /// <param name="item">The item at the head of the queue</param>
        /// <param name="timeout">Peeking timeout</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>True if the item was read</returns>
        protected abstract bool TryPeekCore(out T item, int timeout, CancellationToken token);


        #region Add overloads

        /// <summary>
        /// Adds the item to the tail of the queue
        /// </summary>
        /// <param name="item">New item</param>
        public void Add(T item)
        {
            bool addResult = TryAddCore(item, Timeout.Infinite, new CancellationToken());
            Debug.Assert(addResult, "addResult != true");
        }
        /// <summary>
        /// Adds the item to the tail of the queue
        /// </summary>
        /// <param name="item">New item</param>
        /// <param name="token">Cancellation token</param>
        /// <exception cref="OperationCanceledException">Cancellation was requested by token</exception>
        public void Add(T item, CancellationToken token)
        {
            bool addResult = TryAddCore(item, Timeout.Infinite, token);
            Debug.Assert(addResult, "addResult != true");
        }

        /// <summary>
        /// Attempts to add new item to tail of the the queue
        /// </summary>
        /// <param name="item">New item</param>
        /// <returns>True if item was added, otherwise false</returns>
        public bool TryAdd(T item)
        {
            return TryAddCore(item, 0, new CancellationToken());
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

            return TryAddCore(item, (int)timeoutMs, new CancellationToken());
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
            return TryAddCore(item, timeout, new CancellationToken());
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
            return TryAddCore(item, timeout, token);
        }

        #endregion

        #region Take overloads

        /// <summary>
        /// Removes item from the head of the queue
        /// </summary>
        /// <returns>The item removed from queue</returns>
        public T Take()
        {
            T result;
            bool takeResult = TryTakeCore(out result, Timeout.Infinite, new CancellationToken());
            Debug.Assert(takeResult, "takeResult != true");

            return result;
        }
        /// <summary>
        /// Removes item from the head of the queue
        /// </summary>
        /// <param name="token">Cancellation token</param>
        /// <returns>The item removed from queue</returns>
        /// <exception cref="OperationCanceledException">Cancellation was requested by token</exception>
        public T Take(CancellationToken token)
        {
            T result;
            bool takeResult = TryTakeCore(out result, Timeout.Infinite, token);
            Debug.Assert(takeResult, "takeResult != true");

            return result;
        }

        /// <summary>
        /// Attempts to remove item from the head of the queue
        /// </summary>
        /// <param name="item">The item removed from queue</param>
        /// <returns>True if the item was removed</returns>
        public bool TryTake(out T item)
        {
            return TryTakeCore(out item, 0, CancellationToken.None);
        }
        /// <summary>
        /// Attempts to remove item from the head of the queue
        /// </summary>
        /// <param name="item">The item removed from queue</param>
        /// <param name="timeout">Removing timeout</param>
        /// <returns>True if the item was removed</returns>
        public bool TryTake(out T item, TimeSpan timeout)
        {
            long timeoutMs = (long)timeout.TotalMilliseconds;
            if (timeoutMs > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(timeout));
            if (timeoutMs < 0)
                timeoutMs = Timeout.Infinite;

            return TryTakeCore(out item, (int)timeoutMs, new CancellationToken());
        }
        /// <summary>
        /// Attempts to remove item from the head of the queue
        /// </summary>
        /// <param name="item">The item removed from queue</param>
        /// <param name="timeout">Removing timeout in milliseconds</param>
        /// <returns>True if the item was removed</returns>
        public bool TryTake(out T item, int timeout)
        {
            if (timeout < 0)
                timeout = Timeout.Infinite;
            return TryTakeCore(out item, timeout, new CancellationToken());
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
            return TryTakeCore(out item, timeout, token);
        }

        #endregion

        #region Peek overloads


        /// <summary>
        /// Returns the item at the head of the queue without removing it (blocks the Thread when queue is empty)
        /// </summary>
        /// <returns>The item at the head of the queue</returns>
        public T Peek()
        {
            T result;
            bool takeResult = TryPeekCore(out result, Timeout.Infinite, new CancellationToken());
            Debug.Assert(takeResult, "takeResult != true");

            return result;
        }
        /// <summary>
        /// Returns the item at the head of the queue without removing it (blocks the Thread when queue is empty)
        /// </summary>
        /// <param name="token">Cancellation token</param>
        /// <returns>The item at the head of the queue</returns>
        /// <exception cref="OperationCanceledException">Cancellation was requested by token</exception>
        public T Peek(CancellationToken token)
        {
            T result;
            bool takeResult = TryPeekCore(out result, Timeout.Infinite, token);
            Debug.Assert(takeResult, "takeResult != true");

            return result;
        }

        /// <summary>
        /// Attempts to read the item at the head of the queue without removing it
        /// </summary>
        /// <param name="item">The item at the head of the queue</param>
        /// <returns>True if the item was read</returns>
        public bool TryPeek(out T item)
        {
            return TryPeekCore(out item, 0, new CancellationToken());
        }
        /// <summary>
        /// Attempts to read the item at the head of the queue without removing it
        /// </summary>
        /// <param name="item">The item at the head of the queue</param>
        /// <param name="timeout">Peeking timeout</param>
        /// <returns>True if the item was read</returns>
        public bool TryPeek(out T item, TimeSpan timeout)
        {
            long timeoutMs = (long)timeout.TotalMilliseconds;
            if (timeoutMs > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(timeout));
            if (timeoutMs < 0)
                timeoutMs = Timeout.Infinite;

            return TryPeekCore(out item, (int)timeoutMs, new CancellationToken());
        }
        /// <summary>
        /// Attempts to read the item at the head of the queue without removing it
        /// </summary>
        /// <param name="item">The item at the head of the queue</param>
        /// <param name="timeout">Peeking timeout in milliseconds</param>
        /// <returns>True if the item was read</returns>
        public bool TryPeek(out T item, int timeout)
        {
            if (timeout < 0)
                timeout = Timeout.Infinite;

            return TryPeekCore(out item, timeout, new CancellationToken());
        }
        /// <summary>
        /// Attempts to read the item at the head of the queue without removing it
        /// </summary>
        /// <param name="item">The item at the head of the queue</param>
        /// <param name="timeout">Peeking timeout in milliseconds</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>True if the item was read</returns>
        /// <exception cref="OperationCanceledException">Cancellation was requested by token</exception>
        public bool TryPeek(out T item, int timeout, CancellationToken token)
        {
            if (timeout < 0)
                timeout = Timeout.Infinite;

            return TryPeekCore(out item, timeout, token);
        }

        #endregion


        /// <summary>
        /// Clean-up all resources
        /// </summary>
        /// <param name="isUserCall">Was called by user</param>
        protected abstract void Dispose(bool isUserCall);

        /// <summary>
        /// Clean-up all resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
