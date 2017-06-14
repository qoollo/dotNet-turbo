using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.ObjectPools.Common
{
    /// <summary>
    /// Default implemenation of <see cref="IPoolElementOperationSource{T}"/>.
    /// Always returns 'true' in <see cref="IPoolElementOperationSource{T}.IsValid(PoolElementWrapper{T})"/> method.
    /// </summary>
    /// <typeparam name="T">The type of the ObjectPool element</typeparam>
    public class DefaultPoolElementOperationSource<T> : IPoolElementOperationSource<T>
    {
        /// <summary>
        /// Checks whether the element is valid and can be used for operations (always return true)
        /// </summary>
        /// <param name="container">Element wrapper</param>
        /// <returns>Whether the element is valid</returns>
        bool IPoolElementOperationSource<T>.IsValid(PoolElementWrapper<T> container)
        {
            return true;
        }
    }
}
