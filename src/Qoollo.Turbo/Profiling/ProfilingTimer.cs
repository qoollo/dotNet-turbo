using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Profiling
{
    /// <summary>
    /// Таймер для профилировки
    /// </summary>
    internal struct ProfilingTimer
    {
#if SERVICE_CLASSES_PROFILING && SERVICE_CLASSES_PROFILING_TIME
        private System.Diagnostics.Stopwatch _totalTime;

        internal void StartTime()
        {
            if (_totalTime == null)
                _totalTime = new System.Diagnostics.Stopwatch();

            if (!_totalTime.IsRunning)
                _totalTime.Start();
        }
        internal TimeSpan StopTime()
        {
            if (_totalTime == null)
                return TimeSpan.Zero;

            _totalTime.Stop();
            return _totalTime.Elapsed;
        }
        internal TimeSpan GetTime()
        {
            if (_totalTime == null)
                return TimeSpan.Zero;

            return _totalTime.Elapsed;
        }
        internal void ResetTime()
        {
            if (_totalTime != null)
                _totalTime.Reset();
        }
        internal void RestartTime()
        {
            ResetTime();
            StartTime();
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal static ProfilingTimer StartNew()
        {
            ProfilingTimer res = new ProfilingTimer();
            res.StartTime();
            return res;
        }


        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal static ProfilingTimer Create()
        {
            ProfilingTimer res = new ProfilingTimer();
            res._totalTime = new System.Diagnostics.Stopwatch();
            return res;
        }

#else
        [System.Diagnostics.Conditional("SERVICE_CLASSES_PROFILING_TIME")]
        internal void StartTime()
        {
        }
        internal TimeSpan StopTime()
        {
            return TimeSpan.Zero;
        }
        internal TimeSpan GetTime()
        {
            return TimeSpan.Zero;
        }
        [System.Diagnostics.Conditional("SERVICE_CLASSES_PROFILING_TIME")]
        internal void ResetTime()
        {
        }
        [System.Diagnostics.Conditional("SERVICE_CLASSES_PROFILING_TIME")]
        internal void RestartTime()
        {
        }
#endif
    }
}
