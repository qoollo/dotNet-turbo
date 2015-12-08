using Qoollo.Turbo.IoC.Associations;
using Qoollo.Turbo.IoC.ServiceStuff;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.IoC
{
    /// <summary>
    /// IoC container. Stores associations and makes them available to resolve in runtime.
    /// </summary>
    public class TurboContainer : DirectTypeAssociationContainer, IObjectLocator<Type>, IInjectionResolver
    {
        /// <summary>
        /// TurboContainer constructor
        /// </summary>
        public TurboContainer()
        {
        }

        /// <summary>
        /// Creates a lifetime container by object type and lifetime factory
        /// </summary>
        /// <param name="key">The key that will be used to store lifetime container inside the current AssociationContainer</param>
        /// <param name="objType">The type of the object that will be held by the lifetime container</param>
        /// <param name="val">Factory to create a lifetime container for the sepcified 'objType'</param>
        /// <returns>Created lifetime container</returns>
        protected override Lifetime.LifetimeBase ProduceResolveInfo(Type key, Type objType, Lifetime.Factories.LifetimeFactory val)
        {
            return val.Create(objType, this, null);
        }



        /// <summary>
        /// Determines whether the object of the specified type can be resolved by the container
        /// </summary>
        /// <param name="key">The type of an object to be checked</param>
        /// <returns>True if the object can be resolved</returns>
        public bool CanResolve(Type key)
        {
            Contract.Requires(key != null);

            return this.Contains(key);
        }
        /// <summary>
        /// Determines whether the object of the specified type can be resolved by the container
        /// </summary>
        /// <typeparam name="T">The type of an object to be checked</typeparam>
        /// <returns>True if the object can be resolved</returns>
        public bool CanResolve<T>()
        {
            return this.Contains(typeof(T));
        }

        /// <summary>
        /// Throws ArgumentNullException
        /// </summary>
        private static void ThrowKeyNullException()
        {
            throw new ArgumentNullException("key");
        }
        /// <summary>
        /// Throws ObjectCannotBeResolvedException with the specified type
        /// </summary>
        /// <param name="type">Type of the object that cannot be resolver</param>
        private static void ThrowObjectCannotBeResolvedException(Type type)
        {
            throw new ObjectCannotBeResolvedException(string.Format("Object of type {0} cannot be resolved by IoC container. Probably this type is not registered.", type));
        }

        /// <summary>
        /// Resolves object of the specified type from the container
        /// </summary>
        /// <param name="key">The type of the object to be resolved</param>
        /// <returns>Resolved object</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object Resolve(Type key)
        {
            Contract.Requires(key != null);
            if (key == null)
                ThrowKeyNullException();

            Lifetime.LifetimeBase life = null;
            if (!this.TryGetAssociation(key, out life))
                ThrowObjectCannotBeResolvedException(key);

            Contract.Assume(life != null);
            return life.GetInstance(this);
        }
        /// <summary>
        /// Resolves object of the specified type from the container
        /// </summary>
        /// <typeparam name="T">The type of the object to be resolved</typeparam>
        /// <returns>Resolved object</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Resolve<T>()
        {
            Lifetime.LifetimeBase life = null;
            if (!this.TryGetAssociation(typeof(T), out life))
                ThrowObjectCannotBeResolvedException(typeof(T));

            Contract.Assume(life != null);
            return (T)life.GetInstance(this);
        }

        /// <summary>
        /// Attempts to resolves object of the specified type from the container
        /// </summary>
        /// <param name="key">The type of the object to be resolved</param>
        /// <param name="val">Resolved object</param>
        /// <returns>True if the resolution is successful (specified type and all required injections registered in the container); overwise false</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryResolve(Type key, out object val)
        {
            Contract.Requires(key != null);
            if (key == null)
                ThrowKeyNullException();

            Lifetime.LifetimeBase life = null;

            if (this.TryGetAssociation(key, out life))
            {
                Contract.Assume(life != null);
                val = life.GetInstance(this);
                return true;
            }

            val = null;
            return false;
        }
        /// <summary>
        /// Attempts to resolves object of the specified type from the container
        /// </summary>
        /// <typeparam name="T">The type of the object to be resolved</typeparam>
        /// <param name="val">Resolved object</param>
        /// <returns>True if the resolution is successful (specified type and all required injections registered in the container); overwise false</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryResolve<T>(out T val)
        {
            Lifetime.LifetimeBase life = null;

            if (this.TryGetAssociation(typeof(T), out life))
            {
                Contract.Assume(life != null);
                val = (T)life.GetInstance(this);
                return true;
            }

            val = default(T);
            return false;
        }


        /// <summary>
        /// Creates an instance of an object of type 'T' using the default constructor.
        /// The type of an object can be not registered in the container.
        /// </summary>
        /// <typeparam name="T">The type of the object to be created</typeparam>
        /// <returns>Created object</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T CreateObject<T>()
        {
            return InstantiationService.CreateObject<T>(this);
        }



        /// <summary>
        /// Resolves object from the container for the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>Resolved object</returns>
        object IObjectLocator<Type>.Resolve(Type key)
        {
            return this.Resolve(key);
        }
        /// <summary>
        /// Attempts to resolve object from the container for the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="val">Resolved object</param>
        /// <returns>True if the resolution succeeded; overwise false</returns>
        bool IObjectLocator<Type>.TryResolve(Type key, out object val)
        {
            return this.TryResolve(key, out val);
        }
        /// <summary>
        /// Determines whether the object can be resolved by the container for the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>True if the object can be resolved</returns>
        bool IObjectLocator<Type>.CanResolve(Type key)
        {
            return this.CanResolve(key);
        }


        /// <summary>
        /// Resolves the object of the specified type ('reqObjectType') to be injected to the constructor of another type ('forType')
        /// </summary>
        /// <param name="reqObjectType">The type of the object to be resolved</param>
        /// <param name="paramName">The name of the parameter to that the injection will be performed (can be null)</param>
        /// <param name="forType">The type of the object to be created (can be null)</param>
        /// <param name="extData">Extended information supplied by the user (can be null)</param>
        /// <returns>Resolved instance to be injected</returns>
        object IInjectionResolver.Resolve(Type reqObjectType, string paramName, Type forType, object extData)
        {
            if (reqObjectType == null)
                throw new ArgumentNullException("reqObjectType", "Requested object type cannot be null");

            Lifetime.LifetimeBase life = null;
            if (!this.TryGetAssociation(reqObjectType, out life))
                throw new ObjectCannotBeResolvedException(string.Format("Object of type {0} cannot be resolved by IoC container. That object is required as the parameter ({2}) to create another object of type {1}.", reqObjectType, forType, paramName));

            Contract.Assume(life != null);
            return life.GetInstance(this);
        }
        /// <summary>
        /// Resolves the object of the type 'T' to be injected to the constructor of another type ('forType') (short form)
        /// </summary>
        /// <typeparam name="T">The type of the object to be resolved</typeparam>
        /// <param name="forType">The type of the object to be created (can be null)</param>
        /// <returns>Resolved instance to be injected</returns>
        T IInjectionResolver.Resolve<T>(Type forType)
        {
            return this.Resolve<T>();
        }
    }
}
