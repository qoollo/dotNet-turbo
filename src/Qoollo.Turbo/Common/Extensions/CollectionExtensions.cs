using Qoollo.Turbo.Collections;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Collections.Generic
{
    /// <summary>
    /// Расширения для стандартных коллекций
    /// </summary>
    public static class CollectionExtensions
    {
        /// <summary>
        /// Содержится ли ключ в коллекции ключей словаря
        /// </summary>
        /// <typeparam name="TKey">Тип ключа</typeparam>
        /// <typeparam name="TValue">Тип значения</typeparam>
        /// <param name="dictKeyCol">Коллекция ключей</param>
        /// <param name="item">Проверяемое значение</param>
        /// <returns>Содержится ли</returns>
        public static bool Contains<TKey, TValue>(this Dictionary<TKey, TValue>.KeyCollection dictKeyCol, TKey item)
        {
            Contract.Requires(dictKeyCol != null);

            return (dictKeyCol as ICollection<TKey>).Contains(item);
        }

        /// <summary>
        /// Обернуть список в список ReadOnlyList только для чтения
        /// </summary>
        /// <typeparam name="T">Тип элементов</typeparam>
        /// <param name="list">Список</param>
        /// <returns>Обёрнутый список только для чтения</returns>
        public static ReadOnlyList<T> AsReadOnlyList<T>(this List<T> list)
        {
            Contract.Requires(list != null);
            Contract.Ensures(Contract.Result<ReadOnlyList<T>>() != null);

            return new ReadOnlyList<T>(list);
        }

        /// <summary>
        /// Обернуть список в список ReadOnlyListWrapper только для чтения
        /// </summary>
        /// <typeparam name="T">Тип элементов</typeparam>
        /// <param name="list">Список</param>
        /// <returns>Обёрнутый список только для чтения</returns>
        public static ReadOnlyListWrapper<T> AsReadOnlyListWrapper<T>(this IList<T> list)
        {
            Contract.Requires(list != null);
            Contract.Ensures(Contract.Result<ReadOnlyListWrapper<T>>() != null);

            return new ReadOnlyListWrapper<T>(list);
        }

        /// <summary>
        /// Обернуть коллекцию в ReadOnlyCollectionWrapper только для чтения
        /// </summary>
        /// <typeparam name="T">Тип элементов</typeparam>
        /// <param name="col">Коллекция</param>
        /// <returns>Обёрнутая коллекция только для чтения</returns>
        public static ReadOnlyCollectionWrapper<T> AsReadOnlyCollectionWrapper<T>(this ICollection<T> col)
        {
            Contract.Requires(col != null);
            Contract.Ensures(Contract.Result<ReadOnlyCollectionWrapper<T>>() != null);

            return new ReadOnlyCollectionWrapper<T>(col);
        }

        /// <summary>
        /// Обернуть множество в ReadOnlySetWrapper
        /// </summary>
        /// <typeparam name="T">Тип элементов</typeparam>
        /// <param name="set">Множество</param>
        /// <returns>Обёрнутое множество только для чтения</returns>
        public static ReadOnlySetWrapper<T> AsReadOnlySetWrapper<T>(this ISet<T> set)
        {
            Contract.Requires(set != null);
            Contract.Ensures(Contract.Result<ReadOnlySetWrapper<T>>() != null);

            return new ReadOnlySetWrapper<T>(set);
        }

        /// <summary>
        /// Обернуть множество в ReadOnlyHashSet
        /// </summary>
        /// <typeparam name="T">Тип элементов</typeparam>
        /// <param name="set">Множество</param>
        /// <returns>Обёрнутое множество только для чтения</returns>
        public static ReadOnlyHashSet<T> AsReadOnlyHashSet<T>(this HashSet<T> set)
        {
            Contract.Requires(set != null);
            Contract.Ensures(Contract.Result<ReadOnlyHashSet<T>>() != null);

            return new ReadOnlyHashSet<T>(set);
        }
        
        /// <summary>
        /// Обернуть словарь в ReadOnlyDictionaryWrapper
        /// </summary>
        /// <typeparam name="TKey">Тип ключа</typeparam>
        /// <typeparam name="TValue">Тип значения</typeparam>
        /// <param name="dict">Словарь</param>
        /// <returns>Обёрнутый словарь</returns>
        public static ReadOnlyDictionaryWrapper<TKey, TValue> AsReadOnlyDictionaryWrapper<TKey, TValue>(this IDictionary<TKey, TValue> dict)
        {
            Contract.Requires(dict != null);
            Contract.Ensures(Contract.Result<ReadOnlyDictionaryWrapper<TKey, TValue>>() != null);

            return new ReadOnlyDictionaryWrapper<TKey, TValue>(dict);
        }

        /// <summary>
        /// Обернуть словарь в ReadOnlyDictionary
        /// </summary>
        /// <typeparam name="TKey">Тип ключа</typeparam>
        /// <typeparam name="TValue">Тип значения</typeparam>
        /// <param name="dict">Словарь</param>
        /// <returns>Обёрнутый словарь</returns>
        public static ReadOnlyDictionary<TKey, TValue> AsReadOnlyDictionary<TKey, TValue>(this Dictionary<TKey, TValue> dict)
        {
            Contract.Requires(dict != null);
            Contract.Ensures(Contract.Result<ReadOnlyDictionary<TKey, TValue>>() != null);
       
            return new ReadOnlyDictionary<TKey, TValue>(dict);
        }


        /// <summary>
        /// Получить список с преобразованием элементов на лету
        /// </summary>
        /// <typeparam name="TIn">Исходный тип элементов</typeparam>
        /// <typeparam name="TOut">Выходной тип элементов</typeparam>
        /// <param name="list">Исходный список</param>
        /// <param name="selector">Преобразователь</param>
        /// <returns>Список</returns>
        public static TransformedReadOnlyListWrapper<TIn, TOut> AsTransformedReadOnlyList<TIn, TOut>(this IList<TIn> list, Func<TIn, TOut> selector)
        {
            Contract.Requires(list != null);
            Contract.Requires(selector != null);
            Contract.Ensures(Contract.Result<TransformedReadOnlyListWrapper<TIn, TOut>>() != null);

            return new TransformedReadOnlyListWrapper<TIn, TOut>(list, selector);
        }
    }
}
