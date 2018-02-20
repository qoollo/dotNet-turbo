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
    /// Состояние задачи
    /// </summary>
    internal enum ThreadPoolWorkItemState: int
    {
        /// <summary>
        /// Состояние не отслеживается
        /// </summary>
        NotTracked = -1,
        /// <summary>
        /// Создана
        /// </summary>
        Created = 0,
        /// <summary>
        /// Запущена на исполнение
        /// </summary>
        Started = 1,
        /// <summary>
        /// Завершена
        /// </summary>
        Completed = 2,
        /// <summary>
        /// Отменена
        /// </summary>
        Cancelled = 3
    }

    /// <summary>
    /// Единица работы для пула
    /// </summary>
    public abstract class ThreadPoolWorkItem
    {
        internal static readonly ContextCallback RunContextCallback = new ContextCallback(RunInnerHelper);
        internal static readonly WaitCallback RunWaitCallback = new WaitCallback(RunInnerHelper);
        internal static readonly Action<object> RunAction = new Action<object>(RunInnerHelper);

        /// <summary>
        /// Выполнение действия
        /// </summary>
        /// <param name="workItemState">Единица работы для исполнения</param>
        private static void RunInnerHelper(object workItemState)
        {
            ThreadPoolWorkItem workItem = (ThreadPoolWorkItem)workItemState;
            TurboContract.Assert(workItem != null, conditionString: "workItem != null");
            workItem.RunInner();
        }

        /// <summary>
        /// Допустимый ли переход состояний
        /// </summary>
        /// <param name="newState">Новое состояние</param>
        /// <param name="oldState">Старое состояние</param>
        /// <returns>Допустим переход</returns>
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
        /// Захваченный контекст исполнения
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
        /// Конструктор ThreadPoolWorkItem
        /// </summary>
        /// <param name="allowExecutionContextFlow">Допустимо ли захватывать контекст исполнения</param>
        /// <param name="preferFairness">Требовать постановку в общую очередь</param>
        public ThreadPoolWorkItem(bool allowExecutionContextFlow, bool preferFairness) 
        {
            _allowExecutionContextFlow = allowExecutionContextFlow;
            _preferFairness = preferFairness;
        }

        /// <summary>
        /// Допустимо ли захватывать контекст исполнения
        /// </summary>
        public bool AllowExecutionContextFlow { get { return _allowExecutionContextFlow; } }
        /// <summary>
        /// Требовать постановку в общую очередь
        /// </summary>
        public bool PreferFairness { get { return _preferFairness; } }
        /// <summary>
        /// Захваченный контекст исполнения
        /// </summary>
        internal ExecutionContext CapturedContext { get { return _сapturedContext; } }

#if DEBUG
        /// <summary>
        /// Состояние задачи
        /// </summary>
        internal ThreadPoolWorkItemState State { get { return (ThreadPoolWorkItemState)Volatile.Read(ref _state); } }
#else
        /// <summary>
        /// Состояние задачи
        /// </summary>
        internal ThreadPoolWorkItemState State { get { return ThreadPoolWorkItemState.NotTracked; } }
#endif

       
        /// <summary>
        /// Обновить состояние
        /// </summary>
        /// <param name="newState">Новое состояние</param>
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
        /// Захватить контекст выполнения
        /// </summary>
        /// <param name="captureSyncContext">Захватывать ли контекст синхронизации</param>
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
        /// Метод исполнения задачи
        /// </summary>
        protected abstract void RunInner();
        /// <summary>
        /// Уведомление об отмене операции
        /// </summary>
        protected virtual void CancelInner() { }


        /// <summary>
        /// Запуск исполнения работы
        /// </summary>
        /// <param name="restoreExecContext">Восстанавливать ли контекст исполнения</param>
        /// <param name="preserveSyncContext">Сохранять ли текущий контекст исполнения</param>
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
        /// Уведомление об отмене операции
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Cancel()
        {
            UpdateState(ThreadPoolWorkItemState.Cancelled);
           
            CancelInner();
        }
    }
}
