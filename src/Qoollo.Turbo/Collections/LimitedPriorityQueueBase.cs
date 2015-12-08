using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Collections
{
    /// <summary>
    /// Priority queue with fixed number of priorities
    /// </summary>
    /// <typeparam name="TElem">Specifies the type of elements in the queue</typeparam>
    /// <typeparam name="TPriority">Specifies the type of priority marker</typeparam>
    [System.Diagnostics.DebuggerDisplay("Count = {Count}")]
    [Serializable]
    [ContractClass(typeof(LimitedPriorityQueueBaseCodeContract<,>))]
    public abstract class LimitedPriorityQueueBase<TElem, TPriority> : IEnumerable<TElem>, IReadOnlyCollection<TElem>, ICollection, IEnumerable
    {
        private Queue<TElem>[] _innerQueue;
        private int _count;

        [NonSerialized]
        private object _syncRoot;

        /// <summary>
        /// Code contracts
        /// </summary>
        [ContractInvariantMethod]
        private void Invariant()
        {
            Contract.Invariant(_innerQueue != null);
            Contract.Invariant(_innerQueue.Length > 0);
            Contract.Invariant(_count >= 0);
        }

        /// <summary>
        /// LimitedPriorityQueueBase constructor
        /// </summary>
        /// <param name="priorityLevelsCount">The number of priority levels (the max value returned from MapPriority method)</param>
        public LimitedPriorityQueueBase(int priorityLevelsCount)
        {
            Contract.Requires<ArgumentException>(priorityLevelsCount > 0);

            _innerQueue = new Queue<TElem>[priorityLevelsCount];
            for (int i = 0; i < _innerQueue.Length; i++)
                _innerQueue[i] = new Queue<TElem>();
        }

        /// <summary>
        /// LimitedPriorityQueueBase constructor
        /// </summary>
        /// <param name="collection">The collection whose elements are copied to the new priority queue on the lowest priority level</param>
        /// <param name="priorityLevelsCount">The number of priority levels (the max value returned from MapPriority method)</param>
        public LimitedPriorityQueueBase(IEnumerable<TElem> collection, int priorityLevelsCount)
        {
            Contract.Requires<ArgumentNullException>(collection != null);
            Contract.Requires<ArgumentException>(priorityLevelsCount > 0);

            _innerQueue = new Queue<TElem>[priorityLevelsCount];
            for (int i = 0; i < _innerQueue.Length - 1; i++)
                _innerQueue[i] = new Queue<TElem>();
            _innerQueue[_innerQueue.Length - 1] = new Queue<TElem>(collection);
        }

        /// <summary>
        /// Convert priority marker to the according integral number of that priority
        /// </summary>
        /// <param name="prior">Priority marker</param>
        /// <returns>Integral priority level</returns>
        protected abstract int MapPriority(TPriority prior);

        /// <summary>
        /// Gets the number of priority levels of the queue
        /// </summary>
        protected int PriorityLevelsCount
        {
            get { return _innerQueue.Length; }
        }

        /// <summary>
        /// Gets the number of elements contained in the priority queue
        /// </summary>
        public int Count
        {
            get { return _count; }
        }

        /// <summary>
        /// Removes all objects from the priority queue
        /// </summary>
        public void Clear()
        {
            Contract.Ensures(this.Count == 0);

            for (int i = 0; i < _innerQueue.Length; i++)
                _innerQueue[i].Clear();

            _count = 0;
        }


        /// <summary>
        /// Copies the queue elements to an Array, starting at the specified array index
        /// </summary>
        /// <param name="array">Destination array</param>
        /// <param name="index">Index in array at which copying begins</param>
        public void CopyTo(TElem[] array, int index)
        {
            Contract.Requires<ArgumentNullException>(array != null);
            Contract.Requires<ArgumentOutOfRangeException>(index >= 0 && index < array.Length);
            Contract.Requires<ArgumentException>(index <= array.Length - this.Count);

            int curIndex = index;
            for (int i = 0; i < _innerQueue.Length; i++)
            {
                _innerQueue[i].CopyTo(array, curIndex);
                curIndex += _innerQueue[i].Count;
            }
        }

        /// <summary>
        /// Adds an object to the priority queue at specified priority level
        /// </summary>
        /// <param name="item">The item to add to the queue</param>
        /// <param name="priority">Priority marker</param>
        public void Enqueue(TElem item, TPriority priority)
        {
            Contract.Ensures(this.Count == Contract.OldValue(this.Count) + 1);

            int map = MapPriority(priority);
            _innerQueue[map].Enqueue(item);
            _count++;
        }


        /// <summary>
        /// Returns the item at the head of the priority queue without removing it
        /// </summary>
        /// <returns>The item at the head of the queue</returns>
        public TElem Peek()
        {
            Contract.Requires(this.Count > 0);

            if (Count == 0)
                throw new InvalidOperationException("Collection is empty");

            for (int i = 0; i < _innerQueue.Length; i++)
            {
                if (_innerQueue[i].Count > 0)
                    return _innerQueue[i].Peek();
            }

            throw new InvalidOperationException("Collection is empty");
        }

        /// <summary>
        /// Removes and returns the item at the head of the priority queue (item with the highest available priority)
        /// </summary>
        /// <returns>The item that is removed from the head of the queue</returns>
        public TElem Dequeue()
        {
            Contract.Requires(this.Count > 0);
            Contract.Ensures(this.Count == Contract.OldValue(this.Count) - 1);

            if (Count == 0)
                throw new InvalidOperationException("Collection is empty");

            for (int i = 0; i < _innerQueue.Length; i++)
            {
                if (_innerQueue[i].Count > 0)
                {
                    var res = _innerQueue[i].Dequeue();
                    _count--;
                    return res;
                }
            }

            throw new InvalidOperationException("Collection is empty");
        }

        /// <summary>
        /// Determines whether an element is in the priority queue
        /// </summary>
        /// <param name="item">The element to locate</param>
        /// <returns>True if the item is found</returns>
        public bool Contains(TElem item)
        {
            if (Count == 0)
                return false;

            for (int i = 0; i < _innerQueue.Length; i++)
            {
                if (_innerQueue[i].Contains(item))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Copies the priority queue elements to a new array
        /// </summary>
        /// <returns>A new array containing elements copied from the queue</returns>
        public TElem[] ToArray()
        {
            Contract.Ensures(Contract.Result<TElem[]>() != null);
            Contract.Ensures(Contract.Result<TElem[]>().Length == this.Count);

            TElem[] array = new TElem[this._count];
            this.CopyTo(array, 0);
            return array;
        }

        /// <summary>
        /// Sets the capacity to the actual number of elements in the priority queue
        /// </summary>
        public void TrimExcess()
        {
            for (int i = 0; i < _innerQueue.Length; i++)
            {
                _innerQueue[i].TrimExcess();
            }
        }

        /// <summary>
        /// Returns an Enumerator
        /// </summary>
        /// <returns>Enumerator</returns>
        public IEnumerator<TElem> GetEnumerator()
        {
            for (int i = 0; i < _innerQueue.Length; i++)
            {
                foreach (var elem in _innerQueue[i])
                    yield return elem;
            }
        }

        /// <summary>
        /// Returns an Enumerator
        /// </summary>
        /// <returns>Enumerator</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        /// <summary>
        /// Copies the queue elements to an Array, starting at the specified array index
        /// </summary>
        /// <param name="array">Destination array</param>
        /// <param name="index">Index in array at which copying begins</param>
        void ICollection.CopyTo(Array array, int index)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (array.Rank != 1)
                throw new ArgumentException("Array rank not equal to 1", "array");
            if (index < 0 || index > array.Length)
                throw new ArgumentOutOfRangeException("index", "index < 0 || index > array.Length");
            if (array.Length - index < this._count)
                throw new ArgumentException("array has not enough space");

            if (this._count == 0)
                return;

            int curIndex = index;
            for (int i = 0; i < _innerQueue.Length; i++)
            {
                (_innerQueue[i] as ICollection).CopyTo(array, curIndex);
                curIndex += _innerQueue[i].Count;
            }
        }

        /// <summary>
        /// Is collection synchronized
        /// </summary>
        bool ICollection.IsSynchronized
        {
            get { return false; }
        }

        /// <summary>
        /// Synchronization object
        /// </summary>
        object ICollection.SyncRoot
        {
            get
            {
                if (this._syncRoot == null)
                    Interlocked.CompareExchange<object>(ref this._syncRoot, new object(), null);

                return this._syncRoot;
            }
        }
    }





    /// <summary>
    /// Code contracts for LimitedPriorityQueueBase
    /// </summary>
    /// <typeparam name="TElem">Specifies the type of elements in the queue</typeparam>
    /// <typeparam name="TPriority">Specifies the type of priority marker</typeparam>
    [ContractClassFor(typeof(LimitedPriorityQueueBase<,>))]
    abstract class LimitedPriorityQueueBaseCodeContract<TElem, TPriority> : LimitedPriorityQueueBase<TElem, TPriority>
    {
        private LimitedPriorityQueueBaseCodeContract(): base(1) { }

        protected override int MapPriority(TPriority prior)
        {
            Contract.Ensures(Contract.Result<int>() >= 0);
            Contract.Ensures(Contract.Result<int>() < this.PriorityLevelsCount);

            throw new NotImplementedException();
        }
    }
}
