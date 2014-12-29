using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Collections
{
    /// <summary>
    /// Очередь с фиксированным числом приоритетов
    /// </summary>
    /// <typeparam name="TElem">Тип элемента</typeparam>
    /// <typeparam name="TPriority">Тип приоритета</typeparam>
    [System.Diagnostics.DebuggerDisplay("Count = {Count}")]
    [Serializable]
    [ContractClass(typeof(LimitedPriorityQueueBaseCodeContract<,>))]
    public abstract class LimitedPriorityQueueBase<TElem, TPriority> : IEnumerable<TElem>, IReadOnlyCollection<TElem>, ICollection, IEnumerable
    {
        private Queue<TElem>[] _innerQueue;
        private int _count;

        [NonSerialized]
        private object _syncRoot;

        [ContractInvariantMethod]
        private void Invariant()
        {
            Contract.Invariant(_innerQueue != null);
            Contract.Invariant(_innerQueue.Length > 0);
            Contract.Invariant(_count >= 0);
        }

        /// <summary>
        /// Конструктор LimitedPriorityQueueBase
        /// </summary>
        /// <param name="priorityLevelsCount">Количество уровней приоритета</param>
        public LimitedPriorityQueueBase(int priorityLevelsCount)
        {
            Contract.Requires<ArgumentException>(priorityLevelsCount > 0);

            _innerQueue = new Queue<TElem>[priorityLevelsCount];
            for (int i = 0; i < _innerQueue.Length; i++)
                _innerQueue[i] = new Queue<TElem>();
        }

        /// <summary>
        /// Конструктор LimitedPriorityQueueBase
        /// </summary>
        /// <param name="collection">Исходные данные (попадут в коллекцию с минимальным приоритетом)</param>
        /// <param name="priorityLevelsCount">Количество уровней приоритета</param>
        public LimitedPriorityQueueBase(IEnumerable<TElem> collection, int priorityLevelsCount)
        {
            Contract.Requires<ArgumentNullException>(collection != null);
            Contract.Requires<ArgumentException>(priorityLevelsCount > 0);

            _innerQueue = new Queue<TElem>[priorityLevelsCount];
            for (int i = 0; i < _innerQueue.Length - 1; i++)
                _innerQueue[i] = new Queue<TElem>();
            _innerQueue[_innerQueue.Length - 1] = new Queue<TElem>(collection);
        }

        /// <summary>
        /// Отображение типа приоритета на его номер
        /// </summary>
        /// <param name="prior">Приоритет</param>
        /// <returns>Номер приоритета</returns>
        protected abstract int MapPriority(TPriority prior);

        /// <summary>
        /// Количество уровней приоритета
        /// </summary>
        protected int PriorityLevelsCount
        {
            get { return _innerQueue.Length; }
        }

        /// <summary>
        /// Количество элементов в очереди
        /// </summary>
        public int Count
        {
            get { return _count; }
        }

        /// <summary>
        /// Очистить очередь
        /// </summary>
        public void Clear()
        {
            Contract.Ensures(this.Count == 0);

            for (int i = 0; i < _innerQueue.Length; i++)
                _innerQueue[i].Clear();

            _count = 0;
        }


        /// <summary>
        /// Скопировать элементы в массив
        /// </summary>
        /// <param name="array">Массив</param>
        /// <param name="index">Начальный индекс</param>
        public void CopyTo(TElem[] array, int index)
        {
            Contract.Requires<ArgumentNullException>(array != null);
            Contract.Requires<ArgumentOutOfRangeException>(index >= 0 && index < array.Length);
            Contract.Requires<ArgumentException>(index <= array.Length - this.Count);

            int curIndex = index;
            for (int i = 0; i < _innerQueue.Length; i++)
            {
                _innerQueue[i].CopyTo(array, curIndex);
                curIndex += _innerQueue[i].Count;
            }
        }

        /// <summary>
        /// Добавить элемент в очередь с указанным приоритетом
        /// </summary>
        /// <param name="item">Элемент</param>
        /// <param name="priority">Приоритет</param>
        public void Enqueue(TElem item, TPriority priority)
        {
            Contract.Ensures(this.Count == Contract.OldValue(this.Count) + 1);

            int map = MapPriority(priority);
            _innerQueue[map].Enqueue(item);
            _count++;
        }


        /// <summary>
        /// Просмотреть элемент с головы очереди
        /// </summary>
        /// <returns>Элемент</returns>
        public TElem Peek()
        {
            Contract.Requires(this.Count > 0);

            if (Count == 0)
                throw new InvalidOperationException("Collection is empty");

            for (int i = 0; i < _innerQueue.Length; i++)
            {
                if (_innerQueue[i].Count > 0)
                    return _innerQueue[i].Peek();
            }

            throw new InvalidOperationException("Collection is empty");
        }

        /// <summary>
        /// Выбрать элемент с головы очереди
        /// </summary>
        /// <returns>Элемент</returns>
        public TElem Dequeue()
        {
            Contract.Requires(this.Count > 0);
            Contract.Ensures(this.Count == Contract.OldValue(this.Count) - 1);

            if (Count == 0)
                throw new InvalidOperationException("Collection is empty");

            for (int i = 0; i < _innerQueue.Length; i++)
            {
                if (_innerQueue[i].Count > 0)
                {
                    var res = _innerQueue[i].Dequeue();
                    _count--;
                    return res;
                }
            }

            throw new InvalidOperationException("Collection is empty");
        }

        /// <summary>
        /// Содержит ли очередь элемент
        /// </summary>
        /// <param name="item">Элемент</param>
        /// <returns>Содержит ли</returns>
        public bool Contains(TElem item)
        {
            if (Count == 0)
                return false;

            for (int i = 0; i < _innerQueue.Length; i++)
            {
                if (_innerQueue[i].Contains(item))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Скопировать элементы в массив
        /// </summary>
        /// <returns>Массив</returns>
        public TElem[] ToArray()
        {
            Contract.Ensures(Contract.Result<TElem[]>() != null);
            Contract.Ensures(Contract.Result<TElem[]>().Length == this.Count);

            TElem[] array = new TElem[this._count];
            this.CopyTo(array, 0);
            return array;
        }

        /// <summary>
        /// Удалить излишнее пустое место
        /// </summary>
        public void TrimExcess()
        {
            for (int i = 0; i < _innerQueue.Length; i++)
            {
                _innerQueue[i].TrimExcess();
            }
        }

        /// <summary>
        /// Получить Enumerator
        /// </summary>
        /// <returns>Enumerator</returns>
        public IEnumerator<TElem> GetEnumerator()
        {
            for (int i = 0; i < _innerQueue.Length; i++)
            {
                foreach (var elem in _innerQueue[i])
                    yield return elem;
            }
        }

        /// <summary>
        /// Получить Enumerator
        /// </summary>
        /// <returns>Enumerator</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        /// <summary>
        /// Скопировать данные в массив
        /// </summary>
        /// <param name="array">Массив</param>
        /// <param name="index">Начальный индекс</param>
        void ICollection.CopyTo(Array array, int index)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (array.Rank != 1)
                throw new ArgumentException("Array rank not equal to 1", "array");
            if (index < 0 || index > array.Length)
                throw new ArgumentOutOfRangeException("index", "index < 0 || index > array.Length");
            if (array.Length - index < this._count)
                throw new ArgumentException("array has not enough space");

            if (this._count == 0)
                return;

            int curIndex = index;
            for (int i = 0; i < _innerQueue.Length; i++)
            {
                (_innerQueue[i] as ICollection).CopyTo(array, curIndex);
                curIndex += _innerQueue[i].Count;
            }
        }

        /// <summary>
        /// Синхронизирована ли коллекция
        /// </summary>
        bool ICollection.IsSynchronized
        {
            get { return false; }
        }

        /// <summary>
        /// Объект инхронизации
        /// </summary>
        object ICollection.SyncRoot
        {
            get
            {
                if (this._syncRoot == null)
                    Interlocked.CompareExchange<object>(ref this._syncRoot, new object(), null);

                return this._syncRoot;
            }
        }
    }





    /// <summary>
    /// Контракты
    /// </summary>
    /// <typeparam name="TElem">Тип элемента</typeparam>
    /// <typeparam name="TPriority">Тип приоритета</typeparam>
    [ContractClassFor(typeof(LimitedPriorityQueueBase<,>))]
    abstract class LimitedPriorityQueueBaseCodeContract<TElem, TPriority> : LimitedPriorityQueueBase<TElem, TPriority>
    {
        private LimitedPriorityQueueBaseCodeContract(): base(1) { }

        protected override int MapPriority(TPriority prior)
        {
            Contract.Ensures(Contract.Result<int>() >= 0);
            Contract.Ensures(Contract.Result<int>() < this.PriorityLevelsCount);

            throw new NotImplementedException();
        }
    }
}
