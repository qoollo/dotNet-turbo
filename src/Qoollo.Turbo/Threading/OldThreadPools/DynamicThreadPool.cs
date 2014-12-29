using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Runtime;
using Qoollo.Turbo.Threading.OldThreadPools.ServiceStuff;

namespace Qoollo.Turbo.Threading.OldThreadPools
{
    /// <summary>
    /// Пул потоков с динамическим изменением числа задействованных потоков
    /// </summary>
    internal class DynamicThreadPool : CommonThreadPool
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

            Contract.Invariant(_activeThread.Count <= _maxThreadCount + _maxRescueThreadCount);
        }

        /// <summary>
        /// Обнаруживать ли запуск задачи из одного из потоков пула (для предотвращения dead-lock'а)
        /// </summary>
        private const bool TestThreadRecursion = true;
        /// <summary>
        /// Время ожидания на Add, после которого следует попробовать создать новый поток
        /// </summary>
        private const int AddWaitingTimeToSpawnMs = 250;
        /// <summary>
        /// Число потоков, до которого легко можно создавать новые
        /// </summary>
        private static readonly int FastSpawnThreadCountLimit = Environment.ProcessorCount + 2;

        private bool _isRecursiveAllow;

        /// <summary>
        /// Список всех потоков
        /// </summary>
        private List<Thread> _activeThread;
        private int _maxThreadCount;
        private int _freeThreadCount;
        private int _waitForDestroyThreadCount;

        private int _maxRescueThreadCount;
        private int _rescureThreadCreationInterval;
        private volatile bool _wasSomeActivity;
        private Thread _rescueThreadSpawner;
      
        private int _trimPeriod;
        private int _lastTrimTestTime;
        private int _minFreeElementsBeforeTrim;

        /// <summary>
        /// Очередь задач
        /// </summary>
        private BlockingCollection<ThreadPoolWorkItem> _actQueue;
        private int _maxActQueueSize;

        private CancellationTokenSource _threadWaitCancelation;
        private volatile bool _isDisposed;
        private volatile bool _letFinishProcess;

        /// <summary>
        /// Конструктор DynamicThreadPool
        /// </summary>
        /// <param name="maxThreadCount">Максимальное число потоков</param>
        /// <param name="maxQueueSize">Максимальный размер очереди задач (-1 - не ограничен)</param>
        /// <param name="trimPeriod">Периоды уменьшения числа потоков при их неиспользовании в миллисекундах (-1 - бесконечность)</param>
        /// <param name="maxRescueThreadCount">Максимальное число спасательных потоков</param>
        /// <param name="rescureThreadCreationInterval">Интервал создания спасательных потоков (когда нет продвижения задач) (-1 ~ 4 секунды)</param>
        /// <param name="isBackground">Использовать ли фоновые потоки</param>
        /// <param name="name">Имена потоков</param>
        /// <param name="isRecursiveSync">Разрешение выполнять задачи синхронно в случае рекурсивной постановки</param>
        /// <param name="useOwnSyncContext">Использовать ли свой контекст синхронизации</param>
        /// <param name="flowContext">Протаскивать ли контекст исполнения</param>
        public DynamicThreadPool(int maxThreadCount, int maxQueueSize, int trimPeriod, int maxRescueThreadCount, int rescureThreadCreationInterval,
                                 bool isBackground, string name, bool isRecursiveSync, bool useOwnSyncContext, bool flowContext)
            :base(isBackground, name, useOwnSyncContext, flowContext)
        {
            Contract.Requires<ArgumentException>(maxThreadCount > 0);

            _isRecursiveAllow = isRecursiveSync;

            _maxThreadCount = maxThreadCount;

            _maxRescueThreadCount = maxRescueThreadCount >= 0 ? maxRescueThreadCount : 0;
            _rescureThreadCreationInterval = rescureThreadCreationInterval > 0 ? rescureThreadCreationInterval : 4 * 1000;

            _activeThread = new List<Thread>(_maxThreadCount + _maxRescueThreadCount + 1);

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

            if (trimPeriod > 0)
                _trimPeriod = trimPeriod;
            else
                _trimPeriod = int.MaxValue;
            _wasSomeActivity = false;
            _lastTrimTestTime = GetTimeMeasureInMs();
            _minFreeElementsBeforeTrim = int.MaxValue;

            if (_maxRescueThreadCount > 0)
            {
                _rescueThreadSpawner = new Thread(RescueSpawnThreadProc);
                _rescueThreadSpawner.IsBackground = true;
                _rescueThreadSpawner.Name = "DynamicThreadPool: " + name + ". RescueThreadSpawner.";
                _rescueThreadSpawner.Start();
            }
        }
        /// <summary>
        /// Конструктор DynamicThreadPool
        /// </summary>
        /// <param name="maxThreadCount">Максимальное число потоков</param>
        /// <param name="maxQueueSize">Максимальный размер очереди задач (-1 - не ограничен)</param>
        /// <param name="trimPeriod">Периоды уменьшения числа потоков при их неиспользовании в миллисекундах (-1 - бесконечность)</param>
        /// <param name="maxRescueThreadCount">Максимальное число спасательных потоков</param>
        /// <param name="isBackground">Использовать ли фоновые потоки</param>
        /// <param name="name">Имена потоков</param>
        /// <param name="isRecursiveSync">Разрешение выполнять задачи синхронно в случае рекурсивной постановки</param>
        public DynamicThreadPool(int maxThreadCount, int maxQueueSize, int trimPeriod, int maxRescueThreadCount, bool isBackground, string name, bool isRecursiveSync)
            : this(maxThreadCount, maxQueueSize, trimPeriod, maxRescueThreadCount, -1, isBackground, name, isRecursiveSync, true, false)
        {
        }
        /// <summary>
        /// Конструктор DynamicThreadPool
        /// </summary>
        /// <param name="maxThreadCount">Максимальное число потоков</param>
        /// <param name="maxQueueSize">Максимальный размер очереди задач (-1 - не ограничен)</param>
        /// <param name="trimPeriod">Периоды уменьшения числа потоков при их неиспользовании (-1 - бесконечность)</param>
        /// <param name="maxRescueThreadCount">Максимальное число спасательных потоков</param>
        /// <param name="isBackground">Использовать ли фоновые потоки</param>
        /// <param name="name">Имена потоков</param>
        public DynamicThreadPool(int maxThreadCount, int maxQueueSize, int trimPeriod, int maxRescueThreadCount, bool isBackground, string name)
            : this(maxThreadCount, maxQueueSize, trimPeriod, maxRescueThreadCount, -1, isBackground, name, true, true, false)
        {
        }
        /// <summary>
        /// Конструктор DynamicThreadPool
        /// </summary>
        /// <param name="maxThreadCount">Максимальное число потоков</param>
        /// <param name="maxQueueSize">Максимальный размер очереди задач (-1 - не ограничен)</param>
        /// <param name="trimPeriod">Периоды уменьшения числа потоков при их неиспользовании (-1 - бесконечность)</param>
        /// <param name="maxRescueThreadCount">Максимальное число спасательных потоков</param>
        /// <param name="name">Имена потоков</param>
        public DynamicThreadPool(int maxThreadCount, int maxQueueSize, int trimPeriod, int maxRescueThreadCount, string name)
            : this(maxThreadCount, maxQueueSize, trimPeriod, maxRescueThreadCount, -1, false, name, true, true, false)
        {
        }
        /// <summary>
        /// Конструктор DynamicThreadPool
        /// </summary>
        /// <param name="maxThreadCount">Максимальное число потоков</param>
        /// <param name="maxQueueSize">Максимальный размер очереди задач (-1 - не ограничен)</param>
        /// <param name="isBackground">Использовать ли фоновые потоки</param>
        /// <param name="name">Имена потоков</param>
        public DynamicThreadPool(int maxThreadCount, int maxQueueSize, bool isBackground, string name)
            : this(maxThreadCount, maxQueueSize, -1, -1, -1, isBackground, name, true, true, false)
        {
        }
        /// <summary>
        /// Конструктор DynamicThreadPool
        /// </summary>
        /// <param name="maxThreadCount">Максимальное число потоков</param>
        /// <param name="maxQueueSize">Максимальный размер очереди задач (-1 - не ограничен)</param>
        /// <param name="name">Имена потоков</param>
        public DynamicThreadPool(int maxThreadCount, int maxQueueSize, string name)
            : this(maxThreadCount, maxQueueSize, -1, -1, -1, false, name, true, true, false)
        {
        }
        /// <summary>
        /// Конструктор DynamicThreadPool
        /// </summary>
        /// <param name="maxThreadCount">Максимальное число потоков</param>
        /// <param name="maxQueueSize">Максимальный размер очереди задач (-1 - не ограничен)</param>
        public DynamicThreadPool(int maxThreadCount, int maxQueueSize)
            : this(maxThreadCount, maxQueueSize, -1, -1, -1, false, null, true, true, false)
        {
        }
        /// <summary>
        /// Конструктор DynamicThreadPool
        /// </summary>
        /// <param name="maxThreadCount">Максимальное число потоков</param>
        /// <param name="name">Имена потоков</param>
        public DynamicThreadPool(int maxThreadCount, string name)
            : this(maxThreadCount, -1, -1, -1, -1, false, name, true, true, false)
        {
        }
        /// <summary>
        /// Конструктор DynamicThreadPool
        /// </summary>
        /// <param name="maxThreadCount">Максимальное число потоков</param>
        public DynamicThreadPool(int maxThreadCount)
            : this(maxThreadCount, -1, -1, -1, -1, false, null, true, true, false)
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
        /// Максимальное допустимое число потоков
        /// </summary>
        public int MaxThreadCount
        {
            get { return _maxThreadCount; }
        }
        /// <summary>
        /// Число незанятых потоков
        /// </summary>
        public int FreeThreadCount
        {
            get { return _freeThreadCount; }
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
        /// Наполнить пул до count потоков
        /// </summary>
        /// <param name="count">Число потоков, до которого наполняем</param>
        public void FillPoolUpTo(int count)
        {
            Contract.Requires<ArgumentException>(count >= 0);

            if (_isDisposed)
                throw new ObjectDisposedException("DynamicThreadPool");

            count = Math.Min(count, _maxThreadCount);
            int restCount = count - _activeThread.Count;

            for (int i = 0; i < restCount; i++)
                InitNewThread();
        }

        /// <summary>
        /// Создание и запуск нового потока
        /// </summary>
        /// <returns>Новый поток</returns>
        /// <param name="isRescue">Создаётся ли спасательный поток</param>
        private Thread InitNewThread(bool isRescue = false)
        {
            Thread res = null;

            if (_isDisposed)
                return null;

            int allowThreadCount = _maxThreadCount;
            if (isRescue)
                allowThreadCount += _maxRescueThreadCount;

            if (_activeThread.Count >= allowThreadCount)
                return null;

            bool success = false;
            ThreadStartControllingToken startController = null;
            try
            {
                lock (_activeThread)
                {
                    if (_activeThread.Count >= allowThreadCount)
                        return null;

                    int curTime = GetTimeMeasureInMs();
                    Interlocked.Exchange(ref _lastTrimTestTime, curTime);

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
                throw new CantInitThreadException("DynamicThreadPool. Exception during thread creation.", ex);
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
                        int currentValue = Volatile.Read(ref _waitForDestroyThreadCount);
                        while (currentValue > 0)
                        {
                            int lastValue = Interlocked.CompareExchange(ref _waitForDestroyThreadCount, Math.Max(0, currentValue - 1), currentValue);
                            if (lastValue == currentValue)
                                break;
                            currentValue = lastValue;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Проверяет, должен ли поток завершится, если присутствуют бездействующие потоки
        /// </summary>
        /// <returns>Должен ли завершиться</returns>
        private bool ShouldIDie()
        {
            bool doTrim = false;
            int lastTrimReqStorage = Volatile.Read(ref _lastTrimTestTime);
            int curTimeStorage = GetTimeMeasureInMs();

            // Проверяем на переполнение счётчика времени
            if (curTimeStorage < lastTrimReqStorage)
            {
                Interlocked.Exchange(ref _lastTrimTestTime, curTimeStorage);
            }
            else if (curTimeStorage - lastTrimReqStorage > _trimPeriod)
            {
                // Определяем, нужно ли уменьшать число элементов в пуле
                if (Interlocked.Exchange(ref _lastTrimTestTime, curTimeStorage) == lastTrimReqStorage)
                {
                    doTrim = _minFreeElementsBeforeTrim > 0;
                    Interlocked.Exchange(ref _minFreeElementsBeforeTrim, int.MaxValue);
                    if (doTrim)
                    {
                        Interlocked.Increment(ref _waitForDestroyThreadCount);
                        return true;
                    }
                }
            }

            // Проверяем, можем ли закрыть спасательный поток
            if (_maxRescueThreadCount > 0 &&
                Volatile.Read(ref _waitForDestroyThreadCount) < _activeThread.Count - _maxThreadCount && 
                _wasSomeActivity &&
                _actQueue.Count < ThreadCount)
            {
                int currentValue = Volatile.Read(ref _waitForDestroyThreadCount);
                while (currentValue < _activeThread.Count - _maxThreadCount)
                {
                    int lastValue = Interlocked.CompareExchange(ref _waitForDestroyThreadCount, currentValue + 1, currentValue);
                    if (lastValue == currentValue)
                    {
                        doTrim = true;
                        break;
                    }
                    currentValue = lastValue;
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
                        _wasSomeActivity = true;

                        // Извлечение действия
                        try
                        {
                            Interlocked.Increment(ref _freeThreadCount);
                            newAct = _actQueue.Take(token);
                        }
                        finally
                        {
                            Interlocked.Decrement(ref _freeThreadCount);
                        }

                        Profiling.Profiler.ThreadPoolWorkItemsCountDecreased(Name, PendingTasksCount, _maxActQueueSize);
                        Profiling.Profiler.ThreadPoolWaitingInQueueTime(Name, newAct.StopStoreTimer());

                        // Выполнение действия
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
        /// Функция потока, выполняющего отслеживание необходимости создания спасательных потоков
        /// </summary>
        private void RescueSpawnThreadProc()
        {
            CancellationToken token = _threadWaitCancelation.Token;

            while (!token.IsCancellationRequested)
            {
                if (_actQueue.Count > 1 && ThreadCount <= _actQueue.Count && !_wasSomeActivity)
                    InitNewThread(true);

                _wasSomeActivity = false;
                token.WaitHandle.WaitOne(_rescureThreadCreationInterval);
            }
        }

        /// <summary>
        /// Надо ли попробовать заспаунить поток на входе в метод добавления
        /// </summary>
        /// <returns>Надо ли</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool ShouldTryInitNewThreadFirst(bool fromTryAdd)
        {
            if (_activeThread.Count < _maxThreadCount && _activeThread.Count < FastSpawnThreadCountLimit && _actQueue.Count >= _activeThread.Count - 1)
                return true;

            if (_maxActQueueSize < 0)
                return _activeThread.Count < _maxThreadCount && _actQueue.Count >= 8 * _activeThread.Count;

            if (fromTryAdd)
                return _activeThread.Count < _maxThreadCount && (_actQueue.Count >= 8 * _activeThread.Count || _actQueue.Count >= _maxActQueueSize);

            return false;
        }
        /// <summary>
        /// Надо ли попробовать заспаунить поток после ожидания
        /// </summary>
        /// <returns>Надо ли</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool ShouldTryInitNewThreadAfterWait()
        {
            return _activeThread.Count < _maxThreadCount;
        }



        /// <summary>
        /// Добавление задачи для пула потоков
        /// </summary>
        /// <param name="item">Задача</param>
        protected override void AddWorkItemInner(ThreadPoolWorkItem item)
        {
            if (ShouldTryInitNewThreadFirst(false))
                InitNewThread();
            //if (Volatile.Read(ref _freeThreadCount) == 0 && _activeThread.Count < _maxThreadCount)
            //    InitNewThread();

            if (TestThreadRecursion && IsPoolThread())
            {
                if (!_actQueue.TryAdd(item))
                {
                    if (ShouldTryInitNewThreadAfterWait())
                        InitNewThread();

                    if (_isRecursiveAllow)
                    {
                        //Profiling.Profiler.ThreadPoolWorkItemRunInSync(Name);
                        RunWorkItem(item, true);
                    }
                    else
                    {
                        throw new LockRecursionException("Recursive DynamicThreadPool Run() led to deadlock");
                    }
                }
                else
                {
                    Profiling.Profiler.ThreadPoolWorkItemsCountIncreased(Name, PendingTasksCount, _maxActQueueSize);
                }
            }
            else
            {
                if (!_actQueue.TryAdd(item, AddWaitingTimeToSpawnMs))
                {
                    if (ShouldTryInitNewThreadAfterWait())
                        InitNewThread();
                    _actQueue.Add(item);
                }


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
            if (ShouldTryInitNewThreadFirst(true))
                InitNewThread();

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
            if (waitForStop && _rescueThreadSpawner != null)
                _rescueThreadSpawner.Join();
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
                Contract.Assume(isUserCall, "DynamicThreadPool destructor: Better to dispose by user. Закомментируй, если не нравится.");

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
        ~DynamicThreadPool()
        {
            Dispose(false);
        }
    }
}
