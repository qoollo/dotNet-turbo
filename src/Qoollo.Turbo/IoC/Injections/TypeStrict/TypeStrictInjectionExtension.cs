using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.IoC.Injections
{
    /// <summary>
    /// Additional generic methods for Injection Containers where the key is the type of the injection object
    /// </summary>
    public static class TypeStrictInjectionExtension
    {
        /// <summary>
        /// Gets the injection object of the specified type
        /// </summary>
        /// <typeparam name="T">The type of requested injection object</typeparam>
        /// <param name="obj">Source InjectionContainer</param>
        /// <returns>Resolved object</returns>
        public static T GetInjection<T>(this GenericInjectionContainerBase<Type> obj)
        {
            return (T)obj.GetInjection(typeof(T));
        }

        /// <summary>
        /// Attempts to get the injection object of the specified type
        /// </summary>
        /// <typeparam name="T">The type of requested injection object</typeparam>
        /// <param name="obj">Source InjectionContainer</param>
        /// <param name="val">Resolved object if foundпеха</param>
        /// <returns>True if the injection object is registered for the specified key; overwise false</returns>
        public static bool TryGetInjection<T>(this GenericInjectionContainerBase<Type> obj, out T val)
        {
            object tmpVal = null;
            if (obj.TryGetInjection(typeof(T), out tmpVal))
            {
                val = (T)tmpVal;
                return true;
            }
            val = default(T);
            return false;
        }

        /// <summary>
        /// Determines whether the InjectionSource contains the object of the specified type
        /// </summary>
        /// <typeparam name="T">The type of injection object</typeparam>
        /// <param name="obj">Source InjectionContainer</param>
        /// <returns>True if the InjectionSource contains the object of the specified type</returns>
        public static bool Contains<T>(this GenericInjectionContainerBase<Type> obj)
        {
            return obj.Contains(typeof(T));
        }


        /// <summary>
        /// Adds a new injection to the container
        /// </summary>
        /// <typeparam name="T">The type of injection object</typeparam>
        /// <param name="obj">Source InjectionContainer</param>
        /// <param name="val">Object to add</param>
        public static void AddInjection<T>(this GenericInjectionContainerBase<Type> obj, T val)
        {
            obj.AddInjection(typeof(T), val);
        }


        /// <summary>
        /// Attempts to add a new injection to the container
        /// </summary>
        /// <typeparam name="T">The type of injection object</typeparam>
        /// <param name="obj">Source InjectionContainer</param>
        /// <param name="val">Object to add</param>
        /// <returns>True if the injection was added, that is InjectionContainer not contains lifetime container with the same key; overwise false</returns>
        public static bool TryAddInjection<T>(this GenericInjectionContainerBase<Type> obj, T val)
        {
            return obj.TryAddInjection(typeof(T), val);
        }


        /// <summary>
        /// Removes the injection of the specified type from the container
        /// </summary>
        /// <typeparam name="T">The type of injection object</typeparam>
        /// <param name="obj">Source InjectionContainer</param>
        /// <returns>True if the injection was presented in container</returns>
        public static bool RemoveInjection<T>(this GenericInjectionContainerBase<Type> obj)
        {
            return obj.RemoveInjection(typeof(T));
        }
    }
}
