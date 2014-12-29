using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Threading
{
    /// <summary>
    /// Объект контроля запуска потока
    /// </summary>
    public class ThreadStartControllingToken
    {
        /// <summary>
        /// Время когда можно считать, что возникла ошибка инициализации и состояние не будет установлено.
        /// </summary>
        public const int MaxTimeToFailMs = 15000;
        private const int STATE_NOT_INITIALIZED = 0;
        private const int STATE_INITIALIZED_OK = 1;
        private const int STATE_INITIALIZED_FAIL = -1;

        /// <summary>
        /// Получить отсчёт времени в миллисекундах
        /// </summary>
        /// <returns>Текущее значение</returns>
        private static uint GetTimestamp()
        {
            return (uint)Environment.TickCount;
        }

        private volatile uint _timestamp = GetTimestamp();
        private int _state = STATE_NOT_INITIALIZED;

        /// <summary>
        /// Задано ли значение результата инициализации
        /// </summary>
        public bool IsInitialized
        {
            get
            {
                int locState = Volatile.Read(ref _state);
                if (locState != STATE_NOT_INITIALIZED)
                    return true;

                if (GetTimestamp() - _timestamp > MaxTimeToFailMs)
                {
                    SetFail();
                    return Volatile.Read(ref _state) != STATE_NOT_INITIALIZED;
                }
                return false;
            }
        }
        /// <summary>
        /// Прошла ли инициализация успешно
        /// </summary>
        public bool IsOk { get { return Volatile.Read(ref _state) == STATE_INITIALIZED_OK; } }

        /// <summary>
        /// Установить, что инциализация прошла успешно
        /// </summary>
        public void SetOk()
        {
            Interlocked.CompareExchange(ref _state, STATE_INITIALIZED_OK, STATE_NOT_INITIALIZED);
        }
        /// <summary>
        /// Установить, что возникла ошибка инициализации
        /// </summary>
        public void SetFail()
        {
            Interlocked.CompareExchange(ref _state, STATE_INITIALIZED_FAIL, STATE_NOT_INITIALIZED);
        }
    }
}
