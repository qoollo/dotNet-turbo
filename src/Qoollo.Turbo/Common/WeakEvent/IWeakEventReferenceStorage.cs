namespace Qoollo.Turbo
{
    /// <summary>
    /// Интерфейс хранилища ссылки на объект
    /// </summary>
    internal interface IWeakEventReferenceStorage
    {
        /// <summary>
        /// Собственно объект
        /// </summary>
        object Target { get; }
    }
}