using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Threading.ThreadPools.Common
{
    /// <summary>
    /// Единица работы пула для Action
    /// </summary>
    public sealed class ActionThreadPoolWorkItem: ThreadPoolWorkItem
    {
        private readonly Action _action;

        /// <summary>
        /// Конструктор ActionThreadPoolWorkItem
        /// </summary>
        /// <param name="action">Исполняемое действие</param>
        /// <param name="allowExecutionContextFlow">Допустимо ли захватывать контекст исполнения</param>
        /// <param name="preferFairness">Требовать постановку в общую очередь</param>
        public ActionThreadPoolWorkItem(Action action, bool allowExecutionContextFlow, bool preferFairness)
            : base(allowExecutionContextFlow, preferFairness)
        {
            Contract.Requires(action != null);
            _action = action;
        }
        /// <summary>
        /// Конструктор ActionThreadPoolWorkItem
        /// </summary>
        /// <param name="action">Исполняемое действие</param>
        public ActionThreadPoolWorkItem(Action action)
            : this(action, true, false)
        {
        }

        /// <summary>
        /// Метод исполнения задачи
        /// </summary>
        protected sealed override void RunInner()
        {
            _action();
        }
    }

    /// <summary>
    /// Единица работы пула для Action с одним параметром
    /// </summary>
    /// <typeparam name="T">Тип параметра</typeparam>
    public sealed class ActionThreadPoolWorkItem<T> : ThreadPoolWorkItem
    {
        private readonly Action<T> _action;
        private readonly T _state;

        /// <summary>
        /// Конструктор ActionWithStateThreadPoolWorkItem
        /// </summary>
        /// <param name="action">Исполняемое действие</param>
        /// <param name="state">Параметр</param>
        /// <param name="allowExecutionContextFlow">Допустимо ли захватывать контекст исполнения</param>
        /// <param name="preferFairness">Требовать постановку в общую очередь</param>
        public ActionThreadPoolWorkItem(Action<T> action, T state, bool allowExecutionContextFlow, bool preferFairness)
            : base(allowExecutionContextFlow, preferFairness)
        {
            Contract.Requires(action != null);
            _action = action;
            _state = state;
        }
        /// <summary>
        /// Конструктор ActionWithStateThreadPoolWorkItem
        /// </summary>
        /// <param name="action">Исполняемое действие</param>
        /// <param name="state">Параметр</param>
        public ActionThreadPoolWorkItem(Action<T> action, T state)
            : this(action, state, true, false)
        {

        }

        /// <summary>
        /// Метод исполнения задачи
        /// </summary>
        protected sealed override void RunInner()
        {
            _action(_state);
        }
    }


    /// <summary>
    /// Единица работы пула для Action, которая управляет Task'ом
    /// </summary>
    public sealed class TaskThreadPoolWorkItem : ThreadPoolWorkItem
    {
        private readonly Action _action;
        private readonly TaskCompletionSource<object> _completionSource;

        /// <summary>
        /// Конструктор TaskThreadPoolWorkItem
        /// </summary>
        /// <param name="action">Исполняемое действие</param>
        /// <param name="creationOptions">Параметры создания таска</param>
        public TaskThreadPoolWorkItem(Action action, TaskCreationOptions creationOptions)
            : base(true, (creationOptions & TaskCreationOptions.PreferFairness) != 0)
        {
            Contract.Requires(action != null);
            _action = action;
            _completionSource = new TaskCompletionSource<object>(creationOptions);
        }
        /// <summary>
        /// Конструктор TaskThreadPoolWorkItem
        /// </summary>
        /// <param name="action">Исполняемое действие</param>
        public TaskThreadPoolWorkItem(Action action)
            : this(action, TaskCreationOptions.None)
        {
        }

        /// <summary>
        /// Task
        /// </summary>
        public Task Task { get { return _completionSource.Task; } }

        /// <summary>
        /// Метод исполнения задачи
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
        /// Уведомление об отмене операции
        /// </summary>
        protected sealed override void CancelInner()
        {
            _completionSource.SetCanceled();
        }
    }

    /// <summary>
    /// Единица работы пула для Action, которая управляет Task'ом с параметром состояния
    /// </summary>
    /// <typeparam name="TState">Тип параметра состояния</typeparam>
    public sealed class TaskThreadPoolWorkItem<TState> : ThreadPoolWorkItem
    {
        private readonly Action<TState> _action;
        private readonly TState _state;
        private readonly TaskCompletionSource<object> _completionSource;

        /// <summary>
        /// Конструктор TaskThreadPoolWorkItem
        /// </summary>
        /// <param name="action">Исполняемое действие</param>
        /// <param name="state">Параметр</param>
        /// <param name="creationOptions">Параметры создания таска</param>
        public TaskThreadPoolWorkItem(Action<TState> action, TState state, TaskCreationOptions creationOptions)
            : base(true, (creationOptions & TaskCreationOptions.PreferFairness) != 0)
        {
            Contract.Requires(action != null);
            _action = action;
            _state = state;
            _completionSource = new TaskCompletionSource<object>(creationOptions);
        }
        /// <summary>
        /// Конструктор TaskThreadPoolWorkItem
        /// </summary>
        /// <param name="action">Исполняемое действие</param>
        /// <param name="state">Параметр</param>
        public TaskThreadPoolWorkItem(Action<TState> action, TState state)
            : this(action, state, TaskCreationOptions.None)
        {
        }

        /// <summary>
        /// Task
        /// </summary>
        public Task Task { get { return _completionSource.Task; } }

        /// <summary>
        /// Метод исполнения задачи
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
        /// Уведомление об отмене операции
        /// </summary>
        protected sealed override void CancelInner()
        {
            _completionSource.SetCanceled();
        }
    }

 
    /// <summary>
    /// Единица работы пула для Func, которая управляет Task'ом
    /// </summary>
    /// <typeparam name="TRes">Тип результата задачи</typeparam>
    public sealed class TaskFuncThreadPoolWorkItem<TRes> : ThreadPoolWorkItem
    {
        private readonly Func<TRes> _action;
        private readonly TaskCompletionSource<TRes> _completionSource;

        /// <summary>
        /// Конструктор TaskFuncThreadPoolWorkItem
        /// </summary>
        /// <param name="action">Исполняемое действие</param>
        /// <param name="creationOptions">Параметры создания таска</param>
        public TaskFuncThreadPoolWorkItem(Func<TRes> action, TaskCreationOptions creationOptions)
            : base(true, (creationOptions & TaskCreationOptions.PreferFairness) != 0)
        {
            Contract.Requires(action != null);
            _action = action;
            _completionSource = new TaskCompletionSource<TRes>(creationOptions);
        }
        /// <summary>
        /// Конструктор TaskFuncThreadPoolWorkItem
        /// </summary>
        /// <param name="action">Исполняемое действие</param>
        public TaskFuncThreadPoolWorkItem(Func<TRes> action)
            : this(action, TaskCreationOptions.None)
        {

        }

        /// <summary>
        /// Task
        /// </summary>
        public Task<TRes> Task { get { return _completionSource.Task; } }

        /// <summary>
        /// Метод исполнения задачи
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
        /// Уведомление об отмене операции
        /// </summary>
        protected sealed override void CancelInner()
        {
            _completionSource.SetCanceled();
        }
    }

    /// <summary>
    /// Единица работы пула для Func, которая управляет Task'ом
    /// </summary>
    /// <typeparam name="TState">Тип параметра состояния</typeparam>
    /// <typeparam name="TRes">Тип результата задачи</typeparam>
    public sealed class TaskFuncThreadPoolWorkItem<TState, TRes> : ThreadPoolWorkItem
    {
        private readonly Func<TState, TRes> _action;
        private readonly TState _state;
        private readonly TaskCompletionSource<TRes> _completionSource;

        /// <summary>
        /// Конструктор TaskFuncThreadPoolWorkItem
        /// </summary>
        /// <param name="action">Исполняемое действие</param>
        /// <param name="state">Параметр состояния</param>
        /// <param name="creationOptions">Параметры создания таска</param>
        public TaskFuncThreadPoolWorkItem(Func<TState, TRes> action, TState state, TaskCreationOptions creationOptions)
            : base(true, (creationOptions & TaskCreationOptions.PreferFairness) != 0)
        {
            Contract.Requires(action != null);
            _action = action;
            _state = state;
            _completionSource = new TaskCompletionSource<TRes>(creationOptions);
        }
        /// <summary>
        /// Конструктор TaskFuncThreadPoolWorkItem
        /// </summary>
        /// <param name="action">Исполняемое действие</param>
        /// <param name="state">Параметр состояния</param>
        public TaskFuncThreadPoolWorkItem(Func<TState, TRes> action, TState state)
            : this(action, state, TaskCreationOptions.None)
        {

        }

        /// <summary>
        /// Task
        /// </summary>
        public Task<TRes> Task { get { return _completionSource.Task; } }

        /// <summary>
        /// Метод исполнения задачи
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
        /// Уведомление об отмене операции
        /// </summary>
        protected sealed override void CancelInner()
        {
            _completionSource.SetCanceled();
        }
    }
    

    /// <summary>
    /// Единица работы пула для SendOrPostCallback
    /// </summary>
    public sealed class SendOrPostCallbackThreadPoolWorkItem : ThreadPoolWorkItem
    {
        private readonly SendOrPostCallback _action;
        private readonly object _state;

        /// <summary>
        /// Конструктор SendOrPostCallbackThreadPoolWorkItem
        /// </summary>
        /// <param name="action">Исполняемое действие</param>
        /// <param name="state">Состояние</param>
        /// <param name="allowExecutionContextFlow">Можно ли захватывать контекст исполнения</param>
        /// <param name="preferFairness">Требовать постановку в общую очередь</param>
        public SendOrPostCallbackThreadPoolWorkItem(SendOrPostCallback action, object state, bool allowExecutionContextFlow, bool preferFairness)
            : base(allowExecutionContextFlow, preferFairness)
        {
            Contract.Requires(action != null);
            _action = action;
            _state = state;
        }
        /// <summary>
        /// Конструктор SendOrPostCallbackThreadPoolWorkItem
        /// </summary>
        /// <param name="action">Исполняемое действие</param>
        /// <param name="state">Состояние</param>
        public SendOrPostCallbackThreadPoolWorkItem(SendOrPostCallback action, object state)
            : this(action, state, true, false)
        {
        }

        /// <summary>
        /// Метод исполнения задачи
        /// </summary>
        protected sealed override void RunInner()
        {
            _action(_state);
        }
    }

    /// <summary>
    /// Единица работы пула для SendOrPostCallback с поддержкой синхронизации
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
        /// Конструктор SendOrPostCallbackSyncThreadPoolWorkItem
        /// </summary>
        /// <param name="action">Исполняемое действие</param>
        /// <param name="state">Состояние</param>
        /// <param name="allowExecutionContextFlow">Можно ли захватывать контекст исполнения</param>
        /// <param name="preferFairness">Требовать постановку в общую очередь</param>
        public SendOrPostCallbackSyncThreadPoolWorkItem(SendOrPostCallback action, object state, bool allowExecutionContextFlow, bool preferFairness)
            : base(allowExecutionContextFlow, preferFairness)
        {
            Contract.Requires(action != null);
            _action = action;
            _state = state;
            _isCompleted = false;
            _isCancelled = false;
            _exception = null;
            _syncObject = new object();
            _taskProcessFlag = 0;
        }
        /// <summary>
        /// Конструктор SendOrPostCallbackSyncThreadPoolWorkItem
        /// </summary>
        /// <param name="action">Исполняемое действие</param>
        /// <param name="state">Состояние</param>
        public SendOrPostCallbackSyncThreadPoolWorkItem(SendOrPostCallback action, object state)
            : this(action, state, true, true)
        {
        }

        /// <summary>
        /// Подождать завершение задачи
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
        /// Метод исполнения задачи
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
        /// Уведомление об отмене операции
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
    /// Единица работы пула для WaitCallback
    /// </summary>
    public sealed class WaitCallbackThreadPoolWorkItem : ThreadPoolWorkItem
    {
        private readonly WaitCallback _action;
        private readonly object _state;

        /// <summary>
        /// Конструктор WaitCallbackThreadPoolWorkItem
        /// </summary>
        /// <param name="action">Исполняемое действие</param>
        /// <param name="state">Состояние</param>
        /// <param name="allowExecutionContextFlow">Можно ли захватывать контекст исполнения</param>
        /// <param name="preferFairness">Требовать постановку в общую очередь</param>
        public WaitCallbackThreadPoolWorkItem(WaitCallback action, object state, bool allowExecutionContextFlow, bool preferFairness)
            : base(allowExecutionContextFlow, preferFairness)
        {
            Contract.Requires(action != null);
            _action = action;
            _state = state;
        }
        /// <summary>
        /// Конструктор WaitCallbackThreadPoolWorkItem
        /// </summary>
        /// <param name="action">Исполняемое действие</param>
        /// <param name="state">Состояние</param>
        public WaitCallbackThreadPoolWorkItem(WaitCallback action, object state)
            : this(action, state, true, false)
        {
        }

        /// <summary>
        /// Метод исполнения задачи
        /// </summary>
        protected sealed override void RunInner()
        {
            _action(_state);
        }
    }


    /// <summary>
    /// Единица работы пула для исполнения Task
    /// </summary>
    public sealed class TaskEntryExecutionThreadPoolWorkItem: ThreadPoolWorkItem
    {
        private readonly Task _task;
        private int _taskProcessFlag;
        
        /// <summary>
        /// Конструктор TaskEntryExecutionThreadPoolWorkItem
        /// </summary>
        /// <param name="task">Task</param>
        /// <param name="allowExecutionContextFlow">Можно ли захватывать контекст исполнения</param>
        public TaskEntryExecutionThreadPoolWorkItem(Task task, bool allowExecutionContextFlow)
            : base(allowExecutionContextFlow, (task.CreationOptions & TaskCreationOptions.PreferFairness) != 0)
        {
            Contract.Requires(task != null);
            _task = task;
            _taskProcessFlag = 0;
        }
        /// <summary>
        /// Конструктор TaskEntryExecutionThreadPoolWorkItem
        /// </summary>
        /// <param name="task">Task</param>
        public TaskEntryExecutionThreadPoolWorkItem(Task task)
            : this(task, false)
        {

        }

        /// <summary>
        /// Метод исполнения задачи
        /// </summary>
        protected sealed override void RunInner()
        {
            if (Interlocked.Exchange(ref _taskProcessFlag, 1) != 0)
                throw new InvalidOperationException("Can't execute TaskEntryExecutionThreadPoolWorkItem cause it was already executed or cancelled");

            Qoollo.Turbo.Threading.ServiceStuff.TaskHelper.ExecuteTaskEntry(_task, true);
        }

        /// <summary>
        /// Уведомление об отмене операции
        /// </summary>
        protected sealed override void CancelInner()
        {
            if (Interlocked.Exchange(ref _taskProcessFlag, 1) != 0)
                return;

            Qoollo.Turbo.Threading.ServiceStuff.TaskHelper.CancelTask(_task, false);
        }
    }


    /// <summary>
    /// Единица работы пула для исполнения Task
    /// </summary>
    /// <typeparam name="TState">Тип параметра состояния</typeparam>
    public sealed class TaskEntryExecutionWithClosureThreadPoolWorkItem<TState> : ThreadPoolWorkItem
    {
        /// <summary>
        /// Заранее созданный делегат на RunRaw
        /// </summary>
        internal static readonly Action<object> RunRawAction = new Action<object>(RunRaw);

        /// <summary>
        /// Запустить внутренний Action на выполнение
        /// </summary>
        /// <param name="closure">Объект TaskEntryExecutionWithClosureThreadPoolWorkItem</param>
        private static void RunRaw(object closure)
        {
            var extractedClosure = (TaskEntryExecutionWithClosureThreadPoolWorkItem<TState>)closure;
            Contract.Assert(extractedClosure != null);
            extractedClosure._action(extractedClosure._state);
        }

        // ==================

        private readonly Action<TState> _action;
        private readonly TState _state;

        private Task _task;
        private int _taskProcessFlag;

        /// <summary>
        /// Конструктор TaskEntryExecutionWithClosureThreadPoolWorkItem
        /// </summary>
        /// <param name="action">Действие</param>
        /// <param name="state">Параметр состояния</param>
        /// <param name="allowExecutionContextFlow">Допустимо ли захватывать контекст исполнения</param>
        /// <param name="preferFairness">Требовать постановку в общую очередь</param>
        public TaskEntryExecutionWithClosureThreadPoolWorkItem(Action<TState> action, TState state, bool allowExecutionContextFlow, bool preferFairness)
            : base(allowExecutionContextFlow, preferFairness)
        {
            Contract.Requires(action != null);
            _action = action;
            _state = state;
            _taskProcessFlag = 0;
        }
        /// <summary>
        /// Конструктор TaskEntryExecutionWithClosureThreadPoolWorkItem
        /// </summary>
        /// <param name="action">Действие</param>
        /// <param name="state">Параметр состояния</param>
        /// <param name="creationOptions">Параметры создания таска</param>
        public TaskEntryExecutionWithClosureThreadPoolWorkItem(Action<TState> action, TState state, TaskCreationOptions creationOptions)
            : this(action, state, false, (creationOptions & TaskCreationOptions.PreferFairness) != 0)
        {

        }
        /// <summary>
        /// Конструктор TaskEntryExecutionWithClosureThreadPoolWorkItem
        /// </summary>
        /// <param name="action">Действие</param>
        /// <param name="state">Параметр состояния</param>
        public TaskEntryExecutionWithClosureThreadPoolWorkItem(Action<TState> action, TState state)
            : this(action, state, false, false)
        {

        }

        /// <summary>
        /// Установить таск
        /// </summary>
        /// <param name="task">Task</param>
        public void SetTask(Task task)
        {
            Contract.Requires(task != null);
            if (_task != null)
                throw new InvalidOperationException("Task already setted");
            if (!object.ReferenceEquals(task.AsyncState, this))
                throw new ArgumentException("Task.AsyncState should be set to this object");
            _task = task;

        }

        /// <summary>
        /// Метод исполнения задачи
        /// </summary>
        protected sealed override void RunInner()
        {
            if (_task == null && Volatile.Read(ref _taskProcessFlag) == 0)
                throw new InvalidOperationException("Can't execute TaskEntryExecutionWithClosureThreadPoolWorkItem without Task");
            if (Interlocked.Exchange(ref _taskProcessFlag, 1) != 0)
                throw new InvalidOperationException("Can't execute TaskEntryExecutionWithClosureThreadPoolWorkItem cause it was already executed or cancelled");

            Qoollo.Turbo.Threading.ServiceStuff.TaskHelper.ExecuteTaskEntry(_task, true);
            _task = null;
        }

        /// <summary>
        /// Уведомление об отмене операции
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
    /// Единица работы пула для исполнения Task
    /// </summary>
    /// <typeparam name="TState">Тип параметра состояния</typeparam>
    /// <typeparam name="TRes">Тип результата</typeparam>
    public sealed class TaskEntryExecutionWithClosureThreadPoolWorkItem<TState, TRes> : ThreadPoolWorkItem
    {
        /// <summary>
        /// Заранее созданный делегат на RunRaw
        /// </summary>
        internal static readonly Func<object, TRes> RunRawAction = new Func<object, TRes>(RunRaw);

        /// <summary>
        /// Запустить внутренний Action на выполнение
        /// </summary>
        /// <param name="closure">Объект TaskEntryExecutionWithClosureThreadPoolWorkItem</param>
        /// <returns>Результат исполнения</returns>
        private static TRes RunRaw(object closure)
        {
            var extractedClosure = (TaskEntryExecutionWithClosureThreadPoolWorkItem<TState, TRes>)closure;
            Contract.Assert(extractedClosure != null);
            return extractedClosure._action(extractedClosure._state);
        }

        // ==================

        private readonly Func<TState, TRes> _action;
        private readonly TState _state;

        private Task<TRes> _task;
        private int _taskProcessFlag;

        /// <summary>
        /// Конструктор TaskEntryExecutionWithClosureThreadPoolWorkItem
        /// </summary>
        /// <param name="action">Действие</param>
        /// <param name="state">Параметр состояния</param>
        /// <param name="allowExecutionContextFlow">Допустимо ли захватывать контекст исполнения</param>
        /// <param name="preferFairness">Требовать постановку в общую очередь</param>
        public TaskEntryExecutionWithClosureThreadPoolWorkItem(Func<TState, TRes> action, TState state, bool allowExecutionContextFlow, bool preferFairness)
            : base(allowExecutionContextFlow, preferFairness)
        {
            Contract.Requires(action != null);
            _action = action;
            _state = state;
            _taskProcessFlag = 0;
        }
        /// <summary>
        /// Конструктор TaskEntryExecutionWithClosureThreadPoolWorkItem
        /// </summary>
        /// <param name="action">Действие</param>
        /// <param name="state">Параметр состояния</param>
        /// <param name="creationOptions">Параметры создания таска</param>
        public TaskEntryExecutionWithClosureThreadPoolWorkItem(Func<TState, TRes> action, TState state, TaskCreationOptions creationOptions)
            : this(action, state, false, (creationOptions & TaskCreationOptions.PreferFairness) != 0)
        {

        }
        /// <summary>
        /// Конструктор TaskEntryExecutionWithClosureThreadPoolWorkItem
        /// </summary>
        /// <param name="action">Действие</param>
        /// <param name="state">Параметр состояния</param>
        public TaskEntryExecutionWithClosureThreadPoolWorkItem(Func<TState, TRes> action, TState state)
            : this(action, state, false, false)
        {

        }

        /// <summary>
        /// Установить таск
        /// </summary>
        /// <param name="task">Task</param>
        public void SetTask(Task<TRes> task)
        {
            Contract.Requires(task != null);
            if (_task != null)
                throw new InvalidOperationException("Task already setted");
            if (!object.ReferenceEquals(task.AsyncState, this))
                throw new ArgumentException("Task.AsyncState should be set to this object");
            _task = task;

        }

        /// <summary>
        /// Метод исполнения задачи
        /// </summary>
        protected sealed override void RunInner()
        {
            if (_task == null && Volatile.Read(ref _taskProcessFlag) == 0)
                throw new InvalidOperationException("Can't execute TaskEntryExecutionWithClosureThreadPoolWorkItem without Task");
            if (Interlocked.Exchange(ref _taskProcessFlag, 1) != 0)
                throw new InvalidOperationException("Can't execute TaskEntryExecutionWithClosureThreadPoolWorkItem cause it was already executed or cancelled");

            Qoollo.Turbo.Threading.ServiceStuff.TaskHelper.ExecuteTaskEntry(_task, true);
            _task = null;
        }

        /// <summary>
        /// Уведомление об отмене операции
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
