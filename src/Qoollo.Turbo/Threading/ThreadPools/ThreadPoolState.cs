using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Threading.ThreadPools
{
    /// <summary>
    /// Defines possible states of the ThreadPool
    /// </summary>
    public enum ThreadPoolState: int
    {
        /// <summary>
        /// Indicates that ThreadPool was created
        /// </summary>
        Created = 0,
        /// <summary>
        /// Indicates that threads are running inside ThreadPool
        /// </summary>
        Running = 1,
        /// <summary>
        /// Indicates that stop was requested by user
        /// </summary>
        StopRequested = 2,
        /// <summary>
        /// Indicates that ThreadPool is fully stopped
        /// </summary>
        Stopped = 3
    }
}
