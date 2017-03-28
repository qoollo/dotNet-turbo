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
    /// Guard primitive for MutuallyExclusiveProcessPrimitive that allows to use it with 'using' statement
    /// </summary>
    internal struct MutuallyExclusiveProcessGuard : IDisposable
    {
        private MutuallyExclusiveProcessGate _srcGate;

        internal MutuallyExclusiveProcessGuard(MutuallyExclusiveProcessGate srcGate)
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
    /// Tracks the clients and notifies when closed and all clients exited
    /// </summary>
    internal class MutuallyExclusiveProcessGate : IDisposable
    {
        private const int OffsetToOpenedStateBit = 30;
        private const int OpenedStateBitMask = 1 << OffsetToOpenedStateBit;
        private const int CurrentCountBitMask = (1 << OffsetToOpenedStateBit) - 1;

        private readonly Action _clientsExitedNotification;
        private readonly ManualResetEventSlim _event;
        private readonly SpinLock _lock;

        private volatile CancellationTokenSource _cancellationRequest;
        private volatile int _combinedState;
        private volatile bool _isDisposed;


        public MutuallyExclusiveProcessGate(bool opened, Action clientsExitedNotification)
        {
            _lock = new SpinLock(false);
            _clientsExitedNotification = clientsExitedNotification;

            if (opened)
            {
                _combinedState = OpenedStateBitMask;
                _event = new ManualResetEventSlim(true);
            }
            else
            {
                _combinedState = 0;
                _event = new ManualResetEventSlim(false);
            }
        }

        /// <summary>
        /// The current number of entered clients
        /// </summary>
        public int CurrentCount { get { return _combinedState & CurrentCountBitMask; } }
        /// <summary>
        /// Is gate opened
        /// </summary>
        public bool IsOpened { get { return (_combinedState & OpenedStateBitMask) != 0; } }
        /// <summary>
        /// Is gate closed
        /// </summary>
        public bool IsClosed { get { return (_combinedState & OpenedStateBitMask) == 0; } }
        /// <summary>
        /// Is all clients exited
        /// </summary>
        public bool IsFullyClosed { get { return _combinedState == 0; } }
        /// <summary>
        /// Token to cancel processes depends on this gate
        /// </summary>
        public CancellationToken Token { get { return (_cancellationRequest ?? EnsureTokenSourceCreatedSlow()).Token; } }

        /// <summary>
        /// Lazy cretion of CancellationTokenSource
        /// </summary>
        private CancellationTokenSource EnsureTokenSourceCreatedSlow()
        {
            bool lockTaken = false;
            try
            {
                _lock.Enter(ref lockTaken);

                CancellationTokenSource current = _cancellationRequest;
                if (current != null)
                    return current;

                current = new CancellationTokenSource();
                if (IsClosed || _isDisposed)
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

        private bool TryEnterClient()
        {
            SpinWait sw = new SpinWait();
            int currentState = _combinedState;
            Debug.Assert(currentState >= 0);
            while ((currentState & OpenedStateBitMask) != 0 && Interlocked.CompareExchange(ref _combinedState, currentState + 1, currentState) != currentState)
            {
                sw.SpinOnce();
                currentState = _combinedState;
                Debug.Assert(currentState >= 0);
            }

            return (currentState & OpenedStateBitMask) != 0;
        }
        /// <summary>
        /// Attempts to pass the gate if it is open
        /// </summary>
        public MutuallyExclusiveProcessGuard EnterClient(int timeout, CancellationToken token)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(this.GetType().Name);
            if (token.IsCancellationRequested)
                throw new OperationCanceledException(token);

            if (TryEnterClient())
                return new MutuallyExclusiveProcessGuard(this);

            uint startTime = 0;
            if (timeout > 0)
                startTime = TimeoutHelper.GetTimestamp();
            else if (timeout < -1)
                timeout = Timeout.Infinite;

            if (timeout != 0)
            {
                while (!_isDisposed)
                {
                    int remainingWaitMilliseconds = Timeout.Infinite;
                    if (timeout != Timeout.Infinite)
                    {
                        remainingWaitMilliseconds = TimeoutHelper.UpdateTimeout(startTime, timeout);
                        if (remainingWaitMilliseconds <= 0)
                            return new MutuallyExclusiveProcessGuard();
                    }

                    if (_event.Wait(remainingWaitMilliseconds, token) && !_isDisposed && TryEnterClient())
                        return new MutuallyExclusiveProcessGuard(this);
                }

                if (_isDisposed)
                    throw new ObjectDisposedException(this.GetType().Name);
            }

            return new MutuallyExclusiveProcessGuard();
        }


        private void ExitClientAdditionalActions(int newCombinedState)
        {
            Debug.Assert(newCombinedState == 0);

            if (!_isDisposed && _clientsExitedNotification != null)
                _clientsExitedNotification();
        }

        internal void ExitClient()
        {
            int newCombinedState = Interlocked.Decrement(ref _combinedState);

            Debug.Assert(newCombinedState >= 0);
            if (newCombinedState == 0)
                ExitClientAdditionalActions(newCombinedState);
        }

        /// <summary>
        /// Close gate. Should be syncronized
        /// </summary>
        public void Close()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(this.GetType().Name);

            if (IsClosed)
                return;

            bool lockTaken = false;
            CancellationTokenSource srcToCancel = null;
            bool lastClient = false;
            try
            {
                _lock.Enter(ref lockTaken);
            }
            finally
            {
                SpinWait sw = new SpinWait();
                int currentState = _combinedState;
                Debug.Assert(currentState >= 0);
                while ((currentState & OpenedStateBitMask) != 0 && Interlocked.CompareExchange(ref _combinedState, currentState & ~OpenedStateBitMask, currentState) != currentState)
                {
                    sw.SpinOnce();
                    currentState = _combinedState;
                    Debug.Assert(currentState >= 0);
                }

                if ((currentState & OpenedStateBitMask) != 0)
                {
                    // Closed
                    _event.Reset();
                    srcToCancel = _cancellationRequest;
                    lastClient = (currentState & ~OpenedStateBitMask) == 0;
                }

                if (lockTaken)
                    _lock.Exit();

                if (lastClient)
                    ExitClientAdditionalActions(0); // Last client. Send signal
            }


            if (srcToCancel != null)
                srcToCancel.Cancel(); // Should cancel after ExitClientAdditionalActions  
        }
        /// <summary>
        /// Reopen gate. Should be syncronized
        /// </summary>
        public void Open()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(this.GetType().Name);

            if (IsOpened)
                return;

            bool lockTaken = false;
            try
            {
                _lock.Enter(ref lockTaken);
            }
            finally
            {
                SpinWait sw = new SpinWait();
                int currentState = _combinedState;
                Debug.Assert(currentState >= 0);
                while ((currentState & OpenedStateBitMask) == 0 && Interlocked.CompareExchange(ref _combinedState, currentState | OpenedStateBitMask, currentState) != currentState)
                {
                    sw.SpinOnce();
                    currentState = _combinedState;
                    Debug.Assert(currentState >= 0);
                }

                if ((currentState & OpenedStateBitMask) == 0)
                {
                    // Opened
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

    /// <summary>
    /// Allows clients only for one gate from two to perform processing
    /// </summary>
    internal class MutuallyExclusiveProcessPrimitive : IDisposable
    {
        private readonly MutuallyExclusiveProcessGate _gate1;
        private readonly MutuallyExclusiveProcessGate _gate2;
        private readonly object _syncObj;
        private volatile int _forceGate1Waiter;
        private volatile bool _isDisposed;


        public MutuallyExclusiveProcessPrimitive(bool gate1Opened)
        {
            _syncObj = new object();
            _gate1 = new MutuallyExclusiveProcessGate(gate1Opened, Gate1Closed);
            _gate2 = new MutuallyExclusiveProcessGate(!gate1Opened, Gate2Closed);
        }
        public MutuallyExclusiveProcessPrimitive() : this(true)
        {     
        }

        [Conditional("DEBUG")]
        private void ValidateState()
        {
            SpinWait sw = new SpinWait();
            while (!_isDisposed && _gate1.IsFullyClosed && _gate2.IsFullyClosed)
            {
                sw.SpinOnce();
                if (sw.Count > 200)
                    Debug.Fail("Invalid MutuallyExclusiveProcessPrimitive state. At least one gate should be opened");
            }

            sw.Reset();
            while (!_gate1.IsFullyClosed && !_gate2.IsFullyClosed)
            {
                sw.SpinOnce();
                if (sw.Count > 20)
                    Debug.Fail("Both gates opened");
            }
        }

        private void Gate1Closed()
        {
            if (!_isDisposed)
            {
                lock (_syncObj)
                {
                    if (!_isDisposed)
                    {
                        if (_forceGate1Waiter == 0)
                        {
                            Debug.Assert((_gate1.IsFullyClosed) || (_gate2.IsFullyClosed && _gate1.IsOpened), "Gates desync 1");
                            if (_gate1.IsFullyClosed)
                                _gate2.Open();
                        }
                        else
                        {
                            Debug.Assert((_gate1.IsFullyClosed && _gate2.IsOpened) || (_gate2.IsFullyClosed), "Gates desync 2");
                            if (_gate2.IsFullyClosed)
                                _gate1.Open();
                        }
                    }
                }
            }
        }
        private void Gate2Closed()
        {
            if (!_isDisposed)
            {
                lock (_syncObj)
                {
                    if (!_isDisposed)
                    {
                        Debug.Assert((_gate1.IsFullyClosed && _gate2.IsOpened) || (_gate2.IsFullyClosed), "Gates desync 3");
                        if (_gate2.IsFullyClosed)
                            _gate1.Open();
                    }
                }
            }
        }

        /// <summary>
        /// Request Gate 1 to be opened and Gate 2 to be closed
        /// </summary>
        public void RequestGate1Open()
        {
            ValidateState();
            if (_gate2.IsOpened)
            {
                lock (_syncObj)
                {
                    _gate2.Close();
                }
            }
        }
        /// <summary>
        /// Request Gate 2 to be opened and Gate 1 to be closed
        /// </summary>
        public void RequestGate2Open()
        {
            ValidateState();
            if (_forceGate1Waiter == 0 && _gate1.IsOpened)
            {
                lock (_syncObj)
                {
                    if (_forceGate1Waiter == 0)
                        _gate1.Close();
                }
            }
        }


        /// <summary>
        /// Attempts to pass the gate 1 if it is open
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MutuallyExclusiveProcessGuard EnterGate1(int timeout, CancellationToken token)
        {
            ValidateState();
            return _gate1.EnterClient(timeout, token);
        }

        /// <summary>
        /// Open gate 1 and attempt to pass it
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MutuallyExclusiveProcessGuard OpenAndEnterGate1(int timeout, CancellationToken token)
        {
            ValidateState();
            try
            {
                Interlocked.Increment(ref _forceGate1Waiter);
                lock (_syncObj)
                {            
                    _gate2.Close();
                    //if (_gate2.IsFullyClosed)
                    //    _gate1.Open();          // Prevent deadlock
                }
                return _gate1.EnterClient(timeout, token);
            }
            finally
            {
                Interlocked.Decrement(ref _forceGate1Waiter);
            }
        }
        /// <summary>
        /// Attempts to pass the gate 2 if it is open
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MutuallyExclusiveProcessGuard EnterGate2(int timeout, CancellationToken token)
        {
            ValidateState();
            return _gate2.EnterClient(timeout, token);
        }


        /// <summary>
        /// Cleans-up resources
        /// </summary>
        public void Dispose()
        {
            if (!_isDisposed)
            {
                lock (_syncObj)
                {
                    if (!_isDisposed)
                    {
                        _isDisposed = true;
                        _gate1.Dispose();
                        _gate2.Dispose();
                    }
                }
            }
        }
    }
}
