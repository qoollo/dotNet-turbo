using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.IoC.Associations
{
    /// <summary>
    /// Additional generic overloads for all 'Add' methods for type strict association containers
    /// </summary>
    public static class TypeStrictAssociationExtension
    {
        /// <summary>
        /// Adds an object with singleton lifetime for the specified key
        /// </summary>
        /// <typeparam name="TSrc">The type that will be used as a key</typeparam>
        /// <typeparam name="TTarg">The type of the object that will be held by the singleton lifetime container</typeparam>
        /// <param name="obj">Association container to which the addition is performed</param>
        /// <param name="val">The object that will be held by the singleton lifetime container</param>
        public static void AddSingleton<TSrc, TTarg>(this IDirectSingletonAssociationSupport<Type> obj, TTarg val)
        {
            TurboContract.Requires(obj != null, conditionString: "obj != null");

            obj.AddSingleton(typeof(TSrc), (object)val);
        }

        /// <summary>
        /// Adds an object with singleton lifetime for the specified key
        /// </summary>
        /// <typeparam name="TSrc">The type that will be used as a key</typeparam>
        /// <typeparam name="TTarg">The type of the object that will be held by the singleton lifetime container</typeparam>
        /// <param name="obj">Association container to which the addition is performed</param>
        public static void AddSingleton<TSrc, TTarg>(this ISingletonAssociationSupport<Type> obj)
        {
            TurboContract.Requires(obj != null, conditionString: "obj != null");

            obj.AddSingleton(typeof(TSrc), typeof(TTarg));
        }

        /// <summary>
        /// Adds an object with lazily initialized singleton lifetime for the specified key
        /// </summary>
        /// <typeparam name="TSrc">The type that will be used as a key</typeparam>
        /// <typeparam name="TTarg">The type of the object that will be held by the DeferedSingleton lifetime container</typeparam>
        /// <param name="obj">Association container to which the addition is performed</param>
        public static void AddDeferedSingleton<TSrc, TTarg>(this IDeferedSingletonAssociationSupport<Type> obj)
        {
            TurboContract.Requires(obj != null, conditionString: "obj != null");

            obj.AddDeferedSingleton(typeof(TSrc), typeof(TTarg));
        }

        /// <summary>
        /// Adds an object with per thread lifetime for the specified key
        /// </summary>
        /// <typeparam name="TSrc">The type that will be used as a key</typeparam>
        /// <typeparam name="TTarg">The type of the object that will be held by the PerThread lifetime container</typeparam>
        /// <param name="obj">Association container to which the addition is performed</param>
        public static void AddPerThread<TSrc, TTarg>(this IPerThreadAssociationSupport<Type> obj)
        {
            TurboContract.Requires(obj != null, conditionString: "obj != null");

            obj.AddPerThread(typeof(TSrc), typeof(TTarg));
        }

        /// <summary>
        /// Adds an object with per call lifetime for the specified key
        /// </summary>
        /// <typeparam name="TSrc">The type that will be used as a key</typeparam>
        /// <typeparam name="TTarg">The type of the object that will be held by the PerCall lifetime container</typeparam>
        /// <param name="obj">Association container to which the addition is performed</param>
        public static void AddPerCall<TSrc, TTarg>(this IPerCallAssociationSupport<Type> obj)
        {
            TurboContract.Requires(obj != null, conditionString: "obj != null");

            obj.AddPerCall(typeof(TSrc), typeof(TTarg));
        }

        /// <summary>
        /// Adds an object with per call lifetime with inlined constructor parameters for the specified key
        /// </summary>
        /// <typeparam name="TSrc">The type that will be used as a key</typeparam>
        /// <typeparam name="TTarg">The type of the object that will be held by the PerCallInlinedParams lifetime container</typeparam>
        /// <param name="obj">Association container to which the addition is performed</param>
        public static void AddPerCallInlinedParams<TSrc, TTarg>(this IPerCallInlinedParamsAssociationSupport<Type> obj)
        {
            TurboContract.Requires(obj != null, conditionString: "obj != null");

            obj.AddPerCallInlinedParams(typeof(TSrc), typeof(TTarg));
        }


        /// <summary>
        /// Adds a lifetime object container for the specified key
        /// </summary>
        /// <typeparam name="TSrc">The type that will be used as a key</typeparam>
        /// <param name="obj">Association container to which the addition is performed</param>
        /// <param name="lifetimeContainer">Lifetime object container to add</param>
        public static void AddAssociation<TSrc>(this ICustomAssociationSupport<Type> obj, Lifetime.LifetimeBase lifetimeContainer)
        {
            TurboContract.Requires(obj != null, conditionString: "obj != null");
            TurboContract.Requires(lifetimeContainer != null, conditionString: "lifetimeContainer != null");

            obj.AddAssociation(typeof(TSrc), lifetimeContainer);
        }

        /// <summary>
        /// Adds a lifetime object container created by the 'factory' for the specified key
        /// </summary>
        /// <typeparam name="TSrc">The type that will be used as a key</typeparam>
        /// <param name="obj">Association container to which the addition is performed</param>
        /// <param name="objType">The type of the object that will be held by the lifetime container</param>
        /// <param name="factory">Lifetime object container to add</param>
        public static void AddAssociation<TSrc>(this ICustomAssociationSupport<Type> obj, Type objType, Lifetime.Factories.LifetimeFactory factory)
        {
            TurboContract.Requires(obj != null, conditionString: "obj != null");
            TurboContract.Requires(objType != null, conditionString: "objType != null");
            TurboContract.Requires(factory != null, conditionString: "factory != null");

            obj.AddAssociation(typeof(TSrc), objType, factory);
        }




        /// <summary>
        /// Adds an object with singleton lifetime for the specified key
        /// </summary>
        /// <typeparam name="TSrc">The type that will be used as a key</typeparam>
        /// <param name="obj">Association container to which the addition is performed</param>
        /// <param name="objType">The type of the object that will be held by the singleton lifetime container</param>
        public static void AddSingleton<TSrc>(this ISingletonAssociationSupport<Type> obj, Type objType)
        {
            TurboContract.Requires(obj != null, conditionString: "obj != null");
            TurboContract.Requires(objType != null, conditionString: "objType != null");

            obj.AddSingleton(typeof(TSrc), objType);
        }

        /// <summary>
        /// Adds an object with lazily initialized singleton lifetime for the specified key
        /// </summary>
        /// <typeparam name="TSrc">The type that will be used as a key</typeparam>
        /// <param name="obj">Association container to which the addition is performed</param>
        /// <param name="objType">The type of the object that will be held by the DeferedSingleton lifetime container</param>
        public static void AddDeferedSingleton<TSrc>(this IDeferedSingletonAssociationSupport<Type> obj, Type objType)
        {
            TurboContract.Requires(obj != null, conditionString: "obj != null");
            TurboContract.Requires(objType != null, conditionString: "objType != null");

            obj.AddDeferedSingleton(typeof(TSrc), objType);
        }

        /// <summary>
        /// Adds an object with per thread lifetime for the specified key
        /// </summary>
        /// <typeparam name="TSrc">The type that will be used as a key</typeparam>
        /// <param name="obj">Association container to which the addition is performed</param>
        /// <param name="objType">The type of the object that will be held by the PerThread lifetime container</param>
        public static void AddPerThread<TSrc>(this IPerThreadAssociationSupport<Type> obj, Type objType)
        {
            TurboContract.Requires(obj != null, conditionString: "obj != null");
            TurboContract.Requires(objType != null, conditionString: "objType != null");

            obj.AddPerThread(typeof(TSrc), objType);
        }

        /// <summary>
        /// Adds an object with per call lifetime for the specified key
        /// </summary>
        /// <typeparam name="TSrc">The type that will be used as a key</typeparam>
        /// <param name="obj">Association container to which the addition is performed</param>
        /// <param name="objType">The type of the object that will be held by the PerCall lifetime container</param>
        public static void AddPerCall<TSrc>(this IPerCallAssociationSupport<Type> obj, Type objType)
        {
            TurboContract.Requires(obj != null, conditionString: "obj != null");
            TurboContract.Requires(objType != null, conditionString: "objType != null");

            obj.AddPerCall(typeof(TSrc), objType);
        }

        /// <summary>
        /// Adds an object with per call lifetime with inlined constructor parameters for the specified key
        /// </summary>
        /// <typeparam name="TSrc">The type that will be used as a key</typeparam>
        /// <param name="obj">Association container to which the addition is performed</param>
        /// <param name="objType">The type of the object that will be held by the PerCallInlinedParams lifetime container</param>
        public static void AddPerCallInlinedParams<TSrc>(this IPerCallInlinedParamsAssociationSupport<Type> obj, Type objType)
        {
            TurboContract.Requires(obj != null, conditionString: "obj != null");
            TurboContract.Requires(objType != null, conditionString: "objType != null");

            obj.AddPerCallInlinedParams(typeof(TSrc), objType);
        }








        /// <summary>
        /// Attempts to add an object with singleton lifetime for the specified key
        /// </summary>
        /// <typeparam name="TSrc">The type that will be used as a key</typeparam>
        /// <typeparam name="TTarg">The type of the object that will be held by the singleton lifetime container</typeparam>
        /// <param name="obj">Association container to which the addition is performed</param>
        /// <param name="val">The object that will be held by the singleton lifetime container</param>
        /// <returns>True if the association was added successfully (that is AssociationContainer did not contained lifetime container with the same key); overwise false</returns>
        public static bool TryAddSingleton<TSrc, TTarg>(this IDirectSingletonAssociationSupport<Type> obj, TTarg val)
        {
            TurboContract.Requires(obj != null, conditionString: "obj != null");

            return obj.TryAddSingleton(typeof(TSrc), (object)val);
        }

        /// <summary>
        /// Attempts to add an object with singleton lifetime for the specified key
        /// </summary>
        /// <typeparam name="TSrc">The type that will be used as a key</typeparam>
        /// <typeparam name="TTarg">The type of the object that will be held by the singleton lifetime container</typeparam>
        /// <param name="obj">Association container to which the addition is performed</param>
        /// <returns>True if the association was added successfully (that is AssociationContainer did not contained lifetime container with the same key); overwise false</returns>
        public static bool TryAddSingleton<TSrc, TTarg>(this ISingletonAssociationSupport<Type> obj)
        {
            TurboContract.Requires(obj != null, conditionString: "obj != null");

            return obj.TryAddSingleton(typeof(TSrc), typeof(TTarg));
        }

        /// <summary>
        /// Attempts to add an object with lazily initialized singleton lifetime for the specified
        /// </summary>
        /// <typeparam name="TSrc">The type that will be used as a key</typeparam>
        /// <typeparam name="TTarg">The type of the object that will be held by the DeferedSignleton lifetime container</typeparam>
        /// <param name="obj">Association container to which the addition is performed</param>
        /// <returns>True if the association was added successfully (that is AssociationContainer did not contained lifetime container with the same key); overwise false</returns>
        public static bool TryAddDeferedSingleton<TSrc, TTarg>(this IDeferedSingletonAssociationSupport<Type> obj)
        {
            TurboContract.Requires(obj != null, conditionString: "obj != null");

            return obj.TryAddDeferedSingleton(typeof(TSrc), typeof(TTarg));
        }

        /// <summary>
        /// Attempts to add an object with per thread lifetime for the specified key
        /// </summary>
        /// <typeparam name="TSrc">The type that will be used as a key</typeparam>
        /// <typeparam name="TTarg">The type of the object that will be held by the PerThread lifetime container</typeparam>
        /// <param name="obj">Association container to which the addition is performed</param>
        /// <returns>True if the association was added successfully (that is AssociationContainer did not contained lifetime container with the same key); overwise false</returns>
        public static bool TryAddPerThread<TSrc, TTarg>(this IPerThreadAssociationSupport<Type> obj)
        {
            TurboContract.Requires(obj != null, conditionString: "obj != null");

            return obj.TryAddPerThread(typeof(TSrc), typeof(TTarg));
        }

        /// <summary>
        /// Attempts to add an object with per call lifetime for the specified key
        /// </summary>
        /// <typeparam name="TSrc">The type that will be used as a key</typeparam>
        /// <typeparam name="TTarg">The type of the object that will be held by the PerCall lifetime container</typeparam>
        /// <param name="obj">Association container to which the addition is performed</param>
        /// <returns>True if the association was added successfully (that is AssociationContainer did not contained lifetime container with the same key); overwise false</returns>
        public static bool TryAddPerCall<TSrc, TTarg>(this IPerCallAssociationSupport<Type> obj)
        {
            TurboContract.Requires(obj != null, conditionString: "obj != null");

            return obj.TryAddPerCall(typeof(TSrc), typeof(TTarg));
        }

        /// <summary>
        /// Attempts to add an object with per call lifetime with inlined constructor parameters for the specified key
        /// </summary>
        /// <typeparam name="TSrc">The type that will be used as a key</typeparam>
        /// <typeparam name="TTarg">The type of the object that will be held by the PerCallInlinedParams lifetime container</typeparam>
        /// <param name="obj">Association container to which the addition is performed</param>
        /// <returns>True if the association was added successfully (that is AssociationContainer did not contained lifetime container with the same key); overwise false</returns>
        public static bool TryAddPerCallInlinedParams<TSrc, TTarg>(this IPerCallInlinedParamsAssociationSupport<Type> obj)
        {
            TurboContract.Requires(obj != null, conditionString: "obj != null");

            return obj.TryAddPerCallInlinedParams(typeof(TSrc), typeof(TTarg));
        }


        /// <summary>
        /// Attempts to add a lifetime object container for the specified key
        /// </summary>
        /// <typeparam name="TSrc">The type that will be used as a key</typeparam>
        /// <param name="obj">Association container to which the addition is performed</param>
        /// <param name="lifetimeContainer">Lifetime object container to add</param>
        /// <returns>True if the association was added successfully (that is AssociationContainer did not contained lifetime container with the same key); overwise false</returns>
        public static bool TryAddAssociation<TSrc>(this ICustomAssociationSupport<Type> obj, Lifetime.LifetimeBase lifetimeContainer)
        {
            TurboContract.Requires(obj != null, conditionString: "obj != null");
            TurboContract.Requires(lifetimeContainer != null, conditionString: "lifetimeContainer != null");

            return obj.TryAddAssociation(typeof(TSrc), lifetimeContainer);
        }

        /// <summary>
        /// Attempts to add a lifetime object container created by the 'factory' for the specified key
        /// </summary>
        /// <typeparam name="TSrc">The type that will be used as a key</typeparam>
        /// <param name="obj">Association container to which the addition is performed</param>
        /// <param name="objType">The type of the object that will be held by the lifetime container</param>
        /// <param name="factory">Factory to create a lifetime container for the sepcified 'objType'</param>
        /// <returns>True if the association was added successfully (that is AssociationContainer did not contained lifetime container with the same key); overwise false</returns>
        public static bool TryAddAssociation<TSrc>(this ICustomAssociationSupport<Type> obj, Type objType, Lifetime.Factories.LifetimeFactory factory)
        {
            TurboContract.Requires(obj != null, conditionString: "obj != null");
            TurboContract.Requires(objType != null, conditionString: "objType != null");
            TurboContract.Requires(factory != null, conditionString: "factory != null");

            return obj.TryAddAssociation(typeof(TSrc), objType, factory);
        }



        /// <summary>
        /// Attempts to add an object with singleton lifetime for the specified key
        /// </summary>
        /// <typeparam name="TSrc">The type that will be used as a key</typeparam>
        /// <param name="obj">Association container to which the addition is performed</param>
        /// <param name="objType">The type of the object that will be held by the singleton lifetime container</param>
        /// <returns>True if the association was added successfully (that is AssociationContainer did not contained lifetime container with the same key); overwise false</returns>
        public static bool TryAddSingleton<TSrc>(this ISingletonAssociationSupport<Type> obj, Type objType)
        {
            TurboContract.Requires(obj != null, conditionString: "obj != null");
            TurboContract.Requires(objType != null, conditionString: "objType != null");

            return obj.TryAddSingleton(typeof(TSrc), objType);
        }

        /// <summary>
        /// Attempts to add an object with lazily initialized singleton lifetime for the specified key
        /// </summary>
        /// <typeparam name="TSrc">The type that will be used as a key</typeparam>
        /// <param name="obj">Association container to which the addition is performed</param>
        /// <param name="objType">The type of the object that will be held by the DeferedSingleton lifetime container</param>
        /// <returns>True if the association was added successfully (that is AssociationContainer did not contained lifetime container with the same key); overwise false</returns>
        public static bool TryAddDeferedSingleton<TSrc>(this IDeferedSingletonAssociationSupport<Type> obj, Type objType)
        {
            TurboContract.Requires(obj != null, conditionString: "obj != null");
            TurboContract.Requires(objType != null, conditionString: "objType != null");

            return obj.TryAddDeferedSingleton(typeof(TSrc), objType);
        }

        /// <summary>
        /// Attempts to add an object with per thread lifetime for the specified key
        /// </summary>
        /// <typeparam name="TSrc">The type that will be used as a key</typeparam>
        /// <param name="obj">Association container to which the addition is performed</param>
        /// <param name="objType">The type of the object that will be held by the PerThread lifetime container</param>
        /// <returns>True if the association was added successfully (that is AssociationContainer did not contained lifetime container with the same key); overwise false</returns>
        public static bool TryAddPerThread<TSrc>(this IPerThreadAssociationSupport<Type> obj, Type objType)
        {
            TurboContract.Requires(obj != null, conditionString: "obj != null");
            TurboContract.Requires(objType != null, conditionString: "objType != null");

            return obj.TryAddPerThread(typeof(TSrc), objType);
        }

        /// <summary>
        /// Attempts to add an object with per call lifetime for the specified key
        /// </summary>
        /// <typeparam name="TSrc">The type that will be used as a key</typeparam>
        /// <param name="obj">Association container to which the addition is performed</param>
        /// <param name="objType">The type of the object that will be held by the PerCall lifetime container</param>
        /// <returns>True if the association was added successfully (that is AssociationContainer did not contained lifetime container with the same key); overwise false</returns>
        public static bool TryAddPerCall<TSrc>(this IPerCallAssociationSupport<Type> obj, Type objType)
        {
            TurboContract.Requires(obj != null, conditionString: "obj != null");
            TurboContract.Requires(objType != null, conditionString: "objType != null");

            return obj.TryAddPerCall(typeof(TSrc), objType);
        }

        /// <summary>
        /// Attempts to add an object with per call lifetime with inlined constructor parameters for the specified key
        /// </summary>
        /// <typeparam name="TSrc">The type that will be used as a key</typeparam>
        /// <param name="obj">Association container to which the addition is performed</param>
        /// <param name="objType">The type of the object that will be held by the PerCallInlinedParams lifetime container</param>
        /// <returns>True if the association was added successfully (that is AssociationContainer did not contained lifetime container with the same key); overwise false</returns>
        public static bool TryAddPerCallInlinedParams<TSrc>(this IPerCallInlinedParamsAssociationSupport<Type> obj, Type objType)
        {
            TurboContract.Requires(obj != null, conditionString: "obj != null");
            TurboContract.Requires(objType != null, conditionString: "objType != null");

            return obj.TryAddPerCallInlinedParams(typeof(TSrc), objType);
        }
    }
}
