using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo
{
    /// <summary>
    /// Tracks the time when some event happened and allows to execute action only for the first appearance of that event in the specified time interval
    /// </summary>
    /// <example>Can be used to not log repeated exceptions all the time</example>
    public class EventTimingTracker
    {
        /// <summary>
        /// Represents the method that will be called on the first appearance of the event in the specified time interval
        /// </summary>
        /// <param name="isFirstTime">Is this is the very first registered appearance of the event</param>
        public delegate void ActionOnPeriodPassed(bool isFirstTime);
        
        // =========
        
        /// <summary>
        /// Returns current time stamp in milliseconds
        /// </summary>
        /// <returns>TimeStamp</returns>
        private static int GetTimeStamp()
        {
            int result = Environment.TickCount;
            return result != 0 ? result : 1;
        }
        
        // ============
        
		private readonly int _periodMs;
        private int _registeredTimeStamp;

        /// <summary>
        /// EventTimingTracker constructor.
        /// Default time interval: 5 minutes.
        /// </summary>
        public EventTimingTracker()
            : this(5 * 60 * 1000)
        {
        }
        /// <summary>
        /// EventTimingTracker constructor
        /// </summary>
        /// <param name="period">Time interval between reactions on the event</param>
        public EventTimingTracker(TimeSpan period)
            : this((int)period.TotalMilliseconds)
        {
            if (period < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(period), nameof(period) + " cannot be negative");
            if (period.TotalMilliseconds >= int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(period), nameof(period) + " should be less than int.MaxValue");
        }
        /// <summary>
        /// EventTimingTracker constructor
        /// </summary>
        /// <param name="periodMs">Time interval between reactions on the event</param>
        public EventTimingTracker(int periodMs)
        {
            if (periodMs < 0)
                throw new ArgumentOutOfRangeException(nameof(periodMs), nameof(periodMs) + " cannot be negative");
            
            _periodMs = periodMs;
            _registeredTimeStamp = 0;
        }

        /// <summary>
        /// Gets the value indicating whether the event was already registered
        /// </summary>
        public bool IsEventRegistered { get { return Volatile.Read(ref _registeredTimeStamp) != 0; } }
        /// <summary>
        /// Gets the value indicating whether the time interval elapsed
        /// </summary>
		public bool IsPeriodPassed 
		{
			get
			{
				int registeredTimeStamp = Volatile.Read(ref _registeredTimeStamp);
				return registeredTimeStamp == 0 || GetTimeStamp() - registeredTimeStamp > _periodMs;
			}
		}

        /// <summary>
        /// Registers an event and returns true when it can be handled
        /// </summary>
        /// <param name="firstTime">Is this is the very first appearance of the event</param>
        /// <returns>True when event can be handled; overwise false</returns>
        public bool Register(out bool firstTime)
        {
            int newTimeStamp = GetTimeStamp();
            int registeredTimeStamp = Volatile.Read(ref _registeredTimeStamp);
            firstTime = registeredTimeStamp == 0;

            if (registeredTimeStamp == 0 || newTimeStamp - registeredTimeStamp > _periodMs)
            {
                Interlocked.Exchange(ref _registeredTimeStamp, newTimeStamp);
                return true;
            }

            return false;
        }
        /// <summary>
        /// Registers an event and returns true when it can be handled
        /// </summary>
        /// <returns>True when event can be handled; overwise false</returns>
        public bool Register()
        {
            int newTimeStamp = GetTimeStamp();
            int registeredTimeStamp = Volatile.Read(ref _registeredTimeStamp);

            if (registeredTimeStamp == 0 || newTimeStamp - registeredTimeStamp > _periodMs)
            {
                Interlocked.Exchange(ref _registeredTimeStamp, newTimeStamp);
                return true;
            }

            return false;
        }
		/// <summary>
        /// Registers an event and calls the delegate when it can be handled
		/// </summary>
		/// <param name="action">Method to handle the event appearance</param>
		public void Register(ActionOnPeriodPassed action)
		{
			if (action == null)
                throw new ArgumentNullException(nameof(action));
                
            int newTimeStamp = GetTimeStamp();
            int registeredTimeStamp = Volatile.Read(ref _registeredTimeStamp);

            if (registeredTimeStamp == 0 || newTimeStamp - registeredTimeStamp > _periodMs)
            {
                Interlocked.Exchange(ref _registeredTimeStamp, newTimeStamp);
                action(registeredTimeStamp == 0);
            }
		}

        /// <summary>
        /// Resets the registration of the event
        /// </summary>
        public void Reset()
        {
            if (Volatile.Read(ref _registeredTimeStamp) != 0)
                Interlocked.Exchange(ref _registeredTimeStamp, 0);
        }
    }
}