using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using System.Threading;
using System.Diagnostics.Contracts;
using Qoollo.Turbo;
using System.Diagnostics;

namespace Qoollo.Turbo.OldPool
{
    /// <summary>
    /// Пул с ручным заполнением элементами и балансировкой
    /// </summary>
    /// <typeparam name="T">Тип элемента</typeparam>
    internal class BalancingStaticPoolManager<T> : PoolManagerBase<T, PoolElement<T>>, IPoolElementReleaser<T>
        where T : class
    {
        /// <summary>
        /// Контракты
        /// </summary>
        [ContractInvariantMethod]
        private void Invariant()
        {
            Contract.Invariant(_name != null);

            Contract.Invariant(_elementList != null);
            Contract.Invariant(_freeElements != null);
            Contract.Invariant(_elementComparer != null);

            Contract.Invariant(_disposeCancellation != null);

            Contract.Invariant(_freeElements.Count <= _elementList.Count);
            Contract.Invariant(!Contract.Exists(_elementList, v => v == null));
        }

        private readonly string _name;
        private readonly bool _disposeElementOnDestroy;

        private readonly List<T> _elementList;

        private readonly LinkedList<T> _freeElements;
        private readonly IComparer<T> _elementComparer;

        private readonly CancellationTokenSource _disposeCancellation;

        /// <summary>
        /// Конструктор BalancingStaticPoolManager
        /// </summary>
        /// <param name="elementComparer">Объект сравнения элементов с целью поиска наилучшего</param>
        /// <param name="name">Имя пула</param>
        /// <param name="disposeElementOnDestroy">Вызывать ли Dispose у элементов при их уничтожении</param>
        public BalancingStaticPoolManager(IComparer<T> elementComparer, string name, bool disposeElementOnDestroy)
        {
            _elementList = new List<T>();
            _freeElements = new LinkedList<T>();
            _elementComparer = elementComparer ?? Comparer<T>.Default;

            _name = name ?? this.GetType().GetCSFullName();
            _disposeElementOnDestroy = disposeElementOnDestroy;

            _disposeCancellation = new CancellationTokenSource();
        }

        /// <summary>
        /// Конструктор BalancingStaticPoolManager
        /// </summary>
        /// <param name="elementComparer">Объект сравнения элементов с целью поиска наилучшего</param>
        /// <param name="name">Имя пула</param>
        public BalancingStaticPoolManager(IComparer<T> elementComparer, string name)
            : this(elementComparer, name, true)
        {
        }

        /// <summary>
        /// Конструктор BalancingStaticPoolManager
        /// </summary>
        public BalancingStaticPoolManager()
            : this(null, null, true)
        {
        }


        /// <summary>
        /// Имя пула
        /// </summary>
        public string PoolName
        {
            get { return _name; }
        }

        /// <summary>
        /// Общее число элементов
        /// </summary>
        public override int ElementCount
        {
            get { return _elementList.Count; }
        }

        /// <summary>
        /// Число свободных в данный момент элементов
        /// </summary>
        public override int FreeElementCount
        {
            get { return _freeElements.Count; }
        }

        /// <summary>
        /// Число арендованных элементов в данный момент
        /// </summary>
        public override int RentedElementCount
        {
            get { return _elementList.Count - _freeElements.Count; }
        }


        /// <summary>
        /// Получить отсчёт времени в миллисекундах
        /// </summary>
        /// <returns>Текущее значение</returns>
        private static int GetTimeMeasureInMs()
        {
            return Environment.TickCount & int.MaxValue;
        }



        /// <summary>
        /// Обработчик нотификации об отмене ожидания элементов
        /// </summary>
        private void ElementWaitingCancellationNotification()
        {
            lock (_freeElements)
            {
                Monitor.PulseAll(_freeElements);
            }
        }

        /// <summary>
        /// Попытаться достать лучший на данный момент элемент
        /// </summary>
        /// <param name="result">Найденный элемент</param>
        /// <param name="timeout">Таймаут извлечения</param>
        /// <param name="token">Токен отмены</param>
        /// <returns>Удалось ли найти свободный элемент</returns>
        private bool TryTakeBestElementFromFreeList(out T result, int timeout, CancellationToken token)
        {
            result = default(T);

            lock (_freeElements)
            {
                if (_freeElements.Count == 0)
                {
                    if (timeout == 0)
                        return false;

                    int initTime = GetTimeMeasureInMs();
                    using (var tokenCancelRegistration = token.Register(ElementWaitingCancellationNotification))
                    {
                        while (_freeElements.Count == 0)
                        {
                            if (_disposeCancellation.IsCancellationRequested || token.IsCancellationRequested)
                                return false;

                            if (timeout < 0)
                            {
                                Monitor.Wait(_freeElements);
                            }
                            else
                            {
                                int curTime = GetTimeMeasureInMs() - initTime;
                                // Таймаут
                                if (curTime > timeout)
                                    return false;

                                if (curTime < 0)
                                {
                                    // Переполнение счётчика времени
                                    initTime = GetTimeMeasureInMs();
                                    curTime = 0;
                                }
       
                                Monitor.Wait(_freeElements, timeout - curTime);
                            }
                        }
                    }
                }

                LinkedListNode<T> best = _freeElements.First;
                LinkedListNode<T> current = _freeElements.First.Next;
                while (current != null)
                {
                    if (_elementComparer.Compare(current.Value, best.Value) > 0)
                        best = current;
                    current = current.Next;
                }
                
                try { }
                finally
                {
                    _freeElements.Remove(best);
                }

                result = best.Value;
                return true;
            }
        }

        /// <summary>
        /// Попытаться достать лучший на данный момент элемент
        /// </summary>
        /// <param name="result">Найденный элемент</param>
        /// <returns>Удалось ли найти свободный элемент</returns>
        private bool TryTakeBestElementFromFreeList(out T result)
        {
            return TryTakeBestElementFromFreeList(out result, 0, CancellationToken.None);
        }


        /// <summary>
        /// Вернуть элемент в коллекцию свободных
        /// </summary>
        /// <param name="element">Элемент</param>
        private void AddBackToFreeList(T element)
        {
            lock (_freeElements)
            {
                try { }
                finally
                {
                    _freeElements.AddLast(element);
                }
                Monitor.Pulse(_freeElements);
            }
        }


        /// <summary>
        /// Добавление нового элемента в пул
        /// </summary>
        /// <param name="elem">Элемент</param>
        public void AddNewElement(T elem)
        {
            Contract.Requires<ArgumentNullException>(elem != null, "elem");

            if (_disposeCancellation.IsCancellationRequested)
                throw new ObjectDisposedException(PoolName);

            lock (_elementList)
            {
                if (_disposeCancellation.IsCancellationRequested)
                    throw new ObjectDisposedException(PoolName);

                try { }
                finally
                {
                    _elementList.Add(elem);
                    AddBackToFreeList(elem);
                }
            }
            Profiling.Profiler.ObjectPoolElementCreated(_name, ElementCount);
        }


        /// <summary>
        /// Освобождение ресурсов элемента (точка расширения)
        /// </summary>
        /// <param name="elem">Элемент</param>
        protected virtual void DestroyElement(T elem)
        {
            if (_disposeElementOnDestroy && elem != null && elem is IDisposable)
                (elem as IDisposable).Dispose();
        }

        /// <summary>
        /// Освобождение ресурсов элемента
        /// </summary>
        /// <param name="elem">Элемент</param>
        private void DestroyElementInner(T elem)
        {
            Contract.Requires(elem != null);

            DestroyElement(elem);
            Profiling.Profiler.ObjectPoolElementDestroyed(_name, ElementCount);
        }

        /// <summary>
        /// Удаление элемента из пула
        /// </summary>
        /// <param name="elem">Элемент</param>
        /// <returns>Успешность</returns>
        private bool RemoveElementInner(T elem)
        {
            Contract.Requires(elem != null);

            bool res = false;

            lock (_elementList)
            {
                try { }
                finally
                {
                    res = _elementList.Remove(elem);
                }
            }

            Profiling.Profiler.ObjectPoolElementDestroyed(_name, ElementCount);
            return res;
        }

        /// <summary>
        /// Удалить и освободить ресурсы элемента
        /// </summary>
        /// <param name="elem">Элемент</param>
        /// <returns>Успешность</returns>
        private bool RemoveAndDestroyElementInner(T elem)
        {
            if (elem == null)
                return false;

            bool result = RemoveElementInner(elem);
            if (result)
                DestroyElementInner(elem);
            return result;
        }


        /// <summary>
        /// Удаление арендованного элемента из пула (напрмер, если стал не валиден)
        /// </summary>
        /// <param name="elemWrapper">Обёртка над элементом</param>
        /// <param name="disposeIfValid">Освободить ресурсы элемента</param>
        /// <returns>Успешность</returns>
        public bool RemoveElement(PoolElement<T> elemWrapper, bool disposeIfValid)
        {
            Contract.Requires<ArgumentNullException>(elemWrapper != null, "elemWrapper");
            Debug.Assert(object.ReferenceEquals(this, elemWrapper.Pool));

            var elem = elemWrapper.Element;
            if (elem == null)
                return false;

            elemWrapper.FreeWrapperInternal();

            bool result = RemoveElementInner(elem);
            if (result && disposeIfValid)
                DestroyElementInner(elem);

            return result;
        }

        /// <summary>
        /// Удаление арендованного элемента из пула (напрмер, если стал не валиден)
        /// </summary>
        /// <param name="elemWrapper">Обёртка над элементом</param>
        /// <returns>Успешность</returns>
        public bool RemoveElement(PoolElement<T> elemWrapper)
        {
            return RemoveElement(elemWrapper, false);
        }


        /// <summary>
        /// Арендовать элемент
        /// </summary>
        /// <param name="timeout">Таймаут (-1 - бесконечно, 0 - быстрая проверка, > 0 - полная проверка)</param>
        /// <param name="token">Токен для отмены</param>
        /// <param name="throwOnUnavail">Выбрасывать исключение при недоступности элемента</param>
        /// <returns>Обёртка над элементом пула</returns>
        /// <exception cref="CantRetrieveElementException">Если не удалось получить элемент и throwOnUnavail == true</exception>
        public override PoolElement<T> Rent(int timeout, CancellationToken token, bool throwOnUnavail)
        {
            if (_disposeCancellation.IsCancellationRequested)
            {
                if (throwOnUnavail)
                    throw new ObjectDisposedException(PoolName);

                return new PoolElement<T>(this, default(T));
            }

            if (token.IsCancellationRequested)
            {
                if (throwOnUnavail)
                    token.ThrowIfCancellationRequested();

                return new PoolElement<T>(this, default(T));
            }

            T elem = default(T);
            PoolElement<T> result = null;
            bool elemWasTaken = false;

            if (timeout < 0)
                timeout = -1;

            try
            {
                elemWasTaken = TryTakeBestElementFromFreeList(out elem, timeout, token);
            }
            finally
            {
                if (!elemWasTaken)
                    elem = default(T);

                result = new PoolElement<T>(this, elem);
                result.SetPoolName(_name);
            }


            if (throwOnUnavail && !elemWasTaken)
            {
                if (_disposeCancellation.IsCancellationRequested)
                    throw new CantRetrieveElementException("Rent from pool failed. Dispose was called.", new ObjectDisposedException(PoolName));

                token.ThrowIfCancellationRequested();

                if (timeout >= 0)
                    throw new TimeoutException(string.Format("Pool 'Rent' operation has timeouted. Pool: {0}. Timeout value: {1}ms", PoolName, timeout));

                Debug.Assert(false, "Element in pool is not available. Reason: UNKNOWN!");
                throw new CantRetrieveElementException("Rent from pool failed");
            }

            Profiling.Profiler.ObjectPoolElementRented(_name, this.RentedElementCount);
            return result;
        }


        /// <summary>
        /// Освобождение элемента пула и добавление его к свободным
        /// </summary>
        /// <param name="elem">Элемент</param>
        void IPoolElementReleaser<T>.Release(T elem)
        {
            if (elem == null)
                return;

            if (!_disposeCancellation.IsCancellationRequested)
            {
                AddBackToFreeList(elem);
                // Если был вызван Dispose в процессе возврата, то пытаемся получить и уничтожить добавленный элемент
                if (_disposeCancellation.IsCancellationRequested)
                {
                    T tmpElem;
                    if (TryTakeBestElementFromFreeList(out tmpElem))
                        RemoveAndDestroyElementInner(tmpElem);
                }
            }
            else
            {
                RemoveAndDestroyElementInner(elem);
            }

            Profiling.Profiler.ObjectPoolElementReleased(_name, this.RentedElementCount);
        }



        /// <summary>
        /// Основной код освобождения ресурсов
        /// </summary>
        /// <param name="isUserCall">Вызвано ли освобождение пользователем. False - деструктор</param>
        protected override void Dispose(bool isUserCall)
        {
            if (!_disposeCancellation.IsCancellationRequested && isUserCall)
            {
                lock (_elementList)
                {
                    if (!_disposeCancellation.IsCancellationRequested)
                    {
                        _disposeCancellation.Cancel();
                        ElementWaitingCancellationNotification();

                        lock (_freeElements)
                        {
                            while (_freeElements.Count > 0)
                            {
                                DestroyElementInner(_freeElements.First.Value);
                                _elementList.Remove(_freeElements.First.Value);
                                _freeElements.RemoveFirst();
                            }
                        }
                    }
                }
            }

            base.Dispose(isUserCall);
        }
    }
}
