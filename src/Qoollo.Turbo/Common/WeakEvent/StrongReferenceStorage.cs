namespace Qoollo.Turbo
{
    /// <summary>
    /// Strong reference to the object
    /// </summary>
    internal class StrongReferenceStorage : WeakEventReferenceStorageBase
    {
        private object _target = null;

        /// <summary>
        /// StrongReferenceStorage constructor
        /// </summary>
        /// <param name="target">Object to store</param>
        public StrongReferenceStorage(object target)
        {
            _target = target;
        }

        /// <summary>
        /// Gets the object from the storage
        /// </summary>
        public override object Target { get { return _target; } }
        /// <summary>
        /// Is stored object still alive
        /// </summary>
        public override bool IsAlive { get { return true; } }
    }
}