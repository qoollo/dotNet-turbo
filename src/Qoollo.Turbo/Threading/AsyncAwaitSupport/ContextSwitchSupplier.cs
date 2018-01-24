using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Threading
{
    /// <summary>
    /// Поставщик смены контекста исполнения (смены потока, а не ExecutionContext)
    /// </summary>
    [ContractClass(typeof(IContextSwitchSupplierCodeContractCheck))]
    public interface IContextSwitchSupplier
    {
        /// <summary>
        /// Запустить в другом контексте
        /// </summary>
        /// <param name="act">Действие</param>
        /// <param name="flowContext">Протаскивать ли ExecutionContext</param>
        void Run(Action act, bool flowContext);
        /// <summary>
        /// Запустить в другом контексте
        /// </summary>
        /// <param name="act">Действие</param>
        /// <param name="state">Состояние</param>
        /// <param name="flowContext">Протаскивать ли ExecutionContext</param>
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
