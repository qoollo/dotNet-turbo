using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Queues.DiskQueueComponents
{
    /// <summary>
    /// Factory to instantiate <see cref="MemoryDiskQueueSegment{T}"/>
    /// </summary>
    /// <typeparam name="T">The type of elements in segment</typeparam>
    public class MemoryDiskQueueSegmentFactory<T> : DiskQueueSegmentFactory<T>
    {
        private readonly int _segmentCapacity;

        /// <summary>
        /// MemoryDiskQueueSegmentFactory constructor
        /// </summary>
        /// <param name="segmentCapacity">Capacity of every created <see cref="MemoryDiskQueueSegment{T}"/></param>
        public MemoryDiskQueueSegmentFactory(int segmentCapacity)
        {
            if (segmentCapacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(segmentCapacity));

            _segmentCapacity = segmentCapacity;
        }

        /// <summary>
        /// Capacity of a single segment
        /// </summary>
        public override int SegmentCapacity { get { return _segmentCapacity; } }
        /// <summary>
        /// Creates a new segment
        /// </summary>
        /// <param name="path">Path to the folder where the new segment will be allocated</param>
        /// <param name="number">Number of a segment (should be part of a segment name)</param>
        /// <returns>Created segment</returns>
        public override DiskQueueSegment<T> CreateSegment(string path, long number)
        {
            return new MemoryDiskQueueSegment<T>(_segmentCapacity, number);
        }
        /// <summary>
        /// Discovers existing segments in specified path (always empty for MemoryDiskQueueSegmentFactory)
        /// </summary>
        /// <param name="path">Path to the folder for the segments</param>
        /// <returns>Segments loaded from disk (can be empty)</returns>
        public override DiskQueueSegment<T>[] DiscoverSegments(string path)
        {
            return new DiskQueueSegment<T>[0];
        }
    }


    /// <summary>
    /// DiskQueueSegment that stores items in memory (for test purposes)
    /// </summary>
    /// <typeparam name="T">The type of elements in segment</typeparam>
    public class MemoryDiskQueueSegment<T> : CountingDiskQueueSegment<T>
    {
        /// <summary>
        /// Creates new instance of <see cref="MemoryDiskQueueSegmentFactory{T}"/>
        /// </summary>
        /// <param name="segmentCapacity">Capacity of the segments created by factory</param>
        /// <returns>Create <see cref="MemoryDiskQueueSegmentFactory{T}"/></returns>
        public static MemoryDiskQueueSegmentFactory<T> CreateFactory(int segmentCapacity)
        {
            return new MemoryDiskQueueSegmentFactory<T>(segmentCapacity);
        }

        // =========================

        private readonly ConcurrentQueue<T> _queue;
        private volatile bool _isDisposed;

        /// <summary>
        /// MemoryDiskQueueSegment constructor
        /// </summary>
        /// <param name="capacity">Capacity</param>
        /// <param name="segmentNumber">Segment number</param>
        public MemoryDiskQueueSegment(int capacity, long segmentNumber)
            : base(capacity, 0, 0, segmentNumber)
        {
            _queue = new ConcurrentQueue<T>();
            _isDisposed = false;
        }

        /// <summary>
        /// Adds new item to the tail of the segment (core implementation)
        /// </summary>
        /// <param name="item">New item</param>
        protected override void AddCore(T item)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(this.GetType().Name);

            _queue.Enqueue(item);
        }
        /// <summary>
        /// Removes item from the head of the segment (core implementation)
        /// </summary>
        /// <param name="item">The item removed from segment</param>
        /// <returns>True if the item was removed</returns>
        protected override bool TryTakeCore(out T item)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(this.GetType().Name);

            return _queue.TryDequeue(out item);
        }
        /// <summary>
        /// Returns the item at the head of the segment without removing it (core implementation)
        /// </summary>
        /// <param name="item">The item at the head of the segment</param>
        /// <returns>True if the item was read</returns>
        protected override bool TryPeekCore(out T item)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(this.GetType().Name);

            return _queue.TryPeek(out item);
        }

        /// <summary>
        /// Cleans-up resources
        /// </summary>
        /// <param name="disposeBehaviour">Flag indicating whether the segment can be removed from disk</param>
        /// <param name="isUserCall">Was called explicitly by user</param>
        protected override void Dispose(DiskQueueSegmentDisposeBehaviour disposeBehaviour, bool isUserCall)
        {
            _isDisposed = true;
        }
    }
}
