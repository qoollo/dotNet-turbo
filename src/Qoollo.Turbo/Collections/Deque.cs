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
    /// Дек. Позволяет добавлять и извлекать элементы из начала и конца.
    /// </summary>
    /// <typeparam name="T">Тип элементов</typeparam>
    [System.Diagnostics.DebuggerDisplay("Count = {Count}")]
    [System.Diagnostics.DebuggerTypeProxy(typeof(Qoollo.Turbo.Collections.ServiceStuff.CollectionDebugView<>))]
    [Serializable]
    public class Deque<T> : IEnumerable<T>, IReadOnlyCollection<T>, ICollection, IEnumerable
    {
        /// <summary>
        /// Enumerator для дека
        /// </summary>
		[Serializable]
		public struct Enumerator : IEnumerator<T>, IDisposable, IEnumerator
		{
			private Deque<T> _srcDeque;
			private int _index;
			private int _version;
			private T _currentElement;


            /// <summary>
            /// Конструктор Enumerator
            /// </summary>
            /// <param name="srcDeque">Дек, по которому проходимся</param>
            internal Enumerator(Deque<T> srcDeque)
            {
                Contract.Requires(srcDeque != null);
                _srcDeque = srcDeque;
                _version = _srcDeque._version;
                _index = -1;
                _currentElement = default(T);
            }

            /// <summary>
            /// Текущий элемент
            /// </summary>
			public T Current
			{
				get
				{
					if (_index < 0)
					{
						if (_index == -1)
                            throw new InvalidOperationException("Enumeration has not started yet");
						else
                            throw new InvalidOperationException("Enumeration has already ended");
					}
					return this._currentElement;
				}
			}

            /// <summary>
            /// Перейти к следующему элементу
            /// </summary>
            /// <returns></returns>
			public bool MoveNext()
			{
				if (_version != _srcDeque._version)
                    throw new InvalidOperationException("Collection has changed during enumeration");

				if (_index == -2)
					return false;

				_index++;
				if (_index == _srcDeque._size)
				{
					_index = -2;
					_currentElement = default(T);
					return false;
				}
				_currentElement = _srcDeque.GetElement(this._index);
				return true;
			}

            /// <summary>
            /// Удалить ресурсы
            /// </summary>
            public void Dispose()
            {
                _index = -2;
                _currentElement = default(T);
            }

            /// <summary>
            /// Текущий элемент
            /// </summary>
            object IEnumerator.Current
            {
                get { return this.Current; }
            }

            /// <summary>
            /// Сбросить перечисление
            /// </summary>
			void IEnumerator.Reset()
			{
				if (_version != _srcDeque._version)
                    throw new InvalidOperationException("Collection has changed during enumeration");
				_index = -1;
				_currentElement = default(T);
			}
		}


        private static readonly T[] EmptyArray = new T[0];

        /// <summary>
        /// Минимальный размер, на который увеличивается внутренний массив
        /// </summary>
        private const int MinimumGrow = 4;
        /// <summary>
        /// Порог обрезания размера внутреннего массива (Count &lt; ShrinkRate * Capacity)
        /// </summary>
        private const double ShrinkRate = 0.9;
        /// <summary>
        /// Степень расширения массива при нехватке элементов
        /// </summary>
        private const int GrowFactor = 2;
        /// <summary>
        /// Вместимость по умолчанию
        /// </summary>
        private const int DefaultCapacity = 4;


		private T[] _elemArray;
		private int _head;
		private int _tail;
		private int _size;
		private int _version;

		[NonSerialized]
		private object _syncRoot;

        /// <summary>
        /// Контракты
        /// </summary>
        [ContractInvariantMethod]
        private void Invariant()
        {
            Contract.Invariant(_elemArray != null);
            Contract.Invariant(_head >= 0);
            Contract.Invariant(_head < _elemArray.Length);
            Contract.Invariant(_tail >= 0);
            Contract.Invariant(_tail < _elemArray.Length);
            Contract.Invariant(_size >= 0);
        }
		
        /// <summary>
        /// Конструктор Deque
        /// </summary>
		public Deque()
		{
			_elemArray = Deque<T>.EmptyArray;
		}

        /// <summary>
        /// Конструктор Deque с преаллокацией
        /// </summary>
        /// <param name="capacity">Начальная вместимость</param>
		public Deque(int capacity)
		{
            Contract.Requires<ArgumentOutOfRangeException>(capacity >= 0);

			_elemArray = new T[capacity];
			_head = 0;
			_tail = 0;
			_size = 0;
		}

        /// <summary>
        /// Конструктор Deque
        /// </summary>
        /// <param name="collection">Исходная коллекция</param>
        public Deque(IEnumerable<T> collection)
		{
            Contract.Requires<ArgumentNullException>(collection != null);

            var col = collection as ICollection<T>;
            if (col != null)
            {
                _size = col.Count;
                _elemArray = new T[_size + 1];
                col.CopyTo(_elemArray, 0);
                _head = 0;
                _tail = _size;
                _version = 0;
            }
            else
            {
                var notStrictCol = collection as ICollection;
                if (notStrictCol != null)
                    _elemArray = new T[notStrictCol.Count + 1];
                else
                    _elemArray = new T[DefaultCapacity];
                _size = 0;
                _version = 0;
                _head = 0;
                _tail = 0;

                foreach (var elem in collection)
                    AddToBack(elem);
            }
		}

        /// <summary>
        /// Количество элементов
        /// </summary>
        public int Count
        {
            get { return this._size; }
        }

        /// <summary>
        /// Внутренняя вместимость
        /// </summary>
        public int Capacity
        {
            get { return this._elemArray.Length; }
        }

        /// <summary>
        /// Получить элемент с определённым индексом
        /// </summary>
        /// <param name="i">Индекс</param>
        /// <returns>Элемент</returns>
        [Pure]
        internal T GetElement(int i)
        {
            Contract.Requires(i >= 0);
            Contract.Requires(i <= this.Count);

            return _elemArray[(_head + i) % _elemArray.Length];
        }

        /// <summary>
        /// Очистить дек
        /// </summary>
		public void Clear()
		{
            Contract.Ensures(this.Count == 0);

			if (_head < _tail)
			{
				Array.Clear(_elemArray, _head, _size);
			}
			else
			{
				Array.Clear(_elemArray, _head, _elemArray.Length - _head);
				Array.Clear(_elemArray, 0, _tail);
			}
			_head = 0;
			_tail = 0;
			_size = 0;
			_version++;
		}

        /// <summary>
        /// Скопировать содержимое в массив
        /// </summary>
        /// <param name="array">Массив</param>
        /// <param name="index">Начальный индекс</param>
		public void CopyTo(T[] array, int index)
		{
            Contract.Requires<ArgumentNullException>(array != null);
            Contract.Requires<ArgumentOutOfRangeException>(index >= 0 && index < array.Length);
            Contract.Requires<ArgumentException>(index <= array.Length - this.Count);

            if (this._size == 0)
                return;


            if (_head < _tail)
            {
                Array.Copy(_elemArray, _head, array, index, _size);
            }
            else
            {
                int endCopySize = _elemArray.Length - _head;
                Array.Copy(_elemArray, _head, array, index, endCopySize);
                Array.Copy(_elemArray, 0, array, index + endCopySize, _tail);
            }
		}


        /// <summary>
        /// Добавить элемент в конец
        /// </summary>
        /// <param name="item">Элемент</param>
		public void AddToBack(T item)
		{
            Contract.Ensures(this.Count == Contract.OldValue(this.Count) + 1);

			if (_size == _elemArray.Length)
			{
                int newSize = _elemArray.Length * GrowFactor;
                if (newSize < _elemArray.Length + MinimumGrow)
                    newSize = _elemArray.Length + MinimumGrow;
                this.SetCapacity(0, newSize);
			}
			_elemArray[_tail] = item;
			_tail = (_tail + 1) % _elemArray.Length;
			_size++;
			_version++;
		}

        /// <summary>
        /// Добавить элемент в начало
        /// </summary>
        /// <param name="item">Элемент</param>
        public void AddToFront(T item)
        {
            Contract.Ensures(this.Count == Contract.OldValue(this.Count) + 1);

            if (_size == _elemArray.Length)
            {
                int newSize = _elemArray.Length * GrowFactor;
                if (newSize < _elemArray.Length + MinimumGrow)
                    newSize = _elemArray.Length + MinimumGrow;
                this.SetCapacity(1, newSize);
            }
            var newHead = (_head - 1 + _elemArray.Length) % _elemArray.Length;
            _elemArray[newHead] = item;
            _head = newHead;
            _size++;
            _version++;
        }

        /// <summary>
        /// Просмотреть элемент в начале
        /// </summary>
        /// <returns>Элемент</returns>
        [Pure]
        public T PeekAtFront()
        {
            Contract.Requires(this.Count > 0);
            if (this._size == 0)
                throw new InvalidOperationException("Collection is empty");

            return _elemArray[_head];
        }

        /// <summary>
        /// Просмотреть элемент в конце
        /// </summary>
        /// <returns>Элемент</returns>
        [Pure]
        public T PeekAtEnd()
        {
            Contract.Requires(this.Count > 0);
            if (this._size == 0)
                throw new InvalidOperationException("Collection is empty");

            var lastElem = (_tail - 1 + _elemArray.Length) % _elemArray.Length;
            return _elemArray[lastElem];
        }


        /// <summary>
        /// Удалить из начала
        /// </summary>
        /// <returns>Элемент</returns>
		public T RemoveFromFront()
		{
            Contract.Requires(this.Count > 0);
            Contract.Ensures(this.Count == Contract.OldValue(this.Count) - 1);

			if (this._size == 0)
                throw new InvalidOperationException("Collection is empty");

			T result = _elemArray[this._head];
			_elemArray[this._head] = default(T);
			_head = (_head + 1) % _elemArray.Length;
			_size--;
			_version++;
			return result;
		}

        /// <summary>
        /// Удалить из конца
        /// </summary>
        /// <returns>Элемент</returns>
        public T RemoveFromBack()
        {
            Contract.Requires(this.Count > 0);
            Contract.Ensures(this.Count == Contract.OldValue(this.Count) - 1);

            if (this._size == 0)
                throw new InvalidOperationException("Collection is empty");

            var lastElem = (_tail - 1 + _elemArray.Length) % _elemArray.Length;
            T result = _elemArray[lastElem];
            _elemArray[lastElem] = default(T);
            _tail = lastElem;
            _size--;
            _version++;
            return result;
        }

        /// <summary>
        /// Содержится ли элемент в деке
        /// </summary>
        /// <param name="item">Элемент</param>
        /// <returns>Содержится ли</returns>
        [Pure]
		public bool Contains(T item)
		{
			int curPos = this._head;
			int restCount = this._size;
			EqualityComparer<T> comparer = EqualityComparer<T>.Default;
			while (restCount-- > 0)
			{
				if (item == null)
                {
					if (this._elemArray[curPos] == null)
						return true;
				}
				else
				{
                    if (this._elemArray[curPos] != null && comparer.Equals(this._elemArray[curPos], item))
						return true;
				}
				curPos = (curPos + 1) % this._elemArray.Length;
			}
			return false;
		}

        /// <summary>
        /// Скопировать в массив
        /// </summary>
        /// <returns>Массив</returns>
		public T[] ToArray()
		{
            Contract.Ensures(Contract.Result<T[]>() != null);
            Contract.Ensures(Contract.Result<T[]>().Length == this.Count);

            T[] array = new T[_size];
            if (_size == 0)
				return array;

			if (_head < _tail)
			{
                Array.Copy(_elemArray, _head, array, 0, _size);
			}
			else
			{
				Array.Copy(_elemArray, _head, array, 0, _elemArray.Length - _head);
				Array.Copy(_elemArray, 0, array, _elemArray.Length - _head, _tail);
			}
			return array;
		}

        /// <summary>
        /// Обрезать размеры внутреннего массива
        /// </summary>
        public void TrimExcess()
        {
            Contract.Ensures(this.Count == Contract.OldValue(this.Count));

            int minSize = (int)((double)_elemArray.Length * ShrinkRate);
            if (_size < minSize)
                this.SetCapacity(0, _size);
        }

        /// <summary>
        /// Задать размер внутреннего массива
        /// </summary>
        /// <param name="headOffset">Смещение данных от начала</param>
        /// <param name="capacity">Желаемая вместимость</param>
		private void SetCapacity(int headOffset, int capacity)
		{
            Contract.Requires(capacity >= this.Count);

            if (headOffset < 0)
                headOffset = 0;
            else if (capacity - headOffset < _size)
                headOffset = capacity - _size;

			T[] array = new T[capacity];
			if (_size > 0)
			{
                if (_head < _tail)
                {
                    Array.Copy(_elemArray, _head, array, headOffset, _size);
                }
                else
                {
                    var countToEnd = _elemArray.Length - _head;
                    Array.Copy(_elemArray, _head, array, headOffset, countToEnd);
                    Array.Copy(_elemArray, 0, array, headOffset + countToEnd, _tail);
                }
			}
			_elemArray = array;
            _head = headOffset;
			_tail = ((_size == capacity) ? 0 : this._size);
			_version++;
		}



        /// <summary>
        /// Получить Enumerator
        /// </summary>
        /// <returns>Enumerator</returns>
        public Deque<T>.Enumerator GetEnumerator()
        {
            return new Deque<T>.Enumerator(this);
        }

        /// <summary>
        /// Получить Enumerator
        /// </summary>
        /// <returns>Enumerator</returns>
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new Deque<T>.Enumerator(this);
        }
        /// <summary>
        /// Получить Enumerator
        /// </summary>
        /// <returns>Enumerator</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Deque<T>.Enumerator(this);
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
            if (array.Length - index < this._size)
                throw new ArgumentException("array has not enough space");

            if (this._size == 0)
                return;


            try
            {
                if (_head < _tail)
                {
                    Array.Copy(_elemArray, _head, array, index, _size);
                }
                else
                {
                    int endCopySize = _elemArray.Length - _head;
                    Array.Copy(_elemArray, _head, array, index, endCopySize);
                    Array.Copy(_elemArray, 0, array, index + endCopySize, _tail);
                }
            }
            catch (ArrayTypeMismatchException)
            {
                throw new ArgumentException("Array has wrong element type");
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
}
