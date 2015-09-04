using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;
using System.Diagnostics.Contracts;

namespace Qoollo.Turbo.Threading.QueueProcessing
{
    /// <summary>
    /// Асинхронный обработчик данных в несколько потоков с очередью
    /// </summary>
    /// <typeparam name="T">Тип обрабатываемого элемента</typeparam>
    public abstract class QueueAsyncProcessor<T> : QueueAsyncProcessorBase<T>, IQueueAsyncProcessorStartStopHelper
    {
        /// <summary>
        /// Контракты
        /// </summary>
        [ContractInvariantMethod]
        private void Invariant()
        {
            Contract.Invariant(_procThreads != null);
            Contract.Invariant(_queue != null);
            Contract.Invariant(_name != null);
            Contract.Invariant(_activeThreadCount >= 0);
            Contract.Invariant(Enum.IsDefined(typeof(QueueAsyncProcessorState), (QueueAsyncProcessorState)_state));
        }


        private readonly string _name;
        private readonly bool _isBackground;

        private readonly Thread[] _procThreads;
        private int _activeThreadCount;

        private readonly Collections.Concurrent.BlockingQueue<T> _queue;
        private readonly int _maxQueueSize;

        private readonly ManualResetEventSlim _stoppedEvent;
        private CancellationTokenSource _stopRequestedCancelation;
        private CancellationTokenSource _stoppedCancelation;

        private int _state;
        private volatile bool _completeAdding;
        private volatile bool _letFinishProcess;

        /// <summary>
        /// Конструктор QueueAsyncProcessor
        /// </summary>
        /// <param name="threadCount">Число потоков обработки</param>
        /// <param name="maxQueueSize">Максимальный размер очереди</param>
        /// <param name="name">Имя, присваемое потокам</param>
        /// <param name="isBackground">Будут ли потоки работать в фоновом режиме</param>
        public QueueAsyncProcessor(int threadCount, int maxQueueSize, string name, bool isBackground)
        {
            Contract.Requires(threadCount > 0);

            _isBackground = isBackground;
            _name = name ?? this.GetType().GetCSName();

            _procThreads = new Thread[threadCount];
            _activeThreadCount = 0;

            _maxQueueSize = maxQueueSize > 0 ? maxQueueSize : -1;
            _queue = new Collections.Concurrent.BlockingQueue<T>(_maxQueueSize);

            _stoppedEvent = new ManualResetEventSlim(false);
            _stopRequestedCancelation = null;
            _stoppedCancelation = null;

            _state = (int)QueueAsyncProcessorState.Created;
            _completeAdding = false;
            _letFinishProcess = false;

            Profiling.Profiler.QueueAsyncProcessorCreated(this.Name);
        }
        /// <summary>
        /// Конструктор QueueAsyncProcessor
        /// </summary>
        /// <param name="threadCount">Число потоков обработки</param>
        /// <param name="maxQueueSize">Максимальный размер очереди</param>
        /// <param name="name">Имя, присваемое потокам</param>
        public QueueAsyncProcessor(int threadCount, int maxQueueSize, string name)
            : this(threadCount, maxQueueSize, name, false)
        {
        }
        /// <summary>
        /// Конструктор QueueAsyncProcessor. Размер очереди не ограничивается.
        /// </summary>
        /// <param name="threadCount">Число потоков обработки</param>
        /// <param name="name">Имя, присваемое потокам</param>
        public QueueAsyncProcessor(int threadCount, string name)
            : this(threadCount, -1, name, false)
        {
        }
        /// <summary>
        /// Конструктор QueueAsyncProcessor. Размер очереди не ограничивается.
        /// </summary>
        /// <param name="threadCount">Число потоков обработки</param>
        public QueueAsyncProcessor(int threadCount)
            : this(threadCount, -1, null, false)
        {
        }

