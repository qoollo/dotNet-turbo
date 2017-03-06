using Qoollo.Turbo.Threading.ServiceStuff;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Threading
{
    internal delegate bool ConditionVariablePredicate();
    internal delegate bool ConditionVariablePredicate<TState>(TState state);
    internal delegate bool ConditionVariablePredicateRef<TState>(ref TState state);


    internal class ConditionVariable: IDisposable
    {
        private static readonly Action<object> _cancellationTokenCanceledEventHandler = new Action<object>(CancellationTokenCanceledEventHandler);
        /// <summary>
        /// Обработчик отмены токена
        /// </summary>
        /// <param name="obj">Объект SemaphoreSlimE</param>
        private static void CancellationTokenCanceledEventHandler(object obj)
        {
            ConditionVariable conditionVar = obj as ConditionVariable;
            Debug.Assert(conditionVar != null);
            lock (conditionVar._lockObj)
            {
                Monitor.PulseAll(conditionVar._lockObj);
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

        private volatile int _waitCount;
        private volatile bool _isDisposed;
        private object _lockObj;

        public ConditionVariable()
        {
            _waitCount = 0;
            _lockObj = new object();
        }


        public int WaiterCount { get { return _waitCount; } }


        private bool WaitUntilPredicateOrTimeout(ConditionVariablePredicate predicate, int timeout, uint startTime, CancellationToken token)
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

                if (predicate())
                    return true;
            }
        }

        public bool Wait(ConditionVariablePredicate predicate, int timeout, CancellationToken token)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));
            if (_isDisposed)
                throw new ObjectDisposedException(this.GetType().Name);

            if (timeout < -1)
                timeout = Timeout.Infinite;

            if (token.IsCancellationRequested)
                throw new OperationCanceledException(token);

            uint startTime = 0;
            if (timeout != Timeout.Infinite && timeout > 0)
                startTime = GetTimestamp();

            if (predicate())
                return true;

            if (timeout == 0)
                return false;


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
                    Debug.Assert(lockTaken);
                    _waitCount++;
                }

                if (predicate())
                    return true;

                if (timeout == 0)
                    return false;

                if (WaitUntilPredicateOrTimeout(predicate, timeout, startTime, token))
                    return true;

                if (token.IsCancellationRequested)
                    throw new OperationCanceledException(token);

                if (_isDisposed)
                    throw new OperationInterruptedException("ConditionalVariable wait was interrupted by Dispose", new ObjectDisposedException(this.GetType().Name));
            }
            finally
            {
                if (lockTaken)
                {
                    _waitCount--;
                    Debug.Assert(_waitCount >= 0);
                    Monitor.Exit(_lockObj);
                }

                cancellationTokenRegistration.Dispose();
            }

            return false;
        }


        private bool WaitUntilPredicateOrTimeout<TState>(ConditionVariablePredicate<TState> predicate, TState state, int timeout, uint startTime, CancellationToken token)
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

                if (predicate(state))
                    return true;
            }
        }

        public bool Wait<TState>(ConditionVariablePredicate<TState> predicate, TState state, int timeout, CancellationToken token)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));
            if (_isDisposed)
                throw new ObjectDisposedException(this.GetType().Name);

            if (timeout < -1)
                timeout = Timeout.Infinite;

            if (token.IsCancellationRequested)
                throw new OperationCanceledException(token);

            uint startTime = 0;
            if (timeout != Timeout.Infinite && timeout > 0)
                startTime = GetTimestamp();

            if (predicate(state))
                return true;

            if (timeout == 0)
                return false;


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
                    Debug.Assert(lockTaken);
                    _waitCount++;
                }

                if (predicate(state))
                    return true;

                if (timeout == 0)
                    return false;

                if (WaitUntilPredicateOrTimeout(predicate, state, timeout, startTime, token))
                    return true;

                if (token.IsCancellationRequested)
                    throw new OperationCanceledException(token);

                if (_isDisposed)
                    throw new OperationInterruptedException("ConditionalVariable wait was interrupted by Dispose", new ObjectDisposedException(this.GetType().Name));
            }
            finally
            {
                if (lockTaken)
                {
                    _waitCount--;
                    Debug.Assert(_waitCount >= 0);
                    Monitor.Exit(_lockObj);
                }

                cancellationTokenRegistration.Dispose();
            }

            return false;
        }


        private bool WaitUntilPredicateOrTimeout<TState>(ConditionVariablePredicateRef<TState> predicate, ref TState state, int timeout, uint startTime, CancellationToken token)
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

                if (predicate(ref state))
                    return true;
            }
        }

        public bool Wait<TState>(ConditionVariablePredicateRef<TState> predicate, ref TState state, int timeout, CancellationToken token)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));
            if (_isDisposed)
                throw new ObjectDisposedException(this.GetType().Name);

            if (timeout < -1)
                timeout = Timeout.Infinite;

            if (token.IsCancellationRequested)
                throw new OperationCanceledException(token);

            uint startTime = 0;
            if (timeout != Timeout.Infinite && timeout > 0)
                startTime = GetTimestamp();

            if (predicate(ref state))
                return true;

            if (timeout == 0)
                return false;


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
                    Debug.Assert(lockTaken);
                    Interlocked.Increment(ref _waitCount);
                }

                if (predicate(ref state))
                    return true;

                if (timeout == 0)
                    return false;

                if (WaitUntilPredicateOrTimeout(predicate, ref state, timeout, startTime, token))
                    return true;

                if (token.IsCancellationRequested)
                    throw new OperationCanceledException(token);

                if (_isDisposed)
                    throw new OperationInterruptedException("ConditionalVariable wait was interrupted by Dispose", new ObjectDisposedException(this.GetType().Name));
            }
            finally
            {
                if (lockTaken)
                {
                    _waitCount--;
                    Debug.Assert(_waitCount >= 0);
                    Monitor.Exit(_lockObj);
                }

                cancellationTokenRegistration.Dispose();
            }

            return false;
        }


        public void Signal()
        {
            if (_waitCount > 0)
            {
                lock (_lockObj)
                {
                    Monitor.Pulse(_lockObj);
                }
            }
        }

        public void SignalAll()
        {
            if (_waitCount > 0)
            {
                lock (_lockObj)
                {
                    Monitor.PulseAll(_lockObj);
                }
            }
        }

        /// <summary>
        /// Clean-up resources
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
