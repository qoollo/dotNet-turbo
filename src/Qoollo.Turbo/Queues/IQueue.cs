using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Queues
{
    /// <summary>
    /// Basic Queue interface
    /// </summary>
    /// <typeparam name="T">The type of elements in queue</typeparam>
    public interface IQueue<T>: IDisposable
    {
        /// <summary>
        /// Adds new item to the queue, even when the bounded capacity reached
        /// </summary>
        /// <param name="item">New item</param>
        void AddForced(T item);
        /// <summary>
        /// Attempts to add new item to the tail of the queue
        /// </summary>
        /// <param name="item">New item</param>
        /// <param name="timeout">Adding timeout</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>True if item was added, otherwise false</returns>
        bool TryAdd(T item, int timeout, CancellationToken token);
        /// <summary>
        /// Attempts to remove item from the head of the queue
        /// </summary>
        /// <param name="item">The item removed from queue</param>
        /// <param name="timeout">Removing timeout in milliseconds</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>True if the item was removed</returns>
        bool TryTake(out T item, int timeout, CancellationToken token);
        /// <summary>
        /// Attempts to read the item at the head of the queue without removing it
        /// </summary>
        /// <param name="item">The item at the head of the queue</param>
        /// <param name="timeout">Peeking timeout in milliseconds</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>True if the item was read</returns>
        bool TryPeek(out T item, int timeout, CancellationToken token);


        // For the future use
        //int TryAddPackage(T[] items, int start, int count, int timeout, CancellationToken token);
        //int TryTakePackage(T[] items, int start, int count, int timeout, CancellationToken token);
    }
}
