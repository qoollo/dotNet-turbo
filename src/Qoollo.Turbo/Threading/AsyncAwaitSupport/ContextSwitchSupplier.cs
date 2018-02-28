using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Threading
{
    /// <summary>
    /// Provide methods to switch the context of execution (usually switch a thread on which the code is executed)
    /// </summary>
    [ContractClass(typeof(IContextSwitchSupplierCodeContractCheck))]
    public interface IContextSwitchSupplier
    {
        /// <summary>
        /// Runs action in another context
        /// </summary>
        /// <param name="act">Delegate for action to be executed</param>
        /// <param name="flowContext">Whether the ExecutionContext should be flowed</param>
        void Run(Action act, bool flowContext);
        /// <summary>
        /// Runs action in another context
        /// </summary>
        /// <param name="act">Delegate for action to be executed</param>
        /// <param name="state">State object that will be passed to '<paramref name="act"/>' as argument</param>
        /// <param name="flowContext">hether the ExecutionContext should be flowed</param>
        void RunWithState(Action<object> act, object state, bool flowContext);
    }

    /// <summary>
    /// Code contracts
    /// </summary>
    [ContractClassFor(typeof(IContextSwitchSupplier))]
    abstract class IContextSwitchSupplierCodeContractCheck : IContextSwitchSupplier
    {
        /// <summary>Code contracts</summary>
        private IContextSwitchSupplierCodeContractCheck() { }

        /// <summary>Code contracts</summary>
        public void Run(Action act, bool flowContext)
        {
            TurboContract.Requires(act != null, conditionString: "act != null");

            throw new NotImplementedException();
        }

        /// <summary>Code contracts</summary>
        public void RunWithState(Action<object> act, object state, bool flowContext)
        {
            TurboContract.Requires(act != null, conditionString: "act != null");

            throw new NotImplementedException();
        }
    }
}
