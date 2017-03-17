using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Queues.ServiceStuff
{
    /// <summary>
    /// Guard primitive for MutuallyExclusiveProcessPrimitive that allows to use it with 'using' statement
    /// </summary>
    public struct MutuallyExclusiveProcessGuard : IDisposable
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


    internal class MutuallyExclusiveProcessGate : IDisposable
    {
        private volatile int _currentCountInner;
        private readonly ManualResetEventSlim _event;
        private readonly CancellationTokenSource _cancellationRequest;
        //private volatile bool _isTerminateRequested;
        private volatile bool _isDisposed;


        public MutuallyExclusiveProcessGate(bool opened)
        {
            if (opened)
            {
                _currentCountInner = 1;
                _event = new ManualResetEventSlim(true);
                _cancellationRequest = new CancellationTokenSource();
            }
            else
            {
                _currentCountInner = 0;
                _event = new ManualResetEventSlim(false);
                _cancellationRequest = new CancellationTokenSource();
                _cancellationRequest.Cancel();
            }
        }


        public MutuallyExclusiveProcessGuard EnterClient(int timeout, CancellationToken token)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(this.GetType().Name);
            if (token.IsCancellationRequested)
                throw new OperationCanceledException(token);


            if (_event.Wait(timeout, token))
            {
                int newCount = Interlocked.Increment(ref _currentCountInner);
                Debug.Assert(newCount > 0);

                if (_event.IsSet)
                    return new MutuallyExclusiveProcessGuard(this);

                Interlocked.Decrement(ref _currentCountInner);
            }

            return new MutuallyExclusiveProcessGuard();
        }


        private void ExitClientAdditionalActions(int newCount)
        {
            Debug.Assert(newCount == 0);

            lock (this._event)
            {
                if (!_isDisposed)
                {
                    //this._event.Reset();
                }
            }
        }

        internal void ExitClient()
        {
            int newCount = Interlocked.Decrement(ref _currentCountInner);

            Debug.Assert(newCount >= 0);
            if (newCount == 0)
                ExitClientAdditionalActions(newCount);
        }


        public void Terminate()
        {
            if (_event.IsSet)
            {
                _event.Reset();
                _cancellationRequest.Cancel();
                ExitClient();
            }
        }



        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                lock (_event)
                {
                    _event.Dispose();
                }
            }
        }
    }


    internal class MutuallyExclusiveProcessPrimitive : IDisposable
    {
        private readonly object _syncObj;

        private readonly ManualResetEventSlim _gate1;
        private readonly ManualResetEventSlim _gate2;

        private volatile CancellationTokenSource _gate1Cancellation;
        private volatile CancellationTokenSource _gate2Cancellation;

        private volatile int _activeGate;
        private volatile int _clientCount;
        private volatile bool _isDisposed;


        public MutuallyExclusiveProcessPrimitive()
        {
            _activeGate = 0;
            _clientCount = 0;

            _syncObj = new object();

            _gate1 = new ManualResetEventSlim(true);
            _gate2 = new ManualResetEventSlim(false);

            _gate1Cancellation = new CancellationTokenSource();
            _gate2Cancellation = new CancellationTokenSource();
            _gate2Cancellation.Cancel();

            _isDisposed = false;
        }



        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
