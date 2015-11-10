using System;

namespace Qoollo.Turbo
{
    /// <summary>
    /// Weak reference storage
    /// </summary>
    internal class WeakReferenceStorage : WeakEventReferenceStorageBase
    {
        private WeakReference _reference = null;

        /// <summary>
        /// WeakReferenceStorage constructor
        /// </summary>
        /// <param name="reference">Object to store</param>
        public WeakReferenceStorage(object reference)
        {
            _reference = new WeakReference(reference);
        }

        /// <summary>
        /// Gets the object from the storage (returns null if the object was collected by GC)
        /// </summary>
        public override object Target { get { return _reference.Target; } }
        /// <summary>
        /// Is stored object still alive
        /// </summary>
        public override bool IsAlive { get { return _reference.IsAlive; } }
    }
}