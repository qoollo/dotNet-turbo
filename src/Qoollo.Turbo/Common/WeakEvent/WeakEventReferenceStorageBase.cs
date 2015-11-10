namespace Qoollo.Turbo
{
    /// <summary>
    /// Stores the reference to the original object
    /// </summary>
    internal abstract class WeakEventReferenceStorageBase
    {
        /// <summary>
        /// Gets the object from the storage (can be null)
        /// </summary>
        public abstract object Target { get; }
        /// <summary>
        /// Is stored object still alive
        /// </summary>
        public abstract bool IsAlive { get; }
    }
}