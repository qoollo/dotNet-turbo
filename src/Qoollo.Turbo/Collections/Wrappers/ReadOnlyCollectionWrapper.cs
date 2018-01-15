using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Collections
{
    /// <summary>
    /// Read-only wrapper around ICollection
    /// </summary>
    /// <typeparam name="T">The type of the element in collection</typeparam>
    [System.Diagnostics.DebuggerDisplay("Count = {Count}")]
    [System.Diagnostics.DebuggerTypeProxy(typeof(Qoollo.Turbo.Collections.ServiceStuff.CollectionDebugView<>))]
    [Serializable]
    public class ReadOnlyCollectionWrapper<T>: ICollection<T>, ICollection, IReadOnlyCollection<T>, IEnumerable<T>, IEnumerable
    {
        private static readonly ReadOnlyCollectionWrapper<T> _empty = new ReadOnlyCollectionWrapper<T>(new T[0]);
        /// <summary>
        /// Empty ReadOnlyCollectionWrapper
        /// </summary>
        public static ReadOnlyCollectionWrapper<T> Empty
        {
            get
            {
                return _empty;
            }
        }

        // ===========

        private readonly ICollection<T> _collection;
        [NonSerialized]
        private object _syncRoot;


        /// <summary>
        /// Code contracts
        /// </summary>
        [ContractInvariantMethod]
        private void Invariant()
        {
            TurboContract.Invariant(_collection != null);
        }

        /// <summary>
        /// ReadOnlyCollectionWrapper constructor
        /// </summary>
        /// <param name="collection">The collection to wrap</param>
        public ReadOnlyCollectionWrapper(ICollection<T> collection)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));

            _collection = collection;
        }

        /// <summary>
        /// Wrapped collection
        /// </summary>
        protected ICollection<T> Items { get { return _collection; } }

        /// <summary>
        /// Determines whether the collection contains a specific value.
        /// </summary>
        /// <param name="item">The object to locate in the collection</param>
        /// <returns>True if item is found</returns>
        [Pure]
        public bool Contains(T item)
        {
            return _collection.Contains(item);
        }
        /// <summary>
        /// Copies the elements of the Collection to an Array, starting at a particular index
        /// </summary>
        /// <param name="array">The array that is the destination of the elements</param>
        /// <param name="arrayIndex">Index in array at which copying begins</param>
        public void CopyTo(T[] array, int arrayIndex)
        {
            TurboContract.Requires(array != null);
            TurboContract.Requires(arrayIndex >= 0);
            TurboContract.Requires(arrayIndex <= array.Length - this.Count); 

            _collection.CopyTo(array, arrayIndex);
        }
        /// <summary>
        /// Gets the number of elements contained in the collection
        /// </summary>
        public int Count
        {
            get { return _collection.Count; }
        }
        /// <summary>
        /// Returns an Enumerator
        /// </summary>
        /// <returns>Enumerator</returns>
        public IEnumerator<T> GetEnumerator()
        {
            return _collection.GetEnumerator();
        }


        #region Реализация интерфейсов

        /// <summary>
        /// Adds an item to the collection (not supported)
        /// </summary>
        /// <param name="item">New item</param>
        void ICollection<T>.Add(T item)
        {
            throw new NotSupportedException("Add is not supported for ReadOnlyCollectionWrapper");
        }

        /// <summary>
        /// Removes all items from the collection (not supported)
        /// </summary>
        void ICollection<T>.Clear()
        {
            throw new NotSupportedException("Clear is not supported for ReadOnlyCollectionWrapper");
        }

        /// <summary>
        /// Determines whether the collection contains a specific value.
        /// </summary>
        /// <param name="item">The object to locate in the collection</param>
        /// <returns>True if item is found</returns>
        bool ICollection<T>.Contains(T item)
        {
            return _collection.Contains(item);
        }

        /// <summary>
        /// Copies the elements of the Collection to an Array, starting at a particular index
        /// </summary>
        /// <param name="array">The array that is the destination of the elements</param>
        /// <param name="arrayIndex">Index in array at which copying begins</param>
        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            _collection.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Gets the number of elements contained in the collection
        /// </summary>
        int ICollection<T>.Count
        {
            get { return _collection.Count; }
        }

        /// <summary>
        /// Gets a value indicating whether the Collection is read-only
        /// </summary>
        bool ICollection<T>.IsReadOnly
        {
            get { return true; }
        }

        /// <summary>
        /// Removes the first occurrence of a specific item from the Collection (not supported)
        /// </summary>
        /// <param name="item">Item</param>
        /// <returns>True if item was removed</returns>
        bool ICollection<T>.Remove(T item)
        {
            throw new NotSupportedException("Remove is not supported for ReadOnlyCollectionWrapper");
        }

        /// <summary>
        /// Returns an Enumerator
        /// </summary>
        /// <returns>Enumerator</returns>
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return _collection.GetEnumerator();
        }

        /// <summary>
        /// Returns an Enumerator
        /// </summary>
        /// <returns>Enumerator</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _collection.GetEnumerator();
        }

        /// <summary>
        /// Copies the elements of the Collection to an Array, starting at a particular index
        /// </summary>
        /// <param name="array">The array that is the destination of the elements</param>
        /// <param name="index">Index in array at which copying begins</param>
        void ICollection.CopyTo(Array array, int index)
        {
            (_collection as ICollection).CopyTo(array, index);
        }

        /// <summary>
        /// Gets the number of elements contained in the collection
        /// </summary>
        int ICollection.Count
        {
            get { return _collection.Count; }
        }

        /// <summary>
        /// Is Synchronized
        /// </summary>
        bool ICollection.IsSynchronized
        {
            get { return false; }
        }

        /// <summary>
        /// Sync root object
        /// </summary>
        object ICollection.SyncRoot
        {
            get
            {
                if (this._syncRoot == null)
                {
                    if (this._collection is ICollection collection)
                    {
                        this._syncRoot = collection.SyncRoot;
                    }
                    else
                    {
                        System.Threading.Interlocked.CompareExchange<object>(ref this._syncRoot, new object(), null);
                    }
                }
                return this._syncRoot;
            }
        }

        /// <summary>
        /// Gets the number of elements contained in the collection
        /// </summary>
        int IReadOnlyCollection<T>.Count
        {
            get { return _collection.Count; }
        } 

        #endregion
    }
}
