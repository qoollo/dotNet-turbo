using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Profiling
{
    internal class DefaultProfilingProvider: IProfilingProvider
    {
        public static readonly DefaultProfilingProvider Instance = new DefaultProfilingProvider();

        private DefaultProfilingProvider() { }

        public void ObjectPoolCreated(string poolName) { }
        public void ObjectPoolDisposed(string poolName, bool fromFinalizer) { }
        public void ObjectPoolElementRented(string poolName, int currentRentedCount) { }
        public void ObjectPoolElementReleased(string poolName, int currentRentedCount) { }
        public void ObjectPoolElementFaulted(string poolName, int currentElementCount) { }
        public void ObjectPoolElementCreated(string poolName, int currentElementCount) { }
        public void ObjectPoolElementDestroyed(string poolName, int currentElementCount) { }
        public void ObjectPoolElementRentedTime(string poolName, TimeSpan time) { }


        public void QueueAsyncProcessorCreated(string queueProcName) { }
        public void QueueAsyncProcessorDisposed(string queueProcName, bool fromFinalizer) { }
        public void QueueAsyncProcessorThreadStart(string queueProcName, int curThreadCount, int expectedThreadCount) { }
        public void QueueAsyncProcessorThreadStop(string queueProcName, int curThreadCount, int expectedThreadCount) { }
        public void QueueAsyncProcessorElementProcessed(string queueProcName, TimeSpan time) { }
        public void QueueAsyncProcessorElementCountIncreased(string queueProcName, int newElementCount, int maxElementCount) { }
        public void QueueAsyncProcessorElementCountDecreased(string queueProcName, int newElementCount, int maxElementCount) { }
        public void QueueAsyncProcessorElementRejectedInTryAdd(string queueProcName, int currentElementCount) { }


        public void ThreadPoolCreated(string threadPoolName) { }
        public void ThreadPoolDisposed(string threadPoolName, bool fromFinalizer) { }
        public void ThreadPoolThreadCountChange(string threadPoolName, int curThreadCount) { }
        public void ThreadPoolWaitingInQueueTime(string threadPoolName, TimeSpan time) { }
        public void ThreadPoolWorkProcessed(string threadPoolName, TimeSpan time) { }
        public void ThreadPoolWorkCancelled(string threadPoolName) { }
        public void ThreadPoolWorkItemsCountIncreased(string threadPoolName, int globalQueueItemCount, int maxItemCount) { }
        public void ThreadPoolWorkItemsCountDecreased(string threadPoolName, int globalQueueItemCount, int maxItemCount) { }
        public void ThreadPoolWorkItemRejectedInTryAdd(string threadPoolName) { }


        public void ThreadSetManagerCreated(string threadSetManagerName, int initialThreadCount) { }
        public void ThreadSetManagerDisposed(string threadSetManagerName, bool fromFinalizer) { }
        public void ThreadSetManagerThreadStart(string threadSetManagerName, int curThreadCount, int expectedThreadCount) { }
        public void ThreadSetManagerThreadStop(string threadSetManagerName, int curThreadCount, int expectedThreadCount) { }
    }
}
