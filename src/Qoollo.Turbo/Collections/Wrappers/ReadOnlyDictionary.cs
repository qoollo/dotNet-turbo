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
    /// Read-only wrapper around Dictionary class
    /// </summary>
    /// <typeparam name="TKey">The type of the keys in the dictionary</typeparam>
    /// <typeparam name="TValue">The type of the values in the dictionary</typeparam>
    [Serializable]
    public class ReadOnlyDictionary<TKey, TValue> : IDictionary<TKey, TValue>, ICollection<KeyValuePair<TKey, TValue>>, IDictionary, ICollection, IReadOnlyDictionary<TKey, TValue>, IReadOnlyCollection<KeyValuePair<TKey, TValue>>, IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable
    {
        private static readonly ReadOnlyDictionary<TKey, TValue> _empty = new ReadOnlyDictionary<TKey, TValue>(new Dictionary<TKey, TValue>());
        /// <summary>
        /// Empty dictionary wrapper
        /// </summary>
        public static ReadOnlyDictionary<TKey, TValue> Empty
        {
            get
            {
                return _empty;
            }
        }

        // ===========


        private Dictionary<TKey, TValue> _dictionary;

        /// <summary>
        /// Code contracts
        /// </summary>
        [ContractInvariantMethod]
        private void Invariant()
        {
            Contract.Invariant(_dictionary != null);
        }

        /// <summary>
        /// ReadOnlyDictionary constructor
        /// </summary>
        protected ReadOnlyDictionary()
        {
            _dictionary = new Dictionary<TKey, TValue>();
        }

        /// <summary>
        /// ReadOnlyDictionary constructor
        /// </summary>
        /// <param name="dict">The dictionary to wrap</param>
        public ReadOnlyDictionary(Dictionary<TKey, TValue> dict)
        {
            Contract.Requires<ArgumentNullException>(dict != null);

            _dictionary = dict;
        }

        /// <summary>
        /// ReadOnlyDictionary constructor
        /// </summary>
        /// <param name="srcDict">The source of elements to the newly created read-only dictionary (elements will be copied)</param>
        public ReadOnlyDictionary(IDictionary<TKey, TValue> srcDict)
        {
            Contract.Requires<ArgumentNullException>(srcDict != null);

            _dictionary = new Dictionary<TKey,TValue>(srcDict);
        }

        /// <summary>
        /// ReadOnlyDictionary constructor
        /// </summary>
        /// <param name="srcDict">The source of elements to the newly created read-only dictionary (elements will be copied)</param>
        /// <param name="keyComparer">Key comparer</param>
        public ReadOnlyDictionary(IDictionary<TKey, TValue> srcDict, IEqualityComparer<TKey> keyComparer)
        {
            Contract.Requires<ArgumentNullException>(srcDict != null);
            Contract.Requires<ArgumentNullException>(keyComparer != null);

            _dictionary = new Dictionary<TKey, TValue>(srcDict, keyComparer);
        }

        /// <summary>
        /// Wrapped dictionary
        /// </summary>
        protected Dictionary<TKey, TValue> Dictionary { get { return _dictionary; } }

        /// <summary>
        /// Gets the number of records contained in the Dictionary
        /// </summary>
        public int Count { get { return _dictionary.Count; } }

        /// <summary>
        /// Collection of the keys from Dictionary
        /// </summary>
        public Dictionary<TKey, TValue>.KeyCollection Keys { get { return _dictionary.Keys; } }
        /// <summary>
        /// Collection of the values from Dictionary
        /// </summary>
        public Dictionary<TKey, TValue>.ValueCollection Values { get { return _dictionary.Values; } }

        /// <summary>
        /// Gets the item from Dictionary for specified key
        /// </summary>
        /// <param name="key">The key of the value to get</param>
        /// <returns>Item</returns>
        public TValue this[TKey key] 
        {
            get
            {
                return _dictionary[key];
            }
        }
        /// <summary>
        /// Gets the item associated with the specified key
        /// </summary>
        /// <param name="key">The key of the value to get</param>
        /// <param name="value">The value associated with key</param>
        /// <returns>True if the Dictionary contains element with the specified key</returns>
        public bool TryGetValue(TKey key, out TValue value)
        {
            return _dictionary.TryGetValue(key, out value);
        }

        /// <summary>
        /// Determines whether the Dictionary contains the specified key
        /// </summary>
        /// <param name="key">The key to locate in the Dictionary</param>
        /// <returns>True if the Dictionary contains the key</returns>
        [Pure]
        public bool ContainsKey(TKey key)
        {
            return _dictionary.ContainsKey(key);
        }

        /// <summary>
        /// Determines whether the Dictionary contains a specific value
        /// </summary>
        /// <param name="value">The value to locate in the Dictionary</param>
        /// <returns>True if the Dictionary contains the value</returns>
        [Pure]
        public bool ContainsValue(TValue value)
        {
            return _dictionary.ContainsValue(value);
        }

        /// <summary>
        /// Returns Enumerator
        /// </summary>
        /// <returns>Enumerator</returns>
        public Dictionary<TKey, TValue>.Enumerator GetEnumerator()
        {
            return _dictionary.GetEnumerator();
        }


        #region Реализация интерфейсов


        /// <summary>
        /// Adds an element with the provided key and value to the Dictionary (not supported)
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        void IDictionary<TKey, TValue>.Add(TKey key, TValue value)
        {
            throw new NotSupportedException("Add is not supported for ReadOnlyDictionary");
        }

        /// <summary>
        /// Determines whether the Dictionary contains the specified key
        /// </summary>
        /// <param name="key">The key to locate in the Dictionary</param>
        /// <returns>True if the Dictionary contains the key</returns>
        bool IDictionary<TKey, TValue>.ContainsKey(TKey key)
        {
            return _dictionary.ContainsKey(key);
        }

        /// <summary>
        /// Collection of the keys from Dictionary
        /// </summary>
        ICollection<TKey> IDictionary<TKey, TValue>.Keys
        {
            get { return _dictionary.Keys; }
        }

        /// <summary>
        /// Removes the element with the specified key from the Dictionary (not supported)
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>True if was removed</returns>
        bool IDictionary<TKey, TValue>.Remove(TKey key)
        {
            throw new NotSupportedException("Remove is not supported for ReadOnlyDictionary");
        }

        /// <summary>
        /// Gets the item associated with the specified key
        /// </summary>
        /// <param name="key">The key of the value to get</param>
        /// <param name="value">The value associated with key</param>
        /// <returns>True if the Dictionary contains element with the specified key</returns>
        bool IDictionary<TKey, TValue>.TryGetValue(TKey key, out TValue value)
        {
            return _dictionary.TryGetValue(key, out value);
        }

        /// <summary>
        /// Collection of the values from Dictionary
        /// </summary>
        ICollection<TValue> IDictionary<TKey, TValue>.Values
        {
            get { return _dictionary.Values; }
        }

        /// <summary>
        /// Gets the item from Dictionary for specified key. Set is not supported
        /// </summary>
        /// <param name="key">The key of the value to get</param>
        /// <returns>Item</returns>
        TValue IDictionary<TKey, TValue>.this[TKey key]
        {
            get
            {
                return _dictionary[key];
            }
            set
            {
                throw new NotSupportedException("Items.Set is not supported for ReadOnlyDictionary");
            }
        }

        /// <summary>
        /// Adds an item to the Dictionary (not supported)
        /// </summary>
        /// <param name="item">New item</param>
        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
        {
            throw new NotSupportedException("Add is not supported for ReadOnlyDictionary");
        }

        /// <summary>
        /// Removes all items from the Dictionary (not supported)
        /// </summary>
        void ICollection<KeyValuePair<TKey, TValue>>.Clear()
        {
            throw new NotSupportedException("Clear is not supported for ReadOnlyDictionary");
        }

        /// <summary>
        /// Determines whether the Dictionary contains a specific key-value pair.
        /// </summary>
        /// <param name="item">The object to locate in the collection</param>
        /// <returns>True if item is found</returns>
        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
        {
            return (_dictionary as ICollection<KeyValuePair<TKey, TValue>>).Contains(item);
        }

        /// <summary>
        /// Copies the elements of the Collection to an Array, starting at a particular index
        /// </summary>
        /// <param name="array">The array that is the destination of the elements</param>
        /// <param name="arrayIndex">Index in array at which copying begins</param>
        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            (_dictionary as ICollection<KeyValuePair<TKey, TValue>>).CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Gets the number of elements contained in the Dictionary
        /// </summary>
        int ICollection<KeyValuePair<TKey, TValue>>.Count
        {
            get { return _dictionary.Count; }
        }

        /// <summary>
        /// Gets a value indicating whether the Collection is read-only
        /// </summary>
        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly
        {
            get { return true; }
        }

        /// <summary>
        /// Removes the first occurrence of a specific item from the Collection (not supported)
        /// </summary>
        /// <param name="item">Item</param>
        /// <returns>True if item was removed</returns>
        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        {
            throw new NotSupportedException("Remove is not supported for ReadOnlyDictionary");
        }

        /// <summary>
        /// Returns an Enumerator
        /// </summary>
        /// <returns>Enumerator</returns>
        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        {
            return _dictionary.GetEnumerator();
        }
        /// <summary>
        /// Returns an Enumerator
        /// </summary>
        /// <returns>Enumerator</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _dictionary.GetEnumerator();
        }

        /// <summary>
        /// Adds an element with the provided key and value to the Dictionary (not supported)
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        void IDictionary.Add(object key, object value)
        {
            (_dictionary as IDictionary).Add(key, value);
        }

        /// <summary>
        /// Removes all items from the Dictionary (not supported)
        /// </summary>
        void IDictionary.Clear()
        {
            throw new NotSupportedException("Clear is not supported for ReadOnlyDictionary");
        }

        /// <summary>
        /// Determines whether the Dictionary contains the specified key
        /// </summary>
        /// <param name="key">The key to locate in the Dictionary</param>
        /// <returns>True if the Dictionary contains the key</returns>
        bool IDictionary.Contains(object key)
        {
            return (_dictionary as IDictionary).Contains(key);
        }

        /// <summary>
        /// Returns an Enumerator
        /// </summary>
        /// <returns>Enumerator</returns>
        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            return (_dictionary as IDictionary).GetEnumerator();
        }

        /// <summary>
        /// Is fixed size
        /// </summary>
        bool IDictionary.IsFixedSize
        {
            get { return false; }
        }

        /// <summary>
        /// Is read only
        /// </summary>
        bool IDictionary.IsReadOnly
        {
            get { return true; }
        }

        /// <summary>
        /// Key collection
        /// </summary>
        ICollection IDictionary.Keys
        {
            get { return _dictionary.Keys; }
        }

        /// <summary>
        /// Removes the element with the specified key from the Dictionary (not supported)
        /// </summary>
        /// <param name="key">Key</param>
        void IDictionary.Remove(object key)
        {
            throw new NotSupportedException("Remove is not supported for ReadOnlyDictionary");
        }

        /// <summary>
        /// Value collection
        /// </summary>
        ICollection IDictionary.Values
        {
            get { return _dictionary.Values; }
        }

        /// <summary>
        /// Gets the item from Dictionary for specified key. Set is not supported
        /// </summary>
        /// <param name="key">The key of the value to get</param>
        /// <returns>Item</returns>
        object IDictionary.this[object key]
        {
            get
            {
                return (_dictionary as IDictionary)[key];
            }
            set
            {
                throw new NotSupportedException("Items.Set is not supported for ReadOnlyDictionary");
            }
        }

        /// <summary>
        /// Copies the key-value pairs of the Dictionary to an Array, starting at a particular index
        /// </summary>
        /// <param name="array">The array that is the destination of the elements</param>
        /// <param name="index">Index in array at which copying begins</param>
        void ICollection.CopyTo(Array array, int index)
        {
            (_dictionary as ICollection).CopyTo(array, index);
        }

        /// <summary>
        /// Gets the number of records contained in the Dictionary
        /// </summary>
        int ICollection.Count
        {
            get { return _dictionary.Count; }
        }


        /// <summary>
        /// Is synchronized
        /// </summary>
        bool ICollection.IsSynchronized
        {
            get { return false; }
        }

        /// <summary>
        /// Sync root for synchronization
        /// </summary>
        object ICollection.SyncRoot
        {
            get { return (_dictionary as ICollection).SyncRoot; }
        }

        /// <summary>
        /// Determines whether the Dictionary contains the specified key
        /// </summary>
        /// <param name="key">The key to locate in the Dictionary</param>
        /// <returns>True if the Dictionary contains the key</returns>
        bool IReadOnlyDictionary<TKey, TValue>.ContainsKey(TKey key)
        {
            return _dictionary.ContainsKey(key);
        }

        /// <summary>
        /// Keys collection
        /// </summary>
        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys
        {
            get { return _dictionary.Keys; }
        }

        /// <summary>
        /// Gets the item associated with the specified key
        /// </summary>
        /// <param name="key">The key of the value to get</param>
        /// <param name="value">The value associated with key</param>
        /// <returns>True if the Dictionary contains element with the specified key</returns>
        bool IReadOnlyDictionary<TKey, TValue>.TryGetValue(TKey key, out TValue value)
        {
            return _dictionary.TryGetValue(key, out value);
        }

        /// <summary>
        /// Values collection
        /// </summary>
        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values
        {
            get { return _dictionary.Values; }
        }

        /// <summary>
        /// Gets the item from Dictionary for specified key
        /// </summary>
        /// <param name="key">The key of the value to get</param>
        /// <returns>Item</returns>
        TValue IReadOnlyDictionary<TKey, TValue>.this[TKey key]
        {
            get { return _dictionary[key]; }
        }

        /// <summary>
        /// Gets the number of records contained in the Dictionary
        /// </summary>
        int IReadOnlyCollection<KeyValuePair<TKey, TValue>>.Count
        {
            get { return _dictionary.Count; }
        }

        #endregion
    }
}
