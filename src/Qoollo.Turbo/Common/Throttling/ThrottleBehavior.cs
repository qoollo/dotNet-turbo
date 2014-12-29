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
    /// Логика отсчёта пропуска операций для предотвращения DOS
    /// </summary>
    public class ThrottleBehavior
    {
        /// <summary>
        /// Создать неограниченное поведение
        /// </summary>
        /// <returns>Throttling поведение</returns>
        public static ThrottleBehavior CreateNotLimited()
        {
            Contract.Ensures(Contract.Result<ThrottleBehavior>() != null);
            return new ThrottleBehavior(int.MaxValue, 1000);
        }


        private readonly int _measurePeriod;
        private readonly double _maxRequestPerSecond;
        private readonly int _maxHitPerMeasure;
        private readonly object _syncObj;

        private int _lastTimeMeasure;
        private int _hitCount;


        /// <summary>
        /// Конструктор ThrottleBehavior
        /// </summary>
        /// <param name="maxRequestPerSecond">Максимально допустимое число запросов в секунду</param>
        /// <param name="measurePeriodMs">Период оценки в миллисекундах</param>
        public ThrottleBehavior(double maxRequestPerSecond, int measurePeriodMs)
        {
            Contract.Requires<ArgumentException>(maxRequestPerSecond > 0);
            Contract.Requires<ArgumentException>(measurePeriodMs > 50);

            _measurePeriod = measurePeriodMs;
            _maxRequestPerSecond = maxRequestPerSecond;
            _lastTimeMeasure = GetTimeMeasureInMs();
            _hitCount = 0;

            _syncObj = new object();

            var tmpHit = Math.Ceiling(measurePeriodMs * maxRequestPerSecond / 1000.0);
            if (tmpHit < int.MaxValue)
                _maxHitPerMeasure = (int)tmpHit;
            else
                _maxHitPerMeasure = int.MaxValue;

            Contract.Assert(_maxHitPerMeasure > 0);
        }
        /// <summary>
        /// Конструктор ThrottleBehavior
        /// </summary>
        /// <param name="maxRequestPerSecond">Максимально допустимое число запросов в секунду</param>
        public ThrottleBehavior(double maxRequestPerSecond)
            : this(maxRequestPerSecond, (int)Math.Max(32000.0 / maxRequestPerSecond, 1000.0))
        {
        }


        /// <summary>
        /// Максимально разрешённое число операций в секунду
        /// </summary>
        public double MaxRequestPerSecond
        {
            get { return _maxRequestPerSecond; }
        }

        /// <summary>
        /// Получить отсчёт времени в миллисекундах
        /// </summary>
        /// <returns>Текущее значение</returns>
        private static int GetTimeMeasureInMs()
        {
            return Environment.TickCount & int.MaxValue;
        }

        /// <summary>
        /// Зафиксировать операцию
        /// </summary>
        /// <returns>true - операция может быть выполнена, false - нужно проигнорировать операцию</returns>
        public bool AddHit()
        {
            var curHitCount = Interlocked.Increment(ref _hitCount);
            if (curHitCount < _maxHitPerMeasure)
                return true;

            var elapsedTime = GetTimeMeasureInMs() - Volatile.Read(ref _lastTimeMeasure);
            if (elapsedTime < 0 || elapsedTime >= _measurePeriod)
            {
                if (curHitCount >= _maxHitPerMeasure)
                {
                    lock (_syncObj)
                    {
                        if (curHitCount >= _maxHitPerMeasure)
                        {
                            Interlocked.Add(ref _hitCount, -_maxHitPerMeasure);
                            Interlocked.Exchange(ref _lastTimeMeasure, GetTimeMeasureInMs());
                        }
                    }
                }

                return true;
            }


            OnThrottle(_measurePeriod - elapsedTime);
            return false;
        }

        
        /// <summary>
        /// Вызывается при необходимости пропуска операций
        /// </summary>
        /// <param name="restTimeMs">Время, которое осталось до конца периода</param>
        protected virtual void OnThrottle(int restTimeMs)
        {
            Contract.Requires(restTimeMs >= 0);
        }
    }
}
