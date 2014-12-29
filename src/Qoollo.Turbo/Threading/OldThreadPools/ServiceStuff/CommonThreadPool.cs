using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Threading.OldThreadPools.ServiceStuff
{
    /// <summary>
    /// Общий вспомогательный класс для пула потоков
    /// </summary>
    internal abstract class CommonThreadPool : ThreadPoolBase, ICustomSynchronizationContextSupplier, IContextSwitchSupplier
    {
        /// <summary>
        /// Контракты
        /// </summary>
        [ContractInvariantMethod]
        private void Invariant()
        {
            Contract.Invariant(_isPoolThread != null);
        }

        /// <summary>
        /// True для потоков из пула
        /// </summary>
        private ThreadLocal<bool> _isPoolThread;

        private bool _isBackgroundThreads;
        
        private string _threadName;
        private int _currentThreadNum;

        private bool _restoreExecutionContext;

        /// <summary>
        /// Собственный контекст синхронизации (если null, то не применяется)
        /// </summary>
        private CustomSynchronizationContext _synchroContext;

        /// <summary>
        /// Конструктор CommonThreadPool
        /// </summary>
        /// <param name="isBackground">Фоновые ли потоки</param>
        /// <param name="name">Имя пула</param>
        /// <param name="useOwnSyncContext">Устанавливать ли собственный контекст синхронизации</param>
        /// <param name="flowContext">Протаскивать ли контекст исполнения</param>
        public CommonThreadPool(bool isBackground, string name, bool useOwnSyncContext, bool flowContext)
        {
            _isBackgroundThreads = isBackground;
            _threadName = name ?? "Pool thread";
            _restoreExecutionContext = flowContext;

            if (useOwnSyncContext)
                _synchroContext = new CustomSynchronizationContext(this);

            _isPoolThread = new ThreadLocal<bool>();
        }

        /// <summary>
        /// Обработка свежей задачи при поступлении
        /// </summary>
        /// <param name="item">Задача</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void OnNewWorkItem(ref ThreadPoolWorkItem item)
        {
            if (_restoreExecutionContext && !ExecutionContext.IsFlowSuppressed())
                item.CapturedContext = ExecutionContext.Capture();

            item.StartStoreTimer();
        }

        /// <summary>
        /// Добавление задачи для пула потоков
        /// </summary>
        /// <param name="item">Задача</param>
        protected sealed override void AddWorkItem(ThreadPoolWorkItem item)
        {
            OnNewWorkItem(ref item);
            AddWorkItemInner(item);
        }
        /// <summary>
        /// Попытаться добавить задачу в пул потоков
        /// </summary>
        /// <param name="item">Задача</param>
        /// <returns>Успешность</returns>
        protected sealed override bool TryAddWorkItem(ThreadPoolWorkItem item)
        {
            OnNewWorkItem(ref item);
            return TryAddWorkItemInner(item);
        }

        /// <summary>
        /// Добавление задачи для пула потоков
        /// </summary>
        /// <param name="item">Задача</param>
        protected abstract void AddWorkItemInner(ThreadPoolWorkItem item);
        /// <summary>
        /// Попытаться добавить задачу в пул потоков
        /// </summary>
        /// <param name="item">Задача</param>
        /// <returns>Успешность</returns>
        protected abstract bool TryAddWorkItemInner(ThreadPoolWorkItem item);
        
        /// <summary>
        /// Проверка, принадлежит ли текущий поток пулу
        /// </summary>
        /// <returns>ДА или НЕТ</returns>
        public bool IsPoolThread()
        {
            return _isPoolThread.Value;
        }

        /// <summary>
        /// Имя потоков пула
        /// </summary>
        protected string Name
        {
            get { return _threadName; }
        }


        /// <summary>
        /// Создаёт новый объект потока
        /// </summary>
        /// <param name="start">Метод потока</param>
        /// <param name="startController">Контроллер запуска потока</param>
        /// <returns>Созданный поток</returns>
        protected Thread CreateNewThread(ThreadStart start, ThreadStartControllingToken startController)
        {
            Contract.Requires(start != null);
            Contract.Requires(startController != null);
            Contract.Ensures(Contract.Result<Thread>() != null);

            // Для замыкания создаём локальные копии
            var syncContext = _synchroContext;
            var isPoolThread = _isPoolThread;

            Thread res = new Thread(() =>
            {
                SpinWait sw = new SpinWait();
                while (!startController.IsInitialized)
                    sw.SpinOnce();

                if (!startController.IsOk)
                    return;

                DoOnThreadStart(isPoolThread, syncContext);
                start();
            });

            int threadNum = Interlocked.Increment(ref _currentThreadNum);
            res.IsBackground = _isBackgroundThreads;
            if (_threadName != null)
                res.Name = string.Format("{0}: {1} (#{2})", this.GetType().Name, _threadName, threadNum);

            return res;
        }

        /// <summary>
        /// Действия, выполняемые при запуске потока
        /// </summary>
        /// <param name="isPoolThread">Переменная, определяющая, принадлежит ли поток данному пулу</param>
        /// <param name="syncContext">Контекст синхронизации</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void DoOnThreadStart(ThreadLocal<bool> isPoolThread, SynchronizationContext syncContext)
        {
            isPoolThread.Value = true;

            if (syncContext != null)
                SynchronizationContext.SetSynchronizationContext(syncContext);
        }

        /// <summary>
        /// Запуск задания
        /// </summary>
        /// <param name="item">Задача</param>
        /// <param name="suppressContextRestore">Подавлять ли восстановление контекста исполнения</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void RunWorkItem(ThreadPoolWorkItem item, bool suppressContextRestore = false)
        {
            item.RunJob(_synchroContext, _restoreExecutionContext && !suppressContextRestore);
        }


        /// <summary>
        /// Переход на выполнение в пуле посредством await
        /// </summary>
        /// <returns>Объект смены контекста выполнения</returns>
        public override ContextSwitchAwaitable SwitchToPool()
        {
            if (_isPoolThread.Value)
                return new ContextSwitchAwaitable();

            return new ContextSwitchAwaitable(this);
        }


        /// <summary>
        /// Запустить в другом контексте
        /// </summary>
        /// <param name="act">Действие</param>
        /// <param name="flowContext">Протаскивать ли ExecutionContext</param>
        void IContextSwitchSupplier.Run(Action act, bool flowContext)
        {
            ThreadPoolWorkItem item = new ThreadPoolWorkItem(act);
            if (flowContext && _restoreExecutionContext && !ExecutionContext.IsFlowSuppressed())
                item.CapturedContext = ExecutionContext.Capture();

            this.AddWorkItem(item);
        }

        /// <summary>
        /// Запустить в другом контексте
        /// </summary>
        /// <param name="act">Действие</param>
        /// <param name="state">Состояние</param>
        /// <param name="flowContext">Протаскивать ли ExecutionContext</param>
        void IContextSwitchSupplier.RunWithState(Action<object> act, object state, bool flowContext)
        {
            ThreadPoolWorkItem item = new ThreadPoolWorkItem(() => act(state));
            if (flowContext && _restoreExecutionContext && !ExecutionContext.IsFlowSuppressed())
                item.CapturedContext = ExecutionContext.Capture();

            this.AddWorkItem(item);
        }


        /// <summary>
        /// Асинхронное выполнение задания
        /// </summary>
        /// <param name="act">Задание</param>
        /// <param name="state">Состояние</param>
        void ICustomSynchronizationContextSupplier.RunAsync(SendOrPostCallback act, object state)
        {
            this.RunWithState(new Action<object>(act), state);
        }

        /// <summary>
        /// Синхронное выполнение задание
        /// </summary>
        /// <param name="act">Задание</param>
        /// <param name="state">Состояние</param>
        void ICustomSynchronizationContextSupplier.RunSync(SendOrPostCallback act, object state)
        {
            ManualResetEventSlim mresEv = null;
            try
            {
                mresEv = new ManualResetEventSlim(false);
                this.Run(() =>
                {
                    try
                    {
                        act(state);
                    }
                    finally
                    {
                        try
                        {
                            mresEv.Set();
                        }
                        catch { }
                    }
                });
                mresEv.Wait();
            }
            finally
            {
                if (mresEv != null)
                    mresEv.Dispose();
            }
        }
    }
}
