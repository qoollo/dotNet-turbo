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
    /// Deque (double-ended queue) - collection of elements that can be expanded or contracted on both ends
    /// </summary>
    /// <typeparam name="T">The type of elements in the deque</typeparam>
    [System.Diagnostics.DebuggerDisplay("Count = {Count}")]
    [System.Diagnostics.DebuggerTypeProxy(typeof(Qoollo.Turbo.Collections.ServiceStuff.CollectionDebugView<>))]
    [Serializable]
    public class Deque<T> : IEnumerable<T>, IReadOnlyCollection<T>, ICollection, IEnumerable
    {
        /// <summary>
        /// Deque enumerator
        /// </summary>
		[Serializable]
		public struct Enumerator : IEnumerator<T>, IDisposable, IEnumerator
		{
			private Deque<T> _srcDeque;
			private int _index;
			private int _version;
			private T _currentElement;


            /// <summary>
            /// Enumerator constructor
            /// </summary>
            /// <param name="srcDeque">Source deque to enumerate</param>
            internal Enumerator(Deque<T> srcDeque)
            {
                Contract.Requires(srcDeque != null);
                _srcDeque = srcDeque;
                _version = _srcDeque._version;
                _index = -1;
                _currentElement = default(T);
            }

            /// <summary>
            /// Gets the element at the current position of the enumerator
            /// </summary>
			public T Current
			{
				get
				{
					if (_index < 0)
					{
						if (_index == -1)
                            throw new InvalidOperationException("Enumeration has not started yet");
						else
                            throw new InvalidOperationException("Enumeration has already ended");
					}
					return this._currentElement;
				}
			}

            /// <summary>
            /// Advances the enumerator to the next element
            /// </summary>
            /// <returns></returns>
			public bool MoveNext()
			{
				if (_version != _srcDeque._version)
                    throw new InvalidOperationException("Collection has changed during enumeration");

				if (_index == -2)
					return false;

				_index++;
				if (_index == _srcDeque._size)
				{
					_index = -2;
					_currentElement = default(T);
					return false;
				}
				_currentElement = _srcDeque.GetElement(this._index);
				return true;
			}

            /// <summary>
            /// Clean-up resources
            /// </summary>
            public void Dispose()
            {
                _index = -2;
                _currentElement = default(T);
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
				if (_version != _srcDeque._version)
                    throw new InvalidOperationException("Collection has changed during enumeration");
				_index = -1;
				_currentElement = default(T);
			}
		}


        private static readonly T[] EmptyArray = new T[0];

        /// <summary>
        /// Minimum grow step
        /// </summary>
        private const int MinimumGrow = 4;
        /// <summary>
        /// Factor, when array compaction is required (Count &lt; ShrinkRate * Capacity)
        /// </summary>
        private const double ShrinkRate = 0.9;
        /// <summary>
        /// Capacity grow factor
        /// </summary>
        private const int GrowFactor = 2;
        /// <summary>
        /// Initial capacity for non-empty deque
        /// </summary>
        private const int DefaultCapacity = 4;


		private T[] _elemArray;
		private int _head;
		private int _tail;
		private int _size;
		private int _version;

		[NonSerialized]
		private object _syncRoot;

        /// <summary>
        /// Code contracts
        /// </summary>
        [ContractInvariantMethod]
        private void Invariant()
        {
            Contract.Invariant(_elemArray != null);
            Contract.Invariant(_head >= 0);
            Contract.Invariant(_head < _elemArray.Length);
            Contract.Invariant(_tail >= 0);
            Contract.Invariant(_tail < _elemArray.Length);
            Contract.Invariant(_size >= 0);
        }
		
        /// <summary>
        /// Deque constructor
        /// </summary>
		public Deque()
		{
			_elemArray = Deque<T>.EmptyArray;
		}

        /// <summary>
        /// Deque constructor with specified initial capacity
        /// </summary>
        /// <param name="capacity">Initial capacity</param>
		public Deque(int capacity)
		{
            Contract.Requires<ArgumentOutOfRangeException>(capacity >= 0);

			_elemArray = new T[capacity];
			_head = 0;
			_tail = 0;
			_size = 0;
		}

        /// <summary>
        /// Deque constructor
        /// </summary>
        /// <param name="collection">The collection whose elements are copied to the new deque</param>
        public Deque(IEnumerable<T> collection)
		{
            Contract.Requires<ArgumentNullException>(collection != null);

            var col = collection as ICollection<T>;
            if (col != null)
            {
                _size = col.Count;
                _elemArray = new T[_size + 1];
                col.CopyTo(_elemArray, 0);
                _head = 0;
                _tail = _size;
                _version = 0;
            }
            else
            {
                var notStrictCol = collection as ICollection;
                if (notStrictCol != null)
                    _elemArray = new T[notStrictCol.Count + 1];
                else
                    _elemArray = new T[DefaultCapacity];
                _size = 0;
                _version = 0;
                _head = 0;
                _tail = 0;

                foreach (var elem in collection)
                    AddToBack(elem);
            }
		}

        /// <summary>
        /// Gets the number of elements in the deque
        /// </summary>
        public int Count
        {
            get { return this._size; }
        }

        /// <summary>
        /// Gets the capacity of the deque
        /// </summary>
        public int Capacity
        {
            get { return this._elemArray.Length; }
        }

        /// <summary>
        /// Get element at specified index from the begining of the deque
        /// </summary>
        /// <param name="i">Index of the element</param>
        /// <returns>Element at specified index</returns>
        [Pure]
        internal T GetElement(int i)
        {
            Contract.Requires(i >= 0);
            Contract.Requires(i <= this.Count);

            return _elemArray[(_head + i) % _elemArray.Length];
        }

        /// <summary>
        /// Removes all elements from the deque
        /// </summary>
		public void Clear()
		{
            Contract.Ensures(this.Count == 0);

			if (_head < _tail)
			{
				Array.Clear(_elemArray, _head, _size);
			}
			else
			{
				Array.Clear(_elemArray, _head, _elemArray.Length - _head);
				Array.Clear(_elemArray, 0, _tail);
			}
			_head = 0;
			_tail = 0;
			_size = 0;
			_version++;
		}

        /// <summary>
        /// Copies the deque elements to an existing array, starting at the specified array index.
        /// </summary>
        /// <param name="array">Destination array</param>
        /// <param name="index">Starting index</param>
		public void CopyTo(T[] array, int index)
		{
            Contract.Requires<ArgumentNullException>(array != null);
            Contract.Requires<ArgumentOutOfRangeException>(index >= 0 && index < array.Length);
            Contract.Requires<ArgumentException>(index <= array.Length - this.Count);

            if (this._size == 0)
                return;


            if (_head < _tail)
            {
                Array.Copy(_elemArray, _head, array, index, _size);
            }
            else
            {
                int endCopySize = _elemArray.Length - _head;
                Array.Copy(_elemArray, _head, array, index, endCopySize);
                Array.Copy(_elemArray, 0, array, index + endCopySize, _tail);
            }
		}


        /// <summary>
        /// Adds an element to the end of the deque
        /// </summary>
        /// <param name="item">Element to add</param>
		public void AddToBack(T item)
		{
            Contract.Ensures(this.Count == Contract.OldValue(this.Count) + 1);

			if (_size == _elemArray.Length)
			{
                int newSize = _elemArray.Length * GrowFactor;
                if (newSize < _elemArray.Length + MinimumGrow)
                    newSize = _elemArray.Length + MinimumGrow;
                this.SetCapacity(0, newSize);
			}
			_elemArray[_tail] = item;
			_tail = (_tail + 1) % _elemArray.Length;
			_size++;
			_version++;
		}

        /// <summary>
        /// Adds an element to the begining of the deque
        /// </summary>
        /// <param name="item">Element to add</param>
        public void AddToFront(T item)
        {
            Contract.Ensures(this.Count == Contract.OldValue(this.Count) + 1);

            if (_size == _elemArray.Length)
            {
                int newSize = _elemArray.Length * GrowFactor;
                if (newSize < _elemArray.Length + MinimumGrow)
                    newSize = _elemArray.Length + MinimumGrow;
                this.SetCapacity(1, newSize);
            }
            var newHead = (_head - 1 + _elemArray.Length) % _elemArray.Length;
            _elemArray[newHead] = item;
            _head = newHead;
            _size++;
            _version++;
        }

        /// <summary>
        /// Returns the element at the beginning of the deque without removing it
        /// </summary>
        /// <returns>The element at the beginning of the deque</returns>
        [Pure]
        public T PeekAtFront()
        {
            Contract.Requires(this.Count > 0);
            if (this._size == 0)
                throw new InvalidOperationException("Collection is empty");

            return _elemArray[_head];
        }

        /// <summary>
        /// Returns the element at the ending of the deque without removing it
        /// </summary>
        /// <returns>The element at the ending of the deque</returns>
        [Pure]
        public T PeekAtEnd()
        {
            Contract.Requires(this.Count > 0);
            if (this._size == 0)
                throw new InvalidOperationException("Collection is empty");

            var lastElem = (_tail - 1 + _elemArray.Length) % _elemArray.Length;
            return _elemArray[lastElem];
        }


        /// <summary>
        /// Removes an element at the begining of the deque
        /// </summary>
        /// <returns>The element at the beginning of the deque</returns>
		public T RemoveFromFront()
		{
            Contract.Requires(this.Count > 0);
            Contract.Ensures(this.Count == Contract.OldValue(this.Count) - 1);

			if (this._size == 0)
                throw new InvalidOperationException("Collection is empty");

			T result = _elemArray[this._head];
			_elemArray[this._head] = default(T);
			_head = (_head + 1) % _elemArray.Length;
			_size--;
			_version++;
			return result;
		}

        /// <summary>
        /// Removes an element at the ending of the deque
        /// </summary>
        /// <returns>The element at the ending of the deque</returns>
        public T RemoveFromBack()
        {
            Contract.Requires(this.Count > 0);
            Contract.Ensures(this.Count == Contract.OldValue(this.Count) - 1);

            if (this._size == 0)
                throw new InvalidOperationException("Collection is empty");

            var lastElem = (_tail - 1 + _elemArray.Length) % _elemArray.Length;
            T result = _elemArray[lastElem];
            _elemArray[lastElem] = default(T);
            _tail = lastElem;
            _size--;
            _version++;
            return result;
        }

        /// <summary>
        /// Determines whether an element is in the deque
        /// </summary>
        /// <param name="item">The element to locate</param>
        /// <returns>True if the item is found</returns>
        [Pure]
		public bool Contains(T item)
		{
			int curPos = this._head;
			int restCount = this._size;
			EqualityComparer<T> comparer = EqualityComparer<T>.Default;
			while (restCount-- > 0)
			{
				if (item == null)
                {
					if (this._elemArray[curPos] == null)
						return true;
				}
				else
				{
                    if (this._elemArray[curPos] != null && comparer.Equals(this._elemArray[curPos], item))
						return true;
				}
				curPos = (curPos + 1) % this._elemArray.Length;
			}
			return false;
		}

        /// <summary>
        /// Copies the deque elements to a new array
        /// </summary>
        /// <returns>A new array containing elements copied from the deque</returns>
		public T[] ToArray()
		{
            Contract.Ensures(Contract.Result<T[]>() != null);
            Contract.Ensures(Contract.Result<T[]>().Length == this.Count);

            T[] array = new T[_size];
            if (_size == 0)
				return array;

			if (_head < _tail)
			{
                Array.Copy(_elemArray, _head, array, 0, _size);
			}
			else
			{
				Array.Copy(_elemArray, _head, array, 0, _elemArray.Length - _head);
				Array.Copy(_elemArray, 0, array, _elemArray.Length - _head, _tail);
			}
			return array;
		}

        /// <summary>
        /// Sets the capacity to the actual number of elements in the deque, if that number is less than 90 percent of current capacity.
        /// </summary>
        public void TrimExcess()
        {
            Contract.Ensures(this.Count == Contract.OldValue(this.Count));

            int minSize = (int)((double)_elemArray.Length * ShrinkRate);
            if (_size < minSize)
                this.SetCapacity(0, _size);
        }

        /// <summary>
        /// Sets the capacity of the deque (resize internal array)
        /// </summary>
        /// <param name="headOffset">Desired head offset</param>
        /// <param name="capacity">Desired capacity</param>
		private void SetCapacity(int headOffset, int capacity)
		{
            Contract.Requires(capacity >= this.Count);

            if (headOffset < 0)
                headOffset = 0;
            else if (capacity - headOffset < _size)
                headOffset = capacity - _size;

			T[] array = new T[capacity];
			if (_size > 0)
			{
                if (_head < _tail)
                {
                    Array.Copy(_elemArray, _head, array, headOffset, _size);
                }
                else
                {
                    var countToEnd = _elemArray.Length - _head;
                    Array.Copy(_elemArray, _head, array, headOffset, countToEnd);
                    Array.Copy(_elemArray, 0, array, headOffset + countToEnd, _tail);
                }
			}
			_elemArray = array;
            _head = headOffset;
			_tail = ((_size == capacity) ? 0 : this._size);
			_version++;
		}



        /// <summary>
        /// Returns an Enumerator
        /// </summary>
        /// <returns>Enumerator</returns>
        public Deque<T>.Enumerator GetEnumerator()
        {
            return new Deque<T>.Enumerator(this);
        }

        /// <summary>
        /// Returns an Enumerator
        /// </summary>
        /// <returns>Enumerator</returns>
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new Deque<T>.Enumerator(this);
        }
        /// <summary>
        /// Returns an Enumerator
        /// </summary>
        /// <returns>Enumerator</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Deque<T>.Enumerator(this);
        }

        /// <summary>
        /// Copies the deque elements to an existing array, starting at the specified array index.
        /// </summary>
        /// <param name="array">Destination array</param>
        /// <param name="index">Starting index</param>
        void ICollection.CopyTo(Array array, int index)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (array.Rank != 1)
                throw new ArgumentException("Array rank not equal to 1", "array");
            if (index < 0 || index > array.Length)
                throw new ArgumentOutOfRangeException("index", "index < 0 || index > array.Length");
            if (array.Length - index < this._size)
                throw new ArgumentException("array has not enough space");

            if (this._size == 0)
                return;


            try
            {
                if (_head < _tail)
                {
                    Array.Copy(_elemArray, _head, array, index, _size);
                }
                else
                {
                    int endCopySize = _elemArray.Length - _head;
                    Array.Copy(_elemArray, _head, array, index, endCopySize);
                    Array.Copy(_elemArray, 0, array, index + endCopySize, _tail);
                }
            }
            catch (ArrayTypeMismatchException)
            {
                throw new ArgumentException("Array has wrong element type");
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
}
