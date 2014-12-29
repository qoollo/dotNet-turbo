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
    /// Очередь с возможностью добавления элементов в начало
    /// </summary>
    /// <typeparam name="T">Тип элементов</typeparam>
    [System.Diagnostics.DebuggerDisplay("Count = {Count}")]
    [Serializable]
    public class OutOfTurnQueue<T> : IEnumerable<T>, IReadOnlyCollection<T>, ICollection, IEnumerable
    {
        private readonly Deque<T> _deque;

        [ContractInvariantMethod]
        private void Invariant()
        {
            Contract.Invariant(_deque != null);
        }

        /// <summary>
        /// Конструктор OutOfTurnQueue
        /// </summary>
        public OutOfTurnQueue()
        {
            _deque = new Deque<T>();
        }

        /// <summary>
        /// Конструктор OutOfTurnQueue
        /// </summary>
        /// <param name="capacity">Начальная вместимость</param>
        public OutOfTurnQueue(int capacity)
        {
            Contract.Requires(capacity >= 0);

            _deque = new Deque<T>(capacity);
        }

        /// <summary>
        /// Конструктор OutOfTurnQueue
        /// </summary>
        /// <param name="collection">Начальные элементы</param>
        public OutOfTurnQueue(IEnumerable<T> collection)
        {
            Contract.Requires(collection != null);

            _deque = new Deque<T>(collection);
        }

        /// <summary>
        /// Количество элементов в очереди
        /// </summary>
        public int Count
        {
            get { return _deque.Count; }
        }

        /// <summary>
        /// Вместимость очереди
        /// </summary>
        public int Capacity
        {
            get { return _deque.Capacity; }
        }

        /// <summary>
        /// Очистить очередь
        /// </summary>
        public void Clear()
        {
            _deque.Clear();
        }

        /// <summary>
        /// Скопировать элементы в массив
        /// </summary>
        /// <param name="array">Массив</param>
        /// <param name="index">Начальный индекс</param>
        public void CopyTo(T[] array, int index)
        {
            Contract.Requires(array != null);
            Contract.Requires(index >= 0);
            Contract.Requires(index <= array.Length - this.Count);

            _deque.CopyTo(array, index);
        }

        /// <summary>
        /// Добавить элемент в конец очереди
        /// </summary>
        /// <param name="item">Элемент</param>
        public void Enqueue(T item)
        {
            _deque.AddToBack(item);
        }

        /// <summary>
        /// Добавить элемент в начало очереди
        /// </summary>
        /// <param name="item">Элемент</param>
        public void EnqueueToFront(T item)
        {
            _deque.AddToFront(item);
        }

        /// <summary>
        /// Просмотреть элемент в голове очереди
        /// </summary>
        /// <returns>Элемент</returns>
        public T Peek()
        {
            Contract.Requires(this.Count > 0);

            return _deque.PeekAtFront();
        }

        /// <summary>
        /// Вытащить элемент из головы очереди
        /// </summary>
        /// <returns></returns>
        public T Dequeue()
        {
            Contract.Requires(this.Count > 0);

            return _deque.RemoveFromFront();
        }

        /// <summary>
        /// Содержит ли очередь элементы
        /// </summary>
        /// <param name="item">Элемент</param>
        /// <returns>Содержит ли</returns>
        [Pure]
        public bool Contains(T item)
        {
            return _deque.Contains(item);
        }

        /// <summary>
        /// Скопировать элементы в массив
        /// </summary>
        /// <returns>Массив</returns>
        public T[] ToArray()
        {
            Contract.Ensures(Contract.Result<T[]>() != null);

            return _deque.ToArray();
        }

        /// <summary>
        /// Удалить лишнее пустое место
        /// </summary>
        public void TrimExcess()
        {
            _deque.TrimExcess();
        }

        /// <summary>
        /// Получить Enumerator
        /// </summary>
        /// <returns>Enumerator</returns>
        public Deque<T>.Enumerator GetEnumerator()
        {
            return _deque.GetEnumerator();
        }

        /// <summary>
        /// Получить Enumerator
        /// </summary>
        /// <returns>Enumerator</returns>
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return _deque.GetEnumerator();
        }

        /// <summary>
        /// Получить Enumerator
        /// </summary>
        /// <returns>Enumerator</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _deque.GetEnumerator();
        }

        /// <summary>
        /// Скопировать данные в массив
        /// </summary>
        /// <param name="array">Массив</param>
        /// <param name="index">Начальный индекс</param>
        void ICollection.CopyTo(Array array, int index)
        {
            (_deque as ICollection).CopyTo(array, index);
        }

        /// <summary>
        /// Количество элементов в очереди
        /// </summary>
        int ICollection.Count
        {
            get { return _deque.Count; }
        }

        /// <summary>
        /// Синхронизирована ли коллекция
        /// </summary>
        bool ICollection.IsSynchronized
        {
            get { return (_deque as ICollection).IsSynchronized; }
        }

        /// <summary>
        /// Объект инхронизации
        /// </summary>
        object ICollection.SyncRoot
        {
            get { return (_deque as ICollection).SyncRoot; }
        }

        /// <summary>
        /// Количество элементов в очереди
        /// </summary>
        int IReadOnlyCollection<T>.Count
        {
            get { return _deque.Count; }
        }
    }
}
