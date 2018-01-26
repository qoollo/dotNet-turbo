using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Qoollo.Turbo.Queues;

namespace Qoollo.Turbo.Threading.QueueProcessing
{
    /// <summary>
    /// Asynchronous items processor with queue
    /// </summary>
    /// <typeparam name="T">Type of the elements processed by this <see cref="QueueAsyncProcessor{T}"/></typeparam>
    public abstract class QueueAsyncProcessor<T> : QueueAsyncProcessorBase<T>, IQueueAsyncProcessorStartStopHelper
    {
        /// <summary>
        /// Code contracts invariants
        /// </summary>
        [ContractInvariantMethod]
        private void Invariant()
        {
            TurboContract.Invariant(_procThreads != null);
            TurboContract.Invariant(_queue != null);
            TurboContract.Invariant(_name != null);
            TurboContract.Invariant(_activeThreadCount >= 0);
            TurboContract.Invariant(Enum.IsDefined(typeof(QueueAsyncProcessorState), (QueueAsyncProcessorState)_state));
        }


        private readonly string _name;
        private readonly bool _isBackground;

        private readonly Thread[] _procThreads;
        private volatile int _activeThreadCount;

        private readonly Queues.IQueue<T> _queue;
        private readonly Collections.Concurrent.BlockingQueue<T> _blockingQueue;

        private readonly ManualResetEventSlim _stoppedEvent;
        private CancellationTokenSource _stopRequestedCancelation;
        private CancellationTokenSource _stoppedCancelation;

        private int _state;
        private volatile bool _completeAdding;
        private volatile bool _letFinishProcess;


        /// <summary>
        /// QueueAsyncProcessor constructor
        /// </summary>
        /// <param name="threadCount">Number of processing threads</param>
        /// <param name="queue">Processing queue (current instances of <see cref="QueueAsyncProcessor{T}"/> becomes the owner)</param>
        /// <param name="name">The name for this instance of <see cref="QueueAsyncProcessor{T}"/> and its threads</param>
        /// <param name="isBackground">Whether or not processing threads are background threads</param>
        public QueueAsyncProcessor(int threadCount, IQueue<T> queue, string name, bool isBackground)
        {
            if (threadCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(threadCount), "Number of threads should be positive");
            if (queue == null)
                throw new ArgumentNullException(nameof(queue));

            _isBackground = isBackground;
            _name = name ?? this.GetType().GetCSName();

            _procThreads = new Thread[threadCount];
            _activeThreadCount = 0;

            _queue = queue;
            _blockingQueue = null; // Should work through interface only

            _stoppedEvent = new ManualResetEventSlim(false);
            _stopRequestedCancelation = null;
            _stoppedCancelation = null;

            _state = (int)QueueAsyncProcessorState.Created;
            _completeAdding = false;
            _letFinishProcess = false;

            Profiling.Profiler.QueueAsyncProcessorCreated(this.Name);
        }
        /// <summary>
        /// QueueAsyncProcessor constructor
        /// </summary>
        /// <param name="threadCount">Number of processing threads</param>
        /// <param name="queue">Processing queue (current instances of <see cref="QueueAsyncProcessor{T}"/> becomes the owner)</param>
        /// <param name="name">The name for this instance of <see cref="QueueAsyncProcessor{T}"/> and its threads</param>
        public QueueAsyncProcessor(int threadCount, IQueue<T> queue, string name)
            : this(threadCount, queue, name, false)
        {
        }

