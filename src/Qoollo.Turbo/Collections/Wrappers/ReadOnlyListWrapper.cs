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
    /// Обёртка списков в режим только для чтения
    /// </summary>
    /// <typeparam name="T">Тип элемента</typeparam>
    [Serializable]
    [System.Diagnostics.DebuggerDisplay("Count = {Count}")]
    [System.Diagnostics.DebuggerTypeProxy(typeof(Qoollo.Turbo.Collections.ServiceStuff.CollectionDebugView<>))]
    public class ReadOnlyListWrapper<T> : ReadOnlyCollection<T>
    {
        private static readonly ReadOnlyListWrapper<T> _empty = new ReadOnlyListWrapper<T>(new T[0]);
        /// <summary>
        /// Пустой список
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
        /// Конструктор ReadOnlyListWrapper
        /// </summary>
        /// <param name="list">Обёртываемый список</param>
        public ReadOnlyListWrapper(IList<T> list)
            : base(list)
        {
            Contract.Requires<ArgumentNullException>(list != null);
        }


        /// <summary>
        /// Позиция последнего элемента в списке
        /// </summary>
        /// <param name="item">Элемент</param>
        /// <returns>Позиция, если найден, иначе -1</returns>
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
        /// Выполнение действия для каждого элемента списка
        /// </summary>
        /// <param name="action">Действие</param>
        public void ForEach(Action<T> action)
        {
            Contract.Requires(action != null);

            for (int i = 0; i < Items.Count; i++)
                action(Items[i]);
        }


        /// <summary>
        /// Получить список с преобразованием элементов на лету
        /// </summary>
        /// <typeparam name="TOut">Выходной тип элементов</typeparam>
        /// <param name="selector">Преобразователь</param>
        /// <returns>Список</returns>
        public TransformedReadOnlyListWrapper<T, TOut> AsTransformedReadOnlyList<TOut>(Func<T, TOut> selector)
        {
            Contract.Requires(selector != null);

            return new TransformedReadOnlyListWrapper<T, TOut>(Items, selector);
        }
    }
}
