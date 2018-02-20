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
    internal struct ConditionVariableWaiter: IDisposable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void EnterLock(object syncObj, ref bool lockTaken)
        {
            if (!lockTaken)
            {
                Monitor.Enter(syncObj, ref lockTaken);
                TurboContract.Assert(lockTaken, "lockTaken == false");
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryEnterLock(object syncObj, ref bool lockTaken)
        {
            if (!lockTaken)
                Monitor.TryEnter(syncObj, 0, ref lockTaken);

            return lockTaken;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ExitLock(object syncObj, ref bool lockTaken)
        {
            if (lockTaken)
            {
                try { }
                finally
                {
                    Monitor.Exit(syncObj);
                    Volatile.Write(ref lockTaken, false);
                }
            }
        }

        //=============================


        private ConditionVariable _sourceWaiter;
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
        internal ConditionVariableWaiter(ConditionVariable sourceWaiter, int timeout, uint startTime, CancellationToken token, CancellationTokenRegistration cancellationTokenReg)
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


        /// <summary>
        /// Blocks the current thread until the next notification
        /// </summary>
        /// <param name="customTimeout">Additional timeout in milliseconds that will be combined with initial timeout by Min operation</param>
        /// <returns>True if the current thread successfully received a notification</returns>
        /// <exception cref="ObjectDisposedException">ConditionVariable was disposed</exception>
        /// <exception cref="OperationCanceledException">Cancellation happened</exception>
        /// <exception cref="OperationInterruptedException">Waiting was interrupted by Dispose</exception>
        public bool Wait(int customTimeout)
        {
            if (_sourceWaiter == null)
                throw new ObjectDisposedException(nameof(ConditionVariableWaiter), "Lock section has exited");
            if (_sourceWaiter.IsDisposed)
                throw new ObjectDisposedException(nameof(ConditionVariable), $"ConditionVariable '{_sourceWaiter.Name}' was disposed");
            if (_token.IsCancellationRequested)
                throw new OperationCanceledException(_token);

            TurboContract.Assert(_sourceWaiter.IsEntered(), conditionString: "_sourceWaiter.IsEntered()");

            bool internalLockTaken = false;
            bool externalLockTaken = true;

            try
            {
                try { }
                finally
                {
                    Monitor.Enter(_sourceWaiter.InternalLock, ref internalLockTaken);
                    TurboContract.Assert(internalLockTaken, "internalLockTaken == false");
                }

                if (_sourceWaiter.IsDisposed)
                    throw new ObjectDisposedException(nameof(ConditionVariable), $"ConditionVariable '{_sourceWaiter.Name}' was disposed");
                if (_token.IsCancellationRequested)
                    throw new OperationCanceledException(_token);



                int remainingWaitMilliseconds = Timeout.Infinite;
                if (_timeout != Timeout.Infinite)
                {
                    remainingWaitMilliseconds = TimeoutHelper.UpdateTimeout(_startTime, _timeout);
                    if (customTimeout >= 0)
                        remainingWaitMilliseconds = Math.Min(remainingWaitMilliseconds, customTimeout);
                    if (remainingWaitMilliseconds <= 0)
                        return false;
                }
                else if (customTimeout > 0)
                {
                    remainingWaitMilliseconds = customTimeout;
                }
                else if (customTimeout == 0)
                {
                    return false;
                }

                // Exit external lock right before Wait
                ExitLock(_sourceWaiter.ExternalLock, ref externalLockTaken);

                // Waiting for signal
                if (!Monitor.Wait(_sourceWaiter.InternalLock, remainingWaitMilliseconds))
                    return false;

                // Check if cancellation or dispose was the reasons of the signal
                if (_token.IsCancellationRequested)
                    throw new OperationCanceledException(_token);
                if (_sourceWaiter.IsDisposed)
                    throw new OperationInterruptedException("Wait was interrupted by Dispose", new ObjectDisposedException(nameof(ConditionVariable), $"ConditionVariable '{_sourceWaiter.Name}' was disposed"));
            }
            finally
            {
                if (internalLockTaken)
                    Monitor.Exit(_sourceWaiter.InternalLock);

                EnterLock(_sourceWaiter.ExternalLock, ref externalLockTaken);
            }

            return true;
        }


        /// <summary>
        /// Blocks the current thread until the next notification
        /// </summary>
        /// <returns>True if the current thread successfully received a notification</returns>
        /// <exception cref="ObjectDisposedException">ConditionVariable was disposed</exception>
        /// <exception cref="OperationCanceledException">Cancellation happened</exception>
        /// <exception cref="OperationInterruptedException">Waiting was interrupted by Dispose</exception>
        public bool Wait()
        {
            if (_sourceWaiter == null)
                throw new ObjectDisposedException(nameof(ConditionVariableWaiter), "Lock section has exited");
            if (_sourceWaiter.IsDisposed)
                throw new ObjectDisposedException(nameof(ConditionVariable), $"ConditionVariable '{_sourceWaiter.Name}' was disposed");
            if (_token.IsCancellationRequested)
                throw new OperationCanceledException(_token);

            TurboContract.Assert(_sourceWaiter.IsEntered(), conditionString: "_sourceWaiter.IsEntered()");

            bool internalLockTaken = false;
            bool externalLockTaken = true;

            try
            {
                try { }
                finally
                {
                    Monitor.Enter(_sourceWaiter.InternalLock, ref internalLockTaken);
                    TurboContract.Assert(internalLockTaken, conditionString: "internalLockTaken");
                }

                if (_sourceWaiter.IsDisposed)
                    throw new ObjectDisposedException(nameof(ConditionVariable), $"ConditionVariable '{_sourceWaiter.Name}' was disposed");
                if (_token.IsCancellationRequested)
                    throw new OperationCanceledException(_token);


                int remainingWaitMilliseconds = Timeout.Infinite;
                if (_timeout != Timeout.Infinite)
                {
                    remainingWaitMilliseconds = TimeoutHelper.UpdateTimeout(_startTime, _timeout);
                    if (remainingWaitMilliseconds <= 0)
                        return false;
                }


                // Exit external lock right before Wait
                ExitLock(_sourceWaiter.ExternalLock, ref externalLockTaken);

                // Waiting for signal
                if (!Monitor.Wait(_sourceWaiter.InternalLock, remainingWaitMilliseconds))
                    return false;

                // Check if cancellation or dispose was the reasons of the signal
                if (_token.IsCancellationRequested)
                    throw new OperationCanceledException(_token);
                if (_sourceWaiter.IsDisposed)
                    throw new OperationInterruptedException("Wait was interrupted by Dispose", new ObjectDisposedException(nameof(ConditionVariable), $"ConditionVariable '{_sourceWaiter.Name}' was disposed"));
            }
            finally
            {
                if (internalLockTaken)
                    Monitor.Exit(_sourceWaiter.InternalLock);

                EnterLock(_sourceWaiter.ExternalLock, ref externalLockTaken);
            }

            return true;
        }



        private bool WaitUntilPredicate<TState>(ref bool internalLockTaken, ref bool externalLockTaken, WaitPredicate<TState> predicate, TState state)
        {
            TurboContract.Requires(internalLockTaken, "internalLockTaken == false");
            TurboContract.Requires(externalLockTaken, "externalLockTaken == false");
            TurboContract.Requires(predicate != null, conditionString: "predicate != null");

            int remainingWaitMilliseconds = Timeout.Infinite;

            while (true)
            {
                if (_token.IsCancellationRequested || _sourceWaiter.IsDisposed)
                    return false;

                if (_timeout != Timeout.Infinite)
                {
                    remainingWaitMilliseconds = TimeoutHelper.UpdateTimeout(_startTime, _timeout);
                    if (remainingWaitMilliseconds <= 0)
                        return false;
                }

                ExitLock(_sourceWaiter.ExternalLock, ref externalLockTaken);

                if (!Monitor.Wait(_sourceWaiter.InternalLock, remainingWaitMilliseconds))
                    return false;

                try
                {
                    ExitLock(_sourceWaiter.InternalLock, ref internalLockTaken);
                    EnterLock(_sourceWaiter.ExternalLock, ref externalLockTaken);

                    if (predicate.Invoke(state))
                        return true;
                }
                finally
                {
                    EnterLock(_sourceWaiter.InternalLock, ref internalLockTaken);
                }
            }
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

            TurboContract.Assert(_sourceWaiter.IsEntered(), conditionString: "_sourceWaiter.IsEntered()");

            if (predicate(state))
                return true;
            else if (_timeout == 0)
                return false;

            if (_timeout > 0 && TimeoutHelper.UpdateTimeout(_startTime, _timeout) <= 0) // Predicate estimation took too much time
                return false;

            bool internalLockTaken = false;
            bool externalLockTaken = true;
            try
            {
                try { }
                finally
                {
                    Monitor.Enter(_sourceWaiter.InternalLock, ref internalLockTaken);
                    TurboContract.Assert(internalLockTaken, conditionString: "internalLockTaken");
                }

                if (WaitUntilPredicate(ref internalLockTaken, ref externalLockTaken, predicate, state))
                    return true;

                // Check if cancellation or dispose was the reasons of the signal
                if (_token.IsCancellationRequested)
                    throw new OperationCanceledException(_token);
                if (_sourceWaiter.IsDisposed)
                    throw new OperationInterruptedException("Wait was interrupted by Dispose", new ObjectDisposedException(nameof(ConditionVariable), $"ConditionVariable '{_sourceWaiter.Name}' was disposed"));
            }
            finally
            {
                if (internalLockTaken)
                    Monitor.Exit(_sourceWaiter.InternalLock);

                EnterLock(_sourceWaiter.ExternalLock, ref externalLockTaken);
            }

            // Final check for predicate
            return predicate(state);
        }



        private bool WaitUntilPredicate<TState>(ref bool internalLockTaken, ref bool externalLockTaken, WaitPredicateRef<TState> predicate, ref TState state)
        {
            TurboContract.Requires(internalLockTaken, "internalLockTaken == false");
            TurboContract.Requires(externalLockTaken, "externalLockTaken == false");
            TurboContract.Requires(predicate != null, conditionString: "predicate != null");

            int remainingWaitMilliseconds = Timeout.Infinite;

            while (true)
            {
                if (_token.IsCancellationRequested || _sourceWaiter.IsDisposed)
                    return false;

                if (_timeout != Timeout.Infinite)
                {
                    remainingWaitMilliseconds = TimeoutHelper.UpdateTimeout(_startTime, _timeout);
                    if (remainingWaitMilliseconds <= 0)
                        return false;
                }

                ExitLock(_sourceWaiter.ExternalLock, ref externalLockTaken);

                if (!Monitor.Wait(_sourceWaiter.InternalLock, remainingWaitMilliseconds))
                    return false;

                try
                {
                    ExitLock(_sourceWaiter.InternalLock, ref internalLockTaken);
                    EnterLock(_sourceWaiter.ExternalLock, ref externalLockTaken);

                    if (predicate.Invoke(ref state))
                        return true;
                }
                finally
                {
                    EnterLock(_sourceWaiter.InternalLock, ref internalLockTaken);
                }
            }
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
        public bool Wait<TState>(WaitPredicateRef<TState> predicate, ref TState state)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));
            if (_sourceWaiter == null)
                throw new ObjectDisposedException(nameof(ConditionVariableWaiter), "Lock section has exited");
            if (_sourceWaiter.IsDisposed)
                throw new ObjectDisposedException(nameof(ConditionVariable), $"ConditionVariable '{_sourceWaiter.Name}' was disposed");
            if (_token.IsCancellationRequested)
                throw new OperationCanceledException(_token);

            TurboContract.Assert(_sourceWaiter.IsEntered(), conditionString: "_sourceWaiter.IsEntered()");

            if (predicate(ref state))
                return true;
            else if (_timeout == 0)
                return false;

            if (_timeout > 0 && TimeoutHelper.UpdateTimeout(_startTime, _timeout) <= 0) // Predicate estimation took too much time
                return false;

            bool internalLockTaken = false;
            bool externalLockTaken = true;
            try
            {
                try { }
                finally
                {
                    Monitor.Enter(_sourceWaiter.InternalLock, ref internalLockTaken);
                    TurboContract.Assert(internalLockTaken, conditionString: "internalLockTaken");
                }

                if (WaitUntilPredicate(ref internalLockTaken, ref externalLockTaken, predicate, ref state))
                    return true;

                // Check if cancellation or dispose was the reasons of the signal
                if (_token.IsCancellationRequested)
                    throw new OperationCanceledException(_token);
                if (_sourceWaiter.IsDisposed)
                    throw new OperationInterruptedException("Wait was interrupted by Dispose", new ObjectDisposedException(nameof(ConditionVariable), $"ConditionVariable '{_sourceWaiter.Name}' was disposed"));
            }
            finally
            {
                if (internalLockTaken)
                    Monitor.Exit(_sourceWaiter.InternalLock);

                EnterLock(_sourceWaiter.ExternalLock, ref externalLockTaken);
            }

            // Final check for predicate
            return predicate(ref state);
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


    /// <summary>
    /// Synchronization primitive that blocks the current thread until a specified condition occurs
    /// </summary>
    internal class ConditionVariable: IDisposable
    {
        private static readonly Action<object> _cancellationTokenCanceledEventHandler = new Action<object>(CancellationTokenCanceledEventHandler);
        /// <summary>
        /// Cancellation handler for CancellationToken
        /// </summary>
        /// <param name="obj">ConditionVariable object</param>
        private static void CancellationTokenCanceledEventHandler(object obj)
        {
            ConditionVariable conditionVar = obj as ConditionVariable;
            TurboContract.Assert(conditionVar != null, conditionString: "conditionVar != null");
            lock (conditionVar.InternalLock)
            {
                Monitor.PulseAll(conditionVar.InternalLock);
            }
        }

        // =============

        private readonly string _name;
        private volatile int _waiterCount;
        private volatile bool _isDisposed;
        private readonly object _internalLock;
        private readonly object _externalLock;

        /// <summary>
        /// ConditionVariable constructor
        /// </summary>
        /// <param name="externalLock">External lock object that should be aqcuired before entering the ConditionVariable</param>
        /// <param name="name">Name for the current <see cref="ConditionVariable"/></param>
        public ConditionVariable(object externalLock, string name)
        {
            if (externalLock == null)
                throw new ArgumentNullException(nameof(externalLock));

            _name = name ?? nameof(ConditionVariable);
            _waiterCount = 0;
            _internalLock = new object();
            _externalLock = externalLock;
        }
        /// <summary>
        /// ConditionVariable constructor
        /// </summary>
        /// <param name="externalLock">External lock object that should be aqcuired before entering the ConditionVariable</param>
        public ConditionVariable(object externalLock) 
            : this(externalLock, null)
        {
        }

        /// <summary>
        /// The number of waiting threads on the ConditionVariable
        /// </summary>
        public int WaiterCount { get { return _waiterCount; } }
        /// <summary>
        /// Is ConditionVariable in disposed state
        /// </summary>
        internal bool IsDisposed { get { return _isDisposed; } }
        /// <summary>
        /// Name to identify ConditionVariable instance
        /// </summary>
        public string Name { get { return _name; } }
        /// <summary>
        /// Internal lock object
        /// </summary>
        internal object InternalLock { get { return _internalLock; } }
        /// <summary>
        /// External lock object
        /// </summary>
        internal object ExternalLock { get { return _externalLock; } }

        /// <summary>
        /// Determines whether the current thread holds the lock
        /// </summary>
        /// <returns>True if the thead holds the lock</returns>
        public bool IsEntered()
        {
            return Monitor.IsEntered(_externalLock);
        }


        /// <summary>
        /// Enter the lock on the current <see cref="ConditionVariable"/> object
        /// </summary>
        /// <param name="timeout">Total operation timeout</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>Lock guard to work with 'using' statement</returns>
        /// <exception cref="ObjectDisposedException">ConditionVariable disposed</exception>
        /// <exception cref="OperationCanceledException">Cancellation requested</exception>
        /// <exception cref="SynchronizationLockException">externalLock is already acquired</exception>
        public ConditionVariableWaiter Enter(int timeout, CancellationToken token)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(ConditionVariable), $"ConditionVariable '{Name}' was disposed");
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
                    cancellationTokenRegistration = CancellationTokenHelper.RegisterWithoutECIfPossible(token, _cancellationTokenCanceledEventHandler, this);
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
            return new ConditionVariableWaiter(this, timeout, startTime, token, cancellationTokenRegistration);
        }
        /// <summary>
        /// Enter the lock on the current <see cref="ConditionVariable"/> object
        /// </summary>
        /// <param name="timeout">Total operation timeout</param>
        /// <returns>Lock guard to work with 'using' statement</returns>
        /// <exception cref="ObjectDisposedException">ConditionVariable disposed</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ConditionVariableWaiter Enter(int timeout)
        {
            return Enter(timeout, default(CancellationToken));
        }
        /// <summary>
        /// Enter the lock on the current <see cref="ConditionVariable"/> object
        /// </summary>
        /// <param name="token">Cancellation token</param>
        /// <returns>Lock guard to work with 'using' statement</returns>
        /// <exception cref="ObjectDisposedException">ConditionVariable disposed</exception>
        /// <exception cref="OperationCanceledException">Cancellation requested</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ConditionVariableWaiter Enter(CancellationToken token)
        {
            return Enter(Timeout.Infinite, default(CancellationToken));
        }
        /// <summary>
        /// Enter the lock on the current <see cref="ConditionVariable"/> object
        /// </summary>
        /// <returns>Lock guard to work with 'using' statement</returns>
        /// <exception cref="ObjectDisposedException">ConditionVariable disposed</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ConditionVariableWaiter Enter()
        {
            return Enter(Timeout.Infinite, default(CancellationToken));
        }

        /// <summary>
        /// Leaves the lock section for current <see cref="ConditionVariable"/> object.
        /// Should be called from <see cref="ConditionVariableWaiter.Dispose"/>
        /// </summary>
        /// <param name="cvLock">ConditionVariableWaiter with required info</param>
        internal void Exit(ref ConditionVariableWaiter cvLock)
        {
            Interlocked.Decrement(ref _waiterCount);
            Monitor.Exit(_externalLock);
        }




        /// <summary>
        /// Notifies single waiting thread about possible state change
        /// </summary>
        public void Pulse()
        {
            if (_waiterCount > 0)
            {
                lock (_externalLock)
                {
                    if (_waiterCount > 0)
                    {
                        lock (_internalLock)
                        {
                            Monitor.Pulse(_internalLock);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Notifies specified number of threads about possible state change
        /// </summary>
        /// <param name="count">Number of threads to be notified</param>
        public void Pulse(int count)
        {
            if (count < 0 || count > short.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(count));

            if (_waiterCount > 0)
            {
                lock (_externalLock)
                {
                    if (_waiterCount > 0)
                    {
                        lock (_internalLock)
                        {
                            int countToPulse = Math.Min(_waiterCount, count);
                            for (int i = 0; i < countToPulse; i++)
                                Monitor.Pulse(_internalLock);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Notifies all waiting threads about possible state change
        /// </summary>
        public void PulseAll()
        {
            if (_waiterCount > 0)
            {
                lock (_externalLock)
                {
                    if (_waiterCount > 0)
                    {
                        lock (_internalLock)
                        {
                            Monitor.PulseAll(_internalLock);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Cleans-up resources
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;

            lock (_internalLock)
            {
                if (_isDisposed)
                    return;

                _isDisposed = true;

                Monitor.PulseAll(this._internalLock);
            }
        }
    }
}
