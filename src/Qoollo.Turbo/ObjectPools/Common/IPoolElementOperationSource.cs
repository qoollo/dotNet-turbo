using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.ObjectPools.Common
{
    /// <summary>
    /// Additional operations on the element that should be supported by the concrete ObjectPool implementation
    /// </summary>
    /// <typeparam name="T">The type of the ObjectPool element</typeparam>
    [ContractClass(typeof(IPoolElementOperationSourceCodeContract<>))]
    public interface IPoolElementOperationSource<T>
    {
        /// <summary>
        /// Checks whether the element is valid and can be used for operations
        /// </summary>
        /// <param name="container">Element wrapper</param>
        /// <returns>Whether the element is valid</returns>
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
