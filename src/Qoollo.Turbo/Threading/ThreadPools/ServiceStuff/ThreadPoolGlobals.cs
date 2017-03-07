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
    /// Глобальные данные для пула потоков
    /// </summary>
    internal class ThreadPoolGlobals: IDisposable
    {
        private readonly ThreadLocal<ThreadPoolThreadLocals> _perThreadData;
        private readonly ThreadPoolQueueController _queues;
        private readonly string _ownerPoolName;
        private volatile bool _isDisposed;


        /// <summary>
        /// Конструктор ThreadPoolGlobals
        /// </summary>
        /// <param name="queueBoundedCapacity">Ограничение на размер очереди</param>
        /// <param name="queueStealAwakePeriod">Периоды сна между проверкой возможности похитить элемент из соседних локальных очередей</param>
        /// <param name="ownerPoolName">Имя пула, к которому относится данный контейнер</param>
        public ThreadPoolGlobals(int queueBoundedCapacity, int queueStealAwakePeriod, string ownerPoolName)
        {
            _perThreadData = new ThreadLocal<ThreadPoolThreadLocals>(true);
            _queues = new ThreadPoolQueueController(queueBoundedCapacity, queueStealAwakePeriod);
            _ownerPoolName = ownerPoolName ?? "unknown";
            _isDisposed = false;
        }

        /// <summary>
        /// Есть ли данные, что текущий поток принадлежит пулу
        /// </summary>
        public bool IsThreadPoolThread { get { return _perThreadData.Value != null; } }
        /// <summary>
        /// Общая очередь
        /// </summary>
        public ThreadPoolGlobalQueue GlobalQueue { get { return _queues.GlobalQueue; } }
        /// <summary>
        /// Разреженный массив локальных очередей для потоков
        /// </summary>
        public ThreadPoolLocalQueue[] LocalQueues { get { return _queues.LocalQueues; } }
        /// <summary>
        /// Имя пула, к которому относится данный контейнер
        /// </summary>
        public string OwnerPoolName { get { return _ownerPoolName; } }

        /// <summary>
        /// Получить или создать данные для текущего потока.
        /// Предполагается, что вызывающий поток принадлежит пулу потоков
        /// </summary>
        /// <param name="createThreadLocalQueue">Создавать ли локальную очередь потока</param>
        /// <returns>Данные потока</returns>
        public ThreadPoolThreadLocals GetOrCreateThisThreadData(bool createThreadLocalQueue = true)
        {
            Contract.Ensures(Contract.Result<ThreadPoolThreadLocals>() != null);
            Debug.Assert(!_isDisposed);

            ThreadPoolThreadLocals result = _perThreadData.Value;
            if (result == null)
            {
                result = new ThreadPoolThreadLocals(this, createThreadLocalQueue);
                _perThreadData.Value = result;
                if (createThreadLocalQueue)
                    _queues.AddLocalQueue(result.LocalQueue);
            }

            return result;
        }
        /// <summary>
        /// Удалить локальные данные текущего потока
        /// </summary>
        public void FreeThisThreadData()
        {
            ThreadPoolThreadLocals data = _perThreadData.Value;
            if (data == null)
                return;
            if (data.LocalQueue != null)
                _queues.RemoveLocalQueue(data.LocalQueue);
            _perThreadData.Value = null;

            if (data.LocalQueue != null)
                _queues.MoveItemsFromLocalQueueToGlobal(data.LocalQueue);
            data.Dispose(); 
        }




        /// <summary>
        /// Расширить вместимость общей очереди
        /// </summary>
        /// <param name="extensionVal">Величина, на которую расширяем</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ExtendGlobalQueueCapacity(int extensionVal)
        {
            Contract.Requires(extensionVal >= 0);
            Debug.Assert(!_isDisposed);

            _queues.ExtendGlobalQueueCapacity(extensionVal);
        }

        /// <summary>
        /// Добавить элемент в очередь
        /// </summary>
        /// <param name="item">Единица работы</param>
        /// <param name="forceGlobal">Обязательное добавление в глобальную очередь</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddItem(ThreadPoolWorkItem item, bool forceGlobal)
        {
            Contract.Requires(item != null);
            Debug.Assert(!_isDisposed);

            ThreadPoolLocalQueue localQueue = null;

            if (!forceGlobal)
            {
                ThreadPoolThreadLocals threadLocal = _perThreadData.Value;
                localQueue = threadLocal != null ? threadLocal.LocalQueue : null;
            }

            _queues.Add(item, localQueue, forceGlobal);
        }
        /// <summary>
        /// Попробовать добавить элемент в очередь
        /// </summary>
        /// <param name="item">Единица работы</param>
        /// <param name="forceGlobal">Обязательное добавление в глобальную очередь</param>
        /// <returns>Успешность</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryAddItem(ThreadPoolWorkItem item, bool forceGlobal)
        {
            Contract.Requires(item != null);
            Debug.Assert(!_isDisposed);

            ThreadPoolLocalQueue localQueue = null;

            if (!forceGlobal)
            {
                ThreadPoolThreadLocals threadLocal = _perThreadData.Value;
                localQueue = threadLocal != null ? threadLocal.LocalQueue : null;
            }

            return _queues.TryAdd(item, localQueue, forceGlobal);
        }
        /// <summary>
        /// Забрать элемент из очереди
        /// </summary>
        /// <param name="local">Локальная очередь потока, если есть</param>
        /// <param name="token">Токен отмены</param>
        /// <returns>Полученный элемент</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ThreadPoolWorkItem TakeItem(ThreadPoolThreadLocals local, CancellationToken token)
        {        
            Contract.Ensures(Contract.Result<ThreadPoolWorkItem>() != null);
            Debug.Assert(!_isDisposed);

            if (local != null)
                return _queues.Take(local.LocalQueue, token);
            else
                return _queues.Take(null, token);
        }
        /// <summary>
        /// Попытаться получить элемент из очереди
        /// </summary>
        /// <param name="local">Локальная очередь потока, если есть</param>
        /// <param name="item">Выбранный элемент</param>
        /// <returns>Успешность</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryTakeItem(ThreadPoolThreadLocals local, out ThreadPoolWorkItem item)
        {
            Contract.Ensures(Contract.Result<bool>() == false || Contract.ValueAtReturn(out item) != null);
            Debug.Assert(!_isDisposed);

            if (local != null)
                return _queues.TryTake(local.LocalQueue, out item, 0, new CancellationToken(), true);
            else
                return _queues.TryTake(null, out item, 0, new CancellationToken(), true);
        }
        /// <summary>
        /// Попытаться получить элемент из очереди
        /// </summary>
        /// <param name="local">Локальная очередь потока, если есть</param>
        /// <param name="doLocalSearch">Выполнять ли поиск в локальной очереди</param>
        /// <param name="doWorkSteal">Разрешено ли похищение</param>
        /// <param name="item">Выбранный элемент</param>
        /// <param name="timeout">Таймаут</param>
        /// <param name="token">Токен отмены</param>
        /// <param name="throwOnCancellation">Выбрасывать ли исключение при отмене по токену</param>
        /// <returns>Успешность</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryTakeItem(ThreadPoolThreadLocals local, bool doLocalSearch, bool doWorkSteal, out ThreadPoolWorkItem item, int timeout, CancellationToken token, bool throwOnCancellation)
        {
            Contract.Ensures(Contract.Result<bool>() == false || Contract.ValueAtReturn(out item) != null);
            Debug.Assert(!_isDisposed);

            if (local != null)
                return _queues.TryTake(local.LocalQueue, doLocalSearch, doWorkSteal, out item, timeout, token, throwOnCancellation);
            else
                return _queues.TryTake(null, doLocalSearch, doWorkSteal, out item, timeout, token, throwOnCancellation);
        }
        /// <summary>
        /// Попытаться безопасно получить элемент из общей очереди
        /// </summary>
        /// <param name="item">Выбранный элемент</param>
        /// <returns>Успешность</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryTakeItemSafeFromGlobalQueue(out ThreadPoolWorkItem item)
        {
            return _queues.TryTakeSafeFromGlobalQueue(out item);
        }


        /// <summary>
        /// Освобождение ресурсов
        /// </summary>
        /// <param name="isUserCall">Вызвано ли пользователем</param>
        private void Dispose(bool isUserCall)
        {
            if (!_isDisposed)
            {
                _isDisposed = true;


                Debug.Assert(isUserCall, "Finalizer called for ThreadPoolGlobals. It should be disposed explicitly by calling Dispose on ThreadPool. ThreadPoolName: " + this.OwnerPoolName);

                if (isUserCall)
                {
                    var perThreadData = _perThreadData.Values.ToArray();
                    Debug.Assert(Contract.ForAll(perThreadData, o => o == null), "ThreadPoolGlobals contains thread information on Dispose");
                    if (perThreadData.Any(o => o != null))
                        throw new InvalidOperationException("ThreadPoolGlobals contains thread information on Dispose");

                    _perThreadData.Dispose();
                    _queues.Dispose();
                }
            }
        }

        /// <summary>
        /// Освобождение ресурсов
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ThreadPoolGlobals()
        {
            Dispose(false);
        }
    }
}
