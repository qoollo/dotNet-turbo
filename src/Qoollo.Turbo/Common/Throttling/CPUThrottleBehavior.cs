using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo
{
    /// <summary>
    /// Логика отсчёта пропуска операций для предотвращения DOS по CPU.
    /// Усыпляет потоки при превышении допустимого числа операций.
    /// </summary>
    public class CPUThrottleBehavior : ThrottleBehavior
    {
        /// <summary>
        /// Создать неограниченное поведение
        /// </summary>
        /// <returns>Throttling поведение</returns>
        public new static CPUThrottleBehavior CreateNotLimited()
        {
            Contract.Ensures(Contract.Result<CPUThrottleBehavior>() != null);
            return new CPUThrottleBehavior(int.MaxValue, 1000);
        }

        /// <summary>
        /// Конструктор CPUThrottleBehaviour
        /// </summary>
        /// <param name="maxRequestPerSecond">Максимально допустимое число запросов в секунду</param>
        /// <param name="measurePeriodMs">Период оценки в миллисекундах</param>
        public CPUThrottleBehavior(double maxRequestPerSecond, int measurePeriodMs)
            : base(maxRequestPerSecond, measurePeriodMs)
        {
        }
        /// <summary>
        /// Конструктор CPUThrottleBehaviour
        /// </summary>
        /// <param name="maxRequestPerSecond">Максимально допустимое число запросов в секунду</param>
        public CPUThrottleBehavior(double maxRequestPerSecond)
            : base(maxRequestPerSecond)
        {
        }


        /// <summary>
        /// Вызывается при необходимости пропуска операций
        /// </summary>
        /// <param name="restTimeMs">Время, которое осталось до конца периода</param>
        protected override void OnThrottle(int restTimeMs)
        {
            if (restTimeMs <= 0)
                System.Threading.Thread.Sleep(1);
            else
                System.Threading.Thread.Sleep(restTimeMs);
        }
    }
}
