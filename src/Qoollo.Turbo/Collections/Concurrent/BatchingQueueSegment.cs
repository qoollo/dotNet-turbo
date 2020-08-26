using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Collections.Concurrent
{
    /// <summary>
    /// Segment of <see cref="ConcurrentBatchingQueue{T}"/>
    /// </summary>
    /// <typeparam name="T">The type of elements in segment</typeparam>
    [DebuggerDisplay("Count = {Count}")]
    internal sealed class BatchingQueueSegment<T> : IEnumerable<T>
    {
        /// <summary>
        /// Enumerator for <see cref="BatchingQueueSegment{T}"/>
        /// </summary>
        public struct Enumerator : IEnumerator<T>, IDisposable, IEnumerator
        {
            private readonly BatchingQueueSegment<T> _source;
            private int _index;

            /// <summary>
            /// Enumerator constructor
            /// </summary>
            /// <param name="source">Source BatchQueueSegment to enumerate</param>
            public Enumerator(BatchingQueueSegment<T> source)
            {
                TurboContract.Requires(source != null, "source != null");

                _source = source;
                _index = -1;
            }


            /// <summary>
            /// Gets the element at the current position of the enumerator
            /// </summary>
            public T Current
            {
                get
                {
                    TurboContract.Assert(_index >= 0 && _index < _source.Count, "_index >= 0 && _index < _source.Count");
                    return _source._array[_index];
                }
            }

            /// <summary>
            /// Advances the enumerator to the next element
            /// </summary>
            /// <returns></returns>
            public bool MoveNext()
            {
                if (_index == -2)
                    return false;

                _index++;
                if (_index == _source.Count)
                {
                    _index = -2;
                    return false;
                }
                return true;
            }

            /// <summary>
            /// Clean-up resources
            /// </summary>
            public void Dispose()
            {
                _index = -2;
            }

            /// <summary>
            /// Gets the element at the current position of the enumerator
            /// </summary>
            object IEnumerator.Current
            {
                get { return this.Current; }
            }

            /// <summary>
            /// Sets the enumerator to its initial position
            /// </summary>
            void IEnumerator.Reset()
            {
                _index = -1;
            }
        }

        // =============

        private const int RESERVED_INDEX_MASK = int.MaxValue;
        private const int FINALIZATION_MASK = int.MinValue;

        private const int SegmentPreallocationCapacityThreshold = 4;

        private readonly T[] _array;
        private readonly int _batchId;
        private volatile bool _markedForObservation;

        private volatile int _reservedIndexWithFinalizationMark;
        private volatile int _actualCount;

        private volatile BatchingQueueSegment<T> _next;
        private volatile BatchingQueueSegment<T> _preallocatedNext;

        /// <summary>
        /// <see cref="BatchingQueueSegment{T}"/> constructor
        /// </summary>
        /// <param name="capacity">Capacity of the segment</param>
        /// <param name="batchId">Incremental identifier of batch</param>
        public BatchingQueueSegment(int capacity, int batchId)
        {
            TurboContract.Requires(capacity > 0 && capacity <= int.MaxValue / 2, "'capacity' should be positive and less than int.MaxValue / 2");

            _array = new T[capacity];
            _batchId = batchId;
            _markedForObservation = false;

            _reservedIndexWithFinalizationMark = 0;
            _actualCount = 0;

            _next = null;
            _preallocatedNext = null;
        }
        /// <summary>
        /// <see cref="BatchingQueueSegment{T}"/> constructor
        /// </summary>
        /// <param name="capacity">Capacity of the segment</param>
        public BatchingQueueSegment(int capacity)
            : this(capacity, 0)
        {
        }

        /// <summary>
        /// Checks whether the <paramref name="reservedIndexWithFinalizationMark"/> has finalization mark
        /// </summary>
        /// <param name="reservedIndexWithFinalizationMark">Value of reservedIndexWithFinalizationMark</param>
        /// <returns>True if finalization mark is setted</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsSegmentFinalized(int reservedIndexWithFinalizationMark)
        {
            return reservedIndexWithFinalizationMark < 0;
        }
        /// <summary>
        /// Set segment finalization mark into <paramref name="reservedIndexWithFinalizationMark"/>
        /// </summary>
        /// <param name="reservedIndexWithFinalizationMark">Value of reservedIndexWithFinalizationMark</param>
        /// <returns>Updated value of reservedIndexWithFinalizationMark</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int SetSegmentFinalized(int reservedIndexWithFinalizationMark)
        {
            return reservedIndexWithFinalizationMark | FINALIZATION_MASK;
        }
        /// <summary>
        /// Extracts ReservedIndex from <paramref name="reservedIndexWithFinalizationMark"/>
        /// </summary>
        /// <param name="reservedIndexWithFinalizationMark">Value of reservedIndexWithFinalizationMark</param>
        /// <returns>Reserved index value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ExtractReservedIndex(int reservedIndexWithFinalizationMark)
        {
            return reservedIndexWithFinalizationMark & RESERVED_INDEX_MASK;
        }

        /// <summary>
        /// Incremental batch identifier
        /// </summary>
        public int BatchId { get { return _batchId; } }
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
        public BatchingQueueSegment<T> Next { get { return _next; } }
        /// <summary>
        /// 'true' if the segment is complete or no parallel incomplete inserts are currently in progress
        /// </summary>
        public bool IsNotInWork
        {
            get
            {
                int reservedIndexWithFinalizationMark = _reservedIndexWithFinalizationMark;
                return IsSegmentFinalized(reservedIndexWithFinalizationMark) && (_actualCount == _array.Length || _actualCount == ExtractReservedIndex(reservedIndexWithFinalizationMark));
            }
        }

        /// <summary>
        /// Mark this segment as being observed (ExtractArray will copy the result)
        /// </summary>
        internal void MarkForObservation()
        {
            _markedForObservation = true;
        }

        /// <summary>
        /// Returns array of items stored inside the segment
        /// </summary>
        /// <returns>Array of items</returns>
        public T[] ExtractArray()
        {
            if (Capacity == Count && !_markedForObservation)
                return _array;

            var result = new T[Count];
            Array.Copy(_array, result, result.Length);
            return result;
        }


        /// <summary>
        /// Preallocates next segment to prevent contention during segment grow
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PreallocateNextSegment()
        {
            if (_preallocatedNext == null)
                _preallocatedNext = new BatchingQueueSegment<T>(Capacity, unchecked(_batchId + 1));
        }


        /// <summary>
        /// Attempts to create the next BatchingQueueSegment in the Linked List structure
        /// </summary>
        /// <returns>true - segments created and can be read through <see cref="Next"/> property, false - no new segment created due to already created next segment</returns>
        internal bool Grow()
        {
            int reservedIndexWithFinalizationMark = _reservedIndexWithFinalizationMark;
            if (IsSegmentFinalized(reservedIndexWithFinalizationMark))
                return false;

            bool result = false;
            var newSegment = _preallocatedNext ?? new BatchingQueueSegment<T>(Capacity, unchecked(_batchId + 1));
            SpinWait sw = new SpinWait();

            try { }
            finally
            {
                reservedIndexWithFinalizationMark = _reservedIndexWithFinalizationMark;
                while (!IsSegmentFinalized(reservedIndexWithFinalizationMark))
                {
                    if (Interlocked.CompareExchange(ref _reservedIndexWithFinalizationMark, SetSegmentFinalized(reservedIndexWithFinalizationMark), reservedIndexWithFinalizationMark) == reservedIndexWithFinalizationMark)
                    {
                        result = Interlocked.CompareExchange(ref _next, newSegment, null) == null;
                        TurboContract.Assert(result, "New segment update failed");
                        break;
                    }
                    sw.SpinOnce();
                    reservedIndexWithFinalizationMark = _reservedIndexWithFinalizationMark;
                }
            }

            if (result && Capacity >= SegmentPreallocationCapacityThreshold)
                newSegment.PreallocateNextSegment();

            return result;
        }

        /// <summary>
        /// Attemtpt to add new item at the end of the segment
        /// </summary>
        /// <param name="item">New item</param>
        /// <returns>true - item added, otherwise false (segment is full)</returns>
        public bool TryAdd(T item)
        {
            bool result = false;
            int reservedIndexWithFinalizationMark = _reservedIndexWithFinalizationMark;
            if (IsSegmentFinalized(reservedIndexWithFinalizationMark) || ExtractReservedIndex(reservedIndexWithFinalizationMark) > _array.Length)
                return false;

            try { }
            finally
            {
                reservedIndexWithFinalizationMark = Interlocked.Increment(ref _reservedIndexWithFinalizationMark); // Optimistic increment for better perf
                if (IsSegmentFinalized(reservedIndexWithFinalizationMark))
                {
                    Interlocked.Decrement(ref _reservedIndexWithFinalizationMark);
                }
                else
                {
                    int newPosition = ExtractReservedIndex(reservedIndexWithFinalizationMark) - 1;
                    if (newPosition < _array.Length)
                    {
                        _array[newPosition] = item;
                        Interlocked.Increment(ref _actualCount);
                        result = true;
                    }
                    // Grow, when current segment is full
                    if (newPosition >= _array.Length - 1)
                        Grow();
                }
            }

            return result;
        }


        /// <summary>
        /// Returns Enumerator
        /// </summary>
        /// <returns>Enumerator</returns>
        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }
        /// <summary>
        /// Returns Enumerator
        /// </summary>
        /// <returns>Enumerator</returns>
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
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
