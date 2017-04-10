using Qoollo.Turbo.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Queues.DiskQueueComponents
{
    /// <summary>
    /// Flag indicating whether the segment can be removed from disk
    /// </summary>
    public enum DiskQueueSegmentDisposeBehaviour
    {
        /// <summary>
        /// Do not delete segment from disk
        /// </summary>
        None,
        /// <summary>
        /// Delete segment from disk
        /// </summary>
        Delete
    }
    

    /// <summary>
    /// Base class for the segment of the DiskQueue
    /// </summary>
    /// <typeparam name="T">The type of elements in segment</typeparam>
    public abstract class DiskQueueSegment<T>: IDisposable
    {
        private readonly long _number;

        /// <summary>
        /// DiskQueueSegment constructor
        /// </summary>
        /// <param name="number">Segment number</param>
        public DiskQueueSegment(long number)
        {
            if (number < 0)
                throw new ArgumentOutOfRangeException(nameof(number));

            _number = number;
        }

        /// <summary>
        /// Unique number of the segment that represents its position in the queue
        /// </summary>
        public long Number { get { return _number; } }

        /// <summary>
        /// Number of items inside segment
        /// </summary>
        public abstract int Count { get; }
        /// <summary>
        /// Indicates whether the segment is full (no more items can be added to it excpetion items added by <see cref="AddForced(T)"/>)
        /// </summary>
        public abstract bool IsFull { get; }
        /// <summary>
        /// Indicates whether the segment is completed and can be safely removed (should be equivalent to <see cref="Count"/> == 0 &amp;&amp; <see cref="IsFull"/>)
        /// </summary>
        public virtual bool IsCompleted { get { return IsFull && Count == 0; } }


        /// <summary>
        /// Adds new item to the segment, even when the segement is full
        /// </summary>
        /// <param name="item">New item</param>
        public abstract void AddForced(T item);
        /// <summary>
        /// Adds new item to the tail of the segment (should return false when <see cref="IsFull"/> setted)
        /// </summary>
        /// <param name="item">New item</param>
        /// <returns>Was added sucessufully</returns>
        public abstract bool TryAdd(T item);
        /// <summary>
        /// Removes item from the head of the segment
        /// </summary>
        /// <param name="item">The item removed from segment</param>
        /// <returns>True if the item was removed</returns>
        public abstract bool TryTake(out T item);
        /// <summary>
        /// Returns the item at the head of the segment without removing it
        /// </summary>
        /// <param name="item">The item at the head of the segment</param>
        /// <returns>True if the item was read</returns>
        public abstract bool TryPeek(out T item);



        /// <summary>
        /// Cleans-up resources
        /// </summary>
        /// <param name="disposeBehaviour">Flag indicating whether the segment can be removed from disk</param>
        /// <param name="isUserCall">Was called explicitly by user</param>
        protected abstract void Dispose(DiskQueueSegmentDisposeBehaviour disposeBehaviour, bool isUserCall);

        /// <summary>
        /// Cleans-up resources
        /// </summary>
        /// <param name="disposeBehaviour">Flag indicating whether the segment can be removed from disk</param>
        public void Dispose(DiskQueueSegmentDisposeBehaviour disposeBehaviour)
        {
            Dispose(disposeBehaviour, true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Cleans-up resources
        /// </summary>
        public void Dispose()
        {
            Dispose(DiskQueueSegmentDisposeBehaviour.None, true);
            GC.SuppressFinalize(this);
        }
    }
}
