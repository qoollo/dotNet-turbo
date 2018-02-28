using Qoollo.Turbo.Threading.ServiceStuff;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Threading.ThreadPools.Common
{
    /// <summary>
    /// Defines possible states of the <see cref="ThreadPoolWorkItem"/>
    /// </summary>
    internal enum ThreadPoolWorkItemState: int
    {
        /// <summary>
        /// State is not tracking
        /// </summary>
        NotTracked = -1,
        /// <summary>
        /// Indicates that <see cref="ThreadPoolWorkItem"/> was created but not started
        /// </summary>
        Created = 0,
        /// <summary>
        /// Indicates that <see cref="ThreadPoolWorkItem"/> was started
        /// </summary>
        Started = 1,
        /// <summary>
        /// Indicates that <see cref="ThreadPoolWorkItem"/> was completed
        /// </summary>
        Completed = 2,
        /// <summary>
        /// Indicates that <see cref="ThreadPoolWorkItem"/> was cancelled
        /// </summary>
        Cancelled = 3
    }

    /// <summary>
    /// Work item for the ThreadPool (provides methods to run work inside thread pool)
    /// </summary>
    public abstract class ThreadPoolWorkItem
    {
        internal static readonly ContextCallback RunContextCallback = new ContextCallback(RunInnerHelper);
        internal static readonly WaitCallback RunWaitCallback = new WaitCallback(RunInnerHelper);
        internal static readonly Action<object> RunAction = new Action<object>(RunInnerHelper);

        /// <summary>
        /// Helper method to run work item when state object is required
        /// </summary>
        /// <param name="workItemState"><see cref="ThreadPoolWorkItem"/> to be executed</param>
        private static void RunInnerHelper(object workItemState)
        {
            ThreadPoolWorkItem workItem = (ThreadPoolWorkItem)workItemState;
            TurboContract.Assert(workItem != null, conditionString: "workItem != null");
            workItem.RunInner();
        }

        /// <summary>
        /// Checks whether the state transition is acceptable
        /// </summary>
        /// <param name="newState">Target state</param>
        /// <param name="oldState">Original state</param>
        /// <returns>Whether the state transition is acceptable</returns>
        private static bool IsValidStateTransition(ThreadPoolWorkItemState newState, ThreadPoolWorkItemState oldState)
        {
            switch (oldState)
            {
                case ThreadPoolWorkItemState.Created:
                    return newState == ThreadPoolWorkItemState.Started || newState == ThreadPoolWorkItemState.Cancelled;
                case ThreadPoolWorkItemState.Started:
                    return newState == ThreadPoolWorkItemState.Completed;
                case ThreadPoolWorkItemState.Completed:
                    return false;
                case ThreadPoolWorkItemState.Cancelled:
                    return false;
            }
            return false;
        }

        // =============

        /// <summary>
        /// Captured ExecutionContext
        /// </summary>
        private ExecutionContext _сapturedContext;
        private readonly bool _allowExecutionContextFlow;
        private readonly bool _preferFairness;

#if DEBUG
        private int _state;
#endif

#if SERVICE_CLASSES_PROFILING && SERVICE_CLASSES_PROFILING_TIME
        private Profiling.ProfilingTimer _storeTimer;
#endif

        /// <summary>
        /// <see cref="TaskThreadPoolWorkItem"/> constructor
        /// </summary>
        /// <param name="allowExecutionContextFlow">Indicates whether the ExecutionContext should be captured</param>
        /// <param name="preferFairness">Indicates whether this work item should alwayes be enqueued to the GlobalQueue</param>
        public ThreadPoolWorkItem(bool allowExecutionContextFlow, bool preferFairness) 
        {
            _allowExecutionContextFlow = allowExecutionContextFlow;
            _preferFairness = preferFairness;
        }

        /// <summary>
        /// Indicates whether the ExecutionContext should be captured
        /// </summary>
        public bool AllowExecutionContextFlow { get { return _allowExecutionContextFlow; } }
        /// <summary>
        /// Indicates whether this work item should alwayes be enqueued to the GlobalQueue
        /// </summary>
        public bool PreferFairness { get { return _preferFairness; } }
        /// <summary>
        /// Captured ExecutionContext
        /// </summary>
        internal ExecutionContext CapturedContext { get { return _сapturedContext; } }

#if DEBUG
        /// <summary>
        /// Work item state
        /// </summary>
        internal ThreadPoolWorkItemState State { get { return (ThreadPoolWorkItemState)Volatile.Read(ref _state); } }
#else
        /// <summary>
        /// Work item state
        /// </summary>
        internal ThreadPoolWorkItemState State { get { return ThreadPoolWorkItemState.NotTracked; } }
#endif


        /// <summary>
        /// Updates the state of the Work Item (for debugging purposes)
        /// </summary>
        /// <param name="newState">New state</param>
        [System.Diagnostics.Conditional("DEBUG")]
        private void UpdateState(ThreadPoolWorkItemState newState)
        {
#if DEBUG
            SpinWait sw = new SpinWait();
            int curState = Volatile.Read(ref _state);
            while (IsValidStateTransition(newState, (ThreadPoolWorkItemState)curState))
            {
                if (Interlocked.CompareExchange(ref _state, (int)newState, curState) == curState)
                    return;

                sw.SpinOnce();
                curState = Volatile.Read(ref _state);
            }

            throw new WrongStateException("Not allowed ThreadPoolWorkItem state change from '" + ((ThreadPoolWorkItemState)curState).ToString() + "' to '" + newState.ToString() + "'");
#endif
        }


#if SERVICE_CLASSES_PROFILING && SERVICE_CLASSES_PROFILING_TIME
       
        internal void StartStoreTimer()
        {
            _storeTimer.StartTime();
        }
        internal TimeSpan StopStoreTimer()
        {
            return _storeTimer.StopTime();
        }
        internal TimeSpan GetStoreTime()
        {
            return _storeTimer.GetTime();
        }
#else
        [System.Diagnostics.Conditional("SERVICE_CLASSES_PROFILING_TIME")]
        internal void StartStoreTimer()
        {
        }
        internal TimeSpan StopStoreTimer()
        {
            return TimeSpan.Zero;
        }
        internal TimeSpan GetStoreTime()
        {
            return TimeSpan.Zero;
        }
#endif


        /// <summary>
        /// Captures the current ExecutionContext
        /// </summary>
        /// <param name="captureSyncContext">Whether the synchronization context should be captured too</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CaptureExecutionContext(bool captureSyncContext = false)
        {
            if (!_allowExecutionContextFlow)
                return;

            if (captureSyncContext)
                _сapturedContext = ExecutionContext.Capture();
            else
                _сapturedContext = ExecutionContextHelper.CaptureContextNoSyncContextIfPossible();
        }

        /// <summary>
        /// Runs this work item (internal method)
        /// </summary>
        protected abstract void RunInner();
        /// <summary>
        /// Notifies that work item was cancelled (internal method)
        /// </summary>
        protected virtual void CancelInner() { }


        /// <summary>
        /// Runs this work item  
        /// </summary>
        /// <param name="restoreExecContext">Whether the ExecutionContext should be restored</param>
        /// <param name="preserveSyncContext">Whether the current SynchronizationContext should be preserved</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Run(bool restoreExecContext, bool preserveSyncContext)
        {
            UpdateState(ThreadPoolWorkItemState.Started);

            if (restoreExecContext && _сapturedContext != null)
            {
                ExecutionContextHelper.RunInContext(_сapturedContext, RunContextCallback, this, preserveSyncContext);
            }
            else
            {
                RunInner();
            }

            UpdateState(ThreadPoolWorkItemState.Completed);
        }

        /// <summary>
        /// Cancels the work item
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Cancel()
        {
            UpdateState(ThreadPoolWorkItemState.Cancelled);
           
            CancelInner();
        }
    }
}
