using Qoollo.Turbo.ObjectPools.Common;
using Qoollo.Turbo.ObjectPools.ServiceStuff.ElementCollections;
using Qoollo.Turbo.Threading;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.ObjectPools.ServiceStuff.ElementContainers
{
    /// <summary>
    /// Простой контейнер элементов со стеком свободных элементов.
    /// Не производит выделение памяти на каждый чих, за счёт чего обыгрывает ConcurrentStack
    /// </summary>
    /// <typeparam name="T">Тип элементов пула</typeparam>
    internal class SimpleElementsContainer<T>: IDisposable
    {
        private readonly SparceArrayStorage<PoolElementWrapper<T>> _allElements;
        private readonly BunchElementStorage<T> _globalFreeElements;
        private readonly SemaphoreLight _occupiedElements;

        private volatile bool _isDisposed;

        /// <summary>
        /// Конструктор SimpleElementsContainer
        /// </summary>
        public SimpleElementsContainer()
        {
            _allElements = new SparceArrayStorage<PoolElementWrapper<T>>(false);
            _globalFreeElements = new BunchElementStorage<T>(_allElements);
            _occupiedElements = new SemaphoreLight(0);
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
            TurboContract.Requires(operations != null, conditionString: "operations != null");
            TurboContract.Assert(!_isDisposed, conditionString: "!_isDisposed");

            PoolElementWrapper<T> container = new PoolElementWrapper<T>(rawElement, operations, this);
            container.MakeBusy();
            container.ThisIndex = _allElements.Add(container);

            TurboContract.Assert(container.ThisIndex >= 0 && container.ThisIndex < _allElements.Capacity, conditionString: "container.ThisIndex >= 0 && container.ThisIndex < _allElements.Capacity");
            TurboContract.Assert(object.ReferenceEquals(container, _allElements.RawData[container.ThisIndex]), conditionString: "object.ReferenceEquals(container, _allElements.RawData[container.ThisIndex])");

            if (container.ThisIndex >= (1 << 16) - 2)
            {
                container.MarkElementDestroyed();
                _allElements.RemoveAt(container.ThisIndex);
                container.MarkRemoved();

                throw new InvalidOperationException("Can't add more elements. Max supported element count = " + ((1 << 16) - 2).ToString());
            }

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
            TurboContract.Requires(element != null, conditionString: "element != null");
            TurboContract.Requires(element.Owner == this, conditionString: "element.Owner == this");
            TurboContract.Requires(element.IsBusy, conditionString: "element.IsBusy");

            if (element.IsRemoved)
                return;

            TurboContract.Assert(element.ThisIndex >= 0 && element.ThisIndex < _allElements.Capacity, conditionString: "element.ThisIndex >= 0 && element.ThisIndex < _allElements.Capacity");
            TurboContract.Assert(object.ReferenceEquals(element, _allElements.RawData[element.ThisIndex]), conditionString: "object.ReferenceEquals(element, _allElements.RawData[element.ThisIndex])");

            bool removeResult = _allElements.RemoveAt(element.ThisIndex);
            TurboContract.Assert(removeResult == true, conditionString: "removeResult == true");
            TurboContract.Assert(_allElements.IndexOf(element) < 0, conditionString: "_allElements.IndexOf(element) < 0");
            element.MarkRemoved();
            _allElements.Compact(false);
        }


        /// <summary>
        /// Основной код возвращения элемента в множество свободных
        /// </summary>
        /// <param name="element">Элемент</param>
        private void ReleaseCore(PoolElementWrapper<T> element)
        {
            TurboContract.Requires(element != null, conditionString: "element != null");

            try { }
            finally
            {
                element.MakeAvailable();
                _globalFreeElements.Add(element);
                _occupiedElements.Release();
            }
        }

        /// <summary>
        /// Вернуть элемент в список свободных
        /// </summary>
        /// <param name="element">Элемент</param>
        public void Release(PoolElementWrapper<T> element)
        {
            TurboContract.Requires(element != null, conditionString: "element != null");
            TurboContract.Requires(element.Owner == this, conditionString: "element.Owner == this");

            if (element.IsElementDestroyed)
            {
                PerformRealRemove(element);
                element.MakeAvailable();
                return;
            }

            TurboContract.Assert(element.ThisIndex >= 0 && element.ThisIndex < _allElements.Capacity, conditionString: "element.ThisIndex >= 0 && element.ThisIndex < _allElements.Capacity");
            TurboContract.Assert(object.ReferenceEquals(element, _allElements.RawData[element.ThisIndex]), conditionString: "object.ReferenceEquals(element, _allElements.RawData[element.ThisIndex])");

#pragma warning disable 0420
            _allElements.CompactElementAt(ref element.ThisIndex);
#pragma warning restore 0420

            TurboContract.Assert(element.ThisIndex >= 0 && element.ThisIndex < _allElements.Capacity, conditionString: "element.ThisIndex >= 0 && element.ThisIndex < _allElements.Capacity");
            TurboContract.Assert(object.ReferenceEquals(element, _allElements.RawData[element.ThisIndex]), conditionString: "object.ReferenceEquals(element, _allElements.RawData[element.ThisIndex])");

            ReleaseCore(element);
        }


        /// <summary>
        /// Основной код выборки элемента
        /// </summary>
        /// <param name="element">Выбранный элемент</param>
        /// <returns>Успешность</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryTakeCore(out PoolElementWrapper<T> element)
        {
            _globalFreeElements.Take(out element);
            element.MakeBusy();
            return true;
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
                TurboContract.Assert(removeSucceeded, "Take from underlying collection return false");
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
            TurboContract.Assert(takeSuccess, "Element was not taken from SimpleElementStorage");
            return result;
        }


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
            TurboContract.Requires(action != null, conditionString: "action != null");

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
            TurboContract.Requires(action != null, conditionString: "action != null");


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
