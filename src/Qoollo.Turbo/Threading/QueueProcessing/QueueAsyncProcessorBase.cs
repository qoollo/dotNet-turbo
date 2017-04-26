using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading;

namespace Qoollo.Turbo.Threading.QueueProcessing
{
    /// <summary>
    /// Asynchronous items processor with queue (base methods)
    /// </summary>
    /// <typeparam name="T">Type of the elements processed by this <see cref="QueueAsyncProcessorBase{T}"/></typeparam>
    public abstract class QueueAsyncProcessorBase<T>: IConsumer<T>, IDisposable
    {
        /// <summary>
        /// Attempts to add new item to processing queue
        /// </summary>
        /// <param name="element">New item</param>
        /// <param name="timeout">Adding timeout in milliseconds</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>True if item was added, otherwise false</returns>
        public abstract bool Add(T element, int timeout, CancellationToken token);
        /// <summary>
        /// Adds new item to processing queue (waits for space in processing queue)
        /// </summary>
        /// <param name="element">New item</param>
        public void Add(T element)
        {
            bool result = Add(element, Timeout.Infinite, new CancellationToken());
            Debug.Assert(result);
        }
        /// <summary>
        /// Adds new item to processing queue
        /// </summary>
        /// <param name="element">New item</param>
        /// <param name="token">Cancellation token</param>
        public void Add(T element, CancellationToken token)
        {
            bool result = Add(element, Timeout.Infinite, token);
            Debug.Assert(result);
        }
        /// <summary>
        /// Attempts to add new item to processing queue
        /// </summary>
        /// <param name="element">New item</param>
        /// <param name="timeout">Adding timeout in milliseconds</param>
        /// <returns>True if item was added, otherwise false</returns>
        public bool Add(T element, int timeout)
        {
            return Add(element, timeout, new CancellationToken());
        }
        /// <summary>
        /// Attempts to add new item to processing queue
        /// </summary>
        /// <param name="element">New item</param>
        /// <returns>True if item was added, otherwise false</returns>
        public bool TryAdd(T element)
        {
            return Add(element, 0, new CancellationToken());
        }



        /// <summary>
        /// Pushes new element to the consumer
        /// </summary>
        /// <param name="item">Element</param>
        void IConsumer<T>.Add(T item)
        {
            this.Add(item);
        }
        /// <summary>
        /// Attempts to push new element to the consumer
        /// </summary>
        /// <param name="item">Element</param>
        /// <returns>True if the element was consumed successfully</returns>
        bool IConsumer<T>.TryAdd(T item)
        {
            return this.TryAdd(item);
        }

        /// <summary>
        /// Cleans-up resources
        /// </summary>
        /// <param name="isUserCall">Is it called explicitly by user (False - from finalizer)</param>
        protected virtual void Dispose(bool isUserCall)
        { 
        }

        /// <summary>
        /// Cleans-up resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
