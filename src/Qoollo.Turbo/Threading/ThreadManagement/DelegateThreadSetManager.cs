using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Threading.ThreadManagement
{
    /// <summary>
    /// Represents the method that executes on <see cref="Thread"/> (with cancellation token)
    /// </summary>
    /// <param name="token">Cancellation token</param>
    public delegate void TokenThreadStart(CancellationToken token);
    /// <summary>
    /// Represents the method that executes on <see cref="Thread"/> (with state object and cancellation token)
    /// </summary>
    /// <param name="state">State object</param>
    /// <param name="token">Cancellation token</param>
    public delegate void ParametrizedTokenThreadStart(object state, CancellationToken token);
    /// <summary>
    /// Represents the method that executes on <see cref="Thread"/> (with thread id, state object and cancellation token)
    /// </summary>
    /// <param name="threadUID">Thread ID</param>
    /// <param name="state">State object</param>
    /// <param name="token">Cancellation token</param>
    public delegate void ParametrizedIdTokenThreadStart(int threadUID, object state, CancellationToken token);


    /// <summary>
    /// Manages multiple threads. Concrete processing action passed as delegate.
    /// </summary>
    public sealed class DelegateThreadSetManager: ThreadSetManager
    {
        private readonly ThreadStart _threadStartAction;
        private readonly ParameterizedThreadStart _parametrizedThreadStartAction;
        private readonly TokenThreadStart _tokenThreadStartAction;
        private readonly ParametrizedTokenThreadStart _stateTokenThreadStartAction;
        private readonly ParametrizedIdTokenThreadStart _idStateTokenThreadStartAction;

        private object _state;

        /// <summary>
        /// <see cref="DelegateThreadSetManager"/> constructor
        /// </summary>
        /// <param name="threadStartAction">Delegate that represents the methods to be invoked within each thread</param>
        /// <param name="threadCount">Number of threads to manage</param>
        /// <param name="name">Name for this manager and its threads</param>
        public DelegateThreadSetManager(int threadCount, string name, ThreadStart threadStartAction)
            : base(threadCount, name)
        {
            if (threadStartAction == null)
                throw new ArgumentNullException(nameof(threadStartAction));

            _threadStartAction = threadStartAction;
        }
        /// <summary>
        /// <see cref="DelegateThreadSetManager"/> constructor
        /// </summary>
        /// <param name="parametrizedThreadStartAction">Delegate that represents the methods to be invoked within each thread</param>
        /// <param name="threadCount">Number of threads to manage</param>
        /// <param name="name">Name for this manager and its threads</param>
        public DelegateThreadSetManager(int threadCount, string name, ParameterizedThreadStart parametrizedThreadStartAction)
            : base(threadCount, name)
        {
            if (parametrizedThreadStartAction == null)
                throw new ArgumentNullException(nameof(parametrizedThreadStartAction));

            _parametrizedThreadStartAction = parametrizedThreadStartAction;
        }
        /// <summary>
        /// <see cref="DelegateThreadSetManager"/> constructor
        /// </summary>
        /// <param name="tokenThreadStartAction">Delegate that represents the methods to be invoked within each thread</param>
        /// <param name="threadCount">Number of threads to manage</param>
        /// <param name="name">Name for this manager and its threads</param>
        public DelegateThreadSetManager(int threadCount, string name, TokenThreadStart tokenThreadStartAction)
            : base(threadCount, name)
        {
            if (tokenThreadStartAction == null)
                throw new ArgumentNullException(nameof(tokenThreadStartAction));

            _tokenThreadStartAction = tokenThreadStartAction;
        }
        /// <summary>
        /// <see cref="DelegateThreadSetManager"/> constructor
        /// </summary>
        /// <param name="stateTokenThreadStartAction">Delegate that represents the methods to be invoked within each thread</param>
        /// <param name="threadCount">Number of threads to manage</param>
        /// <param name="name">Name for this manager and its threads</param>
        public DelegateThreadSetManager(int threadCount, string name, ParametrizedTokenThreadStart stateTokenThreadStartAction)
            : base(threadCount, name)
        {
            if (stateTokenThreadStartAction == null)
                throw new ArgumentNullException(nameof(stateTokenThreadStartAction));

            _stateTokenThreadStartAction = stateTokenThreadStartAction;
        }
        /// <summary>
        /// <see cref="DelegateThreadSetManager"/> constructor
        /// </summary>
        /// <param name="idStateTokenThreadStartAction">Delegate that represents the methods to be invoked within each thread</param>
        /// <param name="threadCount">Number of threads to manage</param>
        /// <param name="name">Name for this manager and its threads</param>
        public DelegateThreadSetManager(int threadCount, string name, ParametrizedIdTokenThreadStart idStateTokenThreadStartAction)
            : base(threadCount, name)
        {
            if (idStateTokenThreadStartAction == null)
                throw new ArgumentNullException(nameof(idStateTokenThreadStartAction));

            _idStateTokenThreadStartAction = idStateTokenThreadStartAction;
        }


        /// <summary>
        /// Starts all processing threads and changes state to <see cref="ThreadSetManagerState.Running"/>
        /// </summary>
        /// <param name="state">State object that will be passed to thread delegate</param>
        /// <exception cref="ObjectDisposedException">Object was disposed</exception>
        /// <exception cref="WrongStateException">Can't start manager because it is not in <see cref="ThreadSetManagerState.Created"/> state</exception>
        public void Start(object state)
        {
            _state = state;
            this.Start();
        }

        /// <summary>
        /// The main processing logic for every thread (can contain a loop that runs until the cancellation request)
        /// </summary>
        /// <param name="state">Thread specific state object</param>
        /// <param name="token">Cancellation token that will be cancelled when the stop is requested</param>
        protected override void Process(object state, CancellationToken token)
        {
            if (_threadStartAction != null)
                _threadStartAction();
            else if (_parametrizedThreadStartAction != null)
                _parametrizedThreadStartAction(_state);
            else if (_tokenThreadStartAction != null)
                _tokenThreadStartAction(token);
            else if (_stateTokenThreadStartAction != null)
                _stateTokenThreadStartAction(_state, token);
            else if (_idStateTokenThreadStartAction != null)
                _idStateTokenThreadStartAction(this.GetThreadId(), _state, token);
            else
                throw new InvalidOperationException("ThreadStart delegates not initialized");
        }


        /// <summary>
        /// Stops the current <see cref="ThreadSetManager"/>
        /// </summary>
        /// <param name="waitForStop">Whether the current thread should be blocked until all processing threads are completed</param>
        public override void Stop(bool waitForStop)
        {
            base.Stop(waitForStop);
            _state = null;
        }

        /// <summary>
        /// Cleans-up resources
        /// </summary>
        /// <param name="isUserCall">Is it called explicitly by user (False - from finalizer)</param>
        protected override void Dispose(bool isUserCall)
        {
            base.Dispose(isUserCall);

            if (isUserCall)
                _state = null;
        }
    }
}
