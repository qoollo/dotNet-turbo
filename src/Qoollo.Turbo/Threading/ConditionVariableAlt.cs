using Qoollo.Turbo.Threading.ServiceStuff;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Qoollo.Turbo.Collections;

namespace Qoollo.Turbo.Threading
{
    /// <summary>
    /// Guard helper for <see cref="ConditionVariableAlt"/> to use it with 'using' statement
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

            Debug.Assert(_sourceWaiter.IsEntered());

            bool timeouted = false;

            var waitingPrimitive = _sourceWaiter.RegisterWaiter();
            _sourceWaiter.AddWaiterToQueue(waitingPrimitive);

            try
            {
                Monitor.Exit(_sourceWaiter.ExternalLock);
                if (Monitor.IsEntered(_sourceWaiter.ExternalLock))
                    throw new SynchronizationLockException("Recursive lock is not supported");

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

                if (!timeouted && !waitingPrimitive.Wait(remainingWaitMilliseconds))
                    timeouted = true;

                if (!timeouted)
                {
                    // Check if cancellation or dispose was the reasons of the signal
                    if (_token.IsCancellationRequested)
                        throw new OperationCanceledException(_token);
                    if (_sourceWaiter.IsDisposed)
                        throw new OperationInterruptedException("Wait was interrupted by Dispose", new ObjectDisposedException(nameof(ConditionVariable), $"ConditionVariable '{_sourceWaiter.Name}' was disposed"));
                }
            }
            finally
            {
                Monitor.Enter(_sourceWaiter.ExternalLock);

                if (!waitingPrimitive.IsSet)
                    _sourceWaiter.RemoveWaiterFromQueue(waitingPrimitive);
            }

            return waitingPrimitive.IsSet;
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

            Debug.Assert(_sourceWaiter.IsEntered());

            bool timeouted = false;

            var waitingPrimitive = _sourceWaiter.RegisterWaiter();
            _sourceWaiter.AddWaiterToQueue(waitingPrimitive);

            try
            {
                Monitor.Exit(_sourceWaiter.ExternalLock);
                if (Monitor.IsEntered(_sourceWaiter.ExternalLock))
                    throw new SynchronizationLockException("Recursive lock is not supported");

                int remainingWaitMilliseconds = Timeout.Infinite;
                if (_timeout != Timeout.Infinite)
                {
                    remainingWaitMilliseconds = TimeoutHelper.UpdateTimeout(_startTime, _timeout);
                    if (remainingWaitMilliseconds <= 0)
                        timeouted = true;
                }

                if (!timeouted && !waitingPrimitive.Wait(remainingWaitMilliseconds))
                    timeouted = true;

                if (!timeouted)
                {
                    // Check if cancellation or dispose was the reasons of the signal
                    if (_token.IsCancellationRequested)
                        throw new OperationCanceledException(_token);
                    if (_sourceWaiter.IsDisposed)
                        throw new OperationInterruptedException("Wait was interrupted by Dispose", new ObjectDisposedException(nameof(ConditionVariable), $"ConditionVariable '{_sourceWaiter.Name}' was disposed"));
                }
            }
            finally
            {
                Monitor.Enter(_sourceWaiter.ExternalLock);

                if (!waitingPrimitive.IsSet)
                    _sourceWaiter.RemoveWaiterFromQueue(waitingPrimitive);
            }

            return waitingPrimitive.IsSet;
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

            Debug.Assert(_sourceWaiter.IsEntered());

            if (predicate(state))
                return true;
            else if (_timeout == 0)
                return false;

            if (_timeout > 0 && TimeoutHelper.UpdateTimeout(_startTime, _timeout) <= 0) // Predicate estimation took too much time
                return false;

            int remainingWaitMilliseconds = Timeout.Infinite;

            var waitingPrimitive = _sourceWaiter.RegisterWaiter();
            bool firstTry = true;

            while (!_token.IsCancellationRequested && !_sourceWaiter.IsDisposed)
            {
                try
                {
                    if (waitingPrimitive.IsSet || firstTry)
                    {
                        firstTry = false;
                        waitingPrimitive.Reset();
                        _sourceWaiter.AddWaiterToQueue(waitingPrimitive);
                    }

                    Monitor.Exit(_sourceWaiter.ExternalLock);
                    if (Monitor.IsEntered(_sourceWaiter.ExternalLock))
                        throw new SynchronizationLockException("Recursive lock is not supported");

                    if (_timeout != Timeout.Infinite)
                    {
                        remainingWaitMilliseconds = TimeoutHelper.UpdateTimeout(_startTime, _timeout);
                        if (remainingWaitMilliseconds <= 0)
                            break;
                    }

                    if (!waitingPrimitive.Wait(remainingWaitMilliseconds))
                        break;
                }
                finally
                {
                    Monitor.Enter(_sourceWaiter.ExternalLock);
                    if (!waitingPrimitive.IsSet)
                        _sourceWaiter.RemoveWaiterFromQueue(waitingPrimitive);
                }

                if (predicate(state))
                    return true;
            }

