using Qoollo.Turbo.ObjectPools.Common;
using Qoollo.Turbo.ObjectPools.ServiceStuff;
using Qoollo.Turbo.ObjectPools.ServiceStuff.ElementContainers;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.ObjectPools
{
    /// <summary>
    /// Объектный пул с автоматической регулировкой числа элементов и балансировкой
    /// </summary>
    /// <typeparam name="TElem">Тип элемента</typeparam>
    public abstract class BalancingDynamicPoolManager<TElem> : ObjectPoolManager<TElem>, IPoolElementOperationSource<TElem>
    {
        /// <summary>
        /// Сравниватель элементов
        /// </summary>
        private class ElementComparer: PoolElementComparer<TElem>
        {
            private readonly BalancingDynamicPoolManager<TElem> _objectPool;

            /// <summary>
            /// Конструктор ElementComparer
            /// </summary>
            /// <param name="objectPool">Родительский пул</param>
            public ElementComparer(BalancingDynamicPoolManager<TElem> objectPool)
            {
                Contract.Requires(objectPool != null);

                _objectPool = objectPool;
            }

            /// <summary>
            /// Сравнить. (a &gt; b =&gt; result &gt; 0; a == b =&gt; result == 0; a &lt; b =&gt; result &lt; 0)
            /// </summary>
            /// <param name="a">A</param>
            /// <param name="b">B</param>
            /// <param name="stopHere">Можно остановить поиск лучшего на текущем элементе</param>
            /// <returns>Результат сравнения</returns>
            public override int Compare(PoolElementWrapper<TElem> a, PoolElementWrapper<TElem> b, out bool stopHere)
            {
                return _objectPool.CompareElements(a.Element, b.Element, out stopHere);
            }
        }



        // ============================

        /// <summary>
        /// Получить временной маркер в миллисекундах
        /// </summary>
        /// <returns>Временной маркер</returns>
        private static uint GetTimestamp()
        {
            return (uint)Environment.TickCount;
        }
        /// <summary>
        /// Обновить таймаут
        /// </summary>
        /// <param name="startTime">Время начала</param>
        /// <param name="originalTimeout">Величина таймаута</param>
        /// <returns>Сколько осталось времени</returns>
        private static int UpdateTimeout(uint startTime, int originalTimeout)
        {
            if (originalTimeout < 0)
                return Timeout.Infinite;

            uint elapsed = GetTimestamp() - startTime;
            if (elapsed > (uint)int.MaxValue)
                return 0;

            int rest = originalTimeout - (int)elapsed;
            if (rest <= 0)
                return 0;

            return rest;
        }

        // ===================


        private const int DefaultGetRetryTimeout = 2000;
        private const int DefaultTrimPeriod = 60 * 1000;

        private readonly string _name;
        private readonly int _minElementCount;
        private readonly int _maxElementCount;
        private readonly int _getRetryTimeout;
        private readonly int _trimPeriod;

        private readonly UsedElementTracker _usedElementTracker;
        private readonly PrioritizedElementsContainer<TElem> _elementsContainer;
        private readonly CancellationTokenSource _disposeCancellation;

        private int _reservedCount;

        /// <summary>
        /// Конструктор BalancingDynamicPoolManager
        /// </summary>
        /// <param name="minElementCount">Минимальное число элементов в пуле</param>
        /// <param name="maxElementCount">Максимальное число элементов в пуле</param>
        /// <param name="name">Имя пула</param>
        /// <param name="trimPeriod">Время до уменьшения числа элементов (когда используются не все)</param>
        /// <param name="getRetryTimeout">Время повтора между попытками получить новый элемент</param>
        public BalancingDynamicPoolManager(int minElementCount, int maxElementCount, string name, int trimPeriod, int getRetryTimeout)
        {
            Contract.Requires<ArgumentException>(minElementCount >= 0);
            Contract.Requires<ArgumentException>(maxElementCount > 0);
            Contract.Requires<ArgumentException>(maxElementCount >= minElementCount);
            Contract.Requires<ArgumentException>(getRetryTimeout > 0);


            _name = name ?? this.GetType().GetCSFullName();
            _minElementCount = minElementCount;
            _maxElementCount = maxElementCount;
            _getRetryTimeout = getRetryTimeout;
            _trimPeriod = trimPeriod > 0 ? trimPeriod : int.MaxValue;

            _reservedCount = 0;

            _usedElementTracker = new UsedElementTracker(_trimPeriod);
            _elementsContainer = new PrioritizedElementsContainer<TElem>(new ElementComparer(this));
            _disposeCancellation = new CancellationTokenSource();

            Profiling.Profiler.ObjectPoolCreated(this.Name);
        }
        /// <summary>
        /// Конструктор BalancingDynamicPoolManager
        /// </summary>
        /// <param name="minElementCount">Минимальное число элементов в пуле</param>
        /// <param name="maxElementCount">Максимальное число элементов в пуле</param>
        /// <param name="name">Имя пула</param>
        /// <param name="trimPeriod">Время до уменьшения числа элементов (когда используются не все)</param>
        public BalancingDynamicPoolManager(int minElementCount, int maxElementCount, string name, int trimPeriod)
            : this(minElementCount, maxElementCount, name, trimPeriod, DefaultGetRetryTimeout)
        {
        }
        /// <summary>
        /// Конструктор BalancingDynamicPoolManager
        /// </summary>
        /// <param name="minElementCount">Минимальное число элементов в пуле</param>
        /// <param name="maxElementCount">Максимальное число элементов в пуле</param>
        /// <param name="name">Имя пула</param>
        public BalancingDynamicPoolManager(int minElementCount, int maxElementCount, string name)
            : this(minElementCount, maxElementCount, name, DefaultTrimPeriod, DefaultGetRetryTimeout)
        {
        }
        /// <summary>
        /// Конструктор BalancingDynamicPoolManager
        /// </summary>
        /// <param name="maxElementCount">Максимальное число элементов в пуле</param>
        /// <param name="name">Имя пула</param>
        public BalancingDynamicPoolManager(int maxElementCount, string name)
            : this(0, maxElementCount, name, DefaultTrimPeriod, DefaultGetRetryTimeout)
        {
        }
        /// <summary>
        /// Конструктор BalancingDynamicPoolManager
        /// </summary>
        /// <param name="maxElementCount">Максимальное число элементов в пуле</param>
        public BalancingDynamicPoolManager(int maxElementCount)
            : this(0, maxElementCount, null, DefaultTrimPeriod, DefaultGetRetryTimeout)
        {
        }


        /// <summary>
        /// Имя пула
        /// </summary>
        public string Name
        {
            get { return _name; }
        }

        /// <summary>
        /// Минимальное число элементов
        /// </summary>
        public int MinElementCount
        {
            get { return _minElementCount; }
        }
        /// <summary>
        /// Максимальное число элементов
        /// </summary>
        public int MaxElementCount
        {
            get { return _maxElementCount; }
        }
        /// <summary>
        /// Общее число элементов
        /// </summary>
        public override int ElementCount
        {
            get { return _elementsContainer.Count; }
        }

        /// <summary>
        /// Число свободных в данный момент элементов
        /// </summary>
        public int FreeElementCount
        {
            get { return _elementsContainer.AvailableCount; }
        }
        /// <summary>
        /// Число арендованных элементов
        /// </summary>
        private int RentedElementCount
        {
            get { return this.ElementCount - this.FreeElementCount; }
        }



        /// <summary>
        /// Создание элемента. Ожидание крайне не желательно. 
        /// Не должен кидать исключения, если только не надо прибить всю систему.
        /// </summary>
        /// <param name="elem">Созданный элемент, если удалось создать</param>
        /// <param name="timeout">Таймаут создания</param>
        /// <param name="token">Токен отмены создания элемента</param>
        /// <returns>Удалось ли создать элемент</returns>
        protected abstract bool CreateElement(out TElem elem, int timeout, CancellationToken token);
        /// <summary>
        /// Проверка, пригоден ли элемент для дальнейшего использования
        /// </summary>
        /// <param name="elem">Элемент</param>
        /// <returns>Пригоден ли для дальнейшего использования</returns>
        protected abstract bool IsValidElement(TElem elem);
        /// <summary>
        /// Уничтожить элемент
        /// </summary>
        /// <param name="elem">Элемент</param>
        protected abstract void DestroyElement(TElem elem);
        /// <summary>
        /// Сравнить. (a &gt; b =&gt; result &gt; 0; a == b =&gt; result == 0; a &lt; b =&gt; result &lt; 0)
        /// </summary>
        /// <param name="a">A</param>
        /// <param name="b">B</param>
        /// <param name="stopHere">Можно остановить поиск лучшего на текущем элементе</param>
        /// <returns>Результат сравнения</returns>
        protected abstract int CompareElements(TElem a, TElem b, out bool stopHere);
        /// <summary>
        /// Проверяет, не лучше ли выделить новый элемент в пуле вместо использования указанного
        /// </summary>
        /// <param name="elem">Элемент</param>
        /// <returns>Лучше ли выделить новый</returns>
        protected virtual bool IsBetterAllocateNew(TElem elem)
        {
            return false;
        }



        /// <summary>
        /// Наполнить пул до count элементов
        /// </summary>
        /// <param name="count">Число элементов, до которого наполняется пул</param>
        public void FillPoolUpTo(int count)
        {
            Contract.Requires<ArgumentException>(count >= 0);

            if (_disposeCancellation.IsCancellationRequested)
                throw new ObjectDisposedException(this.GetType().Name);

            count = Math.Min(count, this.MaxElementCount);
            int restCount = count - _reservedCount;

            PoolElementWrapper<TElem> initedElem = null;
            for (int i = 0; i < restCount; i++)
            {
                try
                {
                    initedElem = this.TryCreateNewElement(-1, new CancellationToken());
                }
                finally
                {
                    if (initedElem != null)
                    {
                        this.ReleaseElement(initedElem);
                        initedElem = null;
                    }
                }
            }
        }


        /// <summary>
        /// Уничтожить и удалить элемент
        /// </summary>
        /// <param name="element">Элемент</param>
        private void DestroyAndRemoveElement(PoolElementWrapper<TElem> element)
        {
            Contract.Requires(element != null);
            Contract.Requires(element.IsBusy);
            Contract.Requires(!element.IsElementDestroyed);

            try
            {
                this.DestroyElement(element.Element);
                element.MarkElementDestroyed();
            }
            finally
            {
                if (element.IsElementDestroyed)
                    Interlocked.Decrement(ref _reservedCount);

                _elementsContainer.Release(element);
            }

            Profiling.Profiler.ObjectPoolElementDestroyed(this.Name, this.ElementCount);
        }

        /// <summary>
        /// Забрать свободный элемент, уничтожить и удалить
        /// </summary>
        /// <returns>Был ли свободный элемент</returns>
        private bool TakeDestroyAndRemoveElement()
        {
            PoolElementWrapper<TElem> element = null;
            if (_elementsContainer.TryTakeWorst(out element, 0, new CancellationToken()))
            {
                DestroyAndRemoveElement(element);
                return true;
            }

            return false;
        }


        /// <summary>
        /// Инициализация нового элемента в пуле
        /// </summary>
        /// <param name="timeout">Таймаут инициализации</param>
        /// <param name="token">Токен отмены инициализации</param>
        /// <returns>Созданный и инициализированный элемент в случае успеха. Иначе null</returns>
        private PoolElementWrapper<TElem> TryCreateNewElement(int timeout, CancellationToken token)
        {
            if (_disposeCancellation.IsCancellationRequested || token.IsCancellationRequested)
                return null;
            if (Volatile.Read(ref _reservedCount) >= _maxElementCount)
                return null;

            _usedElementTracker.Reset();

            PoolElementWrapper<TElem> result = null;

            try
            {
                if (Interlocked.Increment(ref _reservedCount) > _maxElementCount)
                    return null;

                TElem element = default(TElem);
                if (CreateElement(out element, timeout, token))
                {
                    result = _elementsContainer.Add(element, this, false);
                    result.SetPoolName(this.Name);
                }
                else
                {
                    Profiling.Profiler.ObjectPoolElementFaulted(this.Name, this.ElementCount);
                }
            }
            finally
            {
                if (result == null)
                    Interlocked.Decrement(ref _reservedCount);
            }


            if (result != null)
                Profiling.Profiler.ObjectPoolElementCreated(this.Name, this.ElementCount);

            return result;
        }


        /// <summary>
        /// Обработать элемент, полученный из основного контейнера
        /// </summary>
        /// <param name="element">Элемент</param>
        /// <returns>Можно ли использовать выбранный элемент</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool ProcessTakenElement(ref PoolElementWrapper<TElem> element)
        {
            Contract.Requires(element != null);

            if (this.IsValidElement(element.Element))
                return true;

            DestroyAndRemoveElement(element);
            element = null;

            Profiling.Profiler.ObjectPoolElementFaulted(this.Name, this.ElementCount);
            return false;
        }

        /// <summary>
        /// Создать новый элемент, если необходимо
        /// </summary>
        /// <param name="element">Элемент</param>
        /// <param name="timeout">Таймаут</param>
        /// <param name="token">Токен отмены</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CreateNewInsteadOfTakenIfRequired(ref PoolElementWrapper<TElem> element, int timeout, CancellationToken token)
        {
            Contract.Requires(element != null);

            if (Volatile.Read(ref _reservedCount) < _maxElementCount && this.IsBetterAllocateNew(element.Element))
            {
                PoolElementWrapper<TElem> newElement = TryCreateNewElement(timeout, token);
                if (newElement != null)
                {
                    _elementsContainer.Release(element);
                    element = newElement;
                }
            }
        }

        /// <summary>
        /// Внутренний метод получения нового элемента.
        /// Он достаётся из конетйнера свободных, либо создаётся новый.
        /// </summary>
        /// <param name="timeout">Таймаут</param>
        /// <param name="token">Токен отмены</param>
        /// <returns>Полученный элемент (если удалось)</returns>
        private PoolElementWrapper<TElem> TryGetElement(int timeout, CancellationToken token)
        {
            if (_disposeCancellation.IsCancellationRequested || token.IsCancellationRequested)
                return null;

            PoolElementWrapper<TElem> result = null;
            while (_elementsContainer.TryTake(out result, 0, new CancellationToken()))
            {
                if (ProcessTakenElement(ref result))
                {
                    CreateNewInsteadOfTakenIfRequired(ref result, timeout, token);
                    return result;
                }
            }

            if (timeout == 0)
                return null;

            uint startTime = timeout > 0 ? GetTimestamp() : 0;
            int restTime = timeout;

            while (timeout < 0 || restTime > 0)
            {
                if (_disposeCancellation.IsCancellationRequested || token.IsCancellationRequested)
                    return null;

                result = TryCreateNewElement(restTime, token);
                if (result != null)
                    return result;

                restTime = UpdateTimeout(startTime, timeout);
                restTime = restTime > 0 ? Math.Min(_getRetryTimeout, restTime) : _getRetryTimeout;

                if (_elementsContainer.TryTake(out result, restTime, token))
                {
                    if (ProcessTakenElement(ref result))
                        return result;
                }

                restTime = UpdateTimeout(startTime, timeout);
            }
          
            return null;
        }






        /// <summary>
        /// Арендовать элемент
        /// </summary>
        /// <param name="timeout">Таймаут (-1 - бесконечно, 0 - быстрая проверка, > 0 - полная проверка)</param>
        /// <param name="token">Токен для отмены</param>
        /// <param name="throwOnUnavail">Выбрасывать исключение при недоступности элемента</param>
        /// <returns>Обёртка над элементом пула</returns>
        protected sealed override PoolElementWrapper<TElem> RentElement(int timeout, System.Threading.CancellationToken token, bool throwOnUnavail)
        {
            if (_disposeCancellation.IsCancellationRequested)
            {
                if (throwOnUnavail)
                    throw new ObjectDisposedException(this.GetType().Name);

                return null;
            }

            if (token.IsCancellationRequested)
            {
                if (throwOnUnavail)
                    token.ThrowIfCancellationRequested();

                return null;
            }


            if (timeout < 0)
                timeout = Timeout.Infinite;

            PoolElementWrapper<TElem> result = null;
            CancellationTokenSource linkedTokenSource = null;

            try
            {
                result = TryGetElement(0, new CancellationToken());

                if (result == null && timeout != 0)
                {
                    if (!token.CanBeCanceled)
                    {
                        result = TryGetElement(timeout, _disposeCancellation.Token);
                    }
                    else
                    {
                        linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token, _disposeCancellation.Token);
                        result = TryGetElement(timeout, linkedTokenSource.Token);
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                if (linkedTokenSource != null)
                    linkedTokenSource.Dispose();
            }


            // Обновление числа занятых элементов (для удаления неиспользуемых)
            _usedElementTracker.UpdateMinFreeElementCount(this.FreeElementCount);


            if (throwOnUnavail && result == null)
            {
                if (_disposeCancellation.IsCancellationRequested)
                    throw new CantRetrieveElementException("Rent from pool failed. Dispose was called.", new ObjectDisposedException(this.GetType().Name));

                token.ThrowIfCancellationRequested();

                if (timeout >= 0)
                    throw new TimeoutException(string.Format("Pool 'Rent' operation has timeouted. Pool: {0}. Timeout value: {1}ms", this.Name, timeout));

                Contract.Assert(false, "Element in pool is not available. Reason: UNKNOWN!");
                throw new CantRetrieveElementException("Rent from pool failed");
            }


            Profiling.Profiler.ObjectPoolElementRented(this.Name, this.RentedElementCount);
            return result;
        }



        /// <summary>
        /// Вернуть элемент в пул
        /// </summary>
        /// <param name="element">Элемент</param>
        protected internal sealed override void ReleaseElement(PoolElementWrapper<TElem> element)
        {
            if (!element.IsBusy)
                throw new InvalidOperationException("Trying to release same element several times in Pool: " + this.Name);

            _usedElementTracker.UpdateState();

            bool doTrim = _elementsContainer.Count > _minElementCount && _usedElementTracker.RequestElementToDestroy();
            bool isValid = this.IsValidElement(element.Element);

            if (_disposeCancellation.IsCancellationRequested || !isValid)
            {
                DestroyAndRemoveElement(element);
            }
            else
            {
                _elementsContainer.Release(element);

                if (_disposeCancellation.IsCancellationRequested || doTrim)
                    TakeDestroyAndRemoveElement();
            }

            if (!isValid)
                Profiling.Profiler.ObjectPoolElementFaulted(this.Name, this.ElementCount);

            Profiling.Profiler.ObjectPoolElementReleased(this.Name, this.RentedElementCount);
        }



        /// <summary>
        /// Является ли элемент валидным
        /// </summary>
        /// <param name="container">Контейнер элемента</param>
        /// <returns>Является ли валидным</returns>
        bool IPoolElementOperationSource<TElem>.IsValid(PoolElementWrapper<TElem> container)
        {
            return this.IsValidElement(container.Element);
        }



        /// <summary>
        /// Основной код освобождения ресурсов
        /// </summary>
        /// <param name="isUserCall">Вызвано ли освобождение пользователем. False - деструктор</param>
        protected override void Dispose(bool isUserCall)
        {
            if (isUserCall)
            {
                if (!_disposeCancellation.IsCancellationRequested)
                {
                    _disposeCancellation.Cancel();

                    try { }
                    finally
                    {
                        int count = _elementsContainer.Count;
                        while (TakeDestroyAndRemoveElement())
                            Contract.Assert(--count >= 0);
                    }

                    Profiling.Profiler.ObjectPoolDisposed(this.Name, false);
                }
            }

            base.Dispose(isUserCall);
        }
    }
}
