using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Threading.ThreadManagement
{
    /// <summary>
    /// Состояния ThreadManager
    /// </summary>
    public enum ThreadSetManagerState: int
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
        /// Все потоки завершились
        /// </summary>
        AllThreadsExited = 4,
        /// <summary>
        /// Остановлен
        /// </summary>
        Stopped = 5,
    }
}
