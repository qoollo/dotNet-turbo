using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Threading.ServiceStuff
{
    /// <summary>
    /// Controls the thread starting procedure
    /// </summary>
    internal class ThreadStartControllingToken
    {
        /// <summary>
        /// Initialization timeout value
        /// </summary>
        /// <remarks>Время когда можно считать, что возникла ошибка инициализации и состояние не будет установлено.</remarks>
        public const int MaxTimeToFailMs = 15000;
        private const int STATE_NOT_INITIALIZED = 0;
        private const int STATE_INITIALIZED_OK = 1;
        private const int STATE_INITIALIZED_FAIL = -1;

        /// <summary>
        /// Get the current timestamp in milliseconds
        /// </summary>
        /// <returns>Timestamp value</returns>
        private static uint GetTimestamp()
        {
            return (uint)Environment.TickCount;
        }

        private volatile uint _timestamp = GetTimestamp();
        private int _state = STATE_NOT_INITIALIZED;

        /// <summary>
        /// Validate that initialization completed with any result
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
        /// Is initialization completed succesfully
        /// </summary>
        public bool IsOk { get { return Volatile.Read(ref _state) == STATE_INITIALIZED_OK; } }

        /// <summary>
        /// Notifies that initialization completed successfully
        /// </summary>
        public void SetOk()
        {
            Interlocked.CompareExchange(ref _state, STATE_INITIALIZED_OK, STATE_NOT_INITIALIZED);
        }
        /// <summary>
        /// Notifies that initialization is failed
        /// </summary>
        public void SetFail()
        {
            Interlocked.CompareExchange(ref _state, STATE_INITIALIZED_FAIL, STATE_NOT_INITIALIZED);
        }
    }
}
