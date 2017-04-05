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
    /// Guard helper for <see cref="ConditionVariable"/> to use it with 'using' statement
    /// </summary>
    internal struct ConditionVariableAltWaiter : IDisposable
    {
        private ConditionVariableAlt _sourceWaiter;
        private readonly int _timeout;
        private readonly uint _startTime;
        private readonly CancellationToken _token;
        private readonly CancellationTokenRegistration _cancellationTokenReg;


        /// <summary>
        /// ConditionVariableWaiter constructor
        /// </summary>
        /// <param name="sourceWaiter">Source ConditionVariable</param>
        /// <param name="timeout">Initial operation timeout</param>
        /// <param name="startTime">Time when entered the lock</param>
        /// <param name="token">Cancellation token</param>
        /// <param name="cancellationTokenReg">Cancellation token registration</param>
        internal ConditionVariableAltWaiter(ConditionVariableAlt sourceWaiter, int timeout, uint startTime, CancellationToken token, CancellationTokenRegistration cancellationTokenReg)
        {
            _sourceWaiter = sourceWaiter;
            _timeout = timeout;
            _startTime = startTime;
            _token = token;
            _cancellationTokenReg = cancellationTokenReg;
        }


        /// <summary>
        /// Is specified for the operation time is up
        /// </summary>
        public bool IsTimeouted
        {
            get
            {
                if (_timeout == Timeout.Infinite)
                    return false;

                return TimeoutHelper.UpdateTimeout(_startTime, _timeout) <= 0;
            }
        }


        public bool Wait()
        {
            if (_sourceWaiter == null)
                throw new ObjectDisposedException(nameof(ConditionVariableWaiter), "Lock section has exited");
            if (_sourceWaiter.IsDisposed)
                throw new ObjectDisposedException(nameof(ConditionVariable), $"ConditionVariable '{_sourceWaiter.Name}' was disposed");
            if (_token.IsCancellationRequested)
                throw new OperationCanceledException(_token);


            // Lazily initialze our event, if necessary.
            ManualResetEventSlim mres = _sourceWaiter._waitEvent.Value;
            if (mres == null)
            {
                mres = _sourceWaiter._waitEvent.Value = new ManualResetEventSlim(false);
            }
            else
            {
                mres.Reset();
            }

            _sourceWaiter._waiters.Enqueue(mres);
            try
            {
                Monitor.Exit(_sourceWaiter._externalLock);
                if (Monitor.IsEntered(_sourceWaiter._externalLock))
                    throw new SynchronizationLockException("Recursive lock is not supported");



                int remainingWaitMilliseconds = Timeout.Infinite;
                if (_timeout != Timeout.Infinite)
                {
                    remainingWaitMilliseconds = TimeoutHelper.UpdateTimeout(_startTime, _timeout);
                    if (remainingWaitMilliseconds <= 0)
                        return false;
                }

                if (!mres.Wait(remainingWaitMilliseconds))
                    return false;

                // Check if cancellation or dispose was the reasons of the signal
                if (_token.IsCancellationRequested)
                    throw new OperationCanceledException(_token);
                if (_sourceWaiter.IsDisposed)
                    throw new OperationInterruptedException("Wait was interrupted by Dispose", new ObjectDisposedException(nameof(ConditionVariable), $"ConditionVariable '{_sourceWaiter.Name}' was disposed"));
            }
            finally
            {
                Monitor.Enter(_sourceWaiter._externalLock);
            }



            return true;
        }






        /// <summary>
        /// Blocks the current thread until predicate estimates as True
        /// </summary>
        /// <typeparam name="TState">Type of the state object</typeparam>
        /// <param name="predicate">Predicate that should return True to complete waiting</param>
        /// <param name="state">State object for the predicate</param>
        /// <returns>True if predicate evaluates to True</returns>
        /// <exception cref="ArgumentNullException">predicate is null</exception>
        /// <exception cref="ObjectDisposedException">ConditionVariable was disposed</exception>
        /// <exception cref="OperationCanceledException">Cancellation happened</exception>
        /// <exception cref="OperationInterruptedException">Waiting was interrupted by Dispose</exception>
        public bool Wait<TState>(WaitPredicate<TState> predicate, TState state)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));
            if (_sourceWaiter == null)
                throw new ObjectDisposedException(nameof(ConditionVariableWaiter), "Lock section has exited");
            if (_sourceWaiter.IsDisposed)
                throw new ObjectDisposedException(nameof(ConditionVariable), $"ConditionVariable '{_sourceWaiter.Name}' was disposed");
            if (_token.IsCancellationRequested)
                throw new OperationCanceledException(_token);

            if (predicate(state))
                return true;
            else if (_timeout == 0)
                return false;

            if (_timeout > 0 && TimeoutHelper.UpdateTimeout(_startTime, _timeout) <= 0) // Predicate estimation took too much time
                return false;

            int remainingWaitMilliseconds = Timeout.Infinite;

            ManualResetEventSlim mres = _sourceWaiter._waitEvent.Value;
            if (mres == null)
            {
                mres = _sourceWaiter._waitEvent.Value = new ManualResetEventSlim(false);
            }

            while (!_token.IsCancellationRequested && !_sourceWaiter.IsDisposed)
            {
                // Lazily initialze our event, if necessary.

                mres.Reset();
                _sourceWaiter._waiters.Enqueue(mres);

                try
                {
                    Monitor.Exit(_sourceWaiter._externalLock);
                    if (Monitor.IsEntered(_sourceWaiter._externalLock))
                        throw new SynchronizationLockException("Recursive lock is not supported");


                    if (_timeout != Timeout.Infinite)
                    {
                        remainingWaitMilliseconds = TimeoutHelper.UpdateTimeout(_startTime, _timeout);
                        if (remainingWaitMilliseconds <= 0)
                            break;
                    }

                    if (!mres.Wait(remainingWaitMilliseconds))
                        break;
                }
                finally
                {
                    Monitor.Enter(_sourceWaiter._externalLock);
                }

                if (predicate.Invoke(state))
                    return true;
            }

            // Check if cancellation or dispose was the reasons of the signal
            if (_token.IsCancellationRequested)
                throw new OperationCanceledException(_token);
            if (_sourceWaiter.IsDisposed)
                throw new OperationInterruptedException("Wait was interrupted by Dispose", new ObjectDisposedException(nameof(MonitorObject), $"MonitorObject '{_sourceWaiter.Name}' was disposed"));



            // Final check for predicate
            return predicate(state);
        }



        /// <summary>
        /// Exit the lock section
        /// </summary>
        public void Dispose()
        {
            if (_sourceWaiter != null)
            {
                _sourceWaiter.Exit(ref this); // Exit lock before disposing CancellationTokenRegistration
                _cancellationTokenReg.Dispose();
                _sourceWaiter = null;
            }
        }
    }


    internal class ConditionVariableAlt: IDisposable
    {
        private static readonly Action<object> _cancellationTokenCanceledEventHandler = new Action<object>(CancellationTokenCanceledEventHandler);
        /// <summary>
        /// Cancellation handler for CancellationToken
        /// </summary>
        /// <param name="obj">ConditionVariable object</param>
        private static void CancellationTokenCanceledEventHandler(object obj)
        {
            ConditionVariableAlt conditionVar = obj as ConditionVariableAlt;
            Debug.Assert(conditionVar != null);
            conditionVar.PulseAllInLock();
        }

        // ==============

        internal readonly object _externalLock;
        internal readonly Queue<ManualResetEventSlim> _waiters = new Queue<ManualResetEventSlim>();
        internal readonly ThreadLocal<ManualResetEventSlim> _waitEvent = new ThreadLocal<ManualResetEventSlim>();
        private volatile int _waiterCount;
        private volatile bool _isDisposed;

        public ConditionVariableAlt(object externalLock)
        {
            if (externalLock == null)
                throw new ArgumentNullException(nameof(externalLock));

            _externalLock = externalLock;
        }

        /// <summary>
        /// The number of waiting threads on the ConditionVariable
        /// </summary>
        public int WaiterCount { get { return _waiterCount; } }
        public string Name { get { return nameof(ConditionVariableAlt); } }
        /// <summary>
        /// Is ConditionVariable in disposed state
        /// </summary>
        internal bool IsDisposed { get { return _isDisposed; } }


        public ConditionVariableAltWaiter Enter(int timeout, CancellationToken token)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(MonitorObject), $"ConditionVariable '{Name}' was disposed");
            if (token.IsCancellationRequested)
                throw new OperationCanceledException(token);
            if (Monitor.IsEntered(_externalLock))
                throw new SynchronizationLockException("Recursive lock is not supported");

            uint startTime = 0;
            if (timeout > 0)
                startTime = TimeoutHelper.GetTimestamp();
            else if (timeout < -1)
                timeout = Timeout.Infinite;


            CancellationTokenRegistration cancellationTokenRegistration = default(CancellationTokenRegistration);
            if (token.CanBeCanceled)
            {
                try
                {
                    cancellationTokenRegistration = CancellationTokenHelper.RegisterWithoutEC(token, _cancellationTokenCanceledEventHandler, this);
                    Monitor.Enter(_externalLock); // Can be interrupted
                }
                catch
                {
                    cancellationTokenRegistration.Dispose();
                    throw;
                }
            }
            else
            {
                Monitor.Enter(_externalLock);
            }

            Interlocked.Increment(ref _waiterCount);
            return new ConditionVariableAltWaiter(this, timeout, startTime, token, cancellationTokenRegistration);
        }
        /// <summary>
        /// Enter the lock on the current <see cref="ConditionVariable"/> object
        /// </summary>
        /// <param name="timeout">Total operation timeout</param>
        /// <returns>Lock guard to work with 'using' statement</returns>
        /// <exception cref="ObjectDisposedException">MonitorObject disposed</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ConditionVariableAltWaiter Enter(int timeout)
        {
            return Enter(timeout, default(CancellationToken));
        }
        /// <summary>
        /// Enter the lock on the current <see cref="ConditionVariable"/> object
        /// </summary>
        /// <param name="token">Cancellation token</param>
        /// <returns>Lock guard to work with 'using' statement</returns>
        /// <exception cref="ObjectDisposedException">MonitorObject disposed</exception>
        /// <exception cref="OperationCanceledException">Cancellation requested</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ConditionVariableAltWaiter Enter(CancellationToken token)
        {
            return Enter(Timeout.Infinite, default(CancellationToken));
        }
        /// <summary>
        /// Enter the lock on the current <see cref="ConditionVariable"/> object
        /// </summary>
        /// <returns>Lock guard to work with 'using' statement</returns>
        /// <exception cref="ObjectDisposedException">MonitorObject disposed</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ConditionVariableAltWaiter Enter()
        {
            return Enter(Timeout.Infinite, default(CancellationToken));
        }

        internal void Exit(ref ConditionVariableAltWaiter cvLock)
        {
            Interlocked.Decrement(ref _waiterCount);
            Monitor.Exit(_externalLock);
        }


        private void Wait()
        {
            if (!Monitor.IsEntered(_externalLock))
                throw new SynchronizationLockException("Lock is not held");

            // Lazily initialze our event, if necessary.
            ManualResetEventSlim mres = _waitEvent.Value;
            if (mres == null)
            {
                mres = _waitEvent.Value = new ManualResetEventSlim(false);
            }
            else
            {
                mres.Reset();
            }

            _waiters.Enqueue(mres);
            try
            {
                Monitor.Exit(_externalLock);
                if (!Monitor.IsEntered(_externalLock))
                    throw new SynchronizationLockException("Recursive lock is not supported");
                mres.Wait();
            }
            finally
            {
                Monitor.Enter(_externalLock);
            }
        }


        public void Pulse(int maxPulses)
        {
            if (!Monitor.IsEntered(_externalLock))
                throw new SynchronizationLockException("Lock is not held");

            for (int i = 0; i < maxPulses; i++)
            {
                if (_waiters.Count == 0)
                    break;

                var waiter = _waiters.Dequeue();
                if (waiter.IsSet)
                {
                    i--;
                    continue;
                }

                waiter.Set();
            }
        }

        public void Pulse()
        {
            Pulse(1);
        }

        public void PulseAll()
        {
            Pulse(int.MaxValue);
        }

        internal void PulseAllInLock()
        {
            lock (_externalLock)
            {
                Pulse(int.MaxValue);
            }
        }


        /// <summary>
        /// Cleans-up resources
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;

            lock (_externalLock)
            {
                _isDisposed = true;
                PulseAll();
            }
        }
    }
}