            // Check if cancellation or dispose was the reasons of the signal
            if (_token.IsCancellationRequested)
                throw new OperationCanceledException(_token);
            if (_sourceWaiter.IsDisposed)
                throw new OperationInterruptedException("Wait was interrupted by Dispose", new ObjectDisposedException(nameof(ConditionVariableAlt), $"ConditionVariable '{_sourceWaiter.Name}' was disposed"));


            // Final check for predicate
            return predicate(state);
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

            Debug.Assert(_sourceWaiter.IsEntered());

            if (predicate(ref state))
                return true;
            else if (_timeout == 0)
                return false;

            if (_timeout > 0 && TimeoutHelper.UpdateTimeout(_startTime, _timeout) <= 0) // Predicate estimation took too much time
                return false;

            int remainingWaitMilliseconds = Timeout.Infinite;

            var waitingPrimitive = _sourceWaiter.RegisterWaiter();
            bool firstTry = true;

            while (!_token.IsCancellationRequested && !_sourceWaiter.IsDisposed)
            {
                try
                {
                    if (waitingPrimitive.IsSet || firstTry)
                    {
                        firstTry = false;
                        waitingPrimitive.Reset();
                        _sourceWaiter.AddWaiterToQueue(waitingPrimitive);
                    }

                    Monitor.Exit(_sourceWaiter.ExternalLock);
                    if (Monitor.IsEntered(_sourceWaiter.ExternalLock))
                        throw new SynchronizationLockException("Recursive lock is not supported");

                    if (_timeout != Timeout.Infinite)
                    {
                        remainingWaitMilliseconds = TimeoutHelper.UpdateTimeout(_startTime, _timeout);
                        if (remainingWaitMilliseconds <= 0)
                            break;
                    }

                    if (!waitingPrimitive.Wait(remainingWaitMilliseconds))
                        break;
                }
                finally
                {
                    Monitor.Enter(_sourceWaiter.ExternalLock);
                    if (!waitingPrimitive.IsSet)
                        _sourceWaiter.RemoveWaiterFromQueue(waitingPrimitive);
                }

                if (predicate(ref state))
                    return true;
            }

