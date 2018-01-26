using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;
using Qoollo.Turbo.Queues;

namespace Qoollo.Turbo.Threading.QueueProcessing
{
    /// <summary>
    /// Asynchronous items processor with queue. Concrete processing action passed by interface <see cref="IQueueAsyncProcessorLogic{T}"/>.
    /// </summary>
    /// <typeparam name="T">Type of the elements processed by this <see cref="InterfaceQueueAsyncProcessor{T}"/></typeparam>
    public class InterfaceQueueAsyncProcessor<T> : QueueAsyncProcessor<T>
    {
        /// <summary>
        /// Code contracts
        /// </summary>
        [ContractInvariantMethod]
        private void Invariant()
        {
            TurboContract.Invariant(_logic != null);
        }

        private readonly IQueueAsyncProcessorLogic<T> _logic;


        /// <summary>
        /// InterfaceQueueAsyncProcessor constructor
        /// </summary>
        /// <param name="logic">Implementation of the item processing logic</param>
        /// <param name="threadCount">Number of processing threads</param>
        /// <param name="queue">Processing queue (current instances of <see cref="QueueAsyncProcessor{T}"/> becomes the owner)</param>
        /// <param name="name">The name for this instance of <see cref="QueueAsyncProcessor{T}"/> and its threads</param>
        /// <param name="isBackground">Whether or not processing threads are background threads</param>
        public InterfaceQueueAsyncProcessor(IQueueAsyncProcessorLogic<T> logic, int threadCount, IQueue<T> queue, string name, bool isBackground)
            : base(threadCount, queue, name, isBackground)
        {
            if (logic == null)
                throw new ArgumentNullException(nameof(logic));

            _logic = logic;
        }
        /// <summary>
        /// InterfaceQueueAsyncProcessor constructor
        /// </summary>
        /// <param name="logic">Implementation of the item processing logic</param>
        /// <param name="threadCount">Number of processing threads</param>
        /// <param name="queue">Processing queue (current instances of <see cref="QueueAsyncProcessor{T}"/> becomes the owner)</param>
        /// <param name="name">The name for this instance of <see cref="QueueAsyncProcessor{T}"/> and its threads</param>
        public InterfaceQueueAsyncProcessor(IQueueAsyncProcessorLogic<T> logic, int threadCount, IQueue<T> queue, string name)
            : base(threadCount, queue, name, false)
        {
            if (logic == null)
                throw new ArgumentNullException(nameof(logic));

            _logic = logic;
        }

        /// <summary>
        /// InterfaceQueueAsyncProcessor constructor
        /// </summary>
        /// <param name="logic">Implementation of the item processing logic</param>
        /// <param name="threadCount">Number of processing threads</param>
        /// <param name="maxQueueSize">The bounded size of the queue (if less or equal to 0 then no limitation)</param>
        /// <param name="name">The name for this instance of <see cref="QueueAsyncProcessor{T}"/> and its threads</param>
        /// <param name="isBackground">Whether or not processing threads are background threads</param>
        public InterfaceQueueAsyncProcessor(IQueueAsyncProcessorLogic<T> logic, int threadCount, int maxQueueSize, string name, bool isBackground)
            : base(threadCount, maxQueueSize, name, isBackground)
        {
            if (logic == null)
                throw new ArgumentNullException(nameof(logic));

            _logic = logic;
        }
        /// <summary>
        /// InterfaceQueueAsyncProcessor constructor
        /// </summary>
        /// <param name="logic">Implementation of the item processing logic</param>
        /// <param name="threadCount">Number of processing threads</param>
        /// <param name="maxQueueSize">The bounded size of the queue (if less or equal to 0 then no limitation)</param>
        /// <param name="name">The name for this instance of <see cref="QueueAsyncProcessor{T}"/> and its threads</param>
        public InterfaceQueueAsyncProcessor(IQueueAsyncProcessorLogic<T> logic, int threadCount, int maxQueueSize, string name)
            : this(logic, threadCount, maxQueueSize, name, false)
        {
        }
        /// <summary>
        /// InterfaceQueueAsyncProcessor constructor
        /// </summary>
        /// <param name="logic">Implementation of the item processing logic</param>
        /// <param name="threadCount">Number of processing threads</param>
        /// <param name="name">The name for this instance of <see cref="QueueAsyncProcessor{T}"/> and its threads</param>
        public InterfaceQueueAsyncProcessor(IQueueAsyncProcessorLogic<T> logic, int threadCount, string name)
            : this(logic, threadCount, -1, name, false)
        {
        }
        /// <summary>
        /// InterfaceQueueAsyncProcessor constructor
        /// </summary>
        /// <param name="logic">Implementation of the item processing logic</param>
        /// <param name="threadCount">Number of processing threads</param>
        public InterfaceQueueAsyncProcessor(IQueueAsyncProcessorLogic<T> logic, int threadCount)
            : this(logic, threadCount, -1, null, false)
        {
        }

