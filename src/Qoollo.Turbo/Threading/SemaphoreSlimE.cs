using Qoollo.Turbo.Threading.ServiceStuff;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Threading
{
    /// <summary>
    /// Быстрый семаформ (в нагруженных сценариях работает эффективнее стандартного SemaphoreSlim)
    /// </summary>
    [DebuggerDisplay("Current Count = {_currentCount}")]
    internal class SemaphoreSlimE : IDisposable
    {
        private static readonly int _processorCount = Environment.ProcessorCount;

        private static readonly Action<object> _cancellationTokenCanceledEventHandler = new Action<object>(CancellationTokenCanceledEventHandler);
        /// <summary>
        /// Обработчик отмены токена
        /// </summary>
        /// <param name="obj">Объект SemaphoreSlimE</param>
        private static void CancellationTokenCanceledEventHandler(object obj)
        {
            SemaphoreSlimE semaphore = obj as SemaphoreSlimE;
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

        

        // Источник номеров для потоков ожидающих захвата Wait
        private int _entringWaitersNumber;
        // Источник номеров для вышедших из Wait потоков (необходим для оценки числа потоков, готовящихся захватить lock)
        private volatile int _finishedWaitersNumber;


        // The semaphore count, initialized in the constructor to the initial value, every release call incremetns it
        // and every wait call decrements it as long as its value is positive otherwise the wait will block.
        // Its value must be between the maximum semaphore value and zero
        private volatile int _currentCount;
 
        // The maximum semaphore value, it is initialized to Int.MaxValue if the client didn't specify it. it is used 
        // to check if the count excceeded the maxi value or not.
        private readonly int _maxCount;
 
        // The number of synchronously waiting threads, it is set to zero in the constructor and increments before blocking the
        // threading and decrements it back after that. It is used as flag for the release call to know if there are
        // waiting threads in the monitor or not.
        private volatile int _waitCount;
 
        // Dummy object used to in lock statements to protect the semaphore count, wait handle and cancelation
        private object _lockObj;
 
        // Act as the semaphore wait handle, it's lazily initialized if needed, the first WaitHandle call initialize it
        // and wait an release sets and resets it respectively as long as it is not null
        private volatile ManualResetEvent _waitHandle;
 


        /// <summary>
        /// Конструктор SemaphoreSlimE
        /// </summary>
        /// <param name="initialCount">Начальное значение в семаформе</param>
        /// <param name="maxCount">Максимальное значение в семаформе</param>
        public SemaphoreSlimE(int initialCount, int maxCount)
        {
            if (initialCount < 0 || initialCount > maxCount)
                throw new ArgumentOutOfRangeException("initialCount", "initialCount should be in range [0, maxCount]");
            if (maxCount <= 0)
                throw new ArgumentOutOfRangeException("maxCount", "maxCount should be positive");
 
            _maxCount = maxCount;
            _lockObj = new object();
            _currentCount = initialCount;
        }
        /// <summary>
        /// Конструктор SemaphoreSlimE
        /// </summary>
        /// <param name="initialCount">Начальное значение в семаформе</param>
        public SemaphoreSlimE(int initialCount)
            : this(initialCount, int.MaxValue)
        {
        }

        /// <summary>
        /// Число доступных слотов
        /// </summary>
        public int CurrentCount
        {
            get { return _currentCount; }
        }

        /// <summary>
        /// WaitHandle
        /// </summary>
        public WaitHandle AvailableWaitHandle
        {
            get
            {
                CheckDispose();
 
                if (_waitHandle == null)
                {
                    lock (_lockObj)
                    {
                        if (_waitHandle == null)
                            _waitHandle = new ManualResetEvent(_currentCount != 0);
                    }
                }
                return _waitHandle;
            }
        }


        /// <summary>
        /// Проверка, не был ли объект освобождён
        /// </summary>
        private void CheckDispose()
        {
            if (_lockObj == null)
                throw new ObjectDisposedException("SemaphoreSlimE");
        }



         
        /// <summary>
        /// Выполнение ожидания (должен вызываться внутри lock на _lockObj)
        /// </summary>
        /// <param name="timeout">Таймаут</param>
        /// <param name="startTime">Начальное время (для отслеживания таймаута)</param>
        /// <param name="token">Токен отмены</param>
        /// <returns>Удалось ли дождаться (false - таймаут или отмена)</returns>
        private bool WaitUntilCountOrTimeout(int timeout, uint startTime, CancellationToken token)
        {
            int remainingWaitMilliseconds = Timeout.Infinite;
 
            while (_currentCount == 0)
            {
                if (token.IsCancellationRequested)
                    return false;

                if (timeout != Timeout.Infinite)
                {
                    remainingWaitMilliseconds = UpdateTimeout(startTime, timeout);
                    if (remainingWaitMilliseconds <= 0)
                        return false;
                }

                if (!Monitor.Wait(_lockObj, remainingWaitMilliseconds))
                    return false;
            }
 
            return true; 
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
            CheckDispose();

            if (timeout < -1)
                timeout = Timeout.Infinite;

            token.ThrowIfCancellationRequested();

            uint startTime = 0;
            if (timeout != Timeout.Infinite && timeout > 0)
                startTime = GetTimestamp();


            CancellationTokenRegistration cancellationTokenRegistration = default(CancellationTokenRegistration);
            if (token.CanBeCanceled && timeout != 0)
                cancellationTokenRegistration = CancellationTokenHelper.RegisterWithoutEC(token, _cancellationTokenCanceledEventHandler, this);

            bool lockTaken = false;
            bool enterWaitUpdated = false;


            try
            {
                int myId = 0;
                try { }
                finally
                {
                    myId = Interlocked.Increment(ref _entringWaitersNumber) - 1;
                    enterWaitUpdated = true;
                }

                if (_processorCount > 1)
                {
                    if (_currentCount <= (myId - _finishedWaitersNumber))
                    {            
                        Thread.SpinWait(128);
                        int spinCnt = 0;
                        while (spinCnt++ < 28 && (_currentCount <= (myId - _finishedWaitersNumber)))
                            Thread.SpinWait(300);
                        if (myId - _finishedWaitersNumber > _processorCount)
                            Thread.Yield();
                    }
                }
                else if (timeout == 0 && _currentCount == 0)
                {
                    return false;
                }



                try { }
                finally
                {
                    Monitor.Enter(_lockObj, ref lockTaken);
                    Contract.Assert(lockTaken);
                    _waitCount++;
                }


                if (_currentCount == 0)
                {
                    if (timeout == 0)
                        return false;

                    bool waitSuccessful = WaitUntilCountOrTimeout(timeout, startTime, token);
                    if (!waitSuccessful)
                    {
                        if (token.IsCancellationRequested)
                            throw new OperationCanceledException(token);

                        return false;
                    }
                }


                Contract.Assert(_currentCount > 0);
                _currentCount--;


                var waitHandle = _waitHandle;
                if (waitHandle != null && _currentCount == 0)
                    waitHandle.Reset();
            }
            finally
            {
                if (enterWaitUpdated)
                {
                    if (lockTaken)
                        _finishedWaitersNumber++;
                    else
                        Interlocked.Decrement(ref _entringWaitersNumber); // Так не будет потерянных обновлений
                }

                if (lockTaken)
                {
                    _waitCount--;
                    Monitor.Exit(_lockObj);
                }

                cancellationTokenRegistration.Dispose();
            }

            return true;
        }




        /// <summary>
        /// Выполнить ожидание появления 1-ого слота в семаформе. 
        /// При успешном захвате число доступных слотов уменьшается на 1
        /// </summary>
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
        public bool Wait(int timeout)
        {
            return Wait(timeout, new CancellationToken());
        }
 
 
        /// <summary>
        /// Освободить N слотов
        /// </summary>
        /// <param name="releaseCount">Число слотов для освобождения</param>
        /// <returns>Число слотов до освобождения</returns>
        public int Release(int releaseCount)
        {
            CheckDispose();

            if (releaseCount < 1)
                throw new ArgumentOutOfRangeException("releaseCount", "releaseCount should be positive");

            int returnCount = 0;

            lock (_lockObj)
            {
                var currentCount = _currentCount; 
                if (_maxCount - currentCount < releaseCount)
                    throw new SemaphoreFullException();

                returnCount = currentCount;
                currentCount += releaseCount;
 
                int waitCount = _waitCount;
                if (currentCount == 1 || waitCount == 1)
                {
                    Monitor.Pulse(_lockObj);
                }
                else if (waitCount > 1)
                {
                    Monitor.PulseAll(_lockObj);
                }
 
                _currentCount = currentCount;

                var waitHandle = _waitHandle;
                if (waitHandle != null && returnCount == 0 && currentCount > 0)
                    waitHandle.Set();
            }
 
            return returnCount;
        }
        /// <summary>
        /// Освободить 1 слот
        /// </summary>
        /// <returns>Число слотов до освобождения</returns>
        public int Release()
        {
            return Release(1);
        }
 


        /// <summary>
        /// Освобождение ресурсов
        /// </summary>
        /// <param name="isUserCall">Вызвано ли пользователем</param>
        protected virtual void Dispose(bool isUserCall)
        {
            if (isUserCall)
            {
                var waitHandle = _waitHandle;
                if (waitHandle != null)
                {
                    waitHandle.Close();
                    _waitHandle = null;
                }
                _lockObj = null;
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
