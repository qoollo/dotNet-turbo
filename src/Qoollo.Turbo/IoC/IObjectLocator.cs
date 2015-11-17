using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.IoC
{
    /// <summary>
    /// Interface for the IoC containers that declares methods to resolve the objects
    /// </summary>
    /// <typeparam name="TKey">The type of the key in object locator</typeparam>
    [ContractClass(typeof(IObjectLocatorCodeContractCheck<>))]
    public interface IObjectLocator<TKey>
    {
        /// <summary>
        /// Resolves object from the container for the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>Resolved object</returns>
        object Resolve(TKey key);
        /// <summary>
        /// Attempts to resolve object from the container for the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="val">Resolved object</param>
        /// <returns>True if the resolution succeeded; overwise false</returns>
        bool TryResolve(TKey key, out object val);
        /// <summary>
        /// Determines whether the object can be resolved by the container for the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>True if the object can be resolved</returns>
        bool CanResolve(TKey key);
    }


    /// <summary>
    /// Code contracts
    /// </summary>
    [ContractClassFor(typeof(IObjectLocator<>))]
    abstract class IObjectLocatorCodeContractCheck<T> : IObjectLocator<T>
    {
        /// <summary>Code contracts</summary>
        private IObjectLocatorCodeContractCheck() { }


        public object Resolve(T key)
        {
            Contract.Requires(key != null);

            throw new NotImplementedException();
        }

        public bool TryResolve(T key, out object val)
        {
            Contract.Requires(key != null);
            Contract.Ensures(Contract.Result<bool>() == true || (Contract.Result<bool>() == false && Contract.ValueAtReturn<object>(out val) == null));

            throw new NotImplementedException();
        }

        public bool CanResolve(T key)
        {
            Contract.Requires(key != null);

            throw new NotImplementedException();
        }
    }
}
