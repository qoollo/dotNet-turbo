using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Qoollo.Turbo.IoC.Helpers;
using Qoollo.Turbo.IoC.Lifetime;
using System.Diagnostics;

namespace Qoollo.Turbo.IoC.Associations
{
    /// <summary>
    /// Base class for standard association containers. Stores association between 'key' and 'object-container'
    /// </summary>
    /// <typeparam name="TKey">The type of the key in association container</typeparam>
    [ContractClass(typeof(GenericAssociationContainerBaseCodeContractCheck<>))]
    public abstract class GenericAssociationContainerBase<TKey>: IAssociationSource<TKey>, IFreezable, IDisposable
    {
        private volatile bool _isFrozen = false;
        private volatile bool _isDisposed = false;

        /// <summary>
        /// Checks whether the object type is appropriate for the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="objType">Object type</param>
        /// <returns>True if the object with 'objType' can be used by container with specified 'key'</returns>
        [Pure]
        protected abstract bool IsGoodTypeForKey(TKey key, Type objType);

        /// <summary>
        /// Creates a lifetime container by object type and lifetime factory
        /// </summary>
        /// <param name="key">The key that will be used to store lifetime container inside the current AssociationContainer</param>
        /// <param name="objType">The type of the object that will be held by the lifetime container</param>
        /// <param name="val">Factory to create a lifetime container for the sepcified 'objType'</param>
        /// <returns>Created lifetime container</returns>
        protected abstract LifetimeBase ProduceResolveInfo(TKey key, Type objType, Lifetime.Factories.LifetimeFactory val);


        /// <summary>
        /// Adds a new association to the container. Should be implemented in derived type.
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="val">Lifetime object container to add</param>
        protected abstract void AddAssociationInner(TKey key, LifetimeBase val);
        /// <summary>
        /// Attempts to add a new association to the container. Should be implemented in derived type.
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="val">Lifetime object container to add</param>
        /// <returns>True if AssociationContainer not contains lifetime container with the same key; overwise false</returns>
        protected abstract bool TryAddAssociationInner(TKey key, LifetimeBase val);
        /// <summary>
        /// Adds a new association to the container. Should be implemented in derived type.
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="objType">The type of the object that will be held by the lifetime container</param>
        /// <param name="val">Factory to create a lifetime container for the sepcified 'objType'</param>
        protected abstract void AddAssociationInner(TKey key, Type objType, Qoollo.Turbo.IoC.Lifetime.Factories.LifetimeFactory val);
        /// <summary>
        /// Attempts to add a new association to the container. Should be implemented in derived type.
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="objType">The type of the object that will be held by the lifetime container</param>
        /// <param name="val">Factory to create a lifetime container for the sepcified 'objType'</param>
        /// <returns>True if AssociationContainer not contains lifetime container with the same key; overwise false</returns>
        protected abstract bool TryAddAssociationInner(TKey key, Type objType, Qoollo.Turbo.IoC.Lifetime.Factories.LifetimeFactory val);
        /// <summary>
        /// Attempts to get an association from the container by the specified key. Should be implemented in derived type.
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="val">Lifetime container for the specified key (in case of success)</param>
        /// <returns>True if the AssociationContainer contains the lifetime container for the specified key</returns>
        protected abstract bool TryGetAssociationInner(TKey key, out LifetimeBase val);
        /// <summary>
        /// Removes the association from the container for the specified key. Should be implemented in derived type.
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>True if the association was presented in container</returns>
        protected abstract bool RemoveAssociationInner(TKey key);
        /// <summary>
        /// Checks whether the AssociationContainer contains the lifetime container for the specified key. Should be implemented in derived type.
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>True if the association is presented in container</returns>
        [Pure]
        protected abstract bool ContainsInner(TKey key);


        /// <summary>
        /// Checks the state of the container and throws exception if the action cannot be performed
        /// </summary>
        /// <param name="onEdit">The action will change the container state</param>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="ObjectFrozenException"></exception>
        protected void CheckContainerState(bool onEdit)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(this.GetType().Name);

