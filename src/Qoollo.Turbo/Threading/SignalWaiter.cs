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
    /// Specifies the number of threads to notify on signal
    /// </summary>
    public enum SignalMode
    {
        /// <summary>
        /// Notifies all threads
        /// </summary>
        All,
        /// <summary>
        /// Notifies only one thread
        /// </summary>
        One
    }

    /// <summary>
    /// Primitive to wait for signals from <see cref="SignalEvent"/>
    /// </summary>
    public class SignalWaiter : IDisposable
    {
        private static readonly Action<object> _cancellationTokenCanceledEventHandler = new Action<object>(CancellationTokenCanceledEventHandler);
        /// <summary>
        /// Cancellation handler for CancellationToken
        /// </summary>
        /// <param name="obj">ConditionVariable object</param>
        private static void CancellationTokenCanceledEventHandler(object obj)
        {
            SignalWaiter signalWaiter = obj as SignalWaiter;
            Debug.Assert(signalWaiter != null);
            signalWaiter.SignalAll();
        }

        // =================


        private readonly WeakReference<SignalEvent> _originalEvent;
        private readonly WeakReference<SignalEvent>[] _originalEventList;
        private readonly SignalMode _signalMode;
        private volatile int _waitCount;
        private volatile bool _isDisposed;

        /// <summary>
        /// SignalWaiter constructor
        /// </summary>
        /// <param name="originalEvent">Event which emits signals for the current waiter</param>
        /// <param name="signalMode">Signaling mode</param>
        public SignalWaiter(SignalEvent originalEvent, SignalMode signalMode)
        {
            if (originalEvent == null)
                throw new ArgumentNullException(nameof(originalEvent));

            Debug.Assert(Enum.IsDefined(typeof(SignalMode), signalMode));

            _originalEventList = null;
            _signalMode = signalMode;
            _waitCount = 0;
            _isDisposed = false;

            _originalEvent = new WeakReference<SignalEvent>(originalEvent);
            originalEvent.Subscribe(SignalHandler);
        }
        /// <summary>
        /// SignalWaiter constructor
        /// </summary>
        /// <param name="originalEvent">Event which emits signals for the current waiter</param>
        public SignalWaiter(SignalEvent originalEvent)
            : this(originalEvent, SignalMode.All)
        {
        }
        /// <summary>
        /// SignalWaiter constructor
        /// </summary>
        /// <param name="originalEvents">The sequence of events that emit signals for the current waiter</param>
        /// <param name="signalMode">Signaling mode</param>
        public SignalWaiter(IEnumerable<SignalEvent> originalEvents, SignalMode signalMode)
        {
            if (originalEvents == null)
                throw new ArgumentNullException(nameof(originalEvents));

            Debug.Assert(Enum.IsDefined(typeof(SignalMode), signalMode));

            _originalEvent = null;
            _signalMode = signalMode;
            _waitCount = 0;
            _isDisposed = false;

            List<WeakReference<SignalEvent>> originalEventsTmp = new List<WeakReference<SignalEvent>>();
            foreach (var ev in originalEvents)
            {
                if (ev == null)
                    throw new ArgumentNullException(nameof(originalEvents), "One of the signal events is null");

                originalEventsTmp.Add(new WeakReference<SignalEvent>(ev));
                ev.Subscribe(SignalHandler);
            }
            _originalEventList = originalEventsTmp.ToArray();
        }
        /// <summary>
        /// SignalWaiter constructor
        /// </summary>
        /// <param name="originalEvents">The sequence of events that emit signals for the current waiter</param>
        public SignalWaiter(IEnumerable<SignalEvent> originalEvents)
            : this (originalEvents, SignalMode.All)
        {
        }


        /// <summary>
        /// Gets the signaling mode of the current waiter
        /// </summary>
        public SignalMode SignalMode { get { return _signalMode; } }
        /// <summary>
        /// The number of waiting threads on the SignalWaiter
        /// </summary>
        public int WaiterCount { get { return _waitCount; } }


        /// <summary>
        /// Helper method to register notification for token cancellation
        /// </summary>
        /// <param name="token">Cancellation token</param>
        /// <returns>Registration info</returns>
        internal CancellationTokenRegistration RegisterNotificationOnCancellation(CancellationToken token)
        {
            return CancellationTokenHelper.RegisterWithoutEC(token, _cancellationTokenCanceledEventHandler, this);
        }


        /// <summary>
        /// Blocks the current thread until the next notification
        /// </summary>
        /// <param name="timeout">Tiemout in milliseconds</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>True if the current thread successfully received a notification</returns>
        /// <exception cref="InvalidOperationException">Lock is not acquired</exception>
        /// <exception cref="ObjectDisposedException">Waiter was disposed</exception>
        /// <exception cref="OperationCanceledException">Cancellation happened</exception>
        /// <exception cref="OperationInterruptedException">Waiting was interrupted by Dispose</exception>
        public bool Wait(int timeout, CancellationToken token)
        {
            if (!Monitor.IsEntered(this))
                throw new InvalidOperationException("Lock on the current SignalWaiter should be acquired");
            if (_isDisposed)
                throw new ObjectDisposedException(this.GetType().Name);
            if (token.IsCancellationRequested)
                throw new OperationCanceledException(token);

            if (timeout < 0)
                timeout = Timeout.Infinite;


            CancellationTokenRegistration cancellationTokenRegistration = default(CancellationTokenRegistration);
            try
            {
                _waitCount++;

                if (token.CanBeCanceled)
                    cancellationTokenRegistration = CancellationTokenHelper.RegisterWithoutEC(token, _cancellationTokenCanceledEventHandler, this);

                // Waiting for signal
                if (!Monitor.Wait(this, timeout))
                    return false;

                // Check if cancellation or dispose was the reasons of the signal
                if (token.IsCancellationRequested)
                    throw new OperationCanceledException(token);
                if (_isDisposed)
                    throw new OperationInterruptedException("Wait was interrupted by Dispose", new ObjectDisposedException(this.GetType().Name));
            }
            finally
            {
                _waitCount--;
                cancellationTokenRegistration.Dispose();
            }

            return true;
        }
        /// <summary>
        /// Blocks the current thread until the next notification
        /// </summary>
        /// <param name="timeout">Tiemout in milliseconds</param>
        /// <returns>True if the current thread successfully received a notification</returns>
        /// <exception cref="InvalidOperationException">Lock is not acquired</exception>
        /// <exception cref="ObjectDisposedException">Waiter was disposed</exception>
        /// <exception cref="OperationInterruptedException">Waiting was interrupted by Dispose</exception>
        public bool Wait(int timeout)
        {
            if (!Monitor.IsEntered(this))
                throw new InvalidOperationException("Lock on the current SignalWaiter should be acquired");
            if (_isDisposed)
                throw new ObjectDisposedException(this.GetType().Name);

            if (timeout < 0)
                timeout = Timeout.Infinite;

            try
            {
                _waitCount++;

                // Waiting for signal
                if (!Monitor.Wait(this, timeout))
                    return false;

                if (_isDisposed)
                    throw new OperationInterruptedException("Wait was interrupted by Dispose", new ObjectDisposedException(this.GetType().Name));
            }
            finally
            {
                _waitCount--;
            }

            return true;
        }
        /// <summary>
        /// Blocks the current thread until the next notification
        /// </summary>
        /// <param name="timeout">Tiemout value</param>
        /// <returns>True if the current thread successfully received a notification</returns>
        /// <exception cref="InvalidOperationException">Lock is not acquired</exception>
        /// <exception cref="ObjectDisposedException">Waiter was disposed</exception>
        /// <exception cref="OperationInterruptedException">Waiting was interrupted by Dispose</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Wait(TimeSpan timeout)
        {
            long timeoutMs = (long)timeout.TotalMilliseconds;
            if (timeoutMs > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(timeout));

            return Wait((int)timeoutMs);
        }
        /// <summary>
        /// Blocks the current thread until the next notification
        /// </summary>
        /// <param name="token">Cancellation token</param>
        /// <returns>True if the current thread successfully received a notification</returns>
        /// <exception cref="InvalidOperationException">Lock is not acquired</exception>
        /// <exception cref="ObjectDisposedException">Waiter was disposed</exception>
        /// <exception cref="OperationCanceledException">Cancellation happened</exception>
        /// <exception cref="OperationInterruptedException">Waiting was interrupted by Dispose</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Wait(CancellationToken token)
        {
            return Wait(Timeout.Infinite, token);
        }




        /// <summary>
        /// Perofrm notification of threads
        /// </summary>
        private void SignalHandler()
        {
            if (_isDisposed)
                return;

            lock (this)
            {
                if (SignalMode == SignalMode.One)
                    Monitor.Pulse(this);
                else
                    Monitor.PulseAll(this);
            }
        }

        /// <summary>
        /// Sends signal to one waiting thread
        /// </summary>
        protected internal void Signal()
        {
            lock (this)
            {
                Monitor.Pulse(this);
            }
        }

        /// <summary>
        /// Sends signal to all waiting threads
        /// </summary>
        protected internal void SignalAll()
        {
            lock (this)
            {
                Monitor.PulseAll(this);
            }
        }


        /// <summary>
        /// Unsubscribe from source EventSignal
        /// </summary>
        private void RemoveAllSubscriptions()
        {
            Debug.Assert((_originalEvent != null && _originalEventList == null) || (_originalEvent == null && _originalEventList != null));

            if (_originalEvent != null)
            {
                SignalEvent originalEventLocal = null;
                if (_originalEvent.TryGetTarget(out originalEventLocal))
                    originalEventLocal.Unsubscribe(SignalHandler);
            }
            if (_originalEventList != null)
            {
                for (int i = 0; i < _originalEventList.Length; i++)
                {
                    SignalEvent originalEventLocal = null;
                    if (_originalEventList[i].TryGetTarget(out originalEventLocal))
                        originalEventLocal.Unsubscribe(SignalHandler);
                }
            }
        }

        /// <summary>
        /// Cleans-up resources
        /// </summary>
        public void Dispose()
        {
            if (!_isDisposed)
            {            
                lock (this)
                {
                    if (!_isDisposed)
                    {
                        _isDisposed = true;
                        RemoveAllSubscriptions();
                        Monitor.PulseAll(this);
                    }
                }              
            }
        }
    }
}
