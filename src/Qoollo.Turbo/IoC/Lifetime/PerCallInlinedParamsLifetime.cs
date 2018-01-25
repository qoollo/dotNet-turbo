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
    /// Lifetime container that creates an instance of an object on every call.
    /// All constructor parameters are resolved only once.
    /// </summary>
    public class PerCallInlinedParamsLifetime : LifetimeBase
    {
        private readonly Func<object> _createInstanceFunc;

        /// <summary>
        /// PerCallInlinedParamsLifetime constructor
        /// </summary>
        /// <param name="outType">The type of the object to be stored in the current Lifetime container</param>
        /// <param name="createInstanceFunc">Instance creation method</param>
        public PerCallInlinedParamsLifetime(Type outType, Func<object> createInstanceFunc)
            : base(outType)
        {
            if (createInstanceFunc == null)
                throw new ArgumentNullException(nameof(createInstanceFunc));

            _createInstanceFunc = createInstanceFunc;
        }

        /// <summary>
        /// Resolves the object held by the container
        /// </summary>
        /// <param name="resolver">Injection resolver to acquire parameters</param>
        /// <returns>Resolved instance of the object</returns>
        /// <exception cref="CommonIoCException">Can be raised when injections not found</exception>
        public sealed override object GetInstance(IInjectionResolver resolver)
        {
            TurboContract.Requires(resolver != null, conditionString: "resolver != null");

            return _createInstanceFunc();
        }
    }
}
