using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
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
            Contract.Requires(obj != null);

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
            Contract.Requires(obj != null);

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
            Contract.Requires(obj != null);

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
            Contract.Requires(obj != null);

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
            Contract.Requires(obj != null);

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
            Contract.Requires(obj != null);

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
            Contract.Requires(obj != null);
            Contract.Requires<ArgumentNullException>(lifetimeContainer != null);

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
            Contract.Requires(obj != null);
            Contract.Requires<ArgumentNullException>(objType != null);
            Contract.Requires<ArgumentNullException>(factory != null);

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
            Contract.Requires(obj != null);
            Contract.Requires<ArgumentNullException>(objType != null);

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
            Contract.Requires(obj != null);
            Contract.Requires<ArgumentNullException>(objType != null);

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
            Contract.Requires(obj != null);
            Contract.Requires<ArgumentNullException>(objType != null);

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
            Contract.Requires(obj != null);
            Contract.Requires<ArgumentNullException>(objType != null);

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
            Contract.Requires(obj != null);
            Contract.Requires<ArgumentNullException>(objType != null);

            obj.AddPerCallInlinedParams(typeof(TSrc), objType);
        }








        /// <summary>
        /// Попытаться добавить синглтон
        /// </summary>
        /// <typeparam name="TSrc">The type that will be used as a key</typeparam>
        /// <typeparam name="TTarg">Тип синглтона</typeparam>
        /// <param name="obj">Association container to which the addition is performed</param>
        /// <param name="val">Значение синглтона</param>
        /// <returns>Успешность</returns>
        public static bool TryAddSingleton<TSrc, TTarg>(this IDirectSingletonAssociationSupport<Type> obj, TTarg val)
        {
            Contract.Requires(obj != null);

            return obj.TryAddSingleton(typeof(TSrc), (object)val);
        }

        /// <summary>
        /// Попытаться добавить синглтон
        /// </summary>
        /// <typeparam name="TSrc">The type that will be used as a key</typeparam>
        /// <typeparam name="TTarg">Тип синглтона</typeparam>
        /// <param name="obj">Association container to which the addition is performed</param>
        /// <returns>Успешность</returns>
        public static bool TryAddSingleton<TSrc, TTarg>(this ISingletonAssociationSupport<Type> obj)
        {
            Contract.Requires(obj != null);

            return obj.TryAddSingleton(typeof(TSrc), typeof(TTarg));
        }

        /// <summary>
        /// Попытаться добавить синглтон с отложенной инициализацией
        /// </summary>
        /// <typeparam name="TSrc">The type that will be used as a key</typeparam>
        /// <typeparam name="TTarg">Тип синглтона</typeparam>
        /// <param name="obj">Association container to which the addition is performed</param>
        /// <returns>Успешность</returns>
        public static bool TryAddDeferedSingleton<TSrc, TTarg>(this IDeferedSingletonAssociationSupport<Type> obj)
        {
            Contract.Requires(obj != null);

            return obj.TryAddDeferedSingleton(typeof(TSrc), typeof(TTarg));
        }

        /// <summary>
        /// Попытаться добавить PerThread
        /// </summary>
        /// <typeparam name="TSrc">The type that will be used as a key</typeparam>
        /// <typeparam name="TTarg">Тип объекта</typeparam>
        /// <param name="obj">Association container to which the addition is performed</param>
        /// <returns>Успешность</returns>
        public static bool TryAddPerThread<TSrc, TTarg>(this IPerThreadAssociationSupport<Type> obj)
        {
            Contract.Requires(obj != null);

            return obj.TryAddPerThread(typeof(TSrc), typeof(TTarg));
        }

        /// <summary>
        /// Попытаться добавить PerCall
        /// </summary>
        /// <typeparam name="TSrc">The type that will be used as a key</typeparam>
        /// <typeparam name="TTarg">Тип объекта</typeparam>
        /// <param name="obj">Association container to which the addition is performed</param>
        /// <returns>Успешность</returns>
        public static bool TryAddPerCall<TSrc, TTarg>(this IPerCallAssociationSupport<Type> obj)
        {
            Contract.Requires(obj != null);

            return obj.TryAddPerCall(typeof(TSrc), typeof(TTarg));
        }

        /// <summary>
        /// Попытаться добавить PerCall с зашитыми параметрами создания
        /// </summary>
        /// <typeparam name="TSrc">The type that will be used as a key</typeparam>
        /// <typeparam name="TTarg">Тип объекта</typeparam>
        /// <param name="obj">Association container to which the addition is performed</param>
        /// <returns>Успешность</returns>
        public static bool TryAddPerCallInlinedParams<TSrc, TTarg>(this IPerCallInlinedParamsAssociationSupport<Type> obj)
        {
            Contract.Requires(obj != null);

            return obj.TryAddPerCallInlinedParams(typeof(TSrc), typeof(TTarg));
        }


        /// <summary>
        /// Попытаться добавить ассоциацию
        /// </summary>
        /// <typeparam name="TSrc">The type that will be used as a key</typeparam>
        /// <param name="obj">Association container to which the addition is performed</param>
        /// <param name="lifetimeContainer">Lifetime контейнер</param>
        /// <returns>Успешность</returns>
        public static bool TryAddAssociation<TSrc>(this ICustomAssociationSupport<Type> obj, Lifetime.LifetimeBase lifetimeContainer)
        {
            Contract.Requires(obj != null);
            Contract.Requires<ArgumentNullException>(lifetimeContainer != null);

            return obj.TryAddAssociation(typeof(TSrc), lifetimeContainer);
        }

        /// <summary>
        /// Попытаться добавить ассоциацию
        /// </summary>
        /// <typeparam name="TSrc">The type that will be used as a key</typeparam>
        /// <param name="obj">Association container to which the addition is performed</param>
        /// <param name="objType">Тип объекта, которым будет управлять Lifetime контейнер</param>
        /// <param name="factory">Фабрика</param>
        /// <returns>Успешность</returns>
        public static bool TryAddAssociation<TSrc>(this ICustomAssociationSupport<Type> obj, Type objType, Lifetime.Factories.LifetimeFactory factory)
        {
            Contract.Requires(obj != null);
            Contract.Requires<ArgumentNullException>(objType != null);
            Contract.Requires<ArgumentNullException>(factory != null);

            return obj.TryAddAssociation(typeof(TSrc), objType, factory);
        }



        /// <summary>
        /// Попытаться добавить синглтон
        /// </summary>
        /// <typeparam name="TSrc">The type that will be used as a key</typeparam>
        /// <param name="obj">Association container to which the addition is performed</param>
        /// <param name="objType">Тип синглтона</param>
        /// <returns>Успешность</returns>
        public static bool TryAddSingleton<TSrc>(this ISingletonAssociationSupport<Type> obj, Type objType)
        {
            Contract.Requires(obj != null);
            Contract.Requires<ArgumentNullException>(objType != null);

            return obj.TryAddSingleton(typeof(TSrc), objType);
        }

        /// <summary>
        /// Попытаться добавить синглтон с отложенной инициализацией
        /// </summary>
        /// <typeparam name="TSrc">The type that will be used as a key</typeparam>
        /// <param name="obj">Association container to which the addition is performed</param>
        /// <param name="objType">Тип синглтона</param>
        /// <returns>Успешность</returns>
        public static bool TryAddDeferedSingleton<TSrc>(this IDeferedSingletonAssociationSupport<Type> obj, Type objType)
        {
            Contract.Requires(obj != null);
            Contract.Requires<ArgumentNullException>(objType != null);

            return obj.TryAddDeferedSingleton(typeof(TSrc), objType);
        }

        /// <summary>
        /// Попытаться добавить PerThread
        /// </summary>
        /// <typeparam name="TSrc">The type that will be used as a key</typeparam>
        /// <param name="obj">Association container to which the addition is performed</param>
        /// <param name="objType">Тип объекта</param>
        /// <returns>Успешность</returns>
        public static bool TryAddPerThread<TSrc>(this IPerThreadAssociationSupport<Type> obj, Type objType)
        {
            Contract.Requires(obj != null);
            Contract.Requires<ArgumentNullException>(objType != null);

            return obj.TryAddPerThread(typeof(TSrc), objType);
        }

        /// <summary>
        /// Попытаться добавить PerCall
        /// </summary>
        /// <typeparam name="TSrc">The type that will be used as a key</typeparam>
        /// <param name="obj">Association container to which the addition is performed</param>
        /// <param name="objType">Тип объекта</param>
        /// <returns>Успешность</returns>
        public static bool TryAddPerCall<TSrc>(this IPerCallAssociationSupport<Type> obj, Type objType)
        {
            Contract.Requires(obj != null);
            Contract.Requires<ArgumentNullException>(objType != null);

            return obj.TryAddPerCall(typeof(TSrc), objType);
        }

        /// <summary>
        /// Попытаться добавить PerCall с зашитыми параметрами инициализации
        /// </summary>
        /// <typeparam name="TSrc">The type that will be used as a key</typeparam>
        /// <param name="obj">Association container to which the addition is performed</param>
        /// <param name="objType">Тип объекта</param>
        /// <returns>Успешность</returns>
        public static bool TryAddPerCallInlinedParams<TSrc>(this IPerCallInlinedParamsAssociationSupport<Type> obj, Type objType)
        {
            Contract.Requires(obj != null);
            Contract.Requires<ArgumentNullException>(objType != null);

            return obj.TryAddPerCallInlinedParams(typeof(TSrc), objType);
        }
    }
}
