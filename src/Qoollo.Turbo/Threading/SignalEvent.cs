using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Threading
{
    /// <summary>
    /// Primitive that emits signals about some event for other threads waiting for that event
    /// </summary>
    internal class SignalEvent : IDisposable
    {
        private volatile bool _isDisposed;

        /// <summary>
        /// SignalEvent constructor
        /// </summary>
        public SignalEvent()
        {
            _isDisposed = false;
        }

        private static void ThrowObjectDisposedException()
        {
            throw new ObjectDisposedException(nameof(SignalEvent));
        }

        /// <summary>
        /// Event to call for every signal
        /// </summary>
        private event Action SubscriptionEvent;

        /// <summary>
        /// Subscribe to notifications
        /// </summary>
        /// <param name="waiter">Delegate to call when event happened</param>
        internal void Subscribe(Action waiter)
        {
            if (_isDisposed)
                ThrowObjectDisposedException();

            SubscriptionEvent += waiter;
        }
        /// <summary>
        /// Unsubscribe from notifications
        /// </summary>
        /// <param name="waiter">Delegate to call when event happened</param>
        internal void Unsubscribe(Action waiter)
        {
            SubscriptionEvent -= waiter;
        }

        /// <summary>
        /// Factory to create Waiters for the current SignalEvent
        /// </summary>
        public SignalWaiterFactory Factory { get { return new SignalWaiterFactory(this); } }


        /// <summary>
        /// Sends signal to all waiters
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Signal()
        {
            if (_isDisposed)
                ThrowObjectDisposedException();

            SubscriptionEvent?.Invoke();
        }



        /// <summary>
        /// Cleans-up resources
        /// </summary>
        public void Dispose()
        {
            // Remove all subscriptions
            SubscriptionEvent = null;
            _isDisposed = true;
        }
    }
}
