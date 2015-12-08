using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Collections
{
    /// <summary>
    /// Priority marker for HighLowPriorityQueue
    /// </summary>
    public enum HighLowPriority
    {
        /// <summary>
        /// High priority level
        /// </summary>
        High = 0,
        /// <summary>
        /// Low priority level (normal priority)
        /// </summary>
        Low = 1
    }

    /// <summary>
    /// Priority queue with high-low priority levels
    /// </summary>
    /// <typeparam name="T">Specifies the type of elements in the queue</typeparam>
    public class HighLowPriorityQueue<T> : LimitedPriorityQueueBase<T, HighLowPriority>
    {
        private const int PriorityLevels = 2;

        /// <summary>
        /// HighLowPriorityQueue constructor
        /// </summary>
        public HighLowPriorityQueue()
            : base(PriorityLevels)
        {
        }

        /// <summary>
        /// HighLowPriorityQueue constructor
        /// </summary>
        /// <param name="collection">The collection whose elements are copied to the new priority queue on the Low priority level</param>
        public HighLowPriorityQueue(IEnumerable<T> collection)
            : base(collection, PriorityLevels)
        {

        }

        /// <summary>
        /// Convert priority marker to the according integral number of that priority
        /// </summary>
        /// <param name="prior">Priority marker</param>
        /// <returns>Integral priority level</returns>
        protected override int MapPriority(HighLowPriority prior)
        {
            return (int)prior;
        }

        /// <summary>
        /// Adds an object to the priority queue with Low priority level
        /// </summary>
        /// <param name="item">The item to add to the queue</param>
        public void Enqueue(T item)
        {
            base.Enqueue(item, HighLowPriority.Low);
        }

        /// <summary>
        /// Adds an object to the priority queue with High priority level
        /// </summary>
        /// <param name="item">The item to add to the queue</param>
        public void EnqueueHigh(T item)
        {
            base.Enqueue(item, HighLowPriority.High);
        }
    }
}
