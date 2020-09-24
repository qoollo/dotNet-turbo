using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Threading.ServiceStuff
{
    /// <summary>
    /// <see cref="SpinWait"/> extension methods
    /// </summary>
    internal static class SpinWaitExtensions
    {
#if NETFRAMEWORK
        /// <summary>
        /// Performs a single spin without calling Thread.Sleep(1)
        /// </summary>
        /// <param name="sw">SpinWait</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SpinOnceNoSleep(this ref SpinWait sw)
        {
            if (sw.Count >= 29)
                sw.Reset();
            sw.SpinOnce();
        }
#elif NETCOREAPP
        /// <summary>
        /// Performs a single spin without calling Thread.Sleep(1)
        /// </summary>
        /// <param name="sw">SpinWait</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SpinOnceNoSleep(this ref SpinWait sw)
        {
            sw.SpinOnce(sleep1Threshold: -1);
        }
#else
        /// <summary>
        /// Performs a single spin
        /// </summary>
        /// <param name="sw">SpinWait</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SpinOnceNoSleep(this ref SpinWait sw)
        {
            sw.SpinOnce();
        }
#endif
    }
}
