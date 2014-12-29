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
    /// Список с преобразованием элементов на лету
    /// </summary>
    /// <typeparam name="TIn"></typeparam>
    /// <typeparam name="TOut"></typeparam>
    [System.Diagnostics.DebuggerDisplay("Count = {Count}")]
    public class TransformedReadOnlyListWrapper<TIn, TOut> : IList<TOut>, IReadOnlyList<TOut>, IList, IReadOnlyCollection<TOut>, ICollection<TOut>, ICollection, IEnumerable<TOut>, IEnumerable
    {
        /// <summary>
        /// Конвертируем ли объект в определённый тип
        /// </summary>
        /// <typeparam name="T">Тип, в который надо конвертировать</typeparam>
        /// <param name="value">Объект</param>
        /// <returns>Можно ли конвертировать</returns>
        private static bool IsCompatibleObject<T>(object value)
        {
            return value is T || (value == null && default(T) == null);
        }


        private object _syncRoot;
        private readonly IList<TIn> _list;
        private readonly Func<TIn, TOut> _transformer; 

        /// <summary>
        /// Контракты
        /// </summary>
        [ContractInvariantMethod]
        private void Invariant()
        {
            Contract.Invariant(_list != null);
            Contract.Invariant(_transformer != null);
        }

        /// <summary>
        /// Конструктор TransformedReadOnlyListWrapper
        /// </summary>
        /// <param name="list">Исходный список</param>
        /// <param name="transformator">Функция конвертации</param>
        public TransformedReadOnlyListWrapper(IList<TIn> list, Func<TIn, TOut> transformator)
        {
            Contract.Requires<ArgumentNullException>(list != null);
            Contract.Requires<ArgumentNullException>(transformator != null);

            _list = list;
            _transformer = transformator;
        }



        /// <summary>
        /// Обёртываемый список
        /// </summary>
        protected IList<TIn> Items { get { return _list; } }
        /// <summary>
        /// Функция преобразования
        /// </summary>
        protected Func<TIn, TOut> Transformer { get { return _transformer; } }


        /// <summary>
        /// Получение элемента списка по индексу
        /// </summary>
        /// <param name="index">Индекс</param>
        /// <returns>Элемент</returns>
        public TOut this[int index]
        {
            get
            {
                return _transformer(_list[index]);
            }
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
        public IEnumerator<TOut> GetEnumerator()
        {
            foreach (var elem in _list)
                yield return _transformer(elem);
        }




        /// <summary>
        /// Копирование коллекции в массив начиная с заданного индекса
        /// </summary>
        /// <param name="array">Массив, в который копируем</param>
        /// <param name="arrayIndex">Индекс внутри массива</param>
        private void CopyTo(TOut[] array, int arrayIndex)
        {
            Contract.Requires(array != null);
            Contract.Requires(arrayIndex >= 0);
            Contract.Requires(arrayIndex <= array.Length - this.Count);

            for (int i = 0; i < _list.Count; i++)
                array[i + arrayIndex] = _transformer(_list[i]);
        }

        /// <summary>
        /// Выполнение действия для каждого элемента списка
        /// </summary>
        /// <param name="action">Действие</param>
        private void ForEach(Action<TOut> action)
        {
            Contract.Requires(action != null);

            for (int i = 0; i < _list.Count; i++)
                action(_transformer(_list[i]));
        }

        /// <summary>
        /// Позиция первого элемента в списке
        /// </summary>
        /// <param name="item">Элемент</param>
        /// <returns>Позиция, если найден, иначе -1</returns>
        private int IndexOf(TOut item)
        {
            var comparer = EqualityComparer<TOut>.Default;
            for (int i = 0; i < _list.Count; i++)
            {
                if (comparer.Equals(_transformer(_list[i]), item))
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// Позиция элемента в списке (поиск идёт с конца)
        /// </summary>
        /// <param name="item">Элемент</param>
        /// <returns>Позиция, если найден, иначе -1</returns>
        private int LastIndexOf(TOut item)
        {
            var comparer = EqualityComparer<TOut>.Default;
            for (int i = _list.Count - 1; i >= 0; i--)
            {
                if (comparer.Equals(_transformer(_list[i]), item))
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// Проверка наличия элемента в коллекции
        /// </summary>
        /// <param name="item">Элемент</param>
        /// <returns>Есть ли он в коллекции</returns>
        private bool Contains(TOut item)
        {
            return this.IndexOf(item) >= 0;
        }


        #region Реализация интерфейсов


        /// <summary>
        /// Индекс элемента в списке
        /// </summary>
        /// <param name="item">Элемент</param>
        /// <returns>Индекс, если присутствует, иначе -1</returns>
        int IList<TOut>.IndexOf(TOut item)
        {
            return this.IndexOf(item);
        }

        /// <summary>
        /// Вставка элемента в список (не поддерживается)
        /// </summary>
        /// <param name="index">Индекс</param>
        /// <param name="item">Элемент</param>
        void IList<TOut>.Insert(int index, TOut item)
        {
            throw new NotSupportedException("Insert is not supported for TransformedReadOnlyListWrapper");
        }

        /// <summary>
        /// Удаление элемента из списка (не поддерживается)
        /// </summary>
        /// <param name="index">Индекс элемента</param>
        void IList<TOut>.RemoveAt(int index)
        {
            throw new NotSupportedException("RemoveAt is not supported for TransformedReadOnlyListWrapper");
        }

        /// <summary>
        /// Доступ к элементам по индексу
        /// </summary>
        /// <param name="index">Индекс</param>
        /// <returns>Элемент</returns>
        TOut IList<TOut>.this[int index]
        {
            get
            {
                return _transformer(_list[index]);
            }
            set
            {
                throw new NotSupportedException("Items.Set is not supported for TransformedReadOnlyListWrapper");
            }
        }

        /// <summary>
        /// Добавление элемента в конец (не поддерживается)
        /// </summary>
        /// <param name="item">Элемент</param>
        void ICollection<TOut>.Add(TOut item)
        {
            throw new NotSupportedException("Add is not supported for TransformedReadOnlyListWrapper");
        }

        /// <summary>
        /// Оичтка списка (не поддерживается)
        /// </summary>
        void ICollection<TOut>.Clear()
        {
            throw new NotSupportedException("Clear is not supported for TransformedReadOnlyListWrapper");
        }

        /// <summary>
        /// Содержится ли элемент в списке
        /// </summary>
        /// <param name="item">Элемент</param>
        /// <returns>Содержится ли</returns>
        bool ICollection<TOut>.Contains(TOut item)
        {
            return this.Contains(item);
        }

        /// <summary>
        /// Скопировать данные списка в массив
        /// </summary>
        /// <param name="array">Массив</param>
        /// <param name="arrayIndex">Индекс, с которого начинается вставка</param>
        void ICollection<TOut>.CopyTo(TOut[] array, int arrayIndex)
        {
            this.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Количество элементов в списке
        /// </summary>
        int ICollection<TOut>.Count
        {
            get { return _list.Count; }
        }

        /// <summary>
        /// Только для чтения
        /// </summary>
        bool ICollection<TOut>.IsReadOnly
        {
            get { return true; }
        }

        /// <summary>
        /// Удалить элемент (не поддерживается)
        /// </summary>
        /// <param name="item">Элемент</param>
        /// <returns>Удалился ли</returns>
        bool ICollection<TOut>.Remove(TOut item)
        {
            throw new NotSupportedException("Remove is not supported for TransformedReadOnlyListWrapper");
        }

        /// <summary>
        /// Получение Enumerator'а
        /// </summary>
        /// <returns>Enumerator</returns>
        IEnumerator<TOut> IEnumerable<TOut>.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        /// <summary>
        /// Получение Enumerator'а
        /// </summary>
        /// <returns>Enumerator</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        /// <summary>
        /// Доступ к элементам списка по индексу
        /// </summary>
        /// <param name="index">Индекс</param>
        /// <returns>Элемент</returns>
        TOut IReadOnlyList<TOut>.this[int index]
        {
            get { return _transformer(_list[index]); }
        }

        /// <summary>
        /// Количество элементов в списке
        /// </summary>
        int IReadOnlyCollection<TOut>.Count
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
            throw new NotSupportedException("Add is not supported for TransformedReadOnlyListWrapper");
        }

        /// <summary>
        /// Очистить список (не поддерживается)
        /// </summary>
        void IList.Clear()
        {
            throw new NotSupportedException("Clear is not supported for TransformedReadOnlyListWrapper");
        }

        /// <summary>
        /// Сожержит ли список элемент
        /// </summary>
        /// <param name="value">Элемент</param>
        /// <returns>Содержит ли</returns>
        bool IList.Contains(object value)
        {
            if (!IsCompatibleObject<TOut>(value))
                return false;

            return this.Contains((TOut)value);
        }

        /// <summary>
        /// Индекс элемента в списке
        /// </summary>
        /// <param name="value">Элемент</param>
        /// <returns>Индекс, если есть, иначе -1</returns>
        int IList.IndexOf(object value)
        {
            if (!IsCompatibleObject<TOut>(value))
                return -1;

            return this.IndexOf((TOut)value);
        }

        /// <summary>
        /// Вставка элемента в список (не поддерживается)
        /// </summary>
        /// <param name="index">Индекс</param>
        /// <param name="value">Значение</param>
        void IList.Insert(int index, object value)
        {
            throw new NotSupportedException("Insert is not supported for TransformedReadOnlyListWrapper");
        }

        /// <summary>
        /// Фиксирован ли размер
        /// </summary>
        bool IList.IsFixedSize
        {
            get { return (_list as IList).IsFixedSize; }
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
            throw new NotSupportedException("Remove is not supported for TransformedReadOnlyListWrapper");
        }

        /// <summary>
        /// Удалить элемент из списка в определённой позиции (не поддерживается)
        /// </summary>
        /// <param name="index">Индекс</param>
        void IList.RemoveAt(int index)
        {
            throw new NotSupportedException("RemoveAt is not supported for TransformedReadOnlyListWrapper");
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
                return _transformer(_list[index]);
            }
            set
            {
                throw new NotSupportedException("Items.Set is not supported for TransformedReadOnlyListWrapper");
            }
        }

        /// <summary>
        /// Скопировать содержимое в массив
        /// </summary>
        /// <param name="array">Массив</param>
        /// <param name="index">Стартовый индекс</param>
        void ICollection.CopyTo(Array array, int index)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (array.Rank != 1)
                throw new ArgumentException("array has wrong dimension");
            if (index < 0)
                throw new ArgumentException("index is less then zero");
            if (array.Length - index < _list.Count)
                throw new ArgumentException("array has not enough space");

            for (int i = 0; i < _list.Count; i++)
                array.SetValue(_transformer(_list[i]), index + i);
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
            get
            {
                if (this._syncRoot == null)
                {
                    ICollection collection = this._list as ICollection;
                    if (collection != null)
                    {
                        this._syncRoot = collection.SyncRoot;
                    }
                    else
                    {
                        System.Threading.Interlocked.CompareExchange<object>(ref this._syncRoot, new object(), null);
                    }
                }
                return this._syncRoot;
            }
        } 

        #endregion
    }
}
