using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Threading.QueueProcessing
{
    /// <summary>
    /// Helper interface for <see cref="QueueAsyncProcessor{T}"/>
    /// </summary>
    public interface IQueueAsyncProcessorStartStopHelper: IDisposable
    {
        /// <summary>
        /// Starts processing of items
        /// </summary>
        void Start();
        /// <summary>
        /// Stops processing of items
        /// </summary>
        /// <param name="waitForStop">When True, waits until all processing threads are stopped</param>
        /// <param name="letFinishProcess">True indicates that all items from inner queue should be processed before stopping</param>
        /// <param name="completeAdding">When True, disallows adding new items to inner queue</param>
        void Stop(bool waitForStop, bool letFinishProcess, bool completeAdding);
        /// <summary>
        /// Stops processing of items
        /// </summary>
        void Stop();
    }

    /// <summary>
    /// <see cref="QueueAsyncProcessor{T}"/> extension methods
    /// </summary>
    public static class QueueAsyncProcessorExtensions
    {
        /// <summary>
        /// Fluent start
        /// </summary>
        /// <typeparam name="TQueueProc">The type of <see cref="QueueAsyncProcessor{T}"/></typeparam>
        /// <param name="proc"><see cref="QueueAsyncProcessor{T}"/> instance</param>
        /// <returns>Same instance of the started <see cref="QueueAsyncProcessor{T}"/></returns>
        public static TQueueProc ThenStart<TQueueProc>(this TQueueProc proc) where TQueueProc : IQueueAsyncProcessorStartStopHelper
        {
            if (proc == null)
                throw new ArgumentNullException(nameof(proc));

            proc.Start();
            return proc;
        }
    }
}
