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
    /// Simple thread-safe array based container that automatically grows if the index is outside the current array bounds
    /// </summary>
    /// <typeparam name="T">The type of the elements in the ArrayBasedContainer</typeparam>
    public class ArrayBasedContainer<T>
    {
        private readonly object _lockObject = new object();
        private T[] _data;

        /// <summary>
        /// ArrayBasedContainer constructor
        /// </summary>
        /// <param name="initialSize">Initial size</param>
        public ArrayBasedContainer(int initialSize)
        {
            Contract.Requires<ArgumentException>(initialSize >= 0);

            _data = new T[initialSize];
        }
        /// <summary>
        /// ArrayBasedContainer constructor
        /// </summary>
        public ArrayBasedContainer()
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
                _data = new T[0];
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

                _data[index] = value;
            }
        }

        /// <summary>
        /// Gets the value at the specified index. Returns default(T) if value is not a part of the container.
        /// </summary>
        /// <param name="index">Index</param>
        /// <returns>Value at the specified 'index'</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetItem(int index)
        {
            var localData = _data;
            if (index < 0 || index >= localData.Length)
                return default(T);

            return localData[index];
        }
    }
}
