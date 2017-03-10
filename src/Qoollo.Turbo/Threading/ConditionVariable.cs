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
        /// <summary>
        /// Combination of all predicates (one for every overload)
        /// </summary>
        /// <typeparam name="TState">Type of the state variable</typeparam>
        private struct WaitPredicateCombination<TState>
        {
            private readonly WaitPredicate SimplePredicate;
            private readonly WaitPredicate<TState> PredicateWithState;
            private readonly WaitPredicateRef<TState> PredicateWithRefState;

            public WaitPredicateCombination(WaitPredicate simplePredicate)
            {
                SimplePredicate = simplePredicate;
                PredicateWithState = null;
                PredicateWithRefState = null;
            }
            public WaitPredicateCombination(WaitPredicate<TState> predicateWithState)
            {
                SimplePredicate = null;
                PredicateWithState = predicateWithState;
                PredicateWithRefState = null;
            }
            public WaitPredicateCombination(WaitPredicateRef<TState> predicateWithRefState)
            {
                SimplePredicate = null;
                PredicateWithState = null;
                PredicateWithRefState = predicateWithRefState;
            }

            /// <summary>
            /// Indicates that no predicate was passed
            /// </summary>
            public bool IsEmpty { get { return SimplePredicate == null && PredicateWithState == null && PredicateWithRefState == null; } }

            /// <summary>
            /// Invokes the correct predicate
            /// </summary>
            /// <param name="state">State object</param>
            /// <returns>Predicate result</returns>
            public bool Invoke(ref TState state)
            {
                if (SimplePredicate != null)
                    return SimplePredicate();
                else if (PredicateWithState != null)
                    return PredicateWithState(state);
                else if (PredicateWithRefState != null)
                    return PredicateWithRefState(ref state);

                return true;
            }
        }

        // =============

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
        /// ConditionVariable constructor
        /// </summary>
        /// <param name="externalSyncObject">User synchronization object</param>
        internal ConditionVariable(object externalSyncObject)
        {
            if (externalSyncObject == null)
                throw new ArgumentNullException(nameof(externalSyncObject));
            _waitCount = 0;
            _lockObj = externalSyncObject;
        }

        /// <summary>
        /// The number of waiting threads on the ConditionVariable
        /// </summary>
        public int WaiterCount { get { return _waitCount; } }



        private bool WaitUntilPredicateOrTimeout<TState>(WaitPredicateCombination<TState> predicate, ref TState state, uint startTime, int timeout, CancellationToken token)
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

                if (predicate.Invoke(ref state))
                    return true;
            }
        }

        /// <summary>
        /// Common slow path
        /// </summary>
        private bool WaitSlowPath<TState>(WaitPredicateCombination<TState> predicate, ref TState state, uint startTime, int timeout, CancellationToken token)
        {
            if (token.IsCancellationRequested)
                throw new OperationCanceledException(token);

            if (timeout < 0)
                timeout = Timeout.Infinite;

            if (timeout > 0 && TimeoutHelper.UpdateTimeout(startTime, timeout) <= 0) // Predicate estimation took too much time
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

                if (!predicate.IsEmpty && predicate.Invoke(ref state))
                    return true;

                if (timeout == 0)
                    return false;
                else if (timeout > 0 && TimeoutHelper.UpdateTimeout(startTime, timeout) <= 0) // Predicate estimation took too much time
                    return false;

                if (WaitUntilPredicateOrTimeout(predicate, ref state, startTime, timeout, token))
                    return true;

                if (token.IsCancellationRequested)
                    throw new OperationCanceledException(token);

                if (_isDisposed)
                    throw new OperationInterruptedException("Wait was interrupted by Dispose", new ObjectDisposedException(this.GetType().Name));
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

            // Invoke at the end
            return predicate.Invoke(ref state);
        }



        /// <summary>
        /// Blocks the current thread until the next notification
        /// </summary>
        /// <param name="timeout">Tiemout in milliseconds</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>True if the current thread successfully received a notification</returns>
        /// <exception cref="ObjectDisposedException">ConditionVariable was disposed</exception>
        /// <exception cref="OperationCanceledException">Cancellation happened</exception>
        /// <exception cref="OperationInterruptedException">Waiting was interrupted by Dispose</exception>
        public bool Wait(int timeout, CancellationToken token)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(this.GetType().Name);
            if (token.IsCancellationRequested)
                throw new OperationCanceledException(token);

            uint startTime = 0;
            if (timeout > 0)
                startTime = TimeoutHelper.GetTimestamp();

            object tmpState = null;
            return WaitSlowPath(new WaitPredicateCombination<object>(), ref tmpState, startTime, timeout, token);
        }
        /// <summary>
        /// Blocks the current thread until the next notification
        /// </summary>
        /// <param name="timeout">Tiemout in milliseconds</param>
        /// <returns>True if the current thread successfully received a notification</returns>
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
        /// <exception cref="ObjectDisposedException">ConditionVariable was disposed</exception>
        /// <exception cref="OperationCanceledException">Cancellation happened</exception>
        /// <exception cref="OperationInterruptedException">Waiting was interrupted by Dispose</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Wait(CancellationToken token)
        {
            return Wait(Timeout.Infinite, token);
        }

        /// <summary>
        /// Blocks the current thread until predicate estimates as True
        /// </summary>
        /// <param name="predicate">Predicate that should return True to complete waiting</param>
        /// <param name="timeout">Tiemout in milliseconds</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>True if predicate evaluates to True</returns>
        /// <exception cref="ObjectDisposedException">ConditionVariable was disposed</exception>
        /// <exception cref="OperationCanceledException">Cancellation happened</exception>
        /// <exception cref="OperationInterruptedException">Waiting was interrupted by Dispose</exception>
        public bool Wait(WaitPredicate predicate, int timeout, CancellationToken token)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));
            if (_isDisposed)
                throw new ObjectDisposedException(this.GetType().Name);
            if (token.IsCancellationRequested)
                throw new OperationCanceledException(token);

            uint startTime = 0;
            if (timeout > 0)
                startTime = TimeoutHelper.GetTimestamp();

            if (predicate())
                return true;

            if (timeout == 0)
                return false;


            object tmpState = null;
            return WaitSlowPath(new WaitPredicateCombination<object>(predicate), ref tmpState, startTime, timeout, token);
        }
        /// <summary>
        /// Blocks the current thread until predicate estimates as True
        /// </summary>
        /// <param name="predicate">Predicate that should return True to complete waiting</param>
        /// <returns>True if predicate evaluates to True</returns>
        /// <exception cref="ObjectDisposedException">ConditionVariable was disposed</exception>
        /// <exception cref="OperationInterruptedException">Waiting was interrupted by Dispose</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Wait(WaitPredicate predicate)
        {
            return Wait(predicate, Timeout.Infinite, default(CancellationToken));
        }
        /// <summary>
        /// Blocks the current thread until predicate estimates as True
        /// </summary>
        /// <param name="predicate">Predicate that should return True to complete waiting</param>
        /// <param name="timeout">Tiemout in milliseconds</param>
        /// <returns>True if predicate evaluates to True</returns>
        /// <exception cref="ObjectDisposedException">ConditionVariable was disposed</exception>
        /// <exception cref="OperationInterruptedException">Waiting was interrupted by Dispose</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Wait(WaitPredicate predicate, int timeout)
        {
            return Wait(predicate, timeout, default(CancellationToken));
        }
        /// <summary>
        /// Blocks the current thread until predicate estimates as True
        /// </summary>
        /// <param name="predicate">Predicate that should return True to complete waiting</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>True if predicate evaluates to True</returns>
        /// <exception cref="ObjectDisposedException">ConditionVariable was disposed</exception>
        /// <exception cref="OperationCanceledException">Cancellation happened</exception>
        /// <exception cref="OperationInterruptedException">Waiting was interrupted by Dispose</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Wait(WaitPredicate predicate, CancellationToken token)
        {
            return Wait(predicate, Timeout.Infinite, token);
        }
        /// <summary>
        /// Blocks the current thread until predicate estimates as True
        /// </summary>
        /// <param name="predicate">Predicate that should return True to complete waiting</param>
        /// <param name="timeout">Tiemout value</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>True if predicate evaluates to True</returns>
        /// <exception cref="ObjectDisposedException">ConditionVariable was disposed</exception>
        /// <exception cref="OperationCanceledException">Cancellation happened</exception>
        /// <exception cref="OperationInterruptedException">Waiting was interrupted by Dispose</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Wait(WaitPredicate predicate, TimeSpan timeout, CancellationToken token)
        {
            long timeoutMs = (long)timeout.TotalMilliseconds;
            if (timeoutMs > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(timeout));

            return Wait(predicate, (int)timeoutMs, token);
        }
        /// <summary>
        /// Blocks the current thread until predicate estimates as True
        /// </summary>
        /// <param name="predicate">Predicate that should return True to complete waiting</param>
        /// <param name="timeout">Tiemout value</param>
        /// <returns>True if predicate evaluates to True</returns>
        /// <exception cref="ObjectDisposedException">ConditionVariable was disposed</exception>
        /// <exception cref="OperationInterruptedException">Waiting was interrupted by Dispose</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Wait(WaitPredicate predicate, TimeSpan timeout)
        {
            long timeoutMs = (long)timeout.TotalMilliseconds;
            if (timeoutMs > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(timeout));

            return Wait(predicate, (int)timeoutMs, default(CancellationToken));
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
        /// <exception cref="ObjectDisposedException">ConditionVariable was disposed</exception>
        /// <exception cref="OperationCanceledException">Cancellation happened</exception>
        /// <exception cref="OperationInterruptedException">Waiting was interrupted by Dispose</exception>
        public bool Wait<TState>(WaitPredicate<TState> predicate, TState state, int timeout, CancellationToken token)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));
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

            return WaitSlowPath(new WaitPredicateCombination<TState>(predicate), ref state, startTime, timeout, token);
        }
        /// <summary>
        /// Blocks the current thread until predicate estimates as True
        /// </summary>
        /// <typeparam name="TState">Type of the state object</typeparam>
        /// <param name="predicate">Predicate that should return True to complete waiting</param>
        /// <param name="state">State object for the predicate</param>
        /// <returns>True if predicate evaluates to True</returns>
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
        /// Blocks the current thread until predicate estimates as True
        /// </summary>
        /// <typeparam name="TState">Type of the state object</typeparam>
        /// <param name="predicate">Predicate that should return True to complete waiting</param>
        /// <param name="state">State object for the predicate</param>
        /// <param name="timeout">Tiemout in milliseconds</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>True if predicate evaluates to True</returns>
        /// <exception cref="ObjectDisposedException">ConditionVariable was disposed</exception>
        /// <exception cref="OperationCanceledException">Cancellation happened</exception>
        /// <exception cref="OperationInterruptedException">Waiting was interrupted by Dispose</exception>
        public bool Wait<TState>(WaitPredicateRef<TState> predicate, ref TState state, int timeout, CancellationToken token)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));
            if (_isDisposed)
                throw new ObjectDisposedException(this.GetType().Name);
            if (token.IsCancellationRequested)
                throw new OperationCanceledException(token);

            uint startTime = 0;
            if (timeout > 0)
                startTime = TimeoutHelper.GetTimestamp();

            if (predicate(ref state))
                return true;

            if (timeout == 0)
                return false;

            return WaitSlowPath(new WaitPredicateCombination<TState>(predicate), ref state, startTime, timeout, token);
        }
        /// <summary>
        /// Blocks the current thread until predicate estimates as True
        /// </summary>
        /// <typeparam name="TState">Type of the state object</typeparam>
        /// <param name="predicate">Predicate that should return True to complete waiting</param>
        /// <param name="state">State object for the predicate</param>
        /// <returns>True if predicate evaluates to True</returns>
        /// <exception cref="ObjectDisposedException">ConditionVariable was disposed</exception>
        /// <exception cref="OperationInterruptedException">Waiting was interrupted by Dispose</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Wait<TState>(WaitPredicateRef<TState> predicate, ref TState state)
        {
            return Wait(predicate, ref state, Timeout.Infinite, default(CancellationToken));
        }
        /// <summary>
        /// Blocks the current thread until predicate estimates as True
        /// </summary>
        /// <typeparam name="TState">Type of the state object</typeparam>
        /// <param name="predicate">Predicate that should return True to complete waiting</param>
        /// <param name="state">State object for the predicate</param>
        /// <param name="timeout">Tiemout in milliseconds</param>
        /// <returns>True if predicate evaluates to True</returns>
        /// <exception cref="ObjectDisposedException">ConditionVariable was disposed</exception>
        /// <exception cref="OperationInterruptedException">Waiting was interrupted by Dispose</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Wait<TState>(WaitPredicateRef<TState> predicate, ref TState state, int timeout)
        {
            return Wait(predicate, ref state, timeout, default(CancellationToken));
        }
        /// <summary>
        /// Blocks the current thread until predicate estimates as True
        /// </summary>
        /// <typeparam name="TState">Type of the state object</typeparam>
        /// <param name="predicate">Predicate that should return True to complete waiting</param>
        /// <param name="state">State object for the predicate</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>True if predicate evaluates to True</returns>
        /// <exception cref="ObjectDisposedException">ConditionVariable was disposed</exception>
        /// <exception cref="OperationCanceledException">Cancellation happened</exception>
        /// <exception cref="OperationInterruptedException">Waiting was interrupted by Dispose</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Wait<TState>(WaitPredicateRef<TState> predicate, ref TState state, CancellationToken token)
        {
            return Wait(predicate, ref state, Timeout.Infinite, token);
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
        /// <exception cref="ObjectDisposedException">ConditionVariable was disposed</exception>
        /// <exception cref="OperationCanceledException">Cancellation happened</exception>
        /// <exception cref="OperationInterruptedException">Waiting was interrupted by Dispose</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Wait<TState>(WaitPredicateRef<TState> predicate, ref TState state, TimeSpan timeout, CancellationToken token)
        {
            long timeoutMs = (long)timeout.TotalMilliseconds;
            if (timeoutMs > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(timeout));

            return Wait(predicate, ref state, (int)timeoutMs, token);
        }
        /// <summary>
        /// Blocks the current thread until predicate estimates as True
        /// </summary>
        /// <typeparam name="TState">Type of the state object</typeparam>
        /// <param name="predicate">Predicate that should return True to complete waiting</param>
        /// <param name="state">State object for the predicate</param>
        /// <param name="timeout">Tiemout value</param>
        /// <returns>True if predicate evaluates to True</returns>
        /// <exception cref="ObjectDisposedException">ConditionVariable was disposed</exception>
        /// <exception cref="OperationInterruptedException">Waiting was interrupted by Dispose</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Wait<TState>(WaitPredicateRef<TState> predicate, ref TState state, TimeSpan timeout)
        {
            long timeoutMs = (long)timeout.TotalMilliseconds;
            if (timeoutMs > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(timeout));

            return Wait(predicate, ref state, (int)timeoutMs, default(CancellationToken));
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
