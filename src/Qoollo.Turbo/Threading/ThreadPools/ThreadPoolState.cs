using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Threading.ThreadPools
{
    /// <summary>
    /// Состояние пула потоков
    /// </summary>
    public enum ThreadPoolState: int
    {
        /// <summary>
        /// Пул создан
        /// </summary>
        Created = 0,
        /// <summary>
        /// В пуле работают потоки
        /// </summary>
        Running = 1,
        /// <summary>
        /// Запрошена остановка
        /// </summary>
        StopRequested = 2,
        /// <summary>
        /// Полностью остановлен
        /// </summary>
        Stopped = 3
    }
}
