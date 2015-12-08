using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Qoollo.Turbo.IoC.Helpers;
using Qoollo.Turbo.IoC.ServiceStuff;

namespace Qoollo.Turbo.IoC.Lifetime
{
    /// <summary>
    /// Lifetime container that holds an instance of an object per each thread
    /// </summary>
    public class PerThreadLifetime: LifetimeBase
    {
        /// <summary>
        /// Stores the object and IsInitialize field
        /// </summary>
        private struct ThreadLocalSlot
        {
            public ThreadLocalSlot(object obj)
            {
                Object = obj;
                IsInitialized = true;
            }
            
            public readonly object Object;
            public readonly bool IsInitialized;
        }

        // ==============

        private readonly ThreadLocal<ThreadLocalSlot> _obj;
        private readonly Func<IInjectionResolver, object> _createInstFunc;

        /// <summary>
        /// Code contracts
        /// </summary>
        [ContractInvariantMethod]
        private void Invariant()
        {
            Contract.Invariant(_obj != null);
            Contract.Invariant(_createInstFunc != null);
        }

        /// <summary>
        /// PerThreadLifetime constructor
        /// </summary>
        /// <param name="createInstFunc">Instance creation method</param>
        /// <param name="objType">The type of the object to be stored in the current Lifetime container</param>
        public PerThreadLifetime(Func<IInjectionResolver, object> createInstFunc, Type objType)
            : base(objType)
        {
            Contract.Requires<ArgumentNullException>(createInstFunc != null, "createInstFunc");

            _obj = new ThreadLocal<ThreadLocalSlot>(true);
            _createInstFunc = createInstFunc;
        }


        /// <summary>
        /// Creates a new instance for the current thread and stores it inside '_obj'
        /// </summary>
        /// <param name="resolver">Injection resolver to acquire parameters</param>
        /// <returns>Created instance</returns>
        private ThreadLocalSlot CreateInstanceCore(IInjectionResolver resolver)
        {
            var result = new ThreadLocalSlot(_createInstFunc(resolver));
            _obj.Value = result;
            return result;
        }

        /// <summary>
        /// Resolves the object held by the container
        /// </summary>
        /// <param name="resolver">Injection resolver to acquire parameters</param>
        /// <returns>Resolved instance of the object</returns>
        /// <exception cref="CommonIoCException">Can be raised when injections not found</exception>
        public sealed override object GetInstance(IInjectionResolver resolver)
        {
            var result = _obj.Value;

            if (!result.IsInitialized)
                result = CreateInstanceCore(resolver);

            return result.Object;
        }


        /// <summary>
        /// Cleans-up all resources
        /// </summary>
        /// <param name="isUserCall">True when called explicitly by user from Dispose method</param>
        protected override void Dispose(bool isUserCall)
        {
            var innerObjects = _obj.Values.ToList();
            _obj.Dispose();

            foreach (var inObj in innerObjects)
            {
                if (inObj.IsInitialized)
                {
                    IDisposable inObjDisp = inObj.Object as IDisposable;
                    if (inObjDisp != null)
                        inObjDisp.Dispose();
                }
            }

            base.Dispose(isUserCall);
        }
    }
}
