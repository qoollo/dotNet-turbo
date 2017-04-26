using Qoollo.Turbo.Threading.ServiceStuff;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Queues.ServiceStuff
{
    /// <summary>
    /// Guard primitive for MutuallyExclusivePrimitive that allows to use it with 'using' statement
    /// </summary>
    internal struct MutuallyExclusiveGuard : IDisposable
    {
        private MutuallyExclusivePrimitive.MutuallyExclusiveGate _srcGate;

        internal MutuallyExclusiveGuard(MutuallyExclusivePrimitive.MutuallyExclusiveGate srcGate)
        {
            _srcGate = srcGate;
        }

        /// <summary>
        /// Is entering the protected section was successful
        /// </summary>
        public bool IsAcquired
        {
            get { return _srcGate != null; }
        }

        /// <summary>
        /// Gets the token to cancel the waiting procedure
        /// </summary>
        public CancellationToken Token
        {
            get
            {
                return _srcGate != null ? _srcGate.Token : new CancellationToken(true);
            }
        }
        /// <summary>
        /// Is operation should be cancelled (lightweight analog of Token.IsCancellationRequested)
        /// </summary>
        public bool IsCancellationRequested
        {
            get
            {
                return _srcGate == null || _srcGate.IsCancelled;
            }
        }


        /// <summary>
        /// Exits the protected code section
        /// </summary>
        public void Dispose()
        {
            if (_srcGate != null)
            {
                _srcGate.ExitClient();
                _srcGate = null;
            }
        }
    }

    /// <summary>
    /// Allows clients only for one gate from two to perform processing
    /// </summary>
    internal sealed class MutuallyExclusivePrimitive : IDisposable
    {
        /// <summary>
        /// Tracks the clients and notifies when closed and all clients exited
        /// </summary>
        internal sealed class MutuallyExclusiveGate : IDisposable
        {
            private readonly bool _isBackgroundGate;
            private readonly MutuallyExclusivePrimitive _owner;
            private readonly ManualResetEventSlim _event;
            private SpinLock _lock; // Should not be readonly

            private volatile CancellationTokenSource _cancellationRequest;
            private volatile int _waiterCount;
            private volatile bool _isDisposed;


            public MutuallyExclusiveGate(MutuallyExclusivePrimitive owner, bool isBackgroundGate, bool opened)
            {
                if (owner == null)
                    throw new ArgumentNullException(nameof(owner));

                _isBackgroundGate = isBackgroundGate;
                _owner = owner;
                _event = new ManualResetEventSlim(opened);
#if DEBUG
                _lock = new SpinLock(true);
#else
                _lock = new SpinLock(false);
#endif

                _cancellationRequest = null;
                _waiterCount = 0;
                _isDisposed = false;

            }

            /// <summary>
            /// The current number of entered clients
            /// </summary>
            public int WaiterCount { get { return _waiterCount; } }
            /// <summary>
            /// Is gate opened
            /// </summary>
            public bool IsOpened { get { return _event.IsSet; } }
            /// <summary>
            /// Is gate closed
            /// </summary>
            public bool IsClosed { get { return !_event.IsSet; } }
            /// <summary>
            /// Token to cancel processes depends on this gate
            /// </summary>
            public CancellationToken Token { get { return (_cancellationRequest ?? EnsureTokenSourceCreatedSlow()).Token; } }
            /// <summary>
            /// Is opertion on this gate should be cancelled
            /// </summary>
            public bool IsCancelled { get { return _isDisposed || !_event.IsSet; } }

            /// <summary>
            /// Lazy cretion of CancellationTokenSource
            /// </summary>
            private CancellationTokenSource EnsureTokenSourceCreatedSlow()
            {
                bool lockTaken = false;
                try
                {
                    _lock.Enter(ref lockTaken);

                    if (_cancellationRequest != null)
                        return _cancellationRequest;

                    CancellationTokenSource current = _cancellationRequest;
                    if (current != null)
                        return current;

                    current = new CancellationTokenSource();
                    if (_isDisposed || IsClosed)
                        current.Cancel();
                    _cancellationRequest = current;
                    return current;
                }
                finally
                {
                    if (lockTaken)
                        _lock.Exit();
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private bool TryEnterClient()
            {
                return _owner.TryEnterClient(_isBackgroundGate);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal void ExitClient()
            {
                _owner.ExitClient(_isBackgroundGate);
            }


            /// <summary>
            /// Attempts to pass the gate if it is open
            /// </summary>
            public MutuallyExclusiveGuard EnterClient(int timeout, CancellationToken token)
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(this.GetType().Name);
                if (token.IsCancellationRequested)
                    throw new OperationCanceledException(token);

                try
                {
                    Interlocked.Increment(ref _waiterCount);

                    if (TryEnterClient()) // Always call TryEnterClient first
                        return new MutuallyExclusiveGuard(this);

                    uint startTime = 0;
                    if (timeout > 0)
                        startTime = TimeoutHelper.GetTimestamp();
                    else if (timeout < -1)
                        timeout = Timeout.Infinite;

                    if (timeout != 0)
                    {
                        SpinWait sw = new SpinWait();
                        while (!_isDisposed)
                        {
                            int remainingWaitMilliseconds = Timeout.Infinite;
                            if (timeout != Timeout.Infinite)
                            {
                                remainingWaitMilliseconds = TimeoutHelper.UpdateTimeout(startTime, timeout);
                                if (remainingWaitMilliseconds <= 0)
                                    return new MutuallyExclusiveGuard();
                            }

                            if (_event.Wait(remainingWaitMilliseconds, token) && !_isDisposed)
                            {
                                if (TryEnterClient())
                                    return new MutuallyExclusiveGuard(this);

                                sw.SpinOnce(); // State swap perfromed before manipulations on _event => Should wait some time
                            }
                        }

                        if (_isDisposed)
                            throw new ObjectDisposedException(this.GetType().Name);
                    }

                    return new MutuallyExclusiveGuard();
                }
                finally
                {
                    Interlocked.Decrement(ref _waiterCount);
                }
            }

            /// <summary>
            /// Close gate. Should be syncronized
            /// </summary>
            public void Close()
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(this.GetType().Name);


                if (_event.IsSet)
                {
                    bool lockTaken = false;
                    CancellationTokenSource srcToCancel = null;

                    _lock.Enter(ref lockTaken);

                    if (_event.IsSet)
                    {
                        _event.Reset();
                        srcToCancel = _cancellationRequest;
                    }

                    if (lockTaken)
                        _lock.Exit();

                    if (srcToCancel != null)
                        srcToCancel.Cancel(); // Should cancell outside the lock
                }
            }
            /// <summary>
            /// Reopen gate. Should be syncronized
            /// </summary>
            public void Open()
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(this.GetType().Name);

                if (!_event.IsSet)
                {
                    bool lockTaken = false;
                    _lock.Enter(ref lockTaken);

                    if (!_event.IsSet)
                    {
                        _cancellationRequest = null;
                        _event.Set();
                    }

                    if (lockTaken)
                        _lock.Exit();
                }
            }

            /// <summary>
            /// Cleans-up resources
            /// </summary>
            public void Dispose()
            {
                if (!_isDisposed)
                {
                    _isDisposed = true;
                    CancellationTokenSource srcToCancel = _cancellationRequest;
                    _cancellationRequest = null;
                    _event.Set(); // Open blocked clients
                    _event.Dispose();
                    if (srcToCancel != null)
                        srcToCancel.Cancel();
                }
            }
        }


        // ==============================

        private const int AllowBackgroundBit = 1 << 30;
        private const int MainGateBit = 1 << 29;
        private const int BackgroundGateBit = 1 << 28;
        private const int ClientCountMask = (1 << 28) - 1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsAllowBackground(int state)
        {
            return (state & AllowBackgroundBit) != 0;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsMainGateOpened(int state)
        {
            return (state & MainGateBit) != 0;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsBackgroundGateOpened(int state)
        {
            return (state & BackgroundGateBit) != 0;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetClientCount(int state)
        {
            return state & ClientCountMask;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetGatesState(int state)
        {
            return state & (BackgroundGateBit | MainGateBit);
        }

        private readonly MutuallyExclusiveGate _mainGate;
        private readonly MutuallyExclusiveGate _backgroundGate;
        private SpinLock _lock; // Should not be readonly

        private volatile int _combinedState;
        private volatile bool _isDisposed;


        /// <summary>
        /// MutuallyExclusivePrimitive constructor
        /// </summary>
        /// <param name="mainGateOpened">True - MainGate opened, BackgroundGate closed, False - MainGate closed, BackgroundGate opened</param>
        public MutuallyExclusivePrimitive(bool mainGateOpened)
        {
            _mainGate = new MutuallyExclusiveGate(this, false, mainGateOpened);
            _backgroundGate = new MutuallyExclusiveGate(this, true, !mainGateOpened);
#if DEBUG
            _lock = new SpinLock(true);
#else
            _lock = new SpinLock(false);
#endif

            _combinedState = mainGateOpened ? MainGateBit : BackgroundGateBit | AllowBackgroundBit;
            _isDisposed = false;
        }
        public MutuallyExclusivePrimitive() : this(true)
        {
        }

        internal bool IsMainGateOpenedState { get { return IsMainGateOpened(_combinedState); } }
        internal bool IsBackgroundGateOpenedState { get { return IsBackgroundGateOpened(_combinedState); } }
        internal int ClientCount { get { return GetClientCount(_combinedState); } }

        /// <summary>
        /// Allow entering to background gate
        /// </summary>
        public bool IsBackgroundGateAllowed
        {
            get { return IsAllowBackground(_combinedState); }
            set
            {
                SpinWait sw = new SpinWait();
                while (true)
                {
                    int combinedState = _combinedState;
                    int newValue = value ? (combinedState | AllowBackgroundBit) : (combinedState & ~AllowBackgroundBit & ~BackgroundGateBit);
                    if (combinedState == newValue)
                        return;

                    if (GetClientCount(combinedState) == 0)
                        newValue = UpdateZeroClientCountState(newValue);

                    if (Interlocked.CompareExchange(ref _combinedState, newValue, combinedState) == combinedState)
                    {
                        if (GetGatesState(combinedState) != GetGatesState(newValue))
                            SwapGatesAtomic(newValue);
                        return;
                    }

                    sw.SpinOnce();
                }
            }
        }

        public void AllowBackgroundGate()
        {
            IsBackgroundGateAllowed = true;
        }
        public void DisallowBackgroundGate()
        {
            IsBackgroundGateAllowed = false;
        }

        /// <summary>
        /// Attempts to pass the main gate if it is open
        /// </summary>
        public MutuallyExclusiveGuard EnterMain(int timeout, CancellationToken token)
        {
            return _mainGate.EnterClient(timeout, token);
        }
        /// <summary>
        /// Attempts to pass the background gate if it is open
        /// </summary>
        public MutuallyExclusiveGuard EnterBackground(int timeout, CancellationToken token)
        {
            return _backgroundGate.EnterClient(timeout, token);
        }



        private void SwapGatesAtomic(int newCombinedStateValue)
        {
            Debug.Assert(!IsBackgroundGateOpened(newCombinedStateValue) || !IsMainGateOpened(newCombinedStateValue), "Two gates can't be opened at the same time");

            bool lockTaken = false;
            try
            {
                _lock.Enter(ref lockTaken);
                if (_isDisposed)
                    return;

                int combinedState = _combinedState; // Should reread the current state
                Debug.Assert(!IsBackgroundGateOpened(combinedState) || !IsMainGateOpened(combinedState), "Two gates can't be opened at the same time");

                try { }
                finally
                {
                    if (!IsBackgroundGateOpened(combinedState))
                        _backgroundGate.Close();
                    if (!IsMainGateOpened(combinedState))
                        _mainGate.Close();

                    // Close gates before Opening another
                    if (IsMainGateOpened(combinedState))
                        _mainGate.Open();
                    if (IsBackgroundGateOpened(combinedState))
                        _backgroundGate.Open();
                }

                Debug.Assert(IsBackgroundGateOpened(combinedState) == _backgroundGate.IsOpened && IsMainGateOpened(combinedState) == _mainGate.IsOpened, "Gate states should be in sync");
            }
            finally
            {
                if (lockTaken)
                    _lock.Exit();
            }
        }

        private int UpdateZeroClientCountState(int newCombinedStateValue)
        {
            Debug.Assert(GetClientCount(newCombinedStateValue) == 0);

            if (_mainGate.WaiterCount > 0)
            {
                newCombinedStateValue = (newCombinedStateValue & ~BackgroundGateBit); // close background gate
                newCombinedStateValue = (newCombinedStateValue | MainGateBit); // Open main gate
            }
            else if (_backgroundGate.WaiterCount > 0 && IsAllowBackground(newCombinedStateValue))
            {
                newCombinedStateValue = (newCombinedStateValue & ~MainGateBit); // close main gate
                newCombinedStateValue = (newCombinedStateValue | BackgroundGateBit); // open background gate
            }
            return newCombinedStateValue;
        }


        internal bool TryEnterClient(bool isBackground)
        {
            if (_isDisposed)
                return false;

            SpinWait sw = new SpinWait();
            while (true)
            {
                int combinedState = _combinedState;
                int newValue = combinedState;
                bool canEnter = false;
                Debug.Assert(combinedState >= 0);
                Debug.Assert(GetClientCount(combinedState) + 1 < ClientCountMask, "Client number is too large");
                Debug.Assert(!IsBackgroundGateOpened(combinedState) || !IsMainGateOpened(combinedState), "Two gates can't be opened at the same time");
                Debug.Assert(IsAllowBackground(combinedState) || !IsBackgroundGateOpened(combinedState), "When background is not allowed, background gate should be closed");


                if (GetClientCount(combinedState) == 0) // Can open any gate
                    newValue = UpdateZeroClientCountState(newValue);
                else if (_mainGate.WaiterCount > 0) // Should close background gate
                    newValue = (newValue & ~BackgroundGateBit);

                if (canEnter = (isBackground && IsBackgroundGateOpened(newValue)) || (!isBackground && IsMainGateOpened(newValue)))
                    newValue = newValue + 1; // Can enter

                if (newValue == combinedState)
                    return canEnter; // Nothing to do

                if (Interlocked.CompareExchange(ref _combinedState, newValue, combinedState) == combinedState)
                {
                    if (GetGatesState(combinedState) != GetGatesState(newValue))
                        SwapGatesAtomic(newValue); // Switch gates
                    return canEnter;
                }

                sw.SpinOnce();
            }
        }

        internal void ExitClient(bool isBackground)
        {
            SpinWait sw = new SpinWait();
            while (true)
            {
                int combinedState = _combinedState;
                int newValue = combinedState - 1;

                Debug.Assert(combinedState >= 0);
                Debug.Assert((!IsMainGateOpened(combinedState) && isBackground) || (!IsBackgroundGateOpened(combinedState) && !isBackground), "Can exit only if specified gate opened");
                Debug.Assert(!IsBackgroundGateOpened(combinedState) || !IsMainGateOpened(combinedState), "Two gates can't be opened at the same time");

                Debug.Assert(GetClientCount(combinedState) > 0, "Client number should be positive");
                Debug.Assert(IsAllowBackground(combinedState) || !IsBackgroundGateOpened(combinedState), "When background is not allowed, background gate should be closed");

                if (GetClientCount(newValue) == 0) // zero number of clients => can switch gates
                    newValue = UpdateZeroClientCountState(newValue);

                if (Interlocked.CompareExchange(ref _combinedState, newValue, combinedState) == combinedState)
                {
                    if (GetGatesState(combinedState) != GetGatesState(newValue))
                        SwapGatesAtomic(newValue);
                    return;
                }

                sw.SpinOnce();
            }
        }

        /// <summary>
        /// Cleans-up resources
        /// </summary>
        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;

                bool lockTaken = false;
                try
                {
                    _lock.Enter(ref lockTaken);
                    _backgroundGate.Dispose();
                    _mainGate.Dispose();
                }
                finally
                {
                    if (lockTaken)
                        _lock.Exit();
                }
            }
        }
    }
}