            if (onEdit && _isFrozen)
                throw new ObjectFrozenException(this.GetType().Name + " is frozen");
        }

        /// <summary>
        /// Checks the state of the container and returns the true if the action can be performed
        /// </summary>
        /// <param name="onEdit">The action will change the container state</param>
        /// <returns>True if the action can be performed</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool CheckContainerStateBool(bool onEdit)
        {
            return !(_isDisposed || (onEdit && _isFrozen));
        }

        /// <summary>
        /// Transform ObjectInstantiationMode by the specified OverrideObjectInstantiationMode
        /// </summary>
        /// <param name="src">Original ObjectInstantiationMode value</param>
        /// <param name="overrideMod">Override mode</param>
        /// <returns>Transformed instantiation mode</returns>
        protected static ObjectInstantiationMode TransformInstMode(ObjectInstantiationMode src, OverrideObjectInstantiationMode overrideMod)
        {
            switch (overrideMod)
            {
                case OverrideObjectInstantiationMode.ToSingleton:
                    return ObjectInstantiationMode.Singleton;
                case OverrideObjectInstantiationMode.ToDeferedSingleton:
                    return ObjectInstantiationMode.DeferedSingleton;
                case OverrideObjectInstantiationMode.ToPerThread:
                    return ObjectInstantiationMode.PerThread;
                case OverrideObjectInstantiationMode.ToPerCall:
                    return ObjectInstantiationMode.PerCall;
                case OverrideObjectInstantiationMode.ToPerCallInlinedParams:
                    return ObjectInstantiationMode.PerCallInlinedParams;
                case OverrideObjectInstantiationMode.None:
                    return src;
            }
            TurboContract.Assert(false, "Unknown OverrideObjectInstantiationMode: " + overrideMod.ToString());
            throw new AssociationIoCException("Unknown OverrideObjectInstantiationMode: " + overrideMod.ToString());
        }

        /// <summary>
        /// Adds a new association to the container
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="val">Lifetime object container to add</param>
        protected void AddAssociation(TKey key, LifetimeBase val)
        {
            TurboContract.Ensures(this.ContainsInner(key));

            if (key == null)
                throw new ArgumentNullException(nameof(key));
            if (val == null)
                throw new ArgumentNullException(nameof(val));

            if (!IsGoodTypeForKey(key, val.OutputType))
                throw new AssociationBadKeyForTypeException(string.Format("Bad key ({0}) for type ({1})", key, val.OutputType));

            CheckContainerState(true);

            AddAssociationInner(key, val);
        }

        /// <summary>
        /// Attempts to add a new association to the container
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="val">Lifetime object container to add</param>
        /// <returns>True if AssociationContainer not contains lifetime container with the same key; overwise false</returns>
        protected bool TryAddAssociation(TKey key, LifetimeBase val)
        {
            TurboContract.Ensures(TurboContract.Result<bool>() == false || this.ContainsInner(key));

            if (key == null)
                throw new ArgumentNullException(nameof(key));
            if (val == null)
                throw new ArgumentNullException(nameof(val));

            if (!IsGoodTypeForKey(key, val.OutputType))
                throw new AssociationBadKeyForTypeException(string.Format("Bad key ({0}) for the type ({1})", key, val.OutputType));

            CheckContainerState(true);

            return TryAddAssociationInner(key, val);
        }

