using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Profiling
{
    /// <summary>
    /// Turbo profiler
    /// </summary>
    public class Profiler
    {
        private static readonly object _syncObject = new object();
        private static IProfilingProvider _profiler = null;
        private static bool _isProfilingEnabled = false;

        /// <summary>
        /// Whether the concrete profiler setted by user
        /// </summary>
        public static bool IsProfilingEnabled { get { return _isProfilingEnabled; } }

        /// <summary>
        /// Sets the profiling data handler
        /// </summary>
        /// <param name="profiler">Profiler that collects profiling events</param>
        public static void SetProfiler(IProfilingProvider profiler)
        {
            lock (_syncObject)
            {
                if (profiler == null)
                    profiler = null;
                else if (profiler.GetType() != typeof(ProfilingProviderWrapper) && !profiler.GetType().IsSubclassOf(typeof(ProfilingProviderWrapper)))
                    profiler = new ProfilingProviderWrapper(profiler);

                System.Threading.Interlocked.Exchange(ref _profiler, profiler);
                _isProfilingEnabled = !object.ReferenceEquals(profiler, null);
            }
        }


        [System.Diagnostics.Conditional("SERVICE_CLASSES_PROFILING")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ObjectPoolCreated(string poolName)
        {
            _profiler?.ObjectPoolCreated(poolName);
        }
        [System.Diagnostics.Conditional("SERVICE_CLASSES_PROFILING")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ObjectPoolDisposed(string poolName, bool fromFinalizer) 
        {
            _profiler?.ObjectPoolDisposed(poolName, fromFinalizer);
        }
        [System.Diagnostics.Conditional("SERVICE_CLASSES_PROFILING")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ObjectPoolElementRented(string poolName, int currentRentedCount) 
        {
            _profiler?.ObjectPoolElementRented(poolName, currentRentedCount);
        }
        [System.Diagnostics.Conditional("SERVICE_CLASSES_PROFILING")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ObjectPoolElementReleased(string poolName, int currentRentedCount) 
        {
            _profiler?.ObjectPoolElementReleased(poolName, currentRentedCount);
        }
        [System.Diagnostics.Conditional("SERVICE_CLASSES_PROFILING")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ObjectPoolElementFaulted(string poolName, int currentElementCount) 
        {
            _profiler?.ObjectPoolElementFaulted(poolName, currentElementCount);
        }
        [System.Diagnostics.Conditional("SERVICE_CLASSES_PROFILING")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ObjectPoolElementCreated(string poolName, int currentElementCount) 
        {
            _profiler?.ObjectPoolElementCreated(poolName, currentElementCount);
        }
        [System.Diagnostics.Conditional("SERVICE_CLASSES_PROFILING")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ObjectPoolElementDestroyed(string poolName, int currentElementCount) 
        {
            _profiler?.ObjectPoolElementDestroyed(poolName, currentElementCount);
        }
        [System.Diagnostics.Conditional("SERVICE_CLASSES_PROFILING_TIME")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ObjectPoolElementRentedTime(string poolName, TimeSpan time)
        {
            _profiler?.ObjectPoolElementRentedTime(poolName, time);
        }



        [System.Diagnostics.Conditional("SERVICE_CLASSES_PROFILING")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void QueueAsyncProcessorCreated(string queueProcName)
        {
            _profiler?.QueueAsyncProcessorCreated(queueProcName);
        }
        [System.Diagnostics.Conditional("SERVICE_CLASSES_PROFILING")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void QueueAsyncProcessorDisposed(string queueProcName, bool fromFinalizer)
        {
            _profiler?.QueueAsyncProcessorDisposed(queueProcName, fromFinalizer);
        }
        [System.Diagnostics.Conditional("SERVICE_CLASSES_PROFILING")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void QueueAsyncProcessorThreadStart(string queueProcName, int curThreadCount, int expectedThreadCount)
        {
            _profiler?.QueueAsyncProcessorThreadStart(queueProcName, curThreadCount, expectedThreadCount);
        }
        [System.Diagnostics.Conditional("SERVICE_CLASSES_PROFILING")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void QueueAsyncProcessorThreadStop(string queueProcName, int curThreadCount, int expectedThreadCount)
        {
            _profiler?.QueueAsyncProcessorThreadStop(queueProcName, curThreadCount, expectedThreadCount);
        }
        [System.Diagnostics.Conditional("SERVICE_CLASSES_PROFILING_TIME")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void QueueAsyncProcessorElementProcessed(string queueProcName, TimeSpan time)
        {
            _profiler?.QueueAsyncProcessorElementProcessed(queueProcName, time);
        }
        [System.Diagnostics.Conditional("SERVICE_CLASSES_PROFILING")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void QueueAsyncProcessorElementCountIncreased(string queueProcName, int newElementCount, int maxElementCount)
        {
            _profiler?.QueueAsyncProcessorElementCountIncreased(queueProcName, newElementCount, maxElementCount);
        }
        [System.Diagnostics.Conditional("SERVICE_CLASSES_PROFILING")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void QueueAsyncProcessorElementCountDecreased(string queueProcName, int newElementCount, int maxElementCount)
        {
            _profiler?.QueueAsyncProcessorElementCountDecreased(queueProcName, newElementCount, maxElementCount);
        }
        [System.Diagnostics.Conditional("SERVICE_CLASSES_PROFILING")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void QueueAsyncProcessorElementRejectedInTryAdd(string queueProcName, int currentElementCount)
        {
            _profiler?.QueueAsyncProcessorElementRejectedInTryAdd(queueProcName, currentElementCount);
        }



        [System.Diagnostics.Conditional("SERVICE_CLASSES_PROFILING")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ThreadPoolCreated(string threadPoolName)
        {
            _profiler?.ThreadPoolCreated(threadPoolName);
        }
        [System.Diagnostics.Conditional("SERVICE_CLASSES_PROFILING")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ThreadPoolDisposed(string threadPoolName, bool fromFinalizer)
        {
            _profiler?.ThreadPoolDisposed(threadPoolName, fromFinalizer);
        }
        [System.Diagnostics.Conditional("SERVICE_CLASSES_PROFILING")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ThreadPoolThreadCountChange(string threadPoolName, int curThreadCount)
        {
            _profiler?.ThreadPoolThreadCountChange(threadPoolName, curThreadCount);
        }
        [System.Diagnostics.Conditional("SERVICE_CLASSES_PROFILING_TIME")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ThreadPoolWaitingInQueueTime(string threadPoolName, TimeSpan time)
        {
            _profiler?.ThreadPoolWaitingInQueueTime(threadPoolName, time);
        }
        [System.Diagnostics.Conditional("SERVICE_CLASSES_PROFILING_TIME")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ThreadPoolWorkProcessed(string threadPoolName, TimeSpan time)
        {
            _profiler?.ThreadPoolWorkProcessed(threadPoolName, time);
        }
        [System.Diagnostics.Conditional("SERVICE_CLASSES_PROFILING")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ThreadPoolWorkCancelled(string threadPoolName)
        {
            _profiler?.ThreadPoolWorkCancelled(threadPoolName);
        }
        [System.Diagnostics.Conditional("SERVICE_CLASSES_PROFILING")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ThreadPoolWorkItemsCountIncreased(string threadPoolName, int globalQueueItemCount, int maxItemCount)
        {
            _profiler?.ThreadPoolWorkItemsCountIncreased(threadPoolName, globalQueueItemCount, maxItemCount);
        }
        [System.Diagnostics.Conditional("SERVICE_CLASSES_PROFILING")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ThreadPoolWorkItemsCountDecreased(string threadPoolName, int globalQueueItemCount, int maxItemCount)
        {
            _profiler?.ThreadPoolWorkItemsCountDecreased(threadPoolName, globalQueueItemCount, maxItemCount);
        }
        [System.Diagnostics.Conditional("SERVICE_CLASSES_PROFILING")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ThreadPoolWorkItemRejectedInTryAdd(string threadPoolName)
        {
            _profiler?.ThreadPoolWorkItemRejectedInTryAdd(threadPoolName);
        }



        [System.Diagnostics.Conditional("SERVICE_CLASSES_PROFILING")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ThreadSetManagerCreated(string threadSetManagerName, int initialThreadCount)
        {
            _profiler?.ThreadSetManagerCreated(threadSetManagerName, initialThreadCount);
        }
        [System.Diagnostics.Conditional("SERVICE_CLASSES_PROFILING")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ThreadSetManagerDisposed(string threadSetManagerName, bool fromFinalizer)
        {
            _profiler?.ThreadSetManagerDisposed(threadSetManagerName, fromFinalizer);
        }
        [System.Diagnostics.Conditional("SERVICE_CLASSES_PROFILING")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ThreadSetManagerThreadStart(string threadSetManagerName, int curThreadCount, int expectedThreadCount)
        {
            _profiler?.ThreadSetManagerThreadStart(threadSetManagerName, curThreadCount, expectedThreadCount);
        }
        [System.Diagnostics.Conditional("SERVICE_CLASSES_PROFILING")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ThreadSetManagerThreadStop(string threadSetManagerName, int curThreadCount, int expectedThreadCount)
        {
            _profiler?.ThreadSetManagerThreadStop(threadSetManagerName, curThreadCount, expectedThreadCount);
        }
    }
}
