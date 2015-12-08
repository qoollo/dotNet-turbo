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
    public class PerCallInlinedParamsInterfaceLifetime : LifetimeBase
    {
        private readonly IInstanceCreatorNoParam _createInstanceObj;

        /// <summary>
        /// PerCallInlinedParamsInterfaceLifetime constructor
        /// </summary>
        /// <param name="outType">The type of the object to be stored in the current Lifetime container</param>
        /// <param name="createInstObj">Instance creator</param>
        public PerCallInlinedParamsInterfaceLifetime(Type outType, IInstanceCreatorNoParam createInstObj)
            : base(outType)
        {
            Contract.Requires<ArgumentNullException>(createInstObj != null, "createInstObj");

            _createInstanceObj = createInstObj;
        }

        /// <summary>
        /// Resolves the object held by the container
        /// </summary>
        /// <param name="resolver">Injection resolver to acquire parameters</param>
        /// <returns>Resolved instance of the object</returns>
        /// <exception cref="CommonIoCException">Can be raised when injections not found</exception>
        public sealed override object GetInstance(IInjectionResolver resolver)
        {
            return _createInstanceObj.CreateInstance();
        }
    }
}
