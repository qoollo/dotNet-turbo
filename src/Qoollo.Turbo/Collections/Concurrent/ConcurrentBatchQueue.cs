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

    [DebuggerDisplay("Count = {Count}")]
    public class ConcurrentBatchQueue<T>: ICollection, IEnumerable<T>
    {
        private volatile int _itemsCount;
        private volatile BatchQueueSegment<T> _head;
        private volatile BatchQueueSegment<T> _tail;

        public ConcurrentBatchQueue(int batchSize)
        {
            if (batchSize <= 0 || batchSize > int.MaxValue / 2)
                throw new ArgumentOutOfRangeException(nameof(batchSize), $"'{nameof(batchSize)}' should be positive and less than {int.MaxValue / 2}");

            _head = new BatchQueueSegment<T>(batchSize);
            _tail = _head;
            _itemsCount = 0;
        }


        private void GetHeadTailAtomic(out BatchQueueSegment<T> head, out BatchQueueSegment<T> tail)
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


        public int Count { get { return _itemsCount; } }

        public int BatchCount
        {
            get
            {
                GetHeadTailAtomic(out BatchQueueSegment<T> head, out BatchQueueSegment<T> tail);
                return unchecked((int)(tail.BatchId - head.BatchId + 1));
            }
        }

        public int CompletedBatchCount { get { return BatchCount - 1; } }

        public void Enqueue(T item, out int batchCountIncreased)
        {
            batchCountIncreased = 0;

            SpinWait spinWait = new SpinWait();
            while (true)
            {
                BatchQueueSegment<T> tail = _tail;

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

        public void Enqueue(T item)
        {
            Enqueue(item, out _);
        }

        internal bool TryDequeue(out BatchQueueSegment<T> segment)
        {
            SpinWait spinWait = new SpinWait();

            while (true)
            {
                BatchQueueSegment<T> head = _head;
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

        public bool TryDequeue(out T[] items)
        {
            if (TryDequeue(out BatchQueueSegment<T> segment))
            {
                items = segment.ExtractArray();
                return true;
            }

            items = null;
            return false;
        }


        public bool TryCompleteCurrentBatch()
        {
            BatchQueueSegment<T> tail = _tail;
            if (_tail.Count == 0)
                return false;

            if (tail.Grow() && tail.Next != null)
            {
                if (Interlocked.CompareExchange(ref _tail, tail.Next, tail) == tail)
                    return true;
            }

            return false;
        }



        private List<T> ToList()
        {
            List<T> result = new List<T>(Count);

            BatchQueueSegment<T> current = _head;

            while (current != null)
            {
                foreach (var elem in current)
                    result.Add(elem);

                current = current.Next;
            }

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
            TurboContract.Assert(array != null, conditionString: "array != null");
            TurboContract.Assert(index >= 0 && index < array.Length, conditionString: "index >= 0 && index < array.Length");

            ToList().CopyTo(array, index);
        }

        /// <summary>
        /// Returns Enumerator
        /// </summary>
        /// <returns>Enumerator</returns>
        public IEnumerator<T> GetEnumerator()
        {
            BatchQueueSegment<T> current = _head;

            while (current != null)
            {
                foreach (var elem in current)
                    yield return elem;

                current = current.Next;
            }
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

            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (index < 0 || index >= array.Length)
                throw new ArgumentOutOfRangeException(nameof(index));


            T[] localArray = this.ToArray();
            if (array.Length - index < localArray.Length)
                throw new ArgumentException("Not enough space in target array");


            Array.Copy(localArray, 0, array, index, localArray.Length);
        }
    }

#pragma warning restore 420
}
