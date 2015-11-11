using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Qoollo.Turbo.IoC.Helpers;
using Qoollo.Turbo.IoC.Lifetime;

namespace Qoollo.Turbo.IoC.Associations
{
    /// <summary>
    /// Represents the source of associations between 'key' and 'object-container'
    /// </summary>
    /// <typeparam name="TKey">The type of the key in association container</typeparam>
    [ContractClass(typeof(IAssociationSourceCodeContractCheck<>))]
    public interface IAssociationSource<TKey>
    {
        /// <summary>
        /// Gets the lifetime object container by its key
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>Lifetime container for the specified key</returns>
        LifetimeBase GetAssociation(TKey key);
        /// <summary>
        /// Attempts to get the lifetime object container by its key
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="val">Associated lifetime container (null when key not exists)</param>
        /// <returns>True if the lifetime container for the speicifed key exists in AssociatnioSource</returns>
        bool TryGetAssociation(TKey key, out LifetimeBase val);
        /// <summary>
        /// Determines whether the AssociationSource contains the key
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>True if the AssociationSource contains the key</returns>
        bool Contains(TKey key);
    }


    /// <summary>
    /// Code contracts
    /// </summary>
    [ContractClassFor(typeof(IAssociationSource<>))]
    abstract class IAssociationSourceCodeContractCheck<T> : IAssociationSource<T>
    {
        /// <summary>Code contracts</summary>
        private IAssociationSourceCodeContractCheck() { }

        /// <summary>Code contracts</summary>
        public LifetimeBase GetAssociation(T key)
        {
            Contract.Ensures(Contract.Result<LifetimeBase>() != null);

            throw new NotImplementedException();
        }

        /// <summary>Code contracts</summary>
        public bool TryGetAssociation(T key, out LifetimeBase val)
        {
            Contract.Ensures((Contract.Result<bool>() == true && Contract.ValueAtReturn<LifetimeBase>(out val) != null) ||
                (Contract.Result<bool>() == false && Contract.ValueAtReturn<LifetimeBase>(out val) == null));

            throw new NotImplementedException();
        }

        /// <summary>Code contracts</summary>
        public bool Contains(T key)
        {
            throw new NotImplementedException();
        }
    }
}
