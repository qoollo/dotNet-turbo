using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Queues
{
    /// <summary>
    /// Queue wrapper that performs item transformation from TExternal to TInner (base implementation)
    /// </summary>
    /// <typeparam name="TExternal">Items type that is visible by external code</typeparam>
    /// <typeparam name="TInner">Items type of the wrapped queue</typeparam>
    public abstract class TransformationQueueBase<TExternal, TInner> : Common.CommonQueueImpl<TExternal>
    {
        private readonly IQueue<TInner> _queue;
        private volatile bool _isDisposed;

        /// <summary>
        /// TransformationQueueBase constructor
        /// </summary>
        /// <param name="queue">Inner queue to be wrapped</param>
        public TransformationQueueBase(IQueue<TInner> queue)
        {
            if (queue == null)
                throw new ArgumentNullException(nameof(queue));

            _queue = queue;
            _isDisposed = false;
        }


        /// <summary>
        /// Direct access to the wrapped queue
        /// </summary>
        protected IQueue<TInner> InnerQueue { get { return _queue; } }

        /// <summary>
        /// Checks if queue is disposed
        /// </summary>
        private void CheckDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(TransformationQueueBase<TExternal, TInner>));
        }


        /// <summary>
        /// Converts item from TExternal to TInner before store inside wrapped queue
        /// </summary>
        /// <param name="item">Item to be converted</param>
        /// <returns>Converted item</returns>
        protected abstract TInner Convert(TExternal item);
        /// <summary>
        /// Converts item from TInner to TExternal after dequeuing item from inner queue
        /// </summary>
        /// <param name="item">Item to be converted</param>
        /// <returns>Converted item</returns>
        protected abstract TExternal ConvertBack(TInner item);


        /// <summary>
        /// Adds new item to the queue, even when the bounded capacity reached
        /// </summary>
        /// <param name="item">New item</param>
        public sealed override void AddForced(TExternal item)
        {
            CheckDisposed();

            var convertedItem = Convert(item);
            _queue.AddForced(convertedItem);
        }

        /// <summary>
        /// Adds new item to the tail of the queue (core method)
        /// </summary>
        /// <param name="item">New item</param>
        /// <param name="timeout">Adding timeout</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>Was added sucessufully</returns>
        protected sealed override bool TryAddCore(TExternal item, int timeout, CancellationToken token)
        {
            CheckDisposed();
            if (token.IsCancellationRequested)
                throw new OperationCanceledException(token);

            var convertedItem = Convert(item);
            return _queue.TryAdd(convertedItem, timeout, token);
        }

        /// <summary>
        /// Removes item from the head of the queue (core method)
        /// </summary>
        /// <param name="item">The item removed from queue</param>
        /// <param name="timeout">Removing timeout</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>True if the item was removed</returns>
        protected sealed override bool TryTakeCore(out TExternal item, int timeout, CancellationToken token)
        {
            CheckDisposed();
            if (token.IsCancellationRequested)
                throw new OperationCanceledException(token);

            TInner innerItem = default(TInner);
            if (_queue.TryTake(out innerItem, timeout, token))
            {
                item = ConvertBack(innerItem);
                return true;
            }

            item = default(TExternal);
            return false;
        }

        /// <summary>
        /// Returns the item at the head of the queue without removing it (core method)
        /// </summary>
        /// <param name="item">The item at the head of the queue</param>
        /// <param name="timeout">Peeking timeout</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>True if the item was read</returns>
        protected sealed override bool TryPeekCore(out TExternal item, int timeout, CancellationToken token)
        {
            CheckDisposed();
            if (token.IsCancellationRequested)
                throw new OperationCanceledException(token);

            TInner innerItem = default(TInner);
            if (_queue.TryPeek(out innerItem, timeout, token))
            {
                item = ConvertBack(innerItem);
                return true;
            }

            item = default(TExternal);
            return false;
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
                _queue.Dispose();
            }
        }
    }
}
