using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Threading.OldThreadPools.ServiceStuff
{
    /// <summary>
    /// Единица работы для пула
    /// </summary>
    internal struct ThreadPoolWorkItem
    {
        /// <summary>
        /// Контракты
        /// </summary>
        [ContractInvariantMethod]
        private void Invariant()
        {
            Contract.Invariant(Act != null);
        }

#if SERVICE_CLASSES_PROFILING && SERVICE_CLASSES_PROFILING_TIME
        private Profiling.ProfilingTimer _storeTimer;

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
        /// Действие
        /// </summary>
        public Action Act;
        /// <summary>
        /// Захваченный контекст исполнения
        /// </summary>
        public ExecutionContext CapturedContext;


        /// <summary>
        /// Конструктор ThreadPoolWorkItem
        /// </summary>
        /// <param name="act">Действие без параметров</param>
        public ThreadPoolWorkItem(Action act)
        {
            Contract.Requires(act != null);

            Act = act;
            CapturedContext = null;

#if SERVICE_CLASSES_PROFILING && SERVICE_CLASSES_PROFILING_TIME
            _storeTimer = Profiling.ProfilingTimer.Create();
#endif
        }


        /// <summary>
        /// Выполнение действия без состояния
        /// </summary>
        /// <param name="syncContextState">Контекст синхронизации</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InnerRunJob(object syncContextState)
        {
            SynchronizationContext syncContext = (SynchronizationContext)syncContextState;

            if (syncContext != null && SynchronizationContext.Current != syncContext)
                SynchronizationContext.SetSynchronizationContext(syncContext);

            Debug.Assert(Act != null);

            Act();
        }

        /// <summary>
        /// Выполнение действия без состояния
        /// </summary>
        /// <param name="syncContext">Контекст синхронизации</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InnerRunJob(SynchronizationContext syncContext)
        {
            if (syncContext != null && SynchronizationContext.Current != syncContext)
                SynchronizationContext.SetSynchronizationContext(syncContext);

            Debug.Assert(Act != null);

            Act();
        }


        /// <summary>
        /// Запуск задания
        /// </summary>
        /// <param name="customSyncContext">Контекст синхронизации, который нужно установить</param>
        /// <param name="restoreExecContext">Восстанавливать ли контекст исполнения</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RunJob(SynchronizationContext customSyncContext, bool restoreExecContext)
        {
            if (CapturedContext != null && restoreExecContext)
            {
                ExecutionContext.Run(CapturedContext, InnerRunJob, customSyncContext);
            }
            else
            {
                InnerRunJob(customSyncContext);
            }
        }
    }
}
