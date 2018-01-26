using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Collections
{
    /// <summary>
    /// Read-only list with on the fly element transformation
    /// </summary>
    /// <typeparam name="TIn">The type of the element in the source list</typeparam>
    /// <typeparam name="TOut">The output type of the element in the current transformed list</typeparam>
    [System.Diagnostics.DebuggerDisplay("Count = {Count}")]
    public class TransformedReadOnlyListWrapper<TIn, TOut> : IList<TOut>, IReadOnlyList<TOut>, IList, IReadOnlyCollection<TOut>, ICollection<TOut>, ICollection, IEnumerable<TOut>, IEnumerable
    {
        /// <summary>
        /// Detemines whether the object is compatible with type
        /// </summary>
        /// <typeparam name="T">The type to which cast checked</typeparam>
        /// <param name="value">Source object</param>
        /// <returns>Can be casted to type T</returns>
        private static bool IsCompatibleObject<T>(object value)
        {
            return value is T || (value == null && default(T) == null);
        }


        private object _syncRoot;
        private readonly IList<TIn> _list;
        private readonly Func<TIn, TOut> _transformer; 

        /// <summary>
        /// Code contracts
        /// </summary>
        [ContractInvariantMethod]
        private void Invariant()
        {
            TurboContract.Invariant(_list != null);
            TurboContract.Invariant(_transformer != null);
        }

        /// <summary>
        /// TransformedReadOnlyListWrapper constructor
        /// </summary>
        /// <param name="list">Source list</param>
        /// <param name="transformator">Transformation function that will be applied to source elements</param>
        public TransformedReadOnlyListWrapper(IList<TIn> list, Func<TIn, TOut> transformator)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));
            if (transformator == null)
                throw new ArgumentNullException(nameof(transformator));

            _list = list;
            _transformer = transformator;
        }



        /// <summary>
        /// Source list
        /// </summary>
        protected IList<TIn> Items { get { return _list; } }
        /// <summary>
        /// Transformation function
        /// </summary>
        protected Func<TIn, TOut> Transformer { get { return _transformer; } }


        /// <summary>
        /// Returns element of the list at specified index
        /// </summary>
        /// <param name="index">Element index</param>
        /// <returns>Element of the list</returns>
        public TOut this[int index]
        {
            get
            {
                return _transformer(_list[index]);
            }
        }

        /// <summary>
        /// Gets the number of elements contained in the List
        /// </summary>
        public int Count
        {
            get { return _list.Count; }
        }

        /// <summary>
        /// Returns an Enumerator
        /// </summary>
        /// <returns>Enumerator</returns>
        public IEnumerator<TOut> GetEnumerator()
        {
            foreach (var elem in _list)
                yield return _transformer(elem);
        }




        /// <summary>
        /// Copies the entire List to Array
        /// </summary>
        /// <param name="array">Target array</param>
        /// <param name="arrayIndex">Index in array at which copying begins</param>
        private void CopyTo(TOut[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (arrayIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            if (arrayIndex > array.Length - this.Count)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));

            for (int i = 0; i < _list.Count; i++)
                array[i + arrayIndex] = _transformer(_list[i]);
        }

        /// <summary>
        /// Performs the specified action on each element of the list
        /// </summary>
        /// <param name="action">Action</param>
        private void ForEach(Action<TOut> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            for (int i = 0; i < _list.Count; i++)
                action(_transformer(_list[i]));
        }

        /// <summary>
        /// Searches for the specified 'item' and returns the index of the first occurrence of the item inside list
        /// </summary>
        /// <param name="item">The item to locate inside the list</param>
        /// <returns>The index of element inside the list, if found. -1 otherwise</returns>
        private int IndexOf(TOut item)
        {
            var comparer = EqualityComparer<TOut>.Default;
            for (int i = 0; i < _list.Count; i++)
            {
                if (comparer.Equals(_transformer(_list[i]), item))
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// Searches for the specified 'item' and returns the index of the last occurrence of the item inside list
        /// </summary>
        /// <param name="item">The item to locate inside the list</param>
        /// <returns>The index of element inside the list, if found. -1 otherwise</returns>
        private int LastIndexOf(TOut item)
        {
            var comparer = EqualityComparer<TOut>.Default;
            for (int i = _list.Count - 1; i >= 0; i--)
            {
                if (comparer.Equals(_transformer(_list[i]), item))
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// Determines whether an element is in the List
        /// </summary>
        /// <param name="item">The object to locate in the List</param>
        /// <returns>True if item is found</returns>
        private bool Contains(TOut item)
        {
            return this.IndexOf(item) >= 0;
        }


        #region Реализация интерфейсов


        /// <summary>
        /// Searches for the specified 'item' and returns the index of the first occurrence of the item inside list
        /// </summary>
        /// <param name="item">The item to locate inside the list</param>
        /// <returns>The index of element inside the list, if found. -1 otherwise</returns>
        int IList<TOut>.IndexOf(TOut item)
        {
            return this.IndexOf(item);
        }

        /// <summary>
        /// Inserts an item to the list (not supported)
        /// </summary>
        /// <param name="index">Index</param>
        /// <param name="item">New element</param>
        void IList<TOut>.Insert(int index, TOut item)
        {
            throw new NotSupportedException("Insert is not supported for TransformedReadOnlyListWrapper");
        }

        /// <summary>
        /// Removes the item at the specified index (not supported)
        /// </summary>
        /// <param name="index">Index</param>
        void IList<TOut>.RemoveAt(int index)
        {
            throw new NotSupportedException("RemoveAt is not supported for TransformedReadOnlyListWrapper");
        }

        /// <summary>
        /// Gets element at specified index (set is not supported)
        /// </summary>
        /// <param name="index">Index</param>
        /// <returns>Element</returns>
        TOut IList<TOut>.this[int index]
        {
            get
            {
                return _transformer(_list[index]);
            }
            set
            {
                throw new NotSupportedException("Items.Set is not supported for TransformedReadOnlyListWrapper");
            }
        }

        /// <summary>
        /// Adds an item to the list (not supported)
        /// </summary>
        /// <param name="item">New item</param>
        void ICollection<TOut>.Add(TOut item)
        {
            throw new NotSupportedException("Add is not supported for TransformedReadOnlyListWrapper");
        }

        /// <summary>
        /// Removes all items from the list (not supported)
        /// </summary>
        void ICollection<TOut>.Clear()
        {
            throw new NotSupportedException("Clear is not supported for TransformedReadOnlyListWrapper");
        }

        /// <summary>
        /// Determines whether an element is in the List
        /// </summary>
        /// <param name="item">The object to locate in the List</param>
        /// <returns>True if item is found</returns>
        bool ICollection<TOut>.Contains(TOut item)
        {
            return this.Contains(item);
        }

        /// <summary>
        /// Copies the entire List to Array
        /// </summary>
        /// <param name="array">Target array</param>
        /// <param name="arrayIndex">Index in array at which copying begins</param>
        void ICollection<TOut>.CopyTo(TOut[] array, int arrayIndex)
        {
            this.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Gets the number of elements contained in the List
        /// </summary>
        int ICollection<TOut>.Count
        {
            get { return _list.Count; }
        }

        /// <summary>
        /// Gets a value indicating whether the Collection is read-only
        /// </summary>
        bool ICollection<TOut>.IsReadOnly
        {
            get { return true; }
        }

        /// <summary>
        /// Removes the first occurrence of a specific item from the Collection (not supported)
        /// </summary>
        /// <param name="item">Item</param>
        /// <returns>True if item was removed</returns>
        bool ICollection<TOut>.Remove(TOut item)
        {
            throw new NotSupportedException("Remove is not supported for TransformedReadOnlyListWrapper");
        }

        /// <summary>
        /// Returns Enumerator
        /// </summary>
        /// <returns>Enumerator</returns>
        IEnumerator<TOut> IEnumerable<TOut>.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        /// <summary>
        /// Returns Enumerator
        /// </summary>
        /// <returns>Enumerator</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        /// <summary>
        /// Gets element at specified index
        /// </summary>
        /// <param name="index">Index</param>
        /// <returns>Element</returns>
        TOut IReadOnlyList<TOut>.this[int index]
        {
            get { return _transformer(_list[index]); }
        }

        /// <summary>
        /// Gets the number of elements contained in the List
        /// </summary>
        int IReadOnlyCollection<TOut>.Count
        {
            get { return _list.Count; }
        }

        /// <summary>
        /// Adds an item to the list (not supported)
        /// </summary>
        /// <param name="value">New item</param>
        int IList.Add(object value)
        {
            throw new NotSupportedException("Add is not supported for TransformedReadOnlyListWrapper");
        }

        /// <summary>
        /// Removes all items from the list (not supported)
        /// </summary>
        void IList.Clear()
        {
            throw new NotSupportedException("Clear is not supported for TransformedReadOnlyListWrapper");
        }

        /// <summary>
        /// Determines whether an element is in the List
        /// </summary>
        /// <param name="value">The object to locate in the List</param>
        /// <returns>True if item is found</returns>
        bool IList.Contains(object value)
        {
            if (!IsCompatibleObject<TOut>(value))
                return false;

            return this.Contains((TOut)value);
        }

        /// <summary>
        /// Searches for the specified 'value' and returns the index of the first occurrence of the item inside list
        /// </summary>
        /// <param name="value">The item to locate inside the list</param>
        /// <returns>The index of element inside the list, if found. -1 otherwise</returns>
        int IList.IndexOf(object value)
        {
            if (!IsCompatibleObject<TOut>(value))
                return -1;

            return this.IndexOf((TOut)value);
        }

        /// <summary>
        /// Inserts an item to the list (not supported)
        /// </summary>
        /// <param name="index">Index</param>
        /// <param name="value">New element</param>
        void IList.Insert(int index, object value)
        {
            throw new NotSupportedException("Insert is not supported for TransformedReadOnlyListWrapper");
        }

        /// <summary>
        /// Is fixed size
        /// </summary>
        bool IList.IsFixedSize
        {
            get { return (_list as IList).IsFixedSize; }
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
            throw new NotSupportedException("Remove is not supported for TransformedReadOnlyListWrapper");
        }

        /// <summary>
        /// Removes the item at the specified index (not supported)
        /// </summary>
        /// <param name="index">Index</param>
        void IList.RemoveAt(int index)
        {
            throw new NotSupportedException("RemoveAt is not supported for TransformedReadOnlyListWrapper");
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
                return _transformer(_list[index]);
            }
            set
            {
                throw new NotSupportedException("Items.Set is not supported for TransformedReadOnlyListWrapper");
            }
        }

        /// <summary>
        /// Copies the elements of the List to an Array, starting at a particular index
        /// </summary>
        /// <param name="array">The array that is the destination of the elements</param>
        /// <param name="index">Index in array at which copying begins</param>
        void ICollection.CopyTo(Array array, int index)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (array.Rank != 1)
                throw new ArgumentException("array has wrong dimension");
            if (index < 0)
                throw new ArgumentOutOfRangeException("index is less then zero");
            if (array.Length - index < _list.Count)
                throw new ArgumentOutOfRangeException("array has not enough space");

            for (int i = 0; i < _list.Count; i++)
                array.SetValue(_transformer(_list[i]), index + i);
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
            get
            {
                if (this._syncRoot == null)
                {
                    if (this._list is ICollection collection)
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

        #endregion
    }
}