        /// <summary>
        /// Adds a new association to the container. Lifetime factory used to create a specific lifetime container.
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="objType">The type of the object that will be held by the lifetime container</param>
        /// <param name="val">Factory to create a lifetime container for the sepcified 'objType'</param>
        protected void AddAssociation(TKey key, Type objType, Qoollo.Turbo.IoC.Lifetime.Factories.LifetimeFactory val)
        {
            TurboContract.Ensures(this.ContainsInner(key));

            if (key == null)
                throw new ArgumentNullException(nameof(key));
            if (objType == null)
                throw new ArgumentNullException(nameof(objType));
            if (val == null)
                throw new ArgumentNullException(nameof(val));

            if (!IsGoodTypeForKey(key, objType))
                throw new AssociationBadKeyForTypeException(string.Format("Bad key ({0}) for the type ({1})", key, objType));

            CheckContainerState(true);

            AddAssociationInner(key, objType, val);
        }

        /// <summary>
        /// Attempts to add a new association to the container. Lifetime factory used to create a specific lifetime container.
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="objType">The type of the object that will be held by the lifetime container</param>
        /// <param name="val">Factory to create a lifetime container for the sepcified 'objType'</param>
        /// <returns>True if AssociationContainer not contains lifetime container with the same key; overwise false</returns>
        protected bool TryAddAssociation(TKey key, Type objType, Qoollo.Turbo.IoC.Lifetime.Factories.LifetimeFactory val)
        {
            Contract.Requires(key != null);
            Contract.Requires(objType != null);
            Contract.Requires(val != null);
            Contract.Ensures(Contract.Result<bool>() == false || this.ContainsInner(key));

            if (key == null)
                throw new ArgumentNullException(nameof(key));
            if (objType == null)
                throw new ArgumentNullException(nameof(objType));
            if (val == null)
                throw new ArgumentNullException(nameof(val));

            if (!IsGoodTypeForKey(key, objType))
                throw new AssociationBadKeyForTypeException(string.Format("Bad key ({0}) for the type ({1})", key, objType));

            CheckContainerState(true);

            return TryAddAssociationInner(key, objType, val);
        }

        /// <summary>
        /// Removes the association from the container for the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>True if the association was presented in container</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public bool RemoveAssociation(TKey key)
        {
            Contract.Requires(key != null);
            Contract.Ensures(!this.ContainsInner(key));

            if (key == null)
                throw new ArgumentNullException(nameof(key));

            CheckContainerState(true);

            return RemoveAssociationInner(key);
        }
        /// <summary>
        /// Determines whether the AssociationContainer contains the lifetime container for the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>True if the association is presented in container</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public bool Contains(TKey key)
        {
            Contract.Requires(key != null);

            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (_isDisposed)
                return false;

            return this.ContainsInner(key);
        }


        /// <summary>
        /// Scans a range of types with attributes and execute 'procAction' for every that passed the filter
        /// </summary>
        /// <typeparam name="TAttr">The type of the attribute that is used to mark a type (should be derived from LocatorTargetObjectAttribute)</typeparam>
        /// <param name="typeSource">The sequence of all types to scan</param>
        /// <param name="attrCmpPredicate">Predicate that allows to filter out non relevant types (can be null)</param>
        /// <param name="procAction">Action executed for every type</param>
        /// <param name="multiAttr">Allows processing of multiple attributes on the same type</param>
        protected void ScanTypeRangeWithStrictAttr<TAttr>(IEnumerable<Type> typeSource, Func<TAttr, bool> attrCmpPredicate,
            Action<Type, TAttr> procAction, bool multiAttr = true)
            where TAttr : LocatorTargetObjectAttribute
        {
            Contract.Requires<ArgumentNullException>(typeSource != null);
            Contract.Requires<ArgumentNullException>(procAction != null);

            CheckContainerState(true);

            object[] attr = null;
            foreach (var curTp in typeSource)
            {
                TurboContract.Assert(curTp != null);

                attr = curTp.GetCustomAttributes(false);
                if (attr == null || attr.Length == 0)
                    continue;

                foreach (var curAttr in attr.OfType<TAttr>())
                {
                    TurboContract.Assert(curAttr != null);

                    if (attrCmpPredicate == null || attrCmpPredicate(curAttr))
                    {
                        procAction(curTp, curAttr);

                        if (!multiAttr)
                            break;
                    }
                }
            }
        }


