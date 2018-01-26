using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Qoollo.Turbo.IoC.Helpers;
using Qoollo.Turbo.IoC.Lifetime;

namespace Qoollo.Turbo.IoC.Associations
{
    /// <summary>
    /// Stores association between custom 'key' and 'object-lifetime-container'.
    /// </summary>
    /// <typeparam name="TKey">The type of the key in association container</typeparam>
    [ContractClass(typeof(ConcurrentGenericAssociationContainerCodeContractCheck<>))]
    public abstract class ConcurrentGenericAssociationContainer<TKey> : GenericAssociationContainerBase<TKey>, IAssociationSource<TKey>
    {
        private readonly ConcurrentDictionary<TKey, LifetimeBase> _storage;


        [ContractInvariantMethod]
        private void Invariant()
        {
            TurboContract.Invariant(_storage != null);
        }

        /// <summary>
        /// ConcurrentGenericAssociationContainer constructor
        /// </summary>
        public ConcurrentGenericAssociationContainer()
        {
            _storage = new ConcurrentDictionary<TKey, LifetimeBase>();
        }

        /// <summary>
        /// Adds a new association to the container
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="val">Lifetime object container to add</param>
        protected sealed override void AddAssociationInner(TKey key, Lifetime.LifetimeBase val)
        {
            TurboContract.Requires(key != null, conditionString: "key != null");
            TurboContract.Requires(val != null, conditionString: "val != null");

            if (!_storage.TryAdd(key, val))
                throw new ItemAlreadyExistsException(string.Format("AssociationContainer already contains the association for the key ({0})", key));
        }
        /// <summary>
        /// Attempts to add a new association to the container
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="val">Lifetime object container to add</param>
        /// <returns>True if AssociationContainer not contains lifetime container with the same key; overwise false</returns>
        protected sealed override bool TryAddAssociationInner(TKey key, Lifetime.LifetimeBase val)
        {
            TurboContract.Requires(key != null, conditionString: "key != null");
            TurboContract.Requires(val != null, conditionString: "val != null");

            return _storage.TryAdd(key, val);
        }
        /// <summary>
        /// Adds a new association to the container
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="objType">The type of the object that will be held by the lifetime container</param>
        /// <param name="val">Factory to create a lifetime container for the sepcified 'objType'</param>
        protected sealed override void AddAssociationInner(TKey key, Type objType, Lifetime.Factories.LifetimeFactory val)
        {
            TurboContract.Requires(key != null, conditionString: "key != null");
            TurboContract.Requires(objType != null, conditionString: "objType != null");
            TurboContract.Requires(val != null, conditionString: "val != null");

            var lfInf = ProduceResolveInfo(key, objType, val);
            if (!_storage.TryAdd(key, lfInf))
                throw new ItemAlreadyExistsException(string.Format("AssociationContainer already contains the association for the key ({0})", key));
        }
        /// <summary>
        /// Attempts to add a new association to the container
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="objType">The type of the object that will be held by the lifetime container</param>
        /// <param name="val">Factory to create a lifetime container for the sepcified 'objType'</param>
        /// <returns>True if AssociationContainer not contains lifetime container with the same key; overwise false</returns>
        protected sealed override bool TryAddAssociationInner(TKey key, Type objType, Lifetime.Factories.LifetimeFactory val)
        {
            TurboContract.Requires(key != null, conditionString: "key != null");
            TurboContract.Requires(objType != null, conditionString: "objType != null");
            TurboContract.Requires(val != null, conditionString: "val != null");

            if (_storage.ContainsKey(key))
                return false;

            var lfInf = ProduceResolveInfo(key, objType, val);
            return _storage.TryAdd(key, lfInf);
        }


        /// <summary>
        /// Attempts to get an association from the container by the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="val">Lifetime container for the specified key (in case of success)</param>
        /// <returns>True if the AssociationContainer contains the lifetime container for the specified key</returns>
        protected sealed override bool TryGetAssociationInner(TKey key, out Lifetime.LifetimeBase val)
        {
            TurboContract.Requires(key != null, conditionString: "key != null");

            return _storage.TryGetValue(key, out val);
        }
        /// <summary>
        /// Removes the association from the container for the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>True if the association was presented in container</returns>
        protected sealed override bool RemoveAssociationInner(TKey key)
        {
            TurboContract.Requires(key != null, conditionString: "key != null");

            return _storage.TryRemove(key, out LifetimeBase tmp);
        }
        /// <summary>
        /// Checks whether the AssociationContainer contains the lifetime container for the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>True if the association is presented in container</returns>
        protected sealed override bool ContainsInner(TKey key)
        {
            TurboContract.Requires(key != null, conditionString: "key != null");

            return _storage.ContainsKey(key);
        }

        /// <summary>
        /// Determines whether the AssociationContainer contains the lifetime container for the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>True if the association is presented in container</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public new bool Contains(TKey key)
        {
            return _storage.ContainsKey(key);
        }

        /// <summary>
        /// Gets the lifetime object container by its key
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>Lifetime container for the specified key</returns>
        LifetimeBase IAssociationSource<TKey>.GetAssociation(TKey key)
        {
            return _storage[key];
        }

        /// <summary>
        /// Attempts to get the lifetime object container by its key
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="val">Associated lifetime container (null when key not exists)</param>
        /// <returns>True if the lifetime container for the speicifed key exists in AssociatnioContainer</returns>
        bool IAssociationSource<TKey>.TryGetAssociation(TKey key, out LifetimeBase val)
        {
            return _storage.TryGetValue(key, out val);
        }

        /// <summary>
        /// Determines whether the AssociationContainer contains the key
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>True if the AssociationSource contains the key</returns>
        bool IAssociationSource<TKey>.Contains(TKey key)
        {
            return _storage.ContainsKey(key);
        }

        /// <summary>
        /// Gets the lifetime object container by its key
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>Lifetime container for the specified key</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="KeyNotFoundException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal LifetimeBase GetAssociation(TKey key)
        {
            return _storage[key];
        }

        /// <summary>
        /// Attempts to get the lifetime object container by its key
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="val">Associated lifetime container (null when key not exists)</param>
        /// <returns>True if the lifetime container for the speicifed key exists in AssociatnioContainer</returns>
        /// <exception cref="ArgumentNullException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool TryGetAssociation(TKey key, out LifetimeBase val)
        {
            return _storage.TryGetValue(key, out val);
        }


        /// <summary>
        /// Cleans-up all resources
        /// </summary>
        /// <param name="isUserCall">True when called explicitly by user from Dispose method</param>
        protected override void Dispose(bool isUserCall)
        {
            if (isUserCall)
            {
                var toDispose = _storage.ToArray();
                _storage.Clear();

                for (int i = 0; i < toDispose.Length; i++)
                    toDispose[i].Value.Dispose();
            }
            base.Dispose(isUserCall);
        }
    }



    /// <summary>
    /// Code contracts
    /// </summary>
    [ContractClassFor(typeof(ConcurrentGenericAssociationContainer<>))]
    abstract class ConcurrentGenericAssociationContainerCodeContractCheck<T> : ConcurrentGenericAssociationContainer<T>
    {
        /// <summary>Code contracts</summary>
        private ConcurrentGenericAssociationContainerCodeContractCheck() { }


        protected override LifetimeBase ProduceResolveInfo(T key, Type objType, Lifetime.Factories.LifetimeFactory val)
        {
            throw new NotImplementedException();
        }

        protected override bool IsGoodTypeForKey(T key, Type objType)
        {
            throw new NotImplementedException();
        }
    }
}
