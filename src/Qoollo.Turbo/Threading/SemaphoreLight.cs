using Qoollo.Turbo.Threading.ServiceStuff;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Threading
{
#pragma warning disable 0420

    /// <summary>
    /// Легковесный семафор
    /// </summary>
    [DebuggerDisplay("CurrentCount = {CurrentCount}")]
    public class SemaphoreLight: IDisposable
    {
        private static readonly int _processorCount = Environment.ProcessorCount;

        private static readonly Action<object> _cancellationTokenCanceledEventHandler = new Action<object>(CancellationTokenCanceledEventHandler);
        /// <summary>
        /// Обработчик отмены токена
        /// </summary>
        /// <param name="obj">Объект SemaphoreSlimE</param>
        private static void CancellationTokenCanceledEventHandler(object obj)
        {
            SemaphoreLight semaphore = obj as SemaphoreLight;
            Contract.Assert(semaphore != null);
            lock (semaphore._lockObj)
            {
                Monitor.PulseAll(semaphore._lockObj);
            }
        }


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

        // =============



        private volatile int _currentCountLocFree;
        private volatile int _currentCountForWait;
        private readonly int _maxCount;

        private volatile int _waitCount;

        private volatile bool _isDisposed;

        private object _lockObj;




        /// <summary>
        /// Конструктор SemaphoreLight
        /// </summary>
        /// <param name="initialCount">Начальное значение в семаформе</param>
        /// <param name="maxCount">Максимальное значение в семаформе</param>
        public SemaphoreLight(int initialCount, int maxCount)
        {
            if (initialCount < 0 || initialCount > maxCount)
                throw new ArgumentOutOfRangeException("initialCount", "initialCount should be in range [0, maxCount]");
            if (maxCount <= 0)
                throw new ArgumentOutOfRangeException("maxCount", "maxCount should be positive");

            _maxCount = maxCount;
            _lockObj = new object();
            _currentCountLocFree = initialCount;
            _currentCountForWait = 0;
            _isDisposed = false;
        }
        /// <summary>
        /// Конструктор SemaphoreLight
        /// </summary>
        /// <param name="initialCount">Начальное значение в семаформе</param>
        public SemaphoreLight(int initialCount)
            : this(initialCount, int.MaxValue)
        {
        }

        /// <summary>
        /// Число доступных слотов
        /// </summary>
        public int CurrentCount
        {
            get { return _currentCountLocFree + _currentCountForWait; }
        }
        /// <summary>
        /// Число ожидающих потоков
        /// </summary>
        public int WaiterCount
        {
            get { return _waitCount; }
        }


        /// <summary>
        /// Атомарно забрать слот из _currentCountLocFree, если там есть
        /// </summary>
        /// <returns>Успешность</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryTakeLockFree()
        {
            SpinWait spin = new SpinWait();
            int currentCountLocFree = _currentCountLocFree;
            while (currentCountLocFree > 0)
            {
                if (Interlocked.CompareExchange(ref _currentCountLocFree, currentCountLocFree - 1, currentCountLocFree) == currentCountLocFree)
                    return true;

                spin.SpinOnce();
                currentCountLocFree = _currentCountLocFree;
            }

            return false;
        }


        /// <summary>
        /// Выполнение ожидания и забор элемента (должен вызываться внутри lock на _lockObj)
        /// </summary>
        /// <param name="timeout">Таймаут</param>
        /// <param name="startTime">Начальное время (для отслеживания таймаута)</param>
        /// <param name="token">Токен отмены</param>
        /// <returns>Удалось ли дождаться (false - таймаут или отмена или Dispose)</returns>
        private bool WaitUntilCountOrTimeoutAndTake(int timeout, uint startTime, CancellationToken token)
        {
            int remainingWaitMilliseconds = Timeout.Infinite;

            while (true)
            {
                if (token.IsCancellationRequested || _isDisposed)
                    return false;

                if (timeout != Timeout.Infinite)
                {
                    remainingWaitMilliseconds = UpdateTimeout(startTime, timeout);
                    if (remainingWaitMilliseconds <= 0)
                        return false;
                }

                if (!Monitor.Wait(_lockObj, remainingWaitMilliseconds))
                    return false;


                if (_currentCountForWait > 0)
                {
                    _currentCountForWait--;
                    return true;
                }

                // Проверяем и lock-free
                if (TryTakeLockFree())
                    return true;
            }
        }



        /// <summary>
        /// Выполнить ожидание появления 1-ого слота в семаформе. 
        /// При успешном захвате число доступных слотов уменьшается на 1
        /// </summary>
        /// <param name="timeout">Таймаут</param>
        /// <param name="token">Токен отмены</param>
        /// <returns>Удалось ли захватить слот</returns>
        public bool Wait(int timeout, CancellationToken token)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(this.GetType().Name);

            if (timeout < -1)
                timeout = Timeout.Infinite;

            token.ThrowIfCancellationRequested();

            uint startTime = 0;
            if (timeout != Timeout.Infinite && timeout > 0)
                startTime = GetTimestamp();

            // Делаем захват
            if (TryTakeLockFree())
                return true;


            // Ждём появления (лучше активно подождать, чем входить в lock)
            if (_processorCount > 1)
            {
                int currentCountLocFree = _currentCountLocFree;
                if (_waitCount >= _currentCountForWait && _waitCount <= _currentCountForWait + 2)
                {
                    for (int i = 0; i < 9; i++)
                    {
                        if (currentCountLocFree > 0 && Interlocked.CompareExchange(ref _currentCountLocFree, currentCountLocFree - 1, currentCountLocFree) == currentCountLocFree)
                            return true;

                        Thread.SpinWait(150 + 16 * i);
                        currentCountLocFree = _currentCountLocFree;
                    }
                }

                // Пробуем захватить ещё раз
                if (currentCountLocFree > 0 && Interlocked.CompareExchange(ref _currentCountLocFree, currentCountLocFree - 1, currentCountLocFree) == currentCountLocFree)
                    return true;
            }


            if (timeout == 0 && _waitCount >= _currentCountForWait)
                return false;

            if (_waitCount >= _currentCountForWait)
            {
                Thread.Yield();

                int currentCountLocFree = _currentCountLocFree;
                if (currentCountLocFree > 0 && Interlocked.CompareExchange(ref _currentCountLocFree, currentCountLocFree - 1, currentCountLocFree) == currentCountLocFree)
                    return true;
            }

            // Вынуждены уходить в lock
            CancellationTokenRegistration cancellationTokenRegistration = default(CancellationTokenRegistration);
            bool lockTaken = false;
            try
            {
                if (token.CanBeCanceled)
                    cancellationTokenRegistration = CancellationTokenHelper.RegisterWithoutEC(token, _cancellationTokenCanceledEventHandler, this);

                try { }
                finally
                {
                    Monitor.Enter(_lockObj, ref lockTaken);
                    Contract.Assert(lockTaken);
                    Interlocked.Increment(ref _waitCount); // Release должен увидеть наше появление
                }

                // Пробуем забрать из _currentCountForWait
                if (_currentCountForWait > 0)
                {
                    _currentCountForWait--;
                    return true;
                }

                // Пока входили в lock могли добавится значения в _currentCountLocFree
                if (TryTakeLockFree())
                    return true;

                if (timeout == 0)
                    return false;

                // Ожидаем появления элементов и забираем сразу
                if (WaitUntilCountOrTimeoutAndTake(timeout, startTime, token))
                    return true;

                if (token.IsCancellationRequested)
                    throw new OperationCanceledException(token);

                if (_isDisposed)
                    throw new OperationInterruptedException("Semaphore wait was interrupted by Dispose", new ObjectDisposedException(this.GetType().Name));
            }
            finally
            {
                if (lockTaken)
                {
                    _waitCount--;
                    Contract.Assert(_waitCount >= 0);
                    Monitor.Exit(_lockObj);
                }

                cancellationTokenRegistration.Dispose();
            }

            return false;
        }




        /// <summary>
        /// Выполнить ожидание появления 1-ого слота в семаформе. 
        /// При успешном захвате число доступных слотов уменьшается на 1
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Wait()
        {
            bool semaphoreSlotTaken = Wait(Timeout.Infinite, new CancellationToken());
            Contract.Assert(semaphoreSlotTaken);
        }
        /// <summary>
        /// Выполнить ожидание появления 1-ого слота в семаформе. 
        /// При успешном захвате число доступных слотов уменьшается на 1
        /// </summary>
        /// <param name="token">Токен отмены</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Wait(CancellationToken token)
        {
            bool semaphoreSlotTaken = Wait(Timeout.Infinite, token);
            Contract.Assert(semaphoreSlotTaken);
        }
        /// <summary>
        /// Выполнить ожидание появления 1-ого слота в семаформе. 
        /// При успешном захвате число доступных слотов уменьшается на 1
        /// </summary>
        /// <param name="timeout">Таймаут</param>
        /// <returns>Удалось ли захватить слот</returns>
        public bool Wait(TimeSpan timeout)
        {
            long timeoutMs = (long)timeout.TotalMilliseconds;
            if (timeoutMs > int.MaxValue)
                throw new ArgumentOutOfRangeException("timeout");

            return Wait((int)timeoutMs, new CancellationToken());
        }
        /// <summary>
        /// Выполнить ожидание появления 1-ого слота в семаформе. 
        /// При успешном захвате число доступных слотов уменьшается на 1
        /// </summary>
        /// <param name="timeout">Таймаут</param>
        /// <param name="token">Токен отмены</param>
        /// <returns>Удалось ли захватить слот</returns>
        public bool Wait(TimeSpan timeout, CancellationToken token)
        {
            long timeoutMs = (long)timeout.TotalMilliseconds;
            if (timeoutMs > int.MaxValue)
                throw new ArgumentOutOfRangeException("timeout");

            return Wait((int)timeoutMs, token);
        }
        /// <summary>
        /// Выполнить ожидание появления 1-ого слота в семаформе. 
        /// При успешном захвате число доступных слотов уменьшается на 1
        /// </summary>
        /// <param name="timeout">Таймаут</param>
        /// <returns>Удалось ли захватить слот</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Wait(int timeout)
        {
            return Wait(timeout, new CancellationToken());
        }



        /// <summary>
        /// Атомарно считать diff между числом Waiter'ов и числом доступных слотов для них
        /// </summary>
        /// <returns>Разница</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetWaiterAndWaitCountDiffAtomic()
        {
            int waitCount = 0;
            int currentCountForWait = 0;
            do
            {
                waitCount = _waitCount;
                currentCountForWait = _currentCountForWait;
            }
            while (waitCount != _waitCount || currentCountForWait != _currentCountForWait);

            return Math.Max(0, waitCount - currentCountForWait);
        }

        /// <summary>
        /// Освободить N слотов
        /// </summary>
        /// <param name="releaseCount">Число слотов для освобождения</param>
        public void Release(int releaseCount)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(this.GetType().Name);
            if (releaseCount < 1)
                throw new ArgumentOutOfRangeException("releaseCount", "releaseCount should be positive");
            if (_maxCount - CurrentCount < releaseCount)
                throw new SemaphoreFullException();

            int waiterAndWaitCountDiff = 0;
            int releaseCountForWait = 0;
            int releaseCountLocFree = releaseCount;

            if (_waitCount > 0)
            {
                waiterAndWaitCountDiff = GetWaiterAndWaitCountDiffAtomic();
                releaseCountForWait = Math.Min(releaseCount, waiterAndWaitCountDiff); // Приоритет waiter'ам
                releaseCountLocFree = releaseCount - releaseCountForWait;
            }

            Contract.Assert(releaseCountForWait >= 0);
            Contract.Assert(releaseCountLocFree >= 0);
            Contract.Assert(releaseCountForWait + releaseCountLocFree == releaseCount);

            // Сначала возврат в lockFree
            if (releaseCountLocFree > 0)
            {
                int currentCountLocFree = Interlocked.Add(ref _currentCountLocFree, releaseCountLocFree);
                Contract.Assert(currentCountLocFree > 0);
            }

            // Теперь возврат для waiter'ов. Если число waiter'ов увеличилось, то тоже нужно зайти в lock
            if (releaseCountForWait > 0 || (_waitCount > 0 && GetWaiterAndWaitCountDiffAtomic() > waiterAndWaitCountDiff))
            {
                lock (_lockObj)
                {
                    int waitCount = _waitCount;
                    int currentCountForWait = _currentCountForWait;
                    int nextCurrentCountForWait = currentCountForWait + releaseCountForWait;

                    // В идеале _waitCount == _currentCountForWait. Если нет, то предпринимаем действия
                    if (nextCurrentCountForWait > waitCount && releaseCountForWait > 0)
                    {
                        // Если слотов оказывается больше, то избыток возвращаем в _currentCountLocFree
                        int countForReturnToLockFree = Math.Min(releaseCountForWait, nextCurrentCountForWait - waitCount);
                        int currentCountLocFree = Interlocked.Add(ref _currentCountLocFree, countForReturnToLockFree);
                        Contract.Assert(currentCountLocFree > 0);
                        releaseCountForWait -= countForReturnToLockFree;
                        releaseCountLocFree += countForReturnToLockFree;
                    }
                    else if (nextCurrentCountForWait < waitCount)
                    {
                        // Если меньше, то пытаемся захватить себе обратно

                        // Не можем забрать больше, чем было добавлено этим вызовом Release
                        int maxToRequestFromLockFree = Math.Min(releaseCountLocFree, waitCount - nextCurrentCountForWait);

                        if (maxToRequestFromLockFree > 0)
                        {
                            SpinWait spin = new SpinWait();
                            int currentCountLocFree = _currentCountLocFree;
                            int countToRequestFromLockFree = Math.Min(currentCountLocFree, maxToRequestFromLockFree);
                            while (countToRequestFromLockFree > 0)
                            {
                                Contract.Assert(currentCountLocFree - countToRequestFromLockFree >= 0);
                                if (Interlocked.CompareExchange(ref _currentCountLocFree, currentCountLocFree - countToRequestFromLockFree, currentCountLocFree) == currentCountLocFree)
                                {
                                    releaseCountForWait += countToRequestFromLockFree;
                                    releaseCountLocFree -= countToRequestFromLockFree;
                                    break;
                                }

                                spin.SpinOnce();
                                currentCountLocFree = _currentCountLocFree;
                                countToRequestFromLockFree = Math.Min(currentCountLocFree, maxToRequestFromLockFree);
                            }
                        }
                    }

                    Contract.Assert(releaseCountForWait >= 0);
                    Contract.Assert(releaseCountLocFree >= 0);
                    Contract.Assert(releaseCountForWait + releaseCountLocFree == releaseCount);

                    if (releaseCountForWait > 0)
                    {
                        Contract.Assert(_currentCountForWait == currentCountForWait);

                        currentCountForWait += releaseCountForWait;
                        Contract.Assert(currentCountForWait > 0);

                        if (currentCountForWait == 1 || waitCount == 1)
                            Monitor.Pulse(_lockObj);
                        else
                            Monitor.PulseAll(_lockObj);

                        _currentCountForWait = currentCountForWait;
                    }
                }
            }
        }
        /// <summary>
        /// Освободить 1 слот
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Release()
        {
            Release(1);
        }


        /// <summary>
        /// Освобождение ресурсов
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;

            lock (_lockObj)
            {
                if (_isDisposed)
                    return;

                _isDisposed = true;

                Monitor.PulseAll(this._lockObj);
            }
        }
    }

#pragma warning restore 0420
}
