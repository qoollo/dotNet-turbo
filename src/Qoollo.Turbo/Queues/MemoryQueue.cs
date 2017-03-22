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
    }
}
