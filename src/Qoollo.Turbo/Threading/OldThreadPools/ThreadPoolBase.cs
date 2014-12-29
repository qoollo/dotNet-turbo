using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Runtime;
using Qoollo.Turbo.Threading.OldThreadPools.ServiceStuff;
using Qoollo.Turbo.Threading.AsyncAwaitSupport;

namespace Qoollo.Turbo.Threading.OldThreadPools
{
    /// <summary>
    /// Базовый класс пула потоков
    /// </summary>
    [ContractClass(typeof(ThreadPoolBaseCodeContractCheck))]
    internal abstract class ThreadPoolBase : IConsumer<Action>, IDisposable
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
        /// <param name="act">Делегат на выполняемый метод</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Run(Action act)
        {
            if (act == null)
                throw new ArgumentNullException("act");

            AddWorkItem(new ThreadPoolWorkItem(act));
        }
        /// <summary>
        /// Попытаться исполнить метод в пуле потоков
        /// </summary>
        /// <param name="act">Делегат на выполняемый метод</param>
        /// <returns>Успшеность постановки в очередь (не гарантирует успешность запуска)</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRun(Action act)
        {
            if (act == null)
                throw new ArgumentNullException("act");

            return TryAddWorkItem(new ThreadPoolWorkItem(act));
        }

        /// <summary>
        /// Исполнение метода с пользовательским параметром в пуле потоков
        /// </summary>
        /// <typeparam name="T">Тип пользовательского параметра</typeparam>
        /// <param name="act">Делегат на выполняемый метод</param>
        /// <param name="state">Пользовательский параметр</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RunWithState<T>(Action<T> act, T state)
        {
            if (act == null)
                throw new ArgumentNullException("act");

            AddWorkItem(new ThreadPoolWorkItem(() => act(state)));
        }

        /// <summary>
        /// Попытаться исполнить метод с пользовательским параметром в пуле потоков
        /// </summary>
        /// <typeparam name="T">Тип пользовательского параметра</typeparam>
        /// <param name="act">Делегат на выполняемый метод</param>
        /// <param name="state">Пользовательский параметр</param>
        /// <returns>Успшеность постановки в очередь (не гарантирует успешность запуска)</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRunWithState<T>(Action<T> act, T state)
        {
            if (act == null)
                throw new ArgumentNullException("act");

            return TryAddWorkItem(new ThreadPoolWorkItem(() => act(state)));
        }


        /// <summary>
        /// Запуск действия с обёртыванием в Task
        /// </summary>
        /// <param name="act">Действие</param>
        /// <returns>Task</returns>
        public virtual Task RunAsTask(Action act)
        {
            if (act == null)
                throw new ArgumentNullException("act");

            var tcs = new TaskCompletionSource<object>();

            this.Run(() =>
                {
                    try
                    {
                        act();
                        tcs.SetResult(null);
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                });

            return tcs.Task;
        }

        /// <summary>
        /// Запуск функции с обёртыванием в Task
        /// </summary>
        /// <typeparam name="T">Тип результата</typeparam>
        /// <param name="fnc">Функций</param>
        /// <returns>Task</returns>
        public virtual Task<T> RunAsTask<T>(Func<T> fnc)
        {
            if (fnc == null)
                throw new ArgumentNullException("fnc");

            var tcs = new TaskCompletionSource<T>();

            this.Run(() =>
            {
                try
                {
                    var res = fnc();
                    tcs.SetResult(res);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });

            return tcs.Task;
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
            throw new NotImplementedException();
        }

        /// <summary>Контракты</summary>
        protected override bool TryAddWorkItem(ThreadPoolWorkItem item)
        {
            throw new NotImplementedException();
        }
    }
}
