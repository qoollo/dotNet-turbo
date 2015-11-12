using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Collections
{
    /// <summary>
    /// Simple array based container that automatically grows if the index is outside the current array bounds.
    /// Track items explicitly. 
    /// </summary>
    /// <typeparam name="T">The type of the elements in the ArrayBasedContainer</typeparam>
    internal class ArrayBasedContainerVal<T> : IEnumerable<T>
    {
        /// <summary>
        /// Item container
        /// </summary>
        private struct ItemContainer
        {
            public ItemContainer(T element)
            {
                Element = element;
                HasElement = true;
            }

            public T Element;
            public bool HasElement;
        }

        // ==============

        /// <summary>
        /// ArrayBasedContainerVal enumerator
        /// </summary>
        public struct Enumerator : IEnumerator<T>, System.Collections.IEnumerator
        {
            private readonly ItemContainer[] _capturedData;
            private int _currentIndex;
            private T _currentData;

            /// <summary>
            /// Enumerator constructor
            /// </summary>
            /// <param name="container">ArrayBasedContainerVal</param>
            public Enumerator(ArrayBasedContainerVal<T> container)
            {
                Contract.Requires<ArgumentNullException>(container != null);

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
            object System.Collections.IEnumerator.Current
            {
                get { return _currentData; }
            }
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
                    var itemData = _capturedData[_currentIndex];
                    if (itemData.HasElement)
                    {
                        _currentData = itemData.Element;
                        return true;
                    }

                    _currentIndex++;
                }

                return false;
            }
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

        private readonly object _lockObject = new object();
        private ItemContainer[] _data;

        /// <summary>
        /// ArrayBasedContainerVal constructor
        /// </summary>
        /// <param name="initialSize">Initial size</param>
        public ArrayBasedContainerVal(int initialSize)
        {
            Contract.Requires<ArgumentException>(initialSize >= 0);

            _data = new ItemContainer[initialSize];
        }
        /// <summary>
        /// ArrayBasedContainerVal constructor
        /// </summary>
        public ArrayBasedContainerVal()
            : this(0)
        {
        }

        /// <summary>
        /// Removes all items from the container and sets size to zero
        /// </summary>
        public void Clear()
        {
            lock (_lockObject)
            {
                _data = new ItemContainer[0];
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
            Contract.Requires<ArgumentOutOfRangeException>(index >= 0);

            lock (_lockObject)
            {
                if (index >= _data.Length)
                    Grow(index + 1);

                _data[index].Element = value;
                System.Threading.Volatile.Write(ref _data[index].HasElement, true);
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
            Contract.Requires<ArgumentOutOfRangeException>(index >= 0);

            lock (_lockObject)
            {
                if (index >= _data.Length)
                    Grow(index + 1);

                if (_data[index].HasElement)
                    return false;

                _data[index].Element = value;
                System.Threading.Volatile.Write(ref _data[index].HasElement, true);
                return true;
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

            if (localData[index].HasElement)
            {
                lock (_lockObject)
                {
                    localData[index].HasElement = false;
                    System.Threading.Thread.MemoryBarrier();
                    localData[index].Element = default(T);
                    return true;
                }
            }
            return false;
        }


        /// <summary>
        /// Gets an item at the specified index. Returns 'null' if item is not a part of the container
        /// </summary>
        /// <param name="index">Index</param>
        /// <returns>Value at the specified 'index'</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetItemOrDefault(int index)
        {
            var localData = _data;
            if (index < 0 || index >= localData.Length)
                return default(T);

            return localData[index].Element;
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
        /// Gets an item at the specified index
        /// </summary>
        /// <param name="index">Index</param>
        /// <returns>Value at the specified 'index'</returns>
        /// <exception cref="IndexOutOfRangeException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetItem(int index)
        {
            var localData = _data;
            if (index < 0 || index >= localData.Length)
                ThrowIndexOutOfRangeException(index);

            var result = localData[index];
            if (!result.HasElement)
                ThrowIndexOutOfRangeException(index);

            return result.Element;
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
                item = default(T);
                return false;
            }

            var itemData = localData[index];
            item = itemData.Element;
            return itemData.HasElement;
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

            return localData[index].HasElement;
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
                if (item.HasElement)
                    yield return new KeyValuePair<int, T>(i, item.Element);
            }
        }
    }
}
