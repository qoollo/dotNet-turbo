using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Qoollo.Turbo.Threading.OldThreadPools.ServiceStuff;

namespace Qoollo.Turbo.Threading.OldThreadPools
{
    /// <summary>
    /// Пул потоков с фиксированным числом потоков.
    /// Тем не менее изменение числа потоков возможно путём явного вызова соостветствующих методов класса.
    /// </summary>
    internal class StaticThreadPool : CommonThreadPool
    {
        /// <summary>
        /// Контракты
        /// </summary>
        [ContractInvariantMethod]
        private void Invariant()
        {
            Contract.Invariant(_activeThread != null);

            Contract.Invariant(_threadWaitCancelation != null);

            Contract.Invariant(_actQueue != null);
        }

        /// <summary>
        /// Обнаруживать ли запуск задачи из одного из потоков пула (для предотвращения dead-lock'а)
        /// </summary>
        private const bool TestThreadRecursion = true;

        private bool _isRecursiveAllow;

        private List<Thread> _activeThread;
        private int _expectedThreadCount;
        private int _waitForDestroyThreadCount;

        /// <summary>
        /// Очередь задач
        /// </summary>
        private BlockingCollection<ThreadPoolWorkItem> _actQueue;
        private int _maxActQueueSize;

        private CancellationTokenSource _threadWaitCancelation;
        private volatile bool _isDisposed;
        private volatile bool _letFinishProcess;


        /// <summary>
        /// Конструктор StaticThreadPool
        /// </summary>
        /// <param name="initialThreadCount">Начальное число потоков в пуле</param>
        /// <param name="maxQueueSize">Максимальный размер очереди задач (-1 - не ограничен)</param>
        /// <param name="isBackground">Использовать ли фоновые потоки</param>
        /// <param name="name">Имена потоков</param>
        /// <param name="isRecursiveSync">Разрешение выполнять задачи синхронно в случае рекурсивной постановки</param>
        /// <param name="useOwnSyncContext">Использовать ли свой контекст синхронизации</param>
        /// <param name="flowContext">Протаскивать ли контекст исполнения</param>
        public StaticThreadPool(int initialThreadCount, int maxQueueSize, bool isBackground, string name, bool isRecursiveSync, bool useOwnSyncContext, bool flowContext)
            : base(isBackground, name, useOwnSyncContext, flowContext)
        {
            Contract.Requires<ArgumentException>(initialThreadCount >= 0);

            _isRecursiveAllow = isRecursiveSync;

            _activeThread = new List<Thread>(Math.Max(100, initialThreadCount) + 1);

            if (maxQueueSize > 0)
            {
                _maxActQueueSize = maxQueueSize;
                _actQueue = new BlockingCollection<ThreadPoolWorkItem>(new ConcurrentQueue<ThreadPoolWorkItem>(), maxQueueSize);
            }
            else
            {
                _maxActQueueSize = -1;
                _actQueue = new BlockingCollection<ThreadPoolWorkItem>(new ConcurrentQueue<ThreadPoolWorkItem>());
            }

            _threadWaitCancelation = new CancellationTokenSource();
            _isDisposed = false;

            AddThreads(initialThreadCount);
        }
        /// <summary>
        /// Конструктор StaticThreadPool
        /// </summary>
        /// <param name="initialThreadCount">Начальное число потоков в пуле</param>
        /// <param name="maxQueueSize">Максимальный размер очереди задач (-1 - не ограничен)</param>
        /// <param name="isBackground">Использовать ли фоновые потоки</param>
        /// <param name="name">Имена потоков</param>
        public StaticThreadPool(int initialThreadCount, int maxQueueSize, bool isBackground, string name)
            : this(initialThreadCount, maxQueueSize, isBackground, name, true, true, false)
        {
        }
        /// <summary>
        /// Конструктор StaticThreadPool
        /// </summary>
        /// <param name="initialThreadCount">Начальное число потоков в пуле</param>
        /// <param name="maxQueueSize">Максимальный размер очереди задач (-1 - не ограничен)</param>
        /// <param name="name">Имена потоков</param>
        public StaticThreadPool(int initialThreadCount, int maxQueueSize, string name)
            : this(initialThreadCount, maxQueueSize, false, name, true, true, false)
        {
        }
        /// <summary>
        /// Конструктор StaticThreadPool
        /// </summary>
        /// <param name="initialThreadCount">Начальное число потоков в пуле</param>
        /// <param name="maxQueueSize">Максимальный размер очереди задач (-1 - не ограничен)</param>
        public StaticThreadPool(int initialThreadCount, int maxQueueSize)
            : this(initialThreadCount, maxQueueSize, false, null, true, true, false)
        {
        }
        /// <summary>
        /// Конструктор StaticThreadPool
        /// </summary>
        /// <param name="initialThreadCount">Начальное число потоков в пуле</param>
        public StaticThreadPool(int initialThreadCount)
            : this(initialThreadCount, -1, false, null, true, true, false)
        {
        }

