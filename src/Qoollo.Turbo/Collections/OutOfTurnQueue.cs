using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Collections
{
    /// <summary>
    /// Queue with possibility to add elements to the head
    /// </summary>
    /// <typeparam name="T">The type of elements in the queue</typeparam>
    [System.Diagnostics.DebuggerDisplay("Count = {Count}")]
    [Serializable]
    public class OutOfTurnQueue<T> : IEnumerable<T>, IReadOnlyCollection<T>, ICollection, IEnumerable
    {
        private readonly CircularList<T> _circularList;

        /// <summary>
        /// Code contracts
        /// </summary>
        [ContractInvariantMethod]
        private void Invariant()
        {
            Contract.Invariant(_circularList != null);
        }

        /// <summary>
        /// OutOfTurnQueue constructor
        /// </summary>
        public OutOfTurnQueue()
        {
            _circularList = new CircularList<T>();
        }

        /// <summary>
        /// OutOfTurnQueue constructor
        /// </summary>
        /// <param name="capacity">Initial capacity</param>
        public OutOfTurnQueue(int capacity)
        {
            Contract.Requires<ArgumentException>(capacity >= 0);

            _circularList = new CircularList<T>(capacity);
        }

        /// <summary>
        /// OutOfTurnQueue constructor
        /// </summary>
        /// <param name="collection">The collection whose elements are copied to the new queue</param>
        public OutOfTurnQueue(IEnumerable<T> collection)
        {
            Contract.Requires<ArgumentNullException>(collection != null);

            _circularList = new CircularList<T>(collection);
        }

        /// <summary>
        /// Gets the number of elements in the queue
        /// </summary>
        public int Count
        {
            get { return _circularList.Count; }
        }

        /// <summary>
        /// Gets the capacity of the queue
        /// </summary>
        public int Capacity
        {
            get { return _circularList.Capacity; }
        }

        /// <summary>
        /// Removes all elements from the queue
        /// </summary>
        public void Clear()
        {
            _circularList.Clear();
        }

        /// <summary>
        /// Copies the queue elements to an existing array, starting at the specified array index
        /// </summary>
        /// <param name="array">Destination array</param>
        /// <param name="index">Starting index</param>
        public void CopyTo(T[] array, int index)
        {
            Contract.Requires(array != null);
            Contract.Requires(index >= 0);
            Contract.Requires(index <= array.Length - this.Count);

            _circularList.CopyTo(array, index);
        }

        /// <summary>
        /// Adds an object to the tail of the queue
        /// </summary>
        /// <param name="item">The item to add to the queue</param>
        public void Enqueue(T item)
        {
            _circularList.AddLast(item);
        }


        /// <summary>
        /// Adds an object to the head of the queue
        /// </summary>
        /// <param name="item">The item to add to the queue</param>
        public void EnqueueFirst(T item)
        {
            _circularList.AddFirst(item);
        }
        /// <summary>
        /// Adds an object to the head of the queue
        /// </summary>
        /// <param name="item">The item to add to the queue</param>
        [Obsolete("Method was renamed. Consider to use 'EnqueueFirst' instead", true)]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public void EnqueueToFront(T item)
        {
            _circularList.AddFirst(item);
        }

        /// <summary>
        /// Returns the item at the head of the queue without removing it
        /// </summary>
        /// <returns>The item at the head of the queue</returns>
        public T Peek()
        {
            if (_circularList.Count == 0)
                throw new InvalidOperationException("Collection is empty");

            return _circularList[0];
        }

        /// <summary>
        /// Removes and returns the item at the head of the queue
        /// </summary>
        /// <returns>The item that is removed from the head of the queue</returns>
        public T Dequeue()
        {
            Contract.Requires(this.Count > 0);

            return _circularList.RemoveFirst();
        }

        /// <summary>
        /// Determines whether an element is in the queue
        /// </summary>
        /// <param name="item">The element to locate</param>
        /// <returns>True if the item is found</returns>
        [Pure]
        public bool Contains(T item)
        {
            return _circularList.Contains(item);
        }

        /// <summary>
        /// Copies the queue elements to a new array
        /// </summary>
        /// <returns>A new array containing elements copied from the queue</returns>
        public T[] ToArray()
        {
            Contract.Ensures(Contract.Result<T[]>() != null);

            return _circularList.ToArray();
        }

        /// <summary>
        /// Sets the capacity to the actual number of elements in the queue
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

        /// <summary>
        /// Returns an Enumerator
        /// </summary>
        /// <returns>Enumerator</returns>
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return _circularList.GetEnumerator();
        }

        /// <summary>
        /// Returns an Enumerator
        /// </summary>
        /// <returns>Enumerator</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _circularList.GetEnumerator();
        }

        /// <summary>
        /// Copies the queue elements to an Array, starting at the specified array index
        /// </summary>
        /// <param name="array">Destination array</param>
        /// <param name="index">Index in array at which copying begins</param>
        void ICollection.CopyTo(Array array, int index)
        {
            (_circularList as ICollection).CopyTo(array, index);
        }

        /// <summary>
        /// Gets the number of elements contained in the queue
        /// </summary>
        int ICollection.Count
        {
            get { return _circularList.Count; }
        }

        /// <summary>
        /// Is collection synchronized
        /// </summary>
        bool ICollection.IsSynchronized
        {
            get { return (_circularList as ICollection).IsSynchronized; }
        }

        /// <summary>
        /// Synchronization object
        /// </summary>
        object ICollection.SyncRoot
        {
            get { return (_circularList as ICollection).SyncRoot; }
        }

        /// <summary>
        /// Gets the number of elements contained in the queue
        /// </summary>
        int IReadOnlyCollection<T>.Count
        {
            get { return _circularList.Count; }
        }
    }
}
