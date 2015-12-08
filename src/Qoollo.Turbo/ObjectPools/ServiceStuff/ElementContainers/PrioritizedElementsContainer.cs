using Qoollo.Turbo.ObjectPools.Common;
using Qoollo.Turbo.ObjectPools.ServiceStuff.ElementCollections;
using Qoollo.Turbo.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.ObjectPools.ServiceStuff.ElementContainers
{
    /// <summary>
    /// Контейнер элементов с приоритезацией. При выборке берётся лучший из доступных.
    /// </summary>
    /// <typeparam name="T">Тип элементов</typeparam>
    internal class PrioritizedElementsContainer<T> : IDisposable
    {
        private readonly SparceArrayStorage<PoolElementWrapper<T>> _allElements;
        private readonly PoolElementComparer<T> _comparer;
        private readonly SemaphoreLight _occupiedElements;
        private readonly object _syncObject;

        private volatile bool _isDisposed;

        /// <summary>
        /// Конструктор PrioritizedElementsContainer
        /// </summary>
        /// <param name="comparer">Объект для сравнения элементов</param>
        public PrioritizedElementsContainer(PoolElementComparer<T> comparer)
        {
            Contract.Requires<ArgumentNullException>(comparer != null);

            _allElements = new SparceArrayStorage<PoolElementWrapper<T>>(true);
            _comparer = comparer;
            _occupiedElements = new SemaphoreLight(0);
            _syncObject = new object();
        }

        /// <summary>
        /// Число элементов
        /// </summary>
        public int Count { get { return _allElements.Count; } }
        /// <summary>
        /// Число доступных для использования элементов
        /// </summary>
        public int AvailableCount { get { return _occupiedElements.CurrentCount; } }


        /// <summary>
        /// Добавить новый элемент
        /// </summary>
        /// <param name="rawElement">Элемент</param>
        /// <param name="operations">Операции для элемента</param>
        /// <param name="makeAvailable">Сделать элемент сразу доступным для использования</param>
        /// <returns>Обёртка для нового элемента</returns>
        public PoolElementWrapper<T> Add(T rawElement, IPoolElementOperationSource<T> operations, bool makeAvailable)
        {
            Contract.Requires(operations != null);
            Contract.Assert(!_isDisposed);

            PoolElementWrapper<T> container = new PoolElementWrapper<T>(rawElement, operations, this);
            container.MakeBusyAtomic();
            _allElements.Add(container);

            if (makeAvailable)
                ReleaseCore(container);

            return container;
        }


        /// <summary>
        /// Выполнить удаление элемента из контейнера
        /// </summary>
        /// <param name="element">Элемент</param>
        private void PerformRealRemove(PoolElementWrapper<T> element)
        {
            Contract.Requires(element != null);
            Contract.Requires(element.Owner == this);
            Contract.Requires(element.IsBusy);

            if (element.IsRemoved)
                return;

            bool removeResult = _allElements.Remove(element);
            Contract.Assert(removeResult == true);
            Contract.Assert(_allElements.IndexOf(element) < 0);
            element.MarkRemoved();
        }


        /// <summary>
        /// Основной код возвращения элемента в множество свободных
        /// </summary>
        /// <param name="element">Элемент</param>
        private void ReleaseCore(PoolElementWrapper<T> element)
        {
            Contract.Requires(element != null);

            try { }
            finally
            {
                element.MakeAvailableAtomic();
                _occupiedElements.Release();
            }
        }

        /// <summary>
        /// Вернуть элемент в список свободных
        /// </summary>
        /// <param name="element">Элемент</param>
        public void Release(PoolElementWrapper<T> element)
        {
            Contract.Requires(element != null);
            Contract.Requires(element.Owner == this);

            if (element.IsElementDestroyed)
            {
                PerformRealRemove(element);
                return;
            }

            ReleaseCore(element);
        }

        // ========================= Take Best ==================

        /// <summary>
        /// Найти лучший элемент в контейнере
        /// </summary>
        /// <returns>Лучший элемент, если есть</returns>
        private PoolElementWrapper<T> FindBest()
        {
            PoolElementWrapper<T> bestItem = null;
            bool stopHere = false;

            var rawItems = _allElements.RawData;
            for (int i = 0; i < rawItems.Length; i++)
            {
                var curItem = Volatile.Read(ref rawItems[i]);
                if (curItem == null || curItem.IsBusy)
                    continue;

                if (bestItem == null || _comparer.Compare(curItem, bestItem, out stopHere) > 0)
                    bestItem = curItem;
            }

            return bestItem;
        }


        /// <summary>
        /// Найти 3 лучших элемента с учётом флага раннего останова.
        /// Если выполнилось условие раннего останова, то возвращается элемент, сразу помеченный как busy.
        /// </summary>
        /// <param name="b1">Лучший</param>
        /// <param name="b2">Второй</param>
        /// <param name="b3">Третий</param>
        /// <returns>Захваченный при ранней остановке элемент</returns>
        private PoolElementWrapper<T> FindBest3WithStopping(out PoolElementWrapper<T> b1, out PoolElementWrapper<T> b2, out PoolElementWrapper<T> b3)
        {
            PoolElementWrapper<T> bestItem1 = null;
            PoolElementWrapper<T> bestItem2 = null;
            PoolElementWrapper<T> bestItem3 = null;
            bool stopHere = false;

            var rawItems = _allElements.RawData;
            for (int i = 0; i < rawItems.Length; i++)
            {
                var curItem = Volatile.Read(ref rawItems[i]);
                if (curItem == null || curItem.IsBusy)
                    continue;

                stopHere = false;
                if (bestItem1 == null || _comparer.Compare(curItem, bestItem1, out stopHere) > 0)
                {
                    if (stopHere && curItem.TryMakeBusyAtomic() && !curItem.IsRemoved)
                    {
                        b1 = null;
                        b2 = null;
                        b3 = null;
                        return curItem;
                    }

                    if (!stopHere)
                    {
                        bestItem3 = bestItem2;
                        bestItem2 = bestItem1;
                        bestItem1 = curItem;
                    }
                }
                else if (bestItem2 == null || _comparer.Compare(curItem, bestItem2, out stopHere) > 0)
                {
                    bestItem3 = bestItem2;
                    bestItem2 = curItem;
                }
                else if (bestItem3 == null || _comparer.Compare(curItem, bestItem2, out stopHere) > 0)
                {
                    bestItem3 = curItem;
                }
            }

            b1 = bestItem1;
            b2 = bestItem2;
            b3 = bestItem3;
            return null;
        }

        /// <summary>
        /// Захватить лучший элемент из доступных
        /// </summary>
        /// <param name="element">Захваченный элемент</param>
        /// <returns>Удалось ли захватить</returns>
        private bool TryTakeBest(out PoolElementWrapper<T> element)
        {
            Contract.Ensures((Contract.Result<bool>() == false && Contract.ValueAtReturn(out element) == null) ||
                             (Contract.Result<bool>() == true && Contract.ValueAtReturn(out element) != null && Contract.ValueAtReturn(out element).IsBusy));

            PoolElementWrapper<T> b1, b2, b3;
            PoolElementWrapper<T> earlyStopResult = FindBest3WithStopping(out b1, out b2, out b3);

            if (earlyStopResult != null)
            {
                Contract.Assert(earlyStopResult.IsBusy);
                element = earlyStopResult;
                return true;
            }
            if (b1 != null && b1.TryMakeBusyAtomic() && !b1.IsRemoved)
            {
                element = b1;
                return true;
            }
            if (b2 != null && b2.TryMakeBusyAtomic() && !b2.IsRemoved)
            {
                element = b2;
                return true;
            }
            if (b3 != null && b3.TryMakeBusyAtomic() && !b3.IsRemoved)
            {
                element = b3;
                return true;
            }

            element = null;
            return false;
        }



        /// <summary>
        /// Основной код выборки элемента
        /// </summary>
        /// <param name="element">Выбранный элемент</param>
        /// <returns>Успешность</returns>
        private bool TryTakeCore(out PoolElementWrapper<T> element)
        {
            if (TryTakeBest(out element))
                return true;

            lock (_syncObject)
            {
                int tryCount = 0;
                while (true)
                {
                    if (TryTakeBest(out element))
                        return true;

                    Contract.Assert(tryCount++ < 1000);
                }
            }
        }



        /// <summary>
        /// Выбрать элемент из списка свободных
        /// </summary>
        /// <param name="element">Выбранный элемент</param>
        /// <param name="timeout">Таймаут</param>
        /// <param name="token">Токен отмены</param>
        /// <returns>Успешность</returns>
        private bool TryTakeInner(out PoolElementWrapper<T> element, int timeout, CancellationToken token)
        {
            element = null;

            if (token.IsCancellationRequested)
                throw new OperationCanceledException(token);


            bool waitForSemaphoreWasSuccessful = _occupiedElements.Wait(0);
            if (waitForSemaphoreWasSuccessful == false && timeout != 0)
                waitForSemaphoreWasSuccessful = _occupiedElements.Wait(timeout, token);

            if (!waitForSemaphoreWasSuccessful)
                return false;

            bool removeSucceeded = false;
            bool removeFaulted = true;
            try
            {
                //token.ThrowIfCancellationRequested(); // TODO: Refactor the code
                removeSucceeded = this.TryTakeCore(out element);
                Contract.Assert(removeSucceeded, "Take from underlying collection return false");
                removeFaulted = false;
            }
            finally
            {
                if (!removeSucceeded && removeFaulted)
                    _occupiedElements.Release();
            }

            return removeSucceeded;
        }

        /// <summary>
        /// Выбрать элемент из списка свободных. С повтором в случае возможности удалить элемент
        /// </summary>
        /// <param name="element">Выбранный элемент</param>
        /// <param name="timeout">Таймаут</param>
        /// <param name="token">Токен отмены</param>
        /// <returns>Успешность</returns>
        private bool TryTakeWithRemoveInner(out PoolElementWrapper<T> element, int timeout, CancellationToken token)
        {
            while (this.TryTakeInner(out element, timeout, token))
            {
                if (!element.IsElementDestroyed)
                    return true;

                PerformRealRemove(element);
            }

            return false;
        }

        /// <summary>
        /// Выбрать элемент из списка свободных
        /// </summary>
        /// <param name="element">Выбранный элемент</param>
        /// <param name="timeout">Таймаут</param>
        /// <param name="token">Токен отмены</param>
        /// <returns>Успешность</returns>
        public bool TryTake(out PoolElementWrapper<T> element, int timeout, CancellationToken token)
        {
            return TryTakeWithRemoveInner(out element, timeout, token);
        }

        /// <summary>
        /// Получить свободный элемент (заблокируется, пока не появится таковой)
        /// </summary>
        /// <returns>Элемент</returns>
        public PoolElementWrapper<T> Take()
        {
            PoolElementWrapper<T> result = null;
            bool takeSuccess = TryTakeWithRemoveInner(out result, Timeout.Infinite, new CancellationToken());
            Contract.Assert(takeSuccess, "Element was not taken from SimpleElementStorage");
            return result;
        }





        // ========================= Take Worst ==================


        /// <summary>
        /// Найти худший элемент в контейнере
        /// </summary>
        /// <returns>Худший элемент, если есть</returns>
        private PoolElementWrapper<T> FindWorst()
        {
            PoolElementWrapper<T> worstItem = null;
            bool stopHere = false;

            var rawItems = _allElements.RawData;
            for (int i = 0; i < rawItems.Length; i++)
            {
                var curItem = Volatile.Read(ref rawItems[i]);
                if (curItem == null || curItem.IsBusy)
                    continue;

                if (worstItem == null || _comparer.Compare(curItem, worstItem, out stopHere) < 0)
                    worstItem = curItem;
            }

            return worstItem;
        }

        /// <summary>
        /// Захватить худший элемент из доступных
        /// </summary>
        /// <param name="element">Захваченный элемент</param>
        /// <returns>Удалось ли захватить</returns>
        private bool TryTakeWorst(out PoolElementWrapper<T> element)
        {
            PoolElementWrapper<T> tmp = FindWorst();
            if (tmp != null && tmp.TryMakeBusyAtomic() && !tmp.IsRemoved)
            {
                element = tmp;
                return true;
            }

            element = null;
            return false;
        }



        /// <summary>
        /// Основной код выборки худшего элемента
        /// </summary>
        /// <param name="element">Выбранный элемент</param>
        /// <returns>Успешность</returns>
        private bool TryTakeWorstCore(out PoolElementWrapper<T> element)
        {
            if (TryTakeWorst(out element))
                return true;

            lock (_syncObject)
            {
                int tryCount = 0;
                while (true)
                {
                    if (TryTakeWorst(out element))
                        return true;

                    Contract.Assert(tryCount++ < 1000);
                }
            }
        }


        /// <summary>
        /// Выбрать худший элемент из списка свободных
        /// </summary>
        /// <param name="element">Выбранный элемент</param>
        /// <param name="timeout">Таймаут</param>
        /// <param name="token">Токен отмены</param>
        /// <returns>Успешность</returns>
        private bool TryTakeWorstInner(out PoolElementWrapper<T> element, int timeout, CancellationToken token)
        {
            element = null;

            if (token.IsCancellationRequested)
                throw new OperationCanceledException(token);


            bool waitForSemaphoreWasSuccessful = _occupiedElements.Wait(0);
            if (waitForSemaphoreWasSuccessful == false && timeout != 0)
                waitForSemaphoreWasSuccessful = _occupiedElements.Wait(timeout, token);

            if (!waitForSemaphoreWasSuccessful)
                return false;

            bool removeSucceeded = false;
            bool removeFaulted = true;
            try
            {
                //token.ThrowIfCancellationRequested();  // TODO: Refactor the code
                removeSucceeded = this.TryTakeWorstCore(out element);
                Contract.Assert(removeSucceeded, "Take from underlying collection return false");
                removeFaulted = false;
            }
            finally
            {
                if (!removeSucceeded && removeFaulted)
                    _occupiedElements.Release();
            }

            return removeSucceeded;
        }


        /// <summary>
        /// Выбрать худший элемент из списка свободных
        /// </summary>
        /// <param name="element">Выбранный элемент</param>
        /// <param name="timeout">Таймаут</param>
        /// <param name="token">Токен отмены</param>
        /// <returns>Успешность</returns>
        public bool TryTakeWorst(out PoolElementWrapper<T> element, int timeout, CancellationToken token)
        {
            while (this.TryTakeWorstInner(out element, timeout, token))
            {
                if (!element.IsElementDestroyed)
                    return true;

                PerformRealRemove(element);
            }

            return false;
        }



        // ==============================




        /// <summary>
        /// Есть ли элементы, доступные для удаления
        /// </summary>
        /// <returns>Есть ли</returns>
        private bool HasAvailableForRemoveElements()
        {
            var rawData = _allElements.RawData;
            for (int i = 0; i < rawData.Length; i++)
            {
                var elem = Volatile.Read(ref rawData[i]);
                if (elem != null && elem.IsElementDestroyed)
                    return true;
            }

            return false;
        }


        /// <summary>
        /// Пересканировать контейнер и удалить элементы, которые можно удалить
        /// </summary>
        public void RescanContainer()
        {
            bool needRemove = HasAvailableForRemoveElements();

            if (needRemove)
            {
                List<PoolElementWrapper<T>> takenElems = new List<PoolElementWrapper<T>>(_allElements.Count + 1);

                try { }
                finally
                {
                    PoolElementWrapper<T> tmp;
                    while (this.TryTakeInner(out tmp, 0, new CancellationToken()))
                        takenElems.Add(tmp);

                    foreach (var elem in takenElems)
                        this.Release(elem);
                }
            }
        }


        /// <summary>
        /// Обработать свободные элементы (забирает, обрабатывает, возвращает)
        /// </summary>
        /// <param name="action">Действие</param>
        public void ProcessFreeElements(Action<PoolElementWrapper<T>> action)
        {
            Contract.Requires(action != null);

            List<PoolElementWrapper<T>> takenElems = new List<PoolElementWrapper<T>>(_allElements.Count + 1);

            try
            {
                PoolElementWrapper<T> tmp = null;
                while (this.TryTakeInner(out tmp, 0, new CancellationToken()))
                {
                    takenElems.Add(tmp);
                    action(tmp);
                }
            }
            finally
            {
                foreach (var elem in takenElems)
                    this.Release(elem);
            }
        }

        /// <summary>
        /// Обработать все элементы
        /// </summary>
        /// <param name="action">Действие</param>
        public void ProcessAllElements(Action<PoolElementWrapper<T>> action)
        {
            Contract.Requires(action != null);


            var rawArray = _allElements.RawData;
            for (int i = 0; i < rawArray.Length; i++)
            {
                var elem = Volatile.Read(ref rawArray[i]);
                if (elem != null)
                    action(elem);
            }


            this.RescanContainer();
        }



        /// <summary>
        /// Внутренний код освобождения ресурсов
        /// </summary>
        /// <param name="isUserCall">Вызван ли пользователем</param>
        protected virtual void Dispose(bool isUserCall)
        {
            if (!_isDisposed)
            {
                _isDisposed = true;

                if (isUserCall)
                    RescanContainer();
            }
        }

        /// <summary>
        /// Освободить ресурсы
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
