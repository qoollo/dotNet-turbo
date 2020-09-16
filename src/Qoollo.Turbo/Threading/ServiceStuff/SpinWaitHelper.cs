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
        internal enum ProcessorKind
        {
            Other = 0,
            IntelPreSkylake,
            IntelPostSkylake
        }


        private const int NORM_COEF_NOT_CALCULATED = 0;
        private const int NORM_COEF_CALC_STARTED = 1;
        private const int NORM_COEF_CALC_FINISHED = 2;

        private const int NsPerSecond = 1000 * 1000 * 1000;
        private const int SpinWaitCountPerStep = 2000;
        /// <summary>
        /// Coefficient getted from Net Core runtime as the base for normalization (this should result in the same behavior as in .NET Core 3.0 runtime)
        /// </summary>
        private const int MinNsPerNormalizedSpin = 37;


        private static int _normalizationCoef = 1;
        private static volatile int _normalizationCalculated = NORM_COEF_NOT_CALCULATED;


#if NETFRAMEWORK || NETSTANDARD

        public static int NormalizationCoef
        {
            get
            {
                EnsureSpinWaitNormalizationCoefCalculated();
                return _normalizationCoef;
            }
        }
        internal static bool NormalizationCoefCalculated { get { return _normalizationCalculated == NORM_COEF_CALC_FINISHED; } }

        public static void SpinWait(int iterations)
        {
            EnsureSpinWaitNormalizationCoefCalculated();
            Thread.SpinWait(iterations * _normalizationCoef);
        }
#else

        public static int NormalizationCoef { get { return 1; } }
        internal static bool NormalizationCoefCalculated { get { return true; } }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SpinWait(int iterations)
        {
            Thread.SpinWait(iterations);
        }
#endif


        private static bool TryParseProcessorIdentifierPart(string[] parts, string partName, out int value)
        {
            value = 0;
            int partIndex = parts.FindIndex(o => string.Equals(o, partName, StringComparison.OrdinalIgnoreCase));
            if (partIndex < 0 || partIndex + 1 >= parts.Length)
                return false;

            return int.TryParse(parts[partIndex + 1], out value);
        }
        internal static ProcessorKind GetProcessorKind()
        {
            var processorIdentifier = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER");
            if (string.IsNullOrWhiteSpace(processorIdentifier))
                return ProcessorKind.Other;

            if (!processorIdentifier.Contains("Intel"))
                return ProcessorKind.Other;

            var parts = processorIdentifier.Split();
            if (!TryParseProcessorIdentifierPart(parts, "Family", out int processorFamily))
                return ProcessorKind.Other;
            if (!TryParseProcessorIdentifierPart(parts, "Model", out int processorModel))
                return ProcessorKind.Other;

            // Only family 6 is known for us
            if (processorFamily != 6)
                return ProcessorKind.Other;

            // 85 is from CPUID list. Models with greater number is definetely after Skylake
            if (processorModel < 85)
                return ProcessorKind.IntelPreSkylake;

            return ProcessorKind.IntelPostSkylake;
        }

        internal static bool IsFrameworkSupportSpinWaitNormalization()
        {
            var hiddenField = typeof(Thread).GetProperty("OptimalMaxSpinWaitsPerSpinIteration", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            return hiddenField != null;
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        private static double MeasureSpinWaitNormalizationCoef(int measureDurationMs)
        {
            int spinCount = 0;
            long expectedDuration = Math.Max(1, measureDurationMs * Stopwatch.Frequency / 1000); // recalc duration from ms to ticks
            long startTimeStamp = Stopwatch.GetTimestamp();
            long elapsedTime;

            do
            {
                Thread.SpinWait(SpinWaitCountPerStep);
                spinCount += SpinWaitCountPerStep;
                elapsedTime = unchecked(Stopwatch.GetTimestamp() - startTimeStamp);
            } while (elapsedTime < expectedDuration);


            double nsPerSpin = (double)elapsedTime * NsPerSecond / ((double)spinCount * Stopwatch.Frequency);
            return MinNsPerNormalizedSpin / nsPerSpin;
        }

        internal static int GetSpinWaitNormalizationCoef()
        {
            // Check whether framework support normalization out of the box (for NETSTANDARD)
            if (IsFrameworkSupportSpinWaitNormalization())
                return 1;

            double mainMeasure;
            if (Stopwatch.IsHighResolution && Stopwatch.Frequency > 1000 * 1000)
            {
                // Perform 3 short measures
                // Choose the largest one (larger coef -> larger spinCount in specified interval -> less probability of context switching)
                mainMeasure = MeasureSpinWaitNormalizationCoef(measureDurationMs: 8);

                Thread.Yield();
                mainMeasure = Math.Max(mainMeasure, MeasureSpinWaitNormalizationCoef(measureDurationMs: 8));

                Thread.Yield();
                mainMeasure = Math.Max(mainMeasure, MeasureSpinWaitNormalizationCoef(measureDurationMs: 8));
            }
            else
            {
                // Single long measure
                mainMeasure = MeasureSpinWaitNormalizationCoef(measureDurationMs: 20);
            }

#if NETFRAMEWORK
            // Correct measure for reduced cpu frequency
            if (GetProcessorKind() == ProcessorKind.IntelPreSkylake && mainMeasure > 1.9 && mainMeasure < 4)
                mainMeasure = 4;
#endif


            if (mainMeasure < 1)
                mainMeasure = 1;
            else if (mainMeasure > 32)
                mainMeasure = 32;

            return (int)Math.Round(mainMeasure);
        }


        private static void MeasureSpinWaitNormalizationCoefThreadFunc(object state)
        {
            try
            {
                Interlocked.Exchange(ref _normalizationCoef, GetSpinWaitNormalizationCoef());
            }
            catch // Catch all exceptions to prevent runtime failure if something unexpected happened
            {
#if DEBUG
                throw;
#endif
            }
            finally
            {
                Interlocked.Exchange(ref _normalizationCalculated, NORM_COEF_CALC_FINISHED);
            }
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
    }
}
