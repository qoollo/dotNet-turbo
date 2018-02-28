using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Threading.Tasks
{
    /// <summary>
    /// Task scheduler with the limited concurrency level (limit the number of simultaniously executing tasks)
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
        /// LimitedConcurrencyLevelTaskScheduler constructor
        /// </summary>
        /// <param name="maxDegreeOfParallelism">Maxumum degree of parallelism (maximum number of simultaniously executing tasks)</param>
        public LimitedConcurrencyLevelTaskScheduler(int maxDegreeOfParallelism)
        {
            if (maxDegreeOfParallelism < 1) 
                throw new ArgumentOutOfRangeException(nameof(maxDegreeOfParallelism));

            _maxDegreeOfParallelism = maxDegreeOfParallelism;
        }

        /// <summary>
        /// LimitedConcurrencyLevelTaskScheduler constructor (limits the number of simultaniously executing tasks by 2 * Environment.ProcessorCount)
        /// </summary>
        public LimitedConcurrencyLevelTaskScheduler()
            : this(Environment.ProcessorCount * 2)
        {
        }


        /// <summary>
        /// Maxumum degree of parallelism
        /// </summary>
        public sealed override int MaximumConcurrencyLevel { get { return _maxDegreeOfParallelism; } }


        /// <summary>
        /// Adds task to the scheduller queue
        /// </summary>
        /// <param name="task">Task</param>
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
        /// Uses new thread from ThreadPool to execute tasks from queue
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


        /// <summary>
        /// Attempts to exectue Task synchronously in the current thread
        /// </summary>
        /// <param name="task">Task to be executed</param>
        /// <param name="taskWasPreviouslyQueued">Task was taken from queue</param>
        /// <returns>True if the task can be executed synchronously in the current thread</returns>
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

        /// <summary>
        /// Dequeus task from scheduler queue
        /// </summary>
        /// <param name="task">Task that should be removed from queue</param>
        /// <returns>True when task was removed</returns>
        protected sealed override bool TryDequeue(Task task)
        {
            lock (_tasks)
            {
                return _tasks.Remove(task);
            }
        }

        /// <summary>
        /// Enumerates Tasks inside queue
        /// </summary>
        /// <returns>Tasks collection</returns>
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
