using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Profiling
{
    /// <summary>
    /// Wrapper for IProfilingProvider that catches all exception
    /// </summary>
    public class ProfilingProviderWrapper : IProfilingProvider
    {
        private readonly IProfilingProvider _wrappedProvider;

        /// <summary>
        /// ProfilingProviderWrapper constructor
        /// </summary>
        /// <param name="wrappedProvider">Profiler to be wrapped</param>
        public ProfilingProviderWrapper(IProfilingProvider wrappedProvider)
        {
            if (wrappedProvider == null)
                throw new ArgumentNullException("wrappedProvider");

            _wrappedProvider = wrappedProvider;
        }
        /// <summary>
        /// Exception handling procedure
        /// </summary>
        /// <param name="ex">Exception (can be null)</param>
        protected virtual void ProcessException(Exception ex)
        {
            if (ex != null)
                Environment.FailFast("Qoollo.Turbo profiler throws unexpected exception." + Environment.NewLine + "Exception details: " + ex.ToString(), ex);
            else
                Environment.FailFast("Qoollo.Turbo profiler throws unexpected exception");
        }



        /// <summary>
        /// Notifies about creation of Object Pool
        /// </summary>
        /// <param name="poolName">Name of the created Object Pool</param>
        public void ObjectPoolCreated(string poolName)
        {
            try { _wrappedProvider.ObjectPoolCreated(poolName); }
            catch (Exception ex) { ProcessException(ex); }
        }
        /// <summary>
        /// Notifies about destruction of Object Pool
        /// </summary>
        /// <param name="poolName">Name of the Object Pool</param>
        /// <param name="fromFinalizer">Is destruction called from finalizer</param>
        public void ObjectPoolDisposed(string poolName, bool fromFinalizer)
        {
            try { _wrappedProvider.ObjectPoolDisposed(poolName, fromFinalizer); }
            catch (Exception ex) { ProcessException(ex); }
        }
        /// <summary>
        /// Notifies about renting an element from Object Pool
        /// </summary>
        /// <param name="poolName">Name of the Object Pool</param>
        /// <param name="currentRentedCount">Number of rented element</param>
        public void ObjectPoolElementRented(string poolName, int currentRentedCount)
        {
            try { _wrappedProvider.ObjectPoolElementRented(poolName, currentRentedCount); }
            catch (Exception ex) { ProcessException(ex); }
        }
        /// <summary>
        /// Notifies about releasing an element back to the Object Pool
        /// </summary>
        /// <param name="poolName">Name of the Object Pool</param>
        /// <param name="currentRentedCount">Number of rented element</param>
        public void ObjectPoolElementReleased(string poolName, int currentRentedCount)
        {
            try { _wrappedProvider.ObjectPoolElementReleased(poolName, currentRentedCount); }
            catch (Exception ex) { ProcessException(ex); }
        }
        /// <summary>
        /// Notifies about faulting of element (element has changed its state and cannot be used anymore)
        /// </summary>
        /// <param name="poolName">Name of the Object Pool</param>
        /// <param name="currentElementCount">Number of elements stored in Object Pool</param>
        public void ObjectPoolElementFaulted(string poolName, int currentElementCount)
        {
            try { _wrappedProvider.ObjectPoolElementFaulted(poolName, currentElementCount); }
            catch (Exception ex) { ProcessException(ex); }
        }
        /// <summary>
        /// Notifies about adding new element to the Object Pool
        /// </summary>
        /// <param name="poolName">Name of the Object Pool</param>
        /// <param name="currentElementCount">Number of elements stored in Object Pool</param>
        public void ObjectPoolElementCreated(string poolName, int currentElementCount)
        {
            try { _wrappedProvider.ObjectPoolElementCreated(poolName, currentElementCount); }
            catch (Exception ex) { ProcessException(ex); }
        }
        /// <summary>
        /// Notifies about destruction of the element inside Object Pool
        /// </summary>
        /// <param name="poolName">Name of the Object Pool</param>
        /// <param name="currentElementCount">Number of elements stored in Object Pool</param>
        public void ObjectPoolElementDestroyed(string poolName, int currentElementCount)
        {
            try { _wrappedProvider.ObjectPoolElementDestroyed(poolName, currentElementCount); }
            catch (Exception ex) { ProcessException(ex); }
        }
        /// <summary>
        /// Measures the time element was rented
        /// </summary>
        /// <param name="poolName">Name of the Object Pool</param>
        /// <param name="time">Period of time when element was rented</param>
        public void ObjectPoolElementRentedTime(string poolName, TimeSpan time)
        {
            try { _wrappedProvider.ObjectPoolElementRentedTime(poolName, time); }
            catch (Exception ex) { ProcessException(ex); }
        }



        /// <summary>
        /// Notifies about QueueAsyncProcessor creation
        /// </summary>
        /// <param name="queueProcName">Name of created QueueAsyncProcessor</param>
        public void QueueAsyncProcessorCreated(string queueProcName)
        {
            try { _wrappedProvider.QueueAsyncProcessorCreated(queueProcName); }
            catch (Exception ex) { ProcessException(ex); }
        }
        /// <summary>
        /// Notifies about stopping of QueueAsyncProcessor
        /// </summary>
        /// <param name="queueProcName">Name of QueueAsyncProcessor</param>
        /// <param name="fromFinalizer">Is stopped from finalizer</param>
        public void QueueAsyncProcessorDisposed(string queueProcName, bool fromFinalizer)
        {
            try { _wrappedProvider.QueueAsyncProcessorDisposed(queueProcName, fromFinalizer); }
            catch (Exception ex) { ProcessException(ex); }
        }
        /// <summary>
        /// Notifies about starting of thread inside QueueAsyncProcessor
        /// </summary>
        /// <param name="queueProcName">Name of QueueAsyncProcessor</param>
        /// <param name="curThreadCount">Current number of active threads</param>
        /// <param name="expectedThreadCount">Expected total number of threads</param>
        public void QueueAsyncProcessorThreadStart(string queueProcName, int curThreadCount, int expectedThreadCount)
        {
            try { _wrappedProvider.QueueAsyncProcessorThreadStart(queueProcName, curThreadCount, expectedThreadCount); }
            catch (Exception ex) { ProcessException(ex); }
        }
        /// <summary>
        /// Notifies about stopping of thread inside QueueAsyncProcessor (due to normal stopping procedure or due to unhandled exception)
        /// </summary>
        /// <param name="queueProcName">Name of QueueAsyncProcessor</param>
        /// <param name="curThreadCount">Current number of active threads</param>
        /// <param name="expectedThreadCount">Expected total number of threads</param>
        public void QueueAsyncProcessorThreadStop(string queueProcName, int curThreadCount, int expectedThreadCount)
        {
            try { _wrappedProvider.QueueAsyncProcessorThreadStop(queueProcName, curThreadCount, expectedThreadCount); }
            catch (Exception ex) { ProcessException(ex); }
        }
        /// <summary>
        /// Measures the time of single element processing
        /// </summary>
        /// <param name="queueProcName">Name of QueueAsyncProcessor</param>
        /// <param name="time">Processing period</param>
        public void QueueAsyncProcessorElementProcessed(string queueProcName, TimeSpan time)
        {
            try { _wrappedProvider.QueueAsyncProcessorElementProcessed(queueProcName, time); }
            catch (Exception ex) { ProcessException(ex); }
        }
        /// <summary>
        /// Notifies about adding new element to the queue of QueueAsyncProcessor
        /// </summary>
        /// <param name="queueProcName">Name of QueueAsyncProcessor</param>
        /// <param name="newElementCount">Number of elements inside queue</param>
        /// <param name="maxElementCount">Maximum number of elements inside queue (negative means unbounded)</param>
        public void QueueAsyncProcessorElementCountIncreased(string queueProcName, int newElementCount, int maxElementCount)
        {
            try { _wrappedProvider.QueueAsyncProcessorElementCountIncreased(queueProcName, newElementCount, maxElementCount); }
            catch (Exception ex) { ProcessException(ex); }
        }
        /// <summary>
        /// Notifies about taking element from the queue of QueueAsyncProcessor
        /// </summary>
        /// <param name="queueProcName">Name of QueueAsyncProcessor</param>
        /// <param name="newElementCount">Number of elements inside queue</param>
        /// <param name="maxElementCount">Maximum number of elements inside queue (negative means unbounded)</param>
        public void QueueAsyncProcessorElementCountDecreased(string queueProcName, int newElementCount, int maxElementCount)
        {
            try { _wrappedProvider.QueueAsyncProcessorElementCountDecreased(queueProcName, newElementCount, maxElementCount); }
            catch (Exception ex) { ProcessException(ex); }
        }
        /// <summary>
        /// Notifies that element was not added to the queue by <see cref="Qoollo.Turbo.Threading.QueueProcessing.QueueAsyncProcessorBase{T}.TryAdd(T)"/> 
        /// </summary>
        /// <param name="queueProcName">Name of QueueAsyncProcessor</param>
        /// <param name="currentElementCount">Number of elements inside queue</param>
        public void QueueAsyncProcessorElementRejectedInTryAdd(string queueProcName, int currentElementCount)
        {
            try { _wrappedProvider.QueueAsyncProcessorElementRejectedInTryAdd(queueProcName, currentElementCount); }
            catch (Exception ex) { ProcessException(ex); }
        }



        /// <summary>
        /// Notifies about creation of ThreadPool
        /// </summary>
        /// <param name="threadPoolName">Name of the created Thread Pool</param>
        public void ThreadPoolCreated(string threadPoolName)
        {
            try { _wrappedProvider.ThreadPoolCreated(threadPoolName); }
            catch (Exception ex) { ProcessException(ex); }
        }
        /// <summary>
        /// Notifies about stopping of ThreadPool
        /// </summary>
        /// <param name="threadPoolName">Name of the Thread Pool</param>
        /// <param name="fromFinalizer">Is stoping from finalizer</param>
        public void ThreadPoolDisposed(string threadPoolName, bool fromFinalizer)
        {
            try { _wrappedProvider.ThreadPoolDisposed(threadPoolName, fromFinalizer); }
            catch (Exception ex) { ProcessException(ex); }
        }
        /// <summary>
        /// Notifies about changing of number of threads inside ThreadPool
        /// </summary>
        /// <param name="threadPoolName">Name of the Thread Pool</param>
        /// <param name="curThreadCount">Current number of threads inside ThreadPool</param>
        public void ThreadPoolThreadCountChange(string threadPoolName, int curThreadCount)
        {
            try { _wrappedProvider.ThreadPoolThreadCountChange(threadPoolName, curThreadCount); }
            catch (Exception ex) { ProcessException(ex); }
        }
        /// <summary>
        /// Measures the time when work item was waiting inside queue
        /// </summary>
        /// <param name="threadPoolName">Name of the Thread Pool</param>
        /// <param name="time">Period of waiting</param>
        public void ThreadPoolWaitingInQueueTime(string threadPoolName, TimeSpan time)
        {
            try { _wrappedProvider.ThreadPoolWaitingInQueueTime(threadPoolName, time); }
            catch (Exception ex) { ProcessException(ex); }
        }
        /// <summary>
        /// Measures the exection time of work item inside ThreadPool
        /// </summary>
        /// <param name="threadPoolName">Name of the Thread Pool</param>
        /// <param name="time">Period of execution</param>
        public void ThreadPoolWorkProcessed(string threadPoolName, TimeSpan time)
        {
            try { _wrappedProvider.ThreadPoolWorkProcessed(threadPoolName, time); }
            catch (Exception ex) { ProcessException(ex); }
        }
        /// <summary>
        /// Notifies about cancellation of work item
        /// </summary>
        /// <param name="threadPoolName">Name of the Thread Pool</param>
        public void ThreadPoolWorkCancelled(string threadPoolName)
        {
            try { _wrappedProvider.ThreadPoolWorkCancelled(threadPoolName); }
            catch (Exception ex) { ProcessException(ex); }
        }
        /// <summary>
        /// Notifies about adding new work item to the Thread Pool queue
        /// </summary>
        /// <param name="threadPoolName">Name of the Thread Pool</param>
        /// <param name="globalQueueItemCount">Current number of work items inside queue</param>
        /// <param name="maxItemCount">Maximum number of work items inside queue</param>
        public void ThreadPoolWorkItemsCountIncreased(string threadPoolName, int globalQueueItemCount, int maxItemCount)
        {
            try { _wrappedProvider.ThreadPoolWorkItemsCountIncreased(threadPoolName, globalQueueItemCount, maxItemCount); }
            catch (Exception ex) { ProcessException(ex); }
        }
        /// <summary>
        /// Изменилось число задач в очереди на обработку в пуле потоков
        /// </summary>
        /// <param name="threadPoolName">Name of the Thread Pool</param>
        /// <param name="globalQueueItemCount">Current number of work items inside queue</param>
        /// <param name="maxItemCount">Maximum number of work items inside queue</param>
        public void ThreadPoolWorkItemsCountDecreased(string threadPoolName, int globalQueueItemCount, int maxItemCount)
        {
            try { _wrappedProvider.ThreadPoolWorkItemsCountDecreased(threadPoolName, globalQueueItemCount, maxItemCount); }
            catch (Exception ex) { ProcessException(ex); }
        }
        /// <summary>
        /// Notifies that work item was not enqueued by <see cref="Qoollo.Turbo.Threading.ThreadPools.ThreadPoolBase.TryRun(Action)"/> 
        /// </summary>
        /// <param name="threadPoolName">Name of the Thread Pool</param>
        public void ThreadPoolWorkItemRejectedInTryAdd(string threadPoolName)
        {
            try { _wrappedProvider.ThreadPoolWorkItemRejectedInTryAdd(threadPoolName); }
            catch (Exception ex) { ProcessException(ex); }
        }




        /// <summary>
        /// Notifies about creation of ThreadSetManager
        /// </summary>
        /// <param name="threadSetManagerName">Name of the created ThreadSetManager</param>
        /// <param name="initialThreadCount">Number of managed threads</param>
        public void ThreadSetManagerCreated(string threadSetManagerName, int initialThreadCount)
        {
            try { _wrappedProvider.ThreadSetManagerCreated(threadSetManagerName, initialThreadCount); }
            catch (Exception ex) { ProcessException(ex); }
        }
        /// <summary>
        /// Notifies about stopping of ThreadSetManager
        /// </summary>
        /// <param name="threadSetManagerName">Name of the ThreadSetManager</param>
        /// <param name="fromFinalizer">Is stopping from finalizer</param>
        public void ThreadSetManagerDisposed(string threadSetManagerName, bool fromFinalizer)
        {
            try { _wrappedProvider.ThreadSetManagerDisposed(threadSetManagerName, fromFinalizer); }
            catch (Exception ex) { ProcessException(ex); }
        }
        /// <summary>
        /// Notifies about thread starting inside ThreadSetManager
        /// </summary>
        /// <param name="threadSetManagerName">Name of the ThreadSetManager</param>
        /// <param name="curThreadCount">Current number of threads inside ThreadSetManager</param>
        /// <param name="expectedThreadCount">Expected number of threads</param>
        public void ThreadSetManagerThreadStart(string threadSetManagerName, int curThreadCount, int expectedThreadCount)
        {
            try { _wrappedProvider.ThreadSetManagerThreadStart(threadSetManagerName, curThreadCount, expectedThreadCount); }
            catch (Exception ex) { ProcessException(ex); }
        }
        /// <summary>
        /// Notifies about stopping of thread inside ThreadSetManager
        /// </summary>
        /// <param name="threadSetManagerName">Name of the ThreadSetManager</param>
        /// <param name="curThreadCount">Current number of threads inside ThreadSetManager</param>
        /// <param name="expectedThreadCount">Expected number of threads</param>
        public void ThreadSetManagerThreadStop(string threadSetManagerName, int curThreadCount, int expectedThreadCount)
        {
            try { _wrappedProvider.ThreadSetManagerThreadStop(threadSetManagerName, curThreadCount, expectedThreadCount); }
            catch (Exception ex) { ProcessException(ex); }
        }
    }
}
