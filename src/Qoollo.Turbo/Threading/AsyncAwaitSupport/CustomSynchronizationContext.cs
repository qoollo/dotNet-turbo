using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Threading
{
    /// <summary>
    /// Synchronization context that works through <see cref="ICustomSynchronizationContextSupplier"/> interface
    /// </summary>
    public class CustomSynchronizationContext: SynchronizationContext
    {
        private readonly ICustomSynchronizationContextSupplier _supplier;

        /// <summary>
        /// <see cref="CustomSynchronizationContext"/> constructor
        /// </summary>
        /// <param name="supplier">Actual executor of the supplied actions</param>
        public CustomSynchronizationContext(ICustomSynchronizationContextSupplier supplier)
        {
            if (supplier == null)
                throw new ArgumentNullException(nameof(supplier));

            _supplier = supplier;
        }

        /// <summary>
        /// Creates a copy of the synchronization context
        /// </summary>
        /// <returns>Created copy of SynchronizationContext</returns>
        public override SynchronizationContext CreateCopy()
        {
            return new CustomSynchronizationContext(_supplier);
        }
        /// <summary>
        /// Dispatches a synchronous message to a synchronization context
        /// </summary>
        /// <param name="d">Delegate to be executed</param>
        /// <param name="state">The object passed to the delegate</param>
        public override void Send(SendOrPostCallback d, object state)
        {
            _supplier.RunSync(d, state);
        }
        /// <summary>
        /// Dispatches an asynchronous message to a synchronization context
        /// </summary>
        /// <param name="d">Delegate to be executed</param>
        /// <param name="state">The object passed to the delegate</param>
        public override void Post(SendOrPostCallback d, object state)
        {
            _supplier.RunAsync(d, state);
        }
    }


    /// <summary>
    /// Provides the methods to run action in another SynchronizationContext
    /// </summary>
    [ContractClass(typeof(ICustomSynchronizationContextSupplierCodeContractCheck))]
    public interface ICustomSynchronizationContextSupplier
    {
        /// <summary>
        /// Runs an asynchronous action in another synchronization context
        /// </summary>
        /// <param name="act">Delegate to be executed</param>
        /// <param name="state">The object passed to the delegate</param>
        void RunAsync(SendOrPostCallback act, object state);
        /// <summary>
        /// Runs a synchronous action in another synchronization context
        /// </summary>
        /// <param name="act">Delegate to be executed</param>
        /// <param name="state">The object passed to the delegate</param>
        void RunSync(SendOrPostCallback act, object state);
    }


    /// <summary>
    /// Code contracts
    /// </summary>
    [ContractClassFor(typeof(ICustomSynchronizationContextSupplier))]
    abstract class ICustomSynchronizationContextSupplierCodeContractCheck : ICustomSynchronizationContextSupplier
    {
        /// <summary>Code contracts</summary>
        private ICustomSynchronizationContextSupplierCodeContractCheck() { }

        /// <summary>Code contracts</summary>
        public void RunAsync(SendOrPostCallback act, object state)
        {
            TurboContract.Requires(act != null, conditionString: "act != null");

            throw new NotImplementedException();
        }

        /// <summary>Code contracts</summary>
        public void RunSync(SendOrPostCallback act, object state)
        {
            TurboContract.Requires(act != null, conditionString: "act != null");

            throw new NotImplementedException();
        }
    }
}
