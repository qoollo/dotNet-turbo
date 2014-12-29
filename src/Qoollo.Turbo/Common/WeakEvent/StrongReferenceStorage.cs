namespace Qoollo.Turbo
{
    /// <summary>
    /// Жёсткая ссылка на объект (держит GC)
    /// </summary>
    class StrongReferenceStorage : IWeakEventReferenceStorage
    {
        private object _target = null;

        /// <summary>
        /// Собственно объект
        /// </summary>
        public object Target
        {
            get
            {
                return _target;
            }
        }

        /// <summary>
        /// Конструктор StrongReferenceStorage
        /// </summary>
        /// <param name="target">Хранимый объект</param>
        public StrongReferenceStorage(object target)
        {
            _target = target;
        }
    }
}