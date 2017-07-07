using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Debug = System.Diagnostics.Debug;

namespace Qoollo.Turbo.Threading.ThreadManagement
{
    /// <summary>
    /// Manages multiple threads
    /// </summary>
    public abstract class ThreadSetManager : IDisposable
    {
        /// <summary>
        /// Code contracts
        /// </summary>
        [ContractInvariantMethod]
        private void Invariant()
        {
            Contract.Invariant(_procThreads != null);
            Contract.Invariant(_name != null);
            Contract.Invariant(_threadExitedEvent != null);
            Contract.Invariant(_activeThreadCount >= 0);
            Contract.Invariant(Enum.IsDefined(typeof(ThreadSetManagerState), (ThreadSetManagerState)_state));
        }


        private readonly string _name;
        private readonly int _maxStackSize;

        private bool _isBackground;
        private ThreadPriority _priority;

        private readonly Thread[] _procThreads;
        private int _activeThreadCount;
        private int _exitedThreadCount;

        private readonly ManualResetEventSlim _threadExitedEvent;
        private readonly CancellationTokenSource _stopRequestedCancelation;

        private int _state;


        /// <summary>
        /// <see cref="ThreadSetManager"/> constructor
        /// </summary>
        /// <param name="threadCount">Number of threads to manage</param>
        /// <param name="name">Name for this manager and its threads</param>
        /// <param name="isBackground">Whether or not threads are a background threads</param>
        /// <param name="priority">Indicates the scheduling priority of the threads</param>
        /// <param name="maxStackSize">The maximum stack size to be used by the thread</param>
        public ThreadSetManager(int threadCount, string name, bool isBackground, ThreadPriority priority, int maxStackSize)
        {
            if (threadCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(threadCount));
            if (maxStackSize < 0)
                throw new ArgumentOutOfRangeException(nameof(maxStackSize));

            _isBackground = isBackground;
            _name = name ?? this.GetType().GetCSName();
            _maxStackSize = maxStackSize;
            _priority = priority;

            _procThreads = new Thread[threadCount];
            _activeThreadCount = 0;
            _exitedThreadCount = 0;

            for (int i = 0; i < _procThreads.Length; i++)
            {
                _procThreads[i] = new Thread(ThreadProcFunc, _maxStackSize);
                _procThreads[i].Name = string.Format("{0} (#{1})", _name, i);
                _procThreads[i].Priority = _priority;
                _procThreads[i].IsBackground = _isBackground;
            }

            _threadExitedEvent = new ManualResetEventSlim(false);
            _stopRequestedCancelation = new CancellationTokenSource();

            _state = (int)ThreadSetManagerState.Created;

            Profiling.Profiler.ThreadSetManagerCreated(this.Name, threadCount);
        }
        /// <summary>
        /// <see cref="ThreadSetManager"/> constructor
        /// </summary>
        /// <param name="threadCount">Number of threads to manage</param>
        /// <param name="name">Name for this manager and its threads</param>
        public ThreadSetManager(int threadCount, string name)
            : this(threadCount, name, false, ThreadPriority.Normal, 0)
        {
        }
        /// <summary>
        /// <see cref="ThreadSetManager"/> constructor
        /// </summary>
        /// <param name="threadCount">Number of threads to manage</param>
        public ThreadSetManager(int threadCount)
            : this(threadCount, null, false, ThreadPriority.Normal, 0)
        {
        }

        /// <summary>
        /// Current state
        /// </summary>
        public ThreadSetManagerState State
        {
            get { return (ThreadSetManagerState)Volatile.Read(ref _state); }
        }
        /// <summary>
        /// Whether the <see cref="ThreadSetManager"/> is running
        /// </summary>
        public bool IsWork
        {
            get { return State == ThreadSetManagerState.Running; }
        }
        /// <summary>
        /// Whether the stop was requested and the processor should complete its work
        /// </summary>
        protected bool IsStopRequested
        {
            get { return State == ThreadSetManagerState.StopRequested; }
        }
        /// <summary>
        /// Whether the <see cref="ThreadSetManager"/> was stopped
        /// </summary>
        protected bool IsStopped
        {
            get { return State == ThreadSetManagerState.Stopped; }
        }
        /// <summary>
        /// Whether the stop was requested or already stopped
        /// </summary>
        protected bool IsStopRequestedOrStopped
        {
            get
            {
                var state = State;
                return state == ThreadSetManagerState.Stopped || state == ThreadSetManagerState.StopRequested;
            }
        }

