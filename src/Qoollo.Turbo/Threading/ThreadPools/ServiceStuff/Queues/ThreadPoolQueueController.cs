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
    internal class ThreadPoolQueueController : IDisposable
    {
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
            uint elapsed = GetTimestamp() - startTime;
            if (elapsed > (uint)int.MaxValue)
                return 0;

            int rest = originalTimeout - (int)elapsed;
            if (rest <= 0)
                return 0;

            return rest;
        }

        // ===================


        private readonly ThreadPoolGlobalQueue _globalQueue;
        private volatile ThreadPoolLocalQueue[] _localQueues;
        private int _stealIndex;
        private readonly int _stealAwakePeriod;
        private readonly object _syncObj;
        private volatile bool _isDisposed;


        public ThreadPoolQueueController(int globalQueueBoundedCapacity, int stealAwakePeriod)
        {
            _globalQueue = new ThreadPoolGlobalQueue(globalQueueBoundedCapacity);
            _localQueues = new ThreadPoolLocalQueue[4];
            _stealAwakePeriod = stealAwakePeriod;
            _syncObj = new object();
            _stealIndex = 0;
            _isDisposed = false;
        }


        public ThreadPoolGlobalQueue GlobalQueue { get { return _globalQueue; } }
        public ThreadPoolLocalQueue[] LocalQueues { get { return _localQueues; } }



        /// <summary>
        /// Добавить локальную оередь в массив
        /// </summary>
        /// <param name="localQueue">Локальная очередь потока</param>
        public void AddLocalQueue(ThreadPoolLocalQueue localQueue)
        {
            Contract.Requires(localQueue != null);
            Contract.Ensures(Contract.Exists(_localQueues, o => o == localQueue));

            Debug.Assert(!_isDisposed);

            lock (_syncObj)
            {
                var arrayCopy = _localQueues;
                Debug.Assert(arrayCopy != null);

                for (int i = 0; i < arrayCopy.Length; i++)
                {
                    Debug.Assert(Volatile.Read(ref arrayCopy[i]) != localQueue);

                    if (Volatile.Read(ref arrayCopy[i]) == null)
                    {
                        Volatile.Write(ref arrayCopy[i], localQueue);
                        return;
                    }
                }

                int oldLength = arrayCopy.Length;
                Array.Resize(ref arrayCopy, oldLength + 4);

                Volatile.Write(ref arrayCopy[oldLength], localQueue);
                _localQueues = arrayCopy;
            }
        }
        /// <summary>
        /// Удалить локальную очередь из массива
        /// </summary>
        /// <param name="localQueue">Локальная очередь</param>
        public void RemoveLocalQueue(ThreadPoolLocalQueue localQueue)
        {
            Contract.Requires(localQueue != null);
            Contract.Ensures(!Contract.Exists(_localQueues, o => o == localQueue));

            Debug.Assert(!_isDisposed);

            lock (_syncObj)
            {
                var arrayCopy = _localQueues;
                Debug.Assert(arrayCopy != null);

                for (int i = 0; i < arrayCopy.Length; i++)
                {
                    if (object.ReferenceEquals(Volatile.Read(ref arrayCopy[i]), localQueue))
                    {
                        Volatile.Write(ref arrayCopy[i], null);
                        return;
                    }
                }
            }
        }



        // ======================


        /// <summary>
        /// Попробовать добавить элемент в очередь
        /// </summary>
        /// <param name="item">Элемент</param>
        /// <param name="localQueue">Локальная очередь потока (если есть)</param>
        /// <param name="forceGlobal">Обязательное добавление в глобальную очередь</param>
        /// <param name="timeout">Таймаут</param>
        /// <param name="token">Токен отмены</param>
        /// <returns>Удалось ли добавить</returns>
        public bool TryAdd(ThreadPoolWorkItem item, ThreadPoolLocalQueue localQueue, bool forceGlobal, int timeout, CancellationToken token)
        {
            Contract.Requires(item != null);
            Debug.Assert(!_isDisposed);

            if (localQueue != null && !forceGlobal)
            {
                bool added = false;
                try { }
                finally
                {
                    added = localQueue.TryAddLocal(item);
                }
                if (added)
                    return true;
            }

            return _globalQueue.TryAdd(item, timeout, token);
        }
        /// <summary>
        /// Попробовать добавить элемент в очередь
        /// </summary>
        /// <param name="item">Элемент</param>
        /// <param name="localQueue">Локальная очередь (если есть)</param>
        /// <param name="forceGlobal">Обязательное добавление в глобальную очередь</param>
        /// <returns>Удалось ли добавить</returns>
        public bool TryAdd(ThreadPoolWorkItem item, ThreadPoolLocalQueue localQueue, bool forceGlobal)
        {
            Contract.Requires(item != null);
            Debug.Assert(!_isDisposed);

            bool result = false;
            try { }
            finally
            {
                if (!forceGlobal && localQueue != null && localQueue.TryAddLocal(item))
                    result = true;
                else
                    result = _globalQueue.TryAdd(item, 0);
            }

            return result;
        }
        /// <summary>
        /// Попробовать добавить элемент в очередь
        /// </summary>
        /// <param name="item">Элемент</param>
        /// <param name="localQueue">Локальная очередь (если есть)</param>
        /// <returns>Удалось ли добавить</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryAdd(ThreadPoolWorkItem item, ThreadPoolLocalQueue localQueue)
        {
            return this.TryAdd(item, localQueue, false);
        }
        /// <summary>
        /// Добавить элемент в очередь
        /// </summary>
        /// <param name="item">Элемент</param>
        /// <param name="localQueue">Локальная очередь (если есть)</param>
        /// <param name="forceGlobal">Обязательное добавление в глобальную очередь</param>
        public void Add(ThreadPoolWorkItem item, ThreadPoolLocalQueue localQueue, bool forceGlobal)
        {
            Contract.Requires(item != null);
            Debug.Assert(!_isDisposed);

            try { }
            finally
            {
                if (forceGlobal || localQueue == null || !localQueue.TryAddLocal(item))
                {
                    bool addToMainRes = _globalQueue.TryAdd(item, -1);
                    Debug.Assert(addToMainRes);
                }
            }
        }
        /// <summary>
        /// Добавить элемент в очередь
        /// </summary>
        /// <param name="item">Элемент</param>
        /// <param name="localQueue">Локальная очередь (если есть)</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(ThreadPoolWorkItem item, ThreadPoolLocalQueue localQueue)
        {
            this.Add(item, localQueue, false);
        }





        // =====================


        /// <summary>
        /// Попробовать выполнить похищение элемента из соседних локальных очередей
        /// </summary>
        /// <param name="localQueue">Локальная очередь текущего потока</param>
        /// <param name="otherLocalQueues">Разреженный массив локальных очередей других потоков</param>
        /// <param name="item">Выбранный элемент</param>
        /// <returns>Удалось ли сделать выборку</returns>
        private bool TryTakeFromOtherLocalQueues(ThreadPoolLocalQueue localQueue, ThreadPoolLocalQueue[] otherLocalQueues, out ThreadPoolWorkItem item)
        {
            Contract.Requires(otherLocalQueues != null);
            Contract.Ensures(Contract.Result<bool>() == false || Contract.ValueAtReturn(out item) != null);


            bool result = false;
            item = null;

            try { }
            finally
            {
                int length = otherLocalQueues.Length;
                int index = Interlocked.Increment(ref _stealIndex) & int.MaxValue;
                for (int i = 0; i < length; i++, index++)
                {
                    var otherQueue = Volatile.Read(ref otherLocalQueues[index % length]);
                    if (otherQueue != null && otherQueue != localQueue && otherQueue.TrySteal(out item))
                    {
                        result = true;
                        break;
                    }
                }
            }

            return result;
        }



        /// <summary>
        /// Попытаться выбрать элемент
        /// </summary>
        /// <param name="localQueue">Локальная очередь (если есть)</param>
        /// <param name="doLocalSearch">Делать ли поиск в локальной очереди</param>
        /// <param name="doWorkSteal">Разрешено ли похищение работы</param>
        /// <param name="item">Выбранный элемент</param>
        /// <param name="timeout">Таймаут</param>
        /// <param name="token">Токен отмены</param>
        /// <param name="throwOnCancellation">Выбрасывать ли исключение при отмене по токену</param>
        /// <returns>Удалось ли выбрать</returns>
        public bool TryTake(ThreadPoolLocalQueue localQueue, bool doLocalSearch, bool doWorkSteal, out ThreadPoolWorkItem item, int timeout, CancellationToken token, bool throwOnCancellation)
        {
            Contract.Ensures(Contract.Result<bool>() == false || Contract.ValueAtReturn(out item) != null);
            Debug.Assert(!_isDisposed);

            // Пробуем выбрать из локальной очереди
            if (doLocalSearch && localQueue != null)
            {
                bool taken = false;
                try { }
                finally
                {
                    taken = localQueue.TryTakeLocal(out item);
                }
                if (taken)
                    return true;
            }

            // Если нельзя делать похищение, то сразу забираем из очереди
            if (!doWorkSteal)
                return _globalQueue.TryTake(out item, timeout, token, throwOnCancellation);

            // Пытаемся выбрать из общей очереди (если не удаётся, то нужно проверить возможность похищения)
            if (_globalQueue.TryTake(out item, 0, new CancellationToken(), false))
                return true;

            // Пытаемся похитить элемент
            if (TryTakeFromOtherLocalQueues(localQueue, _localQueues, out item))
                return true;

            if (timeout == 0)
            {
                // Попытки не дали результатов => выходим
                item = null;
                return false;
            }

            // Если не нужно просыпаться для похищения, то просто выбриаем из общей очереди
            if (_stealAwakePeriod <= 0)
                return _globalQueue.TryTake(out item, timeout, token, throwOnCancellation);


            if (timeout < 0)
            {
                // Если таймаут не ограничен, то выбираем из основной осереди и периодически пытаемся похитить
                while (true)
                {
                    if (token.IsCancellationRequested)
                        break;

                    if (_globalQueue.TryTake(out item, _stealAwakePeriod, token, throwOnCancellation))
                        return true;

                    if (token.IsCancellationRequested)
                        break;

                    if (TryTakeFromOtherLocalQueues(localQueue, _localQueues, out item))
                        return true;
                }
            }
            else
            {
                // Делаем замеры таймаута
                uint startTime = GetTimestamp();
                int restTime = timeout;

                while (restTime > 0)
                {
                    if (token.IsCancellationRequested)
                        break;

                    if (_globalQueue.TryTake(out item, Math.Min(_stealAwakePeriod, restTime), token, throwOnCancellation))
                        return true;

                    if (token.IsCancellationRequested)
                        break;

                    if (TryTakeFromOtherLocalQueues(localQueue, _localQueues, out item))
                        return true;

                    restTime = UpdateTimeout(startTime, timeout);
                }
            }

            if (token.IsCancellationRequested && throwOnCancellation)
                throw new OperationCanceledException(token);

            item = null;
            return false;
        }
        /// <summary>
        /// Попытаться выбрать элемент
        /// </summary>
        /// <param name="localQueue">Локальная очередь (если есть)</param>
        /// <param name="item">Выбранный элемент</param>
        /// <param name="timeout">Таймаут</param>
        /// <param name="token">Токен отмены</param>
        /// <param name="throwOnCancellation">Выбрасывать ли исключение при отмене по токену</param>
        /// <returns>Удалось ли выбрать</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryTake(ThreadPoolLocalQueue localQueue, out ThreadPoolWorkItem item, int timeout, CancellationToken token, bool throwOnCancellation)
        {
            return TryTake(localQueue, true, true, out item, timeout, token, throwOnCancellation);
        }
        /// <summary>
        /// Выбрать элемент из очереди
        /// </summary>
        /// <param name="localQueue">Локальная очередь потока</param>
        /// <param name="token">Токен отмены</param>
        /// <returns>Выбранный элемент</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ThreadPoolWorkItem Take(ThreadPoolLocalQueue localQueue, CancellationToken token)
        {
            ThreadPoolWorkItem item = null;
            bool result = TryTake(localQueue, true, true, out item, -1, token, true);
            Debug.Assert(result, "Something went wrong. Take not return any result.");
            return item;
        }
        /// <summary>
        /// Выбрать элемент из очереди
        /// </summary>
        /// <param name="localQueue">Локальная очередь потока</param>
        /// <returns>Выбранный элемент</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ThreadPoolWorkItem Take(ThreadPoolLocalQueue localQueue)
        {
            ThreadPoolWorkItem item = null;
            bool result = TryTake(localQueue, true, true, out item, -1, new CancellationToken(), true);
            Debug.Assert(result, "Something went wrong. Take not return any result.");
            return item;
        }

        /// <summary>
        /// Выбрать безопасно из общей очереди (работает и после вызова Dispose)
        /// </summary>
        /// <param name="item">Выбранный элемент</param>
        /// <returns>Успешность</returns>
        public bool TryTakeSafeFromGlobalQueue(out ThreadPoolWorkItem item)
        {
            return _globalQueue.TryTake(out item, 0, new CancellationToken(), true);
        }


        // ================


        /// <summary>
        /// Переместить все элементы из локальной очереди в общую
        /// </summary>
        /// <param name="localQueue">Локальная очередь</param>
        public void MoveItemsFromLocalQueueToGlobal(ThreadPoolLocalQueue localQueue)
        {
            Contract.Requires(localQueue != null);
            Debug.Assert(!_isDisposed);

            try { }
            finally
            {
                ThreadPoolWorkItem item = null;
                while (localQueue.TrySteal(out item))
                    _globalQueue.ForceAdd(item);
            }
        }


        /// <summary>
        /// Запросить временное расширение вместимости
        /// </summary>
        /// <param name="extensionVal">Величиная расширения</param>
        public void ExtendGlobalQueueCapacity(int extensionVal)
        {
            Contract.Requires(extensionVal >= 0);
            Debug.Assert(!_isDisposed);

            _globalQueue.RequestCapacityExtension(extensionVal);
        }

        // =======================


        /// <summary>
        /// Освобождение ресурсов
        /// </summary>
        /// <param name="isUserCall">Вызвано ли пользователем</param>
        private void Dispose(bool isUserCall)
        {
            if (!_isDisposed)
            {
                _isDisposed = true;

                if (isUserCall)
                {
                    Debug.Assert(Contract.ForAll(_localQueues, o => o == null), "LocalQueues is not empty on dispose of ThreadPoolQueueController");
                    if (_localQueues.Any(o => o != null))
                        throw new InvalidOperationException("LocalQueues is not empty on dispose of ThreadPoolQueueController");
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
    }
}
