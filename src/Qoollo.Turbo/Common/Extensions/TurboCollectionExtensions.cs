using Qoollo.Turbo;
using Qoollo.Turbo.Collections;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Collections.Generic
{
    /// <summary>
    /// Extension methods for BCL collections
    /// </summary>
    public static class TurboCollectionExtensions
    {
        /// <summary>
        /// Determines whether the Dictionary KeyCollection contains a specific key
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in the dictionary</typeparam>
        /// <typeparam name="TValue">The type of the values in the dictionary</typeparam>
        /// <param name="dictKeyCol">Source Dictionary KeyCollection</param>
        /// <param name="item">The key to locate in the KeyCollection</param>
        /// <returns>True if the collection contains a key</returns>
        public static bool Contains<TKey, TValue>(this Dictionary<TKey, TValue>.KeyCollection dictKeyCol, TKey item)
        {
            TurboContract.Requires(dictKeyCol != null, conditionString: "dictKeyCol != null");

            return (dictKeyCol as ICollection<TKey>).Contains(item);
        }

        /// <summary>
        /// Wraps existed List with ReadOnlyList
        /// </summary>
        /// <typeparam name="T">The type of elements in the list</typeparam>
        /// <param name="list">Source list of items</param>
        /// <returns>ReadOnlyList that wraps specified List instance</returns>
        public static ReadOnlyList<T> AsReadOnlyList<T>(this List<T> list)
        {
            TurboContract.Requires(list != null, conditionString: "list != null");
            TurboContract.Ensures(TurboContract.Result<ReadOnlyList<T>>() != null);

            return new ReadOnlyList<T>(list);
        }

        /// <summary>
        /// Wraps existed IList collection with ReadOnlyListWrapper
        /// </summary>
        /// <typeparam name="T">The type of elements in the list</typeparam>
        /// <param name="list">Source list of items</param>
        /// <returns>ReadOnlyListWrapper that wraps specified IList instance</returns>
        public static ReadOnlyListWrapper<T> AsReadOnlyListWrapper<T>(this IList<T> list)
        {
            TurboContract.Requires(list != null, conditionString: "list != null");
            TurboContract.Ensures(TurboContract.Result<ReadOnlyListWrapper<T>>() != null);

            return new ReadOnlyListWrapper<T>(list);
        }

        /// <summary>
        /// Wraps existed ICollection with ReadOnlyCollectionWrapper
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection</typeparam>
        /// <param name="col">Source collection</param>
        /// <returns>ReadOnlyCollectionWrapper that wraps specified ICollection instance</returns>
        public static ReadOnlyCollectionWrapper<T> AsReadOnlyCollectionWrapper<T>(this ICollection<T> col)
        {
            TurboContract.Requires(col != null, conditionString: "col != null");
            TurboContract.Ensures(TurboContract.Result<ReadOnlyCollectionWrapper<T>>() != null);

            return new ReadOnlyCollectionWrapper<T>(col);
        }

        /// <summary>
        /// Wraps existed ISet with ReadOnlySetWrapper
        /// </summary>
        /// <typeparam name="T">The type of elements in the set</typeparam>
        /// <param name="set">Source set</param>
        /// <returns>ReadOnlySetWrapper that wraps specified ISet instance</returns>
        public static ReadOnlySetWrapper<T> AsReadOnlySetWrapper<T>(this ISet<T> set)
        {
            TurboContract.Requires(set != null, conditionString: "set != null");
            TurboContract.Ensures(TurboContract.Result<ReadOnlySetWrapper<T>>() != null);

            return new ReadOnlySetWrapper<T>(set);
        }

        /// <summary>
        /// Wraps existed Set instance with ReadOnlyHashSet
        /// </summary>
        /// <typeparam name="T">The type of elements in the set</typeparam>
        /// <param name="set">Source HashSet</param>
        /// <returns>ReadOnlyHashSet that wraps specified HashSet instance</returns>
        public static ReadOnlyHashSet<T> AsReadOnlyHashSet<T>(this HashSet<T> set)
        {
            TurboContract.Requires(set != null, conditionString: "set != null");
            TurboContract.Ensures(TurboContract.Result<ReadOnlyHashSet<T>>() != null);

            return new ReadOnlyHashSet<T>(set);
        }
        
        /// <summary>
        /// Wraps existed IDictionary instance with ReadOnlyDictionaryWrapper
        /// </summary>
        /// <typeparam name="TKey">The type of keys in the dictionary</typeparam>
        /// <typeparam name="TValue">The type of values in the dictionary</typeparam>
        /// <param name="dict">Source dictionary</param>
        /// <returns>ReadOnlyDictionaryWrapper that wraps specified IDictionary instance</returns>
        public static ReadOnlyDictionaryWrapper<TKey, TValue> AsReadOnlyDictionaryWrapper<TKey, TValue>(this IDictionary<TKey, TValue> dict)
        {
            TurboContract.Requires(dict != null, conditionString: "dict != null");
            TurboContract.Ensures(TurboContract.Result<ReadOnlyDictionaryWrapper<TKey, TValue>>() != null);

            return new ReadOnlyDictionaryWrapper<TKey, TValue>(dict);
        }

        /// <summary>
        /// Wraps existed Dictionary instance with ReadOnlyDictionary
        /// </summary>
        /// <typeparam name="TKey">The type of keys in the dictionary</typeparam>
        /// <typeparam name="TValue">The type of values in the dictionary</typeparam>
        /// <param name="dict">Source dictionary</param>
        /// <returns>ReadOnlyDictionary that wraps specified Dictionary instance</returns>
        public static ReadOnlyDictionary<TKey, TValue> AsReadOnlyDictionary<TKey, TValue>(this Dictionary<TKey, TValue> dict)
        {
            TurboContract.Requires(dict != null, conditionString: "dict != null");
            TurboContract.Ensures(TurboContract.Result<ReadOnlyDictionary<TKey, TValue>>() != null);
       
            return new ReadOnlyDictionary<TKey, TValue>(dict);
        }


        /// <summary>
        /// Creates TransformedReadOnlyListWrapper from the current IList instace with specified transformation
        /// </summary>
        /// <typeparam name="TIn">he type of the element in the source list</typeparam>
        /// <typeparam name="TOut">The output type of the element in the current transformed list</typeparam>
        /// <param name="list">Source list</param>
        /// <param name="selector">Transformation function that will be applied to source elements</param>
        /// <returns>Created TransformedReadOnlyListWrapper instance</returns>
        public static TransformedReadOnlyListWrapper<TIn, TOut> AsTransformedReadOnlyList<TIn, TOut>(this IList<TIn> list, Func<TIn, TOut> selector)
        {
            TurboContract.Requires(list != null, conditionString: "list != null");
            TurboContract.Requires(selector != null, conditionString: "selector != null");
            TurboContract.Ensures(TurboContract.Result<TransformedReadOnlyListWrapper<TIn, TOut>>() != null);

            return new TransformedReadOnlyListWrapper<TIn, TOut>(list, selector);
        }
    }
}
