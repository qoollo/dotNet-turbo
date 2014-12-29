using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Threading.Tasks
{
    /// <summary>
    /// Планировщик задач с лимитом одновременных запросов
    /// </summary>
    public class LimitedConcurrencyLevelTaskScheduler : TaskScheduler
    {
        /// <summary>Whether the current thread is processing work items.</summary>
        [ThreadStatic]
        private static bool _currentThreadIsProcessingItems;
        /// <summary>The list of tasks to be executed.</summary>
        private readonly LinkedList<Task> _tasks = new LinkedList<Task>(); // protected by lock(_tasks)
        /// <summary>The maximum concurrency level allowed by this scheduler.</summary>
        private readonly int _maxDegreeOfParallelism;
        /// <summary>Whether the scheduler is currently processing work items.</summary>
        private int _delegatesQueuedOrRunning = 0; // protected by lock(_tasks)

        /// <summary>
        /// Конструктор LimitedConcurrencyLevelTaskScheduler
        /// </summary>
        /// <param name="maxDegreeOfParallelism">Максимальный уровень параллелизма</param>
        public LimitedConcurrencyLevelTaskScheduler(int maxDegreeOfParallelism)
        {
            if (maxDegreeOfParallelism < 1) 
                throw new ArgumentOutOfRangeException("maxDegreeOfParallelism");

            _maxDegreeOfParallelism = maxDegreeOfParallelism;
        }

        /// <summary>
        /// Конструктор LimitedConcurrencyLevelTaskScheduler
        /// </summary>
        public LimitedConcurrencyLevelTaskScheduler()
            : this(Environment.ProcessorCount * 2)
        {
        }


        /// <summary>Максимальная степень параллелизма</summary>
        public sealed override int MaximumConcurrencyLevel { get { return _maxDegreeOfParallelism; } }


        /// <summary>Заносит Task в планировщик</summary>
        /// <param name="task">Новый Task</param>
        protected sealed override void QueueTask(Task task)
        {
            lock (_tasks)
            {
                _tasks.AddLast(task);
                if (_delegatesQueuedOrRunning < _maxDegreeOfParallelism)
                {
                    _delegatesQueuedOrRunning++;
                    NotifyThreadPoolOfPendingWork();
                }
            }
        }

        /// <summary>
        /// Запускаем Task из очереди
        /// </summary>
        private void NotifyThreadPoolOfPendingWork()
        {
            ThreadPool.UnsafeQueueUserWorkItem((state) =>
            {
                // Note that the current thread is now processing work items.
                // This is necessary to enable inlining of tasks into this thread.
                _currentThreadIsProcessingItems = true;
                try
                {
                    // Process all available items in the queue.
                    while (true)
                    {
                        Task item;
                        lock (_tasks)
                        {
                            // When there are no more items to be processed,
                            // note that we're done processing, and get out.
                            if (_tasks.Count == 0)
                            {
                                _delegatesQueuedOrRunning--;
                                break;
                            }

                            // Get the next item from the queue
                            item = _tasks.First.Value;
                            _tasks.RemoveFirst();
                        }

                        // Execute the task we pulled out of the queue
                        base.TryExecuteTask(item);
                    }
                }
                // We're done processing items on the current thread
                finally
                {
                    _currentThreadIsProcessingItems = false;
                }
            }, null);
        }


        /// <summary>Попробовать запустить Task в текущем потоке</summary>
        /// <param name="task">Task для исполнения</param>
        /// <param name="taskWasPreviouslyQueued">Task был в очереди</param>
        /// <returns>Может ли он быть запущен в текущем потоке</returns>
        protected sealed override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            // If this thread isn't already processing a task, we don't support inlining
            if (!_currentThreadIsProcessingItems) 
                return false;

            // If the task was previously queued, remove it from the queue
            if (taskWasPreviouslyQueued) 
                TryDequeue(task);

            // Try to run the task.
            return base.TryExecuteTask(task);
        }

        /// <summary>Удаление Task из очереди</summary>
        /// <param name="task">Task для удаления</param>
        /// <returns>Был ли удалён</returns>
        protected sealed override bool TryDequeue(Task task)
        {
            lock (_tasks)
            {
                return _tasks.Remove(task);
            }
        }

        /// <summary>Перечисление всех Task'ов в очереди</summary>
        /// <returns>Перечисление</returns>
        protected sealed override IEnumerable<Task> GetScheduledTasks()
        {
            bool lockTaken = false;
            try
            {
                Monitor.TryEnter(_tasks, ref lockTaken);
                if (lockTaken) 
                    return _tasks.ToArray();
                else 
                    throw new NotSupportedException();
            }
            finally
            {
                if (lockTaken) 
                    Monitor.Exit(_tasks);
            }
        }
    }
}
