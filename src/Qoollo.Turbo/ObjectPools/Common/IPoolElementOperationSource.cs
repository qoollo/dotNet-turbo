using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.ObjectPools.Common
{
    /// <summary>
    /// Операции для элемента, предоставляемые пулом
    /// </summary>
    /// <typeparam name="T">Тип элемента</typeparam>
    [ContractClass(typeof(IPoolElementOperationSourceCodeContract<>))]
    public interface IPoolElementOperationSource<T>
    {
        /// <summary>
        /// Является ли элемент валидным
        /// </summary>
        /// <param name="container">Контейнер элемента</param>
        /// <returns>Является ли валидным</returns>
        bool IsValid(PoolElementWrapper<T> container);
    }



    [ContractClassFor(typeof(IPoolElementOperationSource<>))]
    internal abstract class IPoolElementOperationSourceCodeContract<T>: IPoolElementOperationSource<T>
    {

        public bool IsValid(PoolElementWrapper<T> container)
        {
            Contract.Requires(container != null);

            throw new NotImplementedException();
        }
    }
}
