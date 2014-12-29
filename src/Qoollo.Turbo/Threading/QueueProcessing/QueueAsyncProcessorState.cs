using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Threading.QueueProcessing
{
    /// <summary>
    /// Состояние асинхронного обработчика
    /// </summary>
    public enum QueueAsyncProcessorState : int
    {
        /// <summary>
        /// Создан
        /// </summary>
        Created = 0,
        /// <summary>
        /// В процессе запуска
        /// </summary>
        StartRequested = 1,
        /// <summary>
        /// Работает
        /// </summary>
        Running = 2,
        /// <summary>
        /// В процессе остановки
        /// </summary>
        StopRequested = 3,
        /// <summary>
        /// Остановлен
        /// </summary>
        Stopped = 4,
    }
}
