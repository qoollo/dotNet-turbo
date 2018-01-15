using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Qoollo.Turbo.IoC.Associations;
using Qoollo.Turbo.IoC.Injections;
using Qoollo.Turbo.IoC.ServiceStuff;
using System.Diagnostics;

namespace Qoollo.Turbo.IoC
{
    /// <summary>
    /// Base class for object locators that implements the main methods
    /// </summary>
    /// <typeparam name="TInjection">The type of the injection container</typeparam>
    /// <typeparam name="TInjKey">The type of the key of injection container</typeparam>
    /// <typeparam name="TAssociation">The type of the association container</typeparam>
    /// <typeparam name="TAssocKey">The type of the key of association container</typeparam>
    [Obsolete("Do not use this class as a base for your custom IoC containers. Please, implement them from the core by hand.")]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public abstract class ObjectLocator<TInjection, TInjKey, TAssociation, TAssocKey>: IObjectLocator<TAssocKey>, IDisposable
        where TInjection: IInjectionSource<TInjKey>
        where TAssociation: IAssociationSource<TAssocKey>
    {
        private TInjection _injection;
        private TAssociation _association;
        private IInjectionResolver _resolver;

        /// <summary>
        /// ObjectLocator constructor
        /// </summary>
        /// <param name="injection">Injection container</param>
        /// <param name="association">Association container</param>
        /// <param name="resolver">Injection resolver</param>
        protected ObjectLocator(TInjection injection, TAssociation association, IInjectionResolver resolver)
        {
            Contract.Requires(injection != null);
            Contract.Requires(association != null);
            Contract.Requires(resolver != null);

            _injection = injection;
            _association = association;
            _resolver = resolver;
        }

        /// <summary>
        /// ObjectLocator parameterless constructor.
        /// You should also call SetInnerObjects before use the container
        /// </summary>
        protected ObjectLocator()
        {
        }

        /// <summary>
        /// Sets required objects when the current instance was created by the parameterless constructor
        /// </summary>
        /// <param name="injection">Injection container</param>
        /// <param name="association">Association container</param>
        /// <param name="resolver">Injection resolver</param>
        protected void SetInnerObjects(TInjection injection, TAssociation association, IInjectionResolver resolver)
        {
            Contract.Requires(injection != null);
            Contract.Requires(association != null);
            Contract.Requires(resolver != null);

            _injection = injection;
            _association = association;
            _resolver = resolver;
        }

        /// <summary>
        /// Gets the injection container
        /// </summary>
        public TInjection Injection
        {
            get { return _injection; }
        }

        /// <summary>
        /// Gets the association container
        /// </summary>
        public TAssociation Association
        {
            get { return _association; }
        }

        /// <summary>
        /// Resolves object from the container for the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>Resolved object</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object Resolve(TAssocKey key)
        {
            Contract.Requires((object)key != null);

            var life = _association.GetAssociation(key);
            Debug.Assert(life != null);
            return life.GetInstance(_resolver);
        }
        /// <summary>
        /// Attempts to resolve object from the container for the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="val">Resolved object</param>
        /// <returns>True if the resolution succeeded; overwise false</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryResolve(TAssocKey key, out object val)
        {
            Contract.Requires((object)key != null);

            Lifetime.LifetimeBase life = null;

            if (_association.TryGetAssociation(key, out life))
            {
                Debug.Assert(life != null);
                if (life.TryGetInstance(_resolver, out val))
                    return true;
            }

            val = null;
            return false;
        }

        /// <summary>
        /// Determines whether the object can be resolved by the container for the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>True if the object can be resolved</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CanResolve(TAssocKey key)
        {
            Contract.Requires((object)key != null);

            return Association.Contains(key);
        }

        /// <summary>
        /// Resolves object from the container for the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>Resolved object</returns>
        object IObjectLocator<TAssocKey>.Resolve(TAssocKey key)
        {
            return this.Resolve(key);
        }

        /// <summary>
        /// Attempts to resolve object from the container for the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="val">Resolved object</param>
        /// <returns>True if the resolution succeeded; overwise false</returns>
        bool IObjectLocator<TAssocKey>.TryResolve(TAssocKey key, out object val)
        {
            return this.TryResolve(key, out val);
        }

        /// <summary>
        /// Determines whether the object can be resolved by the container for the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>True if the object can be resolved</returns>
        bool IObjectLocator<TAssocKey>.CanResolve(TAssocKey key)
        {
            return this.CanResolve(key);
        }

        /// <summary>
        /// Cleans-up all resources
        /// </summary>
        /// <param name="isUserCall">True if called by user; false - from finalizer</param>
        protected virtual void Dispose(bool isUserCall)
        {
            if (_association != null && _association is IDisposable)
                (_association as IDisposable).Dispose();

            if (_injection != null && _injection is IDisposable)
                (_injection as IDisposable).Dispose();
        }

        /// <summary>
        /// Cleans-up all resources
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
