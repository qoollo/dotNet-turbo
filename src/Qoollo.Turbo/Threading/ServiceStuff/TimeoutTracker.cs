using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Threading.ServiceStuff
{
    /// <summary>
    /// Struct that recalculate the timeout value of some operation
    /// </summary>
    internal struct TimeoutTracker
    {
        private readonly int _originalTimeout;
        private readonly uint _startTime;

        /// <summary>
        /// TimeoutTracker constructor
        /// </summary>
        /// <param name="timeout">Original timeout value</param>
        /// <param name="startTime">Process start timestamp</param>
        public TimeoutTracker(int timeout, uint startTime)
        {
            _originalTimeout = timeout;
            _startTime = startTime;
        }
        /// <summary>
        /// TimeoutTracker constructor
        /// </summary>
        /// <param name="timeout">Original timeout value in milliseconds</param>
        public TimeoutTracker(int timeout)
        {
            _originalTimeout = timeout;
            _startTime = TimeoutHelper.GetTimestamp();
        }
        /// <summary>
        /// TimeoutTracker constructor
        /// </summary>
        /// <param name="timeout">Original timeout value</param>
        public TimeoutTracker(TimeSpan timeout)
        {
            long timeoutLong = (long)timeout.TotalMilliseconds;
            if (timeoutLong < -1L || timeoutLong > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(timeout));

            _originalTimeout = (int)timeoutLong;
            _startTime = TimeoutHelper.GetTimestamp();
        }

        /// <summary>
        /// Original timeout value in milliseconds
        /// </summary>
        public int OriginalTimeout { get { return _originalTimeout; } }
        /// <summary>
        /// Process start timestamp
        /// </summary>
        public uint StartTime { get { return _startTime; } }


        /// <summary>
        /// Gets the remaining milliseconds of the executed process
        /// </summary>
        public int RemainingMilliseconds
        {
            get
            {
                if (_originalTimeout < 0)
                    return System.Threading.Timeout.Infinite;
                if (_originalTimeout == 0)
                    return 0;

                return TimeoutHelper.UpdateTimeout(_startTime, _originalTimeout);
            }
        }
        /// <summary>
        /// Is specified for the operation time is up
        /// </summary>
        public bool IsTimeouted { get { return RemainingMilliseconds == 0; } }
    }
}
