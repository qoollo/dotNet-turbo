using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo
{
    /// <summary>
    /// ThrottleBehavior allows to limit the number of operations per second.
    /// This can be helpfull in preventing DOS attacs.
    /// </summary>
    public class ThrottleBehavior
    {
        /// <summary>
        /// Creates unlimited ThrottleBehavior (allow all operations)
        /// </summary>
        /// <returns>Created ThrottleBehavior</returns>
        public static ThrottleBehavior CreateNotLimited()
        {
            TurboContract.Ensures(TurboContract.Result<ThrottleBehavior>() != null);
            return new ThrottleBehavior(int.MaxValue, 1000);
        }


        private readonly int _measurePeriod;
        private readonly double _maxRequestPerSecond;
        private readonly int _maxHitPerMeasure;
        private readonly object _syncObj;

        private int _lastTimeMeasure;
        private int _hitCount;


        /// <summary>
        /// ThrottleBehavior constructor
        /// </summary>
        /// <param name="maxRequestPerSecond">Maximum number of operations per second</param>
        /// <param name="measurePeriodMs">Measure period to estimate current number of operations</param>
        public ThrottleBehavior(double maxRequestPerSecond, int measurePeriodMs)
        {
            if (maxRequestPerSecond <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxRequestPerSecond), nameof(maxRequestPerSecond) + " should be positive");
            if (measurePeriodMs <= 50)
                throw new ArgumentOutOfRangeException(nameof(measurePeriodMs), nameof(measurePeriodMs) + " should be grater than 50");

            _measurePeriod = measurePeriodMs;
            _maxRequestPerSecond = maxRequestPerSecond;
            _lastTimeMeasure = GetTimeMeasureInMs();
            _hitCount = 0;

            _syncObj = new object();

            var tmpHit = Math.Ceiling(measurePeriodMs * maxRequestPerSecond / 1000.0);
            if (tmpHit < int.MaxValue)
                _maxHitPerMeasure = (int)tmpHit;
            else
                _maxHitPerMeasure = int.MaxValue;

            TurboContract.Assert(_maxHitPerMeasure > 0);
        }
        /// <summary>
        /// ThrottleBehavior constructor
        /// </summary>
        /// <param name="maxRequestPerSecond">Maximum number of operations per second</param>
        public ThrottleBehavior(double maxRequestPerSecond)
            : this(maxRequestPerSecond, (int)Math.Max(32000.0 / maxRequestPerSecond, 1000.0))
        {
        }


        /// <summary>
        /// Maximum number of operations per second
        /// </summary>
        public double MaxRequestPerSecond
        {
            get { return _maxRequestPerSecond; }
        }

        /// <summary>
        /// Returns the current time measure in milliseconds
        /// </summary>
        /// <returns>Current time</returns>
        private static int GetTimeMeasureInMs()
        {
            return Environment.TickCount & int.MaxValue;
        }


        /// <summary>
        /// Register a new hit. Slow path.
        /// </summary>
        /// <returns>True if the operation can be executed (op/s not exeeded), overwise returns false</returns>
        private bool AddHitSlowPath()
        {
            var elapsedTime = GetTimeMeasureInMs() - Volatile.Read(ref _lastTimeMeasure);
            if (elapsedTime < 0 || elapsedTime >= _measurePeriod)
            {
                if (Volatile.Read(ref _hitCount) >= _maxHitPerMeasure)
                {
                    lock (_syncObj)
                    {
                        if (Volatile.Read(ref _hitCount) >= _maxHitPerMeasure)
                        {
                            Interlocked.Add(ref _hitCount, -_maxHitPerMeasure);
                            Interlocked.Exchange(ref _lastTimeMeasure, GetTimeMeasureInMs());
                        }
                    }
                }

                return true;
            }


            OnThrottle(_measurePeriod - elapsedTime);
            return false;
        }

        /// <summary>
        /// Register a new hit.
        /// Returns true if the operation can be executed (op/s not exeeded), overwise returns false. 
        /// </summary>
        /// <returns>True if the operation can be executed (op/s not exeeded), overwise returns false</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AddHit()
        {
            var curHitCount = Interlocked.Increment(ref _hitCount);
            if (curHitCount < _maxHitPerMeasure)
                return true;

            return AddHitSlowPath();
        }

        
        /// <summary>
        /// Calls when operation should be skipped (op/s exeeded)
        /// </summary>
        /// <param name="restTimeMs">Time in milliseconds till the end of the current measure period</param>
        protected virtual void OnThrottle(int restTimeMs)
        {
            TurboContract.Requires(restTimeMs >= 0);
        }
    }
}
