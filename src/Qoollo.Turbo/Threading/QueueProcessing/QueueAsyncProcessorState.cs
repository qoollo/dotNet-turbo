using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Threading.QueueProcessing
{
    /// <summary>
    /// Defines possible states of the <see cref="QueueAsyncProcessor{T}"/>
    /// </summary>
    public enum QueueAsyncProcessorState : int
    {
        /// <summary>
        /// Indicates that <see cref="QueueAsyncProcessor{T}"/> was created but not started
        /// </summary>
        Created = 0,
        /// <summary>
        /// Indicates that <see cref="QueueAsyncProcessor{T}"/> is being transitioned from <see cref="Created"/> to <see cref="Running"/> state
        /// </summary>
        StartRequested = 1,
        /// <summary>
        /// Indicates that <see cref="QueueAsyncProcessor{T}"/> is started and can process item
        /// </summary>
        Running = 2,
        /// <summary>
        /// Indicates that <see cref="QueueAsyncProcessor{T}"/> is being transitioned from <see cref="Running"/> to <see cref="Stopped"/> state
        /// </summary>
        StopRequested = 3,
        /// <summary>
        /// Indicates that <see cref="QueueAsyncProcessor{T}"/> is stopped
        /// </summary>
        Stopped = 4,
    }
}
