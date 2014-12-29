using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo
{
    /// <summary>
    /// Исключение при изменении замороженного объекта
    /// </summary>
    [Serializable]
    public class ObjectFrozenException : InvalidOperationException
    {
        /// <summary>
        /// Конструктор ObjectFrozenException без параметров
        /// </summary>
        public ObjectFrozenException() : base("Object can't be modified. It is frozen.") { }
        /// <summary>
        /// Конструктор ObjectFrozenException с сообщением об ошибке
        /// </summary>
        /// <param name="message">Сообщение об ошибке</param>
        public ObjectFrozenException(string message) : base(message) { }
        /// <summary>
        /// Конструктор ObjectFrozenException с сообщением об ошибке и внутренним исключением
        /// </summary>
        /// <param name="message">Сообщение об ошибке</param>
        /// <param name="innerException">Внутреннее исключение</param>
        public ObjectFrozenException(string message, Exception innerException) : base(message, innerException) { }
    }
}
