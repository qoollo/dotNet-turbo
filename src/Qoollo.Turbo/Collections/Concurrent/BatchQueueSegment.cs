using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Collections.Concurrent
{
    /// <summary>
    /// Segment of <see cref="ConcurrentBatchQueue{T}"/>
    /// </summary>
    /// <typeparam name="T">The type of elements in segment</typeparam>
    [DebuggerDisplay("Count = {Count}")]
    internal sealed class BatchQueueSegment<T> : IEnumerable<T>
    {
        private readonly T[] _array;
        private readonly long _batchId;

        private volatile int _reservedIndex;
        private volatile int _actualCount;

        private volatile BatchQueueSegment<T> _next;

        /// <summary>
        /// <see cref="BatchQueueSegment{T}"/> constructor
        /// </summary>
        /// <param name="capacity">Capacity of the segment</param>
        /// <param name="batchId">Incremental identifier of batch</param>
        public BatchQueueSegment(int capacity, long batchId)
        {
            TurboContract.Requires(capacity > 0 && capacity <= int.MaxValue / 2, "'capacity' should be positive and less than int.MaxValue / 2");

            _batchId = batchId;
            _reservedIndex = -1;
            _actualCount = 0;
            _array = new T[capacity];

            _next = null;
        }
        /// <summary>
        /// <see cref="BatchQueueSegment{T}"/> constructor
        /// </summary>
        /// <param name="capacity">Capacity of the segment</param>
        public BatchQueueSegment(int capacity)
            : this(capacity, 0)
        {
        }

        /// <summary>
        /// Incremental batch identifier
        /// </summary>
        public long BatchId { get { return _batchId; } }
        /// <summary>
        /// Segment capacity
        /// </summary>
        public int Capacity { get { return _array.Length; } }
        /// <summary>
        /// Number of items stored in this segment
        /// </summary>
        public int Count { get { return _actualCount; } }
        /// <summary>
        /// Next segment (segments are stored as Linked List)
        /// </summary>
        public BatchQueueSegment<T> Next { get { return _next; } }
        /// <summary>
        /// 'true' if the segment is complete or no parallel incomplete inserts are currently in progress
        /// </summary>
        public bool IsNotInWork { get { return _actualCount == _array.Length || _actualCount == _reservedIndex + 1; } }

        /// <summary>
        /// Returns array of items stored inside the segment
        /// </summary>
        /// <returns>Array of items</returns>
        public T[] ExtractArray()
        {
            if (Capacity == Count)
                return _array;

            var result = new T[Count];
            Array.Copy(_array, result, result.Length);
            return result;
        }

        internal bool Grow()
        {
            if (_next != null)
                return false;

            var newBucket = new BatchQueueSegment<T>(Capacity, unchecked(_batchId + 1));
            return Interlocked.CompareExchange(ref _next, newBucket, null) == null;
        }

        public bool TryAdd(T item)
        {
            bool result = false;

            try { }
            finally
            {
                int newPosition = Interlocked.Increment(ref _reservedIndex);
                if (newPosition < _array.Length)
                {
                    _array[newPosition] = item;
                    Interlocked.Increment(ref _actualCount);
                    result = true;
                }
                // Grow, when current segment is filled
                if (newPosition == _array.Length - 1)
                    Grow();
            }

            return result;
        }


        /// <summary>
        /// Returns Enumerator
        /// </summary>
        /// <returns>Enumerator</returns>
        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
                yield return _array[i];
        }
        /// <summary>
        /// Returns Enumerator
        /// </summary>
        /// <returns>Enumerator</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
