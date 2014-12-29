using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics.Contracts;

namespace System.Linq
{
    /// <summary>
    /// Дополнительные функции LINQ to Object
    /// </summary>
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Поиск позиции элемента в коллекции
        /// </summary>
        /// <typeparam name="T">Тип элементов</typeparam>
        /// <param name="collection">Коллекция</param>
        /// <param name="predicate">Предикат сравнения</param>
        /// <returns>Индекс первого подошедшего элемента. -1, если не найден</returns>
        [Pure]
        public static int FindPosition<T>(this IEnumerable<T> collection, Func<T, bool> predicate)
        {
            Contract.Requires(collection != null);
            Contract.Requires(predicate != null);

            Contract.Ensures(Contract.Result<int>() >= -1);

            if (collection == null)
                throw new ArgumentNullException("collection");

            if (predicate == null)
                throw new ArgumentNullException("predicate");

            int index = 0;

            if (collection is IList<T>)
            {
                IList<T> lcol = collection as IList<T>;
                for (index = 0; index < lcol.Count; index++)
                    if (predicate(lcol[index]))
                        return index;

                return -1;
            }

            index = 0;
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
