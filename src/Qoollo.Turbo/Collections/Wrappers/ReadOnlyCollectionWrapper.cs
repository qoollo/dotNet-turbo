using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Collections
{
    /// <summary>
    /// Коллекция в режиме только для чтения
    /// </summary>
    /// <typeparam name="T">Тип элемента</typeparam>
    [System.Diagnostics.DebuggerDisplay("Count = {Count}")]
    [System.Diagnostics.DebuggerTypeProxy(typeof(Qoollo.Turbo.Collections.ServiceStuff.CollectionDebugView<>))]
    [Serializable]
    public class ReadOnlyCollectionWrapper<T>: ICollection<T>, ICollection, IReadOnlyCollection<T>, IEnumerable<T>, IEnumerable
    {
        private static readonly ReadOnlyCollectionWrapper<T> _empty = new ReadOnlyCollectionWrapper<T>(new T[0]);
        /// <summary>
        /// Пустая коллекция
        /// </summary>
        public static ReadOnlyCollectionWrapper<T> Empty
        {
            get
            {
                return _empty;
            }
        }

        // ===========

        private readonly ICollection<T> _collection;
        [NonSerialized]
        private object _syncRoot;


        /// <summary>
        /// Контракты
        /// </summary>
        [ContractInvariantMethod]
        private void Invariant()
        {
            Contract.Invariant(_collection != null);
        }

        /// <summary>
        /// Конструктор ReadOnlyCollectionWrapper
        /// </summary>
        /// <param name="collection">Обёртываемая коллекция</param>
        public ReadOnlyCollectionWrapper(ICollection<T> collection)
        {
            Contract.Requires<ArgumentNullException>(collection != null);

            _collection = collection;
        }

        /// <summary>
        /// Обёртываемая коллекция
        /// </summary>
        protected ICollection<T> Items { get { return _collection; } }

        /// <summary>
        /// Проверка наличия элемента в коллекции
        /// </summary>
        /// <param name="item">Элемент</param>
        /// <returns>Есть ли он в коллекции</returns>
        [Pure]
        public bool Contains(T item)
        {
            return _collection.Contains(item);
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

            _collection.CopyTo(array, arrayIndex);
        }
        /// <summary>
        /// Число элементов в коллекции
        /// </summary>
        public int Count
        {
            get { return _collection.Count; }
        }
        /// <summary>
        /// Получение Enumerator
        /// </summary>
        /// <returns>Enumerator</returns>
        public IEnumerator<T> GetEnumerator()
        {
            return _collection.GetEnumerator();
        }


        #region Реализация интерфейсов

        /// <summary>
        /// Добавление элемента в конец (не поддерживается)
        /// </summary>
        /// <param name="item">Элемент</param>
        void ICollection<T>.Add(T item)
        {
            throw new NotSupportedException("Add is not supported for ReadOnlyCollectionWrapper");
        }

        /// <summary>
        /// Оичтка коллекции (не поддерживается)
        /// </summary>
        void ICollection<T>.Clear()
        {
            throw new NotSupportedException("Clear is not supported for ReadOnlyCollectionWrapper");
        }

        /// <summary>
        /// Содержится ли элемент в коллекции
        /// </summary>
        /// <param name="item">Элемент</param>
        /// <returns>Содержится ли</returns>
        bool ICollection<T>.Contains(T item)
        {
            return _collection.Contains(item);
        }

        /// <summary>
        /// Скопировать данные коллекции в массив
        /// </summary>
        /// <param name="array">Массив</param>
        /// <param name="arrayIndex">Индекс, с которого начинается вставка</param>
        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            _collection.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Количество элементов в коллекции
        /// </summary>
        int ICollection<T>.Count
        {
            get { return _collection.Count; }
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
            throw new NotSupportedException("Remove is not supported for ReadOnlyCollectionWrapper");
        }

        /// <summary>
        /// Получение Enumerator'а
        /// </summary>
        /// <returns>Enumerator</returns>
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return _collection.GetEnumerator();
        }

        /// <summary>
        /// Получение Enumerator'а
        /// </summary>
        /// <returns>Enumerator</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _collection.GetEnumerator();
        }

        /// <summary>
        /// Скопировать содержимое в массив
        /// </summary>
        /// <param name="array">Массив</param>
        /// <param name="index">Стартовый индекс</param>
        void ICollection.CopyTo(Array array, int index)
        {
            (_collection as ICollection).CopyTo(array, index);
        }

        /// <summary>
        /// Количество элементов в коллекции
        /// </summary>
        int ICollection.Count
        {
            get { return _collection.Count; }
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
                    ICollection collection = this._collection as ICollection;
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
        /// Количество элементов в коллекции
        /// </summary>
        int IReadOnlyCollection<T>.Count
        {
            get { return _collection.Count; }
        } 

        #endregion
    }
}
