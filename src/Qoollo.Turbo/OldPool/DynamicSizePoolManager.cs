using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using System.Threading;
using System.Diagnostics.Contracts;
using Qoollo.Turbo;

namespace Qoollo.Turbo.OldPool
{
    /// <summary>
    /// Пул с изменением числа элементов при необходимости
    /// </summary>
    /// <typeparam name="T">Тип элемента</typeparam>
    /// <typeparam name="PE">Тип обёртки над элементом</typeparam>
    [ContractClass(typeof(DynamicSizePoolManagerCodeContractCheck<,>))]
    internal abstract class DynamicSizePoolManager<T, PE> : PoolManagerBase<T, PE>, IPoolElementReleaser<T>
        where T : class
        where PE : PoolElement<T>
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

            Contract.Invariant(_trimPeriod > 0);
            Contract.Invariant(_getRetryTimeout > 0);

            Contract.Invariant(_reservedCount >= _elementList.Count);
            // _reservedCount - плавающее. ограничиваем лишь непрерывный рост
            Contract.Invariant(_reservedCount < 2 * _maxElemCount + Environment.ProcessorCount);

            Contract.Invariant(_freeElements.Count <= _elementList.Count);

            //Contract.Invariant(!Contract.Exists(_elementList, v => v == null));
        }

        private readonly string _name;

        /// <summary>
        /// Список всех элементов
        /// </summary>
        private readonly List<T> _elementList;
        
        /// <summary>
        /// Коллекция свободных элементов
        /// </summary>
        private readonly BlockingCollection<T> _freeElements;

        private int _reservedCount;
        private int _maxElemCount;

        private int _getRetryTimeout = 2000;
        private int _trimPeriod;
        private int _lastTrimTestTime;
        private int _minFreeElementsBeforeTrim;

        private readonly CancellationTokenSource _disposeCancellation;

        /// <summary>
        /// Конструктор DynamicSizePoolManager
        /// </summary>
        /// <param name="maxElemCount">Максимальное число элементов в пуле (-1 - неограничено)</param>
        /// <param name="getRetryTimeout">Время повтора между попытками получить новый элемент</param>
        /// <param name="trimPeriod">Время до уменьшения числа элементов (когда используются не все)</param>
        /// <param name="name">Имя пула</param>
        public DynamicSizePoolManager(int maxElemCount, int trimPeriod, int getRetryTimeout, string name)
        {
            Contract.Requires<ArgumentException>(maxElemCount > 0);
            Contract.Requires<ArgumentException>(getRetryTimeout > 0);


            _maxElemCount = maxElemCount;
            _getRetryTimeout = getRetryTimeout;

            _reservedCount = 0;

            _elementList = new List<T>(maxElemCount);
            _freeElements = new BlockingCollection<T>(new ConcurrentQueue<T>());

            _disposeCancellation = new CancellationTokenSource();

            if (trimPeriod > 0)
                _trimPeriod = trimPeriod;
            else
                _trimPeriod = int.MaxValue;

            _lastTrimTestTime = GetTimeMeasureInMs();
            _minFreeElementsBeforeTrim = int.MaxValue;

            _name = name ?? this.GetType().GetCSFullName();

            Contract.Assume(!Contract.Exists(_elementList, v => v == null));
        }

        /// <summary>
        /// Конструктор DynamicSizePoolManager
        /// </summary>
        /// <param name="maxElemCount">Максимальное число элементов в пуле (-1 - неограничено)</param>
        /// <param name="getRetryTimeout">Время повтора между попытками получить новый элемент</param>
        /// <param name="trimPeriod">Время до уменьшения числа элементов (когда используются не все)</param>
        public DynamicSizePoolManager(int maxElemCount, int trimPeriod, int getRetryTimeout)
            : this(maxElemCount, trimPeriod, getRetryTimeout, null)
        {
        }

        /// <summary>
        /// Конструктор DynamicSizePoolManager
        /// </summary>
        /// <param name="maxElemCount">Максимальное число элементов в пуле (-1 - неограничено)</param>
        /// <param name="trimPeriod">Время до уменьшения числа элементов (когда используются не все)</param>
        /// <param name="name">Имя пула</param>
        public DynamicSizePoolManager(int maxElemCount, int trimPeriod, string name)
            : this(maxElemCount, trimPeriod, 2000, name)
        {
        }

        /// <summary>
        /// Конструктор DynamicSizePoolManager
        /// </summary>
        /// <param name="maxElemCount">Максимальное число элементов в пуле (-1 - неограничено)</param>
        /// <param name="trimPeriod">Время до уменьшения числа элементов (когда используются не все)</param>
        public DynamicSizePoolManager(int maxElemCount, int trimPeriod)
            : this(maxElemCount, trimPeriod, 2000, null)
        {
        }

        /// <summary>
        /// Конструктор DynamicSizePoolManager
        /// </summary>
        /// <param name="maxElemCount">Максимальное число элементов в пуле (-1 - неограничено)</param>
        /// <param name="name">Имя пула</param>
        public DynamicSizePoolManager(int maxElemCount, string name)
            : this(maxElemCount, -1, 2000, name)
        {
        }

        /// <summary>
        /// Конструктор DynamicSizePoolManager
        /// </summary>
        /// <param name="maxElemCount">Максимальное число элементов в пуле (-1 - неограничено)</param>
        public DynamicSizePoolManager(int maxElemCount)
            : this(maxElemCount, -1, 2000, null)
        { 
        }


        /// <summary>
        /// Создание элемента. Ожидание крайне не желательно. 
        /// Не должен кидать исключения, если только не надо прибить всю систему.
        /// </summary>
        /// <param name="elem">Созданный элемент, если удалось создать</param>
        /// <param name="timeout">Таймаут создания</param>
        /// <param name="token">Токен отмены создания элемента</param>
        /// <returns>Удалось ли создать элемент</returns>
        protected abstract bool CreateElement(out T elem, int timeout, CancellationToken token);
        /// <summary>
        /// Проверка, пригоден ли элемент для дальнейшего использования
        /// </summary>
        /// <param name="elem">Элемент</param>
        /// <returns>Пригоден ли для дальнейшего использования</returns>
        protected abstract bool IsValidElement(T elem);
        /// <summary>
        /// Уничтожить элемент
        /// </summary>
        /// <param name="elem">Элемент</param>
        protected abstract void DestroyElement(T elem);


        /// <summary>
        /// Создание обёртки над элементом.
        /// </summary>
        /// <param name="elem">Элемент</param>
        /// <returns>Обёртка</returns>
        protected abstract PE CreatePoolElement(T elem);


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
        /// Максимально возможное число элементов в пуле
        /// </summary>
        public int MaxElementCount
        {
            get { return _maxElemCount; }
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
        /// Наполнить пул до count элементов
        /// </summary>
        /// <param name="count">Число элементов, до которого наполняется пул</param>
        public void FillPoolUpTo(int count)
        {
            Contract.Requires<ArgumentException>(count >= 0);

            if (_disposeCancellation.IsCancellationRequested)
                throw new ObjectDisposedException(PoolName);

            count = Math.Min(count, _maxElemCount);
            int restCount = count - _reservedCount;

            T initedElem;
            for (int i = 0; i < restCount; i++)
            {
                if (InitNewElement(out initedElem, -1, CancellationToken.None))
                    (this as IPoolElementReleaser<T>).Release(initedElem);
            }
        }


        /// <summary>
        /// Инициализация нового элемента в пуле
        /// </summary>
        /// <param name="elem">Инициализированный элемент</param>
        /// <param name="timeout">Таймаут инициализации</param>
        /// <param name="token">Токен отмены инициализации</param>
        /// <returns>Успешность</returns>
        private bool InitNewElement(out T elem, int timeout, CancellationToken token)
        {
            elem = default(T);

            if (_disposeCancellation.IsCancellationRequested)
                return false;

            Interlocked.Exchange(ref _lastTrimTestTime, GetTimeMeasureInMs());
            Interlocked.Exchange(ref _minFreeElementsBeforeTrim, int.MaxValue);

            if (_reservedCount >= _maxElemCount)
                return false;

            bool success = false;
            try
            {
                if (System.Threading.Interlocked.Increment(ref _reservedCount) <= _maxElemCount)
                {
                    if (CreateElement(out elem, timeout, token))
                    {
                        lock (_elementList)
                        {
                            try { }
                            finally
                            {
                                _elementList.Add(elem);
                            }
                        }
                        success = true;
                    }
                    else
                    {
                        elem = default(T);
                        Profiling.Profiler.ObjectPoolElementFaulted(_name, ElementCount);
                    }
                }
            }
            finally
            {
                if (!success)
                {
                    System.Threading.Interlocked.Decrement(ref _reservedCount);
                    if (elem != null)
                    {
                        lock (_elementList)
                        {
                            try { }
                            finally
                            {
                                _elementList.Remove(elem);
                            }
                        }
                        DestroyElement(elem);
                    }
                    elem = default(T);
                }
            }

            if (success)
                Profiling.Profiler.ObjectPoolElementCreated(_name, ElementCount);

            return success;
        }


        /// <summary>
        /// Удаление элемента из пула
        /// </summary>
        /// <param name="elem">Элемент</param>
        private void RemoveElement(T elem)
        {
            Contract.Assume(!_freeElements.Contains(elem));

            lock (_elementList)
            {
                try { }
                finally
                {
                    if (_elementList.Remove(elem))
                        System.Threading.Interlocked.Decrement(ref _reservedCount);
                }
            }

            Profiling.Profiler.ObjectPoolElementDestroyed(_name, ElementCount);
            DestroyElement(elem);
        }




        /// <summary>
        /// Получение элемента при аренде
        /// </summary>
        /// <param name="result">Извлечённый элемент</param>
        /// <param name="token">Токен для отмены ожидания</param>
        /// <returns>Успешность</returns>
        private bool TryGetElementInfiniteTimeout(out T result, CancellationToken token)
        {
            result = default(T);
            if (_disposeCancellation.IsCancellationRequested || token.IsCancellationRequested)
                return false;

            if (_freeElements.TryTake(out result))
                return true;

            try
            {
                while (!_disposeCancellation.IsCancellationRequested && !token.IsCancellationRequested)
                {
                    if (InitNewElement(out result, -1, token))
                        return true;

                    if (_disposeCancellation.IsCancellationRequested || token.IsCancellationRequested)
                        return false;

                    if (_freeElements.TryTake(out result, _getRetryTimeout, token))
                        return true;
                }
            }
            catch (OperationCanceledException)
            {
                if (token.IsCancellationRequested)
                    return false;
                throw;
            }

            return false;
        }

        /// <summary>
        /// Получение элемента при аренде с таймаутом
        /// </summary>
        /// <param name="result">Извлечённый элемент</param>
        /// <param name="timeout">Таймаут</param>
        /// <param name="token">Токен для отмены</param>
        /// <returns>Успешность</returns>
        private bool TryGetElement(out T result, int timeout, CancellationToken token)
        {
            if (timeout < 0)
                return TryGetElementInfiniteTimeout(out result, token);

            result = default(T);
            if (_disposeCancellation.IsCancellationRequested || token.IsCancellationRequested)
                return false;

            if (_freeElements.TryTake(out result))
                return true;

            // timeout == 0, тогда сразу выходим, даже если не удалось ничего получить
            if (timeout == 0)
                return false;


            int curInitTimeout = timeout;            // текущее время таймаута инициализации элемента
            int curTakeTimeout = _getRetryTimeout;   // текущее время таймаута извлечения из _freeElements
            int elapsedTime = 0;                     // замер потраченного в миллисикундах времени


            int initTime = GetTimeMeasureInMs();

            try
            {
                while (!_disposeCancellation.IsCancellationRequested && !token.IsCancellationRequested)
                {
                    elapsedTime = GetTimeMeasureInMs() - initTime;
                    if (elapsedTime < 0)
                    {
                        // Переполнение счётчика времени
                        elapsedTime = 0;
                        initTime = GetTimeMeasureInMs();
                    }

                    curInitTimeout = timeout - elapsedTime; // вычисляем доступное ещё время
                    if (curInitTimeout < 0)
                        return false;




                    if (InitNewElement(out result, curInitTimeout, token))
                        return true;

                    if (_disposeCancellation.IsCancellationRequested || token.IsCancellationRequested)
                        return false;



                    // Оцениваем, сколько будем ждать элемента из коллекции
                    // Получение замера времени
                    elapsedTime = GetTimeMeasureInMs() - initTime;
                    if (elapsedTime < 0)
                    {
                        // Переполнение счётчика времени
                        elapsedTime = 0;
                        initTime = GetTimeMeasureInMs();
                    }

                    curTakeTimeout = timeout - elapsedTime; // вычисляем доступное ещё время
                    if (curTakeTimeout < 0)
                        curTakeTimeout = 0; // Доступного времени не осталось, но всё равно попытаемся получить элемент без задержек
                    else if (curTakeTimeout > _getRetryTimeout)
                        curTakeTimeout = _getRetryTimeout; // Доступное время больше времени задержки проверки. Выбираем последнее



                    // Тут может быть исключение OperationCanceledException
                    if (_freeElements.TryTake(out result, curTakeTimeout, token))
                        return true;
                }
            }
            catch (OperationCanceledException)
            {
                if (token.IsCancellationRequested)
                    return false;
                throw;
            }

            return false;
        }


        /// <summary>
        /// Арендовать элемент
        /// </summary>
        /// <param name="timeout">Таймаут (-1 - бесконечно, 0 - быстрая проверка, > 0 - полная проверка)</param>
        /// <param name="token">Токен для отмены</param>
        /// <param name="throwOnUnavail">Выбрасывать исключение при недоступности элемента</param>
        /// <returns>Обёртка над элементом пула</returns>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="CantRetrieveElementException">Если не удалось получить элемент и throwOnUnavail == true</exception>
        /// <exception cref="TimeoutException">Если не удалось получить элемент и throwOnUnavail == true</exception>
        /// <exception cref="OperationCanceledException">Если не удалось получить элемент и throwOnUnavail == true</exception>
        public override PE Rent(int timeout, CancellationToken token, bool throwOnUnavail)
        {
            if (_disposeCancellation.IsCancellationRequested)
            {
                if (throwOnUnavail)
                    throw new ObjectDisposedException(PoolName);

                return CreatePoolElement(default(T));
            }

            if (token.IsCancellationRequested)
            {
                if (throwOnUnavail)
                    token.ThrowIfCancellationRequested();

                return CreatePoolElement(default(T));
            }

            T elem = default(T);
            PE result = null;
            bool elemWasTaken = false;

            CancellationTokenSource linkedTokenSource = null;

            try
            {
                elemWasTaken = TryGetElement(out elem, 0, CancellationToken.None);
                if (!elemWasTaken && timeout != 0)
                {
                    linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token, _disposeCancellation.Token);
                    elemWasTaken = TryGetElement(out elem, timeout, linkedTokenSource.Token);
                }
            }
            finally
            {
                if (!elemWasTaken)
                    elem = default(T);

                result = CreatePoolElement(elem);
                result.SetPoolName(_name);

                if (linkedTokenSource != null)
                    linkedTokenSource.Dispose();
            }

            // Обновление числа занятых элементов (для удаления неиспользуемых)
            int curMinFreeElementsBeforeTrim = Volatile.Read(ref _minFreeElementsBeforeTrim);
            int curFreeElementsCount = _freeElements.Count;
            while (curFreeElementsCount < curMinFreeElementsBeforeTrim)
            {
                curMinFreeElementsBeforeTrim = Interlocked.CompareExchange(ref _minFreeElementsBeforeTrim, curFreeElementsCount, curMinFreeElementsBeforeTrim);
                curFreeElementsCount = _freeElements.Count;
            }
      

            if (throwOnUnavail && !elemWasTaken)
            {
                if (_disposeCancellation.IsCancellationRequested)
                    throw new CantRetrieveElementException("Rent from pool failed. Dispose was called.", new ObjectDisposedException(PoolName));

                token.ThrowIfCancellationRequested();

                if (timeout >= 0)
                    throw new TimeoutException(string.Format("Pool 'Rent' operation has timeouted. Pool: {0}. Timeout value: {1}ms", PoolName, timeout));

                Contract.Assert(false, "Element in pool is not available. Reason: UNKNOWN!");
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
            bool doTrim = false;   
            int lastTrimReq = Volatile.Read(ref _lastTrimTestTime);
            int locNowTimeMeasure = GetTimeMeasureInMs();

            // Проверяем на переполнение счётчика времени
            if (locNowTimeMeasure < lastTrimReq)
            {
                Interlocked.Exchange(ref _lastTrimTestTime, locNowTimeMeasure);
            }
            else if (locNowTimeMeasure - lastTrimReq > _trimPeriod)
            {
                // Определяем, нужно ли уменьшать число элементов в пуле
                if (Interlocked.Exchange(ref _lastTrimTestTime, locNowTimeMeasure) == lastTrimReq)
                {
                    doTrim = Volatile.Read(ref _minFreeElementsBeforeTrim) > 0;
                    Interlocked.Exchange(ref _minFreeElementsBeforeTrim, int.MaxValue);
                }
            }
            

            bool isValidElem = IsValidElement(elem);
            if (!_disposeCancellation.IsCancellationRequested && !doTrim && isValidElem)
            {
                _freeElements.Add(elem);
                // Если был вызван Dispose в процессе возврата, то пытаемся получить и уничтожить добавленный элемент
                if (_disposeCancellation.IsCancellationRequested)
                {
                    T tmpElem;
                    if (_freeElements.TryTake(out tmpElem))
                        RemoveElement(tmpElem);
                }
            }
            else
            {
                RemoveElement(elem);
            }

            if (!isValidElem)
                Profiling.Profiler.ObjectPoolElementFaulted(_name, ElementCount);

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

                        T elem = null;
                        while (_freeElements.TryTake(out elem))
                        {
                            DestroyElement(elem);
                            _elementList.Remove(elem);
                            Profiling.Profiler.ObjectPoolElementDestroyed(_name, ElementCount);
                        }

                        //_freeElements.Dispose();
                    }
                }
            }
            base.Dispose(isUserCall);
        }
    }


    /// <summary>
    /// Контракты
    /// </summary>
    [ContractClassFor(typeof(DynamicSizePoolManager<,>))]
    abstract class DynamicSizePoolManagerCodeContractCheck<T, PE> : DynamicSizePoolManager<T, PE>
        where T : class
        where PE : PoolElement<T>
    {
        /// <summary>Контракты</summary>
        private DynamicSizePoolManagerCodeContractCheck(): base(1) { }


        /// <summary>Контракты</summary>
        protected override bool CreateElement(out T elem, int timeout, CancellationToken token)
        {
            Contract.Ensures((Contract.Result<bool>() && Contract.ValueAtReturn<T>(out elem) != null) || !Contract.Result<bool>());

            throw new NotImplementedException();
        }
        /// <summary>Контракты</summary>
        protected override bool IsValidElement(T elem)
        {
            Contract.Ensures((Contract.Result<bool>() && elem != null) || !Contract.Result<bool>());

            throw new NotImplementedException();
        }
        /// <summary>Контракты</summary>
        protected override void DestroyElement(T elem)
        {
            throw new NotImplementedException();
        }
        /// <summary>Контракты</summary>
        protected override PE CreatePoolElement(T elem)
        {
            Contract.Ensures(Contract.Result<PE>() != null);

            throw new NotImplementedException();
        }
    }
}
