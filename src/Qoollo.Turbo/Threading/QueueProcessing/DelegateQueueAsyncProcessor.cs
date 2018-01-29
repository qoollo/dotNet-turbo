using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using System.Threading;
using Qoollo.Turbo.Queues;

namespace Qoollo.Turbo.Threading.QueueProcessing
{
    /// <summary>
    /// Asynchronous items processor with queue. Concrete processing action passed as delegate.
    /// </summary>
    /// <typeparam name="T">Type of the elements processed by this <see cref="DelegateQueueAsyncProcessor{T}"/></typeparam>
    public class DelegateQueueAsyncProcessor<T> : QueueAsyncProcessor<T>
    {
        private readonly Action<T, CancellationToken> _processing;
        private readonly Action<Exception> _exceptionProc;



        /// <summary>
        /// DelegateQueueAsyncProcessor constructor
        /// </summary>
        /// <param name="threadCount">Number of processing threads</param>
        /// <param name="queue">Processing queue (current instances of <see cref="QueueAsyncProcessor{T}"/> becomes the owner)</param>
        /// <param name="name">The name for this instance of <see cref="QueueAsyncProcessor{T}"/> and its threads</param>
        /// <param name="isBackground">Whether or not processing threads are background threads</param>
        /// <param name="processing">Delegate that will be invoked to process every item</param>
        /// <param name="exceptionAct">Delegate that will be invoked to process unhandled exception (null is possible value)</param>
        public DelegateQueueAsyncProcessor(int threadCount, IQueue<T> queue, string name, bool isBackground, Action<T, CancellationToken> processing, Action<Exception> exceptionAct)
            : base(threadCount, queue, name, isBackground)
        {
            if (processing == null)
                throw new ArgumentNullException(nameof(processing));

            _processing = processing;
            _exceptionProc = exceptionAct;
        }
        /// <summary>
        /// DelegateQueueAsyncProcessor constructor
        /// </summary>
        /// <param name="threadCount">Number of processing threads</param>
        /// <param name="queue">Processing queue (current instances of <see cref="QueueAsyncProcessor{T}"/> becomes the owner)</param>
        /// <param name="name">The name for this instance of <see cref="QueueAsyncProcessor{T}"/> and its threads</param>
        /// <param name="processing">Delegate that will be invoked to process every item</param>
        /// <param name="exceptionAct">Delegate that will be invoked to process unhandled exception (null is possible value)</param>
        public DelegateQueueAsyncProcessor(int threadCount, IQueue<T> queue, string name, Action<T, CancellationToken> processing, Action<Exception> exceptionAct)
            : this(threadCount, queue, name, false, processing, exceptionAct)
        {
        }
        /// <summary>
        /// DelegateQueueAsyncProcessor constructor
        /// </summary>
        /// <param name="threadCount">Number of processing threads</param>
        /// <param name="queue">Processing queue (current instances of <see cref="QueueAsyncProcessor{T}"/> becomes the owner)</param>
        /// <param name="name">The name for this instance of <see cref="QueueAsyncProcessor{T}"/> and its threads</param>
        /// <param name="processing">Delegate that will be invoked to process every item</param>
        public DelegateQueueAsyncProcessor(int threadCount, IQueue<T> queue, string name, Action<T, CancellationToken> processing)
            : this(threadCount, queue, name, false, processing, null)
        {
        }


