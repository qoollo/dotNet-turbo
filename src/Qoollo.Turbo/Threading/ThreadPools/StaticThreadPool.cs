using Qoollo.Turbo.Threading.ThreadPools.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Threading.ThreadPools
{
    /// <summary>
    /// Параметры для StaticThreadPool
    /// </summary>
    public class StaticThreadPoolOptions
    {
        /// <summary>
        /// Стандартные опции
        /// </summary>
        internal static readonly StaticThreadPoolOptions Default = new StaticThreadPoolOptions();

        /// <summary>
        /// Стандартный период сна между проверкой возможности похитить элемент из соседних локальных очередей
        /// </summary>
        public const int DefaultQueueStealAwakePeriod = 1000;
        /// <summary>
        /// Стандартный период проверки переполненности очереди
        /// </summary>
        public const int DefaultQueueBlockedCheckPeriod = 2000;
        /// <summary>
        /// Максимальная величина расширения очереди по-умолчанию
        /// </summary>
        public const int DefaultMaxQueueCapacityExtension = 256;
        /// <summary>
        /// Стандартное значения для параметра использования своего шедуллера задач
        /// </summary>
        public const bool DefaultUseOwnTaskScheduler = true;
        /// <summary>
        /// Стандартное значения для параметра использования своего контекста синхронизации
        /// </summary>
        public const bool DefaultUseOwnSyncContext = true;
        /// <summary>
        /// Стандартное значения для параметра протаскивания контекст исполнения
        /// </summary>
        public const bool DefaultFlowExecutionContext = false;

        /// <summary>
        /// Конструктор StaticThreadPoolOptions
        /// </summary>
        public StaticThreadPoolOptions()
        {
            QueueStealAwakePeriod = DefaultQueueStealAwakePeriod;
            QueueBlockedCheckPeriod = DefaultQueueBlockedCheckPeriod;
            MaxQueueCapacityExtension = DefaultMaxQueueCapacityExtension;
            UseOwnTaskScheduler = DefaultUseOwnTaskScheduler;
            UseOwnSyncContext = DefaultUseOwnSyncContext;
            FlowExecutionContext = DefaultFlowExecutionContext;
        }

        /// <summary>
        /// Периоды сна между проверкой возможности похитить элемент из соседних локальных очередей
        /// </summary>
        public int QueueStealAwakePeriod { get; set; }
        /// <summary>
        /// Период проверки переполненности очереди
        /// </summary>
        public int QueueBlockedCheckPeriod { get; set; }
        /// <summary>
        /// Максимальная величина расширения очереди
        /// </summary>
        public int MaxQueueCapacityExtension { get; set; }
        /// <summary>
        /// Использовать ли свой шедуллер задач
        /// </summary>
        public bool UseOwnTaskScheduler { get; set; }
        /// <summary>
        /// Использовать ли свой контекст синхронизации
        /// </summary>
        public bool UseOwnSyncContext { get; set; }
        /// <summary>
        /// Протаскивать ли контекст исполнения
        /// </summary>
        public bool FlowExecutionContext { get; set; }
    }

    /// <summary>
    /// Пул потоков с фиксированным числом потоков.
    /// Тем не менее изменение числа потоков возможно путём явного вызова соостветствующих методов класса.
    /// </summary>
    public class StaticThreadPool: Common.CommonThreadPool
    {
        private readonly int _maxQueueCapacityExtension;
        private readonly int _queueBlockedCheckPeriod;

        private int _expectedThreadCount;
        private int _waitForDestroyThreadCount;

        private volatile bool _wasSomeProcessByThreadsFlag;

        private readonly object _syncObject = new object();

        /// <summary>
        /// Конструктор StaticThreadPool
        /// </summary>
        /// <param name="initialThreadCount">Начальное число потоков в пуле</param>
        /// <param name="queueBoundedCapacity">Максимальный размер очереди задач (-1 - не ограничен)</param>
        /// <param name="name">Имена потоков</param>
        /// <param name="isBackground">Использовать ли фоновые потоки</param>
        /// <param name="options">Расширенные настройки пула потоков</param>
        private StaticThreadPool(StaticThreadPoolOptions options, int initialThreadCount, int queueBoundedCapacity, string name, bool isBackground)
            : base(queueBoundedCapacity, options.QueueStealAwakePeriod, isBackground, name, options.UseOwnTaskScheduler, options.UseOwnSyncContext, options.FlowExecutionContext)
        {
            Contract.Requires<ArgumentNullException>(options != null);
            Contract.Requires<ArgumentException>(initialThreadCount >= 0);
            Contract.Requires<ArgumentException>(options.MaxQueueCapacityExtension >= 0);

            _maxQueueCapacityExtension = options.MaxQueueCapacityExtension > 0 ? options.MaxQueueCapacityExtension : 0;
            _queueBlockedCheckPeriod = options.QueueBlockedCheckPeriod > 0 ? options.QueueBlockedCheckPeriod : 0;

            _wasSomeProcessByThreadsFlag = false;

            if (queueBoundedCapacity > 0 && _maxQueueCapacityExtension > 0 && _queueBlockedCheckPeriod > 0)
                Qoollo.Turbo.Threading.ServiceStuff.ManagementThreadController.Instance.RegisterCallback(ManagementThreadProc);

            AddThreads(initialThreadCount);
        }
        /// <summary>
        /// Конструктор StaticThreadPool
        /// </summary>
        /// <param name="initialThreadCount">Начальное число потоков в пуле</param>
        /// <param name="queueBoundedCapacity">Максимальный размер очереди задач (-1 - не ограничен)</param>
        /// <param name="name">Имена потоков</param>
        /// <param name="isBackground">Использовать ли фоновые потоки</param>
        /// <param name="options">Расширенные настройки пула потоков</param>
        public StaticThreadPool(int initialThreadCount, int queueBoundedCapacity, string name, bool isBackground, StaticThreadPoolOptions options)
            : this(options ?? StaticThreadPoolOptions.Default, initialThreadCount, queueBoundedCapacity, name, isBackground)
        {

        }
        /// <summary>
        /// Конструктор StaticThreadPool
        /// </summary>
        /// <param name="initialThreadCount">Начальное число потоков в пуле</param>
        /// <param name="queueBoundedCapacity">Максимальный размер очереди задач (-1 - не ограничен)</param>
        /// <param name="name">Имена потоков</param>
        /// <param name="isBackground">Использовать ли фоновые потоки</param>
        public StaticThreadPool(int initialThreadCount, int queueBoundedCapacity, string name, bool isBackground)
            : this(StaticThreadPoolOptions.Default, initialThreadCount, queueBoundedCapacity, name, isBackground)
        {
        }
        /// <summary>
        /// Конструктор StaticThreadPool
        /// </summary>
        /// <param name="initialThreadCount">Начальное число потоков в пуле</param>
        /// <param name="queueBoundedCapacity">Максимальный размер очереди задач (-1 - не ограничен)</param>
        /// <param name="name">Имена потоков</param>
        public StaticThreadPool(int initialThreadCount, int queueBoundedCapacity, string name)
            : this(StaticThreadPoolOptions.Default, initialThreadCount, queueBoundedCapacity, name, false)
        {
        }
        /// <summary>
        /// Конструктор StaticThreadPool
        /// </summary>
        /// <param name="initialThreadCount">Начальное число потоков в пуле</param>
        /// <param name="name">Имена потоков</param>
        public StaticThreadPool(int initialThreadCount, string name)
            : this(StaticThreadPoolOptions.Default, initialThreadCount, -1, name, false)
        {
        }


        /// <summary>
        /// Увеличить число потоков в пуле
        /// </summary>
        /// <param name="count">Число потоков, на которое увеличиваем</param>
        public void AddThreads(int count = 1)
        {
            Contract.Requires<ArgumentException>(count >= 0);
            CheckPendingDisposeOrDisposed();
            

            Interlocked.Add(ref _expectedThreadCount, count);

            for (int i = 0; i < count; i++)
                AddNewThreadInner();
        }

        /// <summary>
        /// Уменьшить число потоков в пуле
        /// </summary>
        /// <param name="count">Число потоков, на которое уменьшаем</param>
        public void RemoveThreads(int count = 1)
        {
            Contract.Requires<ArgumentException>(count >= 0);
            CheckPendingDisposeOrDisposed();

            if (count > this.ThreadCount)
                count = this.ThreadCount;

            int currentExpectedCount = Volatile.Read(ref _expectedThreadCount);
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
        public void SetThreadCount(int count)
        {
            Contract.Requires<ArgumentException>(count >= 0);
            CheckPendingDisposeOrDisposed();

            Interlocked.Exchange(ref _expectedThreadCount, count);

            while ((this.ThreadCount < Volatile.Read(ref _expectedThreadCount)) && !IsStopRequestedOrStopped)
                AddNewThreadInner();
        }

        /// <summary>
        /// Заполнить число потоков в пуле до count
        /// </summary>
        /// <param name="count">Число потоков, до которого наполняем</param>
        public void FullPoolUpTo(int count)
        {
            Contract.Requires<ArgumentException>(count >= 0);
            CheckPendingDisposeOrDisposed();

            if (this.ThreadCount >= count)
                return;

            SpinWait sw = new SpinWait();
            int expectedThreadCount = Volatile.Read(ref _expectedThreadCount);
            while (expectedThreadCount < count)
            {
                if (Interlocked.CompareExchange(ref _expectedThreadCount, count, expectedThreadCount) == expectedThreadCount)
                    break;

                sw.SpinOnce();
                expectedThreadCount = Volatile.Read(ref _expectedThreadCount);
            }

            while ((this.ThreadCount < Volatile.Read(ref _expectedThreadCount)) && !IsStopRequestedOrStopped)
                AddNewThreadInner();
        }

        /// <summary>
        /// Вспомогательный метод добавления нового потока в статический пул
        /// </summary>
        /// <returns>Инициирован ли поток</returns>
        private bool AddNewThreadInner()
        {
            if (IsStopRequestedOrStopped || ThreadCount >= Volatile.Read(ref _expectedThreadCount))
                return false;

            lock (_syncObject)
            {
                if (IsStopRequestedOrStopped || ThreadCount >= Volatile.Read(ref _expectedThreadCount))
                    return false;

                return AddNewThread(ThreadProc) != null;
            }
        }


        /// <summary>
        /// Сделать пометку, что данный поток должен удалиться
        /// </summary>
        /// <returns>true - удалось, false - условие удаления было нарушено</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private bool TryRequestDieSlot()
        {
            int waitForDestroyThreadCount = Volatile.Read(ref _waitForDestroyThreadCount);

            SpinWait sw = new SpinWait();
            while (ThreadCount - waitForDestroyThreadCount > Volatile.Read(ref _expectedThreadCount))
            {
                if (Interlocked.CompareExchange(ref _waitForDestroyThreadCount, waitForDestroyThreadCount + 1, waitForDestroyThreadCount) == waitForDestroyThreadCount)
                    return true;

                sw.SpinOnce();
                waitForDestroyThreadCount = Volatile.Read(ref _waitForDestroyThreadCount);
            }

            return false;
        }
        /// <summary>
        /// Вернуть пометку, что поток должен удалиться (когда уже удалён)
        /// </summary>
        /// <returns>Успешность</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private bool TryReturnDieSlot()
        {
            int waitForDestroyThreadCount = Volatile.Read(ref _waitForDestroyThreadCount);

            SpinWait sw = new SpinWait();
            while (waitForDestroyThreadCount > 0)
            {
                if (Interlocked.CompareExchange(ref _waitForDestroyThreadCount, waitForDestroyThreadCount - 1, waitForDestroyThreadCount) == waitForDestroyThreadCount)
                    return true;

                sw.SpinOnce();
                waitForDestroyThreadCount = Volatile.Read(ref _waitForDestroyThreadCount);
            }

            return false;
        }


        /// <summary>
        /// Хэндлер удаления потока из пула
        /// </summary>
        /// <param name="elem">Удаляемый элемент</param>
        protected override void OnThreadRemove(Thread elem)
        {
            base.OnThreadRemove(elem);

            if (Volatile.Read(ref _waitForDestroyThreadCount) > 0)
                TryReturnDieSlot();
        }


        /// <summary>
        /// Проверяет, должен ли поток завершится и уменьшает счётчик ожидающих завершение потоков
        /// </summary>
        /// <returns>Должен ли завершиться</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool ShouldIDie()
        {
            int waitForDestroyThreadCount = Volatile.Read(ref _waitForDestroyThreadCount);
            int expectedThreadCount = Volatile.Read(ref _expectedThreadCount);

            if (this.ThreadCount - waitForDestroyThreadCount <= expectedThreadCount)
                return false;

            return TryRequestDieSlot();
        }


        /// <summary>
        /// Главный метод обработки задач
        /// </summary>
        /// <param name="privateData">Данные потока</param>
        /// <param name="token">Токен отмены (при остановке пула)</param>
        [System.Diagnostics.DebuggerNonUserCode]
        private void ThreadProc(ThreadPrivateData privateData, CancellationToken token)
        {
            if (privateData == null)
                throw new InvalidOperationException("privateData for Thread of ThreadPool can't be null");


            ThreadPoolWorkItem currentWorkItem = null;

            try
            {
                while (!token.IsCancellationRequested && !ShouldIDie())
                {
                    if (this.TryTakeWorkItemFromQueue(privateData, out currentWorkItem, Timeout.Infinite, token, false))
                    {
                        this.RunWorkItem(currentWorkItem);
                        currentWorkItem = null;

                        if (_wasSomeProcessByThreadsFlag == false)
                            _wasSomeProcessByThreadsFlag = true;
                    }
                    else
                    {
                        Debug.Assert(token.IsCancellationRequested);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                if (!token.IsCancellationRequested)
                    throw;
            }


            if (token.IsCancellationRequested)
            {
                if (LetFinishedProcess)
                {
                    while (this.TryTakeWorkItemFromQueue(privateData, out currentWorkItem))
                        this.RunWorkItem(currentWorkItem);
                }
                else
                {
                    while (this.TryTakeWorkItemFromQueue(privateData, out currentWorkItem))
                        this.CancelWorkItem(currentWorkItem);
                }
            }
        }

        /// <summary>
        /// Функция для обслуживающего потока.
        /// Отслеживаем ситуацию, когда пул завис и пытаемся решить вопрос расширением вместимости очереди.
        /// </summary>
        /// <param name="elapsedMs">Прошедшее с момента последней обработки время</param>
        /// <returns>Сделана ли обработка</returns>
        private bool ManagementThreadProc(int elapsedMs)
        {
            if (State == ThreadPoolState.Stopped)
            {
                Qoollo.Turbo.Threading.ServiceStuff.ManagementThreadController.Instance.UnregisterCallback(ManagementThreadProc);
                return false;
            }

            if (elapsedMs < _queueBlockedCheckPeriod)
                return false;

            if (this.QueueCapacity > 0 && this.ThreadCount > 0)
                if (!_wasSomeProcessByThreadsFlag)
                    if (this.GlobalQueueWorkItemCount >= this.ExtendedQueueCapacity)
                        if (this.ExtendedQueueCapacity - this.QueueCapacity < _maxQueueCapacityExtension)
                            this.ExtendGlobalQueueCapacity(this.ThreadCount + 1);

            _wasSomeProcessByThreadsFlag = false;

            return true;
        }

        /// <summary>
        /// Добавление задачи для пула потоков
        /// </summary>
        /// <param name="item">Задача</param>
        protected sealed override void AddWorkItem(ThreadPoolWorkItem item)
        {
            CheckDisposed();
            if (IsAddingCompleted)
                throw new InvalidOperationException("Adding was completed for ThreadPool: " + Name);

            this.PrepareWorkItem(item);
            this.AddWorkItemToQueue(item);       
        }
        /// <summary>
        /// Попытаться добавить задачу в пул потоков
        /// </summary>
        /// <param name="item">Задача</param>
        /// <returns>Успешность</returns>
        protected sealed override bool TryAddWorkItem(ThreadPoolWorkItem item)
        {
            if (State == ThreadPoolState.Stopped || IsAddingCompleted)
                return false;

            if (QueueCapacity > 0 && GlobalQueueWorkItemCount >= QueueCapacity)
                return false;

            this.PrepareWorkItem(item);
            return this.TryAddWorkItemToQueue(item);       
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
