using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Qoollo.Turbo
{
    /// <summary>
    /// Исключение при работе ThreadManager
    /// </summary>
    [Serializable]
    public class ThreadSetManagerException : Exception
    {
        /// <summary>
        /// Конструктор ThreadManagerException без параметров
        /// </summary>
        public ThreadSetManagerException() : base("Exception was thrown during processing in ThreadManager") { }
        /// <summary>
        /// Конструктор ThreadManagerException с сообщением об ошибке
        /// </summary>
        /// <param name="message">Сообщение об ошибке</param>
        public ThreadSetManagerException(string message) : base(message) { }
        /// <summary>
        /// Конструктор ThreadManagerException с сообщением об ошибке и внутренним исключением
        /// </summary>
        /// <param name="message">Сообщение об ошибке</param>
        /// <param name="innerException">Внутреннее исключение</param>
        public ThreadSetManagerException(string message, Exception innerException) : base(message, innerException) { }
    }
}
