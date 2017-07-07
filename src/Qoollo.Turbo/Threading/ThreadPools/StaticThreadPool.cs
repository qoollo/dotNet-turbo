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
    /// Configuration parameters for <see cref="StaticThreadPool"/>
    /// </summary>
    public class StaticThreadPoolOptions
    {
        /// <summary>
        /// Default parameters
        /// </summary>
        internal static readonly StaticThreadPoolOptions Default = new StaticThreadPoolOptions();

        /// <summary>
        /// Default period of sleep in milliseconds between checking for the possibility to steal a work item from local queues
        /// </summary>
        public const int DefaultQueueStealAwakePeriod = 1000;
        /// <summary>
        /// Default period in milliseconds when a decision is made to extend the queue capacity
        /// </summary>
        public const int DefaultQueueBlockedCheckPeriod = 2000;
        /// <summary>
        /// Default maximum queue capacity extension
        /// </summary>
        public const int DefaultMaxQueueCapacityExtension = 256;
        /// <summary>
        /// Default value indicating whether or not set ThreadPool TaskScheduler as a default for all ThreadPool threads
        /// </summary>
        public const bool DefaultUseOwnTaskScheduler = false;
        /// <summary>
        /// Default value indicating whether or not set ThreadPool SynchronizationContext as a default for all ThreadPool threads
        /// </summary>
        public const bool DefaultUseOwnSyncContext = false;
        /// <summary>
        /// Default value indicating whether or not to flow ExecutionContext to the ThreadPool thread
        /// </summary>
        public const bool DefaultFlowExecutionContext = false;

        /// <summary>
        /// <see cref="StaticThreadPoolOptions"/> constructor
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
        /// Gets or sets period of sleep in milliseconds between checking for the possibility to steal a work item from local queues
        /// </summary>
        public int QueueStealAwakePeriod { get; set; }
        /// <summary>
        /// Gets or sets the period in milliseconds when a decision is made to extend the queue capacity
        /// </summary>
        public int QueueBlockedCheckPeriod { get; set; }
        /// <summary>
        /// Gets or sets the maximum queue capacity extension
        /// </summary>
        public int MaxQueueCapacityExtension { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether or not to set ThreadPool TaskScheduler as a default for all ThreadPool threads
        /// </summary>
        public bool UseOwnTaskScheduler { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether or not to set ThreadPool SynchronizationContext as a default for all ThreadPool threads
        /// </summary>
        public bool UseOwnSyncContext { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether or not to flow ExecutionContext to the ThreadPool thread
        /// </summary>
        public bool FlowExecutionContext { get; set; }
    }

    /// <summary>
    /// ThreadPool in which the number of threads is selected by user
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
        /// <see cref="StaticThreadPool"/> constructor
        /// </summary>
        /// <param name="initialThreadCount">Initial number of threads in ThreadPool</param>
        /// <param name="queueBoundedCapacity">The bounded size of the work items queue (if less or equal to 0 then no limitation)</param>
        /// <param name="name">The name for this instance of ThreadPool and for its threads</param>
        /// <param name="isBackground">Whether or not threads are a background threads</param>
        /// <param name="options">Additional thread pool creation parameters</param>
        private StaticThreadPool(StaticThreadPoolOptions options, int initialThreadCount, int queueBoundedCapacity, string name, bool isBackground)
            : base(queueBoundedCapacity, options.QueueStealAwakePeriod, isBackground, name, options.UseOwnTaskScheduler, options.UseOwnSyncContext, options.FlowExecutionContext)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));
            if (initialThreadCount < 0)
                throw new ArgumentOutOfRangeException(nameof(initialThreadCount));
            if (options.MaxQueueCapacityExtension < 0)
                throw new ArgumentOutOfRangeException(nameof(options.MaxQueueCapacityExtension));

            _maxQueueCapacityExtension = options.MaxQueueCapacityExtension > 0 ? options.MaxQueueCapacityExtension : 0;
            _queueBlockedCheckPeriod = options.QueueBlockedCheckPeriod > 0 ? options.QueueBlockedCheckPeriod : 0;

            _wasSomeProcessByThreadsFlag = false;

            if (queueBoundedCapacity > 0 && _maxQueueCapacityExtension > 0 && _queueBlockedCheckPeriod > 0)
                Qoollo.Turbo.Threading.ServiceStuff.ManagementThreadController.Instance.RegisterCallback(ManagementThreadProc);

            AddThreads(initialThreadCount);
        }
        /// <summary>
        /// <see cref="StaticThreadPool"/> constructor
        /// </summary>
        /// <param name="initialThreadCount">Initial number of threads in ThreadPool</param>
        /// <param name="queueBoundedCapacity">The bounded size of the work items queue (if less or equal to 0 then no limitation)</param>
        /// <param name="name">The name for this instance of ThreadPool and for its threads</param>
        /// <param name="isBackground">Whether or not threads are a background threads</param>
        /// <param name="options">Additional thread pool creation parameters</param>
        public StaticThreadPool(int initialThreadCount, int queueBoundedCapacity, string name, bool isBackground, StaticThreadPoolOptions options)
            : this(options ?? StaticThreadPoolOptions.Default, initialThreadCount, queueBoundedCapacity, name, isBackground)
        {

        }
        /// <summary>
        /// <see cref="StaticThreadPool"/> constructor
        /// </summary>
        /// <param name="initialThreadCount">Initial number of threads in ThreadPool</param>
        /// <param name="queueBoundedCapacity">The bounded size of the work items queue (if less or equal to 0 then no limitation)</param>
        /// <param name="name">The name for this instance of ThreadPool and for its threads</param>
        /// <param name="isBackground">Whether or not threads are a background threads</param>
        public StaticThreadPool(int initialThreadCount, int queueBoundedCapacity, string name, bool isBackground)
            : this(StaticThreadPoolOptions.Default, initialThreadCount, queueBoundedCapacity, name, isBackground)
        {
        }
        /// <summary>
        /// <see cref="StaticThreadPool"/> constructor
        /// </summary>
        /// <param name="initialThreadCount">Initial number of threads in ThreadPool</param>
        /// <param name="queueBoundedCapacity">The bounded size of the work items queue (if less or equal to 0 then no limitation)</param>
        /// <param name="name">The name for this instance of ThreadPool and for its threads</param>
        public StaticThreadPool(int initialThreadCount, int queueBoundedCapacity, string name)
            : this(StaticThreadPoolOptions.Default, initialThreadCount, queueBoundedCapacity, name, false)
        {
        }
        /// <summary>
        /// <see cref="StaticThreadPool"/> constructor
        /// </summary>
        /// <param name="initialThreadCount">Initial number of threads in ThreadPool</param>
        /// <param name="name">The name for this instance of ThreadPool and for its threads</param>
        public StaticThreadPool(int initialThreadCount, string name)
            : this(StaticThreadPoolOptions.Default, initialThreadCount, -1, name, false)
        {
        }


        /// <summary>
        /// Adds the specified number of threads to the ThreadPool
        /// </summary>
        /// <param name="count">Number of threads that should be added to ThreadPool</param>
        public void AddThreads(int count = 1)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            CheckPendingDisposeOrDisposed();
            

            Interlocked.Add(ref _expectedThreadCount, count);

            for (int i = 0; i < count; i++)
                AddNewThreadInner();
        }

        /// <summary>
        /// Removes the specified number of threads to the ThreadPool
        /// </summary>
        /// <param name="count">Number of threads that should be removed from ThreadPool</param>
        public void RemoveThreads(int count = 1)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
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
        /// Sets the number of threads in ThreadPool to the specified value
        /// </summary>
        /// <param name="count">The number of threads that must be present in the pool after the call</param>
        public void SetThreadCount(int count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            CheckPendingDisposeOrDisposed();

            Interlocked.Exchange(ref _expectedThreadCount, count);

            while ((this.ThreadCount < Volatile.Read(ref _expectedThreadCount)) && !IsStopRequestedOrStopped)
                AddNewThreadInner();
        }

        /// <summary>
        /// Warms-up pool by adding new threads up to the specified '<paramref name="count"/>'
        /// </summary>
        /// <param name="count">The number of threads that must be present in the pool after the call</param>
        public void FullPoolUpTo(int count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
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
        /// Adds new thread to the ThreadPool (helper method)
        /// </summary>
        /// <returns>Ture when thread added successfully</returns>
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
        /// Main processing method
        /// </summary>
        /// <param name="privateData">Thread local data</param>
        /// <param name="token">Cancellation token to stop the thread</param>
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
        /// Places a new task to the thread pool queue
        /// </summary>
        /// <param name="item">Thread pool work item</param>
        protected sealed override void AddWorkItem(ThreadPoolWorkItem item)
        {
            CheckDisposed();
            if (IsAddingCompleted)
                throw new InvalidOperationException("Adding was completed for ThreadPool: " + Name);

            this.PrepareWorkItem(item);
            this.AddWorkItemToQueue(item);       
        }
        /// <summary>
        /// Attemts to place a new task to the thread pool queue
        /// </summary>
        /// <param name="item">Thread pool work item</param>
        /// <returns>True if work item was added to the queue, otherwise false</returns>
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
        /// Finalizer
        /// </summary>
        ~StaticThreadPool()
        {
            Dispose(false);
        }
    }
}
