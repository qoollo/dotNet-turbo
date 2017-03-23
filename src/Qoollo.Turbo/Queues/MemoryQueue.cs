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
    [DebuggerDisplay("Count = {Count}")]
    public class MemoryQueue<T>: Collections.Concurrent.BlockingQueue<T>, IQueue<T>
    {
        /// <summary>
        /// MemoryQueue constructor
        /// </summary>
        /// <param name="boundedCapacity">The bounded size of the queue (if less or equeal to 0 then no limitation)</param>
        public MemoryQueue(int boundedCapacity) : base(boundedCapacity) { }
        /// <summary>
        /// MemoryQueue constructor
        /// </summary>
        public MemoryQueue() { }

        /// <summary>
        /// The bounded size of the queue (-1 means not bounded)
        /// </summary>
        long IQueue<T>.BoundedCapacity { get { return base.BoundedCapacity; } }
        /// <summary>
        /// Number of items inside the queue
        /// </summary>
        long IQueue<T>.Count { get { return base.Count; } }
        /// <summary>
        /// Indicates whether the queue is empty
        /// </summary>
        public bool IsEmpty { get { return base.Count == 0; } }

        /// <summary>
        /// Attempts to add new item to the tail of the queue
        /// </summary>
        /// <param name="item">New item</param>
        /// <param name="timeout">Adding timeout</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>True if item was added, otherwise false</returns>
        bool IQueue<T>.TryAdd(T item, int timeout, CancellationToken token)
        {
            if (timeout == 0 && !token.CanBeCanceled)
                return this.TryAddFast(item); // Speed-up hack

            return this.TryAdd(item, timeout, token);
        }
        /// <summary>
        /// Attempts to remove item from the head of the queue
        /// </summary>
        /// <param name="item">The item removed from queue</param>
        /// <param name="timeout">Removing timeout in milliseconds</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>True if the item was removed</returns>
        bool IQueue<T>.TryTake(out T item, int timeout, CancellationToken token)
        {
            if (timeout == 0 && !token.CanBeCanceled)
                return TryTakeFast(out item);   // Speed-up hack

            return TryTake(out item, timeout, token);
        }
    }
}
