
namespace Qoollo.Turbo.IoC
{
    /// <summary>
    /// Specifies an instantiation mode of the object inside IoC container
    /// </summary>
    public enum ObjectInstantiationMode
    {
        /// <summary>
        /// Always use a single instance of the object (SingletonLifetime container)
        /// </summary>
        Singleton,
        /// <summary>
        /// Use a single lazily initialized instance of the object (DeferedSingletonLifetime container)
        /// </summary>
        DeferedSingleton,
        /// <summary>
        /// Create a separate instance for every thread (PerThreadLifetime container)
        /// </summary>
        PerThread,
        /// <summary>
        /// Create a separate instance on every call (PerCallLifetime container)
        /// </summary>
        PerCall,
        /// <summary>
        /// Create a separate instance on every call (PerCallInlinedParamsLifetime container). Constructor parameters resolves only once.
        /// </summary>
        PerCallInlinedParams
    }

    /// <summary>
    /// Specifies the overrided value for ObjectInstantiationMode
    /// </summary>
    public enum OverrideObjectInstantiationMode
    {
        /// <summary>
        /// Object instantiation mode should not be overrided
        /// </summary>
        None,
        /// <summary>
        /// Overrides instantiation mode to Singleton
        /// </summary>
        ToSingleton,
        /// <summary>
        /// Overrides instantiation mode to DeferedSingleton
        /// </summary>
        ToDeferedSingleton,
        /// <summary>
        /// Overrides instantiation mode to PerThread
        /// </summary>
        ToPerThread,
        /// <summary>
        /// Overrides instantiation mode to PerCall
        /// </summary>
        ToPerCall,
        /// <summary>
        /// Overrides instantiation mode to PerCallInlinedParams
        /// </summary>
        ToPerCallInlinedParams
    }
}