using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Qoollo.Turbo.IoC.Helpers;

namespace Qoollo.Turbo.IoC.ServiceStuff
{
    /// <summary>
    /// Provides a range of methods to create an object of the specified type
    /// </summary>
    public static class InstantiationService
    {
        /// <summary>
        /// Creates an instance of an object of 'objType' type using the parameterless constructor 
        /// </summary>
        /// <param name="objType">The type of the object to be created</param>
        /// <returns>Created object</returns>
        public static object CreateObject(Type objType)
        {
            Contract.Requires(objType != null);

            return OnjectInstantiationHelper.CreateObject(objType);
        }

        /// <summary>
        /// Creates an instance of an object of type 'T' using the default constructor
        /// </summary>
        /// <typeparam name="T">The type of the object to be created</typeparam>
        /// <param name="resolver">Injection source</param>
        /// <returns>Created object</returns>
        public static T CreateObject<T>(IInjectionResolver resolver)
        {
            Contract.Requires(resolver != null);

            return (T)OnjectInstantiationHelper.CreateObject(typeof(T), resolver, null);
        }

        /// <summary>
        /// Creates an instance of an object of type 'objType' using the default constructor
        /// </summary>
        /// <param name="objType">The type of the object to be created</param>
        /// <param name="resolver">Injection source</param>
        /// <returns>Created object</returns>
        public static object CreateObject(Type objType, IInjectionResolver resolver)
        {
            Contract.Requires(objType != null);
            Contract.Requires(resolver != null);

            return OnjectInstantiationHelper.CreateObject(objType, resolver, null);
        }
    }
}