        /// <summary>
        /// Текущее состояние
        /// </summary>
        public QueueAsyncProcessorState State
        {
            get { return (QueueAsyncProcessorState)Volatile.Read(ref _state); }
        }
        /// <summary>
        /// Запущен ли сейчас обработчик
        /// </summary>
        public bool IsWork
        {
            get { return State == QueueAsyncProcessorState.Running; }
        }
        /// <summary>
        /// Запрошена ли остановка
        /// </summary>
        protected bool IsStopRequested
        {
            get { return State == QueueAsyncProcessorState.StopRequested; }
        }
        /// <summary>
        /// Остановлен ли
        /// </summary>
        protected bool IsStopped
        {
            get { return State == QueueAsyncProcessorState.Stopped; }
        }
        /// <summary>
        /// Запрошена ли остановка или остановлен
        /// </summary>
        protected bool IsStopRequestedOrStopped
        {
            get
            {
                var state = State;
                return state == QueueAsyncProcessorState.Stopped || state == QueueAsyncProcessorState.StopRequested;
            }
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
        }
        /// <summary>
        /// Число работающих потоков
        /// </summary>
        protected internal int ActiveThreadCount
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
        /// Число элементов в очереди
        /// </summary>
        public int ElementCount
        {
            get { return _queue.Count; }
        }

        /// <summary>
        /// Ограничения на размер очереди
        /// </summary>
        public int QueueCapacity
        {
            get { return _queue.BoundedCapacity; }
        }




