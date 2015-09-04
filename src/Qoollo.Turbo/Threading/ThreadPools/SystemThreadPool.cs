using Qoollo.Turbo.Threading.ThreadPools.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Text;

namespace Qoollo.Turbo.Threading.ThreadPools
{
    /// <summary>
    /// Системный пул потоков
    /// </summary>
    public sealed class SystemThreadPool: ThreadPoolBase, IContextSwitchSupplier
    {
        /// <summary>
        /// Прамаетризованный объект замыкания
        /// </summary>
        /// <typeparam name="TState">Тип состояния</typeparam>
        private class ParameterizedClosure<TState>
        {
            public static readonly Action<object> RunAction = new Action<object>(Run);
            /// <summary>
            /// Запуск замыкания
            /// </summary>
            /// <param name="closure">Объект замыкания</param>
            public static void Run(object closure)
            {
                var unwrapClosure = (ParameterizedClosure<TState>)closure;
                Contract.Assert(unwrapClosure != null);
                unwrapClosure.Action(unwrapClosure.State);
            }

            public Action<TState> Action;
            public TState State;
        }
        /// <summary>
        /// Прамаетризованный объект замыкания
        /// </summary>
        /// <typeparam name="TState">Тип состояния</typeparam>
        /// <typeparam name="TRes">Тип результата</typeparam>
        private class ParameterizedClosure<TState, TRes>
        {
            public static readonly Func<object, TRes> RunAction = new Func<object, TRes>(Run);
            /// <summary>
            /// Запуск замыкания
            /// </summary>
            /// <param name="closure">Объект замыкания</param>
            /// <returns>Результат</returns>
            public static TRes Run(object closure)
            {
                var unwrapClosure = (ParameterizedClosure<TState, TRes>)closure;
                Contract.Assert(unwrapClosure != null);
                return unwrapClosure.Action(unwrapClosure.State);
            }

            public Func<TState, TRes> Action;
            public TState State;
        }
        
        // =========

        /// <summary>
        /// Максимальное количество потоков в пуле
        /// </summary>
        public static int MaxThreadCount
        {
            get
            {
                int res = 0;
                int tmp = 0;
                System.Threading.ThreadPool.GetMaxThreads(out res, out tmp);
                return res;
            }
            set
            {
                int tmp = 0;
                int portCompTh = 0;
                System.Threading.ThreadPool.GetMaxThreads(out tmp, out portCompTh);
                System.Threading.ThreadPool.SetMaxThreads(value, portCompTh);
            }
        }

        /// <summary>
        /// Минимальное количество потоков в пуле
        /// </summary>
        public static int MinThreadCount
        {
            get
            {
                int res = 0;
                int tmp = 0;
                System.Threading.ThreadPool.GetMinThreads(out res, out tmp);
                return res;
            }
            set
            {
                int tmp = 0;
                int portCompTh = 0;
                System.Threading.ThreadPool.GetMinThreads(out tmp, out portCompTh);
                System.Threading.ThreadPool.SetMinThreads(value, portCompTh);
            }
        }

        private static readonly System.Threading.WaitCallback RunActionWaitCallback = new System.Threading.WaitCallback(RunAction);

        /// <summary>
        /// Выполнение действия переданного как состояние
        /// </summary>
        /// <param name="act">Действие</param>
        private static void RunAction(object act)
        {
            Contract.Requires(act != null);

            ((Action)act)();
        }


        // ====================================

        /// <summary>
        /// Добавление задачи для пула потоков
        /// </summary>
        /// <param name="item">Задача</param>
        protected sealed override void AddWorkItem(ThreadPoolWorkItem item)
        {
            System.Threading.ThreadPool.QueueUserWorkItem(ThreadPoolWorkItem.RunWaitCallback, item);
        }

        /// <summary>
        /// Попытаться добавить задачу в пул потоков
        /// </summary>
        /// <param name="item">Задача</param>
        /// <returns>Успешность</returns>
        protected sealed override bool TryAddWorkItem(ThreadPoolWorkItem item)
        {
            return System.Threading.ThreadPool.QueueUserWorkItem(ThreadPoolWorkItem.RunWaitCallback, item);
        }


        /// <summary>
        /// Исполнение метода в пуле потоков
        /// </summary>
        /// <param name="action">Делегат на выполняемый метод</param>
        public new void Run(Action action)
        {
            Contract.Requires(action != null);
            if (action == null)
                throw new ArgumentNullException("action");

            System.Threading.ThreadPool.QueueUserWorkItem(RunActionWaitCallback, action);
        }
        /// <summary>
        /// Попытаться исполнить метод в пуле потоков
        /// </summary>
        /// <param name="action">Делегат на выполняемый метод</param>
        /// <returns>Успшеность постановки в очередь (не гарантирует успешность запуска)</returns>
        public new bool TryRun(Action action)
        {
            Contract.Requires(action != null);
            if (action == null)
                throw new ArgumentNullException("action");

            return System.Threading.ThreadPool.QueueUserWorkItem(RunActionWaitCallback, action);
        }


