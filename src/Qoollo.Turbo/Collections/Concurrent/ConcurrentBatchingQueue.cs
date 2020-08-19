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
#pragma warning disable 420
    /// <summary>
    /// Thread-safe queue (FIFO collection) with batch aggregation. Items enqueued one-by-one, but dequeued in batches
    /// </summary>
    /// <typeparam name="T">The type of elements in collection</typeparam>
    [DebuggerDisplay("Count = {Count}")]
    public class ConcurrentBatchingQueue<T>: ICollection, IEnumerable<T>
    {
        private volatile int _itemsCount;
        private volatile BatchingQueueSegment<T> _head;
        private volatile BatchingQueueSegment<T> _tail;


        /// <summary>
        /// <see cref="ConcurrentBatchingQueue{T}"/> constructor
        /// </summary>
        /// <param name="batchSize">Size of the batch</param>
        public ConcurrentBatchingQueue(int batchSize)
        {
            if (batchSize <= 0 || batchSize > int.MaxValue / 2)
                throw new ArgumentOutOfRangeException(nameof(batchSize), $"'{nameof(batchSize)}' should be positive and less than {int.MaxValue / 2}");

            _head = new BatchingQueueSegment<T>(batchSize);
            _tail = _head;
            _itemsCount = 0;
        }

        /// <summary>
        /// Reads 'head' and 'tail' atomically
        /// </summary>
        /// <param name="head">Current head of the queue</param>
        /// <param name="tail">Current tail of the queue</param>
        private void GetHeadTailAtomic(out BatchingQueueSegment<T> head, out BatchingQueueSegment<T> tail)
        {
            head = _head;
            tail = _tail;
            SpinWait sw = new SpinWait();
            while (head != _head || tail != _tail)
            {
                sw.SpinOnce();
                head = _head;
                tail = _tail;
            }
        }

        /// <summary>
        /// Number of items inside the queue
        /// </summary>
        public int Count { get { return _itemsCount; } }

        /// <summary>
        /// Number of batches inside the queue
        /// </summary>
        public int BatchCount
        {
            get
            {
                GetHeadTailAtomic(out BatchingQueueSegment<T> head, out BatchingQueueSegment<T> tail);
                return unchecked((int)(tail.BatchId - head.BatchId + 1));
            }
        }

        /// <summary>
        /// Number of completed batches inside the queue (these batches can be dequeued)
        /// </summary>
        public int CompletedBatchCount
        {
            get
            {
                GetHeadTailAtomic(out BatchingQueueSegment<T> head, out BatchingQueueSegment<T> tail);
                return unchecked((int)(tail.BatchId - head.BatchId));
            }
        }

        /// <summary>
        /// Adds the item to the tail of the queue
        /// </summary>
        /// <param name="item">New item</param>
        /// <param name="batchCountIncreased">Number of new batches appeared during this enqueue</param>
        public void Enqueue(T item, out int batchCountIncreased)
        {
            batchCountIncreased = 0;

            SpinWait spinWait = new SpinWait();
            while (true)
            {
                BatchingQueueSegment<T> tail = _tail;

                if (tail.TryAdd(item))
                {
                    Interlocked.Increment(ref _itemsCount);
                    return;
                }

                if (tail.Next != null)
                {
                    if (Interlocked.CompareExchange(ref _tail, tail.Next, tail) == tail)
                        batchCountIncreased++;
                }

                spinWait.SpinOnce();
            }
        }

        /// <summary>
        /// Adds the item to the tail of the queue
        /// </summary>
        /// <param name="item">New item</param>
        public void Enqueue(T item)
        {
            Enqueue(item, out _);
        }

        /// <summary>
        /// Attempts to remove batch from the head of the queue
        /// </summary>
        /// <param name="segment">Removed batch</param>
        /// <returns>True if the batch was removed</returns>
        internal bool TryDequeue(out BatchingQueueSegment<T> segment)
        {
            SpinWait spinWait = new SpinWait();

            while (true)
            {
                BatchingQueueSegment<T> head = _head;
                if (head == _tail)
                {
                    segment = null;
                    return false;
                }

                Debug.Assert(head.Next != null);

                if (Interlocked.CompareExchange(ref _head, head.Next, head) == head)
                {
                    SpinWait completionSw = new SpinWait();
                    while (!head.IsNotInWork)
                        completionSw.SpinOnce();

                    Interlocked.Add(ref _itemsCount, -head.Count);
                    segment = head;
                    return true;
                }

                spinWait.SpinOnce();
            }
        }

        /// <summary>
        /// Attempts to remove batch from the head of the queue
        /// </summary>
        /// <param name="items">Removed batch</param>
        /// <returns>True if the batch was removed</returns>
        public bool TryDequeue(out T[] items)
        {
            if (TryDequeue(out BatchingQueueSegment<T> segment))
            {
                items = segment.ExtractArray();
                return true;
            }

            items = null;
            return false;
        }

        /// <summary>
        /// Mark active batch as completed so that it can be removed from the queue even if it is not full
        /// </summary>
        /// <returns>True when active batch is not empty, otherwise false</returns>
        public bool CompleteCurrentBatch()
        {
            BatchingQueueSegment<T> tail = _tail;
            if (_tail.Count == 0)
                return false;

            if (tail.Grow() && tail.Next != null)
            {
                if (Interlocked.CompareExchange(ref _tail, tail.Next, tail) == tail)
                    return true;
            }

            return false;
        }


        /// <summary>
        /// Reads 'head' and 'tail' atomically and mark all the segments in between for observation.
        /// This ensures that the arrays inside the segments will not be exposed directly to the user
        /// </summary>
        /// <param name="head">Current head of the queue</param>
        /// <param name="tail">Current tail of the queue</param>
        /// <returns>True if the queue slice for observation is not empty, otherwise false</returns>
        private bool GetHeadTailForObservation(out BatchingQueueSegment<T> head, out BatchingQueueSegment<T> tail)
        {
            GetHeadTailAtomic(out head, out tail);

            // Mark for observation
            for (var current = head; current != tail; current = current.Next)
                current.MarkForObservation();

            tail.MarkForObservation();

            // move head forward to the current head position
            while (head != _head)
            {
                // All segments up to the tail was dequeued => nothing to enumerate
                if (head == tail)
                    return false;

                head = head.Next;
            }

            return true;
        }


        /// <summary>
        /// Copies all items from queue into a new <see cref="List{T}"/>
        /// </summary>
        /// <returns>List</returns>
        private List<T> ToList()
        {
            if (!GetHeadTailForObservation(out BatchingQueueSegment<T> head, out BatchingQueueSegment<T> tail))
                return new List<T>();

            List<T> result = new List<T>(Count);

            for (var current = head; current != tail; current = current.Next)
            {
                foreach (var elem in current)
                    result.Add(elem);
            }

            // Iterate tail
            foreach (var elem in tail)
                result.Add(elem);

            return result;
        }


        /// <summary>
        /// Copies all items from queue into a new array
        /// </summary>
        /// <returns>An array</returns>
        public T[] ToArray()
        {
            if (Count == 0)
                return new T[0];

            return ToList().ToArray();
        }


        /// <summary>
        /// Copies all items from queue into a specified array
        /// </summary>
        /// <param name="array">Array that is the destination of the elements copied</param>
        /// <param name="index">Index in array at which copying begins</param>
        public void CopyTo(T[] array, int index)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (index < 0 || index >= array.Length)
                throw new ArgumentOutOfRangeException(nameof(index));

            ToList().CopyTo(array, index);
        }

        /// <summary>
        /// Returns Enumerator
        /// </summary>
        /// <returns>Enumerator</returns>
        public IEnumerator<T> GetEnumerator()
        {
            if (!GetHeadTailForObservation(out BatchingQueueSegment<T> head, out BatchingQueueSegment<T> tail))
                yield break;

            for (var current = head; current != tail; current = current.Next)
            {
                foreach (var elem in current)
                    yield return elem;
            }

            // Iterate tail
            foreach (var elem in tail)
                yield return elem;
        }

        /// <summary>
        /// Returns Enumerator
        /// </summary>
        /// <returns>Enumerator</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }


        /// <summary>
        /// Is synchronized collection
        /// </summary>
        bool ICollection.IsSynchronized { get { return false; } }
        /// <summary>
        /// Sychronization object (not supported)
        /// </summary>
        object ICollection.SyncRoot
        {
            get
            {
                throw new NotSupportedException("SyncRoot is not supported for BlockingQueue");
            }
        }

        /// <summary>
        /// Copy queue items to the array
        /// </summary>
        /// <param name="array">Target array</param>
        /// <param name="index">Start index</param>
        void ICollection.CopyTo(Array array, int index)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (index < 0 || index >= array.Length)
                throw new ArgumentOutOfRangeException(nameof(index));

            ((ICollection)(this.ToList())).CopyTo(array, index);
        }
    }

#pragma warning restore 420
}
