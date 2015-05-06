using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics.Contracts;

namespace Qoollo.Turbo
{
    /// <summary>
    /// Отслеживание событий для выполнения действий с минимально разрешённым периодом.
    /// Применяется, если событие возникает часто, а действие нужно предпринимать с заданным интервалом времени.
    /// Пример: логирование исключений
    /// </summary>
    public class PeriodicalEventTracker
    {
        /// <summary>
        /// Делегат для выполнения действий при наступлении события, когда отведённый период времени истёк
        /// </summary>
        /// <param name="isFirstTime">Первое ли появление события</param>
        public delegate void ActionOnPeriodPassed(bool isFirstTime);
        
        // =========
        
        /// <summary>
        /// Получение временной отметки в миллисекундах
        /// </summary>
        /// <returns>Временная метка</returns>
        private static int GetTimeStamp()
        {
            int result = Environment.TickCount;
            return result != 0 ? result : 1;
        }
        
        // ============
        
		private readonly int _periodMs;
        private int _registeredTimeStamp;

        /// <summary>
        /// Конструктор PeriodicalEventTracker. 
        /// Период по умолчанию: 5 минут.
        /// </summary>
        public PeriodicalEventTracker()
            : this(5 * 60 * 1000)
        {
        }
        /// <summary>
        /// Конструктор PeriodicalEventTracker
        /// </summary>
        /// <param name="period">Период реагирования на событие</param>
        public PeriodicalEventTracker(TimeSpan period)
            : this((int)period.TotalMilliseconds)
        {
			Contract.Requires<ArgumentException>(period >= TimeSpan.Zero);
            Contract.Requires<ArgumentException>(period.TotalMilliseconds < int.MaxValue);
        }
        /// <summary>
        /// Конструктор PeriodicalEventTracker
        /// </summary>
        /// <param name="periodMs">Период реагирования на событие</param>
        public PeriodicalEventTracker(int periodMs)
        {
            Contract.Requires<ArgumentException>(periodMs >= 0);
            
            _periodMs = periodMs;
            _registeredTimeStamp = 0;
        }

        /// <summary>
        /// Зарегистрировано ли уже событие
        /// </summary>
        public bool IsEventRegistered { get { return Volatile.Read(ref _registeredTimeStamp) != 0; } }
        /// <summary>
        /// Прошёл ли период, через который нужно отреагировать на событие
        /// </summary>
		public bool IsPeriodPassed 
		{
			get
			{
				int registeredTimeStamp = Volatile.Read(ref _registeredTimeStamp);
				return registeredTimeStamp == 0 || GetTimeStamp() - registeredTimeStamp > _periodMs;
			}
		}

        /// <summary>
        /// Зарегистрировать событие и получить информацию о необходимости реакции
        /// </summary>
        /// <param name="firstTime">Первое ли появление события</param>
        /// <returns>Нужно ли реагировать на это событие</returns>
        public bool Register(out bool firstTime)
        {
            int newTimeStamp = GetTimeStamp();
            int registeredTimeStamp = Volatile.Read(ref _registeredTimeStamp);
            firstTime = registeredTimeStamp == 0;

            if (registeredTimeStamp == 0 || newTimeStamp - registeredTimeStamp > _periodMs)
            {
                Interlocked.Exchange(ref _registeredTimeStamp, newTimeStamp);
                return true;
            }

            return false;
        }
        /// <summary>
        /// Зарегистрировать событие и получить информацию о необходимости реакции
        /// </summary>
        /// <returns>Нужно ли реагировать на это событие</returns>
        public bool Register()
        {
            int newTimeStamp = GetTimeStamp();
            int registeredTimeStamp = Volatile.Read(ref _registeredTimeStamp);

            if (registeredTimeStamp == 0 || newTimeStamp - registeredTimeStamp > _periodMs)
            {
                Interlocked.Exchange(ref _registeredTimeStamp, newTimeStamp);
                return true;
            }

            return false;
        }
		/// <summary>
        /// Зарегистрировать событие и вызвать делегат при необходимости обработки
		/// </summary>
		/// <param name="action">Действие по реакции на событие</param>
		public void Register(ActionOnPeriodPassed action)
		{
			if (action == null)
                throw new ArgumentNullException("action");
                
            int newTimeStamp = GetTimeStamp();
            int registeredTimeStamp = Volatile.Read(ref _registeredTimeStamp);

            if (registeredTimeStamp == 0 || newTimeStamp - registeredTimeStamp > _periodMs)
            {
                Interlocked.Exchange(ref _registeredTimeStamp, newTimeStamp);
                action(registeredTimeStamp == 0);
            }
		}

        /// <summary>
        /// Сбросить флаг наличия события
        /// </summary>
        public void Reset()
        {
            if (Volatile.Read(ref _registeredTimeStamp) != 0)
                Interlocked.Exchange(ref _registeredTimeStamp, 0);
        }
    }
}