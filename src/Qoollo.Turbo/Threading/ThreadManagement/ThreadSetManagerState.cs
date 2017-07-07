using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Threading.ThreadManagement
{
    /// <summary>
    /// Defines possible states of the <see cref="ThreadSetManager"/>
    /// </summary>
    public enum ThreadSetManagerState: int
    {
        /// <summary>
        /// Indicates that <see cref="ThreadSetManager"/> was created but not started
        /// </summary>
        Created = 0,
        /// <summary>
        /// Indicates that <see cref="ThreadSetManager"/> is being transitioned from <see cref="Created"/> to <see cref="Running"/> state
        /// </summary>
        StartRequested = 1,
        /// <summary>
        /// Indicates that <see cref="ThreadSetManager"/> is started
        /// </summary>
        Running = 2,
        /// <summary>
        /// Indicates that <see cref="ThreadSetManager"/> is being transitioned from <see cref="Running"/> to <see cref="Stopped"/> state
        /// </summary>
        StopRequested = 3,
        /// <summary>
        /// Indicates that all threads from <see cref="ThreadSetManager"/> has exited
        /// </summary>
        AllThreadsExited = 4,
        /// <summary>
        /// Indicates that <see cref="ThreadSetManager"/> is fully stopped
        /// </summary>
        Stopped = 5,
    }
}
