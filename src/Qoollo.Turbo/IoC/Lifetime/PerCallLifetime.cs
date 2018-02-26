using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Qoollo.Turbo.IoC.ServiceStuff;

namespace Qoollo.Turbo.IoC.Lifetime
{
    /// <summary>
    /// Lifetime container that creates an instance of an object on every call
    /// </summary>
    public class PerCallLifetime : LifetimeBase
    {
        private readonly Func<IInjectionResolver, object> _createInstanceFunc;

        /// <summary>
        /// PerCallLifetime constructor
        /// </summary>
        /// <param name="outType">The type of the object to be stored in the current Lifetime container</param>
        /// <param name="createInstanceFunc">Instance creation method</param>
        public PerCallLifetime(Type outType, Func<IInjectionResolver, object> createInstanceFunc)
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

            return _createInstanceFunc(resolver);
        }
    }
}
