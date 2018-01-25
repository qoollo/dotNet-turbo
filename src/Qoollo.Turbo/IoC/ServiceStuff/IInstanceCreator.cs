using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.IoC.ServiceStuff
{
    /// <summary>
    /// Represents the object creator
    /// </summary>
    [ContractClass(typeof(IInstanceCreatorCodeContractCheck))]
    public interface IInstanceCreator
    {
        /// <summary>
        /// Create an instance of an object. All required parameters can be acquired from injection resolver.
        /// </summary>
        /// <param name="resolver">Injection resolver to acquire parameters</param>
        /// <returns>Created object</returns>
        object CreateInstance(IInjectionResolver resolver);
    }

    /// <summary>
    /// Represents the object creator
    /// </summary>
    [ContractClass(typeof(IInstanceCreatorNoParamCodeContractCheck))]
    public interface IInstanceCreatorNoParam
    {
        /// <summary>
        /// Create an instance of an object
        /// </summary>
        /// <returns>Created object</returns>
        object CreateInstance();
    }




    /// <summary>
    /// Code contracts
    /// </summary>
    [ContractClassFor(typeof(IInstanceCreator))]
    abstract class IInstanceCreatorCodeContractCheck : IInstanceCreator
    {
        /// <summary>Code contracts</summary>
        private IInstanceCreatorCodeContractCheck() { }


        public object CreateInstance(IInjectionResolver resolver)
        {
            TurboContract.Requires(resolver != null, conditionString: "resolver != null");
            TurboContract.Ensures(TurboContract.Result<object>() != null);

            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Code contracts
    /// </summary>
    [ContractClassFor(typeof(IInstanceCreatorNoParam))]
    abstract class IInstanceCreatorNoParamCodeContractCheck : IInstanceCreatorNoParam
    {
        /// <summary>Code contracts</summary>
        private IInstanceCreatorNoParamCodeContractCheck() { }


        public object CreateInstance()
        {
            TurboContract.Ensures(TurboContract.Result<object>() != null);

            throw new NotImplementedException();
        }
    }
}
