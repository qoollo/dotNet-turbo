using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Qoollo.Turbo.IoC.Lifetime;
using Qoollo.Turbo.IoC.Lifetime.Factories;
using Qoollo.Turbo.IoC.ServiceStuff;
using System.Diagnostics;

namespace Qoollo.Turbo.IoC
{
    /// <summary>
    /// A set of default lifetime factories
    /// </summary>
    public static class LifetimeFactories
    {
        private static readonly SingletonLifetimeFactory _singleton = new SingletonLifetimeFactory();
        /// <summary>
        /// Gets a SingletonLifetimeFactory instance
        /// </summary>
        public static SingletonLifetimeFactory Singleton
        {
            get { return _singleton; }
        }

        private static readonly DeferedSingletonLifetimeFactory _deferedSingleton = new DeferedSingletonLifetimeFactory();
        /// <summary>
        /// Gets a DeferedSingletonLifetimeFactory instance
        /// </summary>
        public static DeferedSingletonLifetimeFactory DeferedSingleton
        {
            get { return _deferedSingleton; }
        }

        private static readonly PerThreadLifetimeFactory _perThread = new PerThreadLifetimeFactory();
        /// <summary>
        /// Gets a PerThreadLifetimeFactory instance
        /// </summary>
        public static PerThreadLifetimeFactory PerThread
        {
            get { return _perThread; }
        }

        private static readonly PerCallLifetimeFactory _perCall = new PerCallLifetimeFactory();
        /// <summary>
        /// Gets a PerCallLifetimeFactory instance
        /// </summary>
        public static PerCallLifetimeFactory PerCall
        {
            get { return _perCall; }
        }

        private static readonly PerCallInlinedParamsLifetimeFactory _perCallInlinedParams = new PerCallInlinedParamsLifetimeFactory();
        /// <summary>
        /// Gets a PerCallInlinedParamsLifetimeFactory instance
        /// </summary>
        public static PerCallInlinedParamsLifetimeFactory PerCallInlinedParams
        {
            get { return _perCallInlinedParams; }
        }


        /// <summary>
        /// Returns LifetimeFactory for the speicified instantiation mode
        /// </summary>
        /// <param name="instMode">Instantiation mode</param>
        /// <returns>Corresponding LifetimeFactory</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LifetimeFactory GetLifetimeFactory(ObjectInstantiationMode instMode)
        {
            TurboContract.Ensures(TurboContract.Result<LifetimeFactory>() != null);

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
            
            throw new CommonIoCException($"Unknown ObjectInstantiationMode: {instMode}");
        }
    }
}
