using Qoollo.Turbo.Threading.ThreadPools.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Text;

namespace Qoollo.Turbo.Threading.ThreadPools
{
    /// <summary>
    /// Wrapper for <see cref="System.Threading.ThreadPool"/>
    /// </summary>
    public sealed class SystemThreadPool: ThreadPoolBase, IContextSwitchSupplier
    {
        /// <summary>
        /// Parametrized closure object
        /// </summary>
        /// <typeparam name="TState">Type of the state object</typeparam>
        private class ParameterizedClosure<TState>
        {
            public static readonly Action<object> RunAction = new Action<object>(Run);
            /// <summary>
            /// Runs the action inside closure
            /// </summary>
            /// <param name="closure">Closure object</param>
            public static void Run(object closure)
            {
                var unwrapClosure = (ParameterizedClosure<TState>)closure;
                TurboContract.Assert(unwrapClosure != null, conditionString: "unwrapClosure != null");
                unwrapClosure.Action(unwrapClosure.State);
            }

            public Action<TState> Action;
            public TState State;
        }
        /// <summary>
        /// Parametrized closure object
        /// </summary>
        /// <typeparam name="TState">Type of the state object</typeparam>
        /// <typeparam name="TRes">Type of the result</typeparam>
        private class ParameterizedClosure<TState, TRes>
        {
            public static readonly Func<object, TRes> RunAction = new Func<object, TRes>(Run);
            /// <summary>
            /// Runs the action inside closure
            /// </summary>
            /// <param name="closure">Closure object</param>
            /// <returns>Result</returns>
            public static TRes Run(object closure)
            {
                var unwrapClosure = (ParameterizedClosure<TState, TRes>)closure;
                TurboContract.Assert(unwrapClosure != null, conditionString: "unwrapClosure != null");
                return unwrapClosure.Action(unwrapClosure.State);
            }

            public Func<TState, TRes> Action;
            public TState State;
        }

        // =========

#if !NETSTANDARD1_X
        /// <summary>
        /// Gets or sets the maximum number of threads in system ThreadPool
        /// </summary>
        public static int MaxThreadCount
        {
            get
            {
                System.Threading.ThreadPool.GetMaxThreads(out int res, out int tmp);
                return res;
            }
            set
            {
                System.Threading.ThreadPool.GetMaxThreads(out int tmp, out int portCompTh);
                System.Threading.ThreadPool.SetMaxThreads(value, portCompTh);
            }
        }

        /// <summary>
        /// Gets or sets the minimum number of threads in system ThreadPool
        /// </summary>
        public static int MinThreadCount
        {
            get
            {
                System.Threading.ThreadPool.GetMinThreads(out int res, out int tmp);
                return res;
            }
            set
            {
                System.Threading.ThreadPool.GetMinThreads(out int tmp, out int portCompTh);
                System.Threading.ThreadPool.SetMinThreads(value, portCompTh);
            }
        }
#endif

        private static readonly System.Threading.WaitCallback RunActionWaitCallback = new System.Threading.WaitCallback(RunAction);

        /// <summary>
        /// Executes passed action
        /// </summary>
        /// <param name="act">Action to execute</param>
        private static void RunAction(object act)
        {
            TurboContract.Requires(act != null, conditionString: "act != null");

            ((Action)act)();
        }


        // ====================================

        /// <summary>
        /// Places a new task to the thread pool queue
        /// </summary>
        /// <param name="item">Thread pool work item</param>
        protected sealed override void AddWorkItem(ThreadPoolWorkItem item)
        {
            TurboContract.Requires(item != null, conditionString: "item != null");

            System.Threading.ThreadPool.QueueUserWorkItem(ThreadPoolWorkItem.RunWaitCallback, item);
        }

        /// <summary>
        /// Attemts to place a new task to the thread pool queue
        /// </summary>
        /// <param name="item">Thread pool work item</param>
        /// <returns>True if work item was added to the queue, otherwise false</returns>
        protected sealed override bool TryAddWorkItem(ThreadPoolWorkItem item)
        {
            TurboContract.Requires(item != null, conditionString: "item != null");

            return System.Threading.ThreadPool.QueueUserWorkItem(ThreadPoolWorkItem.RunWaitCallback, item);
        }


        /// <summary>
        /// Queues a method for exection inside the current ThreadPool
        /// </summary>
        /// <param name="action">Representing the method to execute</param>
        /// <exception cref="ArgumentNullException">Action is null</exception>
        public new void Run(Action action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            System.Threading.ThreadPool.QueueUserWorkItem(RunActionWaitCallback, action);
        }
        /// <summary>
        /// Attempts to queue a method for exection inside the current ThreadPool
        /// </summary>
        /// <param name="action">Representing the method to execute</param>
        /// <returns>True if work item was added to the queue, otherwise false</returns>
        /// <exception cref="ArgumentNullException">Action is null</exception>
        public new bool TryRun(Action action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            return System.Threading.ThreadPool.QueueUserWorkItem(RunActionWaitCallback, action);
        }


        /// <summary>
        /// Queues a method for exection inside the current ThreadPool
        /// </summary>
        /// <param name="action">Representing the method to execute</param>
        /// <param name="state">State object</param>
        /// <exception cref="ArgumentNullException">Action is null</exception>
        public void Run(System.Threading.WaitCallback action, object state)
        {
            TurboContract.Requires(action != null, conditionString: "action != null");

            System.Threading.ThreadPool.QueueUserWorkItem(action, state);
        }

