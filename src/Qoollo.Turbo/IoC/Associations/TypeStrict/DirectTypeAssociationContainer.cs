using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Qoollo.Turbo.IoC.ServiceStuff;

namespace Qoollo.Turbo.IoC.Associations
{
    /// <summary>
    /// Stores association between Type and 'object-lifetime-container' that helds the object with the same or derived Type.
    /// </summary>
    public abstract class DirectTypeAssociationContainer : TypeStrictAssociationContainer, IDirectSingletonAssociationSupport<Type>
    {
        /// <summary>
        /// Checks whether the object type is appropriate for the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="objType">Object type</param>
        /// <returns>True if the object with 'objType' can be used by container with specified 'key'</returns>
        protected override bool IsGoodTypeForKey(Type key, Type objType)
        {
            TurboContract.Requires(key != null, conditionString: "key != null");
            TurboContract.Requires(objType != null, conditionString: "objType != null");

            return (key == objType) || (key.IsAssignableFrom(objType));
        }

        /// <summary>
        /// Determines whether the AssociationContainer contains the lifetime container for the specified key
        /// </summary>
        /// <typeparam name="TKey">Key</typeparam>
        /// <returns>True if the association is presented in container</returns>
        public bool Contains<TKey>()
        {
            return this.Contains(typeof(TKey));
        }

        /// <summary>
        /// Removes the association from the container for the specified key
        /// </summary>
        /// <typeparam name="TKey">Key</typeparam>
        /// <returns>True if the association was presented in container</returns>
        public bool RemoveAssociation<TKey>()
        {
            return this.RemoveAssociation(typeof(TKey));
        }


