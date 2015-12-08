using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Collections
{
    /// <summary>
    /// Read-only wrapper around IList interface
    /// </summary>
    /// <typeparam name="T">The type of the element in the list</typeparam>
    [Serializable]
    [System.Diagnostics.DebuggerDisplay("Count = {Count}")]
    [System.Diagnostics.DebuggerTypeProxy(typeof(Qoollo.Turbo.Collections.ServiceStuff.CollectionDebugView<>))]
    public class ReadOnlyListWrapper<T> : ReadOnlyCollection<T>
    {
        private static readonly ReadOnlyListWrapper<T> _empty = new ReadOnlyListWrapper<T>(new T[0]);
        /// <summary>
        /// Empty ReadOnlyListWrapper
        /// </summary>
        public static ReadOnlyListWrapper<T> Empty
        {
            get
            {
                return _empty;
            }
        }

        // ===========

        /// <summary>
        /// ReadOnlyListWrapper constructor
        /// </summary>
        /// <param name="list">List to be wrapped</param>
        public ReadOnlyListWrapper(IList<T> list)
            : base(list)
        {
            Contract.Requires<ArgumentNullException>(list != null);
        }


        /// <summary>
        /// Searches for the specified 'item' and returns the index of the last occurrence of the item inside list
        /// </summary>
        /// <param name="item">The item to locate inside the list</param>
        /// <returns>The index of element inside the list, if found. -1 otherwise</returns>
        [Pure]
        public int LastIndexOf(T item)
        {
            var comparer = EqualityComparer<T>.Default;
            for (int i = Items.Count - 1; i >= 0; i--)
            {
                if (comparer.Equals(Items[i], item))
                    return i;
            }

            return -1;
        }


        /// <summary>
        /// Performs the specified action on each element of the list
        /// </summary>
        /// <param name="action">Action</param>
        public void ForEach(Action<T> action)
        {
            Contract.Requires<ArgumentNullException>(action != null);

            for (int i = 0; i < Items.Count; i++)
                action(Items[i]);
        }


        /// <summary>
        /// Determines whether the list contains elements that match the conditions defined by the specified predicate
        /// </summary>
        /// <param name="match">Predicate</param>
        /// <returns>True if the list contains elements that match the condition</returns>
        [Pure]
        public bool Exists(Predicate<T> match)
        {
            Contract.Requires<ArgumentNullException>(match != null);

            for (int i = 0; i < Items.Count; i++)
                if (match(Items[i]))
                    return true;

            return false;
        }

        /// <summary>
        /// Search for the fist element that match the conditions defined by the specified predicate
        /// </summary>
        /// <param name="match">Predicate</param>
        /// <returns>The first element that matches the condition, if found; otherwise, the default value for type T</returns>
        [Pure]
        public T Find(Predicate<T> match)
        {
            Contract.Requires<ArgumentNullException>(match != null);

            for (int i = 0; i < Items.Count; i++)
            {
                var curItem = Items[i];
                if (match(curItem))
                    return curItem;
            }

            return default(T);
        }


        /// <summary>
        /// Creates TransformedReadOnlyListWrapper from the current list
        /// </summary>
        /// <typeparam name="TOut">Type of the elements of the newly created TransformedReadOnlyListWrapper</typeparam>
        /// <param name="selector">Element conversion delegate</param>
        /// <returns>Created TransformedReadOnlyListWrapper</returns>
        public TransformedReadOnlyListWrapper<T, TOut> AsTransformedReadOnlyList<TOut>(Func<T, TOut> selector)
        {
            Contract.Requires(selector != null);

            return new TransformedReadOnlyListWrapper<T, TOut>(Items, selector);
        }
    }
}