        /// <summary>
        /// Допустима ли смена состояния
        /// </summary>
        /// <param name="oldState">Старое состояние</param>
        /// <param name="newState">Новое состояние</param>
        /// <returns>Допустим ли переход</returns>
        private bool IsValidStateTransition(QueueAsyncProcessorState oldState, QueueAsyncProcessorState newState)
        {
            switch (oldState)
            {
                case QueueAsyncProcessorState.Created:
                    return newState == QueueAsyncProcessorState.StartRequested || newState == QueueAsyncProcessorState.StopRequested;
                case QueueAsyncProcessorState.StartRequested:
                    return newState == QueueAsyncProcessorState.Running || newState == QueueAsyncProcessorState.Stopped;
                case QueueAsyncProcessorState.Running:
                    return newState == QueueAsyncProcessorState.StopRequested;
                case QueueAsyncProcessorState.StopRequested:
                    return newState == QueueAsyncProcessorState.Stopped;
                case QueueAsyncProcessorState.Stopped:
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
        private bool ChangeStateSafe(QueueAsyncProcessorState newState, out QueueAsyncProcessorState prevState)
        {
            prevState = (QueueAsyncProcessorState)Volatile.Read(ref _state);

            if (!IsValidStateTransition(prevState, newState))
                return false;

            SpinWait sw = new SpinWait();
            while (Interlocked.CompareExchange(ref _state, (int)newState, (int)prevState) != (int)prevState)
            {
                sw.SpinOnce();
                prevState = (QueueAsyncProcessorState)Volatile.Read(ref _state);
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
            if (State == QueueAsyncProcessorState.Stopped)
                throw new ObjectDisposedException(this.GetType().Name, "QueueAsyncProcessor is Stopped");
        }
        /// <summary>
        /// Проверить, освобождён ли объект и если да, то вызвать исключение ObjectDisposedException
        /// </summary>
        protected void CheckPendingDisposeOrDisposed()
        {
            var state = State;
            if (state == QueueAsyncProcessorState.Stopped || state == QueueAsyncProcessorState.StopRequested)
                throw new ObjectDisposedException(this.GetType().Name, "QueueAsyncProcessor has Stopped or StopRequested state");
        }

        /// <summary>
        /// Форсированно добавить элемент в очередь (игнорирует ограничения вместимости и текущее состояние)
        /// </summary>
        /// <param name="element">Элемент</param>
        protected void AddForced(T element)
        {
            _queue.EnqueueForced(element);
            Profiling.Profiler.QueueAsyncProcessorElementCountIncreased(this.Name, ElementCount, _maxQueueSize);
        }

        /// <summary>
        /// Добавить элемент на обработку
        /// </summary>
        /// <param name="element">Элемент</param>
        /// <param name="timeout">Таймаут добавления в миллисекундах</param>
        /// <param name="token">Токен отмены</param>
        /// <returns>Успешность (удалось ли добавить до истечения таймаута)</returns>
        public override bool Add(T element, int timeout, CancellationToken token)
        {
            CheckDisposed();
            if (IsAddingCompleted)
                throw new InvalidOperationException("Adding was completed for QueueAsyncProcessor: " + this.Name);

            var result = _queue.TryEnqueue(element, timeout, token);
            if (result)
                Profiling.Profiler.QueueAsyncProcessorElementCountIncreased(this.Name, ElementCount, _maxQueueSize);
            else
                Profiling.Profiler.QueueAsyncProcessorElementRejectedInTryAdd(this.Name, ElementCount);
            return result;
        }



        /// <summary>
        /// Запуск обработчиков
        /// </summary>
        public virtual void Start()
        {
            QueueAsyncProcessorState prevState;
            if (!ChangeStateSafe(QueueAsyncProcessorState.StartRequested, out prevState))
            {
                if (prevState == QueueAsyncProcessorState.Stopped)
                    throw new ObjectDisposedException(this.GetType().Name);
                throw new WrongStateException("Can't start QueueAsyncProcessor cause it is in wrong state: " + prevState.ToString());
            }

            try
            {
                _stoppedEvent.Reset();
                _stopRequestedCancelation = new CancellationTokenSource();
                _stoppedCancelation = new CancellationTokenSource();

                _completeAdding = false;
                _letFinishProcess = false;

                for (int i = 0; i < _procThreads.Length; i++)
                {
                    _procThreads[i] = new Thread(new ThreadStart(ThreadProcFunc));
                    _procThreads[i].IsBackground = _isBackground;
                    _procThreads[i].Name = string.Format("{0} (#{1})", _name, i.ToString());
                }

                for (int i = 0; i < _procThreads.Length; i++)
                    _procThreads[i].Start();

                bool changeStateToRunningSuccess = ChangeStateSafe(QueueAsyncProcessorState.Running, out prevState);
                Contract.Assert(changeStateToRunningSuccess && prevState == QueueAsyncProcessorState.StartRequested);
            }
            catch
            {
                ChangeStateSafe(QueueAsyncProcessorState.Stopped, out prevState);

                _stoppedEvent.Set();
                _stopRequestedCancelation.Cancel();
                _stoppedCancelation.Cancel();
                _completeAdding = true;

                throw;
            }
        }

        /// <summary>
        /// Получить токен отмены, срабатывающий при запросе остановки (в том числе и отложенной)
        /// </summary>
        /// <returns>Токен отмены</returns>
        protected CancellationToken GetStopRequestedCancellationToken()
        {
            var tokenSrc = this._stopRequestedCancelation;
            if (tokenSrc != null)
                return tokenSrc.Token;
            return new CancellationToken(true);
        }
        /// <summary>
        /// Получить токен отмены, срабатывающий при запросе немеделнной остановки
        /// </summary>
        /// <returns>Токен отмены</returns>
        protected CancellationToken GetStoppedCancellationToken()
        {
            var tokenSrc = this._stoppedCancelation;
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
            CancellationToken stopRequestedToken = GetStopRequestedCancellationToken();
            CancellationToken stoppedToken = GetStoppedCancellationToken();

            object state = null;        

            try
            {
                Interlocked.Increment(ref _activeThreadCount);
                Profiling.Profiler.QueueAsyncProcessorThreadStart(this.Name, this.ActiveThreadCount, this.ThreadCount);

                state = this.Prepare();
                Profiling.ProfilingTimer timer = new Profiling.ProfilingTimer();
                timer.StartTime();


                while (!stopRequestedToken.IsCancellationRequested)
                {
                    try
                    {
                        T elem = default(T);
                        while (!stopRequestedToken.IsCancellationRequested)
                        {
                            elem = default(T);
                            if (_queue.TryDequeue(out elem, Timeout.Infinite, stopRequestedToken, false))
                            {
                                Profiling.Profiler.QueueAsyncProcessorElementCountDecreased(this.Name, ElementCount, _maxQueueSize);

                                timer.RestartTime();
                                this.Process(elem, state, stoppedToken);
                                Profiling.Profiler.QueueAsyncProcessorElementProcessed(this.Name, timer.GetTime());
                            }
                            else
                            {
                                Contract.Assert(stopRequestedToken.IsCancellationRequested);
                            }
                        }
                    }
                    catch (OperationCanceledException opEx)
                    {
                        if (!stopRequestedToken.IsCancellationRequested)
                            if (!ProcessThreadException(opEx))
                                throw;
                    }
                    catch (Exception ex)
                    {
                        if (ex.GetType() == typeof(ThreadAbortException) || ex.GetType() == typeof(ThreadInterruptedException) || ex.GetType() == typeof(StackOverflowException) || ex.GetType() == typeof(OutOfMemoryException))
                            throw;

                        if (!ProcessThreadException(ex))
                            throw;
                    }
                }


                if (_letFinishProcess)
                {
                    while (!stoppedToken.IsCancellationRequested)
                    {
                        try
                        {
                            T elem = default(T);
                            while (!stoppedToken.IsCancellationRequested && _queue.TryDequeue(out elem))
                            {
                                Profiling.Profiler.QueueAsyncProcessorElementCountDecreased(this.Name, ElementCount, _maxQueueSize);

                                timer.RestartTime();
                                this.Process(elem, state, stoppedToken);
                                Profiling.Profiler.QueueAsyncProcessorElementProcessed(this.Name, timer.GetTime());
                            }

                            if (_queue.Count == 0)
                                break;
                        }
                        catch (OperationCanceledException opEx)
                        {
                            if (!stoppedToken.IsCancellationRequested)
                                if (!ProcessThreadException(opEx))
                                    throw;
                        }
                        catch (Exception ex)
                        {
                            if (ex.GetType() == typeof(ThreadAbortException) || ex.GetType() == typeof(ThreadInterruptedException) || ex.GetType() == typeof(StackOverflowException) || ex.GetType() == typeof(OutOfMemoryException))
                                throw;

                            if (!ProcessThreadException(ex))
                                throw;
                        }
                    }
                }
            }
            finally
            {
                this.Finalize(state);

                if (Interlocked.Decrement(ref _activeThreadCount) <= 0)
                {
                    // Вынуждены ждать
                    SpinWait sw = new SpinWait();
                    while (State == QueueAsyncProcessorState.StartRequested)
                        sw.SpinOnce();

                    if (State == QueueAsyncProcessorState.StopRequested)
                    {
                        QueueAsyncProcessorState prevState;
                        if (ChangeStateSafe(QueueAsyncProcessorState.Stopped, out prevState))
                        {
                            Contract.Assert(prevState == QueueAsyncProcessorState.StopRequested);
                            _stoppedEvent.Set();
                            Profiling.Profiler.QueueAsyncProcessorDisposed(this.Name, false);
                        }
                    }
                }

                Profiling.Profiler.QueueAsyncProcessorThreadStop(this.Name, this.ActiveThreadCount, this.ThreadCount);
            }
        }


        /// <summary>
        /// Обработка исключений. 
        /// Чтобы исключение было проброшено наверх, нужно выбросить новое исключение внутри метода.
        /// </summary>
        /// <param name="ex">Исключение</param>
        /// <returns>Игнорировать ли исключение (false - поток завершает работу)</returns>
        protected virtual bool ProcessThreadException(Exception ex)
        {
            Contract.Requires(ex != null);

            throw new QueueAsyncProcessorException("Unhandled exception during processing in QueueAsyncProcessor ('" + this.Name + "')", ex);
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
        /// Основной метод обработки элементов.
        /// Токен отменяется только при запросе немедленной остановки. 
        /// Остановка может быть отложенной, тогда нужно проверять текущее состояние.
        /// </summary>
        /// <param name="element">Элемент</param>
        /// <param name="state">Объект состояния, инициализированный в методе Prepare()</param>
        /// <param name="token">Токен для отмены обработки при вызове Stop</param>
        protected abstract void Process(T element, object state, CancellationToken token);

        /// <summary>
        /// Освобождение объекта состояния потока
        /// </summary>
        /// <param name="state">Объект состояния</param>
        protected virtual void Finalize(object state)
        {
        }


        /// <summary>
        /// Запретить добавление элементов
        /// </summary>
        public void CompleteAdding()
        {
            _completeAdding = true;
        }

        /// <summary>
        /// Ожидание полной остановки
        /// </summary>
        public void WaitUntilStop()
        {
            if (State == QueueAsyncProcessorState.Stopped)
                return;

            _stoppedEvent.Wait();
        }

        /// <summary>
        /// Ожидание полной остановки с таймаутом
        /// </summary>
        /// <param name="timeout">Таймаут ожидания в миллисекундах</param>
        /// <returns>true - дождались, false - вышли по таймауту</returns>
        public bool WaitUntilStop(int timeout)
        {
            if (State == QueueAsyncProcessorState.Stopped)
                return true;

            return _stoppedEvent.Wait(timeout);
        }


        /// <summary>
        /// Остановка работы асинхронного обработчика
        /// </summary>
        /// <param name="waitForStop">Ждать ли завершения всех потоков</param>
        /// <param name="letFinishProcess">Позволить закончить обработку того, что есть в очереди</param>
        /// <param name="completeAdding">Заблокировать добавление новых элементов</param>
        /// <returns>Запущен ли процесс остановки</returns>
        private bool StopProcessor(bool waitForStop, bool letFinishProcess, bool completeAdding)
        {
            if (this.IsStopRequestedOrStopped)
            {
                if (completeAdding)
                {
                    _completeAdding = completeAdding;
                }
                if (!letFinishProcess)
                {
                    _letFinishProcess = letFinishProcess;
                    _stoppedCancelation.Cancel();
                }
                if (waitForStop)
                {
                    this.WaitUntilStop();
                }

                return false;
            }

            QueueAsyncProcessorState prevState;
            if (!ChangeStateSafe(QueueAsyncProcessorState.StopRequested, out prevState))
            {
                if (prevState != QueueAsyncProcessorState.StartRequested)
                {
                    if (waitForStop)
                        this.WaitUntilStop();

                    return false;
                }

                SpinWait sw = new SpinWait();
                while (State == QueueAsyncProcessorState.StartRequested)
                    sw.SpinOnce();

                if (!ChangeStateSafe(QueueAsyncProcessorState.StopRequested, out prevState))
                {
                    if (waitForStop)
                        this.WaitUntilStop();

                    return false;
                }
            }

            _completeAdding = completeAdding;
            _letFinishProcess = letFinishProcess;

            Contract.Assert(_stopRequestedCancelation != null || prevState == QueueAsyncProcessorState.Created);
            Contract.Assert(_stoppedCancelation != null || prevState == QueueAsyncProcessorState.Created);

            if (_stopRequestedCancelation != null)
                _stopRequestedCancelation.Cancel();
            if (!letFinishProcess && _stoppedCancelation != null)
                _stoppedCancelation.Cancel();

            if (waitForStop && prevState != QueueAsyncProcessorState.Created)
            {
                for (int i = 0; i < _procThreads.Length; i++)
                {
                    if (_procThreads[i] != null)
                        _procThreads[i].Join();
                }
            }

            for (int i = 0; i < _procThreads.Length; i++)
                _procThreads[i] = null;

            if (ActiveThreadCount == 0)
            {
                if (ChangeStateSafe(QueueAsyncProcessorState.Stopped, out prevState))
                {
                    Contract.Assert(prevState == QueueAsyncProcessorState.StopRequested);
                    _stoppedEvent.Set();
                    Profiling.Profiler.QueueAsyncProcessorDisposed(this.Name, false);
                }
            }

            Contract.Assert(State == QueueAsyncProcessorState.StopRequested || State == QueueAsyncProcessorState.Stopped);
            Contract.Assume(!waitForStop || State == QueueAsyncProcessorState.Stopped);
            return true;
        }


        /// <summary>
        /// Остановка и освобождение ресурсов
        /// </summary>
        /// <param name="waitForStop">Ожидать остановки</param>
        /// <param name="letFinishProcess">Позволить обработать всю очередь</param>
        /// <param name="completeAdding">Запретить добавление новых элементов</param>
        public virtual void Stop(bool waitForStop, bool letFinishProcess, bool completeAdding)
        {
            StopProcessor(waitForStop, letFinishProcess, completeAdding);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Остановка и освобождение ресурсов
        /// </summary>
        public void Stop()
        {
            this.Stop(true, true, true);
        }

        /// <summary>
        /// Основной код освобождения ресурсов
        /// </summary>
        /// <param name="isUserCall">Вызвано ли освобождение пользователем. False - деструктор</param>
        protected override void Dispose(bool isUserCall)
        {
            if (!this.IsStopRequestedOrStopped)
            {
                Contract.Assume(isUserCall, "QueueAsyncProcessor destructor: Better to dispose by user. Закомментируй, если не нравится.");

                if (isUserCall)
                    StopProcessor(true, false, true);

                QueueAsyncProcessorState prevState;
                ChangeStateSafe(QueueAsyncProcessorState.StopRequested, out prevState);
                ChangeStateSafe(QueueAsyncProcessorState.Stopped, out prevState);

                if (_stopRequestedCancelation != null)
                    _stopRequestedCancelation.Cancel();
                if (_stoppedCancelation != null)
                    _stoppedCancelation.Cancel();

                if (!isUserCall)
                    Profiling.Profiler.QueueAsyncProcessorDisposed(this.Name, true);
            }
            base.Dispose(isUserCall);
        }

        /// <summary>
        /// Финализатор
        /// </summary>
        ~QueueAsyncProcessor()
        {
            Dispose(false);
        }
    }
}
