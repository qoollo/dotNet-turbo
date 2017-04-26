using Qoollo.Turbo.Threading.ThreadPools.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Threading.ThreadPools.ServiceStuff
{
    /// <summary>
    /// Контроллер очереди для пула потоков
    /// </summary>
    internal class ThreadPoolGlobalQueue
    {
        private readonly ThreadPoolConcurrentQueue _mainQueue;
        private readonly int _boundedCapacity;
        private int _extendedCapacityRequest;

        private readonly SemaphoreLight _freeNodes;
        private readonly SemaphoreLight _occupiedNodes;

        private readonly object _safeDisposeLock;

        /// <summary>
        /// Конструктор ThreadPoolQueue
        /// </summary>
        /// <param name="boundedCapacity">Ограничение размера</param>
        public ThreadPoolGlobalQueue(int boundedCapacity)
        {
            _boundedCapacity = boundedCapacity > 0 ? boundedCapacity : -1;
            _extendedCapacityRequest = 0;
            _mainQueue = new ThreadPoolConcurrentQueue();
            _safeDisposeLock = new object();

            _freeNodes = null;
            if (boundedCapacity > 0)
                _freeNodes = new SemaphoreLight(boundedCapacity);
            _occupiedNodes = new SemaphoreLight(0);
        }

        /// <summary>
        /// Ограничение размера
        /// </summary>
        public int BoundedCapacity { get { return _boundedCapacity; } }
        /// <summary>
        /// Ограничена ли очередь
        /// </summary>
        public bool IsBounded { get { return _boundedCapacity > 0; } }
        /// <summary>
        /// Ограничение размера с учётом расширения
        /// </summary>
        public int ExtendedCapacity { get { return _boundedCapacity + Math.Max(0, _extendedCapacityRequest); } }

        /// <summary>
        /// Число свободных ячеек
        /// </summary>
        public int FreeNodesCount { get { return _freeNodes == null ? -1 : _freeNodes.CurrentCount; } }
        /// <summary>
        /// Число занятых ячеек
        /// </summary>
        public int OccupiedNodesCount { get { return _occupiedNodes.CurrentCount; } }
        /// <summary>
        /// Число потоков, ожидающих появления задачи
        /// </summary>
        public int TakeWaiterCount { get { return _occupiedNodes.WaiterCount; } }
        /// <summary>
        /// Число потоков, ожидающих возможности добавить задачу
        /// </summary>
        public int AddWaiterCount { get { return _freeNodes != null ? _freeNodes.WaiterCount : 0; } }

    
        /// <summary>
        /// Увеличить атомарно значение расширенного объёма очереди (_extendedCapacityRequest)
        /// </summary>
        /// <param name="extensionVal">Величина, на которую увеличиваем</param>
        private void UpdateExtendedCapacityRequestField(int extensionVal)
        {
            Contract.Requires(extensionVal >= 0);
            Debug.Assert(_boundedCapacity > 0);

            int maxExtensionValue = Math.Max(0, int.MaxValue - 1 - _boundedCapacity);

            SpinWait sw = new SpinWait();
            int currentExtendedCapacity = _extendedCapacityRequest;
            int newExtendedCapacityValue = Math.Max(0, currentExtendedCapacity) + extensionVal; 
            // Учитываем возможное переполнение Int32
            if (newExtendedCapacityValue < 0 || newExtendedCapacityValue > maxExtensionValue)
                newExtendedCapacityValue = maxExtensionValue;

            while (Interlocked.CompareExchange(ref _extendedCapacityRequest, newExtendedCapacityValue, currentExtendedCapacity) != currentExtendedCapacity)
            {
                sw.SpinOnce();

                currentExtendedCapacity = _extendedCapacityRequest;
                newExtendedCapacityValue = Math.Max(0, currentExtendedCapacity) + extensionVal; 
                // Учитываем возможное переполнение Int32
                if (newExtendedCapacityValue < 0 || newExtendedCapacityValue > maxExtensionValue)
                    newExtendedCapacityValue = maxExtensionValue;
            }
        }
        /// <summary>
        /// Запросить временное расширение вместимости
        /// </summary>
        /// <param name="extensionVal">Величиная расширения</param>
        public void RequestCapacityExtension(int extensionVal)
        {
            Contract.Requires<ArgumentException>(extensionVal >= 0);

            if (_freeNodes == null || extensionVal == 0)
                return;

            _freeNodes.Release(extensionVal);
            UpdateExtendedCapacityRequestField(extensionVal);
        }


        /// <summary>
        /// Форсировать добавление элемента в главную очередь (игнорирует ограничения по размеру)
        /// </summary>
        /// <param name="item">Элемент</param>
        public void ForceAdd(ThreadPoolWorkItem item)
        {
            Contract.Requires(item != null);

            _mainQueue.Add(item);
            if (_freeNodes != null)
                UpdateExtendedCapacityRequestField(1);
            _occupiedNodes.Release();
        }


        /// <summary>
        /// Попытаться добавить элемент в главную очередь
        /// </summary>
        /// <param name="item">Элемент</param>
        /// <param name="timeout">Таймаут</param>
        /// <returns>Удалось ли добавить</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryAdd(ThreadPoolWorkItem item, int timeout)
        {
            Contract.Requires(item != null);

            bool freeNodesExist = true;
            if (_freeNodes != null)
            {
                freeNodesExist = _freeNodes.Wait(0);
                if (freeNodesExist == false && timeout != 0)
                    freeNodesExist = _freeNodes.Wait(timeout);
            }

            if (freeNodesExist)
            {
                _mainQueue.Add(item);
                _occupiedNodes.Release();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Попытаться добавить элемент в главную очередь
        /// </summary>
        /// <param name="item">Элемент</param>
        /// <param name="timeout">Таймаут</param>
        /// <param name="token">Токен отмены</param>
        /// <returns>Удалось ли добавить</returns>
        public bool TryAdd(ThreadPoolWorkItem item, int timeout, CancellationToken token)
        {
            Contract.Requires(item != null);

            if (token.IsCancellationRequested)
                throw new OperationCanceledException(token);

              
            bool freeNodesExist = true;
            if (_freeNodes != null)
            {
                freeNodesExist = _freeNodes.Wait(0);
                if (freeNodesExist == false && timeout != 0)
                    freeNodesExist = _freeNodes.Wait(timeout, token);
            }

            if (freeNodesExist)
            {
                try
                {
                    token.ThrowIfCancellationRequested();
                    _mainQueue.Add(item);
                }
                catch
                {
                    if (_freeNodes != null)
                        _freeNodes.Release();

                    throw;
                }
                _occupiedNodes.Release();
            }
            return freeNodesExist;
        }
        /// <summary>
        /// Добавить элемент в очередь
        /// </summary>
        /// <param name="item">Элемент</param>
        public void Add(ThreadPoolWorkItem item)
        {
            Contract.Requires(item != null);

            bool result = TryAdd(item, Timeout.Infinite);
            Debug.Assert(result, "Element was not added to ThreadPoolGlobalQueue due to unknown reason");
        }


     




        /// <summary>
        /// Попытаться получить элемент из главной очереди
        /// </summary>
        /// <param name="item">Полученный элемент</param>
        /// <param name="timeout">Таймаут</param>
        /// <param name="token">Токен отмены</param>
        /// <param name="throwOnCancellation">Выбрасывать ли исключение при отмене по токену</param>
        /// <returns>Удалось ли получить</returns>
        public bool TryTake(out ThreadPoolWorkItem item, int timeout, CancellationToken token, bool throwOnCancellation)
        {
            Contract.Ensures(Contract.Result<bool>() == false || Contract.ValueAtReturn(out item) != null);

            item = null;

            if (token.IsCancellationRequested)
            {
                if (throwOnCancellation)
                    throw new OperationCanceledException(token);

                return false;
            }

            bool occupiedNodesExist = _occupiedNodes.Wait(0);
            if (occupiedNodesExist == false && timeout != 0)
                occupiedNodesExist = _occupiedNodes.Wait(timeout, token, throwOnCancellation);

            if (occupiedNodesExist)
            {
                bool wasElementTaken = false;
                bool takePerformed = true;
                try
                {
                    if (token.IsCancellationRequested)
                    {
                        if (throwOnCancellation)
                            throw new OperationCanceledException(token);

                        return false;
                    }
                    wasElementTaken = _mainQueue.TryTake(out item);
                    takePerformed = false;
                    Debug.Assert(wasElementTaken, "Incorrect collection state. Can't take items from collection when they should be there");
                }
                finally
                {
                    if (wasElementTaken)
                    {
                        if (_freeNodes != null)
                        {
                            // Не освобождаем, если запрошен экстеншн
                            if (Volatile.Read(ref _extendedCapacityRequest) <= 0 || Interlocked.Decrement(ref _extendedCapacityRequest) < 0)
                                _freeNodes.Release();
                        }
                    }
                    else
                    {
                        if (takePerformed)
                            _occupiedNodes.Release();
                    }
                }
            }
            return occupiedNodesExist;
        }
        /// <summary>
        /// Забрать элемент из очереди
        /// </summary>
        /// <returns>Полученный элемент</returns>
        public ThreadPoolWorkItem Take()
        {
            Contract.Ensures(Contract.Result<ThreadPoolWorkItem>() != null);

            ThreadPoolWorkItem result = null;
            bool success = TryTake(out result, Timeout.Infinite, new CancellationToken(), true);
            Debug.Assert(success, "Element was not taken from ThreadPoolGlobalQueue due to unknown reason");
            return result;
        }
    }
}