            // Check if cancellation or dispose was the reasons of the signal
            if (_token.IsCancellationRequested)
                throw new OperationCanceledException(_token);
            if (_sourceWaiter.IsDisposed)
                throw new OperationInterruptedException("Wait was interrupted by Dispose", new ObjectDisposedException(nameof(ConditionVariableAlt), $"ConditionVariable '{_sourceWaiter.Name}' was disposed"));


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
            conditionVar.PulseAll();
        }

        // ==============

        private readonly string _name;
        private readonly object _externalLock;
        private readonly CircularList<ManualResetEventSlim> _waiterQueue;
        private readonly ThreadLocal<ManualResetEventSlim> _perThreadWaitEvent;
        private volatile int _waiterCount;
        private volatile bool _isDisposed;


        /// <summary>
        /// ConditionVariable constructor
        /// </summary>
        /// <param name="externalLock">External lock object that should be aqcuired before entering the ConditionVariable</param>
        /// <param name="name">Name for the current <see cref="ConditionVariableAlt"/></param>
        public ConditionVariableAlt(object externalLock, string name)
        {
            if (externalLock == null)
                throw new ArgumentNullException(nameof(externalLock));

            _name = name ?? nameof(ConditionVariable);
            _waiterCount = 0;
            _externalLock = externalLock;
            _waiterQueue = new CircularList<ManualResetEventSlim>();
            _perThreadWaitEvent = new ThreadLocal<ManualResetEventSlim>();
        }
        /// <summary>
        /// ConditionVariable constructor
        /// </summary>
        /// <param name="externalLock">External lock object that should be aqcuired before entering the ConditionVariable</param>
        public ConditionVariableAlt(object externalLock) 
            : this(externalLock, null)
        {
        }

        /// <summary>
        /// The number of waiting threads on the ConditionVariable
        /// </summary>
        public int WaiterCount { get { return _waiterCount; } }
        /// <summary>
        /// Name to identify ConditionVariable instance
        /// </summary>
        public string Name { get { return _name; } }
        /// <summary>
        /// Is ConditionVariable in disposed state
        /// </summary>
        internal bool IsDisposed { get { return _isDisposed; } }
        /// <summary>
        /// External lock object
        /// </summary>
        internal object ExternalLock { get { return _externalLock; } }

        /// <summary>
        /// Register the current thread as waiting thread
        /// </summary>
        /// <returns>Object to wait on</returns>
        internal ManualResetEventSlim RegisterWaiter()
        {
            ManualResetEventSlim result = _perThreadWaitEvent.Value;
            if (result == null)
            {
                result = new ManualResetEventSlim(false, 6);
                _perThreadWaitEvent.Value = result;
            }
            else
            {
                result.Reset();
            }

            return result;
        }
        /// <summary>
        /// Adds waiter to notification queue
        /// </summary>
        /// <param name="waitingPrimitive">Waiting primitive</param>
        internal void AddWaiterToQueue(ManualResetEventSlim waitingPrimitive)
        {
            Debug.Assert(waitingPrimitive != null);
            Debug.Assert(this.IsEntered());

            _waiterQueue.AddLast(waitingPrimitive);
        }
        /// <summary>
        /// Removes waiter from notification queue
        /// </summary>
        /// <param name="waitingPrimitive">Waiting primitive to remove</param>
        /// <returns>Was removed or not</returns>
        internal bool RemoveWaiterFromQueue(ManualResetEventSlim waitingPrimitive)
        {
            Debug.Assert(waitingPrimitive != null);
            Debug.Assert(this.IsEntered());

            return _waiterQueue.Remove(waitingPrimitive);
        }

        /// <summary>
        /// Determines whether the current thread holds the lock
        /// </summary>
        /// <returns>True if the thead holds the lock</returns>
        public bool IsEntered()
        {
            return Monitor.IsEntered(_externalLock);
        }


        /// <summary>
        /// Enter the lock on the current <see cref="ConditionVariableAlt"/> object
        /// </summary>
        /// <param name="timeout">Total operation timeout</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>Lock guard to work with 'using' statement</returns>
        /// <exception cref="ObjectDisposedException">ConditionVariable disposed</exception>
        /// <exception cref="OperationCanceledException">Cancellation requested</exception>
        /// <exception cref="SynchronizationLockException">externalLock is already acquired</exception>
        public ConditionVariableAltWaiter Enter(int timeout, CancellationToken token)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(ConditionVariableAlt), $"ConditionVariable '{Name}' was disposed");
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
        /// Enter the lock on the current <see cref="ConditionVariableAlt"/> object
        /// </summary>
        /// <param name="timeout">Total operation timeout</param>
        /// <returns>Lock guard to work with 'using' statement</returns>
        /// <exception cref="ObjectDisposedException">ConditionVariable disposed</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ConditionVariableAltWaiter Enter(int timeout)
        {
            return Enter(timeout, default(CancellationToken));
        }
        /// <summary>
        /// Enter the lock on the current <see cref="ConditionVariableAlt"/> object
        /// </summary>
        /// <param name="token">Cancellation token</param>
        /// <returns>Lock guard to work with 'using' statement</returns>
        /// <exception cref="ObjectDisposedException">ConditionVariable disposed</exception>
        /// <exception cref="OperationCanceledException">Cancellation requested</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ConditionVariableAltWaiter Enter(CancellationToken token)
        {
            return Enter(Timeout.Infinite, default(CancellationToken));
        }
        /// <summary>
        /// Enter the lock on the current <see cref="ConditionVariableAlt"/> object
        /// </summary>
        /// <returns>Lock guard to work with 'using' statement</returns>
        /// <exception cref="ObjectDisposedException">ConditionVariable disposed</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ConditionVariableAltWaiter Enter()
        {
            return Enter(Timeout.Infinite, default(CancellationToken));
        }


        /// <summary>
        /// Leaves the lock section for current <see cref="ConditionVariableAlt"/> object.
        /// Should be called from <see cref="ConditionVariableAltWaiter.Dispose"/>
        /// </summary>
        /// <param name="cvLock">ConditionVariableWaiter with required info</param>
        internal void Exit(ref ConditionVariableAltWaiter cvLock)
        {
            Interlocked.Decrement(ref _waiterCount);
            Monitor.Exit(_externalLock);
        }


        /// <summary>
        /// Notifies specified number of threads about possible state change
        /// </summary>
        /// <param name="count">Number of threads to be notified</param>
        public void Pulse(int count)
        {
            if (_waiterCount > 0)
            {
                lock (_externalLock)
                {
                    Debug.Assert(_waiterCount >= _waiterQueue.Count);
                    for (int i = 0; i < count; i++)
                    {
                        if (_waiterQueue.Count == 0)
                            break;

                        var waiter = _waiterQueue.RemoveFirst();
                        waiter.Set();
                    }
                }
            }
        }

        /// <summary>
        /// Notifies single waiting thread about possible state change
        /// </summary>
        public void Pulse()
        {
            Pulse(1);
        }

        /// <summary>
        /// Notifies all waiting threads about possible state change
        /// </summary>
        public void PulseAll()
        {
            Pulse(int.MaxValue);
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

                Debug.Assert(_waiterQueue.Count == 0);
                _perThreadWaitEvent.Dispose();
            }
        }
    }
}
