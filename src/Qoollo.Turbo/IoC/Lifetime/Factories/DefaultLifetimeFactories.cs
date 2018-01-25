using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Qoollo.Turbo.IoC.Helpers;
using Qoollo.Turbo.IoC.ServiceStuff;

namespace Qoollo.Turbo.IoC.Lifetime.Factories
{
    /// <summary>
    /// Base class for LifetimeFactory that creates a Lifetime container for object
    /// </summary>
    [ContractClass(typeof(LifetimeFactoryCodeContractCheck))]
    public abstract class LifetimeFactory
    {
        /// <summary>
        /// Creates a lifetime container that resolves an instance of the specified object type
        /// </summary>
        /// <param name="objType">The type of the object that will be held by the lifetime container</param>
        /// <param name="injection">Injection resolver that will be used to create an instance if required</param>
        /// <param name="extInfo">Extended information supplied by the user (can be null)</param>
        /// <returns>Created lifetime container for the specified object type</returns>
        public abstract LifetimeBase Create(Type objType, IInjectionResolver injection, object extInfo);
    }

    /// <summary>
    /// LifetimeFactory that creates a SingletonLifetime container
    /// </summary>
    public sealed class SingletonLifetimeFactory: LifetimeFactory
    {
        /// <summary>
        /// Creates a SingletonLifetime container that resolves an instance of the specified object type
        /// </summary>
        /// <param name="objType">The type of the object that will be held by the lifetime container</param>
        /// <param name="injection">Injection resolver that will be used to create an instance if required</param>
        /// <param name="extInfo">Extended information supplied by the user (can be null)</param>
        /// <returns>Created lifetime container for the specified object type</returns>
        public override LifetimeBase Create(Type objType, IInjectionResolver injection, object extInfo)
        {
            if (objType == null)
                throw new ArgumentNullException(nameof(objType));
            if (injection == null)
                throw new ArgumentNullException(nameof(injection));

            var obj = ObjectInstantiationHelper.CreateObject(objType, injection, extInfo);
            return new SingletonLifetime(obj);
        }
    }

    /// <summary>
    /// LifetimeFactory that creates a DeferedSingletonLifetime container
    /// </summary>
    public sealed class DeferedSingletonLifetimeFactory : LifetimeFactory
    {
        /// <summary>
        /// Creates a DeferedSingletonLifetime container that resolves an instance of the specified object type
        /// </summary>
        /// <param name="objType">The type of the object that will be held by the lifetime container</param>
        /// <param name="injection">Injection resolver that will be used to create an instance if required</param>
        /// <param name="extInfo">Extended information supplied by the user (can be null)</param>
        /// <returns>Created lifetime container for the specified object type</returns>
        public override LifetimeBase Create(Type objType, IInjectionResolver injection, object extInfo)
        {
            if (objType == null)
                throw new ArgumentNullException(nameof(objType));
            if (injection == null)
                throw new ArgumentNullException(nameof(injection));

            var creationFunc = ObjectInstantiationHelper.GetReflectionBasedCreationFunction(objType, extInfo);
            return new DeferedSingletonLifetime(creationFunc, objType);
        }
    }

    /// <summary>
    /// LifetimeFactory that creates a PerThreadLifetime container
    /// </summary>
    public sealed class PerThreadLifetimeFactory : LifetimeFactory
    {
        /// <summary>
        /// Creates a PerThreadLifetime container that resolves an instance of the specified object type
        /// </summary>
        /// <param name="objType">The type of the object that will be held by the lifetime container</param>
        /// <param name="injection">Injection resolver that will be used to create an instance if required</param>
        /// <param name="extInfo">Extended information supplied by the user (can be null)</param>
        /// <returns>Created lifetime container for the specified object type</returns>
        public override LifetimeBase Create(Type objType, IInjectionResolver injection, object extInfo)
        {
            if (objType == null)
                throw new ArgumentNullException(nameof(objType));
            if (injection == null)
                throw new ArgumentNullException(nameof(injection));

            var creationFunc = ObjectInstantiationHelper.GetCompiledCreationFunction(objType, extInfo);
            return new PerThreadLifetime(creationFunc, objType);
        }
    }

    /// <summary>
    /// LifetimeFactory that creates a PerCallInterfaceLifetime container
    /// </summary>
    public sealed class PerCallLifetimeFactory : LifetimeFactory
    {
        /// <summary>
        /// Creates a PerCallInterfaceLifetime container that resolves an instance of the specified object type
        /// </summary>
        /// <param name="objType">The type of the object that will be held by the lifetime container</param>
        /// <param name="injection">Injection resolver that will be used to create an instance if required</param>
        /// <param name="extInfo">Extended information supplied by the user (can be null)</param>
        /// <returns>Created lifetime container for the specified object type</returns>
        public override LifetimeBase Create(Type objType, IInjectionResolver injection, object extInfo)
        {
            if (objType == null)
                throw new ArgumentNullException(nameof(objType));
            if (injection == null)
                throw new ArgumentNullException(nameof(injection));

            var creationObj = ObjectInstantiationHelper.BuildInstanceCreatorInDynAssembly(objType, extInfo);
            return new PerCallInterfaceLifetime(objType, creationObj);
        }
    }

    /// <summary>
    /// LifetimeFactory that creates a PerCallInlinedParamsInterfaceLifetime container
    /// </summary>
    public sealed class PerCallInlinedParamsLifetimeFactory : LifetimeFactory
    {
        /// <summary>
        /// Creates a PerCallInlinedParamsInterfaceLifetime container that resolves an instance of the specified object type
        /// </summary>
        /// <param name="objType">The type of the object that will be held by the lifetime container</param>
        /// <param name="injection">Injection resolver that will be used to create an instance if required</param>
        /// <param name="extInfo">Extended information supplied by the user (can be null)</param>
        /// <returns>Created lifetime container for the specified object type</returns>
        public override LifetimeBase Create(Type objType, IInjectionResolver injection, object extInfo)
        {
            if (objType == null)
                throw new ArgumentNullException(nameof(objType));
            if (injection == null)
                throw new ArgumentNullException(nameof(injection));

            var creationObj = ObjectInstantiationHelper.BuildInstanceCreatorNoParamInDynAssembly(objType, injection, extInfo);
            return new PerCallInlinedParamsInterfaceLifetime(objType, creationObj);
        }
    }









    /// <summary>
    /// Code contracts
    /// </summary>
    [ContractClassFor(typeof(LifetimeFactory))]
    abstract class LifetimeFactoryCodeContractCheck : LifetimeFactory
    {
        /// <summary>Code contracts</summary>
        private LifetimeFactoryCodeContractCheck() { }


        public override LifetimeBase Create(Type objType, IInjectionResolver injection, object extInfo)
        {
            TurboContract.Requires(objType != null, conditionString: "objType != null");
            TurboContract.Requires(injection != null, conditionString: "injection != null");
            TurboContract.Ensures(TurboContract.Result<LifetimeBase>() != null);
            TurboContract.Ensures(TurboContract.Result<LifetimeBase>().OutputType == objType);

            throw new NotImplementedException();
        }
    }

}
