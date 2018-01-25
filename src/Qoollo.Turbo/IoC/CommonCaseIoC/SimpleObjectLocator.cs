using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Qoollo.Turbo.IoC.Associations;
using Qoollo.Turbo.IoC.ServiceStuff;
using System.Diagnostics;

namespace Qoollo.Turbo.IoC
{
    /// <summary>
    /// Simple IoC container and object locator (without separated injection container)
    /// </summary>
    [Obsolete("This container is obsolete. Please, use TurboContainer instead.", true)]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public class SimpleObjectLocator : DirectTypeAssociationContainer, IObjectLocator<Type>
    {
        private readonly IInjectionResolver _resolver;

        [ContractInvariantMethod]
        private void Invariant()
        {
            TurboContract.Invariant(_resolver != null);
        }

        /// <summary>
        /// Injection resolver for SimpleObjectLocator
        /// </summary>
        private class InnerInjectionResolver : IInjectionResolver
        {
            private readonly SimpleObjectLocator _locator;

            [ContractInvariantMethod]
            private void Invariant()
            {
                TurboContract.Invariant(_locator != null);
            }

            /// <summary>
            /// InnerInjectionResolver constructor
            /// </summary>
            /// <param name="locator">Owner</param>
            public InnerInjectionResolver(SimpleObjectLocator locator)
            {
                TurboContract.Requires(locator != null, conditionString: "locator != null");

                _locator = locator;
            }

            public object Resolve(Type reqObjectType, string paramName, Type forType, object extData)
            {
                TurboContract.Requires(reqObjectType != null, conditionString: "reqObjectType != null");

                return _locator.Resolve(reqObjectType);
            }
            public T Resolve<T>(Type forType)
            {
                return _locator.Resolve<T>();
            }
        }

        /// <summary>
        /// SimpleObjectLocator constructor
        /// </summary>
        public SimpleObjectLocator()
        {
            _resolver = new InnerInjectionResolver(this);
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
            TurboContract.Requires(key != null, conditionString: "key != null");
            TurboContract.Requires(objType != null, conditionString: "objType != null");
            TurboContract.Requires(val != null, conditionString: "val != null");

            return val.Create(objType, _resolver, null);
        }


        /// <summary>
        /// Resolves object of the specified type from the container
        /// </summary>
        /// <param name="key">The type of the object to be resolved</param>
        /// <returns>Resolved object</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object Resolve(Type key)
        {
            TurboContract.Requires(key != null, conditionString: "key != null");

            var life = this.GetAssociation(key);
            TurboContract.Assert(life != null, conditionString: "life != null");
            return life.GetInstance(_resolver);
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
            TurboContract.Requires(key != null, conditionString: "key != null");


            if (this.TryGetAssociation(key, out Lifetime.LifetimeBase life))
            {
                TurboContract.Assert(life != null, conditionString: "life != null");
                if (life.TryGetInstance(_resolver, out val))
                    return true;
            }

            val = null;
            return false;
        }

        /// <summary>
        /// Determines whether the object of the specified type can be resolved by the container
        /// </summary>
        /// <param name="key">The type of an object to be checked</param>
        /// <returns>True if the object can be resolved</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CanResolve(Type key)
        {
            TurboContract.Requires(key != null, conditionString: "key != null");

            return this.Contains(key);
        }


        /// <summary>
        /// Resolves object of the specified type from the container
        /// </summary>
        /// <typeparam name="T">The type of the object to be resolved</typeparam>
        /// <returns>Resolved object</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Resolve<T>()
        {
            var life = this.GetAssociation(typeof(T));
            TurboContract.Assert(life != null, conditionString: "life != null");
            return (T)life.GetInstance(_resolver);
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
                TurboContract.Assert(life != null, conditionString: "life != null");
                if (life.TryGetInstance(_resolver, out object tmp))
                {
                    if (tmp is T)
                    {
                        val = (T)tmp;
                        return true;
                    }
                }
            }

            val = default(T);
            return false;
        }

        /// <summary>
        /// Determines whether the object of the specified type can be resolved by the container
        /// </summary>
        /// <typeparam name="T">The type of an object to be checked</typeparam>
        /// <returns>True if the object can be resolved</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CanResolve<T>()
        {
            return this.Contains(typeof(T));
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
            return InstantiationService.CreateObject<T>(_resolver);
        }


        /// <summary>
        /// Resolves object from the container for the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>Resolved object</returns>
        object IObjectLocator<Type>.Resolve(Type key)
        {
            TurboContract.Requires(key != null, conditionString: "key != null");

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
            TurboContract.Requires(key != null, conditionString: "key != null");

            return this.TryResolve(key, out val);
        }
        /// <summary>
        /// Determines whether the object can be resolved by the container for the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>True if the object can be resolved</returns>
        bool IObjectLocator<Type>.CanResolve(Type key)
        {
            TurboContract.Requires(key != null, conditionString: "key != null");

            return this.CanResolve(key);
        }
    }
}
