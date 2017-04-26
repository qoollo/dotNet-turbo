using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Threading.ServiceStuff
{
    /// <summary>
    /// Helper methods to work with timeouts
    /// </summary>
    internal static class TimeoutHelper
    {
        /// <summary>
        /// Returns the current timestamp in milliseconds
        /// </summary>
        /// <returns>Timestamp</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint GetTimestamp()
        {
            return (uint)Environment.TickCount;
        }
        /// <summary>
        /// Updates the timeout value based on elapsed time
        /// </summary>
        /// <param name="startTime">Timestamp for the moment when processing started</param>
        /// <param name="originalTimeout">Original timeout value</param>
        /// <returns>Rest time in milliseconds</returns>
        public static int UpdateTimeout(uint startTime, int originalTimeout)
        {
            uint elapsed = GetTimestamp() - startTime;
            if (elapsed > (uint)int.MaxValue)
                return 0;

            int rest = originalTimeout - (int)elapsed;
            if (rest <= 0)
                return 0;

            return rest;
        }
        /// <summary>
        /// Recalculate the timeout value based on elapsed time (additionally checks wether the originalTimeout is zero or Infinite)
        /// </summary>
        /// <param name="startTime">Timestamp for the moment when processing started</param>
        /// <param name="originalTimeout">Original timeout value</param>
        /// <returns>Rest time in milliseconds</returns>
        public static int RecalculateTimeout(uint startTime, int originalTimeout)
        {
            if (originalTimeout == 0)
                return 0;
            if (originalTimeout == System.Threading.Timeout.Infinite)
                return System.Threading.Timeout.Infinite;

            return UpdateTimeout(startTime, originalTimeout);
        }
    }
}
