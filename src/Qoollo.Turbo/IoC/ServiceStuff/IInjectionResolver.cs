using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.IoC.ServiceStuff
{
    /// <summary>
    /// Represents an injection resolver. 
    /// Resolves the instance of the specified type to be injected to the constructor of another object.
    /// </summary>
    [ContractClass(typeof(IInjectionResolverCodeContractCheck))]
    public interface IInjectionResolver
    {
        /// <summary>
        /// Resolves the object of the specified type ('reqObjectType') to be injected to the constructor of another type ('forType')
        /// </summary>
        /// <param name="reqObjectType">The type of the object to be resolved</param>
        /// <param name="paramName">The name of the parameter to that the injection will be performed (can be null)</param>
        /// <param name="forType">The type of the object to be created (can be null)</param>
        /// <param name="extData">Extended information supplied by the user (can be null)</param>
        /// <returns>Resolved instance to be injected</returns>
        object Resolve(Type reqObjectType, string paramName, Type forType, object extData);
        /// <summary>
        /// Resolves the object of the type 'T' to be injected to the constructor of another type ('forType') (short form)
        /// </summary>
        /// <typeparam name="T">The type of the object to be resolved</typeparam>
        /// <param name="forType">The type of the object to be created (can be null)</param>
        /// <returns>Resolved instance to be injected</returns>
        T Resolve<T>(Type forType);
    }


    /// <summary>
    /// Code contracts
    /// </summary>
    [ContractClassFor(typeof(IInjectionResolver))]
    abstract class IInjectionResolverCodeContractCheck : IInjectionResolver
    {
        /// <summary>Code contracts</summary>
        private IInjectionResolverCodeContractCheck() { }


        public object Resolve(Type reqObjectType, string paramName, Type forType, object extData)
        {
            TurboContract.Requires(reqObjectType != null, conditionString: "reqObjectType != null");

            throw new NotImplementedException();
        }
        public T Resolve<T>(Type forType)
        {
            throw new NotImplementedException();
        }
    }
}
