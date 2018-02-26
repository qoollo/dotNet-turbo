using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.IoC.Associations
{
    /// <summary>
    /// Stores association between Type and 'object-lifetime-container'
    /// </summary>
    public abstract class TypeStrictAssociationContainer : ConcurrentGenericAssociationContainer<Type>,
        ISingletonAssociationSupport<Type>, IDeferedSingletonAssociationSupport<Type>,
        IPerThreadAssociationSupport<Type>, IPerCallAssociationSupport<Type>, IPerCallInlinedParamsAssociationSupport<Type>,
        IDirectSingletonAssociationSupport<Type>, ICustomAssociationSupport<Type>
    {
        /// <summary>
        /// Adds an object with singleton lifetime for the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="val">The object that will be held by the singleton lifetime container</param>
        [Obsolete("Do not use this method. If it is required, it should be implemented in the derived container.")]
        public void AddSingleton(Type key, object val)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            base.AddAssociation(key, new Lifetime.SingletonLifetime(val));
        }
        /// <summary>
        /// Adds an object with singleton lifetime for the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="val">The object that will be held by the singleton lifetime container</param>
        /// <param name="disposeWithContainer">Indicates whether the lifetime container should also dispose the containing object</param>
        [Obsolete("Do not use this method. If it is required, it should be implemented in the derived container.")]
        public void AddSingleton(Type key, object val, bool disposeWithContainer)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            if (val == null)
                throw new ArgumentNullException(nameof(val));

            base.AddAssociation(key, new Lifetime.SingletonLifetime(val, disposeWithContainer));
        }

        /// <summary>
        /// Attempts to add an object with singleton lifetime for the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="val">The object that will be held by the singleton lifetime container</param>
        /// <returns>True if the association was added successfully (that is AssociationContainer did not contained lifetime container with the same key); overwise false</returns>
        [Obsolete("Do not use this method. If it is required, it should be implemented in the derived container.")]
        public bool TryAddSingleton(Type key, object val)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            return base.TryAddAssociation(key, new Lifetime.SingletonLifetime(val));
        }
        /// <summary>
        /// Attempts to add an object with singleton lifetime for the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="val">The object that will be held by the singleton lifetime container</param>
        /// <param name="disposeWithContainer">Indicates whether the lifetime container should also dispose the containing object</param>
        /// <returns>True if the association was added successfully (that is AssociationContainer did not contained lifetime container with the same key); overwise false</returns>
        [Obsolete("Do not use this method. If it is required, it should be implemented in the derived container.")]
        public bool TryAddSingleton(Type key, object val, bool disposeWithContainer)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            if (val == null)
                throw new ArgumentNullException(nameof(val));

            return base.TryAddAssociation(key, new Lifetime.SingletonLifetime(val, disposeWithContainer));
        }




        /// <summary>
        /// Adds a lifetime object container for the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="lifetimeContainer">Lifetime object container to add</param>
        public new void AddAssociation(Type key, Lifetime.LifetimeBase lifetimeContainer)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            if (lifetimeContainer == null)
                throw new ArgumentNullException(nameof(lifetimeContainer));

            base.AddAssociation(key, lifetimeContainer);
        }
        /// <summary>
        /// Adds a lifetime object container created by the 'factory' for the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="objType">The type of the object that will be held by the lifetime container</param>
        /// <param name="factory">Factory to create a lifetime container for the sepcified 'objType'</param>
        public new void AddAssociation(Type key, Type objType, Lifetime.Factories.LifetimeFactory factory)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            if (objType == null)
                throw new ArgumentNullException(nameof(objType));
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            base.AddAssociation(key, objType, factory);
        }

        /// <summary>
        /// Attempts to add a lifetime object container for the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="lifetimeContainer">Lifetime object container to add</param>
        /// <returns>True if the association was added successfully (that is AssociationContainer did not contained lifetime container with the same key); overwise false</returns>
        public new bool TryAddAssociation(Type key, Lifetime.LifetimeBase lifetimeContainer)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            if (lifetimeContainer == null)
                throw new ArgumentNullException(nameof(lifetimeContainer));

            return base.TryAddAssociation(key, lifetimeContainer);
        }
        /// <summary>
        /// Attempts to add a lifetime object container created by the 'factory' for the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="objType">The type of the object that will be held by the lifetime container</param>
        /// <param name="factory">Factory to create a lifetime container for the sepcified 'objType'</param>
        /// <returns>True if the association was added successfully (that is AssociationContainer did not contained lifetime container with the same key); overwise false</returns>
        public new bool TryAddAssociation(Type key, Type objType, Lifetime.Factories.LifetimeFactory factory)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            if (objType == null)
                throw new ArgumentNullException(nameof(objType));
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            return base.TryAddAssociation(key, objType, factory);
        }



        /// <summary>
        /// Adds an object with singleton lifetime for the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="objType">The type of the object that will be held by the singleton lifetime container</param>
        public void AddSingleton(Type key, Type objType)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            if (objType == null)
                throw new ArgumentNullException(nameof(objType));

            base.AddAssociation(key, objType, LifetimeFactories.Singleton);
        }
        /// <summary>
        /// Attempts to add an object with singleton lifetime for the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="objType">The type of the object that will be held by the singleton lifetime container</param>
        /// <returns>True if the association was added successfully (that is AssociationContainer did not contained lifetime container with the same key); overwise false</returns>
        public bool TryAddSingleton(Type key, Type objType)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            if (objType == null)
                throw new ArgumentNullException(nameof(objType));

            return base.TryAddAssociation(key, objType, LifetimeFactories.Singleton);
        }



        /// <summary>
        /// Adds an object with lazily initialized singleton lifetime for the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="objType">The type of the object that will be held by the defered singleton lifetime container</param>
        public void AddDeferedSingleton(Type key, Type objType)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            if (objType == null)
                throw new ArgumentNullException(nameof(objType));

            base.AddAssociation(key, objType, LifetimeFactories.DeferedSingleton);
        }
        /// <summary>
        /// Attempts to add an object with lazily initialized singleton lifetime for the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="objType">The type of the object that will be held by the defered singleton lifetime container</param>
        /// <returns>True if the association was added successfully (that is AssociationContainer did not contained lifetime container with the same key); overwise false</returns>
        public bool TryAddDeferedSingleton(Type key, Type objType)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            if (objType == null)
                throw new ArgumentNullException(nameof(objType));

            return base.TryAddAssociation(key, objType, LifetimeFactories.DeferedSingleton);
        }



        /// <summary>
        /// Adds an object with per thread lifetime for the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="objType">The type of the object that will be held by the PerThread lifetime container</param>
        public void AddPerThread(Type key, Type objType)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            if (objType == null)
                throw new ArgumentNullException(nameof(objType));

            base.AddAssociation(key, objType, LifetimeFactories.PerThread);
        }
        /// <summary>
        /// Attempts to add an object with per thread lifetime for the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="objType">The type of the object that will be held by the PerThread lifetime container</param>
        /// <returns>True if the association was added successfully (that is AssociationContainer did not contained lifetime container with the same key); overwise false</returns>
        public bool TryAddPerThread(Type key, Type objType)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            if (objType == null)
                throw new ArgumentNullException(nameof(objType));

            return base.TryAddAssociation(key, objType, LifetimeFactories.PerThread);
        }



        /// <summary>
        /// Adds an object with per call lifetime for the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="objType">The type of the object that will be held by the PerCall lifetime container</param>
        public void AddPerCall(Type key, Type objType)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            if (objType == null)
                throw new ArgumentNullException(nameof(objType));

            base.AddAssociation(key, objType, LifetimeFactories.PerCall);
        }
        /// <summary>
        /// Attempts to add an object with per call lifetime for the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="objType">The type of the object that will be held by the PerCall lifetime container</param>
        /// <returns>True if the association was added successfully (that is AssociationContainer did not contained lifetime container with the same key); overwise false</returns>
        public bool TryAddPerCall(Type key, Type objType)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            if (objType == null)
                throw new ArgumentNullException(nameof(objType));

            return base.TryAddAssociation(key, objType, LifetimeFactories.PerCall);
        }



        /// <summary>
        /// Adds an object with per call lifetime with inlined constructor parameters for the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="objType">The type of the object that will be held by the PerCallInlinedParams lifetime container</param>
        public void AddPerCallInlinedParams(Type key, Type objType)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            if (objType == null)
                throw new ArgumentNullException(nameof(objType));

            base.AddAssociation(key, objType, LifetimeFactories.PerCallInlinedParams);
        }
        /// <summary>
        /// Attempts to add an object with per call lifetime with inlined constructor parameters for the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="objType">The type of the object that will be held by the PerCallInlinedParams lifetime container</param>
        /// <returns>True if the association was added successfully (that is AssociationContainer did not contained lifetime container with the same key); overwise false</returns>
        public bool TryAddPerCallInlinedParams(Type key, Type objType)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            if (objType == null)
                throw new ArgumentNullException(nameof(objType));

            return base.TryAddAssociation(key, objType, LifetimeFactories.PerCallInlinedParams);
        }
    }
}
