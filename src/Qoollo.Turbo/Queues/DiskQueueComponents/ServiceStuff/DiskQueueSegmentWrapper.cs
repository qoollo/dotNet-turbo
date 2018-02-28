using Qoollo.Turbo.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Queues.DiskQueueComponents
{
    internal static class DiskQueueSegmentWrapperFactoryExtensions
    {
        /// <summary>
        /// Creates new segment
        /// </summary>
        public static DiskQueueSegmentWrapper<T> CreateSegmentWrapped<T>(this DiskQueueSegmentFactory<T> factory, string path, long number)
        {
            TurboContract.Requires(factory != null, conditionString: "factory != null");

            var segment = factory.CreateSegment(path, number);
            return new DiskQueueSegmentWrapper<T>(segment);
        }
        /// <summary>
        /// Discovers existed segments
        /// </summary>
        public static DiskQueueSegmentWrapper<T>[] DiscoverSegmentsWrapped<T>(this DiskQueueSegmentFactory<T> factory, string path)
        {
            TurboContract.Requires(factory != null, conditionString: "factory != null");

            var discovered = factory.DiscoverSegments(path);
            if (discovered == null)
                throw new InvalidOperationException("Existed segment discovery returned null");

            DiskQueueSegmentWrapper<T>[] result = new DiskQueueSegmentWrapper<T>[discovered.Length];
            for (int i = 0; i < discovered.Length; i++)
                result[i] = new DiskQueueSegmentWrapper<T>(discovered[i]);

            return result;
        }
    }


    /// <summary>
    /// Wraps DiskQueueSegment and provides protected disposability behaviour
    /// </summary>
    /// <typeparam name="T">The type of elements in segment</typeparam>
    internal sealed class DiskQueueSegmentWrapper<T> : IDisposable
    {
        private readonly DiskQueueSegment<T> _segment;
        private readonly EntryCountingEvent _entryCounter;
        private volatile DiskQueueSegmentWrapper<T> _nextSegment;


        /// <summary>
        /// DiskQueueSegmentWrapper constructor
        /// </summary>
        /// <param name="segment">Segment to wrap</param>
        public DiskQueueSegmentWrapper(DiskQueueSegment<T> segment)
        {
            if (segment == null)
                throw new ArgumentNullException(nameof(segment), "Created DiskQueueSegment cannot be null");

            _segment = segment;
            _entryCounter = new EntryCountingEvent();
            _nextSegment = null;
        }

        /// <summary>
        /// Link to the next segment (used to build linked-list of segments)
        /// </summary>
        public DiskQueueSegmentWrapper<T> NextSegment { get { return _nextSegment; } set { _nextSegment = value; } }

        /// <summary>
        /// Unique number of the segment that represents its position in the queue
        /// </summary>
        public long Number { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _segment.Number; } }

        /// <summary>
        /// Number of items inside segment
        /// </summary>
        public int Count { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _segment.Count; } }
        /// <summary>
        /// Indicates whether the segment is full
        /// </summary>
        public bool IsFull { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _segment.IsFull; } }
        /// <summary>
        /// Indicates whether the segment is completed and can be safely removed
        /// </summary>
        public bool IsCompleted { [MethodImpl(MethodImplOptions.AggressiveInlining)]  get { return _segment.IsCompleted; } }

        /// <summary>
        /// Adds new item to the segment, even when the segement is full
        /// </summary>
        /// <param name="item">New item</param>
        public void AddForced(T item)
        {
            using (var guard = _entryCounter.TryEnter())
            {
                if (!guard.IsAcquired)
                    return;

                _segment.AddForced(item);
            }
        }
        /// <summary>
        /// Adds new item to the tail of the segment (should return false when <see cref="IsFull"/> setted)
        /// </summary>
        /// <param name="item">New item</param>
        /// <returns>Was added sucessufully</returns>
        public bool TryAdd(T item)
        {
            using (var guard = _entryCounter.TryEnter())
            {
                if (!guard.IsAcquired)
                    return false;

                return _segment.TryAdd(item);
            }
        }
        /// <summary>
        /// Removes item from the head of the segment
        /// </summary>
        /// <param name="item">The item removed from segment</param>
        /// <returns>True if the item was removed</returns>
        public bool TryTake(out T item)
        {
            using (var guard = _entryCounter.TryEnter())
            {
                if (!guard.IsAcquired)
                {
                    item = default(T);
                    return false;
                }

                return _segment.TryTake(out item);
            }
        }
        /// <summary>
        /// Returns the item at the head of the segment without removing it
        /// </summary>
        /// <param name="item">The item at the head of the segment</param>
        /// <returns>True if the item was read</returns>
        public bool TryPeek(out T item)
        {
            using (var guard = _entryCounter.TryEnter())
            {
                if (!guard.IsAcquired)
                {
                    item = default(T);
                    return false;
                }

                return _segment.TryPeek(out item);
            }
        }

        /// <summary>
        /// Cleans-up resources
        /// </summary>
        /// <param name="disposeBehaviour">Flag indicating whether the segment can be removed from disk</param>
        public void Dispose(DiskQueueSegmentDisposeBehaviour disposeBehaviour)
        {
            _entryCounter.TerminateAndWait();
            _segment.Dispose(disposeBehaviour);
        }
        /// <summary>
        /// Cleans-up resources
        /// </summary>
        public void Dispose()
        {
            Dispose(DiskQueueSegmentDisposeBehaviour.None);
        }
    }
}
