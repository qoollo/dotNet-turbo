using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Threading.ThreadPools.Common
{
    /// <summary>
    /// Thread pool work item that wraps <see cref="Action"/>
    /// </summary>
    public sealed class ActionThreadPoolWorkItem: ThreadPoolWorkItem
    {
        private readonly Action _action;

        /// <summary>
        /// <see cref="ActionThreadPoolWorkItem"/> constructor
        /// </summary>
        /// <param name="action">Delegate to execute work</param>
        /// <param name="allowExecutionContextFlow">Indicates whether the ExecutionContext should be captured</param>
        /// <param name="preferFairness">Indicates whether this work item should alwayes be enqueued to the GlobalQueue</param>
        public ActionThreadPoolWorkItem(Action action, bool allowExecutionContextFlow, bool preferFairness)
            : base(allowExecutionContextFlow, preferFairness)
        {
            TurboContract.Requires(action != null, conditionString: "action != null");
            _action = action;
        }
        /// <summary>
        /// <see cref="ActionThreadPoolWorkItem"/> constructor
        /// </summary>
        /// <param name="action">Delegate to execute work</param>
        public ActionThreadPoolWorkItem(Action action)
            : this(action, true, false)
        {
        }

        /// <summary>
        /// Runs this work item (internal method)
        /// </summary>
        protected sealed override void RunInner()
        {
            _action();
        }
    }

    /// <summary>
    /// Thread pool work item that wraps <see cref="Action{T}"/>
    /// </summary>
    /// <typeparam name="T">Type of the state object passed to delegate</typeparam>
    public sealed class ActionThreadPoolWorkItem<T> : ThreadPoolWorkItem
    {
        private readonly Action<T> _action;
        private readonly T _state;

        /// <summary>
        /// <see cref="ActionThreadPoolWorkItem{T}"/> constructor
        /// </summary>
        /// <param name="action">Delegate to execute work</param>
        /// <param name="state">State object that will be passed to delegate</param>
        /// <param name="allowExecutionContextFlow">Indicates whether the ExecutionContext should be captured</param>
        /// <param name="preferFairness">Indicates whether this work item should alwayes be enqueued to the GlobalQueue</param>
        public ActionThreadPoolWorkItem(Action<T> action, T state, bool allowExecutionContextFlow, bool preferFairness)
            : base(allowExecutionContextFlow, preferFairness)
        {
            TurboContract.Requires(action != null, conditionString: "action != null");
            _action = action;
            _state = state;
        }
        /// <summary>
        /// <see cref="ActionThreadPoolWorkItem{T}"/> constructor
        /// </summary>
        /// <param name="action">Delegate to execute work</param>
        /// <param name="state">Параметр</param>
        public ActionThreadPoolWorkItem(Action<T> action, T state)
            : this(action, state, true, false)
        {

        }

        /// <summary>
        /// Runs this work item (internal method)
        /// </summary>
        protected sealed override void RunInner()
        {
            _action(_state);
        }
    }


    /// <summary>
    /// Thread pool work item that wraps <see cref="Action"/> and additionally maintains <see cref="System.Threading.Tasks.Task"/>
    /// </summary>
    public sealed class TaskThreadPoolWorkItem : ThreadPoolWorkItem
    {
        private readonly Action _action;
        private readonly TaskCompletionSource<object> _completionSource;

        /// <summary>
        /// <see cref="TaskThreadPoolWorkItem"/> constructor
        /// </summary>
        /// <param name="action">Delegate to execute work</param>
        /// <param name="creationOptions">Task creation options</param>
        public TaskThreadPoolWorkItem(Action action, TaskCreationOptions creationOptions)
            : base(true, (creationOptions & TaskCreationOptions.PreferFairness) != 0)
        {
            TurboContract.Requires(action != null, conditionString: "action != null");
            _action = action;
            _completionSource = new TaskCompletionSource<object>(creationOptions);
        }
        /// <summary>
        /// <see cref="TaskThreadPoolWorkItem"/> constructor
        /// </summary>
        /// <param name="action">Delegate to execute work</param>
        public TaskThreadPoolWorkItem(Action action)
            : this(action, TaskCreationOptions.None)
        {
        }

        /// <summary>
        /// Encapsulated Task
        /// </summary>
        public Task Task { get { return _completionSource.Task; } }

        /// <summary>
        /// Runs this work item (internal method)
        /// </summary>
        protected sealed override void RunInner()
        {
            try
            {
                _action();
                _completionSource.SetResult(null);
            }
            catch (Exception ex)
            {
                _completionSource.SetException(ex);
            }
        }

        /// <summary>
        /// Notifies that work item was cancelled (internal method)
        /// </summary>
        protected sealed override void CancelInner()
        {
            _completionSource.SetCanceled();
        }
    }

    /// <summary>
    /// Thread pool work item that wraps <see cref="Action{T}"/> and additionally maintains <see cref="System.Threading.Tasks.Task{T}"/>
    /// </summary>
    /// <typeparam name="TState">Type of the state object passed to delegate</typeparam>
    public sealed class TaskThreadPoolWorkItem<TState> : ThreadPoolWorkItem
    {
        private readonly Action<TState> _action;
        private readonly TState _state;
        private readonly TaskCompletionSource<object> _completionSource;

        /// <summary>
        /// <see cref="TaskThreadPoolWorkItem{TState}"/> constructor
        /// </summary>
        /// <param name="action">Delegate to execute work</param>
        /// <param name="state">State object that will be passed to delegate</param>
        /// <param name="creationOptions">Task creation options</param>
        public TaskThreadPoolWorkItem(Action<TState> action, TState state, TaskCreationOptions creationOptions)
            : base(true, (creationOptions & TaskCreationOptions.PreferFairness) != 0)
        {
            TurboContract.Requires(action != null, conditionString: "action != null");
            _action = action;
            _state = state;
            _completionSource = new TaskCompletionSource<object>(creationOptions);
        }
        /// <summary>
        /// <see cref="TaskThreadPoolWorkItem{TState}"/> constructor
        /// </summary>
        /// <param name="action">Delegate to execute work</param>
        /// <param name="state">State object that will be passed to delegate</param>
        public TaskThreadPoolWorkItem(Action<TState> action, TState state)
            : this(action, state, TaskCreationOptions.None)
        {
        }

        /// <summary>
        /// Encapsulated Task
        /// </summary>
        public Task Task { get { return _completionSource.Task; } }

        /// <summary>
        /// Runs this work item (internal method)
        /// </summary>
        protected sealed override void RunInner()
        {
            try
            {
                _action(_state);
                _completionSource.SetResult(null);
            }
            catch (Exception ex)
            {
                _completionSource.SetException(ex);
            }
        }

        /// <summary>
        /// Notifies that work item was cancelled (internal method)
        /// </summary>
        protected sealed override void CancelInner()
        {
            _completionSource.SetCanceled();
        }
    }


    /// <summary>
    /// Thread pool work item that wraps <see cref="Func{TResult}"/> and additionally maintains <see cref="System.Threading.Tasks.Task{T}"/>
    /// </summary>
    /// <typeparam name="TRes">Type of the result</typeparam>
    public sealed class TaskFuncThreadPoolWorkItem<TRes> : ThreadPoolWorkItem
    {
        private readonly Func<TRes> _action;
        private readonly TaskCompletionSource<TRes> _completionSource;

        /// <summary>
        /// <see cref="TaskFuncThreadPoolWorkItem{TRes}"/> constructor
        /// </summary>
        /// <param name="action">Delegate to execute work</param>
        /// <param name="creationOptions">Task creation options</param>
        public TaskFuncThreadPoolWorkItem(Func<TRes> action, TaskCreationOptions creationOptions)
            : base(true, (creationOptions & TaskCreationOptions.PreferFairness) != 0)
        {
            TurboContract.Requires(action != null, conditionString: "action != null");
            _action = action;
            _completionSource = new TaskCompletionSource<TRes>(creationOptions);
        }
        /// <summary>
        /// <see cref="TaskFuncThreadPoolWorkItem{TRes}"/> constructor
        /// </summary>
        /// <param name="action">Delegate to execute work</param>
        public TaskFuncThreadPoolWorkItem(Func<TRes> action)
            : this(action, TaskCreationOptions.None)
        {

        }

        /// <summary>
        /// Encapsulated Task
        /// </summary>
        public Task<TRes> Task { get { return _completionSource.Task; } }

        /// <summary>
        /// Runs this work item (internal method)
        /// </summary>
        protected sealed override void RunInner()
        {
            try
            {
                var result = _action();
                _completionSource.SetResult(result);
            }
            catch (Exception ex)
            {
                _completionSource.SetException(ex);
            }
        }

        /// <summary>
        /// Notifies that work item was cancelled (internal method)
        /// </summary>
        protected sealed override void CancelInner()
        {
            _completionSource.SetCanceled();
        }
    }

    /// <summary>
    /// Thread pool work item that wraps <see cref="Func{T, TResult}"/> and additionally maintains <see cref="System.Threading.Tasks.Task{T}"/>
    /// </summary>
    /// <typeparam name="TState">Type of the state object passed to delegate</typeparam>
    /// <typeparam name="TRes">Type of the result</typeparam>
    public sealed class TaskFuncThreadPoolWorkItem<TState, TRes> : ThreadPoolWorkItem
    {
        private readonly Func<TState, TRes> _action;
        private readonly TState _state;
        private readonly TaskCompletionSource<TRes> _completionSource;

        /// <summary>
        /// <see cref="TaskFuncThreadPoolWorkItem{TState, TRes}"/> constructor
        /// </summary>
        /// <param name="action">Delegate to execute work</param>
        /// <param name="state">State object that will be passed to delegate</param>
        /// <param name="creationOptions">Task creation options</param>
        public TaskFuncThreadPoolWorkItem(Func<TState, TRes> action, TState state, TaskCreationOptions creationOptions)
            : base(true, (creationOptions & TaskCreationOptions.PreferFairness) != 0)
        {
            TurboContract.Requires(action != null, conditionString: "action != null");
            _action = action;
            _state = state;
            _completionSource = new TaskCompletionSource<TRes>(creationOptions);
        }
        /// <summary>
        /// <see cref="TaskFuncThreadPoolWorkItem{TState, TRes}"/> constructor
        /// </summary>
        /// <param name="action">Delegate to execute work</param>
        /// <param name="state">State object that will be passed to delegate</param>
        public TaskFuncThreadPoolWorkItem(Func<TState, TRes> action, TState state)
            : this(action, state, TaskCreationOptions.None)
        {

        }

        /// <summary>
        /// Encapsulated Task
        /// </summary>
        public Task<TRes> Task { get { return _completionSource.Task; } }

        /// <summary>
        /// Runs this work item (internal method)
        /// </summary>
        protected sealed override void RunInner()
        {
            try
            {
                var result = _action(_state);
                _completionSource.SetResult(result);
            }
            catch (Exception ex)
            {
                _completionSource.SetException(ex);
            }
        }

        /// <summary>
        /// Notifies that work item was cancelled (internal method)
        /// </summary>
        protected sealed override void CancelInner()
        {
            _completionSource.SetCanceled();
        }
    }
    

    /// <summary>
    /// Thread pool work item that wraps <see cref="SendOrPostCallback"/>
    /// </summary>
    public sealed class SendOrPostCallbackThreadPoolWorkItem : ThreadPoolWorkItem
    {
        private readonly SendOrPostCallback _action;
        private readonly object _state;

        /// <summary>
        /// <see cref="SendOrPostCallbackThreadPoolWorkItem"/> constructor
        /// </summary>
        /// <param name="action">Delegate to execute work</param>
        /// <param name="state">State object that will be passed to delegate</param>
        /// <param name="allowExecutionContextFlow">Indicates whether the ExecutionContext should be captured</param>
        /// <param name="preferFairness">Indicates whether this work item should alwayes be enqueued to the GlobalQueue</param>
        public SendOrPostCallbackThreadPoolWorkItem(SendOrPostCallback action, object state, bool allowExecutionContextFlow, bool preferFairness)
            : base(allowExecutionContextFlow, preferFairness)
        {
            TurboContract.Requires(action != null, conditionString: "action != null");
            _action = action;
            _state = state;
        }
        /// <summary>
        /// <see cref="SendOrPostCallbackThreadPoolWorkItem"/> constructor
        /// </summary>
        /// <param name="action">Delegate to execute work</param>
        /// <param name="state">State object that will be passed to delegate</param>
        public SendOrPostCallbackThreadPoolWorkItem(SendOrPostCallback action, object state)
            : this(action, state, true, false)
        {
        }

        /// <summary>
        /// Runs this work item (internal method)
        /// </summary>
        protected sealed override void RunInner()
        {
            _action(_state);
        }
    }

    /// <summary>
    /// Thread pool work item that wraps <see cref="SendOrPostCallback"/> and has the possiblity to wait for completion.
    /// Required to implement <see cref="SynchronizationContext.Send(SendOrPostCallback, object)"/>
    /// </summary>
    public sealed class SendOrPostCallbackSyncThreadPoolWorkItem : ThreadPoolWorkItem
    {
        private readonly SendOrPostCallback _action;
        private readonly object _state;

        private volatile bool _isCompleted;
        private volatile bool _isCancelled;
        private volatile System.Runtime.ExceptionServices.ExceptionDispatchInfo _exception;
        
        private readonly object _syncObject;
        private int _taskProcessFlag;

        /// <summary>
        /// <see cref="SendOrPostCallbackSyncThreadPoolWorkItem"/> constructor
        /// </summary>
        /// <param name="action">Delegate to execute work</param>
        /// <param name="state">State object that will be passed to delegate</param>
        /// <param name="allowExecutionContextFlow">Indicates whether the ExecutionContext should be captured</param>
        /// <param name="preferFairness">Indicates whether this work item should alwayes be enqueued to the GlobalQueue</param>
        public SendOrPostCallbackSyncThreadPoolWorkItem(SendOrPostCallback action, object state, bool allowExecutionContextFlow, bool preferFairness)
            : base(allowExecutionContextFlow, preferFairness)
        {
            TurboContract.Requires(action != null, conditionString: "action != null");
            _action = action;
            _state = state;
            _isCompleted = false;
            _isCancelled = false;
            _exception = null;
            _syncObject = new object();
            _taskProcessFlag = 0;
        }
        /// <summary>
        /// <see cref="SendOrPostCallbackSyncThreadPoolWorkItem"/> constructor
        /// </summary>
        /// <param name="action">Delegate to execute work</param>
        /// <param name="state">State object that will be passed to delegate</param>
        public SendOrPostCallbackSyncThreadPoolWorkItem(SendOrPostCallback action, object state)
            : this(action, state, true, true)
        {
        }

        /// <summary>
        /// Waits until the completion of this work item
        /// </summary>
        public void Wait()
        {
            if (_isCompleted)
            {
                if (_isCancelled)
                    throw new OperationInterruptedException();
                if (_exception != null)
                    _exception.Throw();
                return;
            }

            lock (_syncObject)
            {
                while (!_isCompleted)
                    Monitor.Wait(_syncObject);
            }

            if (_isCancelled)
                throw new OperationInterruptedException();
            if (_exception != null)
                _exception.Throw();
        }

        /// <summary>
        /// Runs this work item (internal method)
        /// </summary>
        protected sealed override void RunInner()
        {
            if (Interlocked.Exchange(ref _taskProcessFlag, 1) != 0)
                throw new InvalidOperationException("Can't execute SendOrPostCallbackSyncThreadPoolWorkItem cause it was already executed or cancelled");

            try
            {
                _action(_state);
            }
            catch (Exception ex)
            {
                _exception = System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex);
            }
            finally
            {
                lock (_syncObject)
                {
                    _isCompleted = true;
                    Monitor.PulseAll(_syncObject);
                }
            }
        }

        /// <summary>
        /// Notifies that work item was cancelled (internal method)
        /// </summary>
        protected sealed override void CancelInner()
        {
            if (Interlocked.Exchange(ref _taskProcessFlag, 1) != 0)
                return;

            if (!_isCompleted)
            {
                lock (_syncObject)
                {
                    if (!_isCompleted)
                    {
                        _isCancelled = true;
                        _isCompleted = true;

                        Monitor.PulseAll(_syncObject);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Thread pool work item that wraps <see cref="WaitCallback"/>
    /// </summary>
    public sealed class WaitCallbackThreadPoolWorkItem : ThreadPoolWorkItem
    {
        private readonly WaitCallback _action;
        private readonly object _state;

        /// <summary>
        /// <see cref="WaitCallbackThreadPoolWorkItem"/> constructor
        /// </summary>
        /// <param name="action">Delegate to execute work</param>
        /// <param name="state">State object that will be passed to delegate</param>
        /// <param name="allowExecutionContextFlow">Indicates whether the ExecutionContext should be captured</param>
        /// <param name="preferFairness">Indicates whether this work item should alwayes be enqueued to the GlobalQueue</param>
        public WaitCallbackThreadPoolWorkItem(WaitCallback action, object state, bool allowExecutionContextFlow, bool preferFairness)
            : base(allowExecutionContextFlow, preferFairness)
        {
            TurboContract.Requires(action != null, conditionString: "action != null");
            _action = action;
            _state = state;
        }
        /// <summary>
        /// <see cref="WaitCallbackThreadPoolWorkItem"/> constructor
        /// </summary>
        /// <param name="action">Delegate to execute work</param>
        /// <param name="state">State object that will be passed to delegate</param>
        public WaitCallbackThreadPoolWorkItem(WaitCallback action, object state)
            : this(action, state, true, false)
        {
        }

        /// <summary>
        /// Runs this work item (internal method)
        /// </summary>
        protected sealed override void RunInner()
        {
            _action(_state);
        }
    }


    /// <summary>
    /// Thread pool work item that wraps already created <see cref="Task"/>
    /// </summary>
    public sealed class TaskEntryExecutionThreadPoolWorkItem: ThreadPoolWorkItem
    {
        private readonly Task _task;
        private int _taskProcessFlag;

        /// <summary>
        /// <see cref="TaskEntryExecutionThreadPoolWorkItem"/> constructor
        /// </summary>
        /// <param name="task">Task</param>
        /// <param name="allowExecutionContextFlow">Indicates whether the ExecutionContext should be captured</param>
        public TaskEntryExecutionThreadPoolWorkItem(Task task, bool allowExecutionContextFlow)
            : base(allowExecutionContextFlow, (task.CreationOptions & TaskCreationOptions.PreferFairness) != 0)
        {
            TurboContract.Requires(task != null, conditionString: "task != null");
            _task = task;
            _taskProcessFlag = 0;
        }
        /// <summary>
        /// <see cref="TaskEntryExecutionThreadPoolWorkItem"/> constructor
        /// </summary>
        /// <param name="task">Task</param>
        public TaskEntryExecutionThreadPoolWorkItem(Task task)
            : this(task, false)
        {

        }

        /// <summary>
        /// Runs this work item (internal method)
        /// </summary>
        protected sealed override void RunInner()
        {
            if (Interlocked.Exchange(ref _taskProcessFlag, 1) != 0)
                throw new InvalidOperationException("Can't execute TaskEntryExecutionThreadPoolWorkItem cause it was already executed or cancelled");

            Qoollo.Turbo.Threading.ServiceStuff.TaskHelper.ExecuteTaskEntry(_task);
        }

        /// <summary>
        /// Notifies that work item was cancelled (internal method)
        /// </summary>
        protected sealed override void CancelInner()
        {
            if (Interlocked.Exchange(ref _taskProcessFlag, 1) != 0)
                return;

            Qoollo.Turbo.Threading.ServiceStuff.TaskHelper.CancelTask(_task, false);
        }
    }


    /// <summary>
    /// Thread pool work item that wraps <see cref="Action{T}"/> and <see cref="Task"/> for that action.
    /// Required to execute work within <see cref="TaskScheduler"/>
    /// </summary>
    /// <typeparam name="TState">Type of the state object</typeparam>
    internal sealed class TaskEntryExecutionWithClosureThreadPoolWorkItem<TState> : ThreadPoolWorkItem
    {
        /// <summary>
        /// Preallocated delegate object
        /// </summary>
        internal static readonly Action<object> RunRawAction = new Action<object>(RunRaw);

        /// <summary>
        /// Executes Action from <see cref="TaskEntryExecutionWithClosureThreadPoolWorkItem{TState}"/> passed as parameter
        /// </summary>
        /// <param name="closure">Instance of <see cref="TaskEntryExecutionWithClosureThreadPoolWorkItem{TState}"/></param>
        private static void RunRaw(object closure)
        {
            var extractedClosure = (TaskEntryExecutionWithClosureThreadPoolWorkItem<TState>)closure;
            TurboContract.Assert(extractedClosure != null, conditionString: "extractedClosure != null");
            extractedClosure._action(extractedClosure._state);
        }

        // ==================

        private readonly Action<TState> _action;
        private readonly TState _state;

        private Task _task;
        private int _taskProcessFlag;

        /// <summary>
        /// <see cref="TaskEntryExecutionWithClosureThreadPoolWorkItem{TState}"/> constructor
        /// </summary>
        /// <param name="action">Delegate to execute work</param>
        /// <param name="state">State object that will be passed to delegate</param>
        /// <param name="allowExecutionContextFlow">Indicates whether the ExecutionContext should be captured</param>
        /// <param name="preferFairness">Indicates whether this work item should alwayes be enqueued to the GlobalQueue</param>
        public TaskEntryExecutionWithClosureThreadPoolWorkItem(Action<TState> action, TState state, bool allowExecutionContextFlow, bool preferFairness)
            : base(allowExecutionContextFlow, preferFairness)
        {
            TurboContract.Requires(action != null, conditionString: "action != null");
            _action = action;
            _state = state;
            _taskProcessFlag = 0;
        }
        /// <summary>
        /// <see cref="TaskEntryExecutionWithClosureThreadPoolWorkItem{TState}"/> constructor
        /// </summary>
        /// <param name="action">Delegate to execute work</param>
        /// <param name="state">State object that will be passed to delegate</param>
        /// <param name="creationOptions">Task creation options</param>
        public TaskEntryExecutionWithClosureThreadPoolWorkItem(Action<TState> action, TState state, TaskCreationOptions creationOptions)
            : this(action, state, false, (creationOptions & TaskCreationOptions.PreferFairness) != 0)
        {

        }
        /// <summary>
        /// <see cref="TaskEntryExecutionWithClosureThreadPoolWorkItem{TState}"/> constructor
        /// </summary>
        /// <param name="action">Delegate to execute work</param>
        /// <param name="state">State object that will be passed to delegate</param>
        public TaskEntryExecutionWithClosureThreadPoolWorkItem(Action<TState> action, TState state)
            : this(action, state, false, false)
        {

        }

        /// <summary>
        /// Sets externally created task for passed action
        /// </summary>
        /// <param name="task">Task</param>
        public void SetTask(Task task)
        {
            TurboContract.Requires(task != null, conditionString: "task != null");
            if (_task != null)
                throw new InvalidOperationException("Task already setted");
            if (!object.ReferenceEquals(task.AsyncState, this))
                throw new ArgumentException("Task.AsyncState should be set to this object");
            _task = task;

        }

        /// <summary>
        /// Runs this work item (internal method)
        /// </summary>
        protected sealed override void RunInner()
        {
            if (_task == null && Volatile.Read(ref _taskProcessFlag) == 0)
                throw new InvalidOperationException("Can't execute TaskEntryExecutionWithClosureThreadPoolWorkItem without Task");
            if (Interlocked.Exchange(ref _taskProcessFlag, 1) != 0)
                throw new InvalidOperationException("Can't execute TaskEntryExecutionWithClosureThreadPoolWorkItem cause it was already executed or cancelled");

            Qoollo.Turbo.Threading.ServiceStuff.TaskHelper.ExecuteTaskEntry(_task);
            _task = null;
        }

        /// <summary>
        /// Notifies that work item was cancelled (internal method)
        /// </summary>
        protected sealed override void CancelInner()
        {
            if (_task == null && Volatile.Read(ref _taskProcessFlag) == 0)
                throw new InvalidOperationException("Can't cancel TaskEntryExecutionWithClosureThreadPoolWorkItem without Task");
            if (Interlocked.Exchange(ref _taskProcessFlag, 1) != 0)
                return;

            Qoollo.Turbo.Threading.ServiceStuff.TaskHelper.CancelTask(_task, false);
            _task = null;
        }
    }


    /// <summary>
    /// Thread pool work item that wraps <see cref="Func{T, TResult}"/> and <see cref="Task{T}"/> for that action.
    /// Required to execute work within <see cref="TaskScheduler"/>
    /// </summary>
    /// <typeparam name="TState">Type of the state object</typeparam>
    /// <typeparam name="TRes">Type of the work item result</typeparam>
    internal sealed class TaskEntryExecutionWithClosureThreadPoolWorkItem<TState, TRes> : ThreadPoolWorkItem
    {
        /// <summary>
        /// Preallocated delegate object
        /// </summary>
        internal static readonly Func<object, TRes> RunRawAction = new Func<object, TRes>(RunRaw);

        /// <summary>
        /// Executes Action from <see cref="TaskEntryExecutionWithClosureThreadPoolWorkItem{TState, TRes}"/> passed as parameter
        /// </summary>
        /// <param name="closure">Instance of <see cref="TaskEntryExecutionWithClosureThreadPoolWorkItem{TState, TRes}"/></param>
        private static TRes RunRaw(object closure)
        {
            var extractedClosure = (TaskEntryExecutionWithClosureThreadPoolWorkItem<TState, TRes>)closure;
            TurboContract.Assert(extractedClosure != null, conditionString: "extractedClosure != null");
            return extractedClosure._action(extractedClosure._state);
        }

        // ==================

        private readonly Func<TState, TRes> _action;
        private readonly TState _state;

        private Task<TRes> _task;
        private int _taskProcessFlag;

        /// <summary>
        /// <see cref="TaskEntryExecutionWithClosureThreadPoolWorkItem{TState, TRes}"/> constructor
        /// </summary>
        /// <param name="action">Delegate to execute work</param>
        /// <param name="state">State object that will be passed to delegate</param>
        /// <param name="allowExecutionContextFlow">Indicates whether the ExecutionContext should be captured</param>
        /// <param name="preferFairness">Indicates whether this work item should alwayes be enqueued to the GlobalQueue</param>
        public TaskEntryExecutionWithClosureThreadPoolWorkItem(Func<TState, TRes> action, TState state, bool allowExecutionContextFlow, bool preferFairness)
            : base(allowExecutionContextFlow, preferFairness)
        {
            TurboContract.Requires(action != null, conditionString: "action != null");
            _action = action;
            _state = state;
            _taskProcessFlag = 0;
        }
        /// <summary>
        /// <see cref="TaskEntryExecutionWithClosureThreadPoolWorkItem{TState, TRes}"/> constructor
        /// </summary>
        /// <param name="action">Delegate to execute work</param>
        /// <param name="state">State object that will be passed to delegate</param>
        /// <param name="creationOptions">Task creation options</param>
        public TaskEntryExecutionWithClosureThreadPoolWorkItem(Func<TState, TRes> action, TState state, TaskCreationOptions creationOptions)
            : this(action, state, false, (creationOptions & TaskCreationOptions.PreferFairness) != 0)
        {

        }
        /// <summary>
        /// <see cref="TaskEntryExecutionWithClosureThreadPoolWorkItem{TState, TRes}"/> constructor
        /// </summary>
        /// <param name="action">Delegate to execute work</param>
        /// <param name="state">State object that will be passed to delegate</param>
        public TaskEntryExecutionWithClosureThreadPoolWorkItem(Func<TState, TRes> action, TState state)
            : this(action, state, false, false)
        {

        }

        /// <summary>
        /// Sets externally created task for passed action
        /// </summary>
        /// <param name="task">Task</param>
        public void SetTask(Task<TRes> task)
        {
            TurboContract.Requires(task != null, conditionString: "task != null");
            if (_task != null)
                throw new InvalidOperationException("Task already setted");
            if (!object.ReferenceEquals(task.AsyncState, this))
                throw new ArgumentException("Task.AsyncState should be set to this object");
            _task = task;

        }

        /// <summary>
        /// Runs this work item (internal method)
        /// </summary>
        protected sealed override void RunInner()
        {
            if (_task == null && Volatile.Read(ref _taskProcessFlag) == 0)
                throw new InvalidOperationException("Can't execute TaskEntryExecutionWithClosureThreadPoolWorkItem without Task");
            if (Interlocked.Exchange(ref _taskProcessFlag, 1) != 0)
                throw new InvalidOperationException("Can't execute TaskEntryExecutionWithClosureThreadPoolWorkItem cause it was already executed or cancelled");

            Qoollo.Turbo.Threading.ServiceStuff.TaskHelper.ExecuteTaskEntry(_task);
            _task = null;
        }

        /// <summary>
        /// Notifies that work item was cancelled (internal method)
        /// </summary>
        protected sealed override void CancelInner()
        {
            if (_task == null && Volatile.Read(ref _taskProcessFlag) == 0)
                throw new InvalidOperationException("Can't cancel TaskEntryExecutionWithClosureThreadPoolWorkItem without Task");
            if (Interlocked.Exchange(ref _taskProcessFlag, 1) != 0)
                return;

            Qoollo.Turbo.Threading.ServiceStuff.TaskHelper.CancelTask(_task, false);
            _task = null;
        }
    }
}
