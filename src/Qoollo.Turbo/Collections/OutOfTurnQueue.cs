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
        private readonly Deque<T> _deque;

        /// <summary>
        /// Code contracts
        /// </summary>
        [ContractInvariantMethod]
        private void Invariant()
        {
            Contract.Invariant(_deque != null);
        }

        /// <summary>
        /// OutOfTurnQueue constructor
        /// </summary>
        public OutOfTurnQueue()
        {
            _deque = new Deque<T>();
        }

        /// <summary>
        /// OutOfTurnQueue constructor
        /// </summary>
        /// <param name="capacity">Initial capacity</param>
        public OutOfTurnQueue(int capacity)
        {
            Contract.Requires(capacity >= 0);

            _deque = new Deque<T>(capacity);
        }

        /// <summary>
        /// OutOfTurnQueue constructor
        /// </summary>
        /// <param name="collection">The collection whose elements are copied to the new queue</param>
        public OutOfTurnQueue(IEnumerable<T> collection)
        {
            Contract.Requires(collection != null);

            _deque = new Deque<T>(collection);
        }

        /// <summary>
        /// Gets the number of elements in the queue
        /// </summary>
        public int Count
        {
            get { return _deque.Count; }
        }

        /// <summary>
        /// Gets the capacity of the queue
        /// </summary>
        public int Capacity
        {
            get { return _deque.Capacity; }
        }

        /// <summary>
        /// Removes all elements from the queue
        /// </summary>
        public void Clear()
        {
            _deque.Clear();
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

            _deque.CopyTo(array, index);
        }

        /// <summary>
        /// Adds an object to the tail of the queue
        /// </summary>
        /// <param name="item">The item to add to the queue</param>
        public void Enqueue(T item)
        {
            _deque.AddToBack(item);
        }

        /// <summary>
        /// Adds an object to the head of the queue
        /// </summary>
        /// <param name="item">The item to add to the queue</param>
        public void EnqueueToFront(T item)
        {
            _deque.AddToFront(item);
        }

        /// <summary>
        /// Returns the item at the head of the queue without removing it
        /// </summary>
        /// <returns>The item at the head of the queue</returns>
        public T Peek()
        {
            Contract.Requires(this.Count > 0);

            return _deque.PeekAtFront();
        }

        /// <summary>
        /// Removes and returns the item at the head of the queue
        /// </summary>
        /// <returns>The item that is removed from the head of the queue</returns>
        public T Dequeue()
        {
            Contract.Requires(this.Count > 0);

            return _deque.RemoveFromFront();
        }

        /// <summary>
        /// Determines whether an element is in the queue
        /// </summary>
        /// <param name="item">The element to locate</param>
        /// <returns>True if the item is found</returns>
        [Pure]
        public bool Contains(T item)
        {
            return _deque.Contains(item);
        }

        /// <summary>
        /// Copies the queue elements to a new array
        /// </summary>
        /// <returns>A new array containing elements copied from the queue</returns>
        public T[] ToArray()
        {
            Contract.Ensures(Contract.Result<T[]>() != null);

            return _deque.ToArray();
        }

        /// <summary>
        /// Sets the capacity to the actual number of elements in the queue
        /// </summary>
        public void TrimExcess()
        {
            _deque.TrimExcess();
        }

        /// <summary>
        /// Returns an Enumerator
        /// </summary>
        /// <returns>Enumerator</returns>
        public Deque<T>.Enumerator GetEnumerator()
        {
            return _deque.GetEnumerator();
        }

        /// <summary>
        /// Returns an Enumerator
        /// </summary>
        /// <returns>Enumerator</returns>
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return _deque.GetEnumerator();
        }

        /// <summary>
        /// Returns an Enumerator
        /// </summary>
        /// <returns>Enumerator</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _deque.GetEnumerator();
        }

        /// <summary>
        /// Copies the queue elements to an Array, starting at the specified array index
        /// </summary>
        /// <param name="array">Destination array</param>
        /// <param name="index">Index in array at which copying begins</param>
        void ICollection.CopyTo(Array array, int index)
        {
            (_deque as ICollection).CopyTo(array, index);
        }

        /// <summary>
        /// Gets the number of elements contained in the queue
        /// </summary>
        int ICollection.Count
        {
            get { return _deque.Count; }
        }

        /// <summary>
        /// Is collection synchronized
        /// </summary>
        bool ICollection.IsSynchronized
        {
            get { return (_deque as ICollection).IsSynchronized; }
        }

        /// <summary>
        /// Synchronization object
        /// </summary>
        object ICollection.SyncRoot
        {
            get { return (_deque as ICollection).SyncRoot; }
        }

        /// <summary>
        /// Gets the number of elements contained in the queue
        /// </summary>
        int IReadOnlyCollection<T>.Count
        {
            get { return _deque.Count; }
        }
    }
}
