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
    /// Synchronization primitive that blocks the current thread until a specified condition occurs
    /// </summary>
    internal class ConditionVariableOld: IDisposable
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


        // ========================


        private static readonly Action<object> _cancellationTokenCanceledEventHandler = new Action<object>(CancellationTokenCanceledEventHandler);
        /// <summary>
        /// Cancellation handler for CancellationToken
        /// </summary>
        /// <param name="obj">ConditionVariable object</param>
        private static void CancellationTokenCanceledEventHandler(object obj)
        {
            ConditionVariableOld conditionVar = obj as ConditionVariableOld;
            TurboContract.Assert(conditionVar != null, conditionString: "conditionVar != null");
            lock (conditionVar._internalLock)
            {
                Monitor.PulseAll(conditionVar._internalLock);
            }
        }

        // =============

        private volatile int _waitCount;
        private volatile bool _isDisposed;
        private readonly object _internalLock;
        private readonly object _externalLock;

        /// <summary>
        /// ConditionVariable constructor
        /// </summary>
        /// <param name="externalLock">External lock object that should be aqcuired before entering the ConditionVariable</param>
        public ConditionVariableOld(object externalLock)
        {
            if (externalLock == null)
                throw new ArgumentNullException(nameof(externalLock));

            _waitCount = 0;
            _internalLock = new object();
            _externalLock = externalLock;
        }

        /// <summary>
        /// The number of waiting threads on the ConditionVariable
        /// </summary>
        public int WaiterCount { get { return _waitCount; } }



        /// <summary>
        /// Blocks the current thread until the next notification
        /// </summary>
        /// <param name="timeout">Tiemout in milliseconds</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>True if the current thread successfully received a notification</returns>
        /// <exception cref="SynchronizationLockException">externalLock is not acquired or acquired recursively</exception>
        /// <exception cref="ObjectDisposedException">ConditionVariable was disposed</exception>
        /// <exception cref="OperationCanceledException">Cancellation happened</exception>
        /// <exception cref="OperationInterruptedException">Waiting was interrupted by Dispose</exception>
        public bool Wait(int timeout, CancellationToken token)
        {
            if (!Monitor.IsEntered(_externalLock))
                throw new SynchronizationLockException("External lock should be acquired");
            if (_isDisposed)
                throw new ObjectDisposedException(this.GetType().Name);
            if (token.IsCancellationRequested)
                throw new OperationCanceledException(token);

            uint startTime = 0;
            if (timeout > 0)
                startTime = TimeoutHelper.GetTimestamp();
            else if (timeout < 0)
                timeout = Timeout.Infinite;


            CancellationTokenRegistration cancellationTokenRegistration = default(CancellationTokenRegistration);
            bool internalLockTaken = false;
            bool externalLockTaken = true;
            try
            {
                if (token.CanBeCanceled)
                    cancellationTokenRegistration = CancellationTokenHelper.RegisterWithoutECIfPossible(token, _cancellationTokenCanceledEventHandler, this);

                try { }
                finally
                {
                    Monitor.Enter(_internalLock, ref internalLockTaken);
                    TurboContract.Assert(internalLockTaken, conditionString: "internalLockTaken");
                    _waitCount++;
                }

                // Check if cancelled or disposed after entering the lock
                if (token.IsCancellationRequested)
                    throw new OperationCanceledException(token);
                if (_isDisposed)
                    throw new OperationInterruptedException("Wait was interrupted by Dispose", new ObjectDisposedException(this.GetType().Name));

                // Calculate remaining timeout
                int remainingWaitMilliseconds = Timeout.Infinite;
                if (timeout != Timeout.Infinite)
                {
                    remainingWaitMilliseconds = TimeoutHelper.UpdateTimeout(startTime, timeout);
                    if (remainingWaitMilliseconds <= 0)
                        return false;
                }

                // Exit external lock right before Wait
                ExitLock(_externalLock, ref externalLockTaken);

                if (Monitor.IsEntered(_externalLock)) // Sanity check
                    throw new SynchronizationLockException("Recursive lock is not supported");

                // Waiting for signal
                if (!Monitor.Wait(_internalLock, remainingWaitMilliseconds))
                    return false;

                // Check if cancellation or dispose was the reasons of the signal
                if (token.IsCancellationRequested)
                    throw new OperationCanceledException(token);
                if (_isDisposed)
                    throw new OperationInterruptedException("Wait was interrupted by Dispose", new ObjectDisposedException(this.GetType().Name));
            }
            finally
            {
                if (internalLockTaken)
                {
                    _waitCount--;
                    TurboContract.Assert(_waitCount >= 0, conditionString: "_waitCount >= 0");
                    Monitor.Exit(_internalLock);
                }

                EnterLock(_externalLock, ref externalLockTaken);

                cancellationTokenRegistration.Dispose();
            }

            return true;
        }
        /// <summary>
        /// Blocks the current thread until the next notification
        /// </summary>
        /// <param name="timeout">Tiemout in milliseconds</param>
        /// <returns>True if the current thread successfully received a notification</returns>
        /// <exception cref="SynchronizationLockException">externalLock is not acquired or acquired recursively</exception>
        /// <exception cref="ObjectDisposedException">ConditionVariable was disposed</exception>
        /// <exception cref="OperationInterruptedException">Waiting was interrupted by Dispose</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Wait(int timeout)
        {
            return Wait(timeout, default(CancellationToken));
        }
        /// <summary>
        /// Blocks the current thread until the next notification
        /// </summary>
        /// <param name="timeout">Tiemout value</param>
        /// <returns>True if the current thread successfully received a notification</returns>
        /// <exception cref="SynchronizationLockException">externalLock is not acquired or acquired recursively</exception>
        /// <exception cref="ObjectDisposedException">ConditionVariable was disposed</exception>
        /// <exception cref="OperationInterruptedException">Waiting was interrupted by Dispose</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Wait(TimeSpan timeout)
        {
            long timeoutMs = (long)timeout.TotalMilliseconds;
            if (timeoutMs > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(timeout));

            return Wait((int)timeoutMs, default(CancellationToken));
        }
        /// <summary>
        /// Blocks the current thread until the next notification
        /// </summary>
        /// <param name="token">Cancellation token</param>
        /// <returns>True if the current thread successfully received a notification</returns>
        /// <exception cref="SynchronizationLockException">externalLock is not acquired or acquired recursively</exception>
        /// <exception cref="ObjectDisposedException">ConditionVariable was disposed</exception>
        /// <exception cref="OperationCanceledException">Cancellation happened</exception>
        /// <exception cref="OperationInterruptedException">Waiting was interrupted by Dispose</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Wait(CancellationToken token)
        {
            return Wait(Timeout.Infinite, token);
        }




        private bool WaitUntilPredicate<TState>(ref bool internalLockTaken, ref bool externalLockTaken, WaitPredicate<TState> predicate, TState state, uint startTime, int timeout, CancellationToken token)
        {
            TurboContract.Requires(internalLockTaken, "internalLockTaken == false");
            TurboContract.Requires(externalLockTaken, "externalLockTaken == false");
            TurboContract.Requires(predicate != null, conditionString: "predicate != null");

            int remainingWaitMilliseconds = Timeout.Infinite;
            bool recursiveLockChecked = false;

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

                ExitLock(_externalLock, ref externalLockTaken);

                if (!recursiveLockChecked && Monitor.IsEntered(_externalLock)) // Sanity check
                    throw new SynchronizationLockException("Recursive lock is not supported");
                recursiveLockChecked = true;

                if (!Monitor.Wait(_internalLock, remainingWaitMilliseconds))
                    return false;

                try
                {
                    ExitLock(_internalLock, ref internalLockTaken);
                    EnterLock(_externalLock, ref externalLockTaken);

                    if (predicate.Invoke(state))
                        return true;
                }
                finally
                {
                    EnterLock(_internalLock, ref internalLockTaken);
                }
            }
        }

        /// <summary>
        /// Slow path
        /// </summary>
        private bool WaitSlowPath<TState>(WaitPredicate<TState> predicate, TState state, uint startTime, int timeout, CancellationToken token)
        {
            if (token.IsCancellationRequested)
                throw new OperationCanceledException(token);

            if (timeout < 0)
                timeout = Timeout.Infinite;

            if (timeout > 0 && TimeoutHelper.UpdateTimeout(startTime, timeout) <= 0) // Predicate estimation took too much time
                return false;

            CancellationTokenRegistration cancellationTokenRegistration = default(CancellationTokenRegistration);
            bool internalLockTaken = false;
            bool externalLockTaken = true;
            try
            {
                if (token.CanBeCanceled)
                    cancellationTokenRegistration = CancellationTokenHelper.RegisterWithoutECIfPossible(token, _cancellationTokenCanceledEventHandler, this);

                try { }
                finally
                {
                    Monitor.Enter(_internalLock, ref internalLockTaken);
                    TurboContract.Assert(internalLockTaken, conditionString: "internalLockTaken");
                    _waitCount++;
                }

                if (WaitUntilPredicate(ref internalLockTaken, ref externalLockTaken, predicate, state, startTime, timeout, token))
                    return true;

                if (token.IsCancellationRequested)
                    throw new OperationCanceledException(token);

                if (_isDisposed)
                    throw new OperationInterruptedException("Wait was interrupted by Dispose", new ObjectDisposedException(this.GetType().Name));
            }
            finally
            {
                if (internalLockTaken)
                {
                    _waitCount--;
                    TurboContract.Assert(_waitCount >= 0, conditionString: "_waitCount >= 0");
                    Monitor.Exit(_internalLock);
                }

                EnterLock(_externalLock, ref externalLockTaken);

                cancellationTokenRegistration.Dispose();
            }

            // Final check for predicate
            return predicate(state);
        }


        /// <summary>
        /// Blocks the current thread until predicate estimates as True
        /// </summary>
        /// <typeparam name="TState">Type of the state object</typeparam>
        /// <param name="predicate">Predicate that should return True to complete waiting</param>
        /// <param name="state">State object for the predicate</param>
        /// <param name="timeout">Tiemout in milliseconds</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>True if predicate evaluates to True</returns>
        /// <exception cref="ArgumentNullException">predicate is null</exception>
        /// <exception cref="SynchronizationLockException">externalLock is not acquired or acquired recursively</exception>
        /// <exception cref="ObjectDisposedException">ConditionVariable was disposed</exception>
        /// <exception cref="OperationCanceledException">Cancellation happened</exception>
        /// <exception cref="OperationInterruptedException">Waiting was interrupted by Dispose</exception>
        public bool Wait<TState>(WaitPredicate<TState> predicate, TState state, int timeout, CancellationToken token)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));
            if (!Monitor.IsEntered(_externalLock))
                throw new SynchronizationLockException("External lock should be acquired");
            if (_isDisposed)
                throw new ObjectDisposedException(this.GetType().Name);
            if (token.IsCancellationRequested)
                throw new OperationCanceledException(token);

            uint startTime = 0;
            if (timeout > 0)
                startTime = TimeoutHelper.GetTimestamp();

            if (predicate(state))
                return true;

            if (timeout == 0)
                return false;

            return WaitSlowPath(predicate, state, startTime, timeout, token);
        }
        /// <summary>
        /// Blocks the current thread until predicate estimates as True
        /// </summary>
        /// <typeparam name="TState">Type of the state object</typeparam>
        /// <param name="predicate">Predicate that should return True to complete waiting</param>
        /// <param name="state">State object for the predicate</param>
        /// <returns>True if predicate evaluates to True</returns>
        /// <exception cref="ArgumentNullException">predicate is null</exception>
        /// <exception cref="SynchronizationLockException">externalLock is not acquired or acquired recursively</exception>
        /// <exception cref="ObjectDisposedException">ConditionVariable was disposed</exception>
        /// <exception cref="OperationInterruptedException">Waiting was interrupted by Dispose</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Wait<TState>(WaitPredicate<TState> predicate, TState state)
        {
            return Wait(predicate, state, Timeout.Infinite, default(CancellationToken));
        }
        /// <summary>
        /// Blocks the current thread until predicate estimates as True
        /// </summary>
        /// <typeparam name="TState">Type of the state object</typeparam>
        /// <param name="predicate">Predicate that should return True to complete waiting</param>
        /// <param name="state">State object for the predicate</param>
        /// <param name="timeout">Tiemout in milliseconds</param>
        /// <returns>True if predicate evaluates to True</returns>
        /// <exception cref="ArgumentNullException">predicate is null</exception>
        /// <exception cref="SynchronizationLockException">externalLock is not acquired or acquired recursively</exception>
        /// <exception cref="ObjectDisposedException">ConditionVariable was disposed</exception>
        /// <exception cref="OperationInterruptedException">Waiting was interrupted by Dispose</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Wait<TState>(WaitPredicate<TState> predicate, TState state, int timeout)
        {
            return Wait(predicate, state, timeout, default(CancellationToken));
        }
        /// <summary>
        /// Blocks the current thread until predicate estimates as True
        /// </summary>
        /// <typeparam name="TState">Type of the state object</typeparam>
        /// <param name="predicate">Predicate that should return True to complete waiting</param>
        /// <param name="state">State object for the predicate</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>True if predicate evaluates to True</returns>
        /// <exception cref="ArgumentNullException">predicate is null</exception>
        /// <exception cref="SynchronizationLockException">externalLock is not acquired or acquired recursively</exception>
        /// <exception cref="ObjectDisposedException">ConditionVariable was disposed</exception>
        /// <exception cref="OperationCanceledException">Cancellation happened</exception>
        /// <exception cref="OperationInterruptedException">Waiting was interrupted by Dispose</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Wait<TState>(WaitPredicate<TState> predicate, TState state, CancellationToken token)
        {
            return Wait(predicate, state, Timeout.Infinite, token);
        }
        /// <summary>
        /// Blocks the current thread until predicate estimates as True
        /// </summary>
        /// <typeparam name="TState">Type of the state object</typeparam>
        /// <param name="predicate">Predicate that should return True to complete waiting</param>
        /// <param name="state">State object for the predicate</param>
        /// <param name="timeout">Tiemout value</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>True if predicate evaluates to True</returns>
        /// <exception cref="ArgumentNullException">predicate is null</exception>
        /// <exception cref="SynchronizationLockException">externalLock is not acquired or acquired recursively</exception>
        /// <exception cref="ObjectDisposedException">ConditionVariable was disposed</exception>
        /// <exception cref="OperationCanceledException">Cancellation happened</exception>
        /// <exception cref="OperationInterruptedException">Waiting was interrupted by Dispose</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Wait<TState>(WaitPredicate<TState> predicate, TState state, TimeSpan timeout, CancellationToken token)
        {
            long timeoutMs = (long)timeout.TotalMilliseconds;
            if (timeoutMs > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(timeout));

            return Wait(predicate, state, (int)timeoutMs, token);
        }
        /// <summary>
        /// Blocks the current thread until predicate estimates as True
        /// </summary>
        /// <typeparam name="TState">Type of the state object</typeparam>
        /// <param name="predicate">Predicate that should return True to complete waiting</param>
        /// <param name="state">State object for the predicate</param>
        /// <param name="timeout">Tiemout value</param>
        /// <returns>True if predicate evaluates to True</returns>
        /// <exception cref="ArgumentNullException">predicate is null</exception>
        /// <exception cref="SynchronizationLockException">externalLock is not acquired or acquired recursively</exception>
        /// <exception cref="ObjectDisposedException">ConditionVariable was disposed</exception>
        /// <exception cref="OperationInterruptedException">Waiting was interrupted by Dispose</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Wait<TState>(WaitPredicate<TState> predicate, TState state, TimeSpan timeout)
        {
            long timeoutMs = (long)timeout.TotalMilliseconds;
            if (timeoutMs > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(timeout));

            return Wait(predicate, state, (int)timeoutMs, default(CancellationToken));
        }



        /// <summary>
        /// Notifies single waiting thread about possible state change
        /// </summary>
        /// <exception cref="SynchronizationLockException">External lock is not acquired</exception>
        public void Pulse()
        {
            if (!Monitor.IsEntered(_externalLock))
                throw new SynchronizationLockException("External lock should be acquired");

            lock (_internalLock)
            {
                Monitor.Pulse(_internalLock);
            }
        }

        /// <summary>
        /// Notifies specified number of threads about possible state change
        /// </summary>
        /// <param name="count">Number of threads to be notified</param>
        /// <exception cref="SynchronizationLockException">External lock is not acquired</exception>
        internal void Pulse(int count)
        {
            if (count < 0 || count > short.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (!Monitor.IsEntered(_externalLock))
                throw new SynchronizationLockException("External lock should be acquired");

            lock (_internalLock)
            {
                for (int i = 0; i < count; i++)
                    Monitor.Pulse(_internalLock);
            }
        }

        /// <summary>
        /// Notifies all waiting threads about possible state change
        /// </summary>
        /// <exception cref="SynchronizationLockException">External lock is not acquired</exception>
        public void PulseAll()
        {
            if (!Monitor.IsEntered(_externalLock))
                throw new SynchronizationLockException("External lock should be acquired");

            lock (_internalLock)
            {
                Monitor.PulseAll(_internalLock);
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
