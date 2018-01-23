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
        private readonly CircularList<T> _circularList;
		
        /// <summary>
        /// Deque constructor
        /// </summary>
		public Deque()
		{
            _circularList = new CircularList<T>();
		}

        /// <summary>
        /// Deque constructor with specified initial capacity
        /// </summary>
        /// <param name="capacity">Initial capacity</param>
		public Deque(int capacity)
		{
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), "capacity cannot be negative");

            _circularList = new CircularList<T>(capacity);
		}

        /// <summary>
        /// Deque constructor
        /// </summary>
        /// <param name="collection">The collection whose elements are copied to the new deque</param>
        public Deque(IEnumerable<T> collection)
		{
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));

            _circularList = new CircularList<T>(collection);
		}

        /// <summary>
        /// Gets the number of elements in the deque
        /// </summary>
        public int Count
        {
            get { return _circularList.Count; }
        }

        /// <summary>
        /// Gets the capacity of the deque
        /// </summary>
        public int Capacity
        {
            get { return _circularList.Capacity; }
        }

        /// <summary>
        /// Get element at specified index from the begining of the deque
        /// </summary>
        /// <param name="i">Index of the element</param>
        /// <returns>Element at specified index</returns>
        [Pure]
        internal T GetElement(int i)
        {
            TurboContract.Requires(i >= 0, conditionString: "i >= 0");
            TurboContract.Requires(i <= this.Count, conditionString: "i <= this.Count");

            return _circularList[i];
        }

        /// <summary>
        /// Removes all elements from the deque
        /// </summary>
		public void Clear()
		{
            _circularList.Clear();
		}

        /// <summary>
        /// Copies the deque elements to an existing array, starting at the specified array index.
        /// </summary>
        /// <param name="array">Destination array</param>
        /// <param name="index">Starting index</param>
		public void CopyTo(T[] array, int index)
		{
            _circularList.CopyTo(array, index);
		}


        /// <summary>
        /// Adds an element to the end of the deque
        /// </summary>
        /// <param name="item">Element to add</param>
		public void AddLast(T item)
		{
            _circularList.AddLast(item);
		}
        /// <summary>
        /// Adds an element to the end of the deque
        /// </summary>
        /// <param name="item">Element to add</param>
        [Obsolete("Method was renamed. Consider to use 'AddLast' instead", true)]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public void AddToBack(T item)
        {
            _circularList.AddLast(item);
        }

        /// <summary>
        /// Adds an element to the begining of the deque
        /// </summary>
        /// <param name="item">Element to add</param>
        public void AddFirst(T item)
        {
            _circularList.AddFirst(item);
        }
        /// <summary>
        /// Adds an element to the begining of the deque
        /// </summary>
        /// <param name="item">Element to add</param>
        [Obsolete("Method was renamed. Consider to use 'AddFirst' instead", true)]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public void AddToFront(T item)
        {
            _circularList.AddFirst(item);
        }

        /// <summary>
        /// Returns the element at the beginning of the deque without removing it
        /// </summary>
        /// <returns>The element at the beginning of the deque</returns>
        [Pure]
        public T PeekFirst()
        {
            TurboContract.Requires(this.Count > 0, conditionString: "this.Count > 0");
            if (_circularList.Count == 0)
                throw new InvalidOperationException("Collection is empty");

            return _circularList[0];
        }
        /// <summary>
        /// Returns the element at the beginning of the deque without removing it
        /// </summary>
        /// <returns>The element at the beginning of the deque</returns>
        [Pure]
        [Obsolete("Method was renamed. Consider to use 'PeekFirst' instead", true)]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public T PeekAtFront()
        {
            return PeekFirst();
        }


        /// <summary>
        /// Returns the element at the ending of the deque without removing it
        /// </summary>
        /// <returns>The element at the ending of the deque</returns>
        [Pure]
        public T PeekLast()
        {
            TurboContract.Requires(this.Count > 0, conditionString: "this.Count > 0");
            if (_circularList.Count == 0)
                throw new InvalidOperationException("Collection is empty");

            return _circularList[_circularList.Count - 1];
        }
        /// <summary>
        /// Returns the element at the ending of the deque without removing it
        /// </summary>
        /// <returns>The element at the ending of the deque</returns>
        [Pure]
        [Obsolete("Method was renamed. Consider to use 'PeekLast' instead", true)]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public T PeekAtEnd()
        {
            return PeekLast();
        }


        /// <summary>
        /// Removes an element at the begining of the deque
        /// </summary>
        /// <returns>The element at the beginning of the deque</returns>
        public T RemoveFirst()
        {
            return _circularList.RemoveFirst();
        }
        /// <summary>
        /// Removes an element at the begining of the deque
        /// </summary>
        /// <returns>The element at the beginning of the deque</returns>
        [Obsolete("Method was renamed. Consider to use 'RemoveFirst' instead", true)]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public T RemoveFromFront()
		{
            return _circularList.RemoveFirst();
		}

        /// <summary>
        /// Removes an element at the ending of the deque
        /// </summary>
        /// <returns>The element at the ending of the deque</returns>
        public T RemoveLast()
        {
            return _circularList.RemoveLast();
        }
        /// <summary>
        /// Removes an element at the ending of the deque
        /// </summary>
        /// <returns>The element at the ending of the deque</returns>
        [Obsolete("Method was renamed. Consider to use 'RemoveLast' instead", true)]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public T RemoveFromBack()
        {
            return _circularList.RemoveLast();
        }

        /// <summary>
        /// Determines whether an element is in the deque
        /// </summary>
        /// <param name="item">The element to locate</param>
        /// <returns>True if the item is found</returns>
        [Pure]
		public bool Contains(T item)
		{
            return _circularList.Contains(item);
		}

        /// <summary>
        /// Copies the deque elements to a new array
        /// </summary>
        /// <returns>A new array containing elements copied from the deque</returns>
		public T[] ToArray()
		{
            return _circularList.ToArray();
		}

        /// <summary>
        /// Sets the capacity to the actual number of elements in the deque, if that number is less than 90 percent of current capacity.
        /// </summary>
        public void TrimExcess()
        {
            _circularList.TrimExcess();
        }


        /// <summary>
        /// Returns an Enumerator
        /// </summary>
        /// <returns>Enumerator</returns>
        public CircularList<T>.Enumerator GetEnumerator()
        {
            return _circularList.GetEnumerator();
        }

        // =============== Interfaces ================

        /// <summary>
        /// Returns an Enumerator
        /// </summary>
        /// <returns>Enumerator</returns>
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return this.GetEnumerator();
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
        /// Copies the deque elements to an existing array, starting at the specified array index.
        /// </summary>
        /// <param name="array">Destination array</param>
        /// <param name="index">Starting index</param>
        void ICollection.CopyTo(Array array, int index)
        {
            ((ICollection)_circularList).CopyTo(array, index);
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
            get { return ((ICollection)_circularList).SyncRoot; }
        }

        /// <summary>
        /// Gets the number of elements in the deque
        /// </summary>
        int ICollection.Count
        {
            get { return this.Count; }
        }

        /// <summary>
        /// Gets the number of elements in the deque
        /// </summary>
        int IReadOnlyCollection<T>.Count
        {
            get { return this.Count; }
        }
    }
}
