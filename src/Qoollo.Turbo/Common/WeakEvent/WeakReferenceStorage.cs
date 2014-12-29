using System;

namespace Qoollo.Turbo
{
    /// <summary>
    /// Хранилище слабой ссылки на объект (не удерживаемой для GC)
    /// </summary>
    class WeakReferenceStorage : IWeakEventReferenceStorage
    {
        private WeakReference _reference = null;

        /// <summary>
        /// Собственно объект. Может быть null.
        /// </summary>
        public object Target
        {
            get
            {
                return _reference.Target;
            }
        }

        /// <summary>
        /// Конструктор WeakReferenceStorage
        /// </summary>
        /// <param name="reference">Объект</param>
        public WeakReferenceStorage(object reference)
        {
            _reference = new WeakReference(reference);
        }
    }
}