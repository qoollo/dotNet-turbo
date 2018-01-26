using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Qoollo.Turbo.IoC.ServiceStuff;

namespace Qoollo.Turbo.IoC.Injections
{
    /// <summary>
    /// Stores association between the 'type-of-the-object' and already instantiated 'object' of that type. 
    /// In multithreaded scenarious should be frozen explicitly (simultanious add and get is not supported)
    /// </summary>
    public class TypeStrictFreezeRequiredInjectionContainer : FreezeRequiredGenericInjectionContainer<Type>, IInjectionResolver
    {
        /// <summary>
        /// TypeStrictFreezeRequiredInjectionContainer constructor
        /// </summary>
        public TypeStrictFreezeRequiredInjectionContainer()
        {
        }

        /// <summary>
        /// TypeStrictFreezeRequiredInjectionContainer constructor
        /// </summary>
        /// <param name="disposeInjectionsWithBuilder">Indicates whether the all injected objects should be disposed with the container</param>
        public TypeStrictFreezeRequiredInjectionContainer(bool disposeInjectionsWithBuilder)
            : base(disposeInjectionsWithBuilder)
        {
        }


        /// <summary>
        /// Checks whether the injection is appropriate for the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="injection">Object to store as injection</param>
        /// <returns>True if the object with 'objType' can be used by container with specified 'key'</returns>
        protected override bool IsGoodInjectionForKey(Type key, object injection)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (injection != null)
            {
                var tpInj = injection.GetType();
                return (tpInj == key) || key.IsAssignableFrom(tpInj);
            }

            return key.IsAssignableFromNull();
        }

        /// <summary>
        /// Gets the injection object of the specified type
        /// </summary>
        /// <typeparam name="T">The type of requested injection object</typeparam>
        /// <returns>Resolved object</returns>
        public T GetInjection<T>()
        {
            return (T)this.GetInjection(typeof(T));
        }

        /// <summary>
        /// Attempts to get the injection object of the specified type
        /// </summary>
        /// <typeparam name="T">The type of requested injection object</typeparam>
        /// <param name="val">Resolved object if foundпеха</param>
        /// <returns>True if the injection object is registered for the specified key; overwise false</returns>
        public bool TryGetInjection<T>(out T val)
        {
            if (this.TryGetInjection(typeof(T), out object tmp))
            {
                val = (T)tmp;
                return true;
            }

            val = default(T);
            return false;
        }

        /// <summary>
        /// Determines whether the InjectionSource contains the object of the specified type
        /// </summary>
        /// <typeparam name="T">The type of injection object</typeparam>
        /// <returns>True if the InjectionSource contains the object of the specified type</returns>
        public bool Contains<T>()
        {
            return this.Contains(typeof(T));
        }

        /// <summary>
        /// Adds a new injection to the container
        /// </summary>
        /// <typeparam name="T">The type of injection object</typeparam>
        /// <param name="val">Object to add</param>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="ObjectFrozenException"></exception>
        public void AddInjection<T>(T val)
        {
            this.AddInjection(typeof(T), val);
        }

        /// <summary>
        /// Attempts to add a new injection to the container
        /// </summary>
        /// <typeparam name="T">The type of the injection object</typeparam>
        /// <param name="val">Object to add</param>
        /// <returns>True if the injection was added, that is InjectionContainer not contains lifetime container with the same key; overwise false</returns>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="ObjectFrozenException"></exception>
        public bool TryAddInjection<T>(T val)
        {
            return this.TryAddInjection(typeof(T), val);
        }
        /// <summary>
        /// Removes the injection of the specified type from the container
        /// </summary>
        /// <typeparam name="T">The type of injection object</typeparam>
        /// <returns>True if the injection was presented in container</returns>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="ObjectFrozenException"></exception>
        public bool RemoveInjection<T>()
        {
            return this.RemoveInjection(typeof(T));
        }

        /// <summary>
        /// Returns the injection resolver implementation for the current container
        /// </summary>
        /// <returns>Injection resolver to resolve injections from the current container</returns>
        public IInjectionResolver GetDirectInjectionResolver()
        {
            return this;
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
            TurboContract.Requires(reqObjectType != null, conditionString: "reqObjectType != null");

            return this.GetInjection(reqObjectType);
        }
        /// <summary>
        /// Resolves the object of the type 'T' to be injected to the constructor of another type ('forType') (short form)
        /// </summary>
        /// <typeparam name="T">The type of the object to be resolved</typeparam>
        /// <param name="forType">The type of the object to be created (can be null)</param>
        /// <returns>Resolved instance to be injected</returns>
        T IInjectionResolver.Resolve<T>(Type forType)
        {
            return (T)this.GetInjection(typeof(T));
        }
    }
}