        /// <summary>
        /// Исполнение метода с пользовательским параметром в пуле потоков
        /// </summary>
        /// <param name="action">Делегат на выполняемый метод</param>
        /// <param name="state">Пользовательский параметр</param>
        public void Run(System.Threading.WaitCallback action, object state)
        {
            Contract.Requires(action != null);
            System.Threading.ThreadPool.QueueUserWorkItem(action, state);
        }

        /// <summary>
        /// Попытаться исполнить метод с пользовательским параметром в пуле потоков
        /// </summary>
        /// <param name="action">Делегат на выполняемый метод</param>
        /// <param name="state">Пользовательский параметр</param>
        /// <returns>Успшеность постановки в очередь (не гарантирует успешность запуска)</returns>
        public bool TryRun(System.Threading.WaitCallback action, object state)
        {
            Contract.Requires(action != null);
            return System.Threading.ThreadPool.QueueUserWorkItem(action, state);
        }



        /// <summary>
        /// Запустить в другом контексте
        /// </summary>
        /// <param name="act">Действие</param>
        /// <param name="flowContext">Протаскивать ли ExecutionContext</param>
        void IContextSwitchSupplier.Run(Action act, bool flowContext)
        {
            if (flowContext)
                System.Threading.ThreadPool.QueueUserWorkItem(RunActionWaitCallback, act);
            else
                System.Threading.ThreadPool.UnsafeQueueUserWorkItem(RunActionWaitCallback, act);
        }

        /// <summary>
        /// Запустить в другом контексте
        /// </summary>
        /// <param name="act">Действие</param>
        /// <param name="state">Состояние</param>
        /// <param name="flowContext">Протаскивать ли ExecutionContext</param>
        void IContextSwitchSupplier.RunWithState(Action<object> act, object state, bool flowContext)
        {
            if (flowContext)
                System.Threading.ThreadPool.QueueUserWorkItem(new System.Threading.WaitCallback(act), state);
            else
                System.Threading.ThreadPool.UnsafeQueueUserWorkItem(new System.Threading.WaitCallback(act), state);
        }

        /// <summary>
        /// Переход на выполнение в пуле посредством await
        /// </summary>
        /// <returns>Объект смены контекста выполнения</returns>
        public sealed override ContextSwitchAwaitable SwitchToPool()
        {
            if (System.Threading.Thread.CurrentThread.IsThreadPoolThread)
                return new ContextSwitchAwaitable();

            return new ContextSwitchAwaitable(this);
        }


        /// <summary>
        /// Запуск действия с обёртыванием в Task
        /// </summary>
        /// <param name="action">Действие</param>
        /// <returns>Task</returns>
        public sealed override System.Threading.Tasks.Task RunAsTask(Action action)
        {
            return System.Threading.Tasks.Task.Run(action);
        }
        /// <summary>
        /// Запуск действия с обёртыванием в Task
        /// </summary>
        /// <typeparam name="TState">Тип параметра состояния</typeparam>
        /// <param name="action">Действие</param>
        /// <param name="state">Параметр состояния</param>
        /// <returns>Task</returns>
        public sealed override System.Threading.Tasks.Task RunAsTask<TState>(Action<TState> action, TState state)
        {
            if (action == null)
                throw new ArgumentNullException("action");
            var workItem = new ParameterizedClosure<TState>() { Action = action, State = state };
            return System.Threading.Tasks.Task.Factory.StartNew(ParameterizedClosure<TState>.RunAction, workItem);
        }

        /// <summary>
        /// Запуск функции с обёртыванием в Task
        /// </summary>
        /// <typeparam name="TRes">Тип результата</typeparam>
        /// <param name="func">Функций</param>
        /// <returns>Task</returns>
        public sealed override System.Threading.Tasks.Task<TRes> RunAsTask<TRes>(Func<TRes> func)
        {
            return System.Threading.Tasks.Task.Run(func);
        }
        /// <summary>
        /// Запуск функции с обёртыванием в Task
        /// </summary>
        /// <typeparam name="TState">Тип параметра состояния</typeparam>
        /// <typeparam name="TRes">Тип результата</typeparam>
        /// <param name="func">Функций</param>
        /// <param name="state">Параметр состояния</param>
        /// <returns>Task</returns>
        public sealed override System.Threading.Tasks.Task<TRes> RunAsTask<TState, TRes>(Func<TState, TRes> func, TState state)
        {
            if (func == null)
                throw new ArgumentNullException("func");
            var workItem = new ParameterizedClosure<TState, TRes>() { Action = func, State = state };
            return System.Threading.Tasks.Task.Factory.StartNew(ParameterizedClosure<TState, TRes>.RunAction, workItem);
        }
    }
}
