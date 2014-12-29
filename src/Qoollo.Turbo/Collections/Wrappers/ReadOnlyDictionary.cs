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
    /// Словарь в режиме только для чтения
    /// </summary>
    /// <typeparam name="TKey">Тип ключа</typeparam>
    /// <typeparam name="TValue">Тип значений</typeparam>
    [Serializable]
    public class ReadOnlyDictionary<TKey, TValue> : IDictionary<TKey, TValue>, ICollection<KeyValuePair<TKey, TValue>>, IDictionary, ICollection, IReadOnlyDictionary<TKey, TValue>, IReadOnlyCollection<KeyValuePair<TKey, TValue>>, IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable
    {
        private static readonly ReadOnlyDictionary<TKey, TValue> _empty = new ReadOnlyDictionary<TKey, TValue>(new Dictionary<TKey, TValue>());
        /// <summary>
        /// Пустой словарь
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
        /// Контракты
        /// </summary>
        [ContractInvariantMethod]
        private void Invariant()
        {
            Contract.Invariant(_dictionary != null);
        }

        /// <summary>
        /// Конструктор ReadOnlyDictionary
        /// </summary>
        protected ReadOnlyDictionary()
        {
            _dictionary = new Dictionary<TKey, TValue>();
        }

        /// <summary>
        /// Конструктор ReadOnlyDictionary
        /// </summary>
        /// <param name="dict">Обёртываемый словарь</param>
        public ReadOnlyDictionary(Dictionary<TKey, TValue> dict)
        {
            Contract.Requires<ArgumentNullException>(dict != null);

            _dictionary = dict;
        }

        /// <summary>
        /// Конструктор ReadOnlyDictionary
        /// </summary>
        /// <param name="srcDict">Источник элементов словаря</param>
        public ReadOnlyDictionary(IDictionary<TKey, TValue> srcDict)
        {
            Contract.Requires<ArgumentNullException>(srcDict != null);

            _dictionary = new Dictionary<TKey,TValue>(srcDict);
        }

        /// <summary>
        /// Конструктор ReadOnlyDictionary
        /// </summary>
        /// <param name="srcDict">Источник элементов словаря</param>
        /// <param name="keyComparer">Компаратор ключей</param>
        public ReadOnlyDictionary(IDictionary<TKey, TValue> srcDict, IEqualityComparer<TKey> keyComparer)
        {
            Contract.Requires<ArgumentNullException>(srcDict != null);
            Contract.Requires<ArgumentNullException>(keyComparer != null);

            _dictionary = new Dictionary<TKey, TValue>(srcDict, keyComparer);
        }

        /// <summary>
        /// Обёрнутый словарь
        /// </summary>
        protected Dictionary<TKey, TValue> Dictionary { get { return _dictionary; } }

        /// <summary>
        /// Количество элементов в словаре
        /// </summary>
        public int Count { get { return _dictionary.Count; } }

        /// <summary>
        /// Коллекция ключей
        /// </summary>
        public Dictionary<TKey, TValue>.KeyCollection Keys { get { return _dictionary.Keys; } }
        /// <summary>
        /// Коллекция значений
        /// </summary>
        public Dictionary<TKey, TValue>.ValueCollection Values { get { return _dictionary.Values; } }

        /// <summary>
        /// Доступ к элементам по ключу
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <returns>Элемент</returns>
        public TValue this[TKey key] 
        {
            get
            {
                return _dictionary[key];
            }
        }
        /// <summary>
        /// Получить значение по ключу
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="value">Полученное значение</param>
        /// <returns>Удалось ли получить значение</returns>
        public bool TryGetValue(TKey key, out TValue value)
        {
            return _dictionary.TryGetValue(key, out value);
        }

        /// <summary>
        /// Содержится ли ключ в словаре
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <returns>Содержится ли</returns>
        [Pure]
        public bool ContainsKey(TKey key)
        {
            return _dictionary.ContainsKey(key);
        }

        /// <summary>
        /// Содержится ли значение в словаре
        /// </summary>
        /// <param name="value">Значение</param>
        /// <returns>Содержится ли</returns>
        [Pure]
        public bool ContainsValue(TValue value)
        {
            return _dictionary.ContainsValue(value);
        }

        /// <summary>
        /// Получение Enumerator'а
        /// </summary>
        /// <returns>Enumerator</returns>
        public Dictionary<TKey, TValue>.Enumerator GetEnumerator()
        {
            return _dictionary.GetEnumerator();
        }


        #region Реализация интерфейсов


        /// <summary>
        /// Добавить в словарь (не поддерживается)
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="value">Значение</param>
        void IDictionary<TKey, TValue>.Add(TKey key, TValue value)
        {
            throw new NotSupportedException("Add is not supported for ReadOnlyDictionary");
        }

        /// <summary>
        /// Содержится ли ключ в словаре
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <returns>Содержится ли</returns>
        bool IDictionary<TKey, TValue>.ContainsKey(TKey key)
        {
            return _dictionary.ContainsKey(key);
        }

        /// <summary>
        /// Коллекция ключей
        /// </summary>
        ICollection<TKey> IDictionary<TKey, TValue>.Keys
        {
            get { return _dictionary.Keys; }
        }

        /// <summary>
        /// Удалить из словаря (не поддерживается)
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <returns>Удалился ли</returns>
        bool IDictionary<TKey, TValue>.Remove(TKey key)
        {
            throw new NotSupportedException("Remove is not supported for ReadOnlyDictionary");
        }

        /// <summary>
        /// Получить значение по ключу
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="value">Извлечённое значение</param>
        /// <returns>Было ли значение в словаре</returns>
        bool IDictionary<TKey, TValue>.TryGetValue(TKey key, out TValue value)
        {
            return _dictionary.TryGetValue(key, out value);
        }

        /// <summary>
        /// Коллекция значений
        /// </summary>
        ICollection<TValue> IDictionary<TKey, TValue>.Values
        {
            get { return _dictionary.Values; }
        }

        /// <summary>
        /// Доступ к элементам словаря по ключу
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <returns>Значение</returns>
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
        /// Добавить элемент в коллекцию (не поддерживается)
        /// </summary>
        /// <param name="item">Элемент</param>
        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
        {
            throw new NotSupportedException("Add is not supported for ReadOnlyDictionary");
        }

        /// <summary>
        /// Оичтить словарь (не поддерживается)
        /// </summary>
        void ICollection<KeyValuePair<TKey, TValue>>.Clear()
        {
            throw new NotSupportedException("Clear is not supported for ReadOnlyDictionary");
        }

        /// <summary>
        /// Содержится ли элемент в словаре
        /// </summary>
        /// <param name="item">Элемент</param>
        /// <returns>Содержится ли</returns>
        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
        {
            return (_dictionary as ICollection<KeyValuePair<TKey, TValue>>).Contains(item);
        }

        /// <summary>
        /// Скопировать данные словаря в массив
        /// </summary>
        /// <param name="array">Массив</param>
        /// <param name="arrayIndex">Индекс, с которого начинается вставка</param>
        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            (_dictionary as ICollection<KeyValuePair<TKey, TValue>>).CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Количество элементов в словаре
        /// </summary>
        int ICollection<KeyValuePair<TKey, TValue>>.Count
        {
            get { return _dictionary.Count; }
        }

        /// <summary>
        /// Только для чтения
        /// </summary>
        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly
        {
            get { return true; }
        }

        /// <summary>
        /// Удалить элемент (не поддерживается)
        /// </summary>
        /// <param name="item">Элемент</param>
        /// <returns>Удалился ли</returns>
        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        {
            throw new NotSupportedException("Remove is not supported for ReadOnlyDictionary");
        }

        /// <summary>
        /// Получение Enumerator'а
        /// </summary>
        /// <returns>Enumerator</returns>
        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        {
            return _dictionary.GetEnumerator();
        }
        /// <summary>
        /// Получение Enumerator'а
        /// </summary>
        /// <returns>Enumerator</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _dictionary.GetEnumerator();
        }

        /// <summary>
        /// Добавить в словарь (не поддерживается)
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="value">Значение</param>
        void IDictionary.Add(object key, object value)
        {
            (_dictionary as IDictionary).Add(key, value);
        }

        /// <summary>
        /// Очистить словарь (не поддерживается)
        /// </summary>
        void IDictionary.Clear()
        {
            throw new NotSupportedException("Clear is not supported for ReadOnlyDictionary");
        }

        /// <summary>
        /// Содержит ли словарь ключ
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <returns>Содержит ли</returns>
        bool IDictionary.Contains(object key)
        {
            return (_dictionary as IDictionary).Contains(key);
        }

        /// <summary>
        /// Получение Enumerator'а
        /// </summary>
        /// <returns>Enumerator</returns>
        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            return (_dictionary as IDictionary).GetEnumerator();
        }

        /// <summary>
        /// Фиксирован ли размер
        /// </summary>
        bool IDictionary.IsFixedSize
        {
            get { return false; }
        }

        /// <summary>
        /// Только для чтения
        /// </summary>
        bool IDictionary.IsReadOnly
        {
            get { return true; }
        }

        /// <summary>
        /// Коллекция ключей
        /// </summary>
        ICollection IDictionary.Keys
        {
            get { return _dictionary.Keys; }
        }

        /// <summary>
        /// Удалить элемент из словаря по ключу (не поддерживается)
        /// </summary>
        /// <param name="key">Ключ</param>
        void IDictionary.Remove(object key)
        {
            throw new NotSupportedException("Remove is not supported for ReadOnlyDictionary");
        }

        /// <summary>
        /// Коллекция значений
        /// </summary>
        ICollection IDictionary.Values
        {
            get { return _dictionary.Values; }
        }

        /// <summary>
        /// Доступ к элементам словаря по ключу
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <returns>Значение</returns>
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
        /// Скопировать содержимое в массив
        /// </summary>
        /// <param name="array">Массив</param>
        /// <param name="index">Стартовый индекс</param>
        void ICollection.CopyTo(Array array, int index)
        {
            (_dictionary as ICollection).CopyTo(array, index);
        }

        /// <summary>
        /// Количество элементов в словаре
        /// </summary>
        int ICollection.Count
        {
            get { return _dictionary.Count; }
        }


        /// <summary>
        /// Синхронизирован ли доступ
        /// </summary>
        bool ICollection.IsSynchronized
        {
            get { return false; }
        }

        /// <summary>
        /// Объект синхронизации
        /// </summary>
        object ICollection.SyncRoot
        {
            get { return (_dictionary as ICollection).SyncRoot; }
        }

        /// <summary>
        /// Содержит ли словарь ключ
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <returns>Содержит ли</returns>
        bool IReadOnlyDictionary<TKey, TValue>.ContainsKey(TKey key)
        {
            return _dictionary.ContainsKey(key);
        }

        /// <summary>
        /// Перечень ключей
        /// </summary>
        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys
        {
            get { return _dictionary.Keys; }
        }

        /// <summary>
        /// Получить значение по ключу
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="value">Извлечённое значение</param>
        /// <returns>Было ли значение в словаре</returns>
        bool IReadOnlyDictionary<TKey, TValue>.TryGetValue(TKey key, out TValue value)
        {
            return _dictionary.TryGetValue(key, out value);
        }

        /// <summary>
        /// Перечень элементов словаря
        /// </summary>
        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values
        {
            get { return _dictionary.Values; }
        }

        /// <summary>
        /// Доступ к элементам словаря по ключу
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <returns>Значение</returns>
        TValue IReadOnlyDictionary<TKey, TValue>.this[TKey key]
        {
            get { return _dictionary[key]; }
        }

        /// <summary>
        /// Количество элементов в словаре
        /// </summary>
        int IReadOnlyCollection<KeyValuePair<TKey, TValue>>.Count
        {
            get { return _dictionary.Count; }
        }

        #endregion
    }
}
