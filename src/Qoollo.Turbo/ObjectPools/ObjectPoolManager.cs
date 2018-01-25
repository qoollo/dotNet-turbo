using Qoollo.Turbo.ObjectPools.Common;
using Qoollo.Turbo.ObjectPools.ServiceStuff;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.ObjectPools
{
    /// <summary>
    /// Object pool (stores a set of initialized and ready to use objects)
    /// </summary>
    /// <typeparam name="TElem">The type of elements stored in Pool</typeparam>
    [ContractClass(typeof(ObjectPoolManagerCodeContract<>))]
    public abstract class ObjectPoolManager<TElem>: IDisposable
    {
        /// <summary>
        /// Number of elements stored in ObjectPool
        /// </summary>
        public abstract int ElementCount
        {
            get;
        }

#if DEBUG
        /// <summary>
        /// Rents element from pool
        /// </summary>
        /// <returns>Element holder that can be used inside 'using' statement</returns>
        /// <exception cref="CantRetrieveElementException">Element can't be rented due to some error</exception>
        /// <param name="memberName">Caller method name (only for debug purposes)</param>
        /// <param name="sourceFilePath">Caller source code file name (only for debug purposes)</param>
        /// <param name="sourceLineNumber">Caller line number in source code (only for debug purposes)</param>
        public RentedElementMonitor<TElem> Rent([CallerMemberName] string memberName = null, [CallerFilePath] string sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = 0)
        {
            return new RentedElementMonitor<TElem>(RentElement(-1, new CancellationToken(), true), this, memberName, sourceFilePath, sourceLineNumber);
        }

        /// <summary>
        /// Rents element from pool
        /// </summary>
        /// <param name="throwOnUnavail">True - throws <see cref="CantRetrieveElementException"/> when element can't be rented, otherwise returns empty <see cref="RentedElementMonitor{TElem}"/></param>
        /// <returns>Element holder that can be used inside 'using' statement</returns>
        /// <exception cref="CantRetrieveElementException">Element can't be rented due to some error</exception>
        /// <param name="memberName">Caller method name (only for debug purposes)</param>
        /// <param name="sourceFilePath">Caller source code file name (only for debug purposes)</param>
        /// <param name="sourceLineNumber">Caller line number in source code (only for debug purposes)</param>
        public RentedElementMonitor<TElem> Rent(bool throwOnUnavail, [CallerMemberName] string memberName = null, [CallerFilePath] string sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = 0)
        {
            return new RentedElementMonitor<TElem>(RentElement(-1, new CancellationToken(), throwOnUnavail), this, memberName, sourceFilePath, sourceLineNumber);
        }

        /// <summary>
        /// Rents element from pool
        /// </summary>
        /// <param name="token">Token to cancel waiting for element availability</param>
        /// <returns>Element holder that can be used inside 'using' statement</returns>
        /// <exception cref="CantRetrieveElementException">Element can't be rented due to some error</exception>
        /// <param name="memberName">Caller method name (only for debug purposes)</param>
        /// <param name="sourceFilePath">Caller source code file name (only for debug purposes)</param>
        /// <param name="sourceLineNumber">Caller line number in source code (only for debug purposes)</param>
        public RentedElementMonitor<TElem> Rent(CancellationToken token, [CallerMemberName] string memberName = null, [CallerFilePath] string sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = 0)
        {
            return new RentedElementMonitor<TElem>(RentElement(-1, token, true), this, memberName, sourceFilePath, sourceLineNumber);
        }

        /// <summary>
        /// Rents element from pool
        /// </summary>
        /// <param name="token">Token to cancel waiting for element availability</param>
        /// <param name="throwOnUnavail">True - throws <see cref="CantRetrieveElementException"/> when element can't be rented, otherwise returns empty <see cref="RentedElementMonitor{TElem}"/></param>
        /// <returns>Element holder that can be used inside 'using' statement</returns>
        /// <exception cref="CantRetrieveElementException">Element can't be rented due to some error</exception>
        /// <param name="memberName">Caller method name (only for debug purposes)</param>
        /// <param name="sourceFilePath">Caller source code file name (only for debug purposes)</param>
        /// <param name="sourceLineNumber">Caller line number in source code (only for debug purposes)</param>
        public RentedElementMonitor<TElem> Rent(CancellationToken token, bool throwOnUnavail, [CallerMemberName] string memberName = null, [CallerFilePath] string sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = 0)
        {
            return new RentedElementMonitor<TElem>(RentElement(-1, token, throwOnUnavail), this, memberName, sourceFilePath, sourceLineNumber);
        }

        /// <summary>
        /// Rents element from pool
        /// </summary>
        /// <param name="timeout">Waiting timeout in milliseconds (-1 - infinity)</param>
        /// <returns>Element holder that can be used inside 'using' statement</returns>
        /// <exception cref="CantRetrieveElementException">Element can't be rented due to some error</exception>
        /// <param name="memberName">Caller method name (only for debug purposes)</param>
        /// <param name="sourceFilePath">Caller source code file name (only for debug purposes)</param>
        /// <param name="sourceLineNumber">Caller line number in source code (only for debug purposes)</param>
        public RentedElementMonitor<TElem> Rent(int timeout, [CallerMemberName] string memberName = null, [CallerFilePath] string sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = 0)
        {
            return new RentedElementMonitor<TElem>(RentElement(timeout, new CancellationToken(), true), this, memberName, sourceFilePath, sourceLineNumber);
        }

        /// <summary>
        /// Rents element from pool
        /// </summary>
        /// <param name="timeout">Waiting timeout in milliseconds (-1 - infinity)</param>
        /// <param name="throwOnUnavail">True - throws <see cref="CantRetrieveElementException"/> when element can't be rented, otherwise returns empty <see cref="RentedElementMonitor{TElem}"/></param>
        /// <returns>Element holder that can be used inside 'using' statement</returns>
        /// <exception cref="CantRetrieveElementException">Element can't be rented due to some error</exception>
        /// <param name="memberName">Caller method name (only for debug purposes)</param>
        /// <param name="sourceFilePath">Caller source code file name (only for debug purposes)</param>
        /// <param name="sourceLineNumber">Caller line number in source code (only for debug purposes)</param>
        public RentedElementMonitor<TElem> Rent(int timeout, bool throwOnUnavail, [CallerMemberName] string memberName = null, [CallerFilePath] string sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = 0)
        {
            return new RentedElementMonitor<TElem>(RentElement(timeout, new CancellationToken(), throwOnUnavail), this, memberName, sourceFilePath, sourceLineNumber);
        }

        /// <summary>
        /// Rents element from pool
        /// </summary>
        /// <param name="timeout">Waiting timeout in milliseconds (-1 - infinity)</param>
        /// <param name="token">Token to cancel waiting for element availability</param>
        /// <returns>Element holder that can be used inside 'using' statement</returns>
        /// <exception cref="CantRetrieveElementException">Element can't be rented due to some error</exception>
        /// <param name="memberName">Caller method name (only for debug purposes)</param>
        /// <param name="sourceFilePath">Caller source code file name (only for debug purposes)</param>
        /// <param name="sourceLineNumber">Caller line number in source code (only for debug purposes)</param>
        public RentedElementMonitor<TElem> Rent(int timeout, CancellationToken token, [CallerMemberName] string memberName = null, [CallerFilePath] string sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = 0)
        {
            return new RentedElementMonitor<TElem>(RentElement(timeout, token, true), this, memberName, sourceFilePath, sourceLineNumber);
        }

        /// <summary>
        /// Rents element from pool
        /// </summary>
        /// <param name="timeout">Waiting timeout in milliseconds (-1 - infinity)</param>
        /// <param name="token">Token to cancel waiting for element availability</param>
        /// <param name="throwOnUnavail">True - throws <see cref="CantRetrieveElementException"/> when element can't be rented, otherwise returns empty <see cref="RentedElementMonitor{TElem}"/></param>
        /// <returns>Element holder that can be used inside 'using' statement</returns>
        /// <exception cref="CantRetrieveElementException">Element can't be rented due to some error</exception>
        /// <param name="memberName">Caller method name (only for debug purposes)</param>
        /// <param name="sourceFilePath">Caller source code file name (only for debug purposes)</param>
        /// <param name="sourceLineNumber">Caller line number in source code (only for debug purposes)</param>
        public RentedElementMonitor<TElem> Rent(int timeout, CancellationToken token, bool throwOnUnavail, [CallerMemberName] string memberName = null, [CallerFilePath] string sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = 0)
        {
            return new RentedElementMonitor<TElem>(RentElement(timeout, token, throwOnUnavail), this, memberName, sourceFilePath, sourceLineNumber);
        }
#else
        /// <summary>
        /// Rents element from pool
        /// </summary>
        /// <returns>Element holder that can be used inside 'using' statement</returns>
        /// <exception cref="CantRetrieveElementException">Element can't be rented due to some error</exception>
        public RentedElementMonitor<TElem> Rent()
        {
            return new RentedElementMonitor<TElem>(RentElement(-1, new CancellationToken(), true), this);
        }

        /// <summary>
        /// Rents element from pool
        /// </summary>
        /// <param name="throwOnUnavail">True - throws <see cref="CantRetrieveElementException"/> when element can't be rented, otherwise returns empty <see cref="RentedElementMonitor{TElem}"/></param>
        /// <returns>Element holder that can be used inside 'using' statement</returns>
        /// <exception cref="CantRetrieveElementException">Element can't be rented due to some error</exception>
        public RentedElementMonitor<TElem> Rent(bool throwOnUnavail)
        {
            return new RentedElementMonitor<TElem>(RentElement(-1, new CancellationToken(), throwOnUnavail), this);
        }

        /// <summary>
        /// Rents element from pool
        /// </summary>
        /// <param name="token">Token to cancel waiting for element availability</param>
        /// <returns>Element holder that can be used inside 'using' statement</returns>
        /// <exception cref="CantRetrieveElementException">Element can't be rented due to some error</exception>
        public RentedElementMonitor<TElem> Rent(CancellationToken token)
        {
            return new RentedElementMonitor<TElem>(RentElement(-1, token, true), this);
        }

        /// <summary>
        /// Rents element from pool
        /// </summary>
        /// <param name="token">Token to cancel waiting for element availability</param>
        /// <param name="throwOnUnavail">True - throws <see cref="CantRetrieveElementException"/> when element can't be rented, otherwise returns empty <see cref="RentedElementMonitor{TElem}"/></param>
        /// <returns>Element holder that can be used inside 'using' statement</returns>
        /// <exception cref="CantRetrieveElementException">Element can't be rented due to some error</exception>
        public RentedElementMonitor<TElem> Rent(CancellationToken token, bool throwOnUnavail)
        {
            return new RentedElementMonitor<TElem>(RentElement(-1, token, throwOnUnavail), this);
        }

        /// <summary>
        /// Rents element from pool
        /// </summary>
        /// <param name="timeout">Waiting timeout in milliseconds (-1 - infinity)</param>
        /// <returns>Element holder that can be used inside 'using' statement</returns>
        /// <exception cref="CantRetrieveElementException">Element can't be rented due to some error</exception>
        public RentedElementMonitor<TElem> Rent(int timeout)
        {
            return new RentedElementMonitor<TElem>(RentElement(timeout, new CancellationToken(), true), this);
        }

        /// <summary>
        /// Rents element from pool
        /// </summary>
        /// <param name="timeout">Waiting timeout in milliseconds (-1 - infinity)</param>
        /// <param name="throwOnUnavail">True - throws <see cref="CantRetrieveElementException"/> when element can't be rented, otherwise returns empty <see cref="RentedElementMonitor{TElem}"/></param>
        /// <returns>Element holder that can be used inside 'using' statement</returns>
        /// <exception cref="CantRetrieveElementException">Element can't be rented due to some error</exception>
        public RentedElementMonitor<TElem> Rent(int timeout, bool throwOnUnavail)
        {
            return new RentedElementMonitor<TElem>(RentElement(timeout, new CancellationToken(), throwOnUnavail), this);
        }

        /// <summary>
        /// Rents element from pool
        /// </summary>
        /// <param name="timeout">Waiting timeout in milliseconds (-1 - infinity)</param>
        /// <param name="token">Token to cancel waiting for element availability</param>
        /// <returns>Element holder that can be used inside 'using' statement</returns>
        /// <exception cref="CantRetrieveElementException">Element can't be rented due to some error</exception>
        public RentedElementMonitor<TElem> Rent(int timeout, CancellationToken token)
        {
            return new RentedElementMonitor<TElem>(RentElement(timeout, token, true), this);
        }

        /// <summary>
        /// Rents element from pool
        /// </summary>
        /// <param name="timeout">Waiting timeout in milliseconds (-1 - infinity)</param>
        /// <param name="token">Token to cancel waiting for element availability</param>
        /// <param name="throwOnUnavail">True - throws <see cref="CantRetrieveElementException"/> when element can't be rented, otherwise returns empty <see cref="RentedElementMonitor{TElem}"/></param>
        /// <returns>Element holder that can be used inside 'using' statement</returns>
        /// <exception cref="CantRetrieveElementException">Element can't be rented due to some error</exception>
        public RentedElementMonitor<TElem> Rent(int timeout, CancellationToken token, bool throwOnUnavail)
        {
            return new RentedElementMonitor<TElem>(RentElement(timeout, token, throwOnUnavail), this);
        }
#endif

        /// <summary>
        /// Rents element from pool
        /// </summary>
        /// <param name="timeout">Waiting timeout in milliseconds (-1 - infinity)</param>
        /// <param name="token">Token to cancel waiting for element availability</param>
        /// <param name="throwOnUnavail">True - throws <see cref="CantRetrieveElementException"/> when element can't be rented, otherwise returns null</param>
        /// <returns>Wrapper for rented element</returns>
        protected abstract PoolElementWrapper<TElem> RentElement(int timeout, CancellationToken token, bool throwOnUnavail);

        /// <summary>
        /// Releases element back to the pool. Normally should be called from <see cref="RentedElementMonitor{TElem}"/>
        /// </summary>
        /// <param name="element">Element wrapper to be released</param>
        protected internal abstract void ReleaseElement(PoolElementWrapper<TElem> element);



        /// <summary>
        /// Cleans-up resources
        /// </summary>
        /// <param name="isUserCall">True when called explicitly by user</param>
        protected virtual void Dispose(bool isUserCall)
        {
        }

        /// <summary>
        /// Cleans-up resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }





    [ContractClassFor(typeof(ObjectPoolManager<>))]
    internal abstract class ObjectPoolManagerCodeContract<TElem>: ObjectPoolManager<TElem>
    {
        public override int ElementCount
        {
            get
            {
                TurboContract.Ensures(TurboContract.Result<int>() >= 0);

                throw new NotImplementedException();
            }
        }

        protected override PoolElementWrapper<TElem> RentElement(int timeout, CancellationToken token, bool throwOnUnavail)
        {
            throw new NotImplementedException();
        }

        protected internal override void ReleaseElement(PoolElementWrapper<TElem> element)
        {
            TurboContract.Requires(element != null, conditionString: "element != null");

            throw new NotImplementedException();
        }
    }
}
