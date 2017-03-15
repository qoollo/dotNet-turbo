using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Threading
{
    /// <summary>
    /// SpinWait with linear back-off strategy
    /// </summary>
    public struct LinearSpinWait
    {
        private static readonly int ProcessorCount = Environment.ProcessorCount;

        /// <summary>
        /// Best total spinning iteration count
        /// </summary>
        public const int BestTotalSpinCount = 8196;
        /// <summary>
        /// Default threshold when it is better to yield the processor (perform context switch)
        /// </summary>
        public const int DefaultYieldThreshold = 12;
        /// <summary>
        /// Default number of iteration by which the spin interval increased every time
        /// </summary>
        public const int DefaultSingleSpinCount = 100;

        private const int SLEEP_0_EVERY_HOW_MANY_TIMES = 5;
        private const int SLEEP_1_EVERY_HOW_MANY_TIMES = 20;

        /// <summary>
        /// Рассчитать наилучший Yield Threshold
        /// </summary>
        /// <param name="singleSpinCount">Значение увеличения числа циклов активного ожидания на каждой итерации</param>
        /// <returns>Yield Threshold</returns>
        public static int CalculateYieldThreshold(int singleSpinCount)
        {
            if (singleSpinCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(singleSpinCount));

            return Math.Max(1, (((int)Math.Sqrt(1 + 8 * BestTotalSpinCount / singleSpinCount) - 1) / 2));
        }


        private readonly int _singleSpinCount;
        private readonly int _yieldThreshold;
        private int _count;

        /// <summary>
        /// LinearSpinWait constructor
        /// </summary>
        /// <param name="singleSpinCount">The number of iteration by which the spin interval increased every time</param>
        public LinearSpinWait(int singleSpinCount)
        {
            if (singleSpinCount <= 0)
                throw new ArgumentException(nameof(singleSpinCount));

            _count = 0;
            _singleSpinCount = singleSpinCount;
            _yieldThreshold = (((int)Math.Sqrt(1 + 8 * BestTotalSpinCount / singleSpinCount) - 1) / 2);
            if (_yieldThreshold == 0)
                _yieldThreshold = 1;
        }
        /// <summary>
        /// LinearSpinWait constructor
        /// </summary>
        /// <param name="singleSpinCount">The number of iteration by which the spin interval increased every time</param>
        /// <param name="yieldThreshold">Threshold when it is better to yield the processor (perform context switch)</param>
        public LinearSpinWait(int singleSpinCount, int yieldThreshold)
        {
            if (singleSpinCount <= 0)
                throw new ArgumentException(nameof(singleSpinCount));
            if (yieldThreshold <= 0)
                throw new ArgumentException(nameof(yieldThreshold));

            _count = 0;
            _singleSpinCount = singleSpinCount;
            _yieldThreshold = yieldThreshold;
        }

        /// <summary>
        /// Gets the number of performed spin iterations
        /// </summary>
        public int Count { get { return _count; } }

        /// <summary>
        /// Whether the next call to <see cref="SpinOnce"/> will yield the processor, triggering a forced context switch
        /// </summary>
        public bool NextSpinWillYield
        {
            get { return ((_count >= _yieldThreshold) && (_yieldThreshold > 0 || _count >= DefaultYieldThreshold)) || ProcessorCount == 1; }
        }

        /// <summary>
        /// Resets the spin counter
        /// </summary>
        public void Reset()
        {
            _count = 0;
        }

        /// <summary>
        /// Perform a spin
        /// </summary>
        public void SpinOnce()
        {
            if (this.NextSpinWillYield)
            {
                int yieldThreshold = _yieldThreshold;
                if (yieldThreshold == 0)
                    yieldThreshold = DefaultYieldThreshold;

                int num = (_count >= yieldThreshold) ? (_count - yieldThreshold) : _count;

                if (num % SLEEP_1_EVERY_HOW_MANY_TIMES == (SLEEP_1_EVERY_HOW_MANY_TIMES - 1))
                {
                    Thread.Sleep(1);
                }
                else
                {
                    if (num % SLEEP_0_EVERY_HOW_MANY_TIMES == (SLEEP_0_EVERY_HOW_MANY_TIMES - 1))
                    {
                        Thread.Sleep(0);
                    }
                    else
                    {
                        Thread.Yield();
                    }
                }
            }
            else
            {
                int spinCount = _singleSpinCount;
                if (spinCount == 0)
                    spinCount = DefaultSingleSpinCount;

                Thread.SpinWait(spinCount * _count + spinCount);
            }
            this._count = ((this._count == int.MaxValue) ? 10 : (this._count + 1));
        }

    }
}
