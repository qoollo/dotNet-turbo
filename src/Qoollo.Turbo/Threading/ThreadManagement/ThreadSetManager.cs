using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Threading.ThreadManagement
{
    /// <summary>
    /// Менеджер группы потоков
    /// </summary>
    public abstract class ThreadSetManager : IDisposable
    {
        /// <summary>
        /// Контракты
        /// </summary>
        [ContractInvariantMethod]
        private void Invariant()
        {
            Contract.Invariant(_procThreads != null);
            Contract.Invariant(_name != null);
            Contract.Invariant(_threadExitedEvent != null);
            Contract.Invariant(_activeThreadCount >= 0);
            Contract.Invariant(Enum.IsDefined(typeof(ThreadSetManagerState), (ThreadSetManagerState)_state));
        }


        private readonly string _name;
        private readonly int _maxStackSize;

        private bool _isBackground;
        private ThreadPriority _priority;

        private readonly Thread[] _procThreads;
        private int _activeThreadCount;
        private int _exitedThreadCount;

        private readonly ManualResetEventSlim _threadExitedEvent;
        private readonly CancellationTokenSource _stopRequestedCancelation;

        private int _state;


        /// <summary>
        /// Конструктор ThreadManager
        /// </summary>
        /// <param name="threadCount">Число потоков</param>
        /// <param name="name">Имя для потоков</param>
        /// <param name="isBackground">Являются ли потоки фоновыми</param>
        /// <param name="priority">Приоритет</param>
        /// <param name="maxStackSize">Максимальный размер стека потоков</param>
        public ThreadSetManager(int threadCount, string name, bool isBackground, ThreadPriority priority, int maxStackSize)
        {
            Contract.Requires(threadCount > 0);
            Contract.Requires(maxStackSize >= 0);

            _isBackground = isBackground;
            _name = name ?? this.GetType().GetCSName();
            _maxStackSize = maxStackSize;
            _priority = priority;

            _procThreads = new Thread[threadCount];
            _activeThreadCount = 0;
            _exitedThreadCount = 0;

            for (int i = 0; i < _procThreads.Length; i++)
            {
                _procThreads[i] = new Thread(ThreadProcFunc, _maxStackSize);
                _procThreads[i].Name = string.Format("{0} (#{1})", _name, i);
                _procThreads[i].Priority = _priority;
                _procThreads[i].IsBackground = _isBackground;
            }

            _threadExitedEvent = new ManualResetEventSlim(false);
            _stopRequestedCancelation = new CancellationTokenSource();

            _state = (int)ThreadSetManagerState.Created;

            Profiling.Profiler.ThreadSetManagerCreated(this.Name, threadCount);
        }
        /// <summary>
        /// Конструктор ThreadManager
        /// </summary>
        /// <param name="threadCount">Число потоков</param>
        /// <param name="name">Имя для потоков</param>
        public ThreadSetManager(int threadCount, string name)
            : this(threadCount, name, false, ThreadPriority.Normal, 0)
        {
        }
        /// <summary>
        /// Конструктор ThreadManager
        /// </summary>
        /// <param name="threadCount">Число потоков</param>
        public ThreadSetManager(int threadCount)
            : this(threadCount, null, false, ThreadPriority.Normal, 0)
        {
        }

        /// <summary>
        /// Текущее состояние
        /// </summary>
        public ThreadSetManagerState State
        {
            get { return (ThreadSetManagerState)Volatile.Read(ref _state); }
        }
        /// <summary>
        /// Запущен ли сейчас обработчик
        /// </summary>
        public bool IsWork
        {
            get { return State == ThreadSetManagerState.Running; }
        }
        /// <summary>
        /// Запрошена ли остановка
        /// </summary>
        protected bool IsStopRequested
        {
            get { return State == ThreadSetManagerState.StopRequested; }
        }
        /// <summary>
        /// Остановлен ли
        /// </summary>
        protected bool IsStopped
        {
            get { return State == ThreadSetManagerState.Stopped; }
        }
        /// <summary>
        /// Запрошена ли остановка или остановлен
        /// </summary>
        protected bool IsStopRequestedOrStopped
        {
            get
            {
                var state = State;
                return state == ThreadSetManagerState.Stopped || state == ThreadSetManagerState.StopRequested;
            }
        }

        /// <summary>
        /// Имя обработчика
        /// </summary>
        public string Name
        {
            get { return _name; }
        }

        /// <summary>
        /// Работают ли потоки в фоновом режиме
        /// </summary>
        public bool IsBackground
        {
            get { return _isBackground; }
            set
            {
                CheckPendingDisposeOrDisposed();

                foreach (var th in _procThreads)
                    th.IsBackground = value;
                _isBackground = value;
            }
        }

        /// <summary>
        /// Приоритет потоков
        /// </summary>
        public ThreadPriority Priority
        {
            get { return _priority; }
            set
            {
                CheckPendingDisposeOrDisposed();

                foreach (var th in _procThreads)
                    th.Priority = value;
                _priority = value;
            }
        }

        /// <summary>
        /// Текущие настройки Culture
        /// </summary>
        public System.Globalization.CultureInfo CurrentCulture
        {
            get { return _procThreads[0].CurrentCulture; }
            set
            {
                CheckPendingDisposeOrDisposed();

                foreach (var th in _procThreads)
                    th.CurrentCulture = value;
            }
        }

        /// <summary>
        /// Текущие настройки Culture
        /// </summary>
        public System.Globalization.CultureInfo CurrentUICulture
        {
            get { return _procThreads[0].CurrentUICulture; }
            set
            {
                CheckPendingDisposeOrDisposed();

                foreach (var th in _procThreads)
                    th.CurrentUICulture = value;
            }
        }

        /// <summary>
        /// Число работающих потоков
        /// </summary>
        public int ActiveThreadCount
        {
            get { return Volatile.Read(ref _activeThreadCount); }
        }
        /// <summary>
        /// Число потоков обработки
        /// </summary>
        public int ThreadCount
        {
            get { return _procThreads.Length; }
        }


        /// <summary>
        /// Допустима ли смена состояния
        /// </summary>
        /// <param name="oldState">Старое состояние</param>
        /// <param name="newState">Новое состояние</param>
        /// <returns>Допустим ли переход</returns>
        private bool IsValidStateTransition(ThreadSetManagerState oldState, ThreadSetManagerState newState)
        {
            switch (oldState)
            {
                case ThreadSetManagerState.Created:
                    return newState == ThreadSetManagerState.StartRequested || newState == ThreadSetManagerState.StopRequested;
                case ThreadSetManagerState.StartRequested:
                    return newState == ThreadSetManagerState.Running || newState == ThreadSetManagerState.Stopped;
                case ThreadSetManagerState.Running:
                    return newState == ThreadSetManagerState.StopRequested || newState == ThreadSetManagerState.AllThreadsExited;
                case ThreadSetManagerState.StopRequested:
                    return newState == ThreadSetManagerState.Stopped;
                case ThreadSetManagerState.AllThreadsExited:
                    return newState == ThreadSetManagerState.StopRequested;
                case ThreadSetManagerState.Stopped:
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
        private bool ChangeStateSafe(ThreadSetManagerState newState, out ThreadSetManagerState prevState)
        {
            prevState = (ThreadSetManagerState)Volatile.Read(ref _state);

            if (!IsValidStateTransition(prevState, newState))
                return false;

            SpinWait sw = new SpinWait();
            while (Interlocked.CompareExchange(ref _state, (int)newState, (int)prevState) != (int)prevState)
            {
                sw.SpinOnce();
                prevState = (ThreadSetManagerState)Volatile.Read(ref _state);
                if (!IsValidStateTransition(prevState, newState))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Проверить, освобождён ли объект и если да, то вызвать исключение ObjectDisposedException
        /// </summary>
        protected void CheckDisposed()
        {
            if (State == ThreadSetManagerState.Stopped)
                throw new ObjectDisposedException(this.GetType().Name, "ThreadSetManager is Stopped");
        }
        /// <summary>
        /// Проверить, освобождён ли объект и если да, то вызвать исключение ObjectDisposedException
        /// </summary>
        protected void CheckPendingDisposeOrDisposed()
        {
            var state = State;
            if (state == ThreadSetManagerState.Stopped || state == ThreadSetManagerState.StopRequested)
                throw new ObjectDisposedException(this.GetType().Name, "ThreadSetManager has Stopped or StopRequested state");
        }

        /// <summary>
        /// Получить уникальный ID потока в рамках менеджера (-1, если поток не принадлежит менеджеру)
        /// </summary>
        /// <returns>Идентификатор</returns>
        protected int GetThreadId()
        {
            return Array.IndexOf(_procThreads, Thread.CurrentThread);
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

            for (int i = 0; i < _procThreads.Length; i++)
            {
                if (_procThreads[i] == null)
                    continue;
                
                totalThreadCount++;

                var curState = _procThreads[i].ThreadState;
                if ((curState & ~ThreadState.Background) == ThreadState.Running)
                    runningCount++;
                if ((curState & ThreadState.WaitSleepJoin) != 0)
                    waitingCount++;
            }
        }


        /// <summary>
        /// Запуск обработчиков
        /// </summary>
        public void Start()
        {
            ThreadSetManagerState prevState;
            if (!ChangeStateSafe(ThreadSetManagerState.StartRequested, out prevState))
            {
                if (prevState == ThreadSetManagerState.Stopped)
                    throw new ObjectDisposedException(this.GetType().Name);
                throw new WrongStateException("Can't start ThreadSetManager cause it is in wrong state: " + prevState.ToString());
            }

            try
            {
                _threadExitedEvent.Reset();

                for (int i = 0; i < _procThreads.Length; i++)
                    _procThreads[i].Start();

                bool changeStateToRunningSuccess = ChangeStateSafe(ThreadSetManagerState.Running, out prevState);
                Contract.Assert(changeStateToRunningSuccess && prevState == ThreadSetManagerState.StartRequested);
            }
            catch
            {
                ChangeStateSafe(ThreadSetManagerState.Stopped, out prevState);

                _threadExitedEvent.Set();
                _stopRequestedCancelation.Cancel();

                throw;
            }
        }

        /// <summary>
        /// Получить токен отмены, срабатывающий при запросе остановки
        /// </summary>
        /// <returns>Токен отмены</returns>
        protected CancellationToken GetCancellationToken()
        {
            var tokenSrc = this._stopRequestedCancelation;
            if (tokenSrc != null)
                return tokenSrc.Token;
            return new CancellationToken(true);
        }



        /// <summary>
        /// Основная функция, выполняемая потоками
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCode]
        private void ThreadProcFunc()
        {
            CancellationToken token = GetCancellationToken();

            object state = null;

            try
            {
                Interlocked.Increment(ref _activeThreadCount);
                Profiling.Profiler.ThreadSetManagerThreadStart(this.Name, this.ActiveThreadCount, this.ThreadCount);

                state = this.Prepare();

                token.ThrowIfCancellationRequested();
                this.Process(state, token);
            }
            catch (OperationCanceledException opEx)
            {
                if (!token.IsCancellationRequested)
                {
                    ProcessThreadException(opEx);
                    throw;
                }
            }
            catch (Exception ex)
            {
                if (ex.GetType() == typeof(ThreadAbortException) || ex.GetType() == typeof(ThreadInterruptedException) || ex.GetType() == typeof(StackOverflowException) || ex.GetType() == typeof(OutOfMemoryException))
                    throw;

                ProcessThreadException(ex);
                throw;
            }
            finally
            {
                this.Finalize(state);

                int activeThreadCount = Interlocked.Decrement(ref _activeThreadCount);
                int exitedThreadCount = Interlocked.Increment(ref _exitedThreadCount);
                Contract.Assert(activeThreadCount >= 0);
                Contract.Assert(exitedThreadCount <= this.ThreadCount);

                if (exitedThreadCount >= this.ThreadCount || (activeThreadCount == 0 && IsStopRequested))
                {
                    // Вынуждены ждать
                    SpinWait sw = new SpinWait();
                    while (State == ThreadSetManagerState.StartRequested)
                        sw.SpinOnce();

                    ThreadSetManagerState prevState;
                    if (ChangeStateSafe(ThreadSetManagerState.AllThreadsExited, out prevState))
                    {
                        Contract.Assert(prevState == ThreadSetManagerState.Running);
                        _threadExitedEvent.Set();
                    }
                    else if (ChangeStateSafe(ThreadSetManagerState.Stopped, out prevState))
                    {
                        Contract.Assert(prevState == ThreadSetManagerState.StopRequested);
                        _threadExitedEvent.Set();
                        Profiling.Profiler.ThreadSetManagerDisposed(this.Name, false);
                    }
                }

                Profiling.Profiler.ThreadSetManagerThreadStop(this.Name, this.ActiveThreadCount, this.ThreadCount);
            }
        }


        /// <summary>
        /// Обработка исключений
        /// </summary>
        /// <param name="ex">Исключение</param>
        [System.Diagnostics.DebuggerNonUserCode]
        protected virtual void ProcessThreadException(Exception ex)
        {
            Contract.Requires(ex != null);

            throw new ThreadSetManagerException("Unhandled exception during processing in ThreadSetManager ('" + this.Name + "')", ex);
        }

        /// <summary>
        /// Создание объекта состояния на поток.
        /// Вызывается при старте для каждого потока
        /// </summary>
        /// <returns>Объект состояния</returns>
        protected virtual object Prepare()
        {
            return null;
        }
        /// <summary>
        /// Основной метод обработки
        /// </summary>
        /// <param name="state">Объект состояния, инициализированный в методе Prepare()</param>
        /// <param name="token">Токен для отмены обработки при вызове Stop</param>
        protected abstract void Process(object state, CancellationToken token);

        /// <summary>
        /// Освобождение объекта состояния потока
        /// </summary>
        /// <param name="state">Объект состояния</param>
        protected virtual void Finalize(object state)
        {
        }


        /// <summary>
        /// Ожидание полной остановки
        /// </summary>
        public void Join()
        {
            if (State == ThreadSetManagerState.Stopped || State == ThreadSetManagerState.AllThreadsExited)
                return;

            _threadExitedEvent.Wait();
        }

        /// <summary>
        /// Ожидание полной остановки с таймаутом
        /// </summary>
        /// <param name="timeout">Таймаут ожидания в миллисекундах</param>
        /// <returns>true - дождались, false - вышли по таймауту</returns>
        public bool Join(int timeout)
        {
            if (State == ThreadSetManagerState.Stopped || State == ThreadSetManagerState.AllThreadsExited)
                return true;

            return _threadExitedEvent.Wait(timeout);
        }


        /// <summary>
        /// Остановка работы
        /// </summary>
        /// <param name="waitForStop">Ждать ли завершения всех потоков</param>
        private void StopThreadManager(bool waitForStop)
        {
            if (this.IsStopRequestedOrStopped)
            {
                if (waitForStop)
                    this.Join();
            }

            ThreadSetManagerState prevState;
            if (!ChangeStateSafe(ThreadSetManagerState.StopRequested, out prevState))
            {
                if (prevState != ThreadSetManagerState.StartRequested)
                {
                    if (waitForStop)
                        this.Join();

                    return;
                }

                SpinWait sw = new SpinWait();
                while (State == ThreadSetManagerState.StartRequested)
                    sw.SpinOnce();

                if (!ChangeStateSafe(ThreadSetManagerState.StopRequested, out prevState))
                {
                    if (waitForStop)
                        this.Join();

                    return;
                }
            }

            _stopRequestedCancelation.Cancel();

            if (waitForStop && prevState != ThreadSetManagerState.Created)
            {
                for (int i = 0; i < _procThreads.Length; i++)
                {
                    if (_procThreads[i] != null)
                        _procThreads[i].Join();
                }
            }

            if (ActiveThreadCount == 0)
            {
                if (ChangeStateSafe(ThreadSetManagerState.Stopped, out prevState) && prevState == ThreadSetManagerState.StopRequested)
                {
                    _threadExitedEvent.Set();
                    Profiling.Profiler.ThreadSetManagerDisposed(this.Name, false);
                }
            }

            Contract.Assert(State == ThreadSetManagerState.StopRequested || State == ThreadSetManagerState.Stopped);
            Contract.Assume(!waitForStop || State == ThreadSetManagerState.Stopped);
        }


        /// <summary>
        /// Остановка и освобождение ресурсов
        /// </summary>
        /// <param name="waitForStop">Ожидать остановки</param>
        public virtual void Stop(bool waitForStop)
        {
            StopThreadManager(waitForStop);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Остановка и освобождение ресурсов
        /// </summary>
        public void Stop()
        {
            this.Stop(true);
        }

        /// <summary>
        /// Основной код освобождения ресурсов
        /// </summary>
        /// <param name="isUserCall">Вызвано ли освобождение пользователем. False - деструктор</param>
        protected virtual void Dispose(bool isUserCall)
        {
            if (!this.IsStopRequestedOrStopped)
            {
                Contract.Assume(isUserCall, "ThreadSetManager finalizer called. You should dispose ThreadSetManager explicitly. ThreadSetManagerName: " + this.Name);

                if (isUserCall)
                    StopThreadManager(true);

                ThreadSetManagerState prevState;
                ChangeStateSafe(ThreadSetManagerState.StopRequested, out prevState);
                ChangeStateSafe(ThreadSetManagerState.Stopped, out prevState);

                if (_stopRequestedCancelation != null)
                    _stopRequestedCancelation.Cancel();

                if (!isUserCall)
                    Profiling.Profiler.ThreadSetManagerDisposed(this.Name, true);
            }
        }

        /// <summary>
        /// Освобождение ресурсов
        /// </summary>
        void IDisposable.Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Финализатор
        /// </summary>
        ~ThreadSetManager()
        {
            Dispose(false);
        }
    }
}
