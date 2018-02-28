using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Profiling
{
    /// <summary>
    /// Interface to profile Turbo classes
    /// </summary>
    public interface IProfilingProvider
    {
        /// <summary>
        /// Notifies about creation of Object Pool
        /// </summary>
        /// <param name="poolName">Name of the created Object Pool</param>
        void ObjectPoolCreated(string poolName);
        /// <summary>
        /// Notifies about destruction of Object Pool
        /// </summary>
        /// <param name="poolName">Name of the Object Pool</param>
        /// <param name="fromFinalizer">Is destruction called from finalizer</param>
        void ObjectPoolDisposed(string poolName, bool fromFinalizer);
        /// <summary>
        /// Notifies about renting an element from Object Pool
        /// </summary>
        /// <param name="poolName">Name of the Object Pool</param>
        /// <param name="currentRentedCount">Number of rented element</param>
        void ObjectPoolElementRented(string poolName, int currentRentedCount);
        /// <summary>
        /// Notifies about releasing an element back to the Object Pool
        /// </summary>
        /// <param name="poolName">Name of the Object Pool</param>
        /// <param name="currentRentedCount">Number of rented element</param>
        void ObjectPoolElementReleased(string poolName, int currentRentedCount);
        /// <summary>
        /// Notifies about faulting of element (element has changed its state and cannot be used anymore)
        /// </summary>
        /// <param name="poolName">Name of the Object Pool</param>
        /// <param name="currentElementCount">Number of elements stored in Object Pool</param>
        void ObjectPoolElementFaulted(string poolName, int currentElementCount);
        /// <summary>
        /// Notifies about adding new element to the Object Pool
        /// </summary>
        /// <param name="poolName">Name of the Object Pool</param>
        /// <param name="currentElementCount">Number of elements stored in Object Pool</param>
        void ObjectPoolElementCreated(string poolName, int currentElementCount);
        /// <summary>
        /// Notifies about destruction of the element inside Object Pool
        /// </summary>
        /// <param name="poolName">Name of the Object Pool</param>
        /// <param name="currentElementCount">Number of elements stored in Object Pool</param>
        void ObjectPoolElementDestroyed(string poolName, int currentElementCount);
        /// <summary>
        /// Measures the time element was rented
        /// </summary>
        /// <param name="poolName">Name of the Object Pool</param>
        /// <param name="time">Period of time when element was rented</param>
        void ObjectPoolElementRentedTime(string poolName, TimeSpan time);




        /// <summary>
        /// Notifies about QueueAsyncProcessor creation
        /// </summary>
        /// <param name="queueProcName">Name of created QueueAsyncProcessor</param>
        void QueueAsyncProcessorCreated(string queueProcName);
        /// <summary>
        /// Notifies about stopping of QueueAsyncProcessor
        /// </summary>
        /// <param name="queueProcName">Name of QueueAsyncProcessor</param>
        /// <param name="fromFinalizer">Is stopped from finalizer</param>
        void QueueAsyncProcessorDisposed(string queueProcName, bool fromFinalizer);
        /// <summary>
        /// Notifies about starting of thread inside QueueAsyncProcessor
        /// </summary>
        /// <param name="queueProcName">Name of QueueAsyncProcessor</param>
        /// <param name="curThreadCount">Current number of active threads</param>
        /// <param name="expectedThreadCount">Expected total number of threads</param>
        void QueueAsyncProcessorThreadStart(string queueProcName, int curThreadCount, int expectedThreadCount);
        /// <summary>
        /// Notifies about stopping of thread inside QueueAsyncProcessor (due to normal stopping procedure or due to unhandled exception)
        /// </summary>
        /// <param name="queueProcName">Name of QueueAsyncProcessor</param>
        /// <param name="curThreadCount">Current number of active threads</param>
        /// <param name="expectedThreadCount">Expected total number of threads</param>
        void QueueAsyncProcessorThreadStop(string queueProcName, int curThreadCount, int expectedThreadCount);
        /// <summary>
        /// Measures the time of single element processing
        /// </summary>
        /// <param name="queueProcName">Name of QueueAsyncProcessor</param>
        /// <param name="time">Processing period</param>
        void QueueAsyncProcessorElementProcessed(string queueProcName, TimeSpan time);
        /// <summary>
        /// Notifies about adding new element to the queue of QueueAsyncProcessor
        /// </summary>
        /// <param name="queueProcName">Name of QueueAsyncProcessor</param>
        /// <param name="newElementCount">Number of elements inside queue</param>
        /// <param name="maxElementCount">Maximum number of elements inside queue (negative means unbounded)</param>
        void QueueAsyncProcessorElementCountIncreased(string queueProcName, int newElementCount, int maxElementCount);
        /// <summary>
        /// Notifies about taking element from the queue of QueueAsyncProcessor
        /// </summary>
        /// <param name="queueProcName">Name of QueueAsyncProcessor</param>
        /// <param name="newElementCount">Number of elements inside queue</param>
        /// <param name="maxElementCount">Maximum number of elements inside queue (negative means unbounded)</param>
        void QueueAsyncProcessorElementCountDecreased(string queueProcName, int newElementCount, int maxElementCount);
        /// <summary>
        /// Notifies that element was not added to the queue by <see cref="Qoollo.Turbo.Threading.QueueProcessing.QueueAsyncProcessorBase{T}.TryAdd(T)"/> 
        /// </summary>
        /// <param name="queueProcName">Name of QueueAsyncProcessor</param>
        /// <param name="currentElementCount">Number of elements inside queue</param>
        void QueueAsyncProcessorElementRejectedInTryAdd(string queueProcName, int currentElementCount);



        /// <summary>
        /// Notifies about creation of ThreadPool
        /// </summary>
        /// <param name="threadPoolName">Name of the created Thread Pool</param>
        void ThreadPoolCreated(string threadPoolName);
        /// <summary>
        /// Notifies about stopping of ThreadPool
        /// </summary>
        /// <param name="threadPoolName">Name of the Thread Pool</param>
        /// <param name="fromFinalizer">Is stoping from finalizer</param>
        void ThreadPoolDisposed(string threadPoolName, bool fromFinalizer);
        /// <summary>
        /// Notifies about changing of number of threads inside ThreadPool
        /// </summary>
        /// <param name="threadPoolName">Name of the Thread Pool</param>
        /// <param name="curThreadCount">Current number of threads inside ThreadPool</param>
        void ThreadPoolThreadCountChange(string threadPoolName, int curThreadCount);
        /// <summary>
        /// Measures the time when work item was waiting inside queue
        /// </summary>
        /// <param name="threadPoolName">Name of the Thread Pool</param>
        /// <param name="time">Period of waiting</param>
        void ThreadPoolWaitingInQueueTime(string threadPoolName, TimeSpan time);
        /// <summary>
        /// Measures the exection time of work item inside ThreadPool
        /// </summary>
        /// <param name="threadPoolName">Name of the Thread Pool</param>
        /// <param name="time">Period of execution</param>
        void ThreadPoolWorkProcessed(string threadPoolName, TimeSpan time);
        /// <summary>
        /// Notifies about cancellation of work item
        /// </summary>
        /// <param name="threadPoolName">Name of the Thread Pool</param>
        void ThreadPoolWorkCancelled(string threadPoolName);
        /// <summary>
        /// Notifies about adding new work item to the Thread Pool queue
        /// </summary>
        /// <param name="threadPoolName">Name of the Thread Pool</param>
        /// <param name="globalQueueItemCount">Current number of work items inside queue</param>
        /// <param name="maxItemCount">Maximum number of work items inside queue</param>
        void ThreadPoolWorkItemsCountIncreased(string threadPoolName, int globalQueueItemCount, int maxItemCount);
        /// <summary>
        /// Изменилось число задач в очереди на обработку в пуле потоков
        /// </summary>
        /// <param name="threadPoolName">Name of the Thread Pool</param>
        /// <param name="globalQueueItemCount">Current number of work items inside queue</param>
        /// <param name="maxItemCount">Maximum number of work items inside queue</param>
        void ThreadPoolWorkItemsCountDecreased(string threadPoolName, int globalQueueItemCount, int maxItemCount);
        /// <summary>
        /// Notifies that work item was not enqueued by <see cref="Qoollo.Turbo.Threading.ThreadPools.ThreadPoolBase.TryRun(Action)"/> 
        /// </summary>
        /// <param name="threadPoolName">Name of the Thread Pool</param>
        void ThreadPoolWorkItemRejectedInTryAdd(string threadPoolName);



        /// <summary>
        /// Notifies about creation of ThreadSetManager
        /// </summary>
        /// <param name="threadSetManagerName">Name of the created ThreadSetManager</param>
        /// <param name="initialThreadCount">Number of managed threads</param>
        void ThreadSetManagerCreated(string threadSetManagerName, int initialThreadCount);
        /// <summary>
        /// Notifies about stopping of ThreadSetManager
        /// </summary>
        /// <param name="threadSetManagerName">Name of the ThreadSetManager</param>
        /// <param name="fromFinalizer">Is stopping from finalizer</param>
        void ThreadSetManagerDisposed(string threadSetManagerName, bool fromFinalizer);
        /// <summary>
        /// Notifies about thread starting inside ThreadSetManager
        /// </summary>
        /// <param name="threadSetManagerName">Name of the ThreadSetManager</param>
        /// <param name="curThreadCount">Current number of threads inside ThreadSetManager</param>
        /// <param name="expectedThreadCount">Expected number of threads</param>
        void ThreadSetManagerThreadStart(string threadSetManagerName, int curThreadCount, int expectedThreadCount);
        /// <summary>
        /// Notifies about stopping of thread inside ThreadSetManager
        /// </summary>
        /// <param name="threadSetManagerName">Name of the ThreadSetManager</param>
        /// <param name="curThreadCount">Current number of threads inside ThreadSetManager</param>
        /// <param name="expectedThreadCount">Expected number of threads</param>
        void ThreadSetManagerThreadStop(string threadSetManagerName, int curThreadCount, int expectedThreadCount);
    }
}
