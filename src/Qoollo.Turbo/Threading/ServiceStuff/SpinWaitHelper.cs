using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Threading.ServiceStuff
{
    internal static class SpinWaitHelper
    {
        private const int NORM_COEF_NOT_CALCULATED = 0;
        private const int NORM_COEF_CALC_STARTED = 1;
        private const int NORM_COEF_CALC_FINISHED = 2;

        private const int NsPerSecond = 1000 * 1000 * 1000;
        /// <summary>
        /// Coefficient getted from Net Core runtime as the base for normalization (this should result in the same behavior as in .NET Core 3.0 runtime)
        /// </summary>
        private const int MinNsPerNormalizedSpin = 37;


        private static int _normalizationCoef = 1;
        private static volatile int _normalizationCalculated = NORM_COEF_NOT_CALCULATED;

        public static int NormalizationCoef
        {
            get
            {
                EnsureSpinWaitNormalizationCoefCalculated();
                return _normalizationCoef;
            }
        }
        internal static bool NormalizationCoefCalculated { get { return _normalizationCalculated == NORM_COEF_CALC_FINISHED; } }

        internal static double MeasureSpinWaitNormalizationCoefSinglePass()
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
            return MinNsPerNormalizedSpin / nsPerSpin;
        }

        internal static int MeasureSpinWaitNormalizationCoef()
        {
            double mainMeasure = MeasureSpinWaitNormalizationCoefSinglePass();

            // Validate it is within expected range
            if (mainMeasure < 0.9 || mainMeasure > 1.1)
            {
                // Suspicies result: repeat measurements
                double[] allMeasures = new double[3];
                allMeasures[0] = mainMeasure;
                allMeasures[1] = MeasureSpinWaitNormalizationCoefSinglePass();
                allMeasures[2] = MeasureSpinWaitNormalizationCoefSinglePass();
                Array.Sort(allMeasures);
                mainMeasure = allMeasures[1];
            }

            if (mainMeasure < 1)
                mainMeasure = 1;
            else if (mainMeasure > 32)
                mainMeasure = 32;

            return (int)Math.Round(mainMeasure);
        }


        private static void MeasureSpinWaitNormalizationCoefThreadFunc(object state)
        {
            Interlocked.Exchange(ref _normalizationCoef, MeasureSpinWaitNormalizationCoef());
            Interlocked.Exchange(ref _normalizationCalculated, NORM_COEF_CALC_FINISHED);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void EnsureSpinWaitNormalizationCoefCalculatedSlow()
        {
            if (Interlocked.CompareExchange(ref _normalizationCalculated, NORM_COEF_CALC_STARTED, NORM_COEF_NOT_CALCULATED) == NORM_COEF_NOT_CALCULATED)
            {
                ThreadPool.QueueUserWorkItem(MeasureSpinWaitNormalizationCoefThreadFunc);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void EnsureSpinWaitNormalizationCoefCalculated()
        {
            if (_normalizationCalculated == NORM_COEF_NOT_CALCULATED)
                EnsureSpinWaitNormalizationCoefCalculatedSlow();
        }


        public static void SpinWait(int iterations)
        {
            EnsureSpinWaitNormalizationCoefCalculated();
            Thread.SpinWait(iterations * _normalizationCoef);
        }
    }
}