        /// <summary>
        /// Работает ли пул
        /// </summary>
        public bool IsWork
        {
            get { return !_isDisposed; }
        }

        /// <summary>
        /// Число активных потоков
        /// </summary>
        public int ThreadCount
        {
            get { return _activeThread.Count; }
        }

        /// <summary>
        /// Число задач, ожидающих выполнения
        /// </summary>
        public int PendingTasksCount
        {
            get { return _actQueue.Count; }
        }


        /// <summary>
        /// Получить отсчёт времени в миллисекундах
        /// </summary>
        /// <returns>Текущее значение</returns>
        private static int GetTimeMeasureInMs()
        {
            return Environment.TickCount & int.MaxValue;
        }


        /// <summary>
        /// Увеличить число потоков в пуле
        /// </summary>
        /// <param name="count">Число потоков, на которое увеличиваем</param>
        public void AddThreads(int count = 1)
        {
            Contract.Requires<ArgumentException>(count >= 0);

            if (_isDisposed)
                throw new ObjectDisposedException("StaticThreadPool");

            Interlocked.Add(ref _expectedThreadCount, count);

            for (int i = 0; i < count; i++)
                InitNewThread();
        }

        /// <summary>
        /// Уменьшить число потоков в пуле
        /// </summary>
        /// <param name="count">Число потоков, на которое уменьшаем</param>
        public void RemoveThreads(int count = 1)
        {
            Contract.Requires<ArgumentException>(count >= 0);

            if (_isDisposed)
                throw new ObjectDisposedException("StaticThreadPool");

            if (count > ThreadCount)
                count = ThreadCount;

            int currentExpectedCount = _expectedThreadCount;
            while (true)
            {
                int originalVal = Interlocked.CompareExchange(ref _expectedThreadCount, Math.Max(0, currentExpectedCount - count), currentExpectedCount);
                if (originalVal == currentExpectedCount)
                    break;
                currentExpectedCount = originalVal;
            }
        }


        /// <summary>
        /// Установить count потоков в пуле
        /// </summary>
        /// <param name="count">Число потоков, до которого наполняем</param>
        public void FillPoolUpTo(int count)
        {
            Contract.Requires<ArgumentException>(count >= 0);

            if (_isDisposed)
                throw new ObjectDisposedException("StaticThreadPool");

            Interlocked.Exchange(ref _expectedThreadCount, count);

            while ((ThreadCount < count) && !_isDisposed)
                InitNewThread();
        }


