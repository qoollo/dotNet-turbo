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
    public class ConditionVariable: IDisposable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void EnterLock(object syncObj, ref bool lockTaken)
        {
            if (!lockTaken)
            {
                Monitor.Enter(syncObj, ref lockTaken);
                Debug.Assert(lockTaken);
            }
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
            ConditionVariable conditionVar = obj as ConditionVariable;
            Debug.Assert(conditionVar != null);
            lock (conditionVar._lockObj)
            {
                Monitor.PulseAll(conditionVar._lockObj);
            }
        }

        // =============

        private volatile int _waitCount;
        private volatile bool _isDisposed;
        private object _lockObj;

        /// <summary>
        /// ConditionVariable constructor
        /// </summary>
        public ConditionVariable()
        {
            _waitCount = 0;
            _lockObj = new object();
        }

        /// <summary>
        /// The number of waiting threads on the ConditionVariable
        /// </summary>
        public int WaiterCount { get { return _waitCount; } }



        /// <summary>
        /// Blocks the current thread until the next notification
        /// </summary>
        /// <param name="externalLock">External lock object that should be aqcuired before entering the ConditionVariable</param>
        /// <param name="timeout">Tiemout in milliseconds</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>True if the current thread successfully received a notification</returns>
        /// <exception cref="ArgumentNullException">externalLock is null</exception>
        /// <exception cref="ArgumentException">externalLock is not acquired or acquired recursively</exception>
        /// <exception cref="ObjectDisposedException">ConditionVariable was disposed</exception>
        /// <exception cref="OperationCanceledException">Cancellation happened</exception>
        /// <exception cref="OperationInterruptedException">Waiting was interrupted by Dispose</exception>
        public bool Wait(object externalLock, int timeout, CancellationToken token)
        {
            if (externalLock == null)
                throw new ArgumentNullException(nameof(externalLock));
            if (!Monitor.IsEntered(externalLock))
                throw new ArgumentException("External lock should be acquired", nameof(externalLock));
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
                    cancellationTokenRegistration = CancellationTokenHelper.RegisterWithoutEC(token, _cancellationTokenCanceledEventHandler, this);

                try { }
                finally
                {
                    Monitor.Enter(_lockObj, ref internalLockTaken);
                    Debug.Assert(internalLockTaken);
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
                try { }
                finally
                {
                    Monitor.Exit(externalLock);
                    externalLockTaken = false;
                }

                if (Monitor.IsEntered(externalLock)) // Sanity check
                    throw new ArgumentException("Recursive lock is not supported", nameof(externalLock));

                // Waiting for signal
                if (!Monitor.Wait(_lockObj, remainingWaitMilliseconds))
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
                    Debug.Assert(_waitCount >= 0);
                    Monitor.Exit(_lockObj);
                }
                if (!externalLockTaken)
                {
                    Monitor.Enter(externalLock, ref externalLockTaken);
                    Debug.Assert(externalLockTaken);
                }

                cancellationTokenRegistration.Dispose();
            }

            return true;
        }
        /// <summary>
        /// Blocks the current thread until the next notification
        /// </summary>
        /// <param name="externalLock">External lock object that should be aqcuired before entering the ConditionVariable</param>
        /// <param name="timeout">Tiemout in milliseconds</param>
        /// <returns>True if the current thread successfully received a notification</returns>
        /// <exception cref="ArgumentNullException">externalLock is null</exception>
        /// <exception cref="ArgumentException">externalLock is not acquired or acquired recursively</exception>
        /// <exception cref="ObjectDisposedException">ConditionVariable was disposed</exception>
        /// <exception cref="OperationInterruptedException">Waiting was interrupted by Dispose</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Wait(object externalLock, int timeout)
        {
            return Wait(externalLock, timeout, default(CancellationToken));
        }
        /// <summary>
        /// Blocks the current thread until the next notification
        /// </summary>
        /// <param name="externalLock">External lock object that should be aqcuired before entering the ConditionVariable</param>
        /// <param name="timeout">Tiemout value</param>
        /// <returns>True if the current thread successfully received a notification</returns>
        /// <exception cref="ArgumentNullException">externalLock is null</exception>
        /// <exception cref="ArgumentException">externalLock is not acquired or acquired recursively</exception>
        /// <exception cref="ObjectDisposedException">ConditionVariable was disposed</exception>
        /// <exception cref="OperationInterruptedException">Waiting was interrupted by Dispose</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Wait(object externalLock, TimeSpan timeout)
        {
            long timeoutMs = (long)timeout.TotalMilliseconds;
            if (timeoutMs > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(timeout));

            return Wait(externalLock, (int)timeoutMs, default(CancellationToken));
        }
        /// <summary>
        /// Blocks the current thread until the next notification
        /// </summary>
        /// <param name="externalLock">External lock object that should be aqcuired before entering the ConditionVariable</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>True if the current thread successfully received a notification</returns>
        /// <exception cref="ArgumentNullException">externalLock is null</exception>
        /// <exception cref="ArgumentException">externalLock is not acquired or acquired recursively</exception>
        /// <exception cref="ObjectDisposedException">ConditionVariable was disposed</exception>
        /// <exception cref="OperationCanceledException">Cancellation happened</exception>
        /// <exception cref="OperationInterruptedException">Waiting was interrupted by Dispose</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Wait(object externalLock, CancellationToken token)
        {
            return Wait(externalLock, Timeout.Infinite, token);
        }




        private bool WaitUntilPredicate<TState>(object externalLock, ref bool internalLockTaken, ref bool externalLockTaken, WaitPredicate<TState> predicate, TState state, uint startTime, int timeout, CancellationToken token)
        {
            Debug.Assert(internalLockTaken);
            Debug.Assert(externalLockTaken);

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

                ExitLock(externalLock, ref externalLockTaken);

                if (!recursiveLockChecked && Monitor.IsEntered(externalLock)) // Sanity check
                    throw new ArgumentException("Recursive lock is not supported", nameof(externalLock));
                recursiveLockChecked = true;

                if (!Monitor.Wait(_lockObj, remainingWaitMilliseconds))
                    return false;

                try
                {
                    ExitLock(_lockObj, ref internalLockTaken);
                    EnterLock(externalLock, ref externalLockTaken);

                    if (predicate.Invoke(state))
                        return true;
                }
                finally
                {
                    EnterLock(_lockObj, ref internalLockTaken);
                }
            }
        }

        /// <summary>
        /// Slow path
        /// </summary>
        private bool WaitSlowPath<TState>(object externalLock, WaitPredicate<TState> predicate, TState state, uint startTime, int timeout, CancellationToken token)
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
                    cancellationTokenRegistration = CancellationTokenHelper.RegisterWithoutEC(token, _cancellationTokenCanceledEventHandler, this);

                try { }
                finally
                {
                    Monitor.Enter(_lockObj, ref internalLockTaken);
                    Debug.Assert(internalLockTaken);
                    _waitCount++;
                }

                if (WaitUntilPredicate(externalLock, ref internalLockTaken, ref externalLockTaken, predicate, state, startTime, timeout, token))
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
                    Debug.Assert(_waitCount >= 0);
                    Monitor.Exit(_lockObj);
                }
                if (!externalLockTaken)
                {
                    Monitor.Enter(externalLock, ref externalLockTaken);
                    Debug.Assert(externalLockTaken);
                }

                cancellationTokenRegistration.Dispose();
            }

            // Final check for predicate
            return predicate(state);
        }


        /// <summary>
        /// Blocks the current thread until predicate estimates as True
        /// </summary>
        /// <typeparam name="TState">Type of the state object</typeparam>
        /// <param name="externalLock">External lock object that should be aqcuired before entering the ConditionVariable</param>
        /// <param name="predicate">Predicate that should return True to complete waiting</param>
        /// <param name="state">State object for the predicate</param>
        /// <param name="timeout">Tiemout in milliseconds</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>True if predicate evaluates to True</returns>
        /// <exception cref="ArgumentNullException">externalLock or predicate is null</exception>
        /// <exception cref="ArgumentException">externalLock is not acquired or acquired recursively</exception>
        /// <exception cref="ObjectDisposedException">ConditionVariable was disposed</exception>
        /// <exception cref="OperationCanceledException">Cancellation happened</exception>
        /// <exception cref="OperationInterruptedException">Waiting was interrupted by Dispose</exception>
        public bool Wait<TState>(object externalLock, WaitPredicate<TState> predicate, TState state, int timeout, CancellationToken token)
        {
            if (externalLock == null)
                throw new ArgumentNullException(nameof(externalLock));
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));
            if (!Monitor.IsEntered(externalLock))
                throw new ArgumentException("External lock should be acquired", nameof(externalLock));
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

            return WaitSlowPath(externalLock, predicate, state, startTime, timeout, token);
        }
        /// <summary>
        /// Blocks the current thread until predicate estimates as True
        /// </summary>
        /// <typeparam name="TState">Type of the state object</typeparam>
        /// <param name="externalLock">External lock object that should be aqcuired before entering the ConditionVariable</param>
        /// <param name="predicate">Predicate that should return True to complete waiting</param>
        /// <param name="state">State object for the predicate</param>
        /// <returns>True if predicate evaluates to True</returns>
        /// <exception cref="ArgumentNullException">externalLock or predicate is null</exception>
        /// <exception cref="ArgumentException">externalLock is not acquired or acquired recursively</exception>
        /// <exception cref="ObjectDisposedException">ConditionVariable was disposed</exception>
        /// <exception cref="OperationInterruptedException">Waiting was interrupted by Dispose</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Wait<TState>(object externalLock, WaitPredicate<TState> predicate, TState state)
        {
            return Wait(externalLock, predicate, state, Timeout.Infinite, default(CancellationToken));
        }
        /// <summary>
        /// Blocks the current thread until predicate estimates as True
        /// </summary>
        /// <typeparam name="TState">Type of the state object</typeparam>
        /// <param name="externalLock">External lock object that should be aqcuired before entering the ConditionVariable</param>
        /// <param name="predicate">Predicate that should return True to complete waiting</param>
        /// <param name="state">State object for the predicate</param>
        /// <param name="timeout">Tiemout in milliseconds</param>
        /// <returns>True if predicate evaluates to True</returns>
        /// <exception cref="ArgumentNullException">externalLock or predicate is null</exception>
        /// <exception cref="ArgumentException">externalLock is not acquired or acquired recursively</exception>
        /// <exception cref="ObjectDisposedException">ConditionVariable was disposed</exception>
        /// <exception cref="OperationInterruptedException">Waiting was interrupted by Dispose</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Wait<TState>(object externalLock, WaitPredicate<TState> predicate, TState state, int timeout)
        {
            return Wait(externalLock, predicate, state, timeout, default(CancellationToken));
        }
        /// <summary>
        /// Blocks the current thread until predicate estimates as True
        /// </summary>
        /// <typeparam name="TState">Type of the state object</typeparam>
        /// <param name="externalLock">External lock object that should be aqcuired before entering the ConditionVariable</param>
        /// <param name="predicate">Predicate that should return True to complete waiting</param>
        /// <param name="state">State object for the predicate</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>True if predicate evaluates to True</returns>
        /// <exception cref="ArgumentNullException">externalLock or predicate is null</exception>
        /// <exception cref="ArgumentException">externalLock is not acquired or acquired recursively</exception>
        /// <exception cref="ObjectDisposedException">ConditionVariable was disposed</exception>
        /// <exception cref="OperationCanceledException">Cancellation happened</exception>
        /// <exception cref="OperationInterruptedException">Waiting was interrupted by Dispose</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Wait<TState>(object externalLock, WaitPredicate<TState> predicate, TState state, CancellationToken token)
        {
            return Wait(externalLock, predicate, state, Timeout.Infinite, token);
        }
        /// <summary>
        /// Blocks the current thread until predicate estimates as True
        /// </summary>
        /// <typeparam name="TState">Type of the state object</typeparam>
        /// <param name="externalLock">External lock object that should be aqcuired before entering the ConditionVariable</param>
        /// <param name="predicate">Predicate that should return True to complete waiting</param>
        /// <param name="state">State object for the predicate</param>
        /// <param name="timeout">Tiemout value</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>True if predicate evaluates to True</returns>
        /// <exception cref="ArgumentNullException">externalLock or predicate is null</exception>
        /// <exception cref="ArgumentException">externalLock is not acquired or acquired recursively</exception>
        /// <exception cref="ObjectDisposedException">ConditionVariable was disposed</exception>
        /// <exception cref="OperationCanceledException">Cancellation happened</exception>
        /// <exception cref="OperationInterruptedException">Waiting was interrupted by Dispose</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Wait<TState>(object externalLock, WaitPredicate<TState> predicate, TState state, TimeSpan timeout, CancellationToken token)
        {
            long timeoutMs = (long)timeout.TotalMilliseconds;
            if (timeoutMs > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(timeout));

            return Wait(externalLock, predicate, state, (int)timeoutMs, token);
        }
        /// <summary>
        /// Blocks the current thread until predicate estimates as True
        /// </summary>
        /// <typeparam name="TState">Type of the state object</typeparam>
        /// <param name="externalLock">External lock object that should be aqcuired before entering the ConditionVariable</param>
        /// <param name="predicate">Predicate that should return True to complete waiting</param>
        /// <param name="state">State object for the predicate</param>
        /// <param name="timeout">Tiemout value</param>
        /// <returns>True if predicate evaluates to True</returns>
        /// <exception cref="ArgumentNullException">externalLock or predicate is null</exception>
        /// <exception cref="ArgumentException">externalLock is not acquired or acquired recursively</exception>
        /// <exception cref="ObjectDisposedException">ConditionVariable was disposed</exception>
        /// <exception cref="OperationInterruptedException">Waiting was interrupted by Dispose</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Wait<TState>(object externalLock, WaitPredicate<TState> predicate, TState state, TimeSpan timeout)
        {
            long timeoutMs = (long)timeout.TotalMilliseconds;
            if (timeoutMs > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(timeout));

            return Wait(externalLock, predicate, state, (int)timeoutMs, default(CancellationToken));
        }



        /// <summary>
        /// Notifies single waiting thread about possible state change
        /// </summary>
        public void Signal()
        {
            lock (_lockObj)
            {
                Monitor.Pulse(_lockObj);
            }
        }

        /// <summary>
        /// Notifies all waiting threads about possible state change
        /// </summary>
        public void SignalAll()
        {
            lock (_lockObj)
            {
                Monitor.PulseAll(_lockObj);
            }
        }

        /// <summary>
        /// Cleans-up resources
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