        /// <summary>
        /// Adds a range of types with attributes to the container
        /// </summary>
        /// <typeparam name="TAttr">The type of the attribute that is used to mark a type (should be derived from LocatorTargetObjectAttribute)</typeparam>
        /// <param name="typeSource">The sequence of all types to scan and add to the container</param>
        /// <param name="attrCmpPredicate">Predicate that allows to filter out non relevant types (can be null)</param>
        /// <param name="keyGenerator">Function to create a key by the type and attribute</param>
        /// <param name="modeOver">Overrides the instantiation mode from attribute</param>
        /// <param name="multiAttr">Allows processing of multiple attributes on the same type</param>
        /// <param name="combineIfPossible">Allows to combine instances of the same type with different keys</param>
        protected void AddTypeRangeWithStrictAttrPlain<TAttr>(IEnumerable<Type> typeSource, Func<TAttr, bool> attrCmpPredicate, Func<Type, TAttr, TKey> keyGenerator, 
            OverrideObjectInstantiationMode modeOver = OverrideObjectInstantiationMode.None, bool multiAttr = true, bool combineIfPossible = true)
            where TAttr : LocatorTargetObjectAttribute
        {
            Contract.Requires<ArgumentNullException>(typeSource != null);
            Contract.Requires<ArgumentNullException>(keyGenerator != null);


            Type curAnalizeType = null;
            LifetimeBase singletonLf = null;
            LifetimeBase deferedSingletonLf = null;
            LifetimeBase perThreadLf = null;

            ScanTypeRangeWithStrictAttr<TAttr>(typeSource, attrCmpPredicate, (tp, attr) =>
                {
                    if (tp != curAnalizeType)
                    {
                        curAnalizeType = tp;
                        singletonLf = null;
                        deferedSingletonLf = null;
                        perThreadLf = null;
                    }

                    var key = keyGenerator(tp, attr);

                    ObjectInstantiationMode instMode = TransformInstMode(attr.Mode, modeOver);

                    if (combineIfPossible)
                    {
                        switch (instMode)
                        {
                            case ObjectInstantiationMode.Singleton:
                                if (singletonLf == null)
                                    singletonLf = ProduceResolveInfo(key, tp, LifetimeFactories.Singleton);
                                AddAssociation(key, singletonLf);
                                break;
                            case ObjectInstantiationMode.DeferedSingleton:
                                if (deferedSingletonLf == null)
                                    deferedSingletonLf = ProduceResolveInfo(key, tp, LifetimeFactories.DeferedSingleton);
                                AddAssociation(key, deferedSingletonLf);
                                break;
                            case ObjectInstantiationMode.PerThread:
                                if (perThreadLf == null)
                                    perThreadLf = ProduceResolveInfo(key, tp, LifetimeFactories.PerThread);
                                AddAssociation(key, perThreadLf);
                                break;
                            default:
                                AddAssociation(key, tp, LifetimeFactories.GetLifetimeFactory(instMode));
                                break;
                        }
                    }
                    else
                    {
                        AddAssociation(key, tp, LifetimeFactories.GetLifetimeFactory(instMode));
                    }
                }, multiAttr);
        }



        /// <summary>
        /// Gets the lifetime object container by its key
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>Lifetime container for the specified key</returns>
        LifetimeBase IAssociationSource<TKey>.GetAssociation(TKey key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            if (_isDisposed)
                throw new ObjectDisposedException(this.GetType().Name);
            
            LifetimeBase res = null;
            if (!TryGetAssociationInner(key, out res))
                throw new KeyNotFoundException(string.Format("Key {0} not found in Association Container", key));

            return res;
        }

        /// <summary>
        /// Attempts to get the lifetime object container by its key
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="val">Associated lifetime container (null when key not exists)</param>
        /// <returns>True if the lifetime container for the speicifed key exists in AssociatnioContainer</returns>
        bool IAssociationSource<TKey>.TryGetAssociation(TKey key, out LifetimeBase val)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (_isDisposed)
            {
                val = null;
                return false;
            }

