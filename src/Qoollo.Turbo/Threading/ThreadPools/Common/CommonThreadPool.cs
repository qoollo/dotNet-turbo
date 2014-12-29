using Qoollo.Turbo.Threading.ThreadPools.ServiceStuff;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Threading.ThreadPools.Common
{
    /// <summary>
    /// Общий код для пулов потоков
    /// </summary>
    public abstract class CommonThreadPool : ThreadPoolBase, ICustomSynchronizationContextSupplier, IContextSwitchSupplier
    {
        /// <summary>
        /// Контекст синхронизации для CommonThreadPool
        /// </summary>
        private class CommonThreadPoolSynchronizationContext : SynchronizationContext
        {
            private readonly CommonThreadPool _srcPool;

            /// <summary>
            /// Конструктор CommonThreadPoolSynchronizationContext
            /// </summary>
            /// <param name="srcPool">Пул</param>
            public CommonThreadPoolSynchronizationContext(CommonThreadPool srcPool)
            {
                Contract.Requires(srcPool != null);
                _srcPool = srcPool;
            }

            /// <summary>
            /// Создание копии
            /// </summary>
            /// <returns>Копия</returns>
            public override SynchronizationContext CreateCopy()
            {
                return new CommonThreadPoolSynchronizationContext(_srcPool);
            }
            /// <summary>
            /// Асинхронный запуск задания
            /// </summary>
            /// <param name="d">Задание</param>
            /// <param name="state">Параметр</param>
            public override void Post(SendOrPostCallback d, object state)
            {
                var item = new SendOrPostCallbackThreadPoolWorkItem(d, state, true, false);
                _srcPool.AddWorkItem(item);
            }
            /// <summary>
            /// Синхронный запуск задания
            /// </summary>
            /// <param name="d">Задание</param>
            /// <param name="state">Параметр</param>
            public override void Send(SendOrPostCallback d, object state)
            {
                var item = new SendOrPostCallbackSyncThreadPoolWorkItem(d, state, true, true);
                _srcPool.AddWorkItem(item);
                item.Wait();
            }
        }

        // =====================

        /// <summary>
        /// Шедулер для пула потоков
        /// </summary>
        private class CommonThreadPoolTaskScheduler: TaskScheduler
        {
            private readonly CommonThreadPool _srcPool;
            /// <summary>
            /// Конструктор CommonThreadPoolTaskScheduller
            /// </summary>
            /// <param name="srcPool">Пул потоков</param>
            public CommonThreadPoolTaskScheduler(CommonThreadPool srcPool)
            {
                Contract.Requires(srcPool != null);
                _srcPool = srcPool;
            }
            /// <summary>
            /// Список задач для отладки
            /// </summary>
            protected override IEnumerable<Task> GetScheduledTasks()
            {
                return null;
            }
            /// <summary>
            /// Добавить задачу в очередь
            /// </summary>
            /// <param name="task">Задача</param>
            protected override void QueueTask(Task task)
            {
                _srcPool.AddWorkItem(new TaskEntryExecutionThreadPoolWorkItem(task, false));
            }
            /// <summary>
            /// Попробовать запустить синхронно
            /// </summary>
            /// <param name="task">Задача</param>
            /// <param name="taskWasPreviouslyQueued">Была ли задача добавлена</param>
            /// <returns>Успешность</returns>
            protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
            {
                return !taskWasPreviouslyQueued && _srcPool.IsThreadPoolThread && base.TryExecuteTask(task);
            }
        }


        // =====================

        /// <summary>
        /// Внутренние данные для каждого потока пула
        /// </summary>
        protected class ThreadPrivateData
        {
            internal ThreadPrivateData(ThreadPoolThreadLocals localData)
            {
                Contract.Requires(localData != null);
                LocalData = localData;
            }

            /// <summary>
            /// Локальные данные потока
            /// </summary>
            internal readonly ThreadPoolThreadLocals LocalData;
        }

        // =====================


        /// <summary>
        /// Данные для запуска потока
        /// </summary>
        private class ThreadStartUpData
        {
            public ThreadStartUpData(Action<ThreadPrivateData, CancellationToken> threadProc, ThreadStartControllingToken startController, bool createThreadLocalQueue)
            {
                Contract.Requires(threadProc != null);
                Contract.Requires(startController != null);

                ThreadProcedure = threadProc;
                StartController = startController;
                CreateThreadLocalQueue = createThreadLocalQueue;
            }

            public readonly Action<ThreadPrivateData, CancellationToken> ThreadProcedure;
            public readonly ThreadStartControllingToken StartController;
            public readonly bool CreateThreadLocalQueue;
        }


        // =====================


        // Число ядер
        private static readonly int ProcessorCount = Environment.ProcessorCount;

        // =============

        private readonly bool _isBackgroundThreads;

        private readonly string _name;
        private int _currentThreadNum;


        /// <summary>
        /// Список всех активных потоков
        /// </summary>
        private readonly List<Thread> _activeThread;
        private int _activeThreadCount;


        private readonly bool _restoreExecutionContext;
        private readonly bool _useOwnSyncContext;
        private readonly bool _useOwnTaskScheduler;
        private readonly CommonThreadPoolSynchronizationContext _synchroContext;
        private readonly CommonThreadPoolTaskScheduler _taskScheduler;

        /// <summary>
        /// Общие данные для пула потоков (очередь там же)
        /// </summary>
        private readonly ThreadPoolGlobals _threadPoolGlobals;

        private int _threadPoolState;
        private readonly CancellationTokenSource _threadRunningCancellation;

        private readonly ManualResetEventSlim _stopWaiter;
        private volatile bool _letFinishProcess;
        private volatile bool _completeAdding;
        

        /// <summary>
        /// Конструктор CommonThreadPool
        /// </summary>
        /// <param name="queueBoundedCapacity">Ограничение на размер очереди</param>
        /// <param name="queueStealAwakePeriod">Периоды сна между проверкой возможности похитить элемент из соседних локальных очередей</param>
        /// <param name="isBackground">Фоновые ли потоки</param>
        /// <param name="name">Имя пула</param>
        /// <param name="useOwnTaskScheduler">Использовать ли свой шедулер Task'ов</param>
        /// <param name="useOwnSyncContext">Устанавливать ли собственный контекст синхронизации</param>
        /// <param name="flowExecutionContext">Протаскивать ли контекст исполнения</param>
        public CommonThreadPool(int queueBoundedCapacity, int queueStealAwakePeriod, bool isBackground, string name, bool useOwnTaskScheduler, bool useOwnSyncContext, bool flowExecutionContext)
        {
            _isBackgroundThreads = isBackground;
            _name = name ?? this.GetType().GetCSName();
            _restoreExecutionContext = flowExecutionContext;
            _useOwnSyncContext = useOwnSyncContext;
            _useOwnTaskScheduler = useOwnTaskScheduler;

            _synchroContext = new CommonThreadPoolSynchronizationContext(this);
            _taskScheduler = new CommonThreadPoolTaskScheduler(this);

            _threadPoolGlobals = new ThreadPoolGlobals(queueBoundedCapacity, queueStealAwakePeriod);
            _activeThread = new List<Thread>(Math.Min(100, Math.Max(2, ProcessorCount)));
            _activeThreadCount = 0;

            _threadPoolState = (int)ThreadPoolState.Created;
            _threadRunningCancellation = new CancellationTokenSource();
            _stopWaiter = new ManualResetEventSlim(false);
            _letFinishProcess = false;
            _completeAdding = false;

            Turbo.Profiling.Profiler.ThreadPoolCreated(Name);
        }


        /// <summary>
        /// Имя пула потоков
        /// </summary>
        public string Name
        {
            get { return _name; }
        }
        /// <summary>
        /// Работают ли потоке в фоновом режиме
        /// </summary>
        public bool IsBackgroundThreads
        {
            get { return _isBackgroundThreads; }
        }
        /// <summary>
        /// Число активных потоков
        /// </summary>
        public int ThreadCount
        {
            get { return Volatile.Read(ref _activeThreadCount); }
        }
        /// <summary>
        /// Ограничение на максимальный размер очереди задач (-1 - нет ограничения)
        /// </summary>
        public int QueueCapacity
        {
            get { return _threadPoolGlobals.GlobalQueue.BoundedCapacity; }
        }
        /// <summary>
        /// Расширенная вместимость очереди задач
        /// </summary>
        protected int ExtendedQueueCapacity
        {
            get { return _threadPoolGlobals.GlobalQueue.ExtendedCapacity; }
        }
        /// <summary>
        /// Число элементов в общей очереди
        /// </summary>
        protected int GlobalQueueWorkItemCount
        {
            get { return _threadPoolGlobals.GlobalQueue.OccupiedNodesCount; }
        }
        /// <summary>
        /// Запрещено ли добавление новых задач в пул
        /// </summary>
        public bool IsAddingCompleted
        {
            get { return _completeAdding; }
        }
        /// <summary>
        /// Можно ли закончить обработку существующих задач
        /// </summary>
        protected bool LetFinishedProcess
        {
            get { return _letFinishProcess; }
        }
        /// <summary>
        /// Находимся ли в процессе остановки
        /// </summary>
        protected bool IsStopRequestedOrStopped
        {
            get
            {
                var state = State;
                return state == ThreadPoolState.Stopped || state == ThreadPoolState.StopRequested;
            }
        }
        /// <summary>
        /// Находится ли пул потоков в рабочем состоянии
        /// </summary>
        public bool IsWork
        {
            get
            {
                var state = State;
                return state == ThreadPoolState.Running || state == ThreadPoolState.Created;
            }
        }
        /// <summary>
        /// Принадлежит ли текущий поток пулу
        /// </summary>
        public bool IsThreadPoolThread
        {
            get { return _threadPoolGlobals.IsThreadPoolThread; }
        }
        /// <summary>
        /// Контекст синхронизации
        /// </summary>
        protected SynchronizationContext SynchronizationContext
        {
            get { return _synchroContext; }
        }
        /// <summary>
        /// Шедуллер задач
        /// </summary>
        public TaskScheduler TaskScheduler
        {
            get { return _taskScheduler; }
        }

        /// <summary>
        /// Состояние пула потоков
        /// </summary>
        public ThreadPoolState State
        {
            get { return (ThreadPoolState)Volatile.Read(ref _threadPoolState); }
        }
        /// <summary>
        /// Допустима ли смена состояния
        /// </summary>
        /// <param name="oldState">Старое состояние</param>
        /// <param name="newState">Новое состояние</param>
        /// <returns>Допустим ли переход</returns>
        private bool IsValidStateTransition(ThreadPoolState oldState, ThreadPoolState newState)
        {
            switch (oldState)
            {
                case ThreadPoolState.Created:
                    return newState == ThreadPoolState.Running || newState == ThreadPoolState.StopRequested || newState == ThreadPoolState.Stopped;
                case ThreadPoolState.Running:
                    return newState == ThreadPoolState.StopRequested;
                case ThreadPoolState.StopRequested:
                    return newState == ThreadPoolState.Stopped;
                case ThreadPoolState.Stopped:
                    return false;
                default:
                    return false;
            }
        }
        /// <summary>
        /// Безопасно сменить состояние
        /// </summary>
        /// <param name="newState">Новое состояние</param>
        /// <param name="prevState">Состояние, которое было до смены</param>
        /// <returns>Произошла ли смена</returns>
        private bool ChangeStateSafe(ThreadPoolState newState, out ThreadPoolState prevState)
        {
            prevState = (ThreadPoolState)Volatile.Read(ref _threadPoolState);

            if (!IsValidStateTransition(prevState, newState))
                return false;

            SpinWait sw = new SpinWait();
            while (Interlocked.CompareExchange(ref _threadPoolState, (int)newState, (int)prevState) != (int)prevState)
            {
                sw.SpinOnce();
                prevState = (ThreadPoolState)Volatile.Read(ref _threadPoolState);
                if (!IsValidStateTransition(prevState, newState))
                    return false;
            }

            return true;
        }
        /// <summary>
        /// Обработчик перехода в состояние Stopped
        /// </summary>
        private void OnGoToStoppedState()
        {
            Contract.Assert(State == ThreadPoolState.Stopped);

            Contract.Assert(_stopWaiter.IsSet == false);
            _stopWaiter.Set();
            _threadPoolGlobals.Dispose();
            Profiling.Profiler.ThreadPoolDisposed(Name, false);
        }


        /// <summary>
        /// Исполнение метода в пуле потоков
        /// </summary>
        /// <param name="action">Делегат на выполняемый метод</param>
        /// <param name="preferFairness">Требование постановки в основную очередь</param>
        public void Run(Action action, bool preferFairness)
        {
            Contract.Requires(action != null);
            if (action == null)
                throw new ArgumentNullException("action");

            AddWorkItem(new ActionThreadPoolWorkItem(action, true, preferFairness));
        }
        /// <summary>
        /// Попытаться исполнить метод в пуле потоков
        /// </summary>
        /// <param name="action">Делегат на выполняемый метод</param>
        /// <param name="preferFairness">Требование постановки в основную очередь</param>
        /// <returns>Успшеность постановки в очередь (не гарантирует успешность запуска)</returns>
        public bool TryRun(Action action, bool preferFairness)
        {
            Contract.Requires(action != null);
            if (action == null)
                throw new ArgumentNullException("action");

            return TryAddWorkItem(new ActionThreadPoolWorkItem(action, true, preferFairness));
        }

        /// <summary>
        /// Исполнение метода с пользовательским параметром в пуле потоков
        /// </summary>
        /// <typeparam name="T">Тип пользовательского параметра</typeparam>
        /// <param name="action">Делегат на выполняемый метод</param>
        /// <param name="state">Пользовательский параметр</param>
        /// <param name="preferFairness">Требование постановки в основную очередь</param>
        public void Run<T>(Action<T> action, T state, bool preferFairness)
        {
            Contract.Requires(action != null);
            if (action == null)
                throw new ArgumentNullException("action");

            AddWorkItem(new ActionWithStateThreadPoolWorkItem<T>(action, state, true, preferFairness));
        }

        /// <summary>
        /// Попытаться исполнить метод с пользовательским параметром в пуле потоков
        /// </summary>
        /// <typeparam name="T">Тип пользовательского параметра</typeparam>
        /// <param name="action">Делегат на выполняемый метод</param>
        /// <param name="state">Пользовательский параметр</param>
        /// <param name="preferFairness">Требование постановки в основную очередь</param>
        /// <returns>Успшеность постановки в очередь (не гарантирует успешность запуска)</returns>
        public bool TryRun<T>(Action<T> action, T state, bool preferFairness)
        {
            Contract.Requires(action != null);
            if (action == null)
                throw new ArgumentNullException("action");

            return TryAddWorkItem(new ActionWithStateThreadPoolWorkItem<T>(action, state, true, preferFairness));
        }


        /// <summary>
        /// Запуск действия с обёртыванием в Task
        /// </summary>
        /// <param name="action">Действие</param>
        /// <returns>Task</returns>
        public sealed override Task RunAsTask(Action action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            if (_useOwnTaskScheduler)
            {
                var result = new Task(action);
                result.Start(_taskScheduler);
                return result;
            }
            else
            {
                var item = new TaskThreadPoolWorkItem(action, TaskCreationOptions.None);
                AddWorkItem(item);
                return item.Task;
            }
        }

        /// <summary>
        /// Запуск функции с обёртыванием в Task
        /// </summary>
        /// <typeparam name="T">Тип результата</typeparam>
        /// <param name="func">Функций</param>
        /// <returns>Task</returns>
        public sealed override Task<T> RunAsTask<T>(Func<T> func)
        {
            if (func == null)
                throw new ArgumentNullException("func");

            if (_useOwnTaskScheduler)
            {
                var result = new Task<T>(func);
                result.Start(_taskScheduler);
                return result;
            }
            else
            {
                var item = new TaskThreadPoolWorkItem<T>(func, TaskCreationOptions.None);
                AddWorkItem(item);
                return item.Task;
            }
        }

        /// <summary>
        /// Запуск действия с обёртыванием в Task
        /// </summary>
        /// <param name="action">Действие</param>
        /// <param name="creationOptions">Параметры создания таска</param>
        /// <returns>Task</returns>
        public Task RunAsTask(Action action, TaskCreationOptions creationOptions)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            if (_useOwnTaskScheduler)
            {
                var result = new Task(action, creationOptions);
                result.Start(_taskScheduler);
                return result;
            }
            else
            {
                var item = new TaskThreadPoolWorkItem(action, creationOptions);
                AddWorkItem(item);
                return item.Task;
            }
        }

        /// <summary>
        /// Запуск функции с обёртыванием в Task
        /// </summary>
        /// <typeparam name="T">Тип результата</typeparam>
        /// <param name="func">Функций</param>
        /// <param name="creationOptions">Параметры создания таска</param>
        /// <returns>Task</returns>
        public Task<T> RunAsTask<T>(Func<T> func, TaskCreationOptions creationOptions)
        {
            if (func == null)
                throw new ArgumentNullException("func");

            if (_useOwnTaskScheduler)
            {
                var result = new Task<T>(func, creationOptions);
                result.Start(_taskScheduler);
                return result;
            }
            else
            {
                var item = new TaskThreadPoolWorkItem<T>(func, creationOptions);
                AddWorkItem(item);
                return item.Task;
            }
        }

        /// <summary>
        /// Переход на выполнение в пуле посредством await
        /// </summary>
        /// <returns>Объект смены контекста выполнения</returns>
        public sealed override ContextSwitchAwaitable SwitchToPool()
        {
            if (IsThreadPoolThread)
                return new ContextSwitchAwaitable();

            return new ContextSwitchAwaitable(this);
        }


        /// <summary>
        /// Ожидание полной остановки
        /// </summary>
        public void WaitUntilStop()
        {
            if (State == ThreadPoolState.Stopped)
                return;
            _stopWaiter.Wait();
            Contract.Assert(State == ThreadPoolState.Stopped);
        }

        /// <summary>
        /// Ожидание полной остановки с таймаутом
        /// </summary>
        /// <param name="timeout">Таймаут ожидания в миллисекундах</param>
        /// <returns>true - дождались, false - вышли по таймауту</returns>
        public bool WaitUntilStop(int timeout)
        {
            if (State == ThreadPoolState.Stopped)
                return true;
            return _stopWaiter.Wait(timeout);
        }

        /// <summary>
        /// Проверить, освобождён ли объект и если да, то вызвать исключение ObjectDisposedException
        /// </summary>
        protected void CheckDisposed()
        {
            if (State == ThreadPoolState.Stopped)
                throw new ObjectDisposedException(this.GetType().Name, "ThreadPool is Stopped");
        }
        /// <summary>
        /// Проверить, освобождён ли объект и если да, то вызвать исключение ObjectDisposedException
        /// </summary>
        protected void CheckPendingDisposeOrDisposed()
        {
            var state = State;
            if (state == ThreadPoolState.Stopped || state == ThreadPoolState.StopRequested)
                throw new ObjectDisposedException(this.GetType().Name, "ThreadPool has Stopped or StopRequested state");
        }
        

        /// <summary>
        /// Просканировать все потоки и получить информацию об их состояниях
        /// </summary>
        /// <param name="totalThreadCount">Общее число зарегистрированных потоков</param>
        /// <param name="runningCount">Число исполняющихся в данный момент потоков</param>
        /// <param name="waitingCount">Число заблокированных потоков</param>
        protected void ScanThreadStates(out int totalThreadCount, out int runningCount, out int waitingCount)
        {
            totalThreadCount = 0;
            runningCount = 0;
            waitingCount = 0;

            lock (_activeThread)
            {
                for (int i = 0; i < _activeThread.Count; i++)
                {
                    var curState = _activeThread[i].ThreadState;
                    if ((curState & ~ThreadState.Background) == ThreadState.Running)
                        runningCount++;
                    if ((curState & ThreadState.WaitSleepJoin) != 0)
                        waitingCount++;
                }
                totalThreadCount = _activeThread.Count;
            }
        }

        /// <summary>
        /// Получить токен отмены для остановки пула
        /// </summary>
        /// <returns>Токен отмены</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected CancellationToken GetThreadRunningCancellationToken()
        {
            return _threadRunningCancellation.Token;
        }


        /// <summary>
        /// Процедура для потоков, выполняющая начальную инициализацию и завершение
        /// </summary>
        /// <param name="rawData">Данные для запуска</param>
        private void ThreadStartUpProc(object rawData)
        {
            Contract.Requires(rawData != null);

            ThreadStartUpData startupData = (ThreadStartUpData)rawData;

            SpinWait sw = new SpinWait();
            while (!startupData.StartController.IsInitialized)
                sw.SpinOnce();

            if (!startupData.StartController.IsOk)
                return;

            if (_useOwnSyncContext)
                SynchronizationContext.SetSynchronizationContext(_synchroContext);

            try
            {
                Profiling.Profiler.ThreadPoolThreadCountChange(this.Name, this.ThreadCount);

                ThreadPoolState prevState;
                ChangeStateSafe(ThreadPoolState.Running, out prevState);
                if (prevState == ThreadPoolState.Stopped)
                    return;

                if (!_useOwnTaskScheduler)
                {
                    var threadData = new ThreadPrivateData(_threadPoolGlobals.GetOrCreateThisThreadData(startupData.CreateThreadLocalQueue));
                    startupData.ThreadProcedure(threadData, GetThreadRunningCancellationToken());
                }
                else
                {
                    Task threadTask = new Task(ThreadStartUpInsideTask, rawData, TaskCreationOptions.LongRunning);
                    Qoollo.Turbo.Threading.ServiceStuff.TaskHelper.SetTaskScheduler(threadTask, this.TaskScheduler);
                    Qoollo.Turbo.Threading.ServiceStuff.TaskHelper.ExecuteTaskEntry(threadTask, false);
                }
            }
            finally
            {
                _threadPoolGlobals.FreeThisThreadData();
                RemoveStoppedThread(Thread.CurrentThread);

                Profiling.Profiler.ThreadPoolThreadCountChange(this.Name, this.ThreadCount);
            }
        }

        /// <summary>
        /// Исполнение цикла потока внутри Task (для установки taskScheduler)
        /// </summary>
        /// <param name="rawData">Данные для запуска</param>
        private void ThreadStartUpInsideTask(object rawData)
        {
            Contract.Requires(rawData != null);

            ThreadStartUpData startupData = (ThreadStartUpData)rawData;

            var threadData = new ThreadPrivateData(_threadPoolGlobals.GetOrCreateThisThreadData(startupData.CreateThreadLocalQueue));
            startupData.ThreadProcedure(threadData, GetThreadRunningCancellationToken());
        }


        /// <summary>
        /// Создание нового потока
        /// </summary>
        /// <param name="customName">Дополнительное имя для потока</param>
        /// <returns>Созданный поток</returns>
        private Thread CreateNewThread(string customName)
        {
            Contract.Ensures(Contract.Result<Thread>() != null);

            Thread res = new Thread(ThreadStartUpProc);
            int threadNum = Interlocked.Increment(ref _currentThreadNum);
            res.IsBackground = _isBackgroundThreads;
            if (string.IsNullOrEmpty(customName))
                res.Name = string.Format("{0} (#{1})", _name, threadNum);
            else
                res.Name = string.Format("{0} (#{1}). {2}", _name, threadNum, customName);

            return res;
        }


        /// <summary>
        /// Добавить новый поток в пул потоков
        /// </summary>
        /// <param name="threadProc">Процедура исполнения потока</param>
        /// <param name="customName">Дополнительное имя для потока</param>
        /// <param name="createThreadLocalQueue">Создавать ли локальную очередь потока</param>
        /// <returns>Добавленный поток (null, если не удалось)</returns>
        protected Thread AddNewThread(Action<ThreadPrivateData, CancellationToken> threadProc, string customName = null, bool createThreadLocalQueue = true)
        {
            Contract.Requires(threadProc != null);

            if (State == ThreadPoolState.Stopped)
                return null;

            Thread res = null;
            bool success = false;
            ThreadStartControllingToken startController = null;
            try
            {
                lock (_activeThread)
                {
                    if (State == ThreadPoolState.Stopped)
                        return null;

                    startController = new ThreadStartControllingToken();
                    res = this.CreateNewThread(customName);
                    res.Start(new ThreadStartUpData(threadProc, startController, createThreadLocalQueue));

                    try { }
                    finally
                    {
                        _activeThread.Add(res);
                        Interlocked.Increment(ref _activeThreadCount);
                        success = true;
                    }

                    Contract.Assert(_activeThread.Count == _activeThreadCount);
                }
            }
            catch (Exception ex)
            {
                throw new CantInitThreadException("Exception during thread creation in ThreadPool '" + Name + "'", ex);
            }
            finally
            {
                if (success)
                {
                    startController.SetOk();
                }
                else
                {
                    if (startController != null)
                        startController.SetFail();
                    res = null;
                }
            }

            return res;
        }


        /// <summary>
        /// Хук на удаление потока
        /// </summary>
        /// <param name="elem">Удаляемый поток</param>
        protected virtual void OnThreadRemove(Thread elem)
        {
            Contract.Requires(elem != null);
        }

        /// <summary>
        /// Выполнить удаление завершающегося потока
        /// </summary>
        /// <param name="elem">Поток, который удаляем</param>
        private void RemoveStoppedThread(Thread elem)
        {
            Contract.Requires(elem != null);

            bool removeResult = false;
            lock (_activeThread)
            {
                try { }
                finally
                {
                    removeResult = _activeThread.Remove(elem);
                    if (removeResult)
                    {
                        Interlocked.Decrement(ref _activeThreadCount);

                        // Если поток последний и запрошена остановка, то переходим в состояние "остановлено"
                        if (_activeThread.Count == 0 && State == ThreadPoolState.StopRequested)
                        {
                            ThreadPoolState prevState;
                            bool stateChanged = ChangeStateSafe(ThreadPoolState.Stopped, out prevState);
                            Contract.Assert(stateChanged || prevState == ThreadPoolState.Stopped, "State was not changed to stopped");
                            if (stateChanged)
                                OnGoToStoppedState();
                        }
                    }
                    Contract.Assert(_activeThread.Count == _activeThreadCount);
                }
            }

            Contract.Assert(removeResult, "Remove Thread in Pool failed");
            if (removeResult)
                OnThreadRemove(elem);
        }

        /// <summary>
        /// Хук на остановку пула потоков
        /// </summary>
        /// <param name="waitForStop">Ожидать остановки</param>
        /// <param name="letFinishProcess">Позволить обработать всю очередь</param>
        /// <param name="completeAdding">Запретить добавление новых элементов</param>
        protected virtual void OnThreadPoolStop(bool waitForStop, bool letFinishProcess, bool completeAdding)
        {
        }

        /// <summary>
        /// Очистить основную очередь после остановки
        /// </summary>
        private void CleanUpMainQueueAfterStop()
        {
            Contract.Assert(State == ThreadPoolState.Stopped);

            ThreadPoolWorkItem item = null;
            while (_threadPoolGlobals.TryTakeItemSafeFromGlobalQueue(out item))
                this.CancelWorkItem(item);
        }

        /// <summary>
        /// Завершить добавление для всех последующих задач
        /// </summary>
        public void CompleteAdding()
        {
            _completeAdding = true;
            if (State == ThreadPoolState.Stopped)
                CleanUpMainQueueAfterStop();
        }

        /// <summary>
        /// Внутренняя остановка пула
        /// </summary>
        /// <param name="waitForStop">Ожидать остановки</param>
        /// <param name="letFinishProcess">Позволить обработать всю очередь</param>
        /// <param name="completeAdding">Запретить добавление новых элементов</param>
        protected void StopThreadPool(bool waitForStop, bool letFinishProcess, bool completeAdding)
        {
            if (this.IsStopRequestedOrStopped)
            {
                if (State == ThreadPoolState.Stopped && (completeAdding || this.IsAddingCompleted))
                {
                    if (completeAdding)
                        _completeAdding = completeAdding;
                    CleanUpMainQueueAfterStop();
                }
                if (waitForStop)
                {
                    this.WaitUntilStop();
                }
                return;
            }

            Thread[] waitFor = null;
            bool stopRequestedBefore = false;

            lock (_activeThread)
            {
                stopRequestedBefore = this.IsStopRequestedOrStopped;
                if (!stopRequestedBefore)
                {
                    _letFinishProcess = letFinishProcess;

                    if (waitForStop)
                        waitFor = _activeThread.ToArray();

                    ThreadPoolState prevState;
                    bool stateChangedToStopRequested = ChangeStateSafe(ThreadPoolState.StopRequested, out prevState);
                    Contract.Assert(stateChangedToStopRequested || prevState == ThreadPoolState.Stopped);
                    if (_activeThread.Count == 0 && prevState != ThreadPoolState.Stopped)
                    {
                        // Если потоков нет, то переходим в состояние "остановлено"
                        bool stateChanged = ChangeStateSafe(ThreadPoolState.Stopped, out prevState);
                        Contract.Assert(stateChanged || prevState == ThreadPoolState.Stopped, "State was not changed to stopped");
                        if (stateChanged)
                            OnGoToStoppedState();
                    }

                    _threadRunningCancellation.Cancel();
                    if (completeAdding)
                        _completeAdding = completeAdding;

                    OnThreadPoolStop(waitForStop, letFinishProcess, completeAdding);
                }
            }


            if (waitForStop && waitFor != null)
            {
                while (waitFor.Length > 0)
                {
                    for (int i = 0; i < waitFor.Length; i++)
                        waitFor[i].Join();

                    lock (_activeThread)
                    {
                        waitFor = _activeThread.ToArray();
                    }
                }
            }
            if (stopRequestedBefore)
            {
                if (State == ThreadPoolState.Stopped && (completeAdding || this.IsAddingCompleted))
                {
                    if (completeAdding)
                        _completeAdding = completeAdding;
                    CleanUpMainQueueAfterStop();
                }
                if (waitForStop)
                {
                    this.WaitUntilStop();
                }
            }
        }


        /// <summary>
        /// Обработка свежей задачи при поступлении
        /// </summary>
        /// <param name="item">Задача</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void PrepareWorkItem(ThreadPoolWorkItem item)
        {
            Contract.Requires(item != null);
            if (_restoreExecutionContext && !ExecutionContext.IsFlowSuppressed())
                item.CaptureExecutionContext(!_useOwnSyncContext);
        }
        /// <summary>
        /// Запуск задания
        /// </summary>
        /// <param name="item">Задача</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void RunWorkItem(ThreadPoolWorkItem item)
        {
            Contract.Requires(item != null);
            Contract.Assert(!_useOwnSyncContext || (SynchronizationContext.Current == _synchroContext));

            Profiling.ProfilingTimer timer = new Profiling.ProfilingTimer();
            timer.StartTime();

            item.Run(_restoreExecutionContext, _useOwnSyncContext);

            Profiling.Profiler.ThreadPoolWorkProcessed(this.Name, timer.StopTime());
        }
        /// <summary>
        /// Отмена задания
        /// </summary>
        /// <param name="item">Задача</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void CancelWorkItem(ThreadPoolWorkItem item)
        {
            Contract.Requires(item != null);
            item.Cancel();
            Profiling.Profiler.ThreadPoolWorkCancelled(this.Name);
        }


        /// <summary>
        /// Расширить вместимость общей очереди на указанное значение
        /// </summary>
        /// <param name="extensionVal">Величина расширения</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void ExtendGlobalQueueCapacity(int extensionVal)
        {
            Contract.Requires(extensionVal >= 0);
            Contract.Assert(State != ThreadPoolState.Stopped);

            _threadPoolGlobals.ExtendGlobalQueueCapacity(extensionVal);
        }

        /// <summary>
        /// Добавить работу в очередь пула потоков
        /// </summary>
        /// <param name="item">Работа</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void AddWorkItemToQueue(ThreadPoolWorkItem item)
        {
            Contract.Requires(item != null);
            Contract.Assert(!_restoreExecutionContext || !item.AllowExecutionContextFlow || item.CapturedContext != null);
            Contract.Assert(State != ThreadPoolState.Stopped);

            item.StartStoreTimer();
            _threadPoolGlobals.AddItem(item, item.PreferFairness);
            Profiling.Profiler.ThreadPoolWorkItemsCountIncreased(this.Name, this.GlobalQueueWorkItemCount, this.QueueCapacity);
        }
        /// <summary>
        /// Попробовать добавить работу в очередь пула потоков
        /// </summary>
        /// <param name="item">Работа</param>
        /// <returns>Удалось ли добавить</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool TryAddWorkItemToQueue(ThreadPoolWorkItem item)
        {
            Contract.Requires(item != null);
            Contract.Assert(!_restoreExecutionContext || !item.AllowExecutionContextFlow || item.CapturedContext != null);
            Contract.Assert(State != ThreadPoolState.Stopped);

            item.StartStoreTimer();
            var result = _threadPoolGlobals.TryAddItem(item, item.PreferFairness);
            if (result)
                Profiling.Profiler.ThreadPoolWorkItemsCountIncreased(this.Name, this.GlobalQueueWorkItemCount, this.QueueCapacity);
            else
                Profiling.Profiler.ThreadPoolWorkItemRejectedInTryAdd(this.Name);
            return result;
        }
        /// <summary>
        /// Взять очередной элемент из очереди
        /// </summary>
        /// <param name="privateData">Данные потока</param>
        /// <param name="token">Токен отмены</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected ThreadPoolWorkItem TakeWorkItemFromQueue(ThreadPrivateData privateData, CancellationToken token)
        {
            Contract.Requires(privateData != null);
            Contract.Ensures(Contract.Result<ThreadPoolWorkItem>() != null);
            Contract.Assert(State != ThreadPoolState.Stopped);

            var result = _threadPoolGlobals.TakeItem(privateData.LocalData, token);
            Profiling.Profiler.ThreadPoolWaitingInQueueTime(this.Name, result.StopStoreTimer());
            Profiling.Profiler.ThreadPoolWorkItemsCountDecreased(this.Name, this.GlobalQueueWorkItemCount, this.QueueCapacity);
            return result;
        }
        /// <summary>
        /// Попробовать взять очередной элемент из очереди
        /// </summary>
        /// <param name="privateData">Данные потока</param>
        /// <param name="item">Полученный элемент</param>
        /// <returns>Успешность</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool TryTakeWorkItemFromQueue(ThreadPrivateData privateData, out ThreadPoolWorkItem item)
        {
            Contract.Requires(privateData != null);
            Contract.Ensures(Contract.Result<bool>() == false || Contract.ValueAtReturn(out item) != null);
            Contract.Assert(State != ThreadPoolState.Stopped);

            var result = _threadPoolGlobals.TryTakeItem(privateData.LocalData, out item);
            if (result)
            {
                Profiling.Profiler.ThreadPoolWaitingInQueueTime(this.Name, item.StopStoreTimer());
                Profiling.Profiler.ThreadPoolWorkItemsCountDecreased(this.Name, this.GlobalQueueWorkItemCount, this.QueueCapacity);
            }
            return result;
        }
        /// <summary>
        /// Попробовать взять очередной элемент из очереди
        /// </summary>
        /// <param name="privateData">Данные потока</param>
        /// <param name="item">Полученный элемент</param>
        /// <param name="timeout">Таймаут</param>
        /// <param name="token">Токен отмены</param>
        /// <returns>Успешность</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool TryTakeWorkItemFromQueue(ThreadPrivateData privateData, out ThreadPoolWorkItem item, int timeout, CancellationToken token)
        {
            Contract.Requires(privateData != null);
            Contract.Ensures(Contract.Result<bool>() == false || Contract.ValueAtReturn(out item) != null);
            Contract.Assert(State != ThreadPoolState.Stopped);

            var result = _threadPoolGlobals.TryTakeItem(privateData.LocalData, true, true, out item, timeout, token);
            if (result)
            {
                Profiling.Profiler.ThreadPoolWaitingInQueueTime(this.Name, item.StopStoreTimer());
                Profiling.Profiler.ThreadPoolWorkItemsCountDecreased(this.Name, this.GlobalQueueWorkItemCount, this.QueueCapacity);
            }
            return result;
        }
        /// <summary>
        /// Попробовать взять очередной элемент из очереди (без похищения у других потоков)
        /// </summary>
        /// <param name="privateData">Данные потока</param>
        /// <param name="item">Полученный элемент</param>
        /// <param name="timeout">Таймаут</param>
        /// <param name="token">Токен отмены</param>
        /// <returns>Успешность</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool TryTakeWorkItemFromQueueWithoutSteal(ThreadPrivateData privateData, out ThreadPoolWorkItem item, int timeout, CancellationToken token)
        {
            Contract.Requires(privateData != null);
            Contract.Ensures(Contract.Result<bool>() == false || Contract.ValueAtReturn(out item) != null);
            Contract.Assert(State != ThreadPoolState.Stopped);

            var result = _threadPoolGlobals.TryTakeItem(privateData.LocalData, true, false, out item, timeout, token);
            if (result)
            {
                Profiling.Profiler.ThreadPoolWaitingInQueueTime(this.Name, item.StopStoreTimer());
                Profiling.Profiler.ThreadPoolWorkItemsCountDecreased(this.Name, this.GlobalQueueWorkItemCount, this.QueueCapacity);
            }
            return result;
        }
        /// <summary>
        /// Попробовать взять очередной элемент из очереди без поиска в локально очереди
        /// </summary>
        /// <param name="privateData">Данные потока</param>
        /// <param name="item">Полученный элемент</param>
        /// <param name="timeout">Таймаут</param>
        /// <param name="token">Токен отмены</param>
        /// <returns>Успешность</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool TryTakeWorkItemFromQueueWithoutLocalSearch(ThreadPrivateData privateData, out ThreadPoolWorkItem item, int timeout, CancellationToken token)
        {
            Contract.Requires(privateData != null);
            Contract.Ensures(Contract.Result<bool>() == false || Contract.ValueAtReturn(out item) != null);
            Contract.Assert(State != ThreadPoolState.Stopped);

            var result = _threadPoolGlobals.TryTakeItem(privateData.LocalData, false, true, out item, timeout, token);
            if (result)
            {
                Profiling.Profiler.ThreadPoolWaitingInQueueTime(this.Name, item.StopStoreTimer());
                Profiling.Profiler.ThreadPoolWorkItemsCountDecreased(this.Name, this.GlobalQueueWorkItemCount, this.QueueCapacity);
            }
            return result;
        }



        /// <summary>
        /// Асинхронное выполнение задания
        /// </summary>
        /// <param name="act">Задание</param>
        /// <param name="state">Состояние</param>
        void ICustomSynchronizationContextSupplier.RunAsync(System.Threading.SendOrPostCallback act, object state)
        {
            var item = new SendOrPostCallbackThreadPoolWorkItem(act, state, true, false);
            this.AddWorkItem(item);
        }
        /// <summary>
        /// Синхронное выполнение задание
        /// </summary>
        /// <param name="act">Задание</param>
        /// <param name="state">Состояние</param>
        void ICustomSynchronizationContextSupplier.RunSync(System.Threading.SendOrPostCallback act, object state)
        {
            var item = new SendOrPostCallbackSyncThreadPoolWorkItem(act, state, true, true);
            this.AddWorkItem(item);
            item.Wait();
        }

        /// <summary>
        /// Запустить в другом контексте
        /// </summary>
        /// <param name="act">Действие</param>
        /// <param name="flowContext">Протаскивать ли ExecutionContext</param>
        void IContextSwitchSupplier.Run(Action act, bool flowContext)
        {
            var item = new ActionThreadPoolWorkItem(act, flowContext, false);
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
            var item = new ActionWithStateThreadPoolWorkItem<object>(act, state, flowContext, false);
            this.AddWorkItem(item);
        }


        /// <summary>
        /// Остановка и освобождение ресурсов
        /// </summary>
        /// <param name="waitForStop">Ожидать остановки</param>
        /// <param name="letFinishProcess">Позволить обработать всю очередь</param>
        /// <param name="completeAdding">Запретить добавление новых элементов</param>
        public virtual void Dispose(bool waitForStop, bool letFinishProcess, bool completeAdding)
        {
            StopThreadPool(waitForStop, letFinishProcess, completeAdding);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Освобождение ресурсов
        /// </summary>
        /// <param name="isUserCall">Вызвано ли пользователем</param>
        protected override void Dispose(bool isUserCall)
        {
            if (isUserCall && State == ThreadPoolState.Stopped)
                CleanUpMainQueueAfterStop();

            if (State != ThreadPoolState.StopRequested && State != ThreadPoolState.Stopped)
            {
                Contract.Assume(isUserCall, "ThreadPool destructor: Better to dispose by user. Закомментируй, если не нравится.");

                if (isUserCall)
                    StopThreadPool(true, false, true);

                ThreadPoolState prevState;
                ChangeStateSafe(ThreadPoolState.StopRequested, out prevState);
                ChangeStateSafe(ThreadPoolState.Stopped, out prevState);

                if (_threadRunningCancellation != null)
                    _threadRunningCancellation.Cancel();

                if (!isUserCall)
                    Profiling.Profiler.ThreadPoolDisposed(Name, true);
            }
            base.Dispose(isUserCall);
        }
    }
}
