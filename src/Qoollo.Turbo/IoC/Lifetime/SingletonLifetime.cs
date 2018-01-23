using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Qoollo.Turbo.IoC.ServiceStuff;

namespace Qoollo.Turbo.IoC.Lifetime
{
    /// <summary>
    /// Lifetime container that holds a single instance of an object
    /// </summary>
    public class SingletonLifetime: LifetimeBase
    {
        private readonly object _obj;
        private readonly bool _disposeInnerObject;

        /// <summary>
        /// SingletonLifetime constructor
        /// </summary>
        /// <param name="obj">Instance of an object to be held in the current Singleton lifetime container (can be null)</param>
        /// <param name="outType">The type of the object to be stored in the current Lifetime container</param>
        /// <param name="disposeInnerObject">Indicates whether the instance should be disposed with the container</param>
        public SingletonLifetime(object obj, Type outType, bool disposeInnerObject)
            : base(outType)
        {
            if (!((obj != null && obj.GetType() == outType) || (obj == null && outType.IsAssignableFromNull())))
                throw new ArgumentException("Type of 'obj' is incompatible with 'outType'", nameof(obj));

            _obj = obj;
            _disposeInnerObject = disposeInnerObject;
        }
        /// <summary>
        /// SingletonLifetime constructor
        /// </summary>
        /// <param name="obj">Instance of an object to be held in the current Singleton lifetime container (can be null)</param>
        /// <param name="outType">The type of the object to be stored in the current Lifetime container</param>
        public SingletonLifetime(object obj, Type outType)
            : this(obj, outType, disposeInnerObject: false)
        {
        }
        /// <summary>
        /// SingletonLifetime constructor
        /// </summary>
        /// <param name="obj">Instance of an object to be held in the current Singleton lifetime container (cannot be null)</param>
        /// <param name="disposeInnerObject">Indicates whether the instance should be disposed with the container</param>
        public SingletonLifetime(object obj, bool disposeInnerObject)
            : base(obj.GetType())
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            _obj = obj;
            _disposeInnerObject = disposeInnerObject;
        }
        /// <summary>
        /// SingletonLifetime constructor
        /// </summary>
        /// <param name="obj">Instance of an object to be held in the current Singleton lifetime container (cannot be null)</param>
        public SingletonLifetime(object obj)
            : this(obj, disposeInnerObject: false)
        {
        }

        /// <summary>
        /// Resolves the object held by the container
        /// </summary>
        /// <param name="resolver">Injection resolver to acquire parameters</param>
        /// <returns>Resolved instance of the object</returns>
        /// <exception cref="CommonIoCException">Can be raised when injections not found</exception>
        public sealed override object GetInstance(IInjectionResolver resolver)
        {
            TurboContract.Requires(resolver != null, "resolver != null");

            return _obj;
        }

        /// <summary>
        /// Resolves the object held by the container
        /// </summary>
        /// <returns>Resolved instance of the object</returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public object GetInstance()
        {
            return _obj;
        }

        /// <summary>
        /// Cleans-up all resources
        /// </summary>
        /// <param name="isUserCall">True when called explicitly by user from Dispose method</param>
        protected override void Dispose(bool isUserCall)
        {
            if (_disposeInnerObject)
            {
                if (_obj is IDisposable objDisp)
                    objDisp.Dispose();
            }

            base.Dispose(isUserCall);
        }
    }
}