        /// <summary>
        /// QueueAsyncProcessor constructor
        /// </summary>
        /// <param name="threadCount">Number of processing threads</param>
        /// <param name="maxQueueSize">The bounded size of the queue (if less or equal to 0 then no limitation)</param>
        /// <param name="name">The name for this instance of <see cref="QueueAsyncProcessor{T}"/> and its threads</param>
        /// <param name="isBackground">Whether or not processing threads are background threads</param>
        public QueueAsyncProcessor(int threadCount, int maxQueueSize, string name, bool isBackground)
        {
            if (threadCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(threadCount), "Number of threads should be positive");

            _isBackground = isBackground;
            _name = name ?? this.GetType().GetCSName();

            _procThreads = new Thread[threadCount];
            _activeThreadCount = 0;

            _queue = new MemoryQueue<T>(maxQueueSize > 0 ? maxQueueSize : -1);
            _blockingQueue = _queue as Collections.Concurrent.BlockingQueue<T>;

            _stoppedEvent = new ManualResetEventSlim(false);
            _stopRequestedCancelation = null;
            _stoppedCancelation = null;

            _state = (int)QueueAsyncProcessorState.Created;
            _completeAdding = false;
            _letFinishProcess = false;

            Profiling.Profiler.QueueAsyncProcessorCreated(this.Name);
        }
        /// <summary>
        /// QueueAsyncProcessor constructor
        /// </summary>
        /// <param name="threadCount">Number of processing threads</param>
        /// <param name="maxQueueSize">The bounded size of the queue (if less or equal to 0 then no limitation)</param>
        /// <param name="name">The name for this instance of <see cref="QueueAsyncProcessor{T}"/> and its threads</param>
        public QueueAsyncProcessor(int threadCount, int maxQueueSize, string name)
            : this(threadCount, maxQueueSize, name, false)
        {
        }
        /// <summary>
        /// QueueAsyncProcessor constructor (for unlimited queue capacity)
        /// </summary>
        /// <param name="threadCount">Number of processing threads</param>
        /// <param name="name">The name for this instance of <see cref="QueueAsyncProcessor{T}"/> and its threads</param>
        public QueueAsyncProcessor(int threadCount, string name)
            : this(threadCount, -1, name, false)
        {
        }
        /// <summary>
        /// QueueAsyncProcessor constructor (for unlimited queue capacity)
        /// </summary>
        /// <param name="threadCount">Number of processing threads</param>
        public QueueAsyncProcessor(int threadCount)
            : this(threadCount, -1, null, false)
        {
        }

        /// <summary>
        /// Current state
        /// </summary>
        public QueueAsyncProcessorState State
        {
            get { return (QueueAsyncProcessorState)Volatile.Read(ref _state); }
        }
        /// <summary>
        /// Whether the <see cref="QueueAsyncProcessor{T}"/> is running and can process work items
        /// </summary>
        public bool IsWork
        {
            get { return State == QueueAsyncProcessorState.Running; }
        }
        /// <summary>
        /// Whether the stop was requested and the processor should complete item processing
        /// </summary>
        protected bool IsStopRequested
        {
            get { return State == QueueAsyncProcessorState.StopRequested; }
        }
        /// <summary>
        /// Whether the <see cref="QueueAsyncProcessor{T}"/> was stopped
        /// </summary>
        protected bool IsStopped
        {
            get { return State == QueueAsyncProcessorState.Stopped; }
        }
        /// <summary>
        /// Whether the stop was requested or already stopped
        /// </summary>
        protected bool IsStopRequestedOrStopped
        {
            get
            {
                var state = State;
                return state == QueueAsyncProcessorState.Stopped || state == QueueAsyncProcessorState.StopRequested;
            }
        }
        /// <summary>
        /// Is items queue marked as Completed for Adding (no new item can be added)
        /// </summary>
        public bool IsAddingCompleted
        {
            get { return _completeAdding; }
        }
        /// <summary>
        /// Whether the user specified that all existed items should be processed before stop
        /// </summary>
        protected bool LetFinishedProcess
        {
            get { return _letFinishProcess; }
        }

        /// <summary>
        /// The name for this instance of <see cref="QueueAsyncProcessor{T}"/>
        /// </summary>
        public string Name
        {
            get { return _name; }
        }

        /// <summary>
        /// Whether or not processing threads are background threads
        /// </summary>
        public bool IsBackground
        {
            get { return _isBackground; }
        }
        /// <summary>
        /// Number of processing threads running right now
        /// </summary>
        protected internal int ActiveThreadCount
        {
            get { return _activeThreadCount; }
        }
        /// <summary>
        /// Number of processing threads
        /// </summary>
        public int ThreadCount
        {
            get { return _procThreads.Length; }
        }

