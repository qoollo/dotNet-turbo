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
    /// Read-only wrapper around HashSet class
    /// </summary>
    /// <typeparam name="T">The type of the element in hash set</typeparam>
    [System.Diagnostics.DebuggerDisplay("Count = {Count}")]
    [System.Diagnostics.DebuggerTypeProxy(typeof(Qoollo.Turbo.Collections.ServiceStuff.CollectionDebugView<>))]
    [Serializable]
    public class ReadOnlyHashSet<T>: ISet<T>, ICollection<T>, ICollection, IReadOnlyCollection<T>, IEnumerable<T>, IEnumerable
    {
        private static readonly ReadOnlyHashSet<T> _empty = new ReadOnlyHashSet<T>(new HashSet<T>());
        /// <summary>
        /// Empty hash set
        /// </summary>
        public static ReadOnlyHashSet<T> Empty
        {
            get
            {
                return _empty;
            }
        }

        // ===========

        private readonly HashSet<T> _set;
        [NonSerialized]
        private object _syncRoot;


        /// <summary>
        /// Code contracts
        /// </summary>
        [ContractInvariantMethod]
        private void Invariant()
        {
            Contract.Invariant(_set != null);
        }

        /// <summary>
        /// ReadOnlyHashSet constructor
        /// </summary>
        protected ReadOnlyHashSet()
        {
            _set = new HashSet<T>();
        }

        /// <summary>
        /// ReadOnlyHashSet constructor
        /// </summary>
        /// <param name="set">Hash set to be wrapped</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        public ReadOnlyHashSet(HashSet<T> set)
        {
            Contract.Requires<ArgumentNullException>(set != null);

            _set = set;
        }

        /// <summary>
        /// ReadOnlyHashSet constructor
        /// </summary>
        /// <param name="set">The collection whose elements are copied to the new set</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        public ReadOnlyHashSet(IEnumerable<T> set)
        {
            Contract.Requires<ArgumentNullException>(set != null);

            _set = new HashSet<T>(set);
        }

        /// <summary>
        /// ReadOnlyHashSet constructor
        /// </summary>
        /// <param name="set">The collection whose elements are copied to the new set</param>
        /// <param name="eqCmp">Items equality comparer</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        public ReadOnlyHashSet(IEnumerable<T> set, IEqualityComparer<T> eqCmp)
        {
            Contract.Requires<ArgumentNullException>(set != null);
            Contract.Requires<ArgumentNullException>(eqCmp != null);

            _set = new HashSet<T>(set, eqCmp);
        }

        /// <summary>
        /// Wrapped hash set
        /// </summary>
        protected HashSet<T> Items { get { return _set; } }

        /// <summary>
        /// Determines whether the current set is a proper subset of the specified collection
        /// </summary>
        /// <param name="other">The collection to compare</param>
        /// <returns>True if the current set is a proper subset of collection 'other'</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            Contract.Requires(other != null);

            return _set.IsProperSubsetOf(other);
        }
        /// <summary>
        /// Determines whether the current set is a proper superset of the specified collection
        /// </summary>
        /// <param name="other">The collection to compare</param>
        /// <returns>True if the current set is a proper superset of collection 'other'</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            Contract.Requires(other != null);

            return _set.IsProperSupersetOf(other);
        }

        /// <summary>
        /// Determines whether the current set is a subset of the specified collection
        /// </summary>
        /// <param name="other">The collection to compare</param>
        /// <returns>True if the current set is a subset of collection 'other'</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public bool IsSubsetOf(IEnumerable<T> other)
        {
            Contract.Requires(other != null);

            return _set.IsSubsetOf(other);
        }
        /// <summary>
        /// Determines whether the current set is a superset of the specified collection
        /// </summary>
        /// <param name="other">The collection to compare</param>
        /// <returns>True if the current set is a superset of collection 'other'</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public bool IsSupersetOf(IEnumerable<T> other)
        {
            Contract.Requires(other != null);

            return _set.IsSupersetOf(other);
        }
        /// <summary>
        /// Determines whether the current set and a specified collection share common elements
        /// </summary>
        /// <param name="other">The collection to compare</param>
        /// <returns>True if the current seet and 'other' has at least one common element</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public bool Overlaps(IEnumerable<T> other)
        {
            Contract.Requires(other != null);

            return _set.Overlaps(other);
        }
        /// <summary>
        /// Determines whether the current set and the specified collection contain the same elements
        /// </summary>
        /// <param name="other">The collection to compare</param>
        /// <returns>True if the current set is equal to 'other'</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public bool SetEquals(IEnumerable<T> other)
        {
            Contract.Requires(other != null);

            return _set.SetEquals(other);
        }


        /// <summary>
        /// Determines whether the hash set contains a specific value
        /// </summary>
        /// <param name="item">The object to locate in the set</param>
        /// <returns>True if item is found</returns>
        [Pure]
        public bool Contains(T item)
        {
            return _set.Contains(item);
        }

        /// <summary>
        /// Copies the elements of the ReadOnlyHashSet to an Array, starting at a particular index
        /// </summary>
        /// <param name="array">The array that is the destination of the elements</param>
        /// <param name="arrayIndex">Index in array at which copying begins</param>
        public void CopyTo(T[] array, int arrayIndex)
        {
            Contract.Requires(array != null);
            Contract.Requires(arrayIndex >= 0);
            Contract.Requires(arrayIndex <= array.Length - this.Count);

            _set.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// The number of elements that are contained in a set
        /// </summary>
        public int Count
        {
            get { return _set.Count; }
        }

        /// <summary>
        /// Returns an Enumerator
        /// </summary>
        /// <returns>Enumerator</returns>
        public HashSet<T>.Enumerator GetEnumerator()
        {
            return _set.GetEnumerator();
        }


        #region Реализация интерфейсов

        /// <summary>
        /// Adds an element to the current set (not supported)
        /// </summary>
        /// <param name="item">Element</param>
        bool ISet<T>.Add(T item)
        {
            throw new NotSupportedException("Add is not supported for ReadOnlySetWrapper");
        }

        /// <summary>
        /// Removes all elements in the specified collection from the current set (not supported)
        /// </summary>
        /// <param name="other">The collection of items</param>
        void ISet<T>.ExceptWith(IEnumerable<T> other)
        {
            throw new NotSupportedException("ExceptWith is not supported for ReadOnlySetWrapper");
        }

        /// <summary>
        /// Intersect current set with the specified collection (not supported)
        /// </summary>
        /// <param name="other">The collection of items</param>
        void ISet<T>.IntersectWith(IEnumerable<T> other)
        {
            throw new NotSupportedException("IntersectWith is not supported for ReadOnlySetWrapper");
        }

        /// <summary>
        /// Determines whether the current set is a proper subset of the specified collection
        /// </summary>
        /// <param name="other">The collection to compare</param>
        /// <returns>True if the current set is a proper subset of collection 'other'</returns>
        bool ISet<T>.IsProperSubsetOf(IEnumerable<T> other)
        {
            return _set.IsProperSubsetOf(other);
        }

        /// <summary>
        /// Determines whether the current set is a proper superset of the specified collection
        /// </summary>
        /// <param name="other">The collection to compare</param>
        /// <returns>True if the current set is a proper superset of collection 'other'</returns>
        bool ISet<T>.IsProperSupersetOf(IEnumerable<T> other)
        {
            return _set.IsProperSupersetOf(other);
        }

        /// <summary>
        /// Determines whether the current set is a subset of the specified collection
        /// </summary>
        /// <param name="other">The collection to compare</param>
        /// <returns>True if the current set is a subset of collection 'other'</returns>
        bool ISet<T>.IsSubsetOf(IEnumerable<T> other)
        {
            return _set.IsSubsetOf(other);
        }

        /// <summary>
        /// Determines whether the current set is a superset of the specified collection
        /// </summary>
        /// <param name="other">The collection to compare</param>
        /// <returns>True if the current set is a superset of collection 'other'</returns>
        bool ISet<T>.IsSupersetOf(IEnumerable<T> other)
        {
            return _set.IsSupersetOf(other);
        }
        /// <summary>
        /// Determines whether the current set and a specified collection share common elements
        /// </summary>
        /// <param name="other">The collection to compare</param>
        /// <returns>True if the current seet and 'other' has at least one common element</returns>
        bool ISet<T>.Overlaps(IEnumerable<T> other)
        {
            return _set.Overlaps(other);
        }
        /// <summary>
        /// Determines whether the current set and the specified collection contain the same elements
        /// </summary>
        /// <param name="other">The collection to compare</param>
        /// <returns>True if the current set is equal to 'other'</returns>
        bool ISet<T>.SetEquals(IEnumerable<T> other)
        {
            return _set.SetEquals(other);
        }

        /// <summary>
        /// Modifies the current set so that it contains only elements that are present either in the current set or in the specified collection, but not both 
        /// (not supported)
        /// </summary>
        /// <param name="other">The collection of items</param>
        void ISet<T>.SymmetricExceptWith(IEnumerable<T> other)
        {
            throw new NotSupportedException("SymmetricExceptWith is not supported for ReadOnlySetWrapper");
        }

        /// <summary>
        /// Set union (not supported)
        /// </summary>
        /// <param name="other">The collection of items</param>
        void ISet<T>.UnionWith(IEnumerable<T> other)
        {
            throw new NotSupportedException("UnionWith is not supported for ReadOnlySetWrapper");
        }

        /// <summary>
        /// Adds an element to the current set (not supported)
        /// </summary>
        /// <param name="item">Element</param>
        void ICollection<T>.Add(T item)
        {
            throw new NotSupportedException("Add is not supported for ReadOnlySetWrapper");
        }

        /// <summary>
        /// Clear set (not supported)
        /// </summary>
        void ICollection<T>.Clear()
        {
            throw new NotSupportedException("Clear is not supported for ReadOnlySetWrapper");
        }

        /// <summary>
        /// Determines whether the hash set contains a specific value
        /// </summary>
        /// <param name="item">The object to locate in the set</param>
        /// <returns>True if item is found</returns>
        bool ICollection<T>.Contains(T item)
        {
            return _set.Contains(item);
        }

        /// <summary>
        /// Copies the elements of the ReadOnlyHashSet to an Array, starting at a particular index
        /// </summary>
        /// <param name="array">The array that is the destination of the elements</param>
        /// <param name="arrayIndex">Index in array at which copying begins</param>
        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            _set.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// The number of elements that are contained in a set
        /// </summary>
        int ICollection<T>.Count
        {
            get { return _set.Count; }
        }

        /// <summary>
        /// Is collection read-only
        /// </summary>
        bool ICollection<T>.IsReadOnly
        {
            get { return true; }
        }

        /// <summary>
        /// Remove element from the current set (not supported)
        /// </summary>
        /// <param name="item">Element</param>
        /// <returns>True if item was removed</returns>
        bool ICollection<T>.Remove(T item)
        {
            throw new NotSupportedException("Remove is not supported for ReadOnlySetWrapper");
        }

        /// <summary>
        /// Returns an Enumerator
        /// </summary>
        /// <returns>Enumerator</returns>
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return _set.GetEnumerator();
        }

        /// <summary>
        /// Returns an Enumerator
        /// </summary>
        /// <returns>Enumerator</returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _set.GetEnumerator();
        }

        /// <summary>
        /// Copies the elements of the ReadOnlyHashSet to an Array, starting at a particular index
        /// </summary>
        /// <param name="array">The array that is the destination of the elements</param>
        /// <param name="index">Index in array at which copying begins</param>
        void ICollection.CopyTo(Array array, int index)
        {
            (_set as ICollection).CopyTo(array, index);
        }

        /// <summary>
        /// The number of elements that are contained in a set
        /// </summary>
        int ICollection.Count
        {
            get { return _set.Count; }
        }

        /// <summary>
        /// Is collection synchronized
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
                    ICollection collection = this._set as ICollection;
                    if (collection != null)
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
        /// The number of elements that are contained in a set
        /// </summary>
        int IReadOnlyCollection<T>.Count
        {
            get { return _set.Count; }
        } 

        #endregion
    }
}
