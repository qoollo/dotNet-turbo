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
    /// Base class for standard injection containers. Stores association between 'key' and already instantiated 'object'
    /// </summary>
    /// <typeparam name="TKey">The type of the key in injection container</typeparam>
    [ContractClass(typeof(GenericInjectionContainerBaseCodeContractCheck<>))]
    public abstract class GenericInjectionContainerBase<TKey> : IInjectionSource<TKey>, IFreezable, IDisposable
    {
        private volatile bool _isFrozen = false;
        private volatile bool _isDisposed = false;

        /// <summary>
        /// Checks whether the injection is appropriate for the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="injection">Object to store as injection</param>
        /// <returns>True if the object with 'objType' can be used by container with specified 'key'</returns>
        protected abstract bool IsGoodInjectionForKey(TKey key, object injection);

        /// <summary>
        /// Attempts to get an injection from the container by the specified key. Should be implemented in derived type.
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="val">Object for the specified key (in case of success)</param>
        /// <returns>True if the InjectionContainer contains the object for the specified key</returns>
        protected abstract bool TryGetInjectionInner(TKey key, out object val);
        /// <summary>
        /// Checks whether the InjectionContainer contains the object for the specified key. Should be implemented in derived type.
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>True if the injection is presented in container</returns>
        [Pure]
        protected abstract bool ContainsInner(TKey key);

        /// <summary>
        /// Adds a new injection to the container. Should be implemented in derived type.
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="val">Object to add for the specified key</param>
        protected abstract void AddInjectionInner(TKey key, object val);
        /// <summary>
        /// Attempts to add a new injection to the container. Should be implemented in derived type.
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="val">Object to add for the specified key</param>
        /// <returns>True if the injection was added, that is InjectionContainer not contains lifetime container with the same key; overwise false</returns>
        protected abstract bool TryAddInjectionInner(TKey key, object val);
        /// <summary>
        /// Removes the injection from the container for the specified key. Should be implemented in derived type.
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>True if the injection was presented in container</returns>
        protected abstract bool RemoveInjectionInner(TKey key);


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
        /// Throws ArgumentNullException when key is null
        /// </summary>
        private static void ThrowKeyNullException()
        {
            throw new ArgumentNullException("key");
        }
        /// <summary>
        /// Throws KeyNotFoundException for the specified key
        /// </summary>
        /// <param name="key">Key</param>
        private static void ThrowKeyNotFoundException(TKey key)
        {
            throw new KeyNotFoundException(string.Format("Key {0} not found in Injection Container", key));
        }

        /// <summary>
        /// Gets the injection object for the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>Resolved object to be injected</returns>
        /// <exception cref="KeyNotFoundException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object GetInjection(TKey key)
        {
            Contract.Requires(key != null);

            if (key == null)
                ThrowKeyNullException();
            if (_isDisposed)
                CheckContainerState(false);

            object res = null;
            if (!TryGetInjectionInner(key, out res))
                ThrowKeyNotFoundException(key);

            return res;
        }
        /// <summary>
        /// Attempts to get the injection object for the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="val">Resolved object to be injected if found</param>
        /// <returns>True if the injection object is registered for the specified key; overwise false</returns>
        /// <exception cref="ArgumentNullException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetInjection(TKey key, out object val)
        {
            Contract.Requires(key != null);

            if (key == null)
                ThrowKeyNullException();

            if (_isDisposed)
            {
                val = null;
                return false;
            }

            return TryGetInjectionInner(key, out val);
        }

        /// <summary>
        /// Determines whether the InjectionSource contains the key
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>True if the InjectionSource contains the key</returns>
        /// <exception cref="ArgumentNullException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(TKey key)
        {
            Contract.Requires(key != null);

            if (key == null)
                ThrowKeyNullException();
            if (_isDisposed)
                return false;

            return ContainsInner(key);
        }

        /// <summary>
        /// Adds a new injection to the container
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="val">Object to add</param>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="ObjectFrozenException"></exception>
        public void AddInjection(TKey key, object val)
        {
            Contract.Requires(key != null);
            Contract.Ensures(this.ContainsInner(key));

            if (key == null)
                throw new ArgumentNullException("key");

            if (!IsGoodInjectionForKey(key, val))
                throw new InjectionBadKeyForItemException(string.Format("Bad key ({0}) for the supplied object", key));

            CheckContainerState(true);

            AddInjectionInner(key, val);
        }

        /// <summary>
        /// Attempts to add a new association to the container
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="val">Object to add</param>
        /// <returns>True if the injection was added, that is InjectionContainer not contains lifetime container with the same key; overwise false</returns>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="ObjectFrozenException"></exception>
        public bool TryAddInjection(TKey key, object val)
        {
            Contract.Requires(key != null);
            Contract.Ensures(Contract.Result<bool>() == false || this.ContainsInner(key));

            if (key == null)
                throw new ArgumentNullException("key");

            if (!IsGoodInjectionForKey(key, val))
                throw new InjectionBadKeyForItemException(string.Format("Bad key ({0}) for the supplied object", key));

            CheckContainerState(true);

            return TryAddInjectionInner(key, val);
        }

        /// <summary>
        /// Removes the injection from the container for the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>True if the injection was presented in container</returns>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="ObjectFrozenException"></exception>
        public bool RemoveInjection(TKey key)
        {
            Contract.Requires(key != null);
            Contract.Ensures(!this.ContainsInner(key));

            if (key == null)
                throw new ArgumentNullException("key");

            CheckContainerState(true);

            return RemoveInjectionInner(key);
        }


        /// <summary>
        /// Gets the injection object for the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>Resolved object to be injected</returns>
        object IInjectionSource<TKey>.GetInjection(TKey key)
        {
            return GetInjection(key);
        }

        /// <summary>
        /// Attempts to get the injection object for the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="val">Resolved object to be injected if found</param>
        /// <returns>True if the injection object is registered for the specified key; overwise false</returns>
        bool IInjectionSource<TKey>.TryGetInjection(TKey key, out object val)
        {
            return TryGetInjection(key, out val);
        }

        /// <summary>
        /// Determines whether the InjectionSource contains the key
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>True if the InjectionSource contains the key</returns>
        bool IInjectionSource<TKey>.Contains(TKey key)
        {
            return Contains(key);
        }


        /// <summary>
        /// Freezes the current Injection Container
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
    [ContractClassFor(typeof(GenericInjectionContainerBase<>))]
    abstract class GenericInjectionContainerBaseCodeContractCheck<T> : GenericInjectionContainerBase<T>
    {
        /// <summary>Code contracts</summary>
        private GenericInjectionContainerBaseCodeContractCheck() { }

        protected override bool TryGetInjectionInner(T key, out object val)
        {
            Contract.Requires(key != null);

            throw new NotImplementedException();
        }

        protected override bool ContainsInner(T key)
        {
            Contract.Requires(key != null);

            throw new NotImplementedException();
        }

        protected override void AddInjectionInner(T key, object val)
        {
            Contract.Requires(key != null);

            throw new NotImplementedException();
        }

        protected override bool TryAddInjectionInner(T key, object val)
        {
            Contract.Requires(key != null);

            throw new NotImplementedException();
        }

        protected override bool RemoveInjectionInner(T key)
        {
            Contract.Requires(key != null);

            throw new NotImplementedException();
        }

        protected override bool IsGoodInjectionForKey(T key, object injection)
        {
            Contract.Requires(key != null);

            throw new NotImplementedException();
        }
    }
}
