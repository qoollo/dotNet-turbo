using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Qoollo.Turbo
{
    /// <summary>
    /// Исключение для ситуаций, когда объект находится в состоянии, недопустимом для данной операции
    /// </summary>
    [Serializable]
    public class WrongStateException : InvalidOperationException
    {
        /// <summary>
        /// Конструктор WrongStateException без параметров
        /// </summary>
        public WrongStateException() : base("Object has inappropriate state for requested operation") { }
        /// <summary>
        /// Конструктор WrongStateException с сообщением об ошибке
        /// </summary>
        /// <param name="message">Сообщение об ошибке</param>
        public WrongStateException(string message) : base(message) { }
        /// <summary>
        /// Конструктор WrongStateException с сообщением об ошибке и внутренним исключением
        /// </summary>
        /// <param name="message">Сообщение об ошибке</param>
        /// <param name="innerException">Внутреннее исключение</param>
        public WrongStateException(string message, Exception innerException) : base(message, innerException) { }
    }
}
