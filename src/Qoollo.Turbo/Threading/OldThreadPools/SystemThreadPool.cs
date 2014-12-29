using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Text;
using Qoollo.Turbo.Threading.OldThreadPools.ServiceStuff;

namespace Qoollo.Turbo.Threading.OldThreadPools
{
    /// <summary>
    /// Системный пул потоков
    /// </summary>
    internal class SystemThreadPool : ThreadPoolBase, IContextSwitchSupplier
    {
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

        /// <summary>
        /// Выполнение действия переданного как состояние
        /// </summary>
        /// <param name="act">Действие</param>
        private static void RunAction(object act)
        {
            Contract.Requires(act != null);

            ((Action)act)();
        }

        /// <summary>
        /// Добавление задачи для пула потоков
        /// </summary>
        /// <param name="item">Задача</param>
        protected override void AddWorkItem(ThreadPoolWorkItem item)
        {
            System.Threading.ThreadPool.QueueUserWorkItem(RunAction, item.Act);
        }

        /// <summary>
        /// Попытаться добавить задачу в пул потоков
        /// </summary>
        /// <param name="item">Задача</param>
        /// <returns>Успешность</returns>
        protected override bool TryAddWorkItem(ThreadPoolWorkItem item)
        {
            return System.Threading.ThreadPool.QueueUserWorkItem(RunAction, item.Act);
        }


        /// <summary>
        /// Запустить в другом контексте
        /// </summary>
        /// <param name="act">Действие</param>
        /// <param name="flowContext">Протаскивать ли ExecutionContext</param>
        void IContextSwitchSupplier.Run(Action act, bool flowContext)
        {
            if (flowContext)
                System.Threading.ThreadPool.QueueUserWorkItem(RunAction, act);
            else
                System.Threading.ThreadPool.UnsafeQueueUserWorkItem(RunAction, act);
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
        public override ContextSwitchAwaitable SwitchToPool()
        {
            if (System.Threading.Thread.CurrentThread.IsThreadPoolThread)
                return new ContextSwitchAwaitable();

            return new ContextSwitchAwaitable(this);
        }


        /// <summary>
        /// Запуск действия с обёртыванием в Task
        /// </summary>
        /// <param name="act">Действие</param>
        /// <returns>Task</returns>
        public override System.Threading.Tasks.Task RunAsTask(Action act)
        {
            return System.Threading.Tasks.Task.Run(act);
        }

        /// <summary>
        /// Запуск функции с обёртыванием в Task
        /// </summary>
        /// <typeparam name="T">Тип результата</typeparam>
        /// <param name="fnc">Функций</param>
        /// <returns>Task</returns>
        public override System.Threading.Tasks.Task<T> RunAsTask<T>(Func<T> fnc)
        {
            return System.Threading.Tasks.Task.Run(fnc);
        }
    }
}