        /// <summary>
        /// Creates the state that is specific for every processing thread. Executes once for every thread during start-up.
        /// </summary>
        /// <returns>Created thread-specific state object</returns>
        protected override object Prepare()
        {
            if (_logic is IQueueAsyncProcessorLogicExt<T>)
                return (_logic as IQueueAsyncProcessorLogicExt<T>).Prepare();

            return base.Prepare();
        }

        /// <summary>
        /// Release the thread specific state object when the thread is about to exit
        /// </summary>
        /// <param name="state">Thread-specific state object</param>
        protected override void Finalize(object state)
        {
            if (_logic is IQueueAsyncProcessorLogicExt<T>)
                (_logic as IQueueAsyncProcessorLogicExt<T>).Finalize(state);
            else
                base.Finalize(state);
        }

        /// <summary>
        /// Processes a single item taken from the processing queue.
        /// </summary>
        /// <param name="element">Item to be processed</param>
        /// <param name="state">Thread specific state object</param>
        /// <param name="token">Cancellation token that will be cancelled when the immediate stop is requested</param>
        protected override void Process(T element, object state, CancellationToken token)
        {
            _logic.Process(element, state, token);
        }

        /// <summary>
        /// Method that allows to process unhandled exceptions (e.g. logging).
        /// Default behaviour - throws <see cref="QueueAsyncProcessorException"/>.
        /// </summary>
        /// <param name="ex">Catched exception</param>
        /// <returns>Whether the current exception can be safely skipped (false - the thread will retrow the exception)</returns>
        protected override bool ProcessThreadException(Exception ex)
        {
            if (!_logic.ProcessThreadException(ex))
                return base.ProcessThreadException(ex);

            return true;
        }
    }


    /// <summary>
    /// Specifies methods required to process the items
    /// </summary>
    /// <typeparam name="T">Type of the items</typeparam>
    [ContractClass(typeof(IQueueAsyncProcessorLogicCodeContractCheck<>))]
    public interface IQueueAsyncProcessorLogic<T>
    {
        /// <summary>
        /// Processes a single item taken from the processing queue.
        /// </summary>
        /// <param name="element">Item to be processed</param>
        /// <param name="state">Thread specific state object</param>
        /// <param name="token">Cancellation token that will be cancelled when the immediate stop is requested</param>
        void Process(T element, object state, CancellationToken token);

        /// <summary>
        /// Method that allows to process unhandled exceptions (e.g. logging).
        /// Default behaviour - throws <see cref="QueueAsyncProcessorException"/>.
        /// </summary>
        /// <param name="ex">Catched exception</param>
        /// <returns>Whether the current exception can be safely skipped (false - the thread will retrow the exception)</returns>
        bool ProcessThreadException(Exception ex);
    }



    /// <summary>
    /// Extended methods to process the items by <see cref="InterfaceQueueAsyncProcessor{T}"/>
    /// </summary>
    /// <typeparam name="T">Type of the items</typeparam>
    [ContractClass(typeof(IQueueAsyncProcessorLogicExtCodeContractCheck<>))]
    public interface IQueueAsyncProcessorLogicExt<T> : IQueueAsyncProcessorLogic<T>
    {
        /// <summary>
        /// Creates the state that is specific for every processing thread. Executes once for every thread during start-up.
        /// </summary>
        /// <returns>Created thread-specific state object</returns>
        object Prepare();
        /// <summary>
        /// Release the thread specific state object when the thread is about to exit
        /// </summary>
        /// <param name="state">Thread-specific state object</param>
        void Finalize(object state);
    }






    /// <summary>
    /// Code contracts
    /// </summary>
    [ContractClassFor(typeof(IQueueAsyncProcessorLogic<>))]
    abstract class IQueueAsyncProcessorLogicCodeContractCheck<T> : IQueueAsyncProcessorLogic<T>
    {
        /// <summary>Code contracts</summary>
        private IQueueAsyncProcessorLogicCodeContractCheck() { }

        /// <summary>Code contracts</summary>
        public bool ProcessThreadException(Exception ex)
        {
            TurboContract.Requires(ex != null, conditionString: "ex != null");

            throw new NotImplementedException();
        }
        /// <summary>Code contracts</summary>
        public void Process(T element, object state, CancellationToken token)
        {
            throw new NotImplementedException();
        }
    }




    /// <summary>
    /// Code contracts
    /// </summary>
    [ContractClassFor(typeof(IQueueAsyncProcessorLogicExt<>))]
    abstract class IQueueAsyncProcessorLogicExtCodeContractCheck<T> : IQueueAsyncProcessorLogicExt<T>
    {
        /// <summary>Code contracts</summary>
        private IQueueAsyncProcessorLogicExtCodeContractCheck() { }


        /// <summary>Code contracts</summary>
        public object Prepare()
        {
            throw new NotImplementedException();
        }
        /// <summary>Code contracts</summary>
        public void Finalize(object state)
        {
            throw new NotImplementedException();
        }
        /// <summary>Code contracts</summary>
        public void Process(T element, object state, CancellationToken token)
        {
            throw new NotImplementedException();
        }
        /// <summary>Code contracts</summary>
        public bool ProcessThreadException(Exception ex)
        {
            throw new NotImplementedException();
        }
    }
}
