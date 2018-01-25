using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo
{
    /// <summary>
    /// CPUThrottleBehavior allows to limit the number of operations per second by suspending the execution thread
    /// </summary>
    public class CPUThrottleBehavior : ThrottleBehavior
    {
        /// <summary>
        /// Creates unlimited CPUThrottleBehavior (allow all operations)
        /// </summary>
        /// <returns>Created CPUThrottleBehavior</returns>
        public new static CPUThrottleBehavior CreateNotLimited()
        {
            TurboContract.Ensures(TurboContract.Result<CPUThrottleBehavior>() != null);
            return new CPUThrottleBehavior(int.MaxValue, 1000);
        }

        /// <summary>
        /// CPUThrottleBehaviour constructor
        /// </summary>
        /// <param name="maxRequestPerSecond">Maximum number of operations per second</param>
        /// <param name="measurePeriodMs">Measure period to estimate current number of operations</param>
        public CPUThrottleBehavior(double maxRequestPerSecond, int measurePeriodMs)
            : base(maxRequestPerSecond, measurePeriodMs)
        {
        }
        /// <summary>
        /// CPUThrottleBehaviour constructor
        /// </summary>
        /// <param name="maxRequestPerSecond">Maximum number of operations per second</param>
        public CPUThrottleBehavior(double maxRequestPerSecond)
            : base(maxRequestPerSecond)
        {
        }


        /// <summary>
        /// Suspend the current thread till the end of the current measure period
        /// </summary>
        /// <param name="restTimeMs">Time in milliseconds till the end of the current measure period</param>
        protected override void OnThrottle(int restTimeMs)
        {
            if (restTimeMs <= 0)
                System.Threading.Thread.Sleep(1);
            else
                System.Threading.Thread.Sleep(restTimeMs);
        }
    }
}