        /// <summary>
        /// Adds an object with singleton lifetime for the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="val">The object that will be held by the singleton lifetime container</param>
        public new void AddSingleton(Type key, object val)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (val == null) 
                base.AddAssociation(key, new Lifetime.SingletonLifetime(val, key));
            else             
                base.AddAssociation(key, new Lifetime.SingletonLifetime(val));
        }
        /// <summary>
        /// Adds an object with singleton lifetime for the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="val">The object that will be held by the singleton lifetime container</param>
        /// <param name="disposeWithContainer">Indicates whether the lifetime container should also dispose the containing object</param>
        public new void AddSingleton(Type key, object val, bool disposeWithContainer)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (val == null)
                base.AddAssociation(key, new Lifetime.SingletonLifetime(val, key, disposeWithContainer));
            else
                base.AddAssociation(key, new Lifetime.SingletonLifetime(val, disposeWithContainer));
        }

        /// <summary>
        /// Attempts to add an object with singleton lifetime for the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="val">The object that will be held by the singleton lifetime container</param>
        /// <returns>True if the association was added successfully (that is AssociationContainer did not contained lifetime container with the same key); overwise false</returns>
        public new bool TryAddSingleton(Type key, object val)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (val == null)
                return base.TryAddAssociation(key, new Lifetime.SingletonLifetime(val, key));
            else
                return base.TryAddAssociation(key, new Lifetime.SingletonLifetime(val));
        }
        /// <summary>
        /// Attempts to add an object with singleton lifetime for the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="val">The object that will be held by the singleton lifetime container</param>
        /// <param name="disposeWithContainer">Indicates whether the lifetime container should also dispose the containing object</param>
        /// <returns>True if the association was added successfully (that is AssociationContainer did not contained lifetime container with the same key); overwise false</returns>
        public new bool TryAddSingleton(Type key, object val, bool disposeWithContainer)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (val == null)
                return base.TryAddAssociation(key, new Lifetime.SingletonLifetime(val, key, disposeWithContainer));
            else
                return base.TryAddAssociation(key, new Lifetime.SingletonLifetime(val, disposeWithContainer));
        }


        /// <summary>
        /// Adds an object with singleton lifetime for the specified key
        /// </summary>
        /// <typeparam name="TType">The type of the object that will be held by the singleton lifetime container</typeparam>
        /// <param name="val">The object that will be held by the singleton lifetime container</param>
        public void AddSingleton<TType>(TType val)
        {
            this.AddSingleton(typeof(TType), (object)val);
        }

        /// <summary>
        /// Adds an object with singleton lifetime for the specified key
        /// </summary>
        /// <typeparam name="TType">The type of the object that will be held by the singleton lifetime container</typeparam>
        /// <param name="val">The object that will be held by the singleton lifetime container</param>
        /// <param name="disposeWithContainer">Indicates whether the lifetime container should also dispose the containing object</param>
        public void AddSingleton<TType>(TType val, bool disposeWithContainer)
        {
            this.AddSingleton(typeof(TType), (object)val, disposeWithContainer);
        }

        /// <summary>
        /// Attempts to add an object with singleton lifetime for the specified key
        /// </summary>
        /// <typeparam name="TType">The type of the object that will be held by the singleton lifetime container</typeparam>
        /// <param name="val">>The object that will be held by the singleton lifetime container</param>
        /// <returns>True if the association was added successfully (that is AssociationContainer did not contained lifetime container with the same key); overwise false</returns>
        public bool TryAddSingleton<TType>(TType val)
        {
            return this.TryAddSingleton(typeof(TType), (object)val);
        }

        /// <summary>
        /// Attempts to add an object with singleton lifetime for the specified key
        /// </summary>
        /// <typeparam name="TType">The type of the object that will be held by the singleton lifetime container</typeparam>
        /// <param name="val">>The object that will be held by the singleton lifetime container</param>
        /// <param name="disposeWithContainer">Indicates whether the lifetime container should also dispose the containing object</param>
        /// <returns>True if the association was added successfully (that is AssociationContainer did not contained lifetime container with the same key); overwise false</returns>
        public bool TryAddSingleton<TType>(TType val, bool disposeWithContainer)
        {
            return this.TryAddSingleton(typeof(TType), (object)val, disposeWithContainer);
        }



        /// <summary>
        /// Adds a lifetime object container for the specified key
        /// </summary>
        /// <typeparam name="TKey">The type that will be used as a key</typeparam>
        /// <param name="lifetimeContainer">Lifetime object container to add</param>
        public void AddAssociation<TKey>(Lifetime.LifetimeBase lifetimeContainer)
        {
            if (lifetimeContainer == null)
                throw new ArgumentNullException(nameof(lifetimeContainer));

            base.AddAssociation(typeof(TKey), lifetimeContainer);
        }

        /// <summary>
        /// Adds a lifetime object container created by the 'factory' for the specified key
        /// </summary>
        /// <typeparam name="TKey">The type that will be used as a key</typeparam>
        /// <param name="objType">The type of the object that will be held by the lifetime container</param>
        /// <param name="factory">Factory to create a lifetime container for the sepcified 'objType'</param>
        public void AddAssociation<TKey>(Type objType, Lifetime.Factories.LifetimeFactory factory)
        {
            if (objType == null)
                throw new ArgumentNullException(nameof(objType));
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            base.AddAssociation(typeof(TKey), objType, factory);
        }

        /// <summary>
        /// Attemts to add a lifetime object container for the specified key
        /// </summary>
        /// <typeparam name="TKey">The type that will be used as a key</typeparam>
        /// <param name="lifetimeContainer">Lifetime object container to add</param>
        /// <returns>True if the association was added successfully (that is AssociationContainer did not contained lifetime container with the same key); overwise false</returns>
        public bool TryAddAssociation<TKey>(Lifetime.LifetimeBase lifetimeContainer)
        {
            if (lifetimeContainer == null)
                throw new ArgumentNullException(nameof(lifetimeContainer));

            return base.TryAddAssociation(typeof(TKey), lifetimeContainer);
        }

        /// <summary>
        /// Attempts to add a lifetime object container created by the 'factory' for the specified key
        /// </summary>
        /// <typeparam name="TKey">The type that will be used as a key</typeparam>
        /// <param name="objType">The type of the object that will be held by the lifetime container</param>
        /// <param name="factory">Factory to create a lifetime container for the sepcified 'objType'</param>
        /// <returns>True if the association was added successfully (that is AssociationContainer did not contained lifetime container with the same key); overwise false</returns>
        public bool TryAddAssociation<TKey>(Type objType, Lifetime.Factories.LifetimeFactory factory)
        {
            if (objType == null)
                throw new ArgumentNullException(nameof(objType));
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            return base.TryAddAssociation(typeof(TKey), objType, factory);
        }

        /// <summary>
        /// Adds a lifetime object container created by the 'factory' for the specified key
        /// </summary>
        /// <typeparam name="TKey">The type that will be used as a key</typeparam>
        /// <typeparam name="TValue">The type of the object that will be held by the lifetime container</typeparam>
        /// <param name="factory">Factory to create a lifetime container for the sepcified 'objType'</param>
        public void AddAssociation<TKey, TValue>(Lifetime.Factories.LifetimeFactory factory)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            base.AddAssociation(typeof(TKey), typeof(TValue), factory);
        }

        /// <summary>
        /// Attempts to add a lifetime object container created by the 'factory' for the specified k
        /// </summary>
        /// <typeparam name="TKey">The type that will be used as a key</typeparam>
        /// <typeparam name="TValue">The type of the object that will be held by the lifetime container</typeparam>
        /// <param name="factory">Factory to create a lifetime container for the sepcified 'objType'</param>
        /// <returns>True if the association was added successfully (that is AssociationContainer did not contained lifetime container with the same key); overwise false</returns>
        public bool TryAddAssociation<TKey, TValue>(Lifetime.Factories.LifetimeFactory factory)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            return base.TryAddAssociation(typeof(TKey), typeof(TValue), factory);
        }









        /// <summary>
        /// Adds an object with singleton lifetime for the specified key
        /// </summary>
        /// <typeparam name="TKey">The type that will be used as a key</typeparam>
        /// <param name="objType">Тип инстанцируемого объекта</param>
        public void AddSingleton<TKey>(Type objType)
        {
            if (objType == null)
                throw new ArgumentNullException(nameof(objType));

            base.AddAssociation(typeof(TKey), objType, LifetimeFactories.Singleton);
        }

        /// <summary>
        /// Attempts to add an object with singleton lifetime for the specified key
        /// </summary>
        /// <typeparam name="TKey">The type that will be used as a key</typeparam>
        /// <param name="objType">The type of the object that will be held by the singleton lifetime container</param>
        /// <returns>True if the association was added successfully (that is AssociationContainer did not contained lifetime container with the same key); overwise false</returns>
        public bool TryAddSingleton<TKey>(Type objType)
        {
            if (objType == null)
                throw new ArgumentNullException(nameof(objType));

            return base.TryAddAssociation(typeof(TKey), objType, LifetimeFactories.Singleton);
        }

        /// <summary>
        /// Adds an object with singleton lifetime for the specified key
        /// </summary>
        /// <typeparam name="TKey">The type that will be used as a key</typeparam>
        /// <typeparam name="TValue">The type of the object that will be held by the singleton lifetime container</typeparam>
        public void AddSingleton<TKey, TValue>()
            where TValue : TKey
        {
            base.AddAssociation(typeof(TKey), typeof(TValue), LifetimeFactories.Singleton);
        }

        /// <summary>
        /// Attempts to add an object with singleton lifetime for the specified key
        /// </summary>
        /// <typeparam name="TKey">The type that will be used as a key</typeparam>
        /// <typeparam name="TValue">The type of the object that will be held by the singleton lifetime container</typeparam>
        /// <returns>True if the association was added successfully (that is AssociationContainer did not contained lifetime container with the same key); overwise false</returns>
        public bool TryAddSingleton<TKey, TValue>()
            where TValue : TKey
        {
            return base.TryAddAssociation(typeof(TKey), typeof(TValue), LifetimeFactories.Singleton);
        }

        /// <summary>
        /// Adds an object with lazily initialized singleton lifetime for the specified key
        /// </summary>
        /// <typeparam name="TKey">The type that will be used as a key</typeparam>
        /// <param name="objType">The type of the object that will be held by the singleton lifetime container</param>
        public void AddDeferedSingleton<TKey>(Type objType)
        {
            if (objType == null)
                throw new ArgumentNullException(nameof(objType));

            base.AddAssociation(typeof(TKey), objType, LifetimeFactories.DeferedSingleton);
        }

        /// <summary>
        /// Attempts to add an object with lazily initialized singleton lifetime for the specified key
        /// </summary>
        /// <typeparam name="TKey">The type that will be used as a key</typeparam>
        /// <param name="objType">The type of the object that will be held by the DeferedSingleton lifetime container</param>
        /// <returns>True if the association was added successfully (that is AssociationContainer did not contained lifetime container with the same key); overwise false</returns>
        public bool TryAddDeferedSingleton<TKey>(Type objType)
        {
            if (objType == null)
                throw new ArgumentNullException(nameof(objType));

            return base.TryAddAssociation(typeof(TKey), objType, LifetimeFactories.DeferedSingleton);
        }

        /// <summary>
        /// Adds an object with lazily initialized singleton lifetime for the specified key
        /// </summary>
        /// <typeparam name="TKey">The type that will be used as a key</typeparam>
        /// <typeparam name="TValue">The type of the object that will be held by the DeferedSingleton lifetime container</typeparam>
        public void AddDeferedSingleton<TKey, TValue>()
            where TValue : TKey
        {
            base.AddAssociation(typeof(TKey), typeof(TValue), LifetimeFactories.DeferedSingleton);
        }

        /// <summary>
        /// Attempts to add an object with lazily initialized singleton lifetime for the specified key
        /// </summary>
        /// <typeparam name="TKey">The type that will be used as a key</typeparam>
        /// <typeparam name="TValue">The type of the object that will be held by the DeferedSingleton lifetime container</typeparam>
        /// <returns>True if the association was added successfully (that is AssociationContainer did not contained lifetime container with the same key); overwise false</returns>
        public bool TryAddDeferedSingleton<TKey, TValue>()
            where TValue : TKey
        {
            return base.TryAddAssociation(typeof(TKey), typeof(TValue), LifetimeFactories.DeferedSingleton);
        }

        /// <summary>
        /// Adds an object with per thread lifetime for the specified key
        /// </summary>
        /// <typeparam name="TKey">The type that will be used as a key</typeparam>
        /// <param name="objType">The type of the object that will be held by the PerThread lifetime container</param>
        public void AddPerThread<TKey>(Type objType)
        {
            if (objType == null)
                throw new ArgumentNullException(nameof(objType));

            base.AddAssociation(typeof(TKey), objType, LifetimeFactories.PerThread);
        }

        /// <summary>
        /// Attempts to add an object with per thread lifetime for the specified key
        /// </summary>
        /// <typeparam name="TKey">The type that will be used as a key</typeparam>
        /// <param name="objType">The type of the object that will be held by the PerThread lifetime container</param>
        /// <returns>True if the association was added successfully (that is AssociationContainer did not contained lifetime container with the same key); overwise false</returns>
        public bool TryAddPerThread<TKey>(Type objType)
        {
            if (objType == null)
                throw new ArgumentNullException(nameof(objType));

            return base.TryAddAssociation(typeof(TKey), objType, LifetimeFactories.PerThread);
        }

        /// <summary>
        /// Adds an object with per thread lifetime for the specified key
        /// </summary>
        /// <typeparam name="TKey">The type that will be used as a key</typeparam>
        /// <typeparam name="TValue">The type of the object that will be held by the PerThread lifetime container</typeparam>
        public void AddPerThread<TKey, TValue>()
            where TValue : TKey
        {
            base.AddAssociation(typeof(TKey), typeof(TValue), LifetimeFactories.PerThread);
        }

        /// <summary>
        /// Attempts to add an object with per thread lifetime for the specified key
        /// </summary>
        /// <typeparam name="TKey">The type that will be used as a key</typeparam>
        /// <typeparam name="TValue">The type of the object that will be held by the PerThread lifetime container</typeparam>
        /// <returns>True if the association was added successfully (that is AssociationContainer did not contained lifetime container with the same key); overwise false</returns>
        public bool TryAddPerThread<TKey, TValue>()
            where TValue : TKey
        {
            return base.TryAddAssociation(typeof(TKey), typeof(TValue), LifetimeFactories.PerThread);
        }

        /// <summary>
        /// Adds an object with per call lifetime for the specified key
        /// </summary>
        /// <typeparam name="TKey">The type that will be used as a key</typeparam>
        /// <param name="objType">The type of the object that will be held by the PerCall lifetime container</param>
        public void AddPerCall<TKey>(Type objType)
        {
            if (objType == null)
                throw new ArgumentNullException(nameof(objType));

            base.AddAssociation(typeof(TKey), objType, LifetimeFactories.PerCall);
        }

        /// <summary>
        /// Attempts to add an object with per call lifetime for the specified key
        /// </summary>
        /// <typeparam name="TKey">The type that will be used as a key</typeparam>
        /// <param name="objType">The type of the object that will be held by the PerCall lifetime container</param>
        /// <returns>True if the association was added successfully (that is AssociationContainer did not contained lifetime container with the same key); overwise false</returns>
        public bool TryAddPerCall<TKey>(Type objType)
        {
            if (objType == null)
                throw new ArgumentNullException(nameof(objType));

            return base.TryAddAssociation(typeof(TKey), objType, LifetimeFactories.PerCall);
        }

        /// <summary>
        /// Adds an object with per call lifetime for the specified key
        /// </summary>
        /// <typeparam name="TKey">The type that will be used as a key</typeparam>
        /// <typeparam name="TValue">The type of the object that will be held by the PerCall lifetime container</typeparam>
        public void AddPerCall<TKey, TValue>()
            where TValue : TKey
        {
            base.AddAssociation(typeof(TKey), typeof(TValue), LifetimeFactories.PerCall);
        }

        /// <summary>
        /// Attempts to add an object with per call lifetime for the specified key
        /// </summary>
        /// <typeparam name="TKey">The type that will be used as a key</typeparam>
        /// <typeparam name="TValue">The type of the object that will be held by the PerCall lifetime container</typeparam>
        /// <returns>True if the association was added successfully (that is AssociationContainer did not contained lifetime container with the same key); overwise false</returns>
        public bool TryAddPerCall<TKey, TValue>()
            where TValue : TKey
        {
            return base.TryAddAssociation(typeof(TKey), typeof(TValue), LifetimeFactories.PerCall);
        }

        /// <summary>
        /// Adds an object with per call lifetime with inlined constructor parameters for the specified key
        /// </summary>
        /// <typeparam name="TKey">The type that will be used as a key</typeparam>
        /// <param name="objType">The type of the object that will be held by the PerCallInlinedParams lifetime container</param>
        public void AddPerCallInlinedParams<TKey>(Type objType)
        {
            if (objType == null)
                throw new ArgumentNullException(nameof(objType));

            base.AddAssociation(typeof(TKey), objType, LifetimeFactories.PerCallInlinedParams);
        }

        /// <summary>
        /// Attempts to add an object with per call lifetime with inlined constructor parameters for the specified key
        /// </summary>
        /// <typeparam name="TKey">The type that will be used as a key</typeparam>
        /// <param name="objType">The type of the object that will be held by the PerCallInlinedParams lifetime container</param>
        /// <returns>True if the association was added successfully (that is AssociationContainer did not contained lifetime container with the same key); overwise false</returns>
        public bool TryAddPerCallInlinedParams<TKey>(Type objType)
        {
            if (objType == null)
                throw new ArgumentNullException(nameof(objType));

            return base.TryAddAssociation(typeof(TKey), objType, LifetimeFactories.PerCallInlinedParams);
        }

        /// <summary>
        /// Adds an object with per call lifetime with inlined constructor parameters for the specified key
        /// </summary>
        /// <typeparam name="TKey">The type that will be used as a key</typeparam>
        /// <typeparam name="TValue">The type of the object that will be held by the PerCallInlinedParams lifetime container</typeparam>
        public void AddPerCallInlinedParams<TKey, TValue>()
            where TValue : TKey
        {
            base.AddAssociation(typeof(TKey), typeof(TValue), LifetimeFactories.PerCallInlinedParams);
        }

        /// <summary>
        /// Attempts to add an object with per call lifetime with inlined constructor parameters for the specified key
        /// </summary>
        /// <typeparam name="TKey">The type that will be used as a key</typeparam>
        /// <typeparam name="TValue">The type of the object that will be held by the PerCallInlinedParams lifetime container</typeparam>
        /// <returns>True if the association was added successfully (that is AssociationContainer did not contained lifetime container with the same key); overwise false</returns>
        public bool TryAddPerCallInlinedParams<TKey, TValue>()
            where TValue : TKey
        {
            return base.TryAddAssociation(typeof(TKey), typeof(TValue), LifetimeFactories.PerCallInlinedParams);
        }
    }
}
