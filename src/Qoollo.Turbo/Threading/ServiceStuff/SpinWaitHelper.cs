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


#if NETFRAMEWORK

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


        internal static ProcessorKind DetectProcessorKind()
        {
            var processorIdentifier = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER");
            if (string.IsNullOrWhiteSpace(processorIdentifier))
                return ProcessorKind.Other;

            if (!processorIdentifier.Contains("Intel"))
                return ProcessorKind.Other;

            var parts = processorIdentifier.Split();
            int familyIndex = parts.FindIndex(o => string.Equals(o, "Family", StringComparison.OrdinalIgnoreCase));
            int modelIndex = parts.FindIndex(o => string.Equals(o, "Model", StringComparison.OrdinalIgnoreCase));
            if (familyIndex < 0 || familyIndex + 1 >= parts.Length)
                return ProcessorKind.Other;
            if (modelIndex < 0 || modelIndex + 1 >= parts.Length)
                return ProcessorKind.Other;

            int familyNumber = 0;
            int modelNumber = 0;
            if (!int.TryParse(parts[familyIndex + 1], out familyNumber))
                return ProcessorKind.Other;
            if (!int.TryParse(parts[modelIndex + 1], out modelNumber))
                return ProcessorKind.Other;

            if (familyNumber != 6)
                return ProcessorKind.Other;

            if (modelNumber < 85)
                return ProcessorKind.IntelPreSkylake;

            return ProcessorKind.IntelPostSkylake;
        }

        internal static bool IsFrameworkSupportSpinWaitNormalization()
        {
            var hiddenField = typeof(Thread).GetProperty("OptimalMaxSpinWaitsPerSpinIteration", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            return hiddenField != null;
        }



        [MethodImpl(MethodImplOptions.NoInlining)]
        private static long BurnCpu(int burnDurationMs)
        {
            long result = 1;
            long count = 0;
            long expectedDuration = Math.Max(1, burnDurationMs * Stopwatch.Frequency / 1000); // recalc duration from ms to ticks
            long startTimeStamp = Stopwatch.GetTimestamp();
            long elapsedTime;

            do
            {
                result = (long)(result * Math.Sqrt(++count));
                elapsedTime = unchecked(Stopwatch.GetTimestamp() - startTimeStamp);
            } while (elapsedTime < expectedDuration);


            return result;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static long MeasureSingleStepExpectedDuration()
        {
            long result = long.MaxValue;
            for (int i = 0; i < 3; i++)
            {
                long start = Stopwatch.GetTimestamp();
                Thread.SpinWait(SpinWaitCountPerStep);
                result = Math.Min(result, unchecked(Stopwatch.GetTimestamp() - start));
            }
            return result;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static double MeasureSpinWaitNormalizationCoefSinglePassAlt(int measureDurationMs)
        {
            long singleStepDuration = MeasureSingleStepExpectedDuration();
            int spinCount = 0;
            int outlineSpinCount = 0;
            long expectedDuration = Math.Max(1, measureDurationMs * Stopwatch.Frequency / 1000); // recalc duration from ms to ticks
            long outlineTicks = 0;

            long startTimeStamp = Stopwatch.GetTimestamp();
            long prevTimeStamp = startTimeStamp;
            long curTimeStamp;

            do
            {
                Thread.SpinWait(SpinWaitCountPerStep);
                spinCount += SpinWaitCountPerStep;
                curTimeStamp = Stopwatch.GetTimestamp();
                if (unchecked(curTimeStamp - prevTimeStamp) > 2 * singleStepDuration)
                {
                    // outline
                    outlineSpinCount += SpinWaitCountPerStep;
                    outlineTicks += unchecked(curTimeStamp - prevTimeStamp);
                }
                else
                {
                    singleStepDuration = Math.Min(singleStepDuration, unchecked(curTimeStamp - prevTimeStamp));
                }

                prevTimeStamp = curTimeStamp;
            } while (unchecked(curTimeStamp - startTimeStamp) < expectedDuration);

            long elapsedTime = unchecked(curTimeStamp - startTimeStamp) - outlineTicks;
            spinCount -= outlineSpinCount;

            double nsPerSpin = (double)elapsedTime * NsPerSecond / ((double)spinCount * Stopwatch.Frequency);
            return MinNsPerNormalizedSpin / nsPerSpin;
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static double MeasureSpinWaitNormalizationCoefSinglePass(int measureDurationMs)
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

        internal static int MeasureSpinWaitNormalizationCoef()
        {
            BurnCpu(60);
            double mainMeasure = MeasureSpinWaitNormalizationCoefSinglePassAlt(measureDurationMs: 20);

            // Validate it is within expected range
            if (mainMeasure < 0.9 || mainMeasure > 1.1)
            {
                // Suspicies result: repeat measurements and choose the largest one (larger coef -> larger spinCount in specified interval -> less probability of context switching)
                Thread.Yield();
                BurnCpu(60);
                mainMeasure = Math.Max(mainMeasure, MeasureSpinWaitNormalizationCoefSinglePassAlt(measureDurationMs: 14));
                Thread.Yield();
                BurnCpu(60);
                mainMeasure = Math.Max(mainMeasure, MeasureSpinWaitNormalizationCoefSinglePassAlt(measureDurationMs: 8));
            }

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
                Interlocked.Exchange(ref _normalizationCoef, MeasureSpinWaitNormalizationCoef());
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
