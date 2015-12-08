using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.IoC.Injections
{
    /// <summary>
    /// Stores association between 'key' and already instantiated 'object'. Suitable for concurrent access. Uses ConcurrentDictionary under cover.
    /// </summary>
    /// <typeparam name="TKey">The type of the key in injection container</typeparam>
    public class ConcurrentGenericInjectionContainer<TKey> : GenericInjectionContainerBase<TKey>, IInjectionSource<TKey>
    {
        private readonly ConcurrentDictionary<TKey, object> _injections;
        private readonly bool _disposeInjectionsWithBuiler;

        [ContractInvariantMethod]
        private void Invariant()
        {
            Contract.Invariant(_injections != null);
        }

        /// <summary>
        /// ConcurrentGenericInjectionContainer constructor
        /// </summary>
        /// <param name="disposeInjectionsWithBuilder">Indicates whether the all injected objects should be disposed with the container</param>
        public ConcurrentGenericInjectionContainer(bool disposeInjectionsWithBuilder)
        {
            _injections = new ConcurrentDictionary<TKey, object>();
            _disposeInjectionsWithBuiler = disposeInjectionsWithBuilder;
        }
        /// <summary>
        /// ConcurrentGenericInjectionContainer constructor
        /// </summary>
        public ConcurrentGenericInjectionContainer()
            : this(true)
        {
        }

        /// <summary>
        /// Checks whether the injection is appropriate for the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="injection">Object to store as injection</param>
        /// <returns>True if the object with 'objType' can be used by container with specified 'key'</returns>
        protected override bool IsGoodInjectionForKey(TKey key, object injection)
        {
            return true;
        }

        /// <summary>
        /// Attempts to get an injection from the container by the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="val">Object for the specified key (in case of success)</param>
        /// <returns>True if the InjectionContainer contains the object for the specified key</returns>
        protected sealed override bool TryGetInjectionInner(TKey key, out object val)
        {
            return _injections.TryGetValue(key, out val);
        }

        /// <summary>
        /// Checks whether the InjectionContainer contains the object for the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>True if the injection is presented in container</returns>
        protected sealed override bool ContainsInner(TKey key)
        {
            return _injections.ContainsKey(key);
        }

        /// <summary>
        /// Adds a new injection to the container
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="val">Object to add for the specified key</param>
        protected sealed override void AddInjectionInner(TKey key, object val)
        {
            if (!_injections.TryAdd(key, val))
                throw new ItemAlreadyExistsException(string.Format("InjectionContainer already contains the injection for the key ({0})", key));
        }

        /// <summary>
        /// Attempts to add a new injection to the container
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="val">Object to add for the specified key</param>
        /// <returns>True if the injection was added, that is InjectionContainer not contains lifetime container with the same key; overwise false</returns>
        protected sealed override bool TryAddInjectionInner(TKey key, object val)
        {
            return _injections.TryAdd(key, val);
        }

        /// <summary>
        /// Removes the injection from the container for the specified key. Should be implemented in derived type.
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>True if the injection was presented in container</returns>
        protected sealed override bool RemoveInjectionInner(TKey key)
        {
            object val = null;
            return _injections.TryRemove(key, out val);
        }


        /// <summary>
        /// Gets the injection object for the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>Resolved object to be injected</returns>
        /// <exception cref="KeyNotFoundException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public new object GetInjection(TKey key)
        {
            return _injections[key];
        }
        /// <summary>
        /// Attempts to get the injection object for the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="val">Resolved object to be injected if found</param>
        /// <returns>True if the injection object is registered for the specified key; overwise false</returns>
        /// <exception cref="ArgumentNullException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public new bool TryGetInjection(TKey key, out object val)
        {
            return _injections.TryGetValue(key, out val);
        }
        /// <summary>
        /// Determines whether the InjectionSource contains the key
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>True if the InjectionSource contains the key</returns>
        /// <exception cref="ArgumentNullException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public new bool Contains(TKey key)
        {
            return _injections.ContainsKey(key);
        }


        /// <summary>
        /// Gets the injection object for the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>Resolved object to be injected</returns>
        object IInjectionSource<TKey>.GetInjection(TKey key)
        {
            return _injections[key];
        }
        /// <summary>
        /// Attempts to get the injection object for the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="val">Resolved object to be injected if found</param>
        /// <returns>True if the injection object is registered for the specified key; overwise false</returns>
        bool IInjectionSource<TKey>.TryGetInjection(TKey key, out object val)
        {
            return _injections.TryGetValue(key, out val);
        }
        /// <summary>
        /// Determines whether the InjectionSource contains the key
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>True if the InjectionSource contains the key</returns>
        bool IInjectionSource<TKey>.Contains(TKey key)
        {
            return _injections.ContainsKey(key);
        }

        /// <summary>
        /// Cleans-up all resources
        /// </summary>
        /// <param name="isUserCall">True when called explicitly by user from Dispose method</param>
        protected override void Dispose(bool isUserCall)
        {
            if (isUserCall)
            {
                if (_disposeInjectionsWithBuiler)
                {
                    var toDispose = _injections.ToArray();
                    _injections.Clear();

                    for (int i = 0; i < toDispose.Length; i++)
                    {
                        IDisposable disp = toDispose[i].Value as IDisposable;
                        if (disp != null)
                            disp.Dispose();
                    }
                }
                else
                {
                    _injections.Clear();
                }
            }
            base.Dispose(isUserCall);
        }
    }
}
