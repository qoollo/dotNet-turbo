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
#pragma warning disable 0420

    /// <summary>
    /// Останавливает указанное число потоков
    /// </summary>
    [DebuggerDisplay("RealWaiterCount = {RealWaiterCount}, ExpectedWaiterCount = {ExpectedWaiterCount}")]
    internal class PartialThreadBlocker
    {
        private static readonly int _processorCount = Environment.ProcessorCount;

        private static readonly Action<object> _cancellationTokenCanceledEventHandler = new Action<object>(CancellationTokenCanceledEventHandler);
        /// <summary>
        /// Обработчик отмены токена
        /// </summary>
        /// <param name="obj">Объект PartialThreadBlocker</param>
        private static void CancellationTokenCanceledEventHandler(object obj)
        {
            PartialThreadBlocker blocker = obj as PartialThreadBlocker;
            TurboContract.Assert(blocker != null, conditionString: "blocker != null");
            lock (blocker._lockObj)
            {
                Monitor.PulseAll(blocker._lockObj);
            }
        }


        // ======================


        private volatile int _expectedWaiterCount;
        private volatile int _realWaiterCount;

        private readonly object _lockObj = new object();

        /// <summary>
        /// Конструктор PartialThreadBlocker
        /// </summary>
        /// <param name="expectedWaiterCount">Число потоков для блокировки</param>
        public PartialThreadBlocker(int expectedWaiterCount)
        {
            if (expectedWaiterCount < 0)
                throw new ArgumentOutOfRangeException(nameof(expectedWaiterCount), "expectedWaiterCount should be greater or equal to 0");

            _expectedWaiterCount = expectedWaiterCount;
            _realWaiterCount = 0;
        }
        /// <summary>
        /// Конструктор PartialThreadBlocker
        /// </summary>
        public PartialThreadBlocker()
            : this(0)
        {
        }
        
        /// <summary>
        /// Число потоков для блокировки
        /// </summary>
        public int ExpectedWaiterCount { get { return _expectedWaiterCount; } }
        /// <summary>
        /// Реальное число заблокированных потоков
        /// </summary>
        public int RealWaiterCount { get { return _realWaiterCount; } }


        /// <summary>
        /// Пробудить ожидающих
        /// </summary>
        /// <param name="diff">На сколько изменилось число потоков для блокировки</param>
        private void WakeUpWaiters(int diff)
        {
            if (diff >= 0)
                return;

            lock (_lockObj)
            {
                if (Math.Abs(diff) == 1)
                    Monitor.Pulse(_lockObj);
                else
                    Monitor.PulseAll(_lockObj);
            }
        }

        /// <summary>
        /// Задать число потоков для блокировки
        /// </summary>
        /// <param name="newValue">Новое значение</param>
        /// <returns>Предыдущее значение числа потоков для блокировки</returns>
        public int SetExpectedWaiterCount(int newValue)
        {
            TurboContract.Requires(newValue >= 0, conditionString: "newValue >= 0");

            int prevValue = Interlocked.Exchange(ref _expectedWaiterCount, newValue);
            WakeUpWaiters(newValue - prevValue);
            return prevValue;
        }
        /// <summary>
        /// Увеличить число потоков для блокировки
        /// </summary>
        /// <param name="addValue">Величина увеличения</param>
        /// <returns>Новое значение</returns>
        public int AddExpectedWaiterCount(int addValue)
        {
            SpinWait sw = new SpinWait();
            int expectedWaiterCount = _expectedWaiterCount;
            TurboContract.Assert(expectedWaiterCount + addValue >= 0, "Negative ExpectedWaiterCount. Can be commented");
            int newExpectedWaiterCount = Math.Max(0, expectedWaiterCount + addValue);
            while (Interlocked.CompareExchange(ref _expectedWaiterCount, newExpectedWaiterCount, expectedWaiterCount) != expectedWaiterCount)
            {
                sw.SpinOnce();
                expectedWaiterCount = _expectedWaiterCount;
                TurboContract.Assert(expectedWaiterCount + addValue >= 0, "Negative ExpectedWaiterCount. Can be commented");
                newExpectedWaiterCount = Math.Max(0, expectedWaiterCount + addValue);
            }

            WakeUpWaiters(newExpectedWaiterCount - expectedWaiterCount);

            return newExpectedWaiterCount;
        }
        /// <summary>
        /// Уменьшить число потоков для блокировки
        /// </summary>
        /// <param name="subValue">Величина уменьшения</param>
        /// <returns>Новое значение</returns>
        public int SubstractExpectedWaiterCount(int subValue)
        {
            return AddExpectedWaiterCount(-subValue);
        }




        /// <summary>
        /// Заблокироваться, если требуется
        /// </summary>
        /// <param name="timeout">Таймаут в миллисекундах</param>
        /// <param name="token">Токен отмены</param>
        /// <returns>Успешность (false - выход по таймауту)</returns>
        public bool Wait(int timeout, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (timeout < 0)
                timeout = Timeout.Infinite;

            if (_realWaiterCount >= _expectedWaiterCount)
                return true;

            if (timeout == 0)
                return false;

            uint startTime = 0;
            int currentTime = Timeout.Infinite;
            if (timeout != Timeout.Infinite)
                startTime = TimeoutHelper.GetTimestamp();

            for (int i = 0; i < 10; i++)
            {
                if (_realWaiterCount >= _expectedWaiterCount)
                    return true;

                if (i == 5)
                    Thread.Yield();
                else
                    Thread.SpinWait(150 + (4 << i));
            }


            using (CancellationTokenHelper.RegisterWithoutEC(token, _cancellationTokenCanceledEventHandler, this))
            {
                lock (_lockObj)
                {
                    if (_realWaiterCount < _expectedWaiterCount)
                    {
                        try
                        {
                            _realWaiterCount++;

                            while (_realWaiterCount <= _expectedWaiterCount)
                            {
                                token.ThrowIfCancellationRequested();

                                if (timeout != Timeout.Infinite)
                                {
                                    currentTime = TimeoutHelper.UpdateTimeout(startTime, timeout);
                                    if (currentTime <= 0)
                                        return false;
                                }

                                if (!Monitor.Wait(_lockObj, currentTime))
                                    return false;
                            }
                        }
                        finally
                        {
                            _realWaiterCount--;
                        }
                    }
                }
            }
            return true;
        }



        /// <summary>
        /// Заблокироваться, если требуется
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Wait()
        {
            bool semaphoreSlotTaken = Wait(Timeout.Infinite, new CancellationToken());
            TurboContract.Assert(semaphoreSlotTaken, conditionString: "semaphoreSlotTaken");
        }
        /// <summary>
        /// Заблокироваться, если требуется
        /// </summary>
        /// <param name="token">Токен отмены</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Wait(CancellationToken token)
        {
            bool semaphoreSlotTaken = Wait(Timeout.Infinite, token);
            TurboContract.Assert(semaphoreSlotTaken, conditionString: "semaphoreSlotTaken");
        }
        /// <summary>
        /// Заблокироваться, если требуется
        /// </summary>
        /// <param name="timeout">Таймаут</param>
        /// <returns>Успешность (false - выход по таймауту)</returns>
        public bool Wait(TimeSpan timeout)
        {
            long timeoutMs = (long)timeout.TotalMilliseconds;
            if (timeoutMs > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(timeout));

            return Wait((int)timeoutMs, new CancellationToken());
        }
        /// <summary>
        /// Заблокироваться, если требуется
        /// </summary>
        /// <param name="timeout">Таймаут</param>
        /// <param name="token">Токен отмены</param>
        /// <returns>Успешность (false - выход по таймауту)</returns>
        public bool Wait(TimeSpan timeout, CancellationToken token)
        {
            long timeoutMs = (long)timeout.TotalMilliseconds;
            if (timeoutMs > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(timeout));

            return Wait((int)timeoutMs, token);
        }
        /// <summary>
        /// Заблокироваться, если требуется
        /// </summary>
        /// <param name="timeout">Таймаут</param>
        /// <returns>Успешность (false - выход по таймауту)</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Wait(int timeout)
        {
            return Wait(timeout, new CancellationToken());
        }
    }

#pragma warning restore 0420
}
