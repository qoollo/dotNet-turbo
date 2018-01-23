using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.IoC.Injections
{
    /// <summary>
    /// Stores association between 'key' and already instantiated 'object'.
    /// In multithreaded scenarious should be frozen explicitly (simultanious add and get is not supported)
    /// </summary>
    /// <typeparam name="TKey">The type of the key in injection container</typeparam>
    public class FreezeRequiredGenericInjectionContainer<TKey> : GenericInjectionContainerBase<TKey>, IInjectionSource<TKey>
    {
        private readonly Dictionary<TKey, object> _injections;
        private readonly bool _disposeInjectionsWithBuiler;

        [ContractInvariantMethod]
        private void Invariant()
        {
            TurboContract.Invariant(_injections != null);
        }

        /// <summary>
        /// FreezeRequiredGenericInjectionContainer constructor
        /// </summary>
        /// <param name="disposeInjectionsWithBuilder">Indicates whether the all injected objects should be disposed with the container</param>
        public FreezeRequiredGenericInjectionContainer(bool disposeInjectionsWithBuilder)
        {
            _injections = new Dictionary<TKey, object>();
            _disposeInjectionsWithBuiler = disposeInjectionsWithBuilder;
        }

        /// <summary>
        /// FreezeRequiredGenericInjectionContainer constructor
        /// </summary>
        public FreezeRequiredGenericInjectionContainer()
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
            TurboContract.Requires(key != null, "key != null");

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
            TurboContract.Requires(key != null, "key != null");

            return _injections.TryGetValue(key, out val);
        }

        /// <summary>
        /// Checks whether the InjectionContainer contains the object for the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>True if the injection is presented in container</returns>
        protected sealed override bool ContainsInner(TKey key)
        {
            TurboContract.Requires(key != null, "key != null");

            return _injections.ContainsKey(key);
        }

        /// <summary>
        /// Adds a new injection to the container
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="val">Object to add for the specified key</param>
        protected sealed override void AddInjectionInner(TKey key, object val)
        {
            TurboContract.Requires(key != null, "key != null");

            lock (_injections)
            {
                if (_injections.ContainsKey(key))
                    throw new ItemAlreadyExistsException(string.Format("InjectionContainer already contains the injection for the key ({0})", key));

                _injections.Add(key, val);
            }
        }

        /// <summary>
        /// Attempts to add a new injection to the container
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="val">Object to add for the specified key</param>
        /// <returns>True if the injection was added, that is InjectionContainer not contains lifetime container with the same key; overwise false</returns>
        protected sealed override bool TryAddInjectionInner(TKey key, object val)
        {
            TurboContract.Requires(key != null, "key != null");

            lock (_injections)
            {
                if (_injections.ContainsKey(key))
                    return false;

                _injections.Add(key, val);
            }

            return true;
        }

        /// <summary>
        /// Removes the injection from the container for the specified key. Should be implemented in derived type.
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>True if the injection was presented in container</returns>
        protected sealed override bool RemoveInjectionInner(TKey key)
        {
            TurboContract.Requires(key != null, "key != null");

            lock (_injections)
            {
                return _injections.Remove(key);
            }
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
                    List<KeyValuePair<TKey, object>> toDispose = null;
                    lock (_injections)
                    {
                        toDispose = _injections.ToList();
                        _injections.Clear();
                    }

                    foreach (var elem in toDispose)
                    {
                        if (elem.Value is IDisposable disp)
                            disp.Dispose();
                    }
                }
                else
                {
                    lock (_injections)
                    {
                        _injections.Clear();
                    }
                }
            }

            base.Dispose(isUserCall);
        }
    }
}
