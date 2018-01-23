using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Collections
{
    /// <summary>
    /// Represents a strongly typed list of objects that can be accessed by index.
    /// Equivalent to BCL List, but uses circular buffer inside, which gives O(1) insert and remove complexity on both ends of the list.
    /// </summary>
    /// <typeparam name="T">The type of elements in the CircularList</typeparam>
    [System.Diagnostics.DebuggerDisplay("Count = {Count}")]
    [System.Diagnostics.DebuggerTypeProxy(typeof(Qoollo.Turbo.Collections.ServiceStuff.CollectionDebugView<>))]
    [Serializable]
    public class CircularList<T> : IList<T>, IReadOnlyList<T>, IList, IReadOnlyCollection<T>, ICollection<T>, ICollection, IEnumerable<T>, IEnumerable
    {
        /// <summary>
        /// CircularList enumerator
        /// </summary>
        [Serializable]
        public struct Enumerator : IEnumerator<T>, IDisposable, IEnumerator
        {
            private CircularList<T> _srcList;
            private int _index;
            private int _version;
            private T _currentElement;


            /// <summary>
            /// Enumerator constructor
            /// </summary>
            /// <param name="srcList">Source CircularList to enumerate</param>
            internal Enumerator(CircularList<T> srcList)
            {
                TurboContract.Requires(srcList != null, conditionString: "srcList != null");

                _srcList = srcList;
                _version = _srcList._version;
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
                if (_version != _srcList._version)
                    throw new InvalidOperationException("Collection has changed during enumeration");

                if (_index == -2)
                    return false;

                _index++;
                if (_index == _srcList._size)
                {
                    _index = -2;
                    _currentElement = default(T);
                    return false;
                }
                _currentElement = _srcList[this._index];
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
                if (_version != _srcList._version)
                    throw new InvalidOperationException("Collection has changed during enumeration");
                _index = -1;
                _currentElement = default(T);
            }
        }

        // ============

        /// <summary>
        /// Detemines whether the object is compatible with type
        /// </summary>
        /// <param name="value">Source object</param>
        /// <returns>Can be casted to type T</returns>
        private static bool IsCompatibleObject(object value)
        {
            return value is T || (value == null && default(T) == null);
        }

        /// <summary>
        /// Throws ArgumentOutOfRangeException
        /// </summary>
        private static void ThrowArgumentOutOfRangeOnIndex()
        {
            throw new ArgumentOutOfRangeException("index");
        }

        private static readonly T[] EmptyArray = new T[0];

        // ================

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
        /// Initial capacity for non-empty circular list
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
            TurboContract.Invariant(_elemArray != null);
            TurboContract.Invariant(_head >= 0);
            TurboContract.Invariant((_head < _elemArray.Length) || (_elemArray.Length == 0 && _head == 0));
            TurboContract.Invariant(_tail >= 0);
            TurboContract.Invariant((_tail < _elemArray.Length) || (_elemArray.Length == 0 && _tail == 0));
            TurboContract.Invariant(_size >= 0);
            TurboContract.Invariant(_elemArray.Length == 0 || (((_head + _size) % _elemArray.Length) == _tail));
        }

        /// <summary>
        /// CircularList constructor
        /// </summary>
		public CircularList()
		{
			_elemArray = CircularList<T>.EmptyArray;
		}

        /// <summary>
        /// CircularList constructor with specified initial capacity
        /// </summary>
        /// <param name="capacity">Initial capacity</param>
		public CircularList(int capacity)
		{
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), "capacity cannot be negative");

			_elemArray = new T[capacity];
			_head = 0;
			_tail = 0;
			_size = 0;
		}

        /// <summary>
        /// CircularList constructor
        /// </summary>
        /// <param name="collection">The collection whose elements are copied to the new list</param>
        public CircularList(IEnumerable<T> collection)
		{
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));

            if (collection is ICollection<T> col)
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
                if (collection is ICollection notStrictCol)
                    _elemArray = new T[notStrictCol.Count];
                else
                    _elemArray = new T[DefaultCapacity];
                _size = 0;
                _version = 0;
                _head = 0;
                _tail = 0;

                foreach (var elem in collection)
                    this.Add(elem);
            }
        }


        /// <summary>
        /// Gets the number of elements in the list
        /// </summary>
        public int Count
        {
            get { return this._size; }
        }

        /// <summary>
        /// Gets the capacity of the list
        /// </summary>
        public int Capacity
        {
            get { return this._elemArray.Length; }
        }



        /// <summary>
        /// Gets or sets element of the list at specified index
        /// </summary>
        /// <param name="index">Element index</param>
        /// <returns>Element of the list</returns>
        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= this.Count)
                    ThrowArgumentOutOfRangeOnIndex();

                return _elemArray[(_head + index) % _elemArray.Length];
            }
            set
            {
                if (index < 0 || index >= this.Count)
                    ThrowArgumentOutOfRangeOnIndex();

                _elemArray[(_head + index) % _elemArray.Length] = value;
                _version++;
            }
        }


        /// <summary>
        /// Determines whether an element is in the List
        /// </summary>
        /// <param name="item">The object to locate in the List</param>
        /// <returns>True if item is found</returns>
        [Pure]
        public bool Contains(T item)
        {
            return this.IndexOf(item, 0, this.Count) >= 0;
        }


        /// <summary>
        /// Copies the list elements to an existing array, starting at the specified array index
        /// </summary>
        /// <param name="array">Destination array</param>
        /// <param name="index">Starting index</param>
        public void CopyTo(T[] array, int index)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (index < 0 || index >= array.Length)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (index > array.Length - this.Count)
                throw new ArgumentException(nameof(index));

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
        /// Copies the list elements to a new array
        /// </summary>
        /// <returns>A new array containing elements copied from the list</returns>
        public T[] ToArray()
        {
            TurboContract.Ensures(TurboContract.Result<T[]>() != null);
            TurboContract.Ensures(TurboContract.Result<T[]>().Length == this.Count);

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
        /// Searches for the specified 'item' and returns the index of the first occurrence of the item inside list
        /// </summary>
        /// <param name="item">The item to locate inside the list</param>
        /// <returns>The index of element inside the list, if found. -1 otherwise</returns>
        [Pure]
        public int IndexOf(T item)
        {
            return this.IndexOf(item, 0, this.Count);
        }
        /// <summary>
        /// Searches for the specified 'item' and returns the index of the first occurrence of the item inside list
        /// </summary>
        /// <param name="item">The item to locate inside the list</param>
        /// <param name="index">Starting index of the search</param>
        /// <param name="count">The number of elements in the section to search</param>
        /// <returns>The index of element inside the list, if found. -1 otherwise</returns>
        [Pure]
        public int IndexOf(T item, int index, int count)
        {
            if (index < 0 || index > this.Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (count < 0 || count > this.Count - index)
                throw new ArgumentOutOfRangeException(nameof(count));

            int curPos = (this._head + index) % this._elemArray.Length;
            int end = index + count;

            if (item == null)
            {
                for (int curIndex = index; curIndex < end; curIndex++)
                {
                    if (this._elemArray[curPos] == null)
                        return curIndex;

                    curPos = (curPos + 1) % this._elemArray.Length;
                }

                return -1;
            }


            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            for (int curIndex = index; curIndex < end; curIndex++)
            {
                if (this._elemArray[curPos] != null && comparer.Equals(this._elemArray[curPos], item))
                    return curIndex;

                curPos = (curPos + 1) % this._elemArray.Length;
            }
            return -1;
        }

        /// <summary>
        /// Searches for the specified 'item' and returns the index of the last occurrence of the item inside list
        /// </summary>
        /// <param name="item">The item to locate inside the list</param>
        /// <returns>The index of element inside the list, if found. -1 otherwise</returns>
        [Pure]
        public int LastIndexOf(T item)
        {
            return this.LastIndexOf(item, this.Count - 1, this.Count);
        }
        /// <summary>
        /// Searches for the specified 'item' and returns the index of the last occurrence of the item inside list
        /// </summary>
        /// <param name="item">The item to locate inside the list</param>
        /// <param name="index">Starting index of the search</param>
        /// <param name="count">The number of elements in the section to search</param>
        /// <returns>The index of element inside the list, if found. -1 otherwise</returns>
        [Pure]
        public int LastIndexOf(T item, int index, int count)
        {
            if (this.Count > 0 && index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (this.Count == 0 && index != -1)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (index >= this.Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (count > index + 1)
                throw new ArgumentOutOfRangeException(nameof(count));

            if (this.Count == 0)
                return -1;

            int curPos = (this._head + index) % this._elemArray.Length;
            int start = index - count;

            if (item == null)
            {
                for (int curIndex = index; curIndex > start; curIndex--)
                {
                    TurboContract.Assert(curIndex >= 0, conditionString: "curIndex >= 0");

                    if (this._elemArray[curPos] == null)
                        return curIndex;

                    curPos = (curPos + this._elemArray.Length - 1) % this._elemArray.Length;
                }

                return -1;
            }


            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            for (int curIndex = index; curIndex > start; curIndex--)
            {
                TurboContract.Assert(curIndex >= 0, conditionString: "curIndex >= 0");

                if (this._elemArray[curPos] != null && comparer.Equals(this._elemArray[curPos], item))
                    return curIndex;

                curPos = (curPos + this._elemArray.Length - 1) % this._elemArray.Length;
            }
            return -1;
        }

        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified predicate and returns its index
        /// </summary>
        /// <param name="match">Predicate</param>
        /// <returns>The index of element inside the list, if found. -1 otherwise</returns>
        public int FindIndex(Predicate<T> match)
        {
            if (match == null)
                throw new ArgumentNullException(nameof(match));

            int curPos = this._head;

            for (int i = 0; i < this._size; i++)
            {
                if (match(this._elemArray[curPos]))
                    return i;

                curPos = (curPos + 1) % this._elemArray.Length;
            }
            return -1;
        }


        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified predicate and returns the index of the last occurrence
        /// </summary>
        /// <param name="match">Predicate</param>
        /// <returns>The index of element inside the list, if found. -1 otherwise</returns>
        public int FindLastIndex(Predicate<T> match)
        {
            if (match == null)
                throw new ArgumentNullException(nameof(match));

            int curPos = (this._tail + this._elemArray.Length - 1) % this._elemArray.Length;

            for (int i = this.Count - 1; i >= 0; i--)
            {
                if (match(this._elemArray[curPos]))
                    return i;

                curPos = (curPos + this._elemArray.Length - 1) % this._elemArray.Length;
            }
            return -1;
        }


        /// <summary>
        /// Search for the fist element that match the conditions defined by the specified predicate
        /// </summary>
        /// <param name="match">Predicate</param>
        /// <returns>The first element that matches the condition, if found; otherwise, the default value for type T</returns>
        public T Find(Predicate<T> match)
        {
            TurboContract.Requires(match != null, conditionString: "match != null");

            int index = this.FindIndex(match);
            return index < 0 ? default(T) : this[index];
        }

        /// <summary>
        /// Determines whether the list contains elements that match the conditions defined by the specified predicate
        /// </summary>
        /// <param name="match">Predicate</param>
        /// <returns>True if the list contains elements that match the condition</returns>
        [Pure]
        public bool Exists(Predicate<T> match)
        {
            TurboContract.Requires(match != null, conditionString: "match != null");

            return this.FindIndex(match) >= 0;
        }

        /// <summary>
        /// Performs the specified action on each element of the list
        /// </summary>
        /// <param name="action">Action</param>
        public void ForEach(Action<T> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            int version = this._version;
            int curPos = this._head;

            for (int i = 0; i < this._size && version == this._version; i++)
            {
                action(this._elemArray[curPos]);
                curPos = (curPos + 1) % this._elemArray.Length;
            }

            if (version != this._version)
                throw new InvalidOperationException("Collection was changed while executing ForEach method");
        }


        /// <summary>
        /// Inserts an item to the list at the specified index
        /// </summary>
        /// <param name="index">Index</param>
        /// <param name="item">New element</param>
        public void Insert(int index, T item)
        {
            TurboContract.Ensures(this.Count == TurboContract.OldValue(this.Count) + 1);

            if (index < 0 || index > _size)
                throw new ArgumentOutOfRangeException(nameof(index));

            bool moveToBeginning = index <= this.Count / 2;
            
            if (_size == _elemArray.Length)
                this.EnsureCapacity(moveToBeginning ? 1 : 0, this.Count + 1);

            if (moveToBeginning)
            {
                int insertPos = (index + _head + _elemArray.Length - 1) % _elemArray.Length;
                int newHead = (_head - 1 + _elemArray.Length) % _elemArray.Length;

                if (index > 0)
                {
                    if (_head <= _elemArray.Length - index)
                    {
                        if (_head > 0)
                        {
                            Array.Copy(_elemArray, _head, _elemArray, _head - 1, index);
                        }
                        else
                        {
                            TurboContract.Assert(_head == 0, conditionString: "_head == 0");
                            _elemArray[_elemArray.Length - 1] = _elemArray[0];
                            Array.Copy(_elemArray, 1, _elemArray, 0, insertPos);
                        }
                    }
                    else
                    {
                        Array.Copy(_elemArray, _head, _elemArray, _head - 1, _elemArray.Length - _head);
                        _elemArray[_elemArray.Length - 1] = _elemArray[0];
                        if (insertPos != 0)
                            Array.Copy(_elemArray, 1, _elemArray, 0, insertPos);
                    }
                }

                _elemArray[insertPos] = item;
                _head = newHead;
            }
            else
            {
                int insertPos = (index + _head) % _elemArray.Length;
                int newTail = (_tail + 1) % _elemArray.Length;

                if (index < _size)
                {
                    if (insertPos <= _tail)
                    {
                        Array.Copy(_elemArray, insertPos, _elemArray, insertPos + 1, _tail - insertPos);
                    }
                    else
                    {
                        Array.Copy(_elemArray, 0, _elemArray, 1, _tail);
                        _elemArray[0] = _elemArray[_elemArray.Length - 1];
                        if (insertPos != _elemArray.Length - 1)
                            Array.Copy(_elemArray, insertPos, _elemArray, insertPos + 1, _elemArray.Length - insertPos - 1);
                    }
                }


                _elemArray[insertPos] = item;
                _tail = newTail;
            }

            _size++;
            _version++;
        }


        /// <summary>
        /// Adds an element to the begining of the list
        /// </summary>
        /// <param name="item">Element to add</param>
        public void AddFirst(T item)
        {
            TurboContract.Ensures(this.Count == TurboContract.OldValue(this.Count) + 1);

            if (_size == _elemArray.Length)
                this.EnsureCapacity(1, this.Count + 1);
            var newHead = (_head - 1 + _elemArray.Length) % _elemArray.Length;
            _elemArray[newHead] = item;
            _head = newHead;
            _size++;
            _version++;
        }

        /// <summary>
        /// Adds an element to the end of the list
        /// </summary>
        /// <param name="item">Element to add</param>
        public void AddLast(T item)
        {
            TurboContract.Ensures(this.Count == TurboContract.OldValue(this.Count) + 1);

            if (_size == _elemArray.Length)
                this.EnsureCapacity(0, this.Count + 1);
            _elemArray[_tail] = item;
            _tail = (_tail + 1) % _elemArray.Length;
            _size++;
            _version++;
        }

        /// <summary>
        /// Adds an element to the end of the list
        /// </summary>
        /// <param name="item">Element to add</param>
        public void Add(T item)
        {
            this.AddLast(item);
        }

        /// <summary>
        /// Removes an element at the begining of the circular list
        /// </summary>
        /// <returns>The element at the beginning of the list</returns>
        public T RemoveFirst()
        {
            TurboContract.Requires(this.Count > 0, conditionString: "this.Count > 0");
            TurboContract.Ensures(this.Count == TurboContract.OldValue(this.Count) - 1);

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
        /// Removes an element at the ending of the circular list
        /// </summary>
        /// <returns>The element at the ending of the list</returns>
        public T RemoveLast()
        {
            TurboContract.Requires(this.Count > 0, conditionString: "this.Count > 0");
            TurboContract.Ensures(this.Count == TurboContract.OldValue(this.Count) - 1);

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
        /// Removes the item at the specified index
        /// </summary>
        /// <param name="index">Index of item to remove</param>
        public void RemoveAt(int index)
        {
            TurboContract.Ensures(this.Count == TurboContract.OldValue(this.Count) - 1);

            if (index < 0 || index >= _size)
                throw new ArgumentOutOfRangeException(nameof(index));

            bool moveFromBeginning = index < this.Count / 2;
            int removePos = (index + _head) % _elemArray.Length;

            if (moveFromBeginning)
            {
                int newHead = (_head + 1) % _elemArray.Length;

                if (index > 0)
                {
                    if (_head + index < _elemArray.Length)
                    {
                        Array.Copy(_elemArray, _head, _elemArray, _head + 1, index);
                    }
                    else
                    {
                        if (removePos != 0)
                            Array.Copy(_elemArray, 0, _elemArray, 1, removePos);
                        _elemArray[0] = _elemArray[_elemArray.Length - 1];
                        Array.Copy(_elemArray, _head, _elemArray, _head + 1, _elemArray.Length - _head - 1);
                    }
                }

                _elemArray[_head] = default(T);
                _head = newHead;
            }
            else
            {
                int newTail = (_tail - 1 + _elemArray.Length) % _elemArray.Length;

                if (index < _size - 1)
                {
                    if (removePos <= _tail)
                    {
                        Array.Copy(_elemArray, removePos + 1, _elemArray, removePos, _tail - removePos);
                    }
                    else
                    {
                        if (removePos != _elemArray.Length - 1)
                            Array.Copy(_elemArray, removePos + 1, _elemArray, removePos, _elemArray.Length - removePos - 1);
                        _elemArray[_elemArray.Length - 1] = _elemArray[0];
                        Array.Copy(_elemArray, 1, _elemArray, 0, _tail);
                    }
                }

                _elemArray[newTail] = default(T);
                _tail = newTail;
            }

            _size--;
            _version++;
        }

        /// <summary>
        /// Removes the first occurrence of a specific object from the circular list
        /// </summary>
        /// <param name="item">The object to remove from the list</param>
        /// <returns>True if 'item' is successfully removed; otherwise, false</returns>
        public bool Remove(T item)
        {
            int index = this.IndexOf(item);
            if (index >= 0)
            {
                this.RemoveAt(index);
                return true;
            }
            return false;
        }


        /// <summary>
        /// Removes all elements from the list
        /// </summary>
        public void Clear()
        {
            TurboContract.Ensures(this.Count == 0);

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
        /// Sets the capacity to the actual number of elements in the list, if that number is less than 90 percent of current capacity
        /// </summary>
        public void TrimExcess()
        {
            TurboContract.Ensures(this.Count == TurboContract.OldValue(this.Count));

            int minSize = (int)((double)_elemArray.Length * ShrinkRate);
            if (_size < minSize)
                this.SetCapacity(0, _size);
        }


        /// <summary>
        /// Ensures that the list has enough space
        /// </summary>
        /// <param name="headOffset">Desired head offset on the grow</param>
        /// <param name="capacity">Desired capacity</param>
        private void EnsureCapacity(int headOffset, int capacity)
        {
            if (capacity < 0)
                throw new OverflowException("Capacity overflowed. Collection is too large");

            if (this._elemArray.Length < capacity)
            {
                int newSize = _elemArray.Length * GrowFactor;
                if (newSize < _elemArray.Length + MinimumGrow)
                    newSize = _elemArray.Length + MinimumGrow;
                if (newSize < capacity)
                    newSize = capacity;
                this.SetCapacity(headOffset, newSize);
            }
        }

        /// <summary>
        /// Sets the capacity of the list (resize internal array)
        /// </summary>
        /// <param name="headOffset">Desired head offset</param>
        /// <param name="capacity">Desired capacity</param>
        private void SetCapacity(int headOffset, int capacity)
        {
            TurboContract.Requires(capacity >= this.Count, conditionString: "capacity >= this.Count");

            if (headOffset < 0)
                headOffset = (capacity - _size) / 2;
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
            _tail = _elemArray.Length > 0 ? ((_head + _size) % _elemArray.Length) : 0;
            TurboContract.Assert(_tail == ((_size == capacity) ? 0 : this._size + headOffset), conditionString: "_tail == ((_size == capacity) ? 0 : this._size + headOffset)");

            _version++;
        }


        /// <summary>
        /// Returns an Enumerator
        /// </summary>
        /// <returns>Enumerator</returns>
        public CircularList<T>.Enumerator GetEnumerator()
        {
            return new CircularList<T>.Enumerator(this);
        }


        // ==================== interfaces ===================

        /// <summary>
        /// Returns an Enumerator
        /// </summary>
        /// <returns>Enumerator</returns>
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new CircularList<T>.Enumerator(this);
        }
        /// <summary>
        /// Returns an Enumerator
        /// </summary>
        /// <returns>Enumerator</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return new CircularList<T>.Enumerator(this);
        }

        /// <summary>
        /// Copies the list elements to an existing array, starting at the specified array index
        /// </summary>
        /// <param name="array">Destination array</param>
        /// <param name="index">Starting index</param>
        void ICollection.CopyTo(Array array, int index)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (array.Rank != 1)
                throw new ArgumentException("Array rank not equal to 1", nameof(array));
            if (index < 0 || index > array.Length)
                throw new ArgumentOutOfRangeException(nameof(index), "index < 0 || index > array.Length");
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
                    System.Threading.Interlocked.CompareExchange<object>(ref this._syncRoot, new object(), null);

                return this._syncRoot;
            }
        }

        /// <summary>
        /// Is read-only collection
        /// </summary>
        bool ICollection<T>.IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// Is read-only collection
        /// </summary>
        bool IList.IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// Is fixed size collection
        /// </summary>
        bool IList.IsFixedSize
        {
            get { return false; }
        }


        /// <summary>
        /// Adds an item to the list
        /// </summary>
        /// <param name="value">New item</param>
        int IList.Add(object value)
        {
            if (value == null && default(T) != null)
                throw new ArgumentNullException(nameof(value));

            try
            {
                this.Add((T)((object)value));
            }
            catch (InvalidCastException)
            {
                throw new ArgumentException("Value has a wrong type. Expected type: " + typeof(T).ToString(), nameof(value));
            }
            return this.Count - 1;
        }

        /// <summary>
        /// Determines whether an element is in the List
        /// </summary>
        /// <param name="value">The object to locate in the List</param>
        /// <returns>True if item is found</returns>
        bool IList.Contains(object value)
        {
            return CircularList<T>.IsCompatibleObject(value) && this.Contains((T)((object)value));
        }

        /// <summary>
        /// Searches for the specified 'item' and returns the index of the first occurrence of the item inside list
        /// </summary>
        /// <param name="value">The item to locate inside the list</param>
        /// <returns>The index of element inside the list, if found. -1 otherwise</returns>
        int IList.IndexOf(object value)
        {
            if (CircularList<T>.IsCompatibleObject(value))
            {
                return this.IndexOf((T)((object)value));
            }
            return -1;
        }

        /// <summary>
        /// Inserts an item to the list
        /// </summary>
        /// <param name="index">Index</param>
        /// <param name="value">New element</param>
        void IList.Insert(int index, object value)
        {
            if (value == null && default(T) != null)
                throw new ArgumentNullException(nameof(value));

            try
            {
                this.Insert(index, (T)((object)value));
            }
            catch (InvalidCastException)
            {
                throw new ArgumentException("Value has a wrong type. Expected type: " + typeof(T).ToString(), nameof(value));
            }
        }

        /// <summary>
        /// Removes the item at the specified index
        /// </summary>
        /// <param name="index">Index of item to remove</param>
        void IList.RemoveAt(int index)
        {
            this.RemoveAt(index);
        }

        /// <summary>
        /// Removes the first occurrence of a specific item from the list
        /// </summary>
        /// <param name="value">Item</param>
        void IList.Remove(object value)
        {
            if (CircularList<T>.IsCompatibleObject(value))
            {
                this.Remove((T)((object)value));
            }
        }

        /// <summary>
        /// Gets or sets element at specified index
        /// </summary>
        /// <param name="index">Index</param>
        /// <returns>Element</returns>
        object IList.this[int index]
        {
            get
            {
                return this[index];
            }
            set
            {
                if (value == null && default(T) != null)
                    throw new ArgumentNullException(nameof(value));

                try
                {
                    this[index] = (T)((object)value);
                }
                catch (InvalidCastException)
                {
                    throw new ArgumentException("Value has a wrong type. Expected type: " + typeof(T).ToString(), nameof(value));
                }
            }
        }


        /// <summary>
        /// Gets element of the list at specified index
        /// </summary>
        /// <param name="index">Element index</param>
        /// <returns>Element of the list</returns>
        T IReadOnlyList<T>.this[int index]
        {
            get { return this[index]; }
        }
    }
}