        /// <summary>
        /// Attempts to queue a method for exection inside the current ThreadPool
        /// </summary>
        /// <param name="action">Representing the method to execute</param>
        /// <param name="state">State object</param>
        /// <returns>True if work item was added to the queue, otherwise false</returns>
        /// <exception cref="ArgumentNullException">Action is null</exception>
        public bool TryRun(System.Threading.WaitCallback action, object state)
        {
            TurboContract.Requires(action != null, conditionString: "action != null");

            return System.Threading.ThreadPool.QueueUserWorkItem(action, state);
        }



        /// <summary>
        /// Runs the action within ThreadPool
        /// </summary>
        /// <param name="act">Action to run</param>
        /// <param name="flowContext">Whether or not the exectuion context should be flowed</param>
        void IContextSwitchSupplier.Run(Action act, bool flowContext)
        {
            TurboContract.Requires(act != null, conditionString: "act != null");

#if NETSTANDARD1_X
            System.Threading.ThreadPool.QueueUserWorkItem(RunActionWaitCallback, act);
#else
            if (flowContext)
                System.Threading.ThreadPool.QueueUserWorkItem(RunActionWaitCallback, act);
            else
                System.Threading.ThreadPool.UnsafeQueueUserWorkItem(RunActionWaitCallback, act);
#endif
        }

        /// <summary>
        /// Runs the action within ThreadPool
        /// </summary>
        /// <param name="act">Action to run</param>
        /// <param name="state">State object</param>
        /// <param name="flowContext">Whether or not the exectuion context should be flowed</param>
        void IContextSwitchSupplier.RunWithState(Action<object> act, object state, bool flowContext)
        {
            TurboContract.Requires(act != null, conditionString: "act != null");

#if NETSTANDARD1_X
            System.Threading.ThreadPool.QueueUserWorkItem(new System.Threading.WaitCallback(act), state);
#else
            if (flowContext)
                System.Threading.ThreadPool.QueueUserWorkItem(new System.Threading.WaitCallback(act), state);
            else
                System.Threading.ThreadPool.UnsafeQueueUserWorkItem(new System.Threading.WaitCallback(act), state);
#endif
        }

        /// <summary>
        /// Creates an awaitable that asynchronously switch to the current ThreadPool when awaited
        /// </summary>
        /// <returns>Context switch awaitable object</returns>
        public sealed override ContextSwitchAwaitable SwitchToPool()
        {
#if !NETSTANDARD1_X
            if (System.Threading.Thread.CurrentThread.IsThreadPoolThread)
                return new ContextSwitchAwaitable();
#endif

            return new ContextSwitchAwaitable(this);
        }


        /// <summary>
        /// Queues a method for exection inside the current ThreadPool and returns a <see cref="System.Threading.Tasks.Task"/> that represents queued operation
        /// </summary>
        /// <param name="action">Representing the method to execute</param>
        /// <returns>Create Task</returns>
        /// <exception cref="ArgumentNullException">Action is null</exception>
        public sealed override System.Threading.Tasks.Task RunAsTask(Action action)
        {
            return System.Threading.Tasks.Task.Run(action);
        }
        /// <summary>
        /// Queues a method for exection inside the current ThreadPool and returns a <see cref="System.Threading.Tasks.Task"/> that represents queued operation
        /// </summary>
        /// <typeparam name="TState">Type of the user state object</typeparam>
        /// <param name="action">Representing the method to execute</param>
        /// <param name="state">State object</param>
        /// <returns>Created Task</returns>
        /// <exception cref="ArgumentNullException">Action is null</exception>
        public sealed override System.Threading.Tasks.Task RunAsTask<TState>(Action<TState> action, TState state)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            var workItem = new ParameterizedClosure<TState>() { Action = action, State = state };
            return System.Threading.Tasks.Task.Factory.StartNew(ParameterizedClosure<TState>.RunAction, workItem);
        }

        /// <summary>
        /// Queues a method for exection inside the current ThreadPool and returns a <see cref="System.Threading.Tasks.Task{TRes}"/> that represents queued operation
        /// </summary>
        /// <typeparam name="TRes">The type of the operation result</typeparam>
        /// <param name="func">Representing the method to execute</param>
        /// <returns>Created Task</returns>
        /// <exception cref="ArgumentNullException">Func is null</exception>
        public sealed override System.Threading.Tasks.Task<TRes> RunAsTask<TRes>(Func<TRes> func)
        {
            return System.Threading.Tasks.Task.Run(func);
        }
        /// <summary>
        /// Queues a method for exection inside the current ThreadPool and returns a <see cref="System.Threading.Tasks.Task{TRes}"/> that represents queued operation
        /// </summary>
        /// <typeparam name="TState">Type of the user state object</typeparam>
        /// <typeparam name="TRes">The type of the operation result</typeparam>
        /// <param name="func">Representing the method to execute</param>
        /// <param name="state">State object</param>
        /// <returns>Created Task</returns>
        /// <exception cref="ArgumentNullException">Func is null</exception>
        public sealed override System.Threading.Tasks.Task<TRes> RunAsTask<TState, TRes>(Func<TState, TRes> func, TState state)
        {
            if (func == null)
                throw new ArgumentNullException(nameof(func));
            var workItem = new ParameterizedClosure<TState, TRes>() { Action = func, State = state };
            return System.Threading.Tasks.Task.Factory.StartNew(ParameterizedClosure<TState, TRes>.RunAction, workItem);
        }
    }
}