        /// <summary>
        /// Number of items inside processing queue
        /// </summary>
        public int ElementCount
        {
            get
            {
                if (_blockingQueue != null)
                    return _blockingQueue.Count;

                long longCount = _queue.Count;
                if (longCount > int.MaxValue)
                    return int.MaxValue;
                else if (longCount < 0)
                    return -1;

                return (int)longCount;
            }
        }
        /// <summary>
        /// Number of items inside processing queue
        /// </summary>
        public long ElementCountLong
        {
            get { return _queue.Count; }
        }

        /// <summary>
        /// The bounded size of the queue (if less or equal to 0 then no limitation)
        /// </summary>
        public int QueueCapacity
        {
            get
            {
                if (_blockingQueue != null)
                    return _blockingQueue.BoundedCapacity;

                long longCapacity = _queue.BoundedCapacity;
                if (longCapacity > int.MaxValue)
                    return int.MaxValue;
                else if (longCapacity < 0)
                    return -1;

                return (int)longCapacity;
            }
        }




        /// <summary>
        /// Verifies that state transition is possible
        /// </summary>
        /// <param name="oldState">Current state</param>
        /// <param name="newState">New state</param>
        /// <returns>True when state transition can be performed</returns>
        private bool IsValidStateTransition(QueueAsyncProcessorState oldState, QueueAsyncProcessorState newState)
        {
            switch (oldState)
            {
                case QueueAsyncProcessorState.Created:
                    return newState == QueueAsyncProcessorState.StartRequested || newState == QueueAsyncProcessorState.StopRequested;
                case QueueAsyncProcessorState.StartRequested:
                    return newState == QueueAsyncProcessorState.Running || newState == QueueAsyncProcessorState.Stopped;
                case QueueAsyncProcessorState.Running:
                    return newState == QueueAsyncProcessorState.StopRequested;
                case QueueAsyncProcessorState.StopRequested:
                    return newState == QueueAsyncProcessorState.Stopped;
                case QueueAsyncProcessorState.Stopped:
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
        private bool ChangeStateSafe(QueueAsyncProcessorState newState, out QueueAsyncProcessorState prevState)
        {
            prevState = (QueueAsyncProcessorState)Volatile.Read(ref _state);

            if (!IsValidStateTransition(prevState, newState))
                return false;

            SpinWait sw = new SpinWait();
            while (Interlocked.CompareExchange(ref _state, (int)newState, (int)prevState) != (int)prevState)
            {
                sw.SpinOnce();
                prevState = (QueueAsyncProcessorState)Volatile.Read(ref _state);
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
            if (State == QueueAsyncProcessorState.Stopped)
                throw new ObjectDisposedException(this.GetType().Name, "QueueAsyncProcessor is Stopped");
        }
        /// <summary>
        /// Checks whether the current instance is in Stopped or StopRequested state and throws ObjectDisposedException when it is
        /// </summary>
        protected void CheckPendingDisposeOrDisposed()
        {
            var state = State;
            if (state == QueueAsyncProcessorState.Stopped || state == QueueAsyncProcessorState.StopRequested)
                throw new ObjectDisposedException(this.GetType().Name, "QueueAsyncProcessor has Stopped or StopRequested state");
        }

        /// <summary>
        /// Adds new item to the processing queue, even when the bounded capacity reached
        /// </summary>
        /// <param name="element">New item</param>
        protected void AddForced(T element)
        {
            _queue.AddForced(element);
            if (Profiling.Profiler.IsProfilingEnabled)
                Profiling.Profiler.QueueAsyncProcessorElementCountIncreased(this.Name, ElementCount, QueueCapacity);
        }

        /// <summary>
        /// Attempts to add new item to processing queue
        /// </summary>
        /// <param name="element">New item</param>
        /// <param name="timeout">Adding timeout in milliseconds</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>True if item was added, otherwise false</returns>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public override bool Add(T element, int timeout, CancellationToken token)
        {
            CheckDisposed();
            if (IsAddingCompleted)
                throw new InvalidOperationException("Adding was completed for QueueAsyncProcessor: " + this.Name);

            var result = _queue.TryAdd(element, timeout, token);
            if (Profiling.Profiler.IsProfilingEnabled)
            {
                if (result)
                    Profiling.Profiler.QueueAsyncProcessorElementCountIncreased(this.Name, ElementCount, QueueCapacity);
                else
                    Profiling.Profiler.QueueAsyncProcessorElementRejectedInTryAdd(this.Name, ElementCount);
            }
            return result;
        }



        /// <summary>
        /// Starts all processing threads and changes state to <see cref="QueueAsyncProcessorState.Running"/>
        /// </summary>
        /// <exception cref="ObjectDisposedException">Object was disposed</exception>
        /// <exception cref="WrongStateException">Can't start processor because it is not in <see cref="QueueAsyncProcessorState.Created"/> state</exception>
        public virtual void Start()
        {
            QueueAsyncProcessorState prevState;
            if (!ChangeStateSafe(QueueAsyncProcessorState.StartRequested, out prevState))
            {
                if (prevState == QueueAsyncProcessorState.Stopped)
                    throw new ObjectDisposedException(this.GetType().Name);
                throw new WrongStateException("Can't start QueueAsyncProcessor cause it is in wrong state: " + prevState.ToString());
            }

            try
            {
                _stoppedEvent.Reset();
                _stopRequestedCancelation = new CancellationTokenSource();
                _stoppedCancelation = new CancellationTokenSource();

                _completeAdding = false;
                _letFinishProcess = false;

                for (int i = 0; i < _procThreads.Length; i++)
                {
                    _procThreads[i] = new Thread(new ThreadStart(ThreadProcFunc));
                    _procThreads[i].IsBackground = _isBackground;
                    _procThreads[i].Name = string.Format("{0} (#{1})", _name, i.ToString());
                }

                for (int i = 0; i < _procThreads.Length; i++)
                    _procThreads[i].Start();

                bool changeStateToRunningSuccess = ChangeStateSafe(QueueAsyncProcessorState.Running, out prevState);
                TurboContract.Assert(changeStateToRunningSuccess && prevState == QueueAsyncProcessorState.StartRequested, conditionString: "changeStateToRunningSuccess && prevState == QueueAsyncProcessorState.StartRequested");
            }
            catch
            {
                ChangeStateSafe(QueueAsyncProcessorState.Stopped, out prevState);

                _stoppedEvent.Set();
                _stopRequestedCancelation.Cancel();
                _stoppedCancelation.Cancel();
                _completeAdding = true;

                throw;
            }
        }

        /// <summary>
        /// Gets the CancellationToken that will be cancelled when a stop is requested 
        /// </summary>
        /// <returns>Cancellation token</returns>
        protected CancellationToken GetStopRequestedCancellationToken()
        {
            var tokenSrc = this._stopRequestedCancelation;
            if (tokenSrc != null)
                return tokenSrc.Token;
            return new CancellationToken(true);
        }
        /// <summary>
        /// Gets the CancellationToken that will be cancelled when a stop should be completed immediately (without <see cref="LetFinishedProcess"/> flag)
        /// </summary>
        /// <returns>Cancellation token</returns>
        protected CancellationToken GetStoppedCancellationToken()
        {
            var tokenSrc = this._stoppedCancelation;
            if (tokenSrc != null)
                return tokenSrc.Token;
            return new CancellationToken(true);
        }


        /// <summary>
        /// Attempts to take item from inner queue
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryTakeFromQueue(out T item)
        {
            return _queue.TryTake(out item, 0, default(CancellationToken));
        }
        /// <summary>
        /// Attempts to take item from inner queue
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryTakeFromQueue(out T item, int timeout, CancellationToken token)
        {
            var blockQ = _blockingQueue;
            if (blockQ != null)
                return blockQ.TryTake(out item, timeout, token, false);

            return _queue.TryTake(out item, timeout, token);
        }


        /// <summary>
        /// Main thread procedure
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCode]
        private void ThreadProcFunc()
        {
            CancellationToken stopRequestedToken = GetStopRequestedCancellationToken();
            CancellationToken stoppedToken = GetStoppedCancellationToken();

            object state = null;        

            try
            {
                Interlocked.Increment(ref _activeThreadCount);
                Profiling.Profiler.QueueAsyncProcessorThreadStart(this.Name, this.ActiveThreadCount, this.ThreadCount);

                state = this.Prepare();
                Profiling.ProfilingTimer timer = new Profiling.ProfilingTimer();
                timer.StartTime();


                while (!stopRequestedToken.IsCancellationRequested)
                {
                    try
                    {
                        T elem = default(T);
                        while (!stopRequestedToken.IsCancellationRequested)
                        {
                            elem = default(T);
                            if (TryTakeFromQueue(out elem, Timeout.Infinite, stopRequestedToken))
                            {
                                if (Profiling.Profiler.IsProfilingEnabled)
                                    Profiling.Profiler.QueueAsyncProcessorElementCountDecreased(this.Name, ElementCount, QueueCapacity);

                                timer.RestartTime();

                                this.Process(elem, state, stoppedToken);

                                if (Profiling.Profiler.IsProfilingEnabled)
                                    Profiling.Profiler.QueueAsyncProcessorElementProcessed(this.Name, timer.GetTime());
                            }
                            else
                            {
                                TurboContract.Assert(stopRequestedToken.IsCancellationRequested, conditionString: "stopRequestedToken.IsCancellationRequested");
                            }
                        }
                    }
                    catch (OperationCanceledException opEx)
                    {
                        if (!stopRequestedToken.IsCancellationRequested)
                            if (!ProcessThreadException(opEx))
                                throw;
                    }
                    catch (Exception ex)
                    {
                        if (ex.GetType() == typeof(ThreadAbortException) || ex.GetType() == typeof(ThreadInterruptedException) || ex.GetType() == typeof(StackOverflowException) || ex.GetType() == typeof(OutOfMemoryException))
                            throw;

                        if (!ProcessThreadException(ex))
                            throw;
                    }
                }


                if (_letFinishProcess)
                {
                    while (!stoppedToken.IsCancellationRequested)
                    {
                        try
                        {
                            T elem = default(T);
                            while (!stoppedToken.IsCancellationRequested && TryTakeFromQueue(out elem))
                            {
                                if (Profiling.Profiler.IsProfilingEnabled)
                                    Profiling.Profiler.QueueAsyncProcessorElementCountDecreased(this.Name, ElementCount, QueueCapacity);

                                timer.RestartTime();
                                this.Process(elem, state, stoppedToken);
                                if (Profiling.Profiler.IsProfilingEnabled)
                                    Profiling.Profiler.QueueAsyncProcessorElementProcessed(this.Name, timer.GetTime());
                            }

                            if (_queue.Count == 0)
                                break;
                        }
                        catch (OperationCanceledException opEx)
                        {
                            if (!stoppedToken.IsCancellationRequested)
                                if (!ProcessThreadException(opEx))
                                    throw;
                        }
                        catch (Exception ex)
                        {
                            if (ex.GetType() == typeof(ThreadAbortException) || ex.GetType() == typeof(ThreadInterruptedException) || ex.GetType() == typeof(StackOverflowException) || ex.GetType() == typeof(OutOfMemoryException))
                                throw;

                            if (!ProcessThreadException(ex))
                                throw;
                        }
                    }
                }
            }
            finally
            {
                this.Finalize(state);

                if (Interlocked.Decrement(ref _activeThreadCount) <= 0)
                {
                    // Вынуждены ждать
                    SpinWait sw = new SpinWait();
                    while (State == QueueAsyncProcessorState.StartRequested)
                        sw.SpinOnce();

                    if (State == QueueAsyncProcessorState.StopRequested)
                    {
                        QueueAsyncProcessorState prevState;
                        if (ChangeStateSafe(QueueAsyncProcessorState.Stopped, out prevState))
                        {
                            TurboContract.Assert(prevState == QueueAsyncProcessorState.StopRequested, conditionString: "prevState == QueueAsyncProcessorState.StopRequested");
                            _stoppedEvent.Set();
                            this.DisposeQueue(); // Can throw exception
                            Profiling.Profiler.QueueAsyncProcessorDisposed(this.Name, false);
                        }
                    }
                }

                Profiling.Profiler.QueueAsyncProcessorThreadStop(this.Name, this.ActiveThreadCount, this.ThreadCount);
            }
        }


        /// <summary>
        /// Method that allows to process unhandled exceptions (e.g. logging).
        /// Default behaviour - throws <see cref="QueueAsyncProcessorException"/>.
        /// </summary>
        /// <param name="ex">Catched exception</param>
        /// <returns>Whether the current exception can be safely skipped (false - the thread will retrow the exception)</returns>
        protected virtual bool ProcessThreadException(Exception ex)
        {
            TurboContract.Requires(ex != null, conditionString: "ex != null");

            throw new QueueAsyncProcessorException("Unhandled exception during processing in QueueAsyncProcessor ('" + this.Name + "')", ex);
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
        /// Processes a single item taken from the processing queue.
        /// </summary>
        /// <remarks>
        /// Cancellation token is cancelled only when immediate stop is requested (LetFinishProcess is false).
        /// For that case it is improtant to manually check for <see cref="QueueAsyncProcessorState.StartRequested"/> state or use token from <see cref="GetStopRequestedCancellationToken"/>.
        /// </remarks>
        /// <param name="element">Item to be processed</param>
        /// <param name="state">Thread specific state object initialized by <see cref="Prepare"/> method</param>
        /// <param name="token">Cancellation token that will be cancelled when the immediate stop is requested (see <see cref="GetStoppedCancellationToken"/>)</param>
        protected abstract void Process(T element, object state, CancellationToken token);

        /// <summary>
        /// Release the thread specific state object when the thread is about to exit
        /// </summary>
        /// <param name="state">Thread-specific state object</param>
        protected virtual void Finalize(object state)
        {
        }


        /// <summary>
        /// Marks that new items cannot be added to processing queue
        /// </summary>
        public void CompleteAdding()
        {
            _completeAdding = true;
        }

        /// <summary>
        /// Blocks the current thread and waits for all processing threads to complete
        /// </summary>
        public void WaitUntilStop()
        {
            if (State == QueueAsyncProcessorState.Stopped)
                return;

            _stoppedEvent.Wait();
        }

        /// <summary>
        /// Blocks the current thread and waits for all processing threads to complete
        /// </summary>
        /// <param name="timeout">Waiting timeout in milliseconds</param>
        /// <returns>True when all threads completed in time</returns>
        public bool WaitUntilStop(int timeout)
        {
            if (State == QueueAsyncProcessorState.Stopped)
                return true;

            return _stoppedEvent.Wait(timeout);
        }


        /// <summary>
        /// Stops processing of items and changes state to <see cref="QueueAsyncProcessorState.StopRequested"/>
        /// </summary>
        /// <param name="waitForStop">Whether the current thread should be blocked until all processing threads are be completed</param>
        /// <param name="letFinishProcess">Whether all items that have already been added must be processed before stopping</param>
        /// <param name="completeAdding">Marks that new items cannot be added to processing queue</param>
        /// <returns>Is stopping process triggered</returns>
        private bool StopProcessor(bool waitForStop, bool letFinishProcess, bool completeAdding)
        {
            if (this.IsStopRequestedOrStopped)
            {
                if (completeAdding)
                {
                    _completeAdding = completeAdding;
                }
                if (!letFinishProcess)
                {
                    _letFinishProcess = letFinishProcess;
                    _stoppedCancelation.Cancel();
                }
                if (waitForStop)
                {
                    this.WaitUntilStop();
                }

                return false;
            }

            QueueAsyncProcessorState prevState;
            if (!ChangeStateSafe(QueueAsyncProcessorState.StopRequested, out prevState))
            {
                if (prevState != QueueAsyncProcessorState.StartRequested)
                {
                    if (waitForStop)
                        this.WaitUntilStop();

                    return false;
                }

                SpinWait sw = new SpinWait();
                while (State == QueueAsyncProcessorState.StartRequested)
                    sw.SpinOnce();

                if (!ChangeStateSafe(QueueAsyncProcessorState.StopRequested, out prevState))
                {
                    if (waitForStop)
                        this.WaitUntilStop();

                    return false;
                }
            }

            _completeAdding = completeAdding;
            _letFinishProcess = letFinishProcess;

            TurboContract.Assert(_stopRequestedCancelation != null || prevState == QueueAsyncProcessorState.Created, conditionString: "_stopRequestedCancelation != null || prevState == QueueAsyncProcessorState.Created");
            TurboContract.Assert(_stoppedCancelation != null || prevState == QueueAsyncProcessorState.Created, conditionString: "_stoppedCancelation != null || prevState == QueueAsyncProcessorState.Created");

            if (_stopRequestedCancelation != null)
                _stopRequestedCancelation.Cancel();
            if (!letFinishProcess && _stoppedCancelation != null)
                _stoppedCancelation.Cancel();

            if (waitForStop && prevState != QueueAsyncProcessorState.Created)
            {
                for (int i = 0; i < _procThreads.Length; i++)
                {
                    if (_procThreads[i] != null)
                        _procThreads[i].Join();
                }
            }

            for (int i = 0; i < _procThreads.Length; i++)
                _procThreads[i] = null;

            if (ActiveThreadCount == 0)
            {
                if (ChangeStateSafe(QueueAsyncProcessorState.Stopped, out prevState))
                {
                    TurboContract.Assert(prevState == QueueAsyncProcessorState.StopRequested, conditionString: "prevState == QueueAsyncProcessorState.StopRequested");
                    _stoppedEvent.Set();
                    this.DisposeQueue(); // Can throw exception
                    Profiling.Profiler.QueueAsyncProcessorDisposed(this.Name, false);
                }
            }

            TurboContract.Assert(State == QueueAsyncProcessorState.StopRequested || State == QueueAsyncProcessorState.Stopped, conditionString: "State == QueueAsyncProcessorState.StopRequested || State == QueueAsyncProcessorState.Stopped");
            TurboContract.Assert(!waitForStop || State == QueueAsyncProcessorState.Stopped, conditionString: "!waitForStop || State == QueueAsyncProcessorState.Stopped");
            return true;
        }


        /// <summary>
        /// Stops processing of items and changes state to <see cref="QueueAsyncProcessorState.StopRequested"/>
        /// </summary>
        /// <param name="waitForStop">Whether the current thread should be blocked until all processing threads are be completed</param>
        /// <param name="letFinishProcess">Whether all items that have already been added must be processed before stopping</param>
        /// <param name="completeAdding">Marks that new items cannot be added to processing queue</param>
        public virtual void Stop(bool waitForStop, bool letFinishProcess, bool completeAdding)
        {
            StopProcessor(waitForStop, letFinishProcess, completeAdding);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Stops processing of items and changes state to <see cref="QueueAsyncProcessorState.StopRequested"/>
        /// </summary>
        public void Stop()
        {
            this.Stop(true, true, true);
        }

        /// <summary>
        /// Cleans-up inner queue when it was passed by user
        /// </summary>
        private void DisposeQueue()
        {
            if (_blockingQueue == null && _queue != null)
                _queue.Dispose();
        }

        /// <summary>
        /// Cleans-up resources
        /// </summary>
        /// <param name="isUserCall">Is it called explicitly by user (False - from finalizer)</param>
        protected override void Dispose(bool isUserCall)
        {
            if (!this.IsStopRequestedOrStopped)
            {
                TurboContract.Assert(isUserCall, "QueueAsyncProcessor destructor: Better to dispose by user");

                if (isUserCall)
                    StopProcessor(true, false, true);

                QueueAsyncProcessorState prevState;
                ChangeStateSafe(QueueAsyncProcessorState.StopRequested, out prevState);
                ChangeStateSafe(QueueAsyncProcessorState.Stopped, out prevState);

                if (_stopRequestedCancelation != null)
                    _stopRequestedCancelation.Cancel();
                if (_stoppedCancelation != null)
                    _stoppedCancelation.Cancel();
                this.DisposeQueue();

                if (!isUserCall)
                    Profiling.Profiler.QueueAsyncProcessorDisposed(this.Name, true);
            }
            base.Dispose(isUserCall);
        }

#if DEBUG
        /// <summary>
        /// Finalizer
        /// </summary>
        ~QueueAsyncProcessor()
        {
            Dispose(false);
        }
#endif
    }
}
