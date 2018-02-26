using Qoollo.Turbo.Threading.AsyncAwaitSupport;
using Qoollo.Turbo.Threading.ThreadPools.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Threading.ThreadPools
{
    /// <summary>
    /// Base class for pool of threads that can be used to execute asynchronous tasks
    /// </summary>
    [ContractClass(typeof(ThreadPoolBaseCodeContractCheck))]
    public abstract class ThreadPoolBase : IConsumer<Action>, IDisposable
    {
        /// <summary>
        /// Places a new task to the thread pool queue
        /// </summary>
        /// <param name="item">Thread pool work item</param>
        protected abstract void AddWorkItem(ThreadPoolWorkItem item);
        /// <summary>
        /// Attemts to place a new task to the thread pool queue
        /// </summary>
        /// <param name="item">Thread pool work item</param>
        /// <returns>True if work item was added to the queue, otherwise false</returns>
        protected abstract bool TryAddWorkItem(ThreadPoolWorkItem item);


        /// <summary>
        /// Enqueues a method for exection inside the current ThreadPool
        /// </summary>
        /// <param name="action">Representing the method to execute</param>
        /// <exception cref="ArgumentNullException">Action is null</exception>
        public void Run(Action action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            AddWorkItem(new ActionThreadPoolWorkItem(action));
        }
        /// <summary>
        /// Attempts to enqueue a method for exection inside the current ThreadPool
        /// </summary>
        /// <param name="action">Representing the method to execute</param>
        /// <returns>True if work item was added to the queue, otherwise false</returns>
        /// <exception cref="ArgumentNullException">Action is null</exception>
        public bool TryRun(Action action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            return TryAddWorkItem(new ActionThreadPoolWorkItem(action));
        }

        /// <summary>
        /// Enqueues a method for exection inside the current ThreadPool
        /// </summary>
        /// <typeparam name="T">Type of the user state object</typeparam>
        /// <param name="action">Representing the method to execute</param>
        /// <param name="state">State object</param>
        /// <exception cref="ArgumentNullException">Action is null</exception>
        public void Run<T>(Action<T> action, T state)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            AddWorkItem(new ActionThreadPoolWorkItem<T>(action, state));
        }

        /// <summary>
        /// Attempts to queue a method for exection inside the current ThreadPool
        /// </summary>
        /// <typeparam name="T">Type of the user state object</typeparam>
        /// <param name="action">Representing the method to execute</param>
        /// <param name="state">State object</param>
        /// <returns>True if work item was added to the queue, otherwise false</returns>
        /// <exception cref="ArgumentNullException">Action is null</exception>
        public bool TryRun<T>(Action<T> action, T state)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            return TryAddWorkItem(new ActionThreadPoolWorkItem<T>(action, state));
        }


        /// <summary>
        /// Enqueues a method for exection inside the current ThreadPool and returns a <see cref="Task"/> that represents queued operation
        /// </summary>
        /// <param name="action">Representing the method to execute</param>
        /// <returns>Create Task</returns>
        /// <exception cref="ArgumentNullException">Action is null</exception>
        public virtual Task RunAsTask(Action action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            var item = new TaskThreadPoolWorkItem(action);
            AddWorkItem(item);
            return item.Task;
        }
        /// <summary>
        /// Enqueues a method for exection inside the current ThreadPool and returns a <see cref="Task"/> that represents queued operation
        /// </summary>
        /// <typeparam name="TState">Type of the user state object</typeparam>
        /// <param name="action">Representing the method to execute</param>
        /// <param name="state">State object</param>
        /// <returns>Created Task</returns>
        /// <exception cref="ArgumentNullException">Action is null</exception>
        public virtual Task RunAsTask<TState>(Action<TState> action, TState state)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            var item = new TaskThreadPoolWorkItem<TState>(action, state);
            AddWorkItem(item);
            return item.Task;
        }

        /// <summary>
        /// Enqueues a method for exection inside the current ThreadPool and returns a <see cref="Task{TRes}"/> that represents queued operation
        /// </summary>
        /// <typeparam name="TRes">The type of the operation result</typeparam>
        /// <param name="func">Representing the method to execute</param>
        /// <returns>Created Task</returns>
        /// <exception cref="ArgumentNullException">Func is null</exception>
        public virtual Task<TRes> RunAsTask<TRes>(Func<TRes> func)
        {
            if (func == null)
                throw new ArgumentNullException(nameof(func));

            var item = new TaskFuncThreadPoolWorkItem<TRes>(func);
            AddWorkItem(item);
            return item.Task;
        }
        /// <summary>
        /// Enqueues a method for exection inside the current ThreadPool and returns a <see cref="Task{TRes}"/> that represents queued operation
        /// </summary>
        /// <typeparam name="TState">Type of the user state object</typeparam>
        /// <typeparam name="TRes">The type of the operation result</typeparam>
        /// <param name="func">Representing the method to execute</param>
        /// <param name="state">State object</param>
        /// <returns>Created Task</returns>
        /// <exception cref="ArgumentNullException">Func is null</exception>
        public virtual Task<TRes> RunAsTask<TState, TRes>(Func<TState, TRes> func, TState state)
        {
            if (func == null)
                throw new ArgumentNullException(nameof(func));

            var item = new TaskFuncThreadPoolWorkItem<TState, TRes>(func, state);
            AddWorkItem(item);
            return item.Task;
        }


        /// <summary>
        /// Creates an awaitable that asynchronously switch to the current ThreadPool when awaited
        /// </summary>
        /// <returns>Context switch awaitable object</returns>
        public virtual ContextSwitchAwaitable SwitchToPool()
        {
            return new ContextSwitchAwaitable(new SingleDelegateNoFlowContextSwitchSupplier(Run));
        }

        /// <summary>
        /// Pushes new element to the thread pool
        /// </summary>
        /// <param name="item">Element</param>
        void IConsumer<Action>.Add(Action item)
        {
            this.Run(item);
        }
        /// <summary>
        /// Attempts to push new element to the thread pool
        /// </summary>
        /// <param name="item">Element</param>
        /// <returns>True if the element was consumed successfully</returns>
        bool IConsumer<Action>.TryAdd(Action item)
        {
            return this.TryRun(item);
        }

        /// <summary>
        /// Cleans-up resources
        /// </summary>
        /// <param name="isUserCall">Is it called explicitly by user (False - from finalizer)</param>
        protected virtual void Dispose(bool isUserCall)
        {
        }

        /// <summary>
        /// Cleans-up resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }







    /// <summary>
    /// Code contracts
    /// </summary>
    [ContractClassFor(typeof(ThreadPoolBase))]
    abstract class ThreadPoolBaseCodeContractCheck : ThreadPoolBase
    {
        /// <summary>Code contracts</summary>
        private ThreadPoolBaseCodeContractCheck() { }

        /// <summary>Code contracts</summary>
        protected override void AddWorkItem(ThreadPoolWorkItem item)
        {
            TurboContract.Requires(item != null, conditionString: "item != null");

            throw new NotImplementedException();
        }

        /// <summary>Code contracts</summary>
        protected override bool TryAddWorkItem(ThreadPoolWorkItem item)
        {
            TurboContract.Requires(item != null, conditionString: "item != null");

            throw new NotImplementedException();
        }
    }
}
