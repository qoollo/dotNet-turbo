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
    /// Read-only wrapper around List class
    /// </summary>
    /// <typeparam name="T">The type of the element in the list</typeparam>
    [System.Diagnostics.DebuggerDisplay("Count = {Count}")]
    [System.Diagnostics.DebuggerTypeProxy(typeof(Qoollo.Turbo.Collections.ServiceStuff.CollectionDebugView<>))]
    [Serializable]
    public class ReadOnlyList<T>: IList<T>, IReadOnlyList<T>, IList, IReadOnlyCollection<T>, ICollection<T>, ICollection, IEnumerable<T>, IEnumerable
    {
        private static readonly ReadOnlyList<T> _empty = new ReadOnlyList<T>(new List<T>());
        /// <summary>
        /// Empty ReadOnlyList
        /// </summary>
        public static ReadOnlyList<T> Empty
        {
            get
            {
                return _empty;
            }
        }

        // ===========

        private readonly List<T> _list;

        /// <summary>
        /// Code contracts
        /// </summary>
        [ContractInvariantMethod]
        private void Invariant()
        {
            TurboContract.Invariant(_list != null);
        }

        /// <summary>
        /// ReadOnlyList constructor
        /// </summary>
        protected ReadOnlyList()
        {
            _list = new List<T>();
        }

        /// <summary>
        /// ReadOnlyList constructor
        /// </summary>
        /// <param name="list">List to be wrapped</param>
        public ReadOnlyList(List<T> list)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            _list = list;
        }

        /// <summary>
        /// ReadOnlyList constructor. Creates ReadOnlyList from element sequence
        /// </summary>
        /// <param name="collection">The collection whose elements are copied to the new ReadOnlyList</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        public ReadOnlyList(IEnumerable<T> collection)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));

            _list = new List<T>(collection);
        }



        /// <summary>
        /// Wrapped List
        /// </summary>
        protected List<T> Items { get { return _list; } }


        /// <summary>
        /// Searches for the specified 'item' and returns the index of the first occurrence of the item inside list
        /// </summary>
        /// <param name="item">The item to locate inside the list</param>
        /// <returns>The index of element inside the list, if found. -1 otherwise</returns>
        [Pure]
        public int IndexOf(T item)
        {
            return _list.IndexOf(item);
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
            return _list.IndexOf(item, index, count);
        }

        /// <summary>
        /// Searches for the specified 'item' and returns the index of the last occurrence of the item inside list
        /// </summary>
        /// <param name="item">The item to locate inside the list</param>
        /// <returns>The index of element inside the list, if found. -1 otherwise</returns>
        [Pure]
        public int LastIndexOf(T item)
        {
            return _list.LastIndexOf(item);
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
            return _list.LastIndexOf(item, index, count);
        }

        /// <summary>
        /// Returns element of the list at specified index
        /// </summary>
        /// <param name="index">Element index</param>
        /// <returns>Element of the list</returns>
        public T this[int index]
        {
            get
            {
                TurboContract.Requires(index >= 0, conditionString: "index >= 0");
                TurboContract.Requires(index < this.Count, conditionString: "index < this.Count");

                return _list[index];
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
            return _list.Contains(item);
        }

        /// <summary>
        /// Copies the entire List to Array
        /// </summary>
        /// <param name="array">Target array</param>
        /// <param name="arrayIndex">Index in array at which copying begins</param>
        public void CopyTo(T[] array, int arrayIndex)
        {
            TurboContract.Requires(array != null, conditionString: "array != null");
            TurboContract.Requires(arrayIndex >= 0, conditionString: "arrayIndex >= 0");
            TurboContract.Requires(arrayIndex <= array.Length - this.Count, conditionString: "arrayIndex <= array.Length - this.Count");

            _list.CopyTo(array, arrayIndex);
        }
        /// <summary>
        /// Gets the number of elements contained in the List
        /// </summary>
        public int Count
        {
            get { return _list.Count; }
        }

        /// <summary>
        /// Returns Enumerator
        /// </summary>
        /// <returns>Enumerator</returns>
        public List<T>.Enumerator GetEnumerator()
        {
            return _list.GetEnumerator();
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

            return _list.Exists(match);
        }

        /// <summary>
        /// Search for the fist element that match the conditions defined by the specified predicate
        /// </summary>
        /// <param name="match">Predicate</param>
        /// <returns>The first element that matches the condition, if found; otherwise, the default value for type T</returns>
        [Pure]
        public T Find(Predicate<T> match)
        {
            TurboContract.Requires(match != null, conditionString: "match != null");

            return _list.Find(match);
        }

        /// <summary>
        ///  Copies the elements of the list to a new array
        /// </summary>
        /// <returns>An array with copied elements</returns>
        public T[] ToArray()
        {
            TurboContract.Ensures(TurboContract.Result<T[]>() != null);

            return _list.ToArray();
        }
        /// <summary>
        /// Performs the specified action on each element of the list
        /// </summary>
        /// <param name="action">Action</param>
        public void ForEach(Action<T> action)
        {
            TurboContract.Requires(action != null, conditionString: "action != null");

            _list.ForEach(action);
        }

        /// <summary>
        /// Determines whether every element in the list matches the condition
        /// </summary>
        /// <param name="match">Predicate that defines the condition</param>
        /// <returns>True if every element in the list matches the condition</returns>
        [Pure]
        public bool TrueForAll(Predicate<T> match)
        {
            TurboContract.Requires(match != null, conditionString: "match != null");

            return _list.TrueForAll(match);
        }


        /// <summary>
        /// Creates TransformedReadOnlyListWrapper from the current list
        /// </summary>
        /// <typeparam name="TOut">Type of the elements of the newly created TransformedReadOnlyListWrapper</typeparam>
        /// <param name="selector">Element conversion delegate</param>
        /// <returns>Created TransformedReadOnlyListWrapper</returns>
        public TransformedReadOnlyListWrapper<T, TOut> AsTransformedReadOnlyList<TOut>(Func<T, TOut> selector)
        {
            TurboContract.Requires(selector != null, conditionString: "selector != null");
            TurboContract.Ensures(TurboContract.Result<TransformedReadOnlyListWrapper<T, TOut>>() != null);

            return new TransformedReadOnlyListWrapper<T, TOut>(_list, selector);
        }

        #region Реализация интерфейсов

        /// <summary>
        /// Searches for the specified 'item' and returns the index of the first occurrence of the item inside list
        /// </summary>
        /// <param name="item">The item to locate inside the list</param>
        /// <returns>The index of element inside the list, if found. -1 otherwise</returns>
        int IList<T>.IndexOf(T item)
        {
            return _list.IndexOf(item);
        }

        /// <summary>
        /// Inserts an item to the list (not supported)
        /// </summary>
        /// <param name="index">Index</param>
        /// <param name="item">New element</param>
        void IList<T>.Insert(int index, T item)
        {
            throw new NotSupportedException("Insert is not supported for ReadOnlyList");
        }

        /// <summary>
        /// Removes the item at the specified index (not supported)
        /// </summary>
        /// <param name="index">Index</param>
        void IList<T>.RemoveAt(int index)
        {
            throw new NotSupportedException("RemoveAt is not supported for ReadOnlyList");
        }

        /// <summary>
        /// Gets element at specified index (set is not supported)
        /// </summary>
        /// <param name="index">Index</param>
        /// <returns>Element</returns>
        T IList<T>.this[int index]
        {
            get
            {
                return _list[index];
            }
            set
            {
                throw new NotSupportedException("Items.Set is not supported for ReadOnlyList");
            }
        }

        /// <summary>
        /// Adds an item to the list (not supported)
        /// </summary>
        /// <param name="item">New item</param>
        void ICollection<T>.Add(T item)
        {
            throw new NotSupportedException("Add is not supported for ReadOnlyList");
        }

        /// <summary>
        /// Removes all items from the list (not supported)
        /// </summary>
        void ICollection<T>.Clear()
        {
            throw new NotSupportedException("Clear is not supported for ReadOnlyList");
        }

        /// <summary>
        /// Determines whether an element is in the List
        /// </summary>
        /// <param name="item">The object to locate in the List</param>
        /// <returns>True if item is found</returns>
        bool ICollection<T>.Contains(T item)
        {
            return _list.Contains(item);
        }

        /// <summary>
        /// Copies the entire List to Array
        /// </summary>
        /// <param name="array">Target array</param>
        /// <param name="arrayIndex">Index in array at which copying begins</param>
        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Gets the number of elements contained in the List
        /// </summary>
        int ICollection<T>.Count
        {
            get { return _list.Count; }
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
            throw new NotSupportedException("Remove is not supported for ReadOnlyList");
        }
        /// <summary>
        /// Returns Enumerator
        /// </summary>
        /// <returns>Enumerator</returns>
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return _list.GetEnumerator();
        }
        /// <summary>
        /// Returns Enumerator
        /// </summary>
        /// <returns>Enumerator</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _list.GetEnumerator();
        }
        /// <summary>
        /// Gets element at specified index
        /// </summary>
        /// <param name="index">Index</param>
        /// <returns>Element</returns>
        T IReadOnlyList<T>.this[int index]
        {
            get { return _list[index]; }
        }

        /// <summary>
        /// Gets the number of elements contained in the List
        /// </summary>
        int IReadOnlyCollection<T>.Count
        {
            get { return _list.Count; }
        }

        /// <summary>
        /// Adds an item to the list (not supported)
        /// </summary>
        /// <param name="value">New item</param>
        int IList.Add(object value)
        {
            throw new NotSupportedException("Add is not supported for ReadOnlyList");
        }

        /// <summary>
        /// Removes all items from the list (not supported)
        /// </summary>
        void IList.Clear()
        {
            throw new NotSupportedException("Clear is not supported for ReadOnlyList");
        }

        /// <summary>
        /// Determines whether an element is in the List
        /// </summary>
        /// <param name="value">The object to locate in the List</param>
        /// <returns>True if item is found</returns>
        bool IList.Contains(object value)
        {
            return (_list as IList).Contains(value);
        }

        /// <summary>
        /// Searches for the specified 'item' and returns the index of the first occurrence of the item inside list
        /// </summary>
        /// <param name="value">The item to locate inside the list</param>
        /// <returns>The index of element inside the list, if found. -1 otherwise</returns>
        int IList.IndexOf(object value)
        {
            return (_list as IList).IndexOf(value);
        }

        /// <summary>
        /// Inserts an item to the list (not supported)
        /// </summary>
        /// <param name="index">Index</param>
        /// <param name="value">New element</param>
        void IList.Insert(int index, object value)
        {
            throw new NotSupportedException("Insert is not supported for ReadOnlyList");
        }

        /// <summary>
        /// Is fixed size
        /// </summary>
        bool IList.IsFixedSize
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a value indicating whether the Collection is read-only
        /// </summary>
        bool IList.IsReadOnly
        {
            get { return true; }
        }

        /// <summary>
        /// Removes the first occurrence of a specific item from the list (not supported)
        /// </summary>
        /// <param name="value">Item</param>
        void IList.Remove(object value)
        {
            throw new NotSupportedException("Remove is not supported for ReadOnlyList");
        }
        /// <summary>
        /// Removes the item at the specified index (not supported)
        /// </summary>
        /// <param name="index">Index</param>
        void IList.RemoveAt(int index)
        {
            throw new NotSupportedException("RemoveAt is not supported for ReadOnlyList");
        }

        /// <summary>
        /// Gets element at specified index (set is not supported)
        /// </summary>
        /// <param name="index">Index</param>
        /// <returns>Element</returns>
        object IList.this[int index]
        {
            get
            {
                return _list[index];
            }
            set
            {
                throw new NotSupportedException("Items.Set is not supported for ReadOnlyList");
            }
        }

        /// <summary>
        /// Copies the elements of the List to an Array, starting at a particular index
        /// </summary>
        /// <param name="array">The array that is the destination of the elements</param>
        /// <param name="index">Index in array at which copying begins</param>
        void ICollection.CopyTo(Array array, int index)
        {
            (_list as ICollection).CopyTo(array, index);
        }

        /// <summary>
        /// Gets the number of elements contained in the collection
        /// </summary>
        int ICollection.Count
        {
            get { return _list.Count; }
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
            get { return (_list as ICollection).SyncRoot; }
        } 

        #endregion
    }
}
