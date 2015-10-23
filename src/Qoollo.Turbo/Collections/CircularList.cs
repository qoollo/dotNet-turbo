using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Collections
{
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
                Contract.Requires(srcList != null);
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
        /// <typeparam name="T">The type to which cast checked</typeparam>
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
            Contract.Requires<ArgumentOutOfRangeException>(capacity >= 0);

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
                    _elemArray = new T[notStrictCol.Count];
                else
                    _elemArray = new T[DefaultCapacity];
                _size = 0;
                _version = 0;
                _head = 0;
                _tail = 0;

                //foreach (var elem in collection)
                //    AddToBack(elem);
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
            int curPos = this._head;
            int restCount = this._size;

            if (item == null)
            {
                while (restCount-- > 0)
                {
                    if (this._elemArray[curPos] == null)
                        return true;

                    curPos = (curPos + 1) % this._elemArray.Length;
                }

                return false;
            }


            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            while (restCount-- > 0)
            {
                if (this._elemArray[curPos] != null && comparer.Equals(this._elemArray[curPos], item))
                    return true;

                curPos = (curPos + 1) % this._elemArray.Length;
            }
            return false;
        }


        /// <summary>
        /// Copies the list elements to an existing array, starting at the specified array index
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
        /// Copies the list elements to a new array
        /// </summary>
        /// <returns>A new array containing elements copied from the list</returns>
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


        public int IndexOf(T item)
        {
            throw new NotImplementedException();
        }

        public void Insert(int index, T item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        public void Add(T item)
        {
            throw new NotImplementedException();
        }

        public bool Remove(T item)
        {
            throw new NotImplementedException();
        }





        /// <summary>
        /// Removes all elements from the list
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
        /// Sets the capacity to the actual number of elements in the list, if that number is less than 90 percent of current capacity
        /// </summary>
        public void TrimExcess()
        {
            Contract.Ensures(this.Count == Contract.OldValue(this.Count));

            int minSize = (int)((double)_elemArray.Length * ShrinkRate);
            if (_size < minSize)
                this.SetCapacity(0, _size);
        }

        /// <summary>
        /// Sets the capacity of the list (resize internal array)
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
                throw new ArgumentNullException("value");

            try
            {
                this.Add((T)((object)value));
            }
            catch (InvalidCastException)
            {
                throw new ArgumentException("Value has a wrong type. Expected type: " + typeof(T).ToString(), "value");
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
                throw new ArgumentNullException("value");

            try
            {
                this.Insert(index, (T)((object)value));
            }
            catch (InvalidCastException)
            {
                throw new ArgumentException("Value has a wrong type. Expected type: " + typeof(T).ToString(), "value");
            }
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
                    throw new ArgumentNullException("value");

                try
                {
                    this[index] = (T)((object)value);
                }
                catch (InvalidCastException)
                {
                    throw new ArgumentException("Value has a wrong type. Expected type: " + typeof(T).ToString(), "value");
                }
            }
        }
    }
}
