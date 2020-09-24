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
    /// <summary>
    /// SpinWait with normalization. Performs period normalization to the post-skylake level. Should be roughly the same as .NET Core 3.1 logic
    /// </summary>
    internal static class SpinWaitHelper
    {
        /// <summary>
        /// CPU kind
        /// </summary>
        internal enum ProcessorKind
        {
            /// <summary>
            /// Other
            /// </summary>
            Other = 0,
            /// <summary>
            /// Intel before Skylake. Expected normalization coefficient: 8-10
            /// </summary>
            IntelPreSkylake,
            /// <summary>
            /// Intel Skylake and later. Expected normalization coefficient: 1
            /// </summary>
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
        /// <summary>
        /// SpinWait normalization coefficient. Number of iterations passed to <see cref="SpinWait(int)"/> will be multiplied to this value
        /// </summary>
        public static int NormalizationCoef
        {
            get
            {
                EnsureSpinWaitNormalizationCoefCalculated();
                return _normalizationCoef;
            }
        }
        /// <summary>
        /// True when <see cref="NormalizationCoef"/> is calculated, otherwise False
        /// </summary>
        internal static bool NormalizationCoefCalculated { get { return _normalizationCalculated == NORM_COEF_CALC_FINISHED; } }

        /// <summary>
        /// Causes a thread to wait the number of times defined by the iterations parameter (normalized version)
        /// </summary>
        /// <param name="iterations">Integer that defines how long a thread is to wait</param>
        public static void SpinWait(int iterations)
        {
            EnsureSpinWaitNormalizationCoefCalculated();
            Thread.SpinWait(iterations * _normalizationCoef);
        }
#else
        /// <summary>
        /// SpinWait normalization coefficient. Number of iterations passed to <see cref="SpinWait(int)"/> will be multiplied to this value
        /// </summary>
        public static int NormalizationCoef { get { return 1; } }
        /// <summary>
        /// True when <see cref="NormalizationCoef"/> is calculated, otherwise False
        /// </summary>
        internal static bool NormalizationCoefCalculated { get { return true; } }

        /// <summary>
        /// Causes a thread to wait the number of times defined by the iterations parameter (normalized version)
        /// </summary>
        /// <param name="iterations">Integer that defines how long a thread is to wait</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SpinWait(int iterations)
        {
            Thread.SpinWait(iterations);
        }
#endif

        /// <summary>
        /// Triggers calculation of <see cref="NormalizationCoef"/> and waits for its completion
        /// </summary>
        internal static void WaitUntilNormalizationCoefCalculated()
        {
            EnsureSpinWaitNormalizationCoefCalculated();
            int count = 0;
            while (!NormalizationCoefCalculated)
            {
                if (count % 10 == 0)
                    Thread.Sleep(1);
                else if (count % 2 == 0)
                    Thread.Sleep(0);
                else
                    Thread.Yield();
            }
        }

        /// <summary>
        /// Attempts to parse CPU identifier part
        /// </summary>
        /// <param name="parts">CPU identifier splitted by words</param>
        /// <param name="partName">Name of the part (example: 'Family', 'Model')</param>
        /// <param name="value">Parsed value of the part</param>
        /// <returns>True if parsed successfully</returns>
        private static bool TryParseProcessorIdentifierPart(string[] parts, string partName, out int value)
        {
            value = 0;
            int partIndex = parts.FindIndex(o => string.Equals(o, partName, StringComparison.OrdinalIgnoreCase));
            if (partIndex < 0 || partIndex + 1 >= parts.Length)
                return false;

            return int.TryParse(parts[partIndex + 1], out value);
        }
        /// <summary>
        /// Gets CPU kind. Uses 'PROCESSOR_IDENTIFIER' environment variable, so valid only on Windows
        /// </summary>
        /// <returns>CPU kind</returns>
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

        /// <summary>
        /// Checkes whether NET Framework supports SpinWait normalization out of the box
        /// </summary>
        /// <returns>True if supports (for .NET Core 3.0 and later), otherwise False</returns>
        internal static bool IsFrameworkSupportSpinWaitNormalization()
        {
            var hiddenField = typeof(Thread).GetProperty("OptimalMaxSpinWaitsPerSpinIteration", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            return hiddenField != null;
        }

        /// <summary>
        /// Performes single mesure of normalization coefficient
        /// </summary>
        /// <param name="measureDurationMs">Duration of the measure in milliseconds</param>
        /// <returns></returns>
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

        /// <summary>
        /// Calculates <see cref="NormalizationCoef"/> based on several measures, on running Framework and etc.
        /// </summary>
        /// <returns>Calculated normalization coefficient</returns>
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

                Thread.Sleep(0);
                mainMeasure = Math.Max(mainMeasure, MeasureSpinWaitNormalizationCoef(measureDurationMs: 4));

                Thread.Sleep(0);
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

        /// <summary>
        /// Method that runs on separate thread and runs Normalization coefficient calculation
        /// </summary>
        /// <param name="state">State parameter</param>
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

        /// <summary>
        /// Ensures that <see cref="NormalizationCoef"/> calculated and, if not, starts the calculation (slow path)
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void EnsureSpinWaitNormalizationCoefCalculatedSlow()
        {
            if (Interlocked.CompareExchange(ref _normalizationCalculated, NORM_COEF_CALC_STARTED, NORM_COEF_NOT_CALCULATED) == NORM_COEF_NOT_CALCULATED)
            {
                ThreadPool.QueueUserWorkItem(MeasureSpinWaitNormalizationCoefThreadFunc);
            }
        }
        /// <summary>
        /// Ensures that <see cref="NormalizationCoef"/> calculated and, if not, starts the calculation
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void EnsureSpinWaitNormalizationCoefCalculated()
        {
            if (_normalizationCalculated == NORM_COEF_NOT_CALCULATED)
                EnsureSpinWaitNormalizationCoefCalculatedSlow();
        }
    }
}