        /// <summary>
        /// The name for this instance of <see cref="ThreadSetManager"/>
        /// </summary>
        public string Name
        {
            get { return _name; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not threads are a background threads
        /// </summary>
        public bool IsBackground
        {
            get { return _isBackground; }
            set
            {
                CheckPendingDisposeOrDisposed();

                foreach (var th in _procThreads)
                    th.IsBackground = value;
                _isBackground = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating the scheduling priority of the threads
        /// </summary>
        public ThreadPriority Priority
        {
            get { return _priority; }
            set
            {
                CheckPendingDisposeOrDisposed();

                foreach (var th in _procThreads)
                    th.Priority = value;
                _priority = value;
            }
        }

        /// <summary>
        /// Gets or sets the culture for the managed threads
        /// </summary>
        public System.Globalization.CultureInfo CurrentCulture
        {
            get { return _procThreads[0].CurrentCulture; }
            set
            {
                CheckPendingDisposeOrDisposed();

                foreach (var th in _procThreads)
                    th.CurrentCulture = value;
            }
        }

        /// <summary>
        /// Gets or sets the current culture used by the Resource Manager to look up culture-specific resources
        /// </summary>
        public System.Globalization.CultureInfo CurrentUICulture
        {
            get { return _procThreads[0].CurrentUICulture; }
            set
            {
                CheckPendingDisposeOrDisposed();

                foreach (var th in _procThreads)
                    th.CurrentUICulture = value;
            }
        }

        /// <summary>
        /// Number of processing threads running right now
        /// </summary>
        public int ActiveThreadCount
        {
            get { return Volatile.Read(ref _activeThreadCount); }
        }
        /// <summary>
        /// Number of processing threads
        /// </summary>
        public int ThreadCount
        {
            get { return _procThreads.Length; }
        }


        /// <summary>
        /// Verifies that state transition is possible
        /// </summary>
        /// <param name="oldState">Current state</param>
        /// <param name="newState">New state</param>
        /// <returns>True when state transition can be performed</returns>
        private bool IsValidStateTransition(ThreadSetManagerState oldState, ThreadSetManagerState newState)
        {
            switch (oldState)
            {
                case ThreadSetManagerState.Created:
                    return newState == ThreadSetManagerState.StartRequested || newState == ThreadSetManagerState.StopRequested;
                case ThreadSetManagerState.StartRequested:
                    return newState == ThreadSetManagerState.Running || newState == ThreadSetManagerState.Stopped;
                case ThreadSetManagerState.Running:
                    return newState == ThreadSetManagerState.StopRequested || newState == ThreadSetManagerState.AllThreadsExited;
                case ThreadSetManagerState.StopRequested:
                    return newState == ThreadSetManagerState.Stopped;
                case ThreadSetManagerState.AllThreadsExited:
                    return newState == ThreadSetManagerState.StopRequested;
                case ThreadSetManagerState.Stopped:
                    return false;
                default:
                    return false;
            }
        }
        /// <summary>
        /// Safely changes the current state
        /// </summary>
        /// <param name="newState">New state</param>
        /// <param name="prevState">Previously observed state</param>
        /// <returns>Was state changed (false means that the state transition is not valid)</returns>
        private bool ChangeStateSafe(ThreadSetManagerState newState, out ThreadSetManagerState prevState)
        {
            prevState = (ThreadSetManagerState)Volatile.Read(ref _state);

            if (!IsValidStateTransition(prevState, newState))
                return false;

            SpinWait sw = new SpinWait();
            while (Interlocked.CompareExchange(ref _state, (int)newState, (int)prevState) != (int)prevState)
            {
                sw.SpinOnce();
                prevState = (ThreadSetManagerState)Volatile.Read(ref _state);
                if (!IsValidStateTransition(prevState, newState))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Checks whether the current instance is in Stopped state and throws ObjectDisposedException when it is
        /// </summary>
        protected void CheckDisposed()
        {
            if (State == ThreadSetManagerState.Stopped)
                throw new ObjectDisposedException(this.GetType().Name, "ThreadSetManager is Stopped");
        }
        /// <summary>
        /// Checks whether the current instance is in Stopped or StopRequested state and throws ObjectDisposedException when it is
        /// </summary>
        protected void CheckPendingDisposeOrDisposed()
        {
            var state = State;
            if (state == ThreadSetManagerState.Stopped || state == ThreadSetManagerState.StopRequested)
                throw new ObjectDisposedException(this.GetType().Name, "ThreadSetManager has Stopped or StopRequested state");
        }

        /// <summary>
        /// Gets the unique thread ID whithin the current ThreadSetManager (-1 when thread is not a part of the ThreadSetManager)
        /// </summary>
        /// <returns>Thread number</returns>
        protected int GetThreadId()
        {
            return Array.IndexOf(_procThreads, Thread.CurrentThread);
        }

        /// <summary>
        /// Scans all threads and gets the information about their states
        /// </summary>
        /// <param name="totalThreadCount">Total number of threads</param>
        /// <param name="runningCount">Number of running threads (in <see cref="ThreadState.Running"/> state)</param>
        /// <param name="waitingCount">Number of threads in <see cref="ThreadState.WaitSleepJoin"/> state</param>
        protected void ScanThreadStates(out int totalThreadCount, out int runningCount, out int waitingCount)
        {
            totalThreadCount = 0;
            runningCount = 0;
            waitingCount = 0;

            for (int i = 0; i < _procThreads.Length; i++)
            {
                if (_procThreads[i] == null)
                    continue;
                
                totalThreadCount++;

                var curState = _procThreads[i].ThreadState;
                if ((curState & ~ThreadState.Background) == ThreadState.Running)
                    runningCount++;
                if ((curState & ThreadState.WaitSleepJoin) != 0)
                    waitingCount++;
            }
        }


        /// <summary>
        /// Starts all processing threads and changes state to <see cref="ThreadSetManagerState.Running"/>
        /// </summary>
        /// <exception cref="ObjectDisposedException">Object was disposed</exception>
        /// <exception cref="WrongStateException">Can't start manager because it is not in <see cref="ThreadSetManagerState.Created"/> state</exception>
        public void Start()
        {
            ThreadSetManagerState prevState;
            if (!ChangeStateSafe(ThreadSetManagerState.StartRequested, out prevState))
            {
                if (prevState == ThreadSetManagerState.Stopped)
                    throw new ObjectDisposedException(this.GetType().Name);
                throw new WrongStateException("Can't start ThreadSetManager cause it is in wrong state: " + prevState.ToString());
            }

            try
            {
                _threadExitedEvent.Reset();

                for (int i = 0; i < _procThreads.Length; i++)
                    _procThreads[i].Start();

                bool changeStateToRunningSuccess = ChangeStateSafe(ThreadSetManagerState.Running, out prevState);
                Debug.Assert(changeStateToRunningSuccess && prevState == ThreadSetManagerState.StartRequested);
            }
            catch
            {
                ChangeStateSafe(ThreadSetManagerState.Stopped, out prevState);

                _threadExitedEvent.Set();
                _stopRequestedCancelation.Cancel();

                throw;
            }
        }

        /// <summary>
        /// Gets the CancellationToken that will be cancelled when a stop is requested 
        /// </summary>
        /// <returns>Cancellation token</returns>
        protected CancellationToken GetCancellationToken()
        {
            var tokenSrc = this._stopRequestedCancelation;
            if (tokenSrc != null)
                return tokenSrc.Token;
            return new CancellationToken(true);
        }



        /// <summary>
        /// Main thread procedure
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCode]
        private void ThreadProcFunc()
        {
            CancellationToken token = GetCancellationToken();

            object state = null;

            try
            {
                Interlocked.Increment(ref _activeThreadCount);
                Profiling.Profiler.ThreadSetManagerThreadStart(this.Name, this.ActiveThreadCount, this.ThreadCount);

                state = this.Prepare();

                token.ThrowIfCancellationRequested();
                this.Process(state, token);
            }
            catch (OperationCanceledException opEx)
            {
                if (!token.IsCancellationRequested)
                {
                    ProcessThreadException(opEx);
                    throw;
                }
            }
            catch (Exception ex)
            {
                if (ex.GetType() == typeof(ThreadAbortException) || ex.GetType() == typeof(ThreadInterruptedException) || ex.GetType() == typeof(StackOverflowException) || ex.GetType() == typeof(OutOfMemoryException))
                    throw;

                ProcessThreadException(ex);
                throw;
            }
            finally
            {
                this.Finalize(state);

                int activeThreadCount = Interlocked.Decrement(ref _activeThreadCount);
                int exitedThreadCount = Interlocked.Increment(ref _exitedThreadCount);
                Debug.Assert(activeThreadCount >= 0);
                Debug.Assert(exitedThreadCount <= this.ThreadCount);

                if (exitedThreadCount >= this.ThreadCount || (activeThreadCount == 0 && IsStopRequested))
                {
                    // Вынуждены ждать
                    SpinWait sw = new SpinWait();
                    while (State == ThreadSetManagerState.StartRequested)
                        sw.SpinOnce();

                    ThreadSetManagerState prevState;
                    if (ChangeStateSafe(ThreadSetManagerState.AllThreadsExited, out prevState))
                    {
                        Debug.Assert(prevState == ThreadSetManagerState.Running);
                        _threadExitedEvent.Set();
                    }
                    else if (ChangeStateSafe(ThreadSetManagerState.Stopped, out prevState))
                    {
                        Debug.Assert(prevState == ThreadSetManagerState.StopRequested);
                        _threadExitedEvent.Set();
                        Profiling.Profiler.ThreadSetManagerDisposed(this.Name, false);
                    }
                }

                Profiling.Profiler.ThreadSetManagerThreadStop(this.Name, this.ActiveThreadCount, this.ThreadCount);
            }
        }


        /// <summary>
        /// Method that allows to process unhandled exceptions (e.g. logging).
        /// Default behaviour - throws <see cref="ThreadSetManagerException"/>.
        /// </summary>
        /// <param name="ex">Catched exception</param>
        [System.Diagnostics.DebuggerNonUserCode]
        protected virtual void ProcessThreadException(Exception ex)
        {
            Contract.Requires(ex != null);

            throw new ThreadSetManagerException("Unhandled exception during processing in ThreadSetManager ('" + this.Name + "')", ex);
        }

        /// <summary>
        /// Creates the state that is specific for every processing thread. Executes once for every thread during start-up.
        /// </summary>
        /// <returns>Created thread-specific state object</returns>
        protected virtual object Prepare()
        {
            return null;
        }
        /// <summary>
        /// The main processing logic for every thread (can contain a loop that runs until the cancellation request)
        /// </summary>
        /// <param name="state">Thread specific state object initialized by <see cref="Prepare"/> method</param>
        /// <param name="token">Cancellation token that will be cancelled when the stop is requested</param>
        protected abstract void Process(object state, CancellationToken token);

        /// <summary>
        /// Release the thread specific state object when the thread is about to exit
        /// </summary>
        /// <param name="state">Thread-specific state object</param>
        protected virtual void Finalize(object state)
        {
        }


        /// <summary>
        /// Blocks the current thread and waits for all processing threads to complete
        /// </summary>
        public void Join()
        {
            if (State == ThreadSetManagerState.Stopped || State == ThreadSetManagerState.AllThreadsExited)
                return;

            _threadExitedEvent.Wait();
        }

        /// <summary>
        /// Blocks the current thread and waits for all processing threads to complete
        /// </summary>
        /// <param name="timeout">Waiting timeout in milliseconds</param>
        /// <returns>True when all threads completed in time</returns>
        public bool Join(int timeout)
        {
            if (State == ThreadSetManagerState.Stopped || State == ThreadSetManagerState.AllThreadsExited)
                return true;

            return _threadExitedEvent.Wait(timeout);
        }


        /// <summary>
        /// Stops the current <see cref="ThreadSetManager"/>
        /// </summary>
        /// <param name="waitForStop">Whether the current thread should be blocked until all processing threads are be completed</param>
        private void StopThreadManager(bool waitForStop)
        {
            if (this.IsStopRequestedOrStopped)
            {
                if (waitForStop)
                    this.Join();
            }

            ThreadSetManagerState prevState;
            if (!ChangeStateSafe(ThreadSetManagerState.StopRequested, out prevState))
            {
                if (prevState != ThreadSetManagerState.StartRequested)
                {
                    if (waitForStop)
                        this.Join();

                    return;
                }

                SpinWait sw = new SpinWait();
                while (State == ThreadSetManagerState.StartRequested)
                    sw.SpinOnce();

                if (!ChangeStateSafe(ThreadSetManagerState.StopRequested, out prevState))
                {
                    if (waitForStop)
                        this.Join();

                    return;
                }
            }

            _stopRequestedCancelation.Cancel();

            if (waitForStop && prevState != ThreadSetManagerState.Created)
            {
                for (int i = 0; i < _procThreads.Length; i++)
                {
                    if (_procThreads[i] != null)
                        _procThreads[i].Join();
                }
            }

            if (ActiveThreadCount == 0)
            {
                if (ChangeStateSafe(ThreadSetManagerState.Stopped, out prevState) && prevState == ThreadSetManagerState.StopRequested)
                {
                    _threadExitedEvent.Set();
                    Profiling.Profiler.ThreadSetManagerDisposed(this.Name, false);
                }
            }

            Debug.Assert(State == ThreadSetManagerState.StopRequested || State == ThreadSetManagerState.Stopped);
            Debug.Assert(!waitForStop || State == ThreadSetManagerState.Stopped);
        }


        /// <summary>
        /// Stops the current <see cref="ThreadSetManager"/>
        /// </summary>
        /// <param name="waitForStop">Whether the current thread should be blocked until all processing threads are completed</param>
        public virtual void Stop(bool waitForStop)
        {
            StopThreadManager(waitForStop);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Stops the current <see cref="ThreadSetManager"/>
        /// </summary>
        public void Stop()
        {
            this.Stop(true);
        }

        /// <summary>
        /// Cleans-up resources
        /// </summary>
        /// <param name="isUserCall">Is it called explicitly by user (False - from finalizer)</param>
        protected virtual void Dispose(bool isUserCall)
        {
            if (!this.IsStopRequestedOrStopped)
            {
                Debug.Assert(isUserCall, "ThreadSetManager finalizer called. You should dispose ThreadSetManager explicitly. ThreadSetManagerName: " + this.Name);

                if (isUserCall)
                    StopThreadManager(true);

                ThreadSetManagerState prevState;
                ChangeStateSafe(ThreadSetManagerState.StopRequested, out prevState);
                ChangeStateSafe(ThreadSetManagerState.Stopped, out prevState);

                if (_stopRequestedCancelation != null)
                    _stopRequestedCancelation.Cancel();

                if (!isUserCall)
                    Profiling.Profiler.ThreadSetManagerDisposed(this.Name, true);
            }
        }

        /// <summary>
        /// Cleans-up resources
        /// </summary>
        void IDisposable.Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Finalizer
        /// </summary>
        ~ThreadSetManager()
        {
            Dispose(false);
        }
    }
}