            return TryGetAssociationInner(key, out val);
        }

        /// <summary>
        /// Determines whether the AssociationContainer contains the key
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>True if the AssociationSource contains the key</returns>
        bool IAssociationSource<TKey>.Contains(TKey key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            if (_isDisposed)
                return false;

            return ContainsInner(key);
        }


        /// <summary>
        /// Freezes the current Association Container
        /// </summary>
        public void Freeze()
        {
            _isFrozen = true;
        }

        /// <summary>
        /// Gets the value indicating whether the current container is frozen
        /// </summary>
        public bool IsFrozen
        {
            get { return _isFrozen; }
        }

        /// <summary>
        /// Gets the value indicating whether the current container is disposed
        /// </summary>
        protected bool IsDisposed
        {
            get { return _isDisposed; }
        }

        /// <summary>
        /// Cleans-up all resources
        /// </summary>
        /// <param name="isUserCall">True when called explicitly by user from Dispose method</param>
        protected virtual void Dispose(bool isUserCall)
        {
        }

        /// <summary>
        /// Cleans-up all resources
        /// </summary>
        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                Dispose(true);
                GC.SuppressFinalize(this);
            }
        }
    }





    /// <summary>
    /// Code contracts
    /// </summary>
    [ContractClassFor(typeof(GenericAssociationContainerBase<>))]
    abstract class GenericAssociationContainerBaseCodeContractCheck<T> : GenericAssociationContainerBase<T>
    {
        /// <summary>Code contracts</summary>
        private GenericAssociationContainerBaseCodeContractCheck() { }


        protected override LifetimeBase ProduceResolveInfo(T key, Type objType, Lifetime.Factories.LifetimeFactory val)
        {
            Contract.Requires((object)key != null);
            Contract.Requires(objType != null);
            Contract.Requires(val != null);
            Contract.Ensures(Contract.Result<LifetimeBase>() != null);

            throw new NotImplementedException();
        }


        protected override bool IsGoodTypeForKey(T key, Type objType)
        {
            Contract.Requires((object)key != null);
            Contract.Requires(objType != null);

            throw new NotImplementedException();
        }

        protected override void AddAssociationInner(T key, LifetimeBase val)
        {
            Contract.Requires((object)key != null);
            Contract.Requires(val != null);

            throw new NotImplementedException();
        }

        protected override bool TryAddAssociationInner(T key, LifetimeBase val)
        {
            Contract.Requires((object)key != null);
            Contract.Requires(val != null);

            throw new NotImplementedException();
        }

        protected override void AddAssociationInner(T key, Type objType, Lifetime.Factories.LifetimeFactory val)
        {
            Contract.Requires((object)key != null);
            Contract.Requires(objType != null);
            Contract.Requires(val != null);

            throw new NotImplementedException();
        }

        protected override bool TryAddAssociationInner(T key, Type objType, Lifetime.Factories.LifetimeFactory val)
        {
            Contract.Requires((object)key != null);
            Contract.Requires(objType != null);
            Contract.Requires(val != null);

            throw new NotImplementedException();
        }

        protected override bool TryGetAssociationInner(T key, out LifetimeBase val)
        {
            Contract.Requires((object)key != null);
            Contract.Ensures((Contract.Result<bool>() == true && Contract.ValueAtReturn<LifetimeBase>(out val) != null) ||
                (Contract.Result<bool>() == false && Contract.ValueAtReturn<LifetimeBase>(out val) == null));

            throw new NotImplementedException();
        }

        protected override bool RemoveAssociationInner(T key)
        {
            Contract.Requires((object)key != null);

            throw new NotImplementedException();
        }

        protected override bool ContainsInner(T key)
        {
            Contract.Requires((object)key != null);

            throw new NotImplementedException();
        }
    }
}
