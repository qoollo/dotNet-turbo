using Qoollo.Turbo.Threading.ServiceStuff;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Threading
{
    /// <summary>
    /// Lightweigh semaphore based on Interlocked operations
    /// </summary>
    [DebuggerDisplay("CurrentCount = {CurrentCount}")]
    public sealed class SemaphoreLight: IDisposable
    {
        private static readonly int _processorCount = Environment.ProcessorCount;

        private static readonly Action<object> _cancellationTokenCanceledEventHandler = new Action<object>(CancellationTokenCanceledEventHandler);
        /// <summary>
        /// Cancellation handler for CancellationToken
        /// </summary>
        /// <param name="obj">SemaphoreLight object</param>
        private static void CancellationTokenCanceledEventHandler(object obj)
        {
            SemaphoreLight semaphore = obj as SemaphoreLight;
            TurboContract.Assert(semaphore != null, conditionString: "semaphore != null");
            lock (semaphore._lockObj)
            {
                Monitor.PulseAll(semaphore._lockObj);
            }
        }


        // =============


        private volatile int _currentCountLockFree;
        private volatile int _currentCountForWait;
        private readonly int _maxCount;

        private volatile int _waitCount;

        private volatile bool _isDisposed;

        private object _lockObj;




        /// <summary>
        /// SemaphoreLight constructor
        /// </summary>
        /// <param name="initialCount">Initial number of request that can be granted concurrently</param>
        /// <param name="maxCount">Maximal count of requests that can be granted concurrently</param>
        public SemaphoreLight(int initialCount, int maxCount)
        {
            if (initialCount < 0 || initialCount > maxCount)
                throw new ArgumentOutOfRangeException(nameof(initialCount), "initialCount should be in range [0, maxCount]");
            if (maxCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxCount), "maxCount should be positive");

            _maxCount = maxCount;
            _lockObj = new object();
            _currentCountLockFree = initialCount;
            _currentCountForWait = 0;
            _isDisposed = false;
        }
        /// <summary>
        /// SemaphoreLight constructor
        /// </summary>
        /// <param name="initialCount">Initial number of request that can be granted concurrently</param>
        public SemaphoreLight(int initialCount)
            : this(initialCount, int.MaxValue)
        {
        }

        /// <summary>
        /// The number of threads that will be allowed to enter the Semaphore
        /// </summary>
        public int CurrentCount
        {
            get { return _currentCountLockFree + _currentCountForWait; }
        }
        /// <summary>
        /// The number of waiting threads on the Semaphore
        /// </summary>
        public int WaiterCount
        {
            get { return _waitCount; }
        }


        /// <summary>
        /// Atomically take the value from _currentCountLocFree if it's possible
        /// </summary>
        /// <returns>Is the slot was taken</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryTakeLockFree()
        {
            SpinWait spin = new SpinWait();
            int currentCountLocFree = _currentCountLockFree;
            while (currentCountLocFree > 0)
            {
                if (Interlocked.CompareExchange(ref _currentCountLockFree, currentCountLocFree - 1, currentCountLocFree) == currentCountLocFree)
                    return true;

                spin.SpinOnce();
                currentCountLocFree = _currentCountLockFree;
            }

            return false;
        }


        /// <summary>
        /// Waits until count will be available or timeout happend and take the slot from '_currentCountForWait' 
        /// (should be called inside 'lock' statement on '_lockObj')
        /// </summary>
        /// <param name="timeout">Timeout</param>
        /// <param name="startTime">Starting time of the procedure to track the timeout</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>True if the waiting was successfull (false - timeout or cancellation or Dispose happened)</returns>
        private bool WaitUntilCountOrTimeoutAndTake(int timeout, uint startTime, CancellationToken token)
        {
            int remainingWaitMilliseconds = Timeout.Infinite;

            while (true)
            {
                if (token.IsCancellationRequested || _isDisposed)
                    return false;

                if (timeout != Timeout.Infinite)
                {
                    remainingWaitMilliseconds = TimeoutHelper.UpdateTimeout(startTime, timeout);
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
        /// Blocks the current thread until it can enter the semaphore
        /// </summary>
        /// <param name="timeout">Tiemout in milliseconds</param>
        /// <param name="token">Cancellation token</param>
        /// <param name="throwOnCancellation">Whether the OperationCanceledException should be thrown if cancellation happened</param>
        /// <returns>True if the current thread successfully entered the semaphore</returns>
        internal bool Wait(int timeout, CancellationToken token, bool throwOnCancellation)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(this.GetType().Name);

            if (token.IsCancellationRequested)
            {
                if (throwOnCancellation)
                    throw new OperationCanceledException(token);

                return false;
            }

            // Делаем захват
            if (TryTakeLockFree())
                return true;

            // Early exit: nothing to wait
            if (timeout == 0 && _waitCount >= _currentCountForWait)
                return false;

            uint startTime = 0;
            if (timeout > 0)
                startTime = TimeoutHelper.GetTimestamp();
            else if (timeout < -1)
                timeout = Timeout.Infinite;


            // Ждём появления (лучше активно подождать, чем входить в lock)
            if (_processorCount > 1)
            {
                int currentCountLocFree = _currentCountLockFree;
                if (_waitCount >= _currentCountForWait && _waitCount <= _currentCountForWait + 2)
                {
                    for (int i = 0; i < 8; i++)
                    {
                        if (currentCountLocFree > 0 && Interlocked.CompareExchange(ref _currentCountLockFree, currentCountLocFree - 1, currentCountLocFree) == currentCountLocFree)
                            return true;

                        SpinWaitHelper.SpinWait(10 + 2 * i);
                        currentCountLocFree = _currentCountLockFree;
                    }
                }

                // Пробуем захватить ещё раз
                if (currentCountLocFree > 0 && Interlocked.CompareExchange(ref _currentCountLockFree, currentCountLocFree - 1, currentCountLocFree) == currentCountLocFree)
                    return true;
            }


            if (_waitCount >= _currentCountForWait)
            {
                if (timeout == 0) // Редкая ситуация. При нулевом таймауте нам нечего ловить
                    return false;

                int currentCountLocFree;
                for (int i = 0; i < 3; i++)
                {
                    if ((i % 2) == 1 && _processorCount > 1)
                        SpinWaitHelper.SpinWait(5);
                    else
                        Thread.Yield();

                    currentCountLocFree = _currentCountLockFree;
                    if (currentCountLocFree > 0 && Interlocked.CompareExchange(ref _currentCountLockFree, currentCountLocFree - 1, currentCountLocFree) == currentCountLocFree)
                        return true;
                }
            }

            // Вынуждены уходить в lock
            CancellationTokenRegistration cancellationTokenRegistration = default(CancellationTokenRegistration);
            bool lockTaken = false;
            try
            {
                if (token.CanBeCanceled)
                    cancellationTokenRegistration = CancellationTokenHelper.RegisterWithoutECIfPossible(token, _cancellationTokenCanceledEventHandler, this);

                try { }
                finally
                {
                    Monitor.Enter(_lockObj, ref lockTaken);
                    TurboContract.Assert(lockTaken, conditionString: "lockTaken");
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
                {
                    if (throwOnCancellation)
                        throw new OperationCanceledException(token);

                    return false;
                }

                if (_isDisposed)
                    throw new OperationInterruptedException("Semaphore wait was interrupted by Dispose", new ObjectDisposedException(this.GetType().Name));
            }
            finally
            {
                if (lockTaken)
                {
                    _waitCount--;
                    TurboContract.Assert(_waitCount >= 0, conditionString: "_waitCount >= 0");
                    Monitor.Exit(_lockObj);
                }

                cancellationTokenRegistration.Dispose();
            }

            return false;
        }


        /// <summary>
        /// Blocks the current thread until it can enter the semaphore
        /// </summary>
        /// <param name="timeout">Tiemout in milliseconds</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>True if the current thread successfully entered the semaphore</returns>
        /// <exception cref="ObjectDisposedException">Semaphore was disposed</exception>
        /// <exception cref="OperationCanceledException">Cancellation happened</exception>
        /// <exception cref="OperationInterruptedException">Waiting was interrupted by Dispose</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Wait(int timeout, CancellationToken token)
        {
            return Wait(timeout, token, true);
        }

        /// <summary>
        /// Blocks the current thread until it can enter the semaphore
        /// </summary>
        /// <exception cref="ObjectDisposedException">Semaphore was disposed</exception>
        /// <exception cref="OperationInterruptedException">Waiting was interrupted by Dispose</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Wait()
        {
            bool semaphoreSlotTaken = Wait(Timeout.Infinite, new CancellationToken(), true);
            TurboContract.Assert(semaphoreSlotTaken, "semaphoreSlotTaken is false when timeout is infinite");
        }
        /// <summary>
        /// Blocks the current thread until it can enter the semaphore
        /// </summary>
        /// <param name="token">Cancellation token</param>
        /// <exception cref="ObjectDisposedException">Semaphore was disposed</exception>
        /// <exception cref="OperationCanceledException">Cancellation happened</exception>
        /// <exception cref="OperationInterruptedException">Waiting was interrupted by Dispose</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Wait(CancellationToken token)
        {
            bool semaphoreSlotTaken = Wait(Timeout.Infinite, token, true);
            TurboContract.Assert(semaphoreSlotTaken, "semaphoreSlotTaken is false when timeout is infinite");
        }
        /// <summary>
        /// Blocks the current thread until it can enter the semaphore
        /// </summary>
        /// <param name="timeout">Tiemout value</param>
        /// <returns>True if the current thread successfully entered the semaphore</returns>
        /// <exception cref="ObjectDisposedException">Semaphore was disposed</exception>
        /// <exception cref="OperationInterruptedException">Waiting was interrupted by Dispose</exception>
        public bool Wait(TimeSpan timeout)
        {
            long timeoutMs = (long)timeout.TotalMilliseconds;
            if (timeoutMs > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(timeout));

            return Wait((int)timeoutMs, new CancellationToken(), true);
        }
        /// <summary>
        /// Blocks the current thread until it can enter the semaphore
        /// </summary>
        /// <param name="timeout">Tiemout value</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>True if the current thread successfully entered the semaphore</returns>
        /// <exception cref="ObjectDisposedException">Semaphore was disposed</exception>
        /// <exception cref="OperationCanceledException">Cancellation happened</exception>
        /// <exception cref="OperationInterruptedException">Waiting was interrupted by Dispose</exception>
        public bool Wait(TimeSpan timeout, CancellationToken token)
        {
            long timeoutMs = (long)timeout.TotalMilliseconds;
            if (timeoutMs > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(timeout));

            return Wait((int)timeoutMs, token, true);
        }
        /// <summary>
        /// Blocks the current thread until it can enter the semaphore
        /// </summary>
        /// <param name="timeout">Tiemout in milliseconds</param>
        /// <returns>True if the current thread successfully entered the semaphore</returns>
        /// <exception cref="ObjectDisposedException">Semaphore was disposed</exception>
        /// <exception cref="OperationInterruptedException">Waiting was interrupted by Dispose</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Wait(int timeout)
        {
            return Wait(timeout, new CancellationToken(), true);
        }



        /// <summary>
        /// Atomically calculates the difference between number of waiting threads and number of available slots
        /// </summary>
        /// <returns>Difference</returns>
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
        /// Exists the semaphore the specified number of times
        /// </summary>
        /// <param name="releaseCount">The number of times to exit the semaphore</param>
        public void Release(int releaseCount)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(this.GetType().Name);
            if (releaseCount < 1)
                throw new ArgumentOutOfRangeException(nameof(releaseCount), "releaseCount should be positive");
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

            TurboContract.Assert(releaseCountForWait >= 0, conditionString: "releaseCountForWait >= 0");
            TurboContract.Assert(releaseCountLocFree >= 0, conditionString: "releaseCountLocFree >= 0");
            TurboContract.Assert(releaseCountForWait + releaseCountLocFree == releaseCount, conditionString: "releaseCountForWait + releaseCountLocFree == releaseCount");

            // Сначала возврат в lockFree
            if (releaseCountLocFree > 0)
            {
                int currentCountLocFree = Interlocked.Add(ref _currentCountLockFree, releaseCountLocFree);
                TurboContract.Assert(currentCountLocFree > 0, conditionString: "currentCountLocFree > 0");
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
                        int currentCountLocFree = Interlocked.Add(ref _currentCountLockFree, countForReturnToLockFree);
                        TurboContract.Assert(currentCountLocFree > 0, conditionString: "currentCountLocFree > 0");
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
                            int currentCountLocFree = _currentCountLockFree;
                            int countToRequestFromLockFree = Math.Min(currentCountLocFree, maxToRequestFromLockFree);
                            while (countToRequestFromLockFree > 0)
                            {
                                TurboContract.Assert(currentCountLocFree - countToRequestFromLockFree >= 0, conditionString: "currentCountLocFree - countToRequestFromLockFree >= 0");
                                if (Interlocked.CompareExchange(ref _currentCountLockFree, currentCountLocFree - countToRequestFromLockFree, currentCountLocFree) == currentCountLocFree)
                                {
                                    releaseCountForWait += countToRequestFromLockFree;
                                    releaseCountLocFree -= countToRequestFromLockFree;
                                    break;
                                }

                                spin.SpinOnce();
                                currentCountLocFree = _currentCountLockFree;
                                countToRequestFromLockFree = Math.Min(currentCountLocFree, maxToRequestFromLockFree);
                            }
                        }
                    }

                    TurboContract.Assert(releaseCountForWait >= 0, conditionString: "releaseCountForWait >= 0");
                    TurboContract.Assert(releaseCountLocFree >= 0, conditionString: "releaseCountLocFree >= 0");
                    TurboContract.Assert(releaseCountForWait + releaseCountLocFree == releaseCount, conditionString: "releaseCountForWait + releaseCountLocFree == releaseCount");

                    if (releaseCountForWait > 0)
                    {
                        TurboContract.Assert(_currentCountForWait == currentCountForWait, conditionString: "_currentCountForWait == currentCountForWait");

                        currentCountForWait += releaseCountForWait;
                        TurboContract.Assert(currentCountForWait > 0, conditionString: "currentCountForWait > 0");

                        int waitersToNotify = Math.Min(currentCountForWait, waitCount);
                        for (int i = 0; i < waitersToNotify; i++)
                            Monitor.Pulse(_lockObj);

                        _currentCountForWait = currentCountForWait;
                    }
                }
            }
        }
        /// <summary>
        /// Exists the semaphore once
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Release()
        {
            Release(1);
        }


        /// <summary>
        /// Cleans-up all resources
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
}
