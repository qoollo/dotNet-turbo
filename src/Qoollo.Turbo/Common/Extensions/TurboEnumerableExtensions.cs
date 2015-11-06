using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics.Contracts;

namespace System.Linq
{
    /// <summary>
    /// Additional LINQ to Object extension methods
    /// </summary>
    public static class TurboEnumerableExtensions
    {
        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified predicate and returns its index
        /// </summary>
        /// <typeparam name="T">The type of the objects in collection</typeparam>
        /// <param name="collection">Collection</param>
        /// <param name="predicate">Predicate</param>
        /// <returns>The index of element inside the collection, if found. -1 otherwise</returns>
        [Pure]
        [Obsolete("Method was renamed. Consider to use FindIndex instead")]
        public static int FindPosition<T>(this IEnumerable<T> collection, Func<T, bool> predicate)
        {
            return FindIndex(collection, new Predicate<T>(predicate));
        }


        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified predicate and returns its index
        /// </summary>
        /// <typeparam name="T">The type of the objects in collection</typeparam>
        /// <param name="collection">Collection</param>
        /// <param name="predicate">Predicate</param>
        /// <returns>The index of element inside the collection, if found. -1 otherwise</returns>
        [Pure]
        public static int FindIndex<T>(this IEnumerable<T> collection, Predicate<T> predicate)
        {
            Contract.Requires(collection != null);
            Contract.Requires(predicate != null);

            Contract.Ensures(Contract.Result<int>() >= -1);

            if (collection == null)
                throw new ArgumentNullException("collection");

            if (predicate == null)
                throw new ArgumentNullException("predicate");

            IList<T> iList = collection as IList<T>;
            if (iList != null)
            {
                List<T> list = collection as List<T>;
                if (list != null)
                    return list.FindIndex(predicate);

                T[] array = collection as T[];
                if (array != null)
                    return Array.FindIndex(array, predicate);

                int count = iList.Count;
                for (int i = 0; i < count; i++)
                    if (predicate(iList[i]))
                        return i;

                return -1;
            }


            int index = 0;
            foreach (T elem in collection)
            {
                if (predicate(elem))
                    return index;
                index++;
            }

            return -1;
        }
    }
}
