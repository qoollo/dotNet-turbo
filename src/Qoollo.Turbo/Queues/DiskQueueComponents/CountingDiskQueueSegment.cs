using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Queues.DiskQueueComponents
{
    /// <summary>
    /// Segment for DiskQueue that limits the capacity by the number of stored items.
    /// Provides base implementation of counting logic
    /// </summary>
    /// <typeparam name="T">The type of elements in segment</typeparam>
    public abstract class CountingDiskQueueSegment<T>: DiskQueueSegment<T>
    {
        private readonly int _capacity;
        private volatile int _itemCount;
        private volatile int _fillCount;
        private volatile int _takenCount;

        /// <summary>
        /// CountingDiskQueueSegment constructor
        /// </summary>
        /// <param name="capacity">Maximum number of stored items inside the segement (overall capacity)</param>
        /// <param name="initialItemCount">Count of already presented items inside the segment</param>
        /// <param name="fillCount">Number of items that was stored inside segment (number of filled slots for items)</param>
        /// <param name="segmentNumber">Segment number</param>
        public CountingDiskQueueSegment(long segmentNumber, int capacity, int initialItemCount, int fillCount)
            : base(segmentNumber)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity should be positive");
            if (initialItemCount < 0)
                throw new ArgumentOutOfRangeException(nameof(initialItemCount));
            if (fillCount < initialItemCount)
                throw new ArgumentOutOfRangeException(nameof(fillCount), "FillCount cannot be less than initialItemCount");

            _capacity = capacity;
            _itemCount = initialItemCount;
            _fillCount = fillCount;
            _takenCount = fillCount - initialItemCount;
        }

        /// <summary>
        /// Maximum number of stored items inside the segement (including taken items)
        /// </summary>
        public int Capacity { get { return _capacity; } }
        /// <summary>
        /// Number of items inside segment
        /// </summary>
        public sealed override int Count { get { return _itemCount; } }
        /// <summary>
        /// Indicates whether the segment is full (no more items can be added to it excpetion items added by <see cref="AddForced(T)"/>)
        /// </summary>
        public sealed override bool IsFull { get { return _fillCount >= _capacity; } }
        /// <summary>
        /// Indicates whether the segment is completed and can be safely removed (should be equivalent to <see cref="Count"/> == 0 &amp;&amp; <see cref="IsFull"/>)
        /// </summary>
        public sealed override bool IsCompleted
        {
            get
            {
                // Estimation order is critical: takenCount should read before fillCount
                return _fillCount >= _capacity && _takenCount >= _fillCount;
            }
        }

        /// <summary>
        /// Adds new item to the tail of the segment (core implementation)
        /// </summary>
        /// <param name="item">New item</param>
        protected abstract void AddCore(T item);
        /// <summary>
        /// Removes item from the head of the segment (core implementation)
        /// </summary>
        /// <param name="item">The item removed from segment</param>
        /// <returns>True if the item was removed</returns>
        protected abstract bool TryTakeCore(out T item);
        /// <summary>
        /// Returns the item at the head of the segment without removing it (core implementation)
        /// </summary>
        /// <param name="item">The item at the head of the segment</param>
        /// <returns>True if the item was read</returns>
        protected abstract bool TryPeekCore(out T item);


        /// <summary>
        /// Adds new item to the segment, even when the segement is full
        /// </summary>
        /// <param name="item">New item</param>
        public sealed override void AddForced(T item)
        {
            bool itemAdded = false;
            try
            {
                Interlocked.Increment(ref _fillCount);
                AddCore(item);
                itemAdded = true;
            }
            finally
            {
                if (itemAdded)
                    Interlocked.Increment(ref _itemCount);
                else
                    Interlocked.Decrement(ref _fillCount);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryAcquireFillCount()
        {
            SpinWait sw = new SpinWait();
            int fillCount = _fillCount;
            while (fillCount < _capacity && Interlocked.CompareExchange(ref _fillCount, fillCount + 1, fillCount) != fillCount)
            {
                sw.SpinOnce();
                fillCount = _fillCount;
            }

            return fillCount < _capacity;
        }
        /// <summary>
        /// Adds new item to the tail of the segment (should return false when <see cref="IsFull"/> setted)
        /// </summary>
        /// <param name="item">New item</param>
        /// <returns>Was added sucessufully</returns>
        public sealed override bool TryAdd(T item)
        {
            bool itemAdded = false;

            if (!TryAcquireFillCount())
                return false;

            try
            {
                AddCore(item);
                itemAdded = true;
                return true;
            }
            finally
            {
                if (itemAdded)
                    Interlocked.Increment(ref _itemCount);
                else
                    Interlocked.Decrement(ref _fillCount);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryAcquireItemCount()
        {
            SpinWait sw = new SpinWait();
            int itemCount = _itemCount;
            while (itemCount > 0 && Interlocked.CompareExchange(ref _itemCount, itemCount - 1, itemCount) != itemCount)
            {
                sw.SpinOnce();
                itemCount = _itemCount;
            }

            return itemCount > 0;
        }
        /// <summary>
        /// Removes item from the head of the segment
        /// </summary>
        /// <param name="item">The item removed from segment</param>
        /// <returns>True if the item was removed</returns>
        public sealed override bool TryTake(out T item)
        {
            bool itemTaken = false;

            if (!TryAcquireItemCount())
            {
                item = default(T);
                return false;
            }

            try
            {
                itemTaken = TryTakeCore(out item);
                Debug.Assert(itemTaken, "Item not taken");
            }
            finally
            {
                if (itemTaken)
                    Interlocked.Increment(ref _takenCount);
                else
                    Interlocked.Decrement(ref _itemCount);
            }

            return itemTaken;
        }
        /// <summary>
        /// Returns the item at the head of the segment without removing it
        /// </summary>
        /// <param name="item">The item at the head of the segment</param>
        /// <returns>True if the item was read</returns>
        public sealed override bool TryPeek(out T item)
        {
            return TryPeekCore(out item);
        }
    }
}
