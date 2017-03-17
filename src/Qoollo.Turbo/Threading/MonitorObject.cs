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
    /// Guard helper for <see cref="MonitorObject"/> to use it with 'using' statement
    /// </summary>
    public struct MonitorWaiter : IDisposable
    {
        private MonitorObject _sourceWaiter;
        private readonly int _timeout;
        private readonly uint _startTime;
        private readonly CancellationToken _token;
        private readonly CancellationTokenRegistration _cancellationTokenReg;


        /// <summary>
        /// MonitorWaiter constructor
        /// </summary>
        /// <param name="sourceWaiter">Source MonitorWaiter</param>
        /// <param name="timeout">Initial operation timeout</param>
        /// <param name="startTime">Time when entered the lock</param>
        /// <param name="token">Cancellation token</param>
        /// <param name="cancellationTokenReg">Cancellation token registration</param>
        internal MonitorWaiter(MonitorObject sourceWaiter, int timeout, uint startTime, CancellationToken token, CancellationTokenRegistration cancellationTokenReg)
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
                throw new ObjectDisposedException(nameof(MonitorWaiter), "Lock section has exited");
            if (_sourceWaiter.IsDisposed)
                throw new ObjectDisposedException(nameof(MonitorObject));
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

            // Waiting for signal
            if (!Monitor.Wait(_sourceWaiter, remainingWaitMilliseconds))
                return false;

            // Check if cancellation or dispose was the reasons of the signal
            if (_token.IsCancellationRequested)
                throw new OperationCanceledException(_token);
            if (_sourceWaiter.IsDisposed)
                throw new OperationInterruptedException("Wait was interrupted by Dispose", new ObjectDisposedException(nameof(MonitorObject)));

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
                throw new ObjectDisposedException(nameof(MonitorWaiter), "Lock section has exited");
            if (_sourceWaiter.IsDisposed)
                throw new ObjectDisposedException(nameof(MonitorObject));
            if (_token.IsCancellationRequested)
                throw new OperationCanceledException(_token);

            int remainingWaitMilliseconds = Timeout.Infinite;
            if (_timeout != Timeout.Infinite)
            {
                remainingWaitMilliseconds = TimeoutHelper.UpdateTimeout(_startTime, _timeout);
                if (remainingWaitMilliseconds <= 0)
                    return false;
            }

            // Waiting for signal
            if (!Monitor.Wait(_sourceWaiter, remainingWaitMilliseconds))
                return false;

            // Check if cancellation or dispose was the reasons of the signal
            if (_token.IsCancellationRequested)
                throw new OperationCanceledException(_token);
            if (_sourceWaiter.IsDisposed)
                throw new OperationInterruptedException("Wait was interrupted by Dispose", new ObjectDisposedException(nameof(MonitorObject)));

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
                throw new ObjectDisposedException(nameof(MonitorWaiter), "Lock section has exited");
            if (_sourceWaiter.IsDisposed)
                throw new ObjectDisposedException(nameof(MonitorObject));
            if (_token.IsCancellationRequested)
                throw new OperationCanceledException(_token);

            if (predicate(state))
                return true;
            else if (_timeout == 0)
                return false;

            int remainingWaitMilliseconds = Timeout.Infinite;

            while (!_token.IsCancellationRequested && !_sourceWaiter.IsDisposed)
            {
                if (_timeout != Timeout.Infinite)
                {
                    remainingWaitMilliseconds = TimeoutHelper.UpdateTimeout(_startTime, _timeout);
                    if (remainingWaitMilliseconds <= 0)
                        break;
                }

                if (!Monitor.Wait(_sourceWaiter, remainingWaitMilliseconds))
                    break;

                if (predicate.Invoke(state))
                    return true;
            }


            // Check if cancellation or dispose was the reasons of the signal
            if (_token.IsCancellationRequested)
                throw new OperationCanceledException(_token);
            if (_sourceWaiter.IsDisposed)
                throw new OperationInterruptedException("Wait was interrupted by Dispose", new ObjectDisposedException(nameof(MonitorObject)));

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
                throw new ObjectDisposedException(nameof(MonitorWaiter), "Lock section has exited");
            if (_sourceWaiter.IsDisposed)
                throw new ObjectDisposedException(nameof(MonitorObject));
            if (_token.IsCancellationRequested)
                throw new OperationCanceledException(_token);

            if (predicate(ref state))
                return true;
            else if (_timeout == 0)
                return false;

            int remainingWaitMilliseconds = Timeout.Infinite;

            while (!_token.IsCancellationRequested && !_sourceWaiter.IsDisposed)
            {
                if (_timeout != Timeout.Infinite)
                {
                    remainingWaitMilliseconds = TimeoutHelper.UpdateTimeout(_startTime, _timeout);
                    if (remainingWaitMilliseconds <= 0)
                        break;
                }

                if (!Monitor.Wait(_sourceWaiter, remainingWaitMilliseconds))
                    break;

                if (predicate.Invoke(ref state))
                    return true;
            }


            // Check if cancellation or dispose was the reasons of the signal
            if (_token.IsCancellationRequested)
                throw new OperationCanceledException(_token);
            if (_sourceWaiter.IsDisposed)
                throw new OperationInterruptedException("Wait was interrupted by Dispose", new ObjectDisposedException(nameof(MonitorObject)));

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
    /// Waiter for external signals (wrapper for <see cref="Monitor.Wait(object)"/> and <see cref="Monitor.Pulse(object)"/>)
    /// </summary>
    [DebuggerDisplay("Name = {Name}, WaiterCount = {WaiterCount}")]
    public class MonitorObject : IDisposable
    {
        private static readonly Action<object> _cancellationTokenCanceledEventHandler = new Action<object>(CancellationTokenCanceledEventHandler);
        /// <summary>
        /// Cancellation handler for CancellationToken
        /// </summary>
        /// <param name="obj">ConditionVariable object</param>
        private static void CancellationTokenCanceledEventHandler(object obj)
        {
            MonitorObject monitorObject = obj as MonitorObject;
            Debug.Assert(monitorObject != null);
            monitorObject.PulseAll();
        }

        // =============

        private readonly string _name;
        private volatile int _waiterCount;
        private volatile bool _isDisposed;

        /// <summary>
        /// MonitorObject constructor
        /// </summary>
        /// <param name="name">Name for the current <see cref="MonitorObject"/></param>
        public MonitorObject(string name)
        {
            _name = name ?? nameof(MonitorObject);
            _waiterCount = 0;
            _isDisposed = false;
        }
        /// <summary>
        /// MonitorObject constructor
        /// </summary>
        public MonitorObject()
            : this(null)
        {
        }

        /// <summary>
        /// The number of waiting threads on the MonitorObject
        /// </summary>
        public int WaiterCount { get { return _waiterCount; } }
        /// <summary>
        /// Is MonitorObject in disposed state
        /// </summary>
        internal bool IsDisposed { get { return _isDisposed; } }
        /// <summary>
        /// Name to identify MonitorObject instance
        /// </summary>
        public string Name { get { return _name; } }



        /// <summary>
        /// Enter the lock on the current <see cref="MonitorObject"/> object
        /// </summary>
        /// <param name="timeout">Total operation timeout</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>Lock guard to work with 'using' statement</returns>
        /// <exception cref="ObjectDisposedException">MonitorObject disposed</exception>
        /// <exception cref="OperationCanceledException">Cancellation requested</exception>
        public MonitorWaiter Enter(int timeout, CancellationToken token)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(MonitorObject));
            if (token.IsCancellationRequested)
                throw new OperationCanceledException(token);

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
                    Monitor.Enter(this); // Can be interrupted
                }
                catch
                {
                    cancellationTokenRegistration.Dispose();
                    throw;
                }
            }
            else
            {
                Monitor.Enter(this);
            }

            Interlocked.Increment(ref _waiterCount);
            return new MonitorWaiter(this, timeout, startTime, token, cancellationTokenRegistration);
        }
        /// <summary>
        /// Enter the lock on the current <see cref="MonitorObject"/> object
        /// </summary>
        /// <param name="timeout">Total operation timeout</param>
        /// <returns>Lock guard to work with 'using' statement</returns>
        /// <exception cref="ObjectDisposedException">MonitorObject disposed</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MonitorWaiter Enter(int timeout)
        {
            return Enter(timeout, default(CancellationToken));
        }
        /// <summary>
        /// Enter the lock on the current <see cref="MonitorObject"/> object
        /// </summary>
        /// <param name="token">Cancellation token</param>
        /// <returns>Lock guard to work with 'using' statement</returns>
        /// <exception cref="ObjectDisposedException">MonitorObject disposed</exception>
        /// <exception cref="OperationCanceledException">Cancellation requested</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MonitorWaiter Enter(CancellationToken token)
        {
            return Enter(Timeout.Infinite, default(CancellationToken));
        }
        /// <summary>
        /// Enter the lock on the current <see cref="MonitorObject"/> object
        /// </summary>
        /// <returns>Lock guard to work with 'using' statement</returns>
        /// <exception cref="ObjectDisposedException">MonitorObject disposed</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MonitorWaiter Enter()
        {
            return Enter(Timeout.Infinite, default(CancellationToken));
        }

        /// <summary>
        /// Leaves the lock section for current <see cref="MonitorObject"/> object.
        /// Should be called from <see cref="MonitorWaiter.Dispose"/>
        /// </summary>
        /// <param name="mwLock">MonitorWaiter with required info</param>
        internal void Exit(ref MonitorWaiter mwLock)
        {
            Interlocked.Decrement(ref _waiterCount);
            Monitor.Exit(this);
        }


        /// <summary>
        /// Blocks the current thread until the next notification
        /// </summary>
        /// <param name="timeout">Tiemout in milliseconds</param>
        /// <returns>True if the current thread successfully received a notification</returns>
        /// <exception cref="SynchronizationLockException">externalLock is not acquired or acquired recursively</exception>
        /// <exception cref="ObjectDisposedException">ConditionVariable was disposed</exception>
        /// <exception cref="OperationInterruptedException">Waiting was interrupted by Dispose</exception>
        internal bool Wait(int timeout)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(MonitorObject));

            Debug.Assert(Monitor.IsEntered(this), "External lock should be acquired");

            if (timeout < -1)
                timeout = Timeout.Infinite;

            try
            {
                Interlocked.Increment(ref _waiterCount);

                // Waiting for signal
                if (!Monitor.Wait(this, timeout))
                    return false;

                if (_isDisposed)
                    throw new OperationInterruptedException("Wait was interrupted by Dispose", new ObjectDisposedException(nameof(MonitorObject)));
            }
            finally
            {
                Interlocked.Decrement(ref _waiterCount);
            }

            return true;
        }


        /// <summary>
        /// Notifies single waiting thread about possible state change
        /// </summary>
        /// <exception cref="SynchronizationLockException">External lock is not acquired</exception>
        public void Pulse()
        {
            lock (this)
            {
                Monitor.Pulse(this);
            }
        }

        /// <summary>
        /// Notifies specified number of threads about possible state change
        /// </summary>
        /// <param name="count">Number of threads to be notified</param>
        /// <exception cref="ArgumentOutOfRangeException">Incorrect count</exception>
        /// <exception cref="SynchronizationLockException">External lock is not acquired</exception>
        public void Pulse(int count)
        {
            if (count < 0 || count > short.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(count));

            lock (this)
            {
                for (int i = 0; i < count; i++)
                    Monitor.Pulse(this);
            }
        }

        /// <summary>
        /// Notifies all waiting threads about possible state change
        /// </summary>
        /// <exception cref="SynchronizationLockException">External lock is not acquired</exception>
        public void PulseAll()
        {
            lock (this)
            {
                Monitor.PulseAll(this);
            }
        }

        /// <summary>
        /// Cleans-up resources
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;

            lock (this)
            {
                if (_isDisposed)
                    return;

                _isDisposed = true;

                Monitor.PulseAll(this);
            }
        }
    }
}
