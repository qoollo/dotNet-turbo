using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Qoollo.Turbo.IoC.Lifetime;
using Qoollo.Turbo.IoC.Lifetime.Factories;
using Qoollo.Turbo.IoC.ServiceStuff;

namespace Qoollo.Turbo.IoC
{
    /// <summary>
    /// Фабрики для создания контейнеров, управляющих объектами
    /// </summary>
    public static class LifetimeFactories
    {
        /// <summary>
        /// Контракты
        /// </summary>
        [ContractInvariantMethod]
        private static void Invariant()
        {
            Contract.Invariant(Singleton != null);
            Contract.Invariant(DeferedSingleton != null);
            Contract.Invariant(PerThread != null);
            Contract.Invariant(PerCall != null);
            Contract.Invariant(PerCallInlinedParams != null); 
        }



        private static readonly SingletonLifetimeFactory _singleton = new SingletonLifetimeFactory();
        /// <summary>
        /// Фабрика для синглтона
        /// </summary>
        public static SingletonLifetimeFactory Singleton
        {
            get { return _singleton; }
        }

        private static readonly DeferedSingletonLifetimeFactory _deferedSingleton = new DeferedSingletonLifetimeFactory();
        /// <summary>
        /// Фабрика для синглтона с отложенной инициализацией
        /// </summary>
        public static DeferedSingletonLifetimeFactory DeferedSingleton
        {
            get { return _deferedSingleton; }
        }

        private static readonly PerThreadLifetimeFactory _perThread = new PerThreadLifetimeFactory();
        /// <summary>
        /// Фабрика для контейнера хранения объекта на каждый поток
        /// </summary>
        public static PerThreadLifetimeFactory PerThread
        {
            get { return _perThread; }
        }

        private static readonly PerCallLifetimeFactory _perCall = new PerCallLifetimeFactory();
        /// <summary>
        /// Фабрика для контейнера, который создаёт объект на каждый вызов
        /// </summary>
        public static PerCallLifetimeFactory PerCall
        {
            get { return _perCall; }
        }

        private static readonly PerCallInlinedParamsLifetimeFactory _perCallInlinedParams = new PerCallInlinedParamsLifetimeFactory();
        /// <summary>
        /// Фабрика для контейнера, который создаёт объект на каждый вызов с зашитыми параметрами
        /// </summary>
        public static PerCallInlinedParamsLifetimeFactory PerCallInlinedParams
        {
            get { return _perCallInlinedParams; }
        }


        /// <summary>
        /// Получение фабрики по режиму инстанцирования объекта
        /// </summary>
        /// <param name="instMode">Режим инстанцирования</param>
        /// <returns>Фабрика</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LifetimeFactory GetLifetimeFactory(ObjectInstantiationMode instMode)
        {
            Contract.Ensures(Contract.Result<LifetimeFactory>() != null);

            switch (instMode)
            {
                case ObjectInstantiationMode.Singleton:
                    return LifetimeFactories.Singleton;
                case ObjectInstantiationMode.DeferedSingleton:
                    return LifetimeFactories.DeferedSingleton;
                case ObjectInstantiationMode.PerThread:
                    return LifetimeFactories.PerThread;
                case ObjectInstantiationMode.PerCall:
                    return LifetimeFactories.PerCall;
                case ObjectInstantiationMode.PerCallInlinedParams:
                    return LifetimeFactories.PerCallInlinedParams;
            }
            Contract.Assert(false, "unknown ObjectInstantiationMode");
            throw new CommonIoCException("unknown ObjectInstantiationMode");
        }
    }
}
