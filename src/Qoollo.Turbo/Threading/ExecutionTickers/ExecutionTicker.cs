using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Threading.ExecutionTickers
{
    /// <summary>
    /// Примитив передачи управления по сообщению
    /// </summary>
    public class ExecutionTicker
    {
        private static readonly ExecutionTicker _instance = new ExecutionTicker();
        /// <summary>
        /// Стандартный инстанс
        /// </summary>
        public static ExecutionTicker Default { get { return _instance; } }

        // =============

        /// <summary>
        /// Сообщить, что можно переключится в данном месте.
        /// Подразумевается необходимость ожидания с таймаутом и отмены по токену.
        /// </summary>
        /// <param name="waitTimeout">Таймаут в миллисекундах</param>
        /// <param name="token">Токен отмены</param>
        public virtual void Tick(int waitTimeout, CancellationToken token)
        {
            if (token.CanBeCanceled)
                token.WaitHandle.WaitOne(waitTimeout);
            else
                Thread.Sleep(waitTimeout);
        }
        /// <summary>
        /// Сообщить, что можно переключится в данном месте.
        /// Подразумевается необходимость ожидания с таймаутом и отмены по токену.
        /// </summary>
        /// <param name="waitTimeout">Таймаут</param>
        /// <param name="token">Токен отмены</param>
        public void Tick(TimeSpan waitTimeout, CancellationToken token)
        {
            long timeoutMs = (long)waitTimeout.TotalMilliseconds;
            if (timeoutMs < -1L || timeoutMs > 2147483647L)
                throw new ArgumentOutOfRangeException("timeout", waitTimeout, "ExecutionTicker timeout is not in interval [-1, int.MaxValue]");

            Tick((int)timeoutMs, token);
        }
        /// <summary>
        /// Сообщить, что можно переключится в данном месте.
        /// Подразумевается необходимость ожидания с таймаутом
        /// </summary>
        /// <param name="waitTimeout">Таймаут в миллисекундах</param>
        public virtual void Tick(int waitTimeout)
        {
            Thread.Sleep(waitTimeout);
        }
        /// <summary>
        /// Сообщить, что можно переключится в данном месте.
        /// Подразумевается необходимость ожидания с таймаутом
        /// </summary>
        /// <param name="waitTimeout">Таймаут</param>
        public void Tick(TimeSpan waitTimeout)
        {
            long timeoutMs = (long)waitTimeout.TotalMilliseconds;
            if (timeoutMs < -1L || timeoutMs > 2147483647L)
                throw new ArgumentOutOfRangeException("timeout", waitTimeout, "ExecutionTicker timeout is not in interval [-1, int.MaxValue]");

            Tick((int)timeoutMs);
        }
        /// <summary>
        /// Сообщить, что можно переключится в данном месте
        /// </summary>
        public virtual void Tick()
        {
        }
    }
}
