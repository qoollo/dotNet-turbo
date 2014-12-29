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
    /// Обёртка множества в режиме только для чтения
    /// </summary>
    /// <typeparam name="T">Тип элемента</typeparam>
    [System.Diagnostics.DebuggerDisplay("Count = {Count}")]
    [System.Diagnostics.DebuggerTypeProxy(typeof(Qoollo.Turbo.Collections.ServiceStuff.CollectionDebugView<>))]
    [Serializable]
    public class ReadOnlyHashSet<T>: ISet<T>, ICollection<T>, ICollection, IReadOnlyCollection<T>, IEnumerable<T>, IEnumerable
    {
        private static readonly ReadOnlyHashSet<T> _empty = new ReadOnlyHashSet<T>(new HashSet<T>());
        /// <summary>
        /// Пустое множество
        /// </summary>
        public static ReadOnlyHashSet<T> Empty
        {
            get
            {
                return _empty;
            }
        }

        // ===========

        private readonly HashSet<T> _set;
        [NonSerialized]
        private object _syncRoot;


        /// <summary>
        /// Контракты
        /// </summary>
        [ContractInvariantMethod]
        private void Invariant()
        {
            Contract.Invariant(_set != null);
        }

        /// <summary>
        /// Конструктор ReadOnlyHashSet
        /// </summary>
        protected ReadOnlyHashSet()
        {
            _set = new HashSet<T>();
        }

        /// <summary>
        /// Конструктор ReadOnlyHashSet
        /// </summary>
        /// <param name="set">Обёртываемое множество</param>
        public ReadOnlyHashSet(HashSet<T> set)
        {
            Contract.Requires<ArgumentNullException>(set != null);

            _set = set;
        }

        /// <summary>
        /// Конструктор ReadOnlyHashSet
        /// </summary>
        /// <param name="set">Исходные элементы</param>
        public ReadOnlyHashSet(IEnumerable<T> set)
        {
            Contract.Requires<ArgumentNullException>(set != null);

            _set = new HashSet<T>(set);
        }

        /// <summary>
        /// Конструктор ReadOnlyHashSet
        /// </summary>
        /// <param name="set">Исходные элементы</param>
        /// <param name="eqCmp">Компаратор данных</param>
        public ReadOnlyHashSet(IEnumerable<T> set, IEqualityComparer<T> eqCmp)
        {
            Contract.Requires<ArgumentNullException>(set != null);
            Contract.Requires<ArgumentNullException>(eqCmp != null);

            _set = new HashSet<T>(set, eqCmp);
        }

        /// <summary>
        /// Обёртываемое множество
        /// </summary>
        protected HashSet<T> Items { get { return _set; } }

        /// <summary>
        /// Явлется ли множество строгим подмножеством коллекции other
        /// </summary>
        /// <param name="other">Коллекция для проверки</param>
        /// <returns>Является ли строгим подмножеством</returns>
        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            Contract.Requires(other != null);

            return _set.IsProperSubsetOf(other);
        }
        /// <summary>
        /// Явлется ли множество строгим надмножеством коллекции other
        /// </summary>
        /// <param name="other">Коллекция для проверки</param>
        /// <returns>Является ли строгим надмножеством</returns>
        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            Contract.Requires(other != null);

            return _set.IsProperSupersetOf(other);
        }

        /// <summary>
        /// Явлется ли множество подмножеством коллекции other
        /// </summary>
        /// <param name="other">Коллекция для проверки</param>
        /// <returns>Является ли подмножеством</returns>
        public bool IsSubsetOf(IEnumerable<T> other)
        {
            Contract.Requires(other != null);

            return _set.IsSubsetOf(other);
        }
        /// <summary>
        /// Явлется ли множество надмножеством коллекции other
        /// </summary>
        /// <param name="other">Коллекция для проверки</param>
        /// <returns>Является ли надмножеством</returns>
        public bool IsSupersetOf(IEnumerable<T> other)
        {
            Contract.Requires(other != null);

            return _set.IsSupersetOf(other);
        }
        /// <summary>
        /// Пересекаются ли множества
        /// </summary>
        /// <param name="other">Коллекция для проверки</param>
        /// <returns>Пересекаются ли</returns>
        public bool Overlaps(IEnumerable<T> other)
        {
            Contract.Requires(other != null);

            return _set.Overlaps(other);
        }
        /// <summary>
        /// Эквивалентны ли множества
        /// </summary>
        /// <param name="other">Коллекция для проверки</param>
        /// <returns>Эквивалентны ли</returns>
        public bool SetEquals(IEnumerable<T> other)
        {
            Contract.Requires(other != null);

            return _set.SetEquals(other);
        }


        /// <summary>
        /// Проверка наличия элемента в коллекции
        /// </summary>
        /// <param name="item">Элемент</param>
        /// <returns>Есть ли он в коллекции</returns>
        [Pure]
        public bool Contains(T item)
        {
            return _set.Contains(item);
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

            _set.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Число элементов в множестве
        /// </summary>
        public int Count
        {
            get { return _set.Count; }
        }

        /// <summary>
        /// Получение Enumerator
        /// </summary>
        /// <returns>Enumerator</returns>
        public HashSet<T>.Enumerator GetEnumerator()
        {
            return _set.GetEnumerator();
        }


        #region Реализация интерфейсов

        /// <summary>
        /// Добавление элемента в множество (не поддерживается)
        /// </summary>
        /// <param name="item">Элемент</param>
        bool ISet<T>.Add(T item)
        {
            throw new NotSupportedException("Add is not supported for ReadOnlySetWrapper");
        }

        /// <summary>
        /// Исключить элементы из множества (не поддерживается)
        /// </summary>
        /// <param name="other">Перечень элементов</param>
        void ISet<T>.ExceptWith(IEnumerable<T> other)
        {
            throw new NotSupportedException("ExceptWith is not supported for ReadOnlySetWrapper");
        }

        /// <summary>
        /// Пересечь с элементами из перечня (не поддерживается)
        /// </summary>
        /// <param name="other">Перечень элементов</param>
        void ISet<T>.IntersectWith(IEnumerable<T> other)
        {
            throw new NotSupportedException("IntersectWith is not supported for ReadOnlySetWrapper");
        }

        /// <summary>
        /// Явлется ли множество строгим подмножеством коллекции other
        /// </summary>
        /// <param name="other">Коллекция для проверки</param>
        /// <returns>Является ли строгим подмножеством</returns>
        bool ISet<T>.IsProperSubsetOf(IEnumerable<T> other)
        {
            return _set.IsProperSubsetOf(other);
        }

        /// <summary>
        /// Явлется ли множество строгим надмножеством коллекции other
        /// </summary>
        /// <param name="other">Коллекция для проверки</param>
        /// <returns>Является ли строгим надмножеством</returns>
        bool ISet<T>.IsProperSupersetOf(IEnumerable<T> other)
        {
            return _set.IsProperSupersetOf(other);
        }

        /// <summary>
        /// Явлется ли множество подмножеством коллекции other
        /// </summary>
        /// <param name="other">Коллекция для проверки</param>
        /// <returns>Является ли подмножеством</returns>
        bool ISet<T>.IsSubsetOf(IEnumerable<T> other)
        {
            return _set.IsSubsetOf(other);
        }

        /// <summary>
        /// Явлется ли множество надмножеством коллекции other
        /// </summary>
        /// <param name="other">Коллекция для проверки</param>
        /// <returns>Является ли надмножеством</returns>
        bool ISet<T>.IsSupersetOf(IEnumerable<T> other)
        {
            return _set.IsSupersetOf(other);
        }
        /// <summary>
        /// Пересекаются ли множества
        /// </summary>
        /// <param name="other">Коллекция для проверки</param>
        /// <returns>Пересекаются ли</returns>
        bool ISet<T>.Overlaps(IEnumerable<T> other)
        {
            return _set.Overlaps(other);
        }
        /// <summary>
        /// Эквивалентны ли множества
        /// </summary>
        /// <param name="other">Коллекция для проверки</param>
        /// <returns>Эквивалентны ли</returns>
        bool ISet<T>.SetEquals(IEnumerable<T> other)
        {
            return _set.SetEquals(other);
        }

        /// <summary>
        /// Симметричное исключение элементов. 
        /// В результате содержится либо элементы исходного множества, либо другого, но не обоих вместе.
        /// (не поддерживается)
        /// </summary>
        /// <param name="other">Перечень элементов</param>
        void ISet<T>.SymmetricExceptWith(IEnumerable<T> other)
        {
            throw new NotSupportedException("SymmetricExceptWith is not supported for ReadOnlySetWrapper");
        }

        /// <summary>
        /// Объеденить с множеством (не поддерживается)
        /// </summary>
        /// <param name="other">Множество элементов</param>
        void ISet<T>.UnionWith(IEnumerable<T> other)
        {
            throw new NotSupportedException("UnionWith is not supported for ReadOnlySetWrapper");
        }

        /// <summary>
        /// Добавление элемента в множество (не поддерживается)
        /// </summary>
        /// <param name="item">Элемент</param>
        void ICollection<T>.Add(T item)
        {
            throw new NotSupportedException("Add is not supported for ReadOnlySetWrapper");
        }

        /// <summary>
        /// Оичтка множества (не поддерживается)
        /// </summary>
        void ICollection<T>.Clear()
        {
            throw new NotSupportedException("Clear is not supported for ReadOnlySetWrapper");
        }

        /// <summary>
        /// Содержится ли элемент в множестве
        /// </summary>
        /// <param name="item">Элемент</param>
        /// <returns>Содержится ли</returns>
        bool ICollection<T>.Contains(T item)
        {
            return _set.Contains(item);
        }

        /// <summary>
        /// Скопировать данные множества в массив
        /// </summary>
        /// <param name="array">Массив</param>
        /// <param name="arrayIndex">Индекс, с которого начинается вставка</param>
        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            _set.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Количество элементов
        /// </summary>
        int ICollection<T>.Count
        {
            get { return _set.Count; }
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
            throw new NotSupportedException("Remove is not supported for ReadOnlySetWrapper");
        }

        /// <summary>
        /// Получение Enumerator'а
        /// </summary>
        /// <returns>Enumerator</returns>
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return _set.GetEnumerator();
        }

        /// <summary>
        /// Получение Enumerator'а
        /// </summary>
        /// <returns>Enumerator</returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _set.GetEnumerator();
        }

        /// <summary>
        /// Скопировать содержимое в массив
        /// </summary>
        /// <param name="array">Массив</param>
        /// <param name="index">Стартовый индекс</param>
        void ICollection.CopyTo(Array array, int index)
        {
            (_set as ICollection).CopyTo(array, index);
        }

        /// <summary>
        /// Количество элементов
        /// </summary>
        int ICollection.Count
        {
            get { return _set.Count; }
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
                    ICollection collection = this._set as ICollection;
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

        /// <summary>
        /// Количество элементов
        /// </summary>
        int IReadOnlyCollection<T>.Count
        {
            get { return _set.Count; }
        } 

        #endregion
    }
}
