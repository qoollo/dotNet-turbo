using Qoollo.Turbo.Threading.ThreadPools.Common;
using Qoollo.Turbo.Threading.ThreadPools.ServiceStuff;
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
    /// Configuration parameters for <see cref="DynamicThreadPool"/>
    /// </summary>
    public class DynamicThreadPoolOptions
    {
        /// <summary>
        /// Default parameters
        /// </summary>
        internal static readonly DynamicThreadPoolOptions Default = new DynamicThreadPoolOptions();

        /// <summary>
        /// Default period in milliseconds when unused threads will be removed from ThreadPool
        /// </summary>
        public const int DefaultNoWorkItemTrimPeriod = 5 * 60 * 1000;
        /// <summary>
        /// Default period of sleep in milliseconds between checking for the possibility to steal a work item from local queues
        /// </summary>
        public const int DefaultQueueStealAwakePeriod = 2000;
        /// <summary>
        /// Default maximum queue capacity extension
        /// </summary>
        public const int DefaultMaxQueueCapacityExtension = 256;
        /// <summary>
        /// Default priod in milliseconds when the management thread is checks the state of the pool and changes the number of threads
        /// </summary>
        public const int DefaultManagementProcessPeriod = 500;
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
        /// <see cref="DynamicThreadPoolOptions"/> constructor
        /// </summary>
        public DynamicThreadPoolOptions()
        {
            NoWorkItemTrimPeriod = DefaultNoWorkItemTrimPeriod;
            QueueStealAwakePeriod = DefaultQueueStealAwakePeriod;
            ManagementProcessPeriod = DefaultManagementProcessPeriod;
            MaxQueueCapacityExtension = DefaultMaxQueueCapacityExtension;
            UseOwnTaskScheduler = DefaultUseOwnTaskScheduler;
            UseOwnSyncContext = DefaultUseOwnSyncContext;
            FlowExecutionContext = DefaultFlowExecutionContext;
        }

        /// <summary>
        /// Gets or sets the period in milliseconds when unused and suspended threads will be removed from ThreadPool (if less than zero then threads are never removed)
        /// </summary>
        public int NoWorkItemTrimPeriod { get; set; }
        /// <summary>
        /// Gets or sets period of sleep in milliseconds between checking for the possibility to steal a work item from local queues
        /// </summary>
        public int QueueStealAwakePeriod { get; set; }
        /// <summary>
        /// Gets or sets the maximum queue capacity extension
        /// </summary>
        public int MaxQueueCapacityExtension { get; set; }
        /// <summary>
        /// Gets or sets the period in milliseconds when the management procedure checks the state of the ThreadPool and changes the number of active threads
        /// </summary>
        public int ManagementProcessPeriod { get; set; }
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
    /// ThreadPool that automatically changes the number of running threads in accordance with the workload
    /// </summary>
    public class DynamicThreadPool: Common.CommonThreadPool
    {
        internal static bool DisableCritical = false;

        private const int WorkItemPerThreadLimit = 32;
        private const int NoWorkItemPreventDeactivationPeriod = 2 * 1000;
        /// <summary>
        /// Число потоков, до которого легко можно создавать новые
        /// </summary>
        private static readonly int FastSpawnThreadCountLimit = Environment.ProcessorCount <= 2 ? Environment.ProcessorCount : Environment.ProcessorCount / 2;
        private static readonly int ReasonableThreadCount = Environment.ProcessorCount;


        private readonly int _minThreadCount;
        private readonly int _maxThreadCount;

        private readonly int _fastSpawnThreadCountLimit;
        private readonly int _reasonableThreadCount;

        private readonly int _managementProcessPeriod;
        private readonly int _maxQueueCapacityExtension;
        private readonly int _noWorkItemTrimPeriod;

        private readonly ExecutionThroughoutTrackerUpDownCorrection _throughoutTracker;
        private volatile bool _wasSomeProcessByThreadsFlag;

        /// <summary>
        /// Композитная переменная, хранящая DieSlot в старших 8 битах, ActiveThreadCount в следующих 12 битах и FullThreadCount в оставшихся 12 битах.
        /// Нужна для возможности атомарного обновления этих 3-ёх значений.
        /// </summary>
        private int _dieSlotActiveFullThreadCountCombination;

        private readonly PartialThreadBlocker _extThreadBlocker;

        private readonly object _syncObject = new object();


        /// <summary>
        /// <see cref="DynamicThreadPool"/> constructor
        /// </summary>
        /// <param name="minThreadCount">Minimum number of threads that should always be in pool</param>
        /// <param name="maxThreadCount">Maximum number of threads in pool</param>
        /// <param name="queueBoundedCapacity">The bounded size of the work items queue (if less or equal to 0 then no limitation)</param>
        /// <param name="name">The name for this instance of ThreadPool and for its threads</param>
        /// <param name="isBackground">Whether or not threads are a background threads</param>
        /// <param name="options">Additional thread pool creation parameters</param>
        private DynamicThreadPool(DynamicThreadPoolOptions options, int minThreadCount, int maxThreadCount, int queueBoundedCapacity, string name, bool isBackground)
            : base(queueBoundedCapacity, options.QueueStealAwakePeriod, isBackground, name, options.UseOwnTaskScheduler, options.UseOwnSyncContext, options.FlowExecutionContext)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));
            if (minThreadCount < 0)
                throw new ArgumentOutOfRangeException(nameof(minThreadCount));
            if (maxThreadCount <= 0 || maxThreadCount >= 4096)
                throw new ArgumentOutOfRangeException(nameof(maxThreadCount), "maxThreadCount should be in range [0, 4096)");
            if (maxThreadCount < minThreadCount)
                throw new ArgumentOutOfRangeException(nameof(maxThreadCount), "maxThreadCount should be greater than minThreadCount");
            if (options.MaxQueueCapacityExtension < 0)
                throw new ArgumentOutOfRangeException(nameof(options.MaxQueueCapacityExtension));
            if (options.ManagementProcessPeriod <= 0)
                throw new ArgumentOutOfRangeException(nameof(options.ManagementProcessPeriod));

            _minThreadCount = minThreadCount;
            _maxThreadCount = maxThreadCount;

            _fastSpawnThreadCountLimit = Math.Max(_minThreadCount, Math.Min(_maxThreadCount, FastSpawnThreadCountLimit));
            _reasonableThreadCount = Math.Max(_minThreadCount, Math.Min(_maxThreadCount, ReasonableThreadCount));

            _managementProcessPeriod = options.ManagementProcessPeriod;
            _maxQueueCapacityExtension = options.MaxQueueCapacityExtension;
            _noWorkItemTrimPeriod = options.NoWorkItemTrimPeriod >= 0 ? options.NoWorkItemTrimPeriod : -1;

            _throughoutTracker = new ExecutionThroughoutTrackerUpDownCorrection(_maxThreadCount, _reasonableThreadCount);
            _wasSomeProcessByThreadsFlag = false;

            _dieSlotActiveFullThreadCountCombination = 0;

            _extThreadBlocker = new PartialThreadBlocker(0);

            Qoollo.Turbo.Threading.ServiceStuff.ManagementThreadController.Instance.RegisterCallback(ManagementThreadProc);

            FillPoolUpTo(minThreadCount);
        }
        /// <summary>
        /// <see cref="DynamicThreadPool"/> constructor
        /// </summary>
        /// <param name="minThreadCount">Minimum number of threads that should always be in pool</param>
        /// <param name="maxThreadCount">Maximum number of threads in pool</param>
        /// <param name="queueBoundedCapacity">The bounded size of the work items queue (if less or equal to 0 then no limitation)</param>
        /// <param name="name">The name for this instance of ThreadPool and for its threads</param>
        /// <param name="isBackground">Whether or not threads are a background threads</param>
        /// <param name="options">Additional thread pool creation parameters</param>
        public DynamicThreadPool(int minThreadCount, int maxThreadCount, int queueBoundedCapacity, string name, bool isBackground, DynamicThreadPoolOptions options)
            : this(options ?? DynamicThreadPoolOptions.Default, minThreadCount, maxThreadCount, queueBoundedCapacity, name, isBackground)
        {
        }
        /// <summary>
        /// <see cref="DynamicThreadPool"/> constructor
        /// </summary>
        /// <param name="minThreadCount">Minimum number of threads that should always be in pool</param>
        /// <param name="maxThreadCount">Maximum number of threads in pool</param>
        /// <param name="queueBoundedCapacity">The bounded size of the work items queue (if less or equal to 0 then no limitation)</param>
        /// <param name="name">The name for this instance of ThreadPool and for its threads</param>
        /// <param name="isBackground">Whether or not threads are a background threads</param>
        public DynamicThreadPool(int minThreadCount, int maxThreadCount, int queueBoundedCapacity, string name, bool isBackground)
            : this(DynamicThreadPoolOptions.Default, minThreadCount, maxThreadCount, queueBoundedCapacity, name, isBackground)
        {
        }
        /// <summary>
        /// <see cref="DynamicThreadPool"/> constructor
        /// </summary>
        /// <param name="minThreadCount">Minimum number of threads that should always be in pool</param>
        /// <param name="maxThreadCount">Maximum number of threads in pool</param>
        /// <param name="queueBoundedCapacity">The bounded size of the work items queue (if less or equal to 0 then no limitation)</param>
        /// <param name="name">The name for this instance of ThreadPool and for its threads</param>
        public DynamicThreadPool(int minThreadCount, int maxThreadCount, int queueBoundedCapacity, string name)
            : this(DynamicThreadPoolOptions.Default, minThreadCount, maxThreadCount, queueBoundedCapacity, name, false)
        {
        }
        /// <summary>
        /// <see cref="DynamicThreadPool"/> constructor
        /// </summary>
        /// <param name="minThreadCount">Minimum number of threads that should always be in pool</param>
        /// <param name="maxThreadCount">Maximum number of threads in pool</param>
        /// <param name="name">The name for this instance of ThreadPool and for its threads</param>
        public DynamicThreadPool(int minThreadCount, int maxThreadCount, string name)
            : this(DynamicThreadPoolOptions.Default, minThreadCount, maxThreadCount, -1, name, false)
        {
        }
        /// <summary>
        /// <see cref="DynamicThreadPool"/> constructor
        /// </summary>
        /// <param name="maxThreadCount">Maximum number of threads in pool</param>
        /// <param name="name">The name for this instance of ThreadPool and for its threads</param>
        public DynamicThreadPool(int maxThreadCount, string name)
            : this(DynamicThreadPoolOptions.Default, 0, maxThreadCount, -1, name, false)
        {
        }
        /// <summary>
        /// <see cref="DynamicThreadPool"/> constructor
        /// </summary>
        /// <param name="name">The name for this instance of ThreadPool and for its threads</param>
        public DynamicThreadPool(string name)
            : this(DynamicThreadPoolOptions.Default, 0, 2 * Environment.ProcessorCount + 1, -1, name, false)
        {
        }

        /// <summary>
        /// Minimum number of threads that should always be in pool
        /// </summary>
        public int MinThreadCount
        {
            get { return _minThreadCount; }
        }
        /// <summary>
        /// Maximum number of threads in pool
        /// </summary>
        public int MaxThreadCount
        {
            get { return _maxThreadCount; }
        }
        /// <summary>
        /// Number of threads tracked by the current DynamicThreadPool
        /// </summary>
        protected int PrimaryThreadCount
        {
            get { return GetThreadCountFromCombination(Volatile.Read(ref _dieSlotActiveFullThreadCountCombination)); }
        }
        /// <summary>
        /// Number of active threads
        /// </summary>
        public int ActiveThreadCount
        {
            get { return GetActiveThreadCountFromCombination(Volatile.Read(ref _dieSlotActiveFullThreadCountCombination)); }
        }


        /// <summary>
        /// Warms-up pool by adding new threads up to the specified '<paramref name="count"/>'
        /// </summary>
        /// <param name="count">The number of threads that must be present in the pool after the call</param>
        public void FillPoolUpTo(int count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            CheckPendingDisposeOrDisposed();         

            count = Math.Min(count, _maxThreadCount);

            int addCount = count - this.PrimaryThreadCount;
            while (addCount-- > 0 && this.PrimaryThreadCount < count)
                if (!this.AddNewThreadInner(count))
                    break;

            int activateCount = (count - this.ActiveThreadCount);
            while (activateCount-- > 0 && this.ActiveThreadCount < count)
                if (!AddOrActivateThread(count))
                    break;
        }


        #region ====== DieSlotActiveFullThreadCountCombination Work Proc =====

        private const int OneThreadForDieSlotActiveFullThreadCountCombination = 1;
        private const int OneActiveThreadForDieSlotActiveFullThreadCountCombination = (1 << 12);
        private const int OneDieSlotForDieSlotActiveFullThreadCountCombination = (1 << 24);

        /// <summary>
        /// Вытащить число потоков из DieSlotActiveFullThreadCount
        /// </summary>
        /// <param name="val">Значени DieSlotActiveFullThreadCount</param>
        /// <returns>Число потоков</returns>
        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetThreadCountFromCombination(int val)
        {
            return val & ((1 << 12) - 1);
        }
        /// <summary>
        /// Вытащить число запросов на остановку из DieSlotActiveFullThreadCount
        /// </summary>
        /// <param name="val">Значение DieSlotActiveFullThreadCount</param>
        /// <returns>Число запросов на остановку</returns>
        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetActiveThreadCountFromCombination(int val)
        {
            return (val >> 12) & ((1 << 12) - 1);
        }
        /// <summary>
        /// Вытащить число слотов завершения (число потоков, готовящихся к завершению) из DieSlotActiveFullThreadCount
        /// </summary>
        /// <param name="val">Значение DieSlotActiveFullThreadCount</param>
        /// <returns>Число слотов завершения</returns>
        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetDieSlotCountFromCombination(int val)
        {
            return (val >> 24) & ((1 << 8) - 1);
        }
        /// <summary>
        /// Получить число потоков после выполнения всех операций завершения
        /// </summary>
        /// <param name="val">Значение DieSlotActiveFullThreadCount</param>
        /// <returns>Оценочное число потоков</returns>
        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int EvaluateThreadCountFromCombination(int val)
        {
            return GetThreadCountFromCombination(val) - GetDieSlotCountFromCombination(val);
        }
        /// <summary>
        /// Получить число потоков на паузе
        /// </summary>
        /// <param name="val">Значение DieSlotActiveFullThreadCount</param>
        /// <returns>Оценочное число потоков на паузе</returns>
        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int EvaluatePausedThreadCountFromCombination(int val)
        {
            return GetThreadCountFromCombination(val) - GetActiveThreadCountFromCombination(val);
        }

        /// <summary>
        /// Получить число потоков после выполнения всех операций завершения
        /// </summary>
        /// <returns>Оценочное число потоков</returns>
        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int EvaluateThreadCountFromCombination()
        {
            return EvaluateThreadCountFromCombination(Volatile.Read(ref _dieSlotActiveFullThreadCountCombination));
        }
        /// <summary>
        /// Получить число потоков на паузе
        /// </summary>
        /// <returns>Оценочное число потоков на паузе</returns>
        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int EvaluatePausedThreadCountFromCombination()
        {
            return EvaluatePausedThreadCountFromCombination(Volatile.Read(ref _dieSlotActiveFullThreadCountCombination));
        }

        /// <summary>
        /// Увеличить число потоков
        /// </summary>
        /// <param name="newThreadCount">Обновлённое или последнее значение числа потоков</param>
        /// <returns>Успешность</returns>
        private bool IncrementThreadCount(out int newThreadCount)
        {
            Contract.Ensures(EvaluateThreadCountFromCombination() >= 0);
            Contract.Ensures(EvaluatePausedThreadCountFromCombination() >= 0);

            SpinWait sw = new SpinWait();
            var dieSlotActiveFullThreadCountTracker = Volatile.Read(ref _dieSlotActiveFullThreadCountCombination);
            while (GetThreadCountFromCombination(dieSlotActiveFullThreadCountTracker) < _maxThreadCount)
            {
                if (Interlocked.CompareExchange(
                        ref _dieSlotActiveFullThreadCountCombination,
                        dieSlotActiveFullThreadCountTracker + OneThreadForDieSlotActiveFullThreadCountCombination,
                        dieSlotActiveFullThreadCountTracker) == dieSlotActiveFullThreadCountTracker)
                {
                    newThreadCount = GetThreadCountFromCombination(dieSlotActiveFullThreadCountTracker + OneThreadForDieSlotActiveFullThreadCountCombination);
                    return true;
                }

                sw.SpinOnce();
                dieSlotActiveFullThreadCountTracker = Volatile.Read(ref _dieSlotActiveFullThreadCountCombination);
            }

            newThreadCount = GetThreadCountFromCombination(dieSlotActiveFullThreadCountTracker);
            return false;
        }

        /// <summary>
        /// Уменьшить число потоков
        /// </summary>
        /// <param name="minThreadCount">Минимально допустимое число потоков</param>
        /// <returns>Успешность</returns>
        private bool DecremenetThreadCount(int minThreadCount = 0)
        {
            Contract.Requires(minThreadCount >= 0);
            Contract.Ensures(EvaluateThreadCountFromCombination() >= 0);
            Contract.Ensures(EvaluatePausedThreadCountFromCombination() >= 0);

            SpinWait sw = new SpinWait();
            var dieSlotActiveFullThreadCountTracker = Volatile.Read(ref _dieSlotActiveFullThreadCountCombination);
            while (GetThreadCountFromCombination(dieSlotActiveFullThreadCountTracker) > minThreadCount)
            {
                if (Interlocked.CompareExchange(
                        ref _dieSlotActiveFullThreadCountCombination,
                        dieSlotActiveFullThreadCountTracker - OneThreadForDieSlotActiveFullThreadCountCombination,
                        dieSlotActiveFullThreadCountTracker) == dieSlotActiveFullThreadCountTracker)
                    return true;

                sw.SpinOnce();
                dieSlotActiveFullThreadCountTracker = Volatile.Read(ref _dieSlotActiveFullThreadCountCombination);
            }

            return false;
        }

        /// <summary>
        /// Пересчитать новое значени DieSlotActiveFullThreadCount при уменьшении числа потоков
        /// </summary>
        /// <param name="val">Исходное значение DieSlotActiveFullThreadCount</param>
        /// <param name="wasActiveThreadCountDecremented">Было ли уменьшено число активных потоков</param>
        /// <returns>Перерасчитанное значение DieSlotActiveFullThreadCount</returns>
        private int CalculateValueForDecrementThreadCountCascade(int val, out bool wasActiveThreadCountDecremented)
        {
            wasActiveThreadCountDecremented = false;
            if (GetDieSlotCountFromCombination(val) > 0)
                val -= OneDieSlotForDieSlotActiveFullThreadCountCombination;
            if (GetActiveThreadCountFromCombination(val) == GetThreadCountFromCombination(val))
            {
                wasActiveThreadCountDecremented = true;
                val -= OneActiveThreadForDieSlotActiveFullThreadCountCombination;
            }

            return val - OneThreadForDieSlotActiveFullThreadCountCombination;
        }
        /// <summary>
        /// Уменьшить число потоков (также забирает с собой DieSlot и ActiveThreadCount, если нужно)
        /// </summary>
        /// <param name="wasActiveThreadCountDecremented">Было ли уменьшено число активных потоков</param>
        /// <returns>Успешность</returns>
        private bool DecrementThreadCountCascade(out bool wasActiveThreadCountDecremented)
        {
            Contract.Ensures(EvaluateThreadCountFromCombination() >= 0);
            Contract.Ensures(EvaluatePausedThreadCountFromCombination() >= 0);

            SpinWait sw = new SpinWait();
            var dieSlotActiveFullThreadCountTracker = Volatile.Read(ref _dieSlotActiveFullThreadCountCombination);
            while (GetThreadCountFromCombination(dieSlotActiveFullThreadCountTracker) > 0)
            {
                if (Interlocked.CompareExchange(ref _dieSlotActiveFullThreadCountCombination,
                        CalculateValueForDecrementThreadCountCascade(dieSlotActiveFullThreadCountTracker, out wasActiveThreadCountDecremented),
                        dieSlotActiveFullThreadCountTracker) == dieSlotActiveFullThreadCountTracker)
                {
                    return true;
                }

                sw.SpinOnce();
                dieSlotActiveFullThreadCountTracker = Volatile.Read(ref _dieSlotActiveFullThreadCountCombination);
            }

            wasActiveThreadCountDecremented = false;
            return false;
        }

        /// <summary>
        /// Увеличить число активных потоков
        /// </summary>
        /// <returns>Успешность</returns>
        private bool IncrementActiveThreadCount()
        {
            Contract.Ensures(EvaluateThreadCountFromCombination() >= 0);
            Contract.Ensures(EvaluatePausedThreadCountFromCombination() >= 0);

            SpinWait sw = new SpinWait();
            var dieSlotActiveFullThreadCountTracker = Volatile.Read(ref _dieSlotActiveFullThreadCountCombination);
            while (GetActiveThreadCountFromCombination(dieSlotActiveFullThreadCountTracker) < GetThreadCountFromCombination(dieSlotActiveFullThreadCountTracker))
            {
                if (Interlocked.CompareExchange(ref _dieSlotActiveFullThreadCountCombination,
                        dieSlotActiveFullThreadCountTracker + OneActiveThreadForDieSlotActiveFullThreadCountCombination,
                        dieSlotActiveFullThreadCountTracker) == dieSlotActiveFullThreadCountTracker)
                    return true;

                sw.SpinOnce();
                dieSlotActiveFullThreadCountTracker = Volatile.Read(ref _dieSlotActiveFullThreadCountCombination);
            }

            return false;
        }

        /// <summary>
        /// Уменьшить число активных потоков
        /// </summary>
        /// <param name="activeThreadCountLowLimit">Минимальное число активных потоков</param>
        /// <returns>Успешность</returns>
        private bool DecrementActiveThreadCount(int activeThreadCountLowLimit = 0)
        {
            Contract.Requires(activeThreadCountLowLimit >= 0);
            Contract.Ensures(EvaluateThreadCountFromCombination() >= 0);
            Contract.Ensures(EvaluatePausedThreadCountFromCombination() >= 0);

            SpinWait sw = new SpinWait();
            var dieSlotActiveFullThreadCountTracker = Volatile.Read(ref _dieSlotActiveFullThreadCountCombination);
            while (GetActiveThreadCountFromCombination(dieSlotActiveFullThreadCountTracker) > activeThreadCountLowLimit)
            {
                if (Interlocked.CompareExchange(ref _dieSlotActiveFullThreadCountCombination,
                        dieSlotActiveFullThreadCountTracker - OneActiveThreadForDieSlotActiveFullThreadCountCombination,
                        dieSlotActiveFullThreadCountTracker) == dieSlotActiveFullThreadCountTracker)
                    return true;

                sw.SpinOnce();
                dieSlotActiveFullThreadCountTracker = Volatile.Read(ref _dieSlotActiveFullThreadCountCombination);
            }

            return false;
        }


        /// <summary>
        /// Запросить начало завершения потока (также выдёргивает StopRequest, если он есть)
        /// </summary>
        /// <param name="threadCountLowLimit">Минимальное число потоков</param>
        /// <param name="curThreadCountMax">Уменьшение делается только если число потоков меньше данного значения</param>
        /// <returns>Успешность</returns>
        private bool RequestDieSlot(int threadCountLowLimit, int curThreadCountMax = int.MaxValue)
        {
            Contract.Requires(threadCountLowLimit >= 0);
            Contract.Requires(curThreadCountMax >= 0);
            Contract.Ensures(EvaluateThreadCountFromCombination() >= 0);
            Contract.Ensures(EvaluatePausedThreadCountFromCombination() >= 0);

            SpinWait sw = new SpinWait();
            var dieSlotActiveFullThreadCountTracker = Volatile.Read(ref _dieSlotActiveFullThreadCountCombination);
            while (EvaluateThreadCountFromCombination(dieSlotActiveFullThreadCountTracker) > threadCountLowLimit &&
                   EvaluateThreadCountFromCombination(dieSlotActiveFullThreadCountTracker) <= curThreadCountMax &&
                   GetDieSlotCountFromCombination(dieSlotActiveFullThreadCountTracker) < 255)
            {
                if (Interlocked.CompareExchange(ref _dieSlotActiveFullThreadCountCombination,
                        dieSlotActiveFullThreadCountTracker + OneDieSlotForDieSlotActiveFullThreadCountCombination,
                        dieSlotActiveFullThreadCountTracker) == dieSlotActiveFullThreadCountTracker)
                    return true;

                sw.SpinOnce();
                dieSlotActiveFullThreadCountTracker = Volatile.Read(ref _dieSlotActiveFullThreadCountCombination);
            }

            return false;
        }


        #endregion



        /// <summary>
        /// Активировать поток
        /// </summary>
        /// <returns>Успешность</returns>
        private bool ActivateThread()
        {
            if (EvaluatePausedThreadCountFromCombination(Volatile.Read(ref _dieSlotActiveFullThreadCountCombination)) == 0)
                return false;

            bool wasThreadActivated = false;
            lock (_syncObject)
            {
                try { }
                finally
                {
                    if (IncrementActiveThreadCount())
                    {
                        _extThreadBlocker.SubstractExpectedWaiterCount(1);
                        //Console.WriteLine("Thread activated = " + ActiveThreadCount.ToString());
                        wasThreadActivated = true;
                    }
                }
            }
            return wasThreadActivated;
        }

        /// <summary>
        /// Деактивирует поток
        /// </summary>
        /// <param name="threadCountLowLimit">Минимальное число активных потоков</param>
        /// <returns>Успешность</returns>
        private bool DeactivateThread(int threadCountLowLimit)
        {
            if (GetActiveThreadCountFromCombination(Volatile.Read(ref _dieSlotActiveFullThreadCountCombination)) <= threadCountLowLimit)
                return false;

            bool result = false;
            lock (_syncObject)
            {
                try { }
                finally
                {
                    if (DecrementActiveThreadCount(threadCountLowLimit))
                    {
                        _extThreadBlocker.AddExpectedWaiterCount(1);
                        //Console.WriteLine("Thread deactivated = " + ActiveThreadCount.ToString());
                        result = true;
                    }
                }
            }
            return result;
        }


        /// <summary>
        /// Вспомогательный метод добавления нового потока в статический пул
        /// </summary>
        /// <param name="threadCountLimit">Дополнительное ограничение на число потоков</param>
        /// <returns>Инициирован ли поток</returns>
        private bool AddNewThreadInner(int threadCountLimit)
        {
            if (State == ThreadPoolState.Stopped || (State == ThreadPoolState.StopRequested && !LetFinishedProcess) ||
                this.PrimaryThreadCount >= Math.Min(_maxThreadCount, threadCountLimit))
            {
                return false;
            }

            lock (_syncObject)
            {
                if (State == ThreadPoolState.Stopped || (State == ThreadPoolState.StopRequested && !LetFinishedProcess) ||
                    this.PrimaryThreadCount >= Math.Min(_maxThreadCount, threadCountLimit))
                {
                    return false;
                }

                //Console.WriteLine("Thread spawn = " + (this.PrimaryThreadCount + 1).ToString());

                bool result = false;
                int threadCountBefore = base.ThreadCount;
                try
                {
                    int trackingThreadCount = 0;
                    bool incrementThreadCountSuccess = IncrementThreadCount(out trackingThreadCount);
                    Debug.Assert(incrementThreadCountSuccess, "Error. Thread count was not incremented");

                    result = AddNewThread(UniversalThreadProc) != null;
                }
                finally
                {
                    if (result || base.ThreadCount == threadCountBefore + 1)
                    {
                        IncrementActiveThreadCount();
                        //Console.WriteLine("Thread activated = " + this.ActiveThreadCount.ToString());
                    }
                    else
                    {
                        bool decrementThreadCountSuccess = DecremenetThreadCount();
                        Debug.Assert(decrementThreadCountSuccess, "Error. Thread count was not decremented");
                    }
                }

                return result;
            }
        }

        /// <summary>
        /// Активировать поток, либо создать новый
        /// </summary>
        /// <param name="threadCountLimit">Дополнительное ограничение на число потоков</param>
        /// <returns>Успешность</returns>
        private bool AddOrActivateThread(int threadCountLimit)
        {
            if (ActivateThread())
                return true;

            return AddNewThreadInner(threadCountLimit);
        }



        /// <summary>
        /// Хэндлер удаления потока из пула
        /// </summary>
        /// <param name="elem">Удаляемый элемент</param>
        protected override void OnThreadRemove(Thread elem)
        {
            base.OnThreadRemove(elem);

            lock (_syncObject)
            {
                try { }
                finally
                {
                    bool wasActiveThreadCountDecremented = false;
                    bool decrementThreadCountSuccess = DecrementThreadCountCascade(out wasActiveThreadCountDecremented);
                    Debug.Assert(decrementThreadCountSuccess, "Error. Thread count was not decremented.");
                    // Если не было уменьшения активных, то заблокируем лишний => уменьшае число потоков для блокирования
                    if (!wasActiveThreadCountDecremented)
                    {
                        _extThreadBlocker.SubstractExpectedWaiterCount(1);
                    }
                    else
                    {
                        //Console.WriteLine("Thread deactivated = " + this.ActiveThreadCount.ToString());
                    }
                }

                //Console.WriteLine("Thread exit: " + (this.PrimaryThreadCount + 1).ToString());
            }
        }


        /// <summary>
        /// Метод обработки задач
        /// </summary>
        /// <param name="privateData">Данные потока</param>
        /// <param name="token">Токен отмены (при остановке пула)</param>
        [System.Diagnostics.DebuggerNonUserCode]
        private void UniversalThreadProc(ThreadPrivateData privateData, CancellationToken token)
        {
            if (privateData == null)
                throw new InvalidOperationException("privateData for Thread of ThreadPool can't be null");


            ThreadPoolWorkItem currentWorkItem = null;
            int lastViewedActiveThreadCount = 0;

            try
            {
                while (!token.IsCancellationRequested)
                {
                    if (!_extThreadBlocker.Wait(_noWorkItemTrimPeriod, token))
                    {
                        if (RequestDieSlot(_minThreadCount))
                        {
                            //Console.WriteLine("Thread exit due to staying deactivated");
                            break;
                        }
                        // Иначе активируемся
                        ActivateThread();
                    }

                    bool itemTaken = this.TryTakeWorkItemFromQueue(privateData, out currentWorkItem, 0, new CancellationToken(), false);
                    if (itemTaken == false)
                    {
                        lastViewedActiveThreadCount = this.ActiveThreadCount;
                        // this.ActiveThreadCount <= _reasonableThreadCount - возможна гонка, но нам не критично
                        if (lastViewedActiveThreadCount <= _reasonableThreadCount)
                            itemTaken = this.TryTakeWorkItemFromQueue(privateData, out currentWorkItem, _noWorkItemTrimPeriod, token, false);
                        else
                            itemTaken = this.TryTakeWorkItemFromQueue(privateData, out currentWorkItem, NoWorkItemPreventDeactivationPeriod, token, false);
                    }

                    if (itemTaken)
                    {
                        this.RunWorkItem(currentWorkItem);
                        currentWorkItem = null;

                        _throughoutTracker.RegisterExecution();
                        if (_wasSomeProcessByThreadsFlag == false)
                            _wasSomeProcessByThreadsFlag = true;
                    }
                    else if (!token.IsCancellationRequested)
                    {
                        if (lastViewedActiveThreadCount <= _reasonableThreadCount)
                        {
                            if (this.PrimaryThreadCount > _fastSpawnThreadCountLimit)
                                DeactivateThread(_fastSpawnThreadCountLimit);
                            else
                                DeactivateThread(_minThreadCount);
                        }
                        else
                        {
                            DeactivateThread(_reasonableThreadCount);
                        }

                        //Console.WriteLine("Thread self deactivation due to empty queue");
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
        /// Создаём новые потоки при необходимости.
        /// Отслеживаем ситуацию, когда пул завис и пытаемся решить вопрос расширением вместимости очереди и созданием потоков.
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


            if (elapsedMs < _managementProcessPeriod)
                return false;


            bool wasThreadSpawned = false;
            bool isCriticalCondition = false;
            bool needThreadCountAdjustment = false;
            int activeThreadCountBefore = this.ActiveThreadCount;

            // Защитная функция, когда все потоки внезапно деактивировались
            if (this.ActiveThreadCount == 0 && this.GlobalQueueWorkItemCount > 0)
                wasThreadSpawned = AddOrActivateThread(1);

            // Создаём поток в нормальном сценарии
            if (!wasThreadSpawned && this.ActiveThreadCount < _reasonableThreadCount)
            {
                if (this.GlobalQueueWorkItemCount > WorkItemPerThreadLimit * this.PrimaryThreadCount)
                    wasThreadSpawned = AddOrActivateThread(_reasonableThreadCount);
                else if (this.QueueCapacity > 0 && this.GlobalQueueWorkItemCount >= this.QueueCapacity)
                    wasThreadSpawned = AddOrActivateThread(_reasonableThreadCount);
            }

            // Пробуем расширить очередь, если прижало (проще, чем создание новых потоков)
            if (this.QueueCapacity > 0 && this.PrimaryThreadCount > 0)
                if (!_wasSomeProcessByThreadsFlag)
                    if (this.GlobalQueueWorkItemCount >= this.ExtendedQueueCapacity)
                        if (this.ExtendedQueueCapacity - this.QueueCapacity < _maxQueueCapacityExtension)
                            this.ExtendGlobalQueueCapacity(this.PrimaryThreadCount + 1);



            // Проверяем критический сценарий (много потоков просто встало)
            if (!DisableCritical)
            {
                if (!wasThreadSpawned && this.ActiveThreadCount <= _maxThreadCount && this.PrimaryThreadCount >= _reasonableThreadCount)
                {
                    if (this.GlobalQueueWorkItemCount > WorkItemPerThreadLimit * this.ActiveThreadCount || (this.QueueCapacity > 0 && this.GlobalQueueWorkItemCount >= this.QueueCapacity))
                    {
                        int totalThreadCount, runningCount, waitingCount;
                        this.ScanThreadStates(out totalThreadCount, out runningCount, out waitingCount);
                        if (runningCount <= 1 || (!_wasSomeProcessByThreadsFlag && runningCount < _reasonableThreadCount))
                        {
                            //Console.WriteLine("Critical spawn");
                            wasThreadSpawned = AddOrActivateThread(_maxThreadCount);
                            if (runningCount == 0 && _reasonableThreadCount >= 2)
                                wasThreadSpawned = AddOrActivateThread(_maxThreadCount) || wasThreadSpawned;
                            isCriticalCondition = true;
                        }
                    }
                }
            }

            // Проверяем критический сценарий (потоки работают, но возможно изменение их числа повлияет на производительность)
            if (this.MaxThreadCount > this.MinThreadCount + 1)
                if (this.ActiveThreadCount <= _maxThreadCount && this.PrimaryThreadCount >= _reasonableThreadCount)
                    if (this.GlobalQueueWorkItemCount > WorkItemPerThreadLimit * this.ActiveThreadCount || (this.QueueCapacity > 0 && this.GlobalQueueWorkItemCount >= this.QueueCapacity))
                        needThreadCountAdjustment = true;


            int threadCountChange = _throughoutTracker.RegisterAndMakeSuggestion(activeThreadCountBefore, needThreadCountAdjustment, isCriticalCondition);
            if (threadCountChange > 0)
            {
                for (int i = 0; i < threadCountChange; i++)
                    wasThreadSpawned = AddOrActivateThread(_maxThreadCount);
            }
            else if (threadCountChange < 0)
            {
                for (int i = 0; i < -threadCountChange; i++)
                    DeactivateThread(_reasonableThreadCount);
            }
            

            _wasSomeProcessByThreadsFlag = false;

            return true;
        }




        /// <summary>
        /// Запрос быстрого создания потока при добавлении элементов в пул
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void TryRequestNewThreadOnAdd()
        {
            int threadCount = this.ActiveThreadCount;
            if (threadCount >= _fastSpawnThreadCountLimit || threadCount >= this.GlobalQueueWorkItemCount + 2)
                return;

            this.AddOrActivateThread(_fastSpawnThreadCountLimit);
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

            TryRequestNewThreadOnAdd();
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

            bool result = false;

            if (QueueCapacity <= 0 || GlobalQueueWorkItemCount < QueueCapacity)
            {
                this.PrepareWorkItem(item);
                result = this.TryAddWorkItemToQueue(item);
            }

            TryRequestNewThreadOnAdd();
            return result;
        }



        /// <summary>
        /// Finalizer
        /// </summary>
        ~DynamicThreadPool()
        {
            Dispose(false);
        }
    }
}
