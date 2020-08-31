using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Threading.ServiceStuff
{
    internal static class SpinWaitHelper
    {
        private const int NsPerSecond = 1000 * 1000 * 1000;
        /// <summary>
        /// Coefficient getted from Net Core runtime as the base for normalization (this should result in the same behavior as in .NET Core 3.0 runtime)
        /// </summary>
        private const int MinNsPerNormalizedSpin = 37;

        private static int _normalizationCoef = 1;
        private static volatile bool _normalizationCalculated = false;

        public static int NormalizationCoef { get { return _normalizationCoef; } }

        internal static double MeasureSpinWaitNormalizationCoef()
        {
            int spinCount = 0;
            long expectedDuration = 20 * Stopwatch.Frequency / 1000; // 20ms
            long startTimeStamp = Stopwatch.GetTimestamp();
            long elapsedTime;

            do
            {
                Thread.SpinWait(1000);
                spinCount += 1000;
                elapsedTime = unchecked(Stopwatch.GetTimestamp() - startTimeStamp);
            } while (elapsedTime < expectedDuration);


            double nsPerSpin = (double)elapsedTime * NsPerSecond / ((double)spinCount * Stopwatch.Frequency);
            return nsPerSpin / MinNsPerNormalizedSpin;
        }


        public static void SpinWait(int iterations)
        {
            Thread.SpinWait(iterations);
        }
    }
}
