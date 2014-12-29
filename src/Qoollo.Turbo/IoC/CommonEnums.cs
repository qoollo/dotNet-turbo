
namespace Qoollo.Turbo.IoC
{
    /// <summary>
    /// Способ инстанцирования объекта
    /// </summary>
    public enum ObjectInstantiationMode
    {
        /// <summary>
        /// Использовать единую копию объекта
        /// </summary>
        Singleton,
        /// <summary>
        /// Единая копия объекта с инициализацией при первом обращении
        /// </summary>
        DeferedSingleton,
        /// <summary>
        /// Одна копия объекта на каждый поток
        /// </summary>
        PerThread,
        /// <summary>
        /// Создавать объект при каждом вызове
        /// </summary>
        PerCall,
        /// <summary>
        /// Создавать объект при каждом вызове, но параметры конструктора получить лишь 1 раз при инициализации
        /// </summary>
        PerCallInlinedParams
    }

    /// <summary>
    /// Переопределение режима инстанцирования
    /// </summary>
    public enum OverrideObjectInstantiationMode
    {
        /// <summary>
        /// Не переопределять
        /// </summary>
        None,
        /// <summary>
        /// Переопределить в режим 'использование одной копии'
        /// </summary>
        ToSingleton,
        /// <summary>
        /// Переопределить в режим 'отложенный синглтон'
        /// </summary>
        ToDeferedSingleton,
        /// <summary>
        /// Переопределить в режим 'объект на поток'
        /// </summary>
        ToPerThread,
        /// <summary>
        /// Переопределить в режим 'создания на каждый вызов'
        /// </summary>
        ToPerCall,
        /// <summary>
        /// Переопределить в режим 'создания на каждый вызов с зашитыми параметрами'
        /// </summary>
        ToPerCallInlinedParams
    }
}