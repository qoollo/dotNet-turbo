using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Collections
{
    /// <summary>
    /// Simple array based container that automatically grows if the index is outside the current array bounds.
    /// Works only with reference types. Null is not valid value.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the ArrayBasedContainer</typeparam>
    public class IndexedContainerRef<T> : IEnumerable<T> where T : class
    {
        /// <summary>
        /// IndexedContainerRef enumerator
        /// </summary>
        public struct Enumerator : IEnumerator<T>, System.Collections.IEnumerator
        {
            private readonly T[] _capturedData;
            private int _currentIndex;
            private T _currentData;

            /// <summary>
            /// Enumerator constructor
            /// </summary>
            /// <param name="container">IndexedContainerRef</param>
            public Enumerator(IndexedContainerRef<T> container)
            {
                if (container == null)
                    throw new ArgumentNullException(nameof(container));

                _capturedData = container._data;
                _currentIndex = -1;
                _currentData = default(T);
            }
            /// <summary>
            /// Gets the element at the current position of the enumerator
            /// </summary>
            public T Current
            {
                get { return _currentData; }
            }
            /// <summary>
            /// Gets the element at the current position of the enumerator
            /// </summary>
            object System.Collections.IEnumerator.Current
            {
                get { return _currentData; }
            }
            /// <summary>
            /// Gets the element at the current position of the enumerator
            /// </summary>
            T IEnumerator<T>.Current
            {
                get { return _currentData; }
            }

            /// <summary>
            /// Advances the enumerator to the next element
            /// </summary>
            /// <returns></returns>
            public bool MoveNext()
            {
                _currentIndex++;
                while (_currentIndex < _capturedData.Length)
                {
                    var item = _capturedData[_currentIndex];
                    if (item != null)
                    {
                        _currentData = item;
                        return true;
                    }

                    _currentIndex++;
                }

                return false;
            }
            /// <summary>
            /// Advances the enumerator to the next element
            /// </summary>
            /// <returns></returns>
            bool System.Collections.IEnumerator.MoveNext()
            {
                return MoveNext();
            }
            /// <summary>
            /// Sets the enumerator to its initial position
            /// </summary>
            public void Reset()
            {
                _currentIndex = -1;
                _currentData = default(T);
            }
            /// <summary>
            /// Clean-up resources
            /// </summary>
            public void Dispose()
            {
                _currentData = default(T);
            }
        }

        // ==============

        private static readonly T[] _emptyArray = new T[0];

        private readonly object _lockObject = new object();
        private T[] _data;

        /// <summary>
        /// IndexedContainerRef constructor
        /// </summary>
        /// <param name="initialSize">Initial size</param>
        public IndexedContainerRef(int initialSize)
        {
            if (initialSize < 0)
                throw new ArgumentOutOfRangeException(nameof(initialSize), "initialSize cannot be negative");

            _data = new T[initialSize];
        }
        /// <summary>
        /// IndexedContainerRef constructor
        /// </summary>
        public IndexedContainerRef()
        {
            _data = _emptyArray;
        }

        /// <summary>
        /// Gets the capacity of the container
        /// </summary>
        public int Capacity { get { return _data.Length; } }
        /// <summary>
        /// Gets the internal data array
        /// </summary>
        protected T[] DataArray { get { return _data; } }

        /// <summary>
        /// Removes all items from the container and sets size to zero
        /// </summary>
        public void Clear()
        {
            lock (_lockObject)
            {
                _data = _emptyArray;
            }
        }

        /// <summary>
        /// Increase size
        /// </summary>
        /// <param name="expectedSize">Expected size</param>
        private void Grow(int expectedSize)
        {
            Array.Resize(ref _data, Math.Max(_data.Length * 2 + 2, expectedSize));
        }

        /// <summary>
        /// Sets the value at the specified index
        /// </summary>
        /// <param name="index">Index</param>
        /// <param name="value">Value to set at the 'index'</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void SetItem(int index, T value)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index), "index cannot be negative");

            lock (_lockObject)
            {
                if (index >= _data.Length)
                    Grow(index + 1);

                System.Threading.Volatile.Write(ref _data[index], value);
            }
        }
        /// <summary>
        /// Attempts to set the value at the specified index
        /// </summary>
        /// <param name="index">Index</param>
        /// <param name="value">Value to set at the 'index'</param>
        /// <returns>True if the item was not existed</returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public bool TrySetItem(int index, T value)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index), "index cannot be negative");

            lock (_lockObject)
            {
                if (index >= _data.Length)
                    Grow(index + 1);

                return System.Threading.Interlocked.CompareExchange(ref _data[index], value, null) == null;
            }
        }
        /// <summary>
        /// Removes item at the specified index
        /// </summary>
        /// <param name="index">Index</param>
        /// <returns>True if 'item' is successfully removed; otherwise, false</returns>
        public bool RemoveItem(int index)
        {
            var localData = _data;
            if (index < 0 || index >= localData.Length)
                return false;

            return System.Threading.Interlocked.Exchange(ref localData[index], null) != null;
        }


        /// <summary>
        /// Gets an item at the specified index. Returns 'null' if the index is outside the array
        /// </summary>
        /// <param name="index">Index</param>
        /// <returns>Value at the specified 'index'</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetItemOrDefault(int index)
        {
            var localData = _data;
            if (index < 0 || index >= localData.Length)
                return null;

            return localData[index];
        }

        /// <summary>
        /// Throws IndexOutOfRangeException
        /// </summary>
        /// <param name="index"></param>
        private static void ThrowIndexOutOfRangeException(int index)
        {
            throw new IndexOutOfRangeException("Container does not contains item with index = " + index.ToString());
        }
        /// <summary>
        /// Throws ItemNotFoundException
        /// </summary>
        /// <param name="index"></param>
        private static void ThrowItemNotFoundException(int index)
        {
            throw new ItemNotFoundException("Container does not contains item with index = " + index.ToString());
        }
        /// <summary>
        /// Gets an item at the specified index
        /// </summary>
        /// <param name="index">Index</param>
        /// <returns>Value at the specified 'index'</returns>
        /// <exception cref="ItemNotFoundException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetItem(int index)
        {
            var localData = _data;
            if (index < 0 || index >= localData.Length)
                ThrowItemNotFoundException(index);

            var result = localData[index];
            if (result == null)
                ThrowItemNotFoundException(index);

            return result;
        }
        /// <summary>
        /// Attempts to get an item at the specified index
        /// </summary>
        /// <param name="index">Index</param>
        /// <param name="item">Value at the specified 'index'</param>
        /// <returns>True if the item is presented in the container</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetItem(int index, out T item)
        {
            var localData = _data;
            if (index < 0 || index >= localData.Length)
            {
                item = null;
                return false;
            }

            item = localData[index];
            return item != null;
        }
        /// <summary>
        /// Determines whether the container has an item at the specified index
        /// </summary>
        /// <param name="index">Index</param>
        /// <returns>True if the item is presented in the container</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasItem(int index)
        {
            var localData = _data;
            if (index < 0 || index >= localData.Length)
                return false;

            return localData[index] != null;
        }


        /// <summary>
        /// Returns an Enumerator
        /// </summary>
        /// <returns>Enumerator</returns>
        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }
        /// <summary>
        /// Returns an Enumerator
        /// </summary>
        /// <returns>Enumerator</returns>
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new Enumerator(this);
        }
        /// <summary>
        /// Returns an Enumerator
        /// </summary>
        /// <returns>Enumerator</returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }


        /// <summary>
        /// Enumerate the container with indexes
        /// </summary>
        /// <returns>Enumerable object</returns>
        public IEnumerable<KeyValuePair<int, T>> EnumerateWithKeys()
        {
            var localData = _data;
            for (int i = 0; i < localData.Length; i++)
            {
                var item = localData[i];
                if (item != null)
                    yield return new KeyValuePair<int, T>(i, item);
            }
        }
    }
}