        /// <summary>
        /// DelegateQueueAsyncProcessor constructor
        /// </summary>
        /// <param name="threadCount">Number of processing threads</param>
        /// <param name="maxQueueSize">The bounded size of the queue (if less or equal to 0 then no limitation)</param>
        /// <param name="name">The name for this instance of <see cref="QueueAsyncProcessor{T}"/> and its threads</param>
        /// <param name="isBackground">Whether or not processing threads are background threads</param>
        /// <param name="processing">Delegate that will be invoked to process every item</param>
        /// <param name="exceptionAct">Delegate that will be invoked to process unhandled exception (null is possible value)</param>
        public DelegateQueueAsyncProcessor(int threadCount, int maxQueueSize, string name, bool isBackground, Action<T, CancellationToken> processing, Action<Exception> exceptionAct)
            : base(threadCount, maxQueueSize, name, isBackground)
        {
            if (processing == null)
                throw new ArgumentNullException(nameof(processing));

            _processing = processing;
            _exceptionProc = exceptionAct;
        }
        /// <summary>
        /// DelegateQueueAsyncProcessor constructor
        /// </summary>
        /// <param name="threadCount">Number of processing threads</param>
        /// <param name="maxQueueSize">The bounded size of the queue (if less or equal to 0 then no limitation)</param>
        /// <param name="name">The name for this instance of <see cref="QueueAsyncProcessor{T}"/> and its threads</param>
        /// <param name="processing">Delegate that will be invoked to process every item</param>
        /// <param name="exceptionAct">Delegate that will be invoked to process unhandled exception (null is possible value)</param>
        public DelegateQueueAsyncProcessor(int threadCount, int maxQueueSize, string name, Action<T, CancellationToken> processing, Action<Exception> exceptionAct)
            : this(threadCount, maxQueueSize, name, false, processing, exceptionAct)
        {
        }
        /// <summary>
        /// DelegateQueueAsyncProcessor constructor
        /// </summary>
        /// <param name="threadCount">Number of processing threads</param>
        /// <param name="maxQueueSize">The bounded size of the queue (if less or equal to 0 then no limitation)</param>
        /// <param name="name">The name for this instance of <see cref="QueueAsyncProcessor{T}"/> and its threads</param>
        /// <param name="processing">Delegate that will be invoked to process every item</param>
        public DelegateQueueAsyncProcessor(int threadCount, int maxQueueSize, string name, Action<T, CancellationToken> processing)
            : this(threadCount, maxQueueSize, name, false, processing, null)
        {
        }
        /// <summary>
        /// DelegateQueueAsyncProcessor constructor
        /// </summary>
        /// <param name="threadCount">Number of processing threads</param>
        /// <param name="name">The name for this instance of <see cref="QueueAsyncProcessor{T}"/> and its threads</param>
        /// <param name="processing">Delegate that will be invoked to process every item</param>
        /// <param name="exceptionAct">Delegate that will be invoked to process unhandled exception (null is possible value)</param>
        public DelegateQueueAsyncProcessor(int threadCount, string name, Action<T, CancellationToken> processing, Action<Exception> exceptionAct)
            : this(threadCount, -1, name, false, processing, exceptionAct)
        {
        }
        /// <summary>
        /// DelegateQueueAsyncProcessor constructor
        /// </summary>
        /// <param name="threadCount">Number of processing threads</param>
        /// <param name="name">The name for this instance of <see cref="QueueAsyncProcessor{T}"/> and its threads</param>
        /// <param name="processing">Delegate that will be invoked to process every item</param>
        public DelegateQueueAsyncProcessor(int threadCount, string name, Action<T, CancellationToken> processing)
            : this(threadCount, -1, name, false, processing, null)
        {
        }
        /// <summary>
        /// DelegateQueueAsyncProcessor constructor
        /// </summary>
        /// <param name="threadCount">Number of processing threads</param>
        /// <param name="processing">Delegate that will be invoked to process every item</param>
        public DelegateQueueAsyncProcessor(int threadCount, Action<T, CancellationToken> processing)
            : this(threadCount, -1, null, false, processing, null)
        {
        }

        /// <summary>
        /// Processes a single item taken from the processing queue.
        /// </summary>
        /// <param name="element">Item to be processed</param>
        /// <param name="state">Thread specific state object</param>
        /// <param name="token">Cancellation token that will be cancelled when the immediate stop is requested</param>
        protected override void Process(T element, object state, CancellationToken token)
        {
            _processing(element, token);
        }


        /// <summary>
        /// Method that allows to process unhandled exceptions (e.g. logging).
        /// Default behaviour - throws <see cref="QueueAsyncProcessorException"/>.
        /// </summary>
        /// <param name="ex">Catched exception</param>
        /// <returns>Whether the current exception can be safely skipped (false - the thread will retrow the exception)</returns>
        protected override bool ProcessThreadException(Exception ex)
        {
            if (_exceptionProc != null)
                _exceptionProc(ex);

            return base.ProcessThreadException(ex);
        }
    }
}
