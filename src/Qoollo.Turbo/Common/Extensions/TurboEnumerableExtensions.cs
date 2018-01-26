using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Qoollo.Turbo;

namespace System.Linq
{
    /// <summary>
    /// Additional LINQ to Object extension methods
    /// </summary>
    [Obsolete("Class was renamed to TurboEnumerableExtensions", true)]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class EnumerableExtensions
    {
    }

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
        [Obsolete("Method was renamed. Consider to use FindIndex instead", true)]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
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
            TurboContract.Ensures(TurboContract.Result<int>() >= -1);

            if (collection == null)
                throw new ArgumentNullException(nameof(collection));
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            if (collection is IList<T> iList)
            {
                if (collection is List<T> list)
                    return list.FindIndex(predicate);

                if (collection is T[] array)
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


        /// <summary>
        /// Returns the maximum element in sequence using custom user 'comparer'
        /// </summary>
        /// <typeparam name="TSource">The type of the elements</typeparam>
        /// <param name="source">A sequence of values</param>
        /// <param name="comparer">Comparer to compare elements from sequence</param>
        /// <returns>The maximum value in the sequence</returns>
        internal static TSource Max<TSource>(this IEnumerable<TSource> source, IComparer<TSource> comparer)
        {
            if (comparer == null)
                return Enumerable.Max(source);

            if (source == null)
                throw new ArgumentNullException(nameof(source));

            using (var enumerator = source.GetEnumerator())
            {
                if (!enumerator.MoveNext())
                    throw new InvalidOperationException("Sequence contains no elements");

                var max = enumerator.Current;
                while (enumerator.MoveNext())
                {
                    var current = enumerator.Current;
                    if (comparer.Compare(current, max) > 0)
                        max = current;
                }

                return max;
            }
        }



        /// <summary>
        /// Returns the minimum element in sequence using custom user 'comparer'
        /// </summary>
        /// <typeparam name="TSource">The type of the elements</typeparam>
        /// <param name="source">A sequence of values</param>
        /// <param name="comparer">Comparer to compare elements from sequence</param>
        /// <returns>The minimum value in the sequence</returns>
        internal static TSource Min<TSource>(this IEnumerable<TSource> source, IComparer<TSource> comparer)
        {
            if (comparer == null)
                return Enumerable.Min(source);

            if (source == null)
                throw new ArgumentNullException(nameof(source));

            using (var enumerator = source.GetEnumerator())
            {
                if (!enumerator.MoveNext())
                    throw new InvalidOperationException("Sequence contains no elements");

                var min = enumerator.Current;
                while (enumerator.MoveNext())
                {
                    var current = enumerator.Current;
                    if (comparer.Compare(current, min) < 0)
                        min = current;
                }

                return min;
            }
        }





        /// <summary>
        /// Returns the element with maximum key from sequence using custom user 'comparer'
        /// </summary>
        /// <typeparam name="TSource">The type of the elements</typeparam>
        /// <typeparam name="TKey">The type of the key</typeparam>
        /// <param name="source">A sequence of values</param>
        /// <param name="keySelector">Selector to extract key from element</param>
        /// <param name="comparer">Comparer to compare keys for elements from sequence</param>
        /// <returns>The maximum value in the sequence</returns>
        public static TSource MaxBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey> comparer)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (comparer == null)
                comparer = Comparer<TKey>.Default;

            using (var enumerator = source.GetEnumerator())
            {
                if (!enumerator.MoveNext())
                    throw new InvalidOperationException("Sequence contains no elements");

                var max = enumerator.Current;
                var maxKey = keySelector(max);
                while (enumerator.MoveNext())
                {
                    var current = enumerator.Current;
                    var currentKey = keySelector(current);
                    if (comparer.Compare(currentKey, maxKey) > 0)
                    {
                        max = current;
                        maxKey = currentKey;
                    }
                }

                return max;
            }
        }

        /// <summary>
        /// Returns the element with maximum key from sequence
        /// </summary>
        /// <typeparam name="TSource">The type of the elements</typeparam>
        /// <typeparam name="TKey">The type of the key</typeparam>
        /// <param name="source">A sequence of values</param>
        /// <param name="keySelector">Selector to extract key from element</param>
        /// <returns>The value with maximum key in the sequence</returns>
        public static TSource MaxBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            return MaxBy(source, keySelector, null);
        }



        /// <summary>
        /// Returns the element with minimum key from sequence using custom user 'comparer'
        /// </summary>
        /// <typeparam name="TSource">The type of the elements</typeparam>
        /// <typeparam name="TKey">The type of the key</typeparam>
        /// <param name="source">A sequence of values</param>
        /// <param name="keySelector">Selector to extract key from element</param>
        /// <param name="comparer">Comparer to compare keys for elements from sequence</param>
        /// <returns>The minimum value in the sequence</returns>
        public static TSource MinBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey> comparer)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (comparer == null)
                comparer = Comparer<TKey>.Default;

            using (var enumerator = source.GetEnumerator())
            {
                if (!enumerator.MoveNext())
                    throw new InvalidOperationException("Sequence contains no elements");

                var min = enumerator.Current;
                var minKey = keySelector(min);
                while (enumerator.MoveNext())
                {
                    var current = enumerator.Current;
                    var currentKey = keySelector(current);
                    if (comparer.Compare(currentKey, minKey) < 0)
                    {
                        min = current;
                        minKey = currentKey;
                    }
                }

                return min;
            }
        }

        /// <summary>
        /// Returns the element with minimum key from sequence
        /// </summary>
        /// <typeparam name="TSource">The type of the elements</typeparam>
        /// <typeparam name="TKey">The type of the key</typeparam>
        /// <param name="source">A sequence of values</param>
        /// <param name="keySelector">Selector to extract key from element</param>
        /// <returns>The value with minimum key in the sequence</returns>
        public static TSource MinBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            return MinBy(source, keySelector, null);
        }
    }
}
