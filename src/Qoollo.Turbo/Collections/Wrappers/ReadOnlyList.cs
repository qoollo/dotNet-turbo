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
    /// Список в режиме только для чтения
    /// </summary>
    /// <typeparam name="T">Тип элементов</typeparam>
    [System.Diagnostics.DebuggerDisplay("Count = {Count}")]
    [System.Diagnostics.DebuggerTypeProxy(typeof(Qoollo.Turbo.Collections.ServiceStuff.CollectionDebugView<>))]
    [Serializable]
    public class ReadOnlyList<T>: IList<T>, IReadOnlyList<T>, IList, IReadOnlyCollection<T>, ICollection<T>, ICollection, IEnumerable<T>, IEnumerable
    {
        private static readonly ReadOnlyList<T> _empty = new ReadOnlyList<T>(new List<T>());
        /// <summary>
        /// Пустой список
        /// </summary>
        public static ReadOnlyList<T> Empty
        {
            get
            {
                return _empty;
            }
        }

        // ===========

        private readonly List<T> _list;

        /// <summary>
        /// Контракты
        /// </summary>
        [ContractInvariantMethod]
        private void Invariant()
        {
            Contract.Invariant(_list != null);
        }

        /// <summary>
        /// Конструктор ReadOnlyList без параметров
        /// </summary>
        protected ReadOnlyList()
        {
            _list = new List<T>();
        }

        /// <summary>
        /// Конструктор ReadOnlyList
        /// </summary>
        /// <param name="list">Обёртываемый список</param>
        public ReadOnlyList(List<T> list)
        {
            Contract.Requires<ArgumentNullException>(list != null);

            _list = list;
        }

        /// <summary>
        /// Конструктор ReadOnlyList из перечня данных
        /// </summary>
        /// <param name="data">Данные</param>
        public ReadOnlyList(IEnumerable<T> data)
        {
            Contract.Requires<ArgumentNullException>(data != null);

            _list = new List<T>(data);
        }



        /// <summary>
        /// Обёртываемый список
        /// </summary>
        protected List<T> Items { get { return _list; } }


        /// <summary>
        /// Позиция первого включения элемента в список
        /// </summary>
        /// <param name="item">Элемент</param>
        /// <returns>Позиция, если найден, иначе -1</returns>
        [Pure]
        public int IndexOf(T item)
        {
            return _list.IndexOf(item);
        }

        /// <summary>
        /// Позиция элемента в списке (поиск идёт с конца)
        /// </summary>
        /// <param name="item">Элемент</param>
        /// <returns>Позиция, если найден, иначе -1</returns>
        [Pure]
        public int LastIndexOf(T item)
        {
            return _list.LastIndexOf(item);
        }

        /// <summary>
        /// Получение элемента списка по индексу
        /// </summary>
        /// <param name="index">Индекс</param>
        /// <returns>Элемент</returns>
        public T this[int index]
        {
            get
            {
                Contract.Requires(index >= 0);
                Contract.Requires(index < this.Count);

                return _list[index];
            }
        }

        /// <summary>
        /// Проверка наличия элемента в коллекции
        /// </summary>
        /// <param name="item">Элемент</param>
        /// <returns>Есть ли он в коллекции</returns>
        [Pure]
        public bool Contains(T item)
        {
            return _list.Contains(item);
        }

        /// <summary>
        /// Копирование коллекции в массив начиная с заданного индекса
        /// </summary>
        /// <param name="array">Массив, в который копируем</param>
        /// <param name="arrayIndex">Индекс внутри массива</param>
        public void CopyTo(T[] array, int arrayIndex)
        {
            Contract.Requires(array != null);
            Contract.Requires(arrayIndex >= 0);
            Contract.Requires(arrayIndex <= array.Length - this.Count);

            _list.CopyTo(array, arrayIndex);
        }
        /// <summary>
        /// Число элементов в списке
        /// </summary>
        public int Count
        {
            get { return _list.Count; }
        }

        /// <summary>
        /// Получение Enumerator
        /// </summary>
        /// <returns>Enumerator</returns>
        public List<T>.Enumerator GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        /// <summary>
        /// Существует ли в списке хоть один элемент, для которого пердикат вернёт true
        /// </summary>
        /// <param name="match">Предикат</param>
        /// <returns>Существование элемента</returns>
        [Pure]
        public bool Exists(Predicate<T> match)
        {
            Contract.Requires(match != null);

            return _list.Exists(match);
        }

        /// <summary>
        /// Конвертация в массив
        /// </summary>
        /// <returns>Массив</returns>
        public T[] ToArray()
        {
            Contract.Ensures(Contract.Result<T[]>() != null);

            return _list.ToArray();
        }
        /// <summary>
        /// Выполнение действия для каждого элемента списка
        /// </summary>
        /// <param name="action">Действие</param>
        public void ForEach(Action<T> action)
        {
            Contract.Requires(action != null);

            _list.ForEach(action);
        }

        /// <summary>
        /// Выполняется ли предикат для всех элементов списка
        /// </summary>
        /// <param name="match">Предикат</param>
        /// <returns>Выполняется ли</returns>
        [Pure]
        public bool TrueForAll(Predicate<T> match)
        {
            Contract.Requires(match != null);

            return _list.TrueForAll(match);
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
            Contract.Ensures(Contract.Result<TransformedReadOnlyListWrapper<T, TOut>>() != null);

            return new TransformedReadOnlyListWrapper<T, TOut>(_list, selector);
        }

        #region Реализация интерфейсов

        /// <summary>
        /// Индекс элемента в списке
        /// </summary>
        /// <param name="item">Элемент</param>
        /// <returns>Индекс, если присутствует, иначе -1</returns>
        int IList<T>.IndexOf(T item)
        {
            return _list.IndexOf(item);
        }

        /// <summary>
        /// Вставка элемента в список (не поддерживается)
        /// </summary>
        /// <param name="index">Индекс</param>
        /// <param name="item">Элемент</param>
        void IList<T>.Insert(int index, T item)
        {
            throw new NotSupportedException("Insert is not supported for ReadOnlyList");
        }

        /// <summary>
        /// Удаление элемента из списка (не поддерживается)
        /// </summary>
        /// <param name="index">Индекс элемента</param>
        void IList<T>.RemoveAt(int index)
        {
            throw new NotSupportedException("RemoveAt is not supported for ReadOnlyList");
        }

        /// <summary>
        /// Доступ к элементам по индексу
        /// </summary>
        /// <param name="index">Индекс</param>
        /// <returns>Элемент</returns>
        T IList<T>.this[int index]
        {
            get
            {
                return _list[index];
            }
            set
            {
                throw new NotSupportedException("Items.Set is not supported for ReadOnlyList");
            }
        }

        /// <summary>
        /// Добавление элемента в конец (не поддерживается)
        /// </summary>
        /// <param name="item">Элемент</param>
        void ICollection<T>.Add(T item)
        {
            throw new NotSupportedException("Add is not supported for ReadOnlyList");
        }

        /// <summary>
        /// Оичтка списка (не поддерживается)
        /// </summary>
        void ICollection<T>.Clear()
        {
            throw new NotSupportedException("Clear is not supported for ReadOnlyList");
        }

        /// <summary>
        /// Содержится ли элемент в списке
        /// </summary>
        /// <param name="item">Элемент</param>
        /// <returns>Содержится ли</returns>
        bool ICollection<T>.Contains(T item)
        {
            return _list.Contains(item);
        }

        /// <summary>
        /// Скопировать данные списка в массив
        /// </summary>
        /// <param name="array">Массив</param>
        /// <param name="arrayIndex">Индекс, с которого начинается вставка</param>
        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Количество элементов в списке
        /// </summary>
        int ICollection<T>.Count
        {
            get { return _list.Count; }
        }

        /// <summary>
        /// Только для чтения
        /// </summary>
        bool ICollection<T>.IsReadOnly
        {
            get { return true; }
        }

        /// <summary>
        /// Удалить элемент (не поддерживается)
        /// </summary>
        /// <param name="item">Элемент</param>
        /// <returns>Удалился ли</returns>
        bool ICollection<T>.Remove(T item)
        {
            throw new NotSupportedException("Remove is not supported for ReadOnlyList");
        }
        /// <summary>
        /// Получение Enumerator'а
        /// </summary>
        /// <returns>Enumerator</returns>
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return _list.GetEnumerator();
        }
        /// <summary>
        /// Получение Enumerator'а
        /// </summary>
        /// <returns>Enumerator</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _list.GetEnumerator();
        }
        /// <summary>
        /// Доступ к элементам списка по индексу
        /// </summary>
        /// <param name="index">Индекс</param>
        /// <returns>Элемент</returns>
        T IReadOnlyList<T>.this[int index]
        {
            get { return _list[index]; }
        }

        /// <summary>
        /// Количество элементов в списке
        /// </summary>
        int IReadOnlyCollection<T>.Count
        {
            get { return _list.Count; }
        }

        /// <summary>
        /// Добавить элемент в список (не поддерживается)
        /// </summary>
        /// <param name="value">Элемент</param>
        /// <returns>Позиция</returns>
        int IList.Add(object value)
        {
            throw new NotSupportedException("Add is not supported for ReadOnlyList");
        }

        /// <summary>
        /// Очистить список (не поддерживается)
        /// </summary>
        void IList.Clear()
        {
            throw new NotSupportedException("Clear is not supported for ReadOnlyList");
        }

        /// <summary>
        /// Сожержит ли список элемент
        /// </summary>
        /// <param name="value">Элемент</param>
        /// <returns>Содержит ли</returns>
        bool IList.Contains(object value)
        {
            return (_list as IList).Contains(value);
        }

        /// <summary>
        /// Индекс элемента в списке
        /// </summary>
        /// <param name="value">Элемент</param>
        /// <returns>Индекс, если есть, иначе -1</returns>
        int IList.IndexOf(object value)
        {
            return (_list as IList).IndexOf(value);
        }

        /// <summary>
        /// Вставка элемента в список (не поддерживается)
        /// </summary>
        /// <param name="index">Индекс</param>
        /// <param name="value">Значение</param>
        void IList.Insert(int index, object value)
        {
            throw new NotSupportedException("Insert is not supported for ReadOnlyList");
        }

        /// <summary>
        /// Фиксирован ли размер
        /// </summary>
        bool IList.IsFixedSize
        {
            get { return false; }
        }

        /// <summary>
        /// Только для чтения
        /// </summary>
        bool IList.IsReadOnly
        {
            get { return true; }
        }

        /// <summary>
        /// Удалить элемент из списка (не поддерживается)
        /// </summary>
        /// <param name="value">Элемент</param>
        void IList.Remove(object value)
        {
            throw new NotSupportedException("Remove is not supported for ReadOnlyList");
        }
        /// <summary>
        /// Удалить элемент из списка в определённой позиции (не поддерживается)
        /// </summary>
        /// <param name="index">Индекс</param>
        void IList.RemoveAt(int index)
        {
            throw new NotSupportedException("RemoveAt is not supported for ReadOnlyList");
        }

        /// <summary>
        /// Доступ к элементам списка по индексу
        /// </summary>
        /// <param name="index">Индекс</param>
        /// <returns>Элемент</returns>
        object IList.this[int index]
        {
            get
            {
                return _list[index];
            }
            set
            {
                throw new NotSupportedException("Items.Set is not supported for ReadOnlyList");
            }
        }

        /// <summary>
        /// Скопировать содержимое в массив
        /// </summary>
        /// <param name="array">Массив</param>
        /// <param name="index">Стартовый индекс</param>
        void ICollection.CopyTo(Array array, int index)
        {
            (_list as ICollection).CopyTo(array, index);
        }

        /// <summary>
        /// Количество элементов в списке
        /// </summary>
        int ICollection.Count
        {
            get { return _list.Count; }
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
            get { return (_list as ICollection).SyncRoot; }
        } 

        #endregion
    }
}