        /// <summary>
        /// Создание и запуск нового потока
        /// </summary>
        /// <returns>Новый поток</returns>
        private Thread InitNewThread()
        {
            Thread res = null;

            if (_isDisposed)
                return null;

            if (_activeThread.Count >= Volatile.Read(ref _expectedThreadCount))
                return null;

            bool success = false;
            ThreadStartControllingToken startController = null;
            try
            {
                lock (_activeThread)
                {
                    if (_activeThread.Count >= Volatile.Read(ref _expectedThreadCount))
                        return null;

                    startController = new ThreadStartControllingToken();
                    res = this.CreateNewThread(ThreadProc, startController);
                    res.Start();

                    try { }
                    finally
                    {
                        _activeThread.Add(res);
                        success = true;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new CantInitThreadException("StaticThreadPool. Exception during thread creation.", ex);
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
        /// Удаление потока из пула (предполагается, что поток уже завершился или находится на стадии завершения)
        /// </summary>
        /// <param name="elem">Поток</param>
        private void RemoveElement(Thread elem)
        {
            lock (_activeThread)
            {
                try { }
                finally
                {
                    if (_activeThread.Remove(elem))
                    {
                        if (_waitForDestroyThreadCount > 0)
                            if (Interlocked.Decrement(ref _waitForDestroyThreadCount) < 0)
                                Interlocked.Increment(ref _waitForDestroyThreadCount);
                    }
                }
            }
        }


        /// <summary>
        /// Проверяет, должен ли поток завершится и уменьшает счётчик ожидающих завершение потоков
        /// </summary>
        /// <returns>Должен ли завершиться</returns>
        private bool ShouldIDie()
        {
            bool doTrim = false;

            // Проверяем, можем ли закрыть поток
            if (Volatile.Read(ref _waitForDestroyThreadCount) < _activeThread.Count - _expectedThreadCount)
            {
                try { }
                finally
                {
                    if (Interlocked.Increment(ref _waitForDestroyThreadCount) <= _activeThread.Count - _expectedThreadCount)
                        doTrim = true;
                    else
                        Interlocked.Decrement(ref _waitForDestroyThreadCount);
                }
            }

            return doTrim;
        }


        /// <summary>
        /// Процедура потока
        /// </summary>
        private void ThreadProc()
        {
            CancellationToken token = _threadWaitCancelation.Token;
            ThreadPoolWorkItem newAct;

            Profiling.ProfilingTimer processTimer = new Profiling.ProfilingTimer();
            processTimer.StartTime();
            Profiling.Profiler.ThreadPoolThreadCountChange(Name, ThreadCount);

            try
            {
                try
                {
                    while (!token.IsCancellationRequested && !ShouldIDie())
                    {
                        newAct = _actQueue.Take(token);

                        Profiling.Profiler.ThreadPoolWorkItemsCountDecreased(Name, PendingTasksCount, _maxActQueueSize);
                        Profiling.Profiler.ThreadPoolWaitingInQueueTime(Name, newAct.StopStoreTimer());

                        processTimer.RestartTime();
                        RunWorkItem(newAct);
                        Profiling.Profiler.ThreadPoolWorkProcessed(Name, processTimer.GetTime());
                    }
                }
                catch (OperationCanceledException ex)
                {
                    if (ex.CancellationToken != token)
                        throw;
                }

                if (_letFinishProcess && token.IsCancellationRequested)
                {
                    while (_actQueue.TryTake(out newAct))
                    {
                        Profiling.Profiler.ThreadPoolWaitingInQueueTime(Name, newAct.StopStoreTimer());
                        Profiling.Profiler.ThreadPoolWorkItemsCountDecreased(Name, PendingTasksCount, _maxActQueueSize);

                        processTimer.RestartTime();
                        RunWorkItem(newAct);
                        Profiling.Profiler.ThreadPoolWorkProcessed(Name, processTimer.GetTime());
                    }
                }   
            }
            finally
            {
                RemoveElement(Thread.CurrentThread);

                Profiling.Profiler.ThreadPoolThreadCountChange(Name, ThreadCount);
            }
        }


        /// <summary>
        /// Добавление задачи для пула потоков
        /// </summary>
        /// <param name="item">Задача</param>
        protected override void AddWorkItemInner(ThreadPoolWorkItem item)
        {
            if (TestThreadRecursion && IsPoolThread())
            {
                if (!_actQueue.TryAdd(item))
                {
                    if (_isRecursiveAllow)
                    {
                        //Profiling.Profiler.ThreadPoolWorkItemRunInSync(Name);
                        RunWorkItem(item, true);
                    }
                    else
                    {
                        throw new LockRecursionException("Recursive StaticThreadPool Run() led to deadlock");
                    }
                }
                else
                {
                    Profiling.Profiler.ThreadPoolWorkItemsCountIncreased(Name, PendingTasksCount, _maxActQueueSize);
                }
            }
            else
            {
                _actQueue.Add(item);
                Profiling.Profiler.ThreadPoolWorkItemsCountIncreased(Name, PendingTasksCount, _maxActQueueSize);
            }
        }
        /// <summary>
        /// Попытаться добавить задачу в пул потоков
        /// </summary>
        /// <param name="item">Задача</param>
        /// <returns>Успешность</returns>
        protected override bool TryAddWorkItemInner(ThreadPoolWorkItem item)
        {
            bool result = _actQueue.TryAdd(item);
            if (result)
                Profiling.Profiler.ThreadPoolWorkItemsCountIncreased(Name, PendingTasksCount, _maxActQueueSize);
            else
                Profiling.Profiler.ThreadPoolWorkItemRejectedInTryAdd(Name);
            return result;
        }



        /// <summary>
        /// Внутренняя остановка пула
        /// </summary>
        /// <param name="waitForStop">Ожидать остановки</param>
        /// <param name="letFinishProcess">Позволить обработать всю очередь</param>
        /// <param name="completeAdding">Запретить добавление новых элементов</param>
        private void StopInner(bool waitForStop, bool letFinishProcess, bool completeAdding)
        {
            if (_isDisposed)
                return;

            Thread[] waitFor = null;

            lock (_activeThread)
            {
                if (_isDisposed)
                    return;

                _letFinishProcess = letFinishProcess;

                if (waitForStop)
                    waitFor = _activeThread.ToArray();

                _isDisposed = true;
                _threadWaitCancelation.Cancel();
            }


            if (completeAdding)
                _actQueue.CompleteAdding();


            if (waitForStop && waitFor != null)
            {
                for (int i = 0; i < waitFor.Length; i++)
                    waitFor[i].Join();
            }
        }




        /// <summary>
        /// Ожидание полной остановки
        /// </summary>
        public void WaitUntilStop()
        {
            SpinWait sw = new SpinWait();
            while (_activeThread.Count > 0)
                sw.SpinOnce();
        }

        /// <summary>
        /// Ожидание полной остановки с таймаутом
        /// </summary>
        /// <param name="timeout">Таймаут ожидания в миллисекундах</param>
        /// <returns>true - дождались, false - вышли по таймауту</returns>
        public bool WaitUntilStop(int timeout)
        {
            return SpinWait.SpinUntil(() => _activeThread.Count == 0, timeout);
        }


        /// <summary>
        /// Остановка и освобождение ресурсов
        /// </summary>
        /// <param name="waitForStop">Ожидать остановки</param>
        /// <param name="letFinishProcess">Позволить обработать всю очередь</param>
        /// <param name="completeAdding">Запретить добавление новых элементов</param>
        public void Dispose(bool waitForStop, bool letFinishProcess, bool completeAdding)
        {
            StopInner(waitForStop, letFinishProcess, completeAdding);
            GC.SuppressFinalize(this);
        }


        /// <summary>
        /// Основной код освобождения ресурсов
        /// </summary>
        /// <param name="isUserCall">Вызвано ли освобождение пользователем. False - деструктор</param>
        protected override void Dispose(bool isUserCall)
        {
            if (!_isDisposed)
            {
                Contract.Assume(isUserCall, "StaticThreadPool destructor: Better to dispose by user. Закомментируй, если не нравится.");

                if (isUserCall)
                {
                    StopInner(true, false, false);
                    _actQueue.CompleteAdding();
                }

                _isDisposed = true;
                if (_threadWaitCancelation != null)
                    _threadWaitCancelation.Cancel();

                //if (!isUserCall)
                //    Profiling.Profiler.ThreadPoolFinalizerRun(Name);
            }
            base.Dispose(isUserCall);
        }


        /// <summary>
        /// Деструктор
        /// </summary>
        ~StaticThreadPool()
        {
            Dispose(false);
        }
    }
}
