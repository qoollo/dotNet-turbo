using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Threading.QueueProcessing
{
    /// <summary>
    /// Interface for asynchronous items processor with queue
    /// </summary>
    /// <typeparam name="T">Type of the elements processed by this <see cref="IQueueAsyncProcessor{T}"/></typeparam>
    internal interface IQueueAsyncProcessor<T>: IDisposable
    {
        /// <summary>
        /// Attempts to add new item to processing queue
        /// </summary>
        /// <param name="element">New item</param>
        /// <param name="timeout">Adding timeout in milliseconds</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>True if item was added, otherwise false</returns>
        bool Add(T element, int timeout, CancellationToken token);
        /// <summary>
        /// Adds new item to processing queue (waits for space in processing queue)
        /// </summary>
        /// <param name="element">New item</param>
        void Add(T element);
        /// <summary>
        /// Adds new item to processing queue
        /// </summary>
        /// <param name="element">New item</param>
        /// <param name="token">Cancellation token</param>
        void Add(T element, CancellationToken token);
        /// <summary>
        /// Attempts to add new item to processing queue
        /// </summary>
        /// <param name="element">New item</param>
        /// <param name="timeout">Adding timeout in milliseconds</param>
        /// <returns>True if item was added, otherwise false</returns>
        bool Add(T element, int timeout);
        /// <summary>
        /// Attempts to add new item to processing queue
        /// </summary>
        /// <param name="element">New item</param>
        /// <returns>True if item was added, otherwise false</returns>
        bool TryAdd(T element);
    }
}
