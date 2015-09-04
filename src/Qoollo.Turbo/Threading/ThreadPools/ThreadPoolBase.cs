using Qoollo.Turbo.Threading.AsyncAwaitSupport;
using Qoollo.Turbo.Threading.ThreadPools.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Threading.ThreadPools
{
    /// <summary>
    /// Базовый класс пула потоков
    /// </summary>
    [ContractClass(typeof(ThreadPoolBaseCodeContractCheck))]
    public abstract class ThreadPoolBase : IConsumer<Action>, IDisposable
    {
        /// <summary>
        /// Добавление задачи для пула потоков
        /// </summary>
        /// <param name="item">Задача</param>
        protected abstract void AddWorkItem(ThreadPoolWorkItem item);
        /// <summary>
        /// Попытаться добавить задачу в пул потоков
        /// </summary>
        /// <param name="item">Задача</param>
        /// <returns>Успешность</returns>
        protected abstract bool TryAddWorkItem(ThreadPoolWorkItem item);


        /// <summary>
        /// Исполнение метода в пуле потоков
        /// </summary>
        /// <param name="action">Делегат на выполняемый метод</param>
        public void Run(Action action)
        {
            Contract.Requires(action != null);
            if (action == null)
                throw new ArgumentNullException("action");

            AddWorkItem(new ActionThreadPoolWorkItem(action));
        }
        /// <summary>
        /// Попытаться исполнить метод в пуле потоков
        /// </summary>
        /// <param name="action">Делегат на выполняемый метод</param>
        /// <returns>Успшеность постановки в очередь (не гарантирует успешность запуска)</returns>
        public bool TryRun(Action action)
        {
            Contract.Requires(action != null);
            if (action == null)
                throw new ArgumentNullException("action");

            return TryAddWorkItem(new ActionThreadPoolWorkItem(action));
        }

        /// <summary>
        /// Исполнение метода с пользовательским параметром в пуле потоков
        /// </summary>
        /// <typeparam name="T">Тип пользовательского параметра</typeparam>
        /// <param name="action">Делегат на выполняемый метод</param>
        /// <param name="state">Пользовательский параметр</param>
        public void Run<T>(Action<T> action, T state)
        {
            Contract.Requires(action != null);
            if (action == null)
                throw new ArgumentNullException("action");

            AddWorkItem(new ActionThreadPoolWorkItem<T>(action, state));
        }

        /// <summary>
        /// Попытаться исполнить метод с пользовательским параметром в пуле потоков
        /// </summary>
        /// <typeparam name="T">Тип пользовательского параметра</typeparam>
        /// <param name="action">Делегат на выполняемый метод</param>
        /// <param name="state">Пользовательский параметр</param>
        /// <returns>Успшеность постановки в очередь (не гарантирует успешность запуска)</returns>
        public bool TryRun<T>(Action<T> action, T state)
        {
            Contract.Requires(action != null);
            if (action == null)
                throw new ArgumentNullException("action");

            return TryAddWorkItem(new ActionThreadPoolWorkItem<T>(action, state));
        }


        /// <summary>
        /// Запуск действия с обёртыванием в Task
        /// </summary>
        /// <param name="action">Действие</param>
        /// <returns>Task</returns>
        public virtual Task RunAsTask(Action action)
        {
            Contract.Requires(action != null);
            if (action == null)
                throw new ArgumentNullException("action");

            var item = new TaskThreadPoolWorkItem(action);
            AddWorkItem(item);
            return item.Task;
        }
        /// <summary>
        /// Запуск действия с обёртыванием в Task
        /// </summary>
        /// <typeparam name="TState">Тип параметра состояния</typeparam>
        /// <param name="action">Действие</param>
        /// <param name="state">Параметр состояния</param>
        /// <returns>Task</returns>
        public virtual Task RunAsTask<TState>(Action<TState> action, TState state)
        {
            Contract.Requires(action != null);
            if (action == null)
                throw new ArgumentNullException("action");

            var item = new TaskThreadPoolWorkItem<TState>(action, state);
            AddWorkItem(item);
            return item.Task;
        }

        /// <summary>
        /// Запуск функции с обёртыванием в Task
        /// </summary>
        /// <typeparam name="TRes">Тип результата</typeparam>
        /// <param name="func">Функций</param>
        /// <returns>Task</returns>
        public virtual Task<TRes> RunAsTask<TRes>(Func<TRes> func)
        {
            Contract.Requires(func != null);
            if (func == null)
                throw new ArgumentNullException("func");

            var item = new TaskFuncThreadPoolWorkItem<TRes>(func);
            AddWorkItem(item);
            return item.Task;
        }
        /// <summary>
        /// Запуск функции с обёртыванием в Task
        /// </summary>
        /// <typeparam name="TState">Тип параметра состояния</typeparam>
        /// <typeparam name="TRes">Тип результата</typeparam>
        /// <param name="func">Функций</param>
        /// <param name="state">Параметр состояния</param>
        /// <returns>Task</returns>
        public virtual Task<TRes> RunAsTask<TState, TRes>(Func<TState, TRes> func, TState state)
        {
            Contract.Requires(func != null);
            if (func == null)
                throw new ArgumentNullException("func");

            var item = new TaskFuncThreadPoolWorkItem<TState, TRes>(func, state);
            AddWorkItem(item);
            return item.Task;
        }


        /// <summary>
        /// Переход на выполнение в пуле посредством await
        /// </summary>
        /// <returns>Объект смены контекста выполнения</returns>
        public virtual ContextSwitchAwaitable SwitchToPool()
        {
            return new ContextSwitchAwaitable(new SingleDelegateNoFlowContextSwitchSupplier(Run));
        }

        /// <summary>
        /// Добавить элемент
        /// </summary>
        /// <param name="item">Элемент</param>
        void IConsumer<Action>.Add(Action item)
        {
            this.Run(item);
        }
        /// <summary>
        /// Попытаться добавить элемент
        /// </summary>
        /// <param name="item">Элемент</param>
        /// <returns>Успешность</returns>
        bool IConsumer<Action>.TryAdd(Action item)
        {
            return this.TryRun(item);
        }

        /// <summary>
        /// Основной код освобождения ресурсов
        /// </summary>
        /// <param name="isUserCall">Вызвано ли освобождение пользователем. False - деструктор</param>
        protected virtual void Dispose(bool isUserCall)
        {
        }

        /// <summary>
        /// Освобождение ресурсов
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }







    /// <summary>
    /// Контракты
    /// </summary>
    [ContractClassFor(typeof(ThreadPoolBase))]
    abstract class ThreadPoolBaseCodeContractCheck : ThreadPoolBase
    {
        /// <summary>Контракты</summary>
        private ThreadPoolBaseCodeContractCheck() { }

        /// <summary>Контракты</summary>
        protected override void AddWorkItem(ThreadPoolWorkItem item)
        {
            Contract.Requires(item != null);

            throw new NotImplementedException();
        }

        /// <summary>Контракты</summary>
        protected override bool TryAddWorkItem(ThreadPoolWorkItem item)
        {
            Contract.Requires(item != null);

            throw new NotImplementedException();
        }
    }
}
