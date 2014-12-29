using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo
{
    /// <summary>
    /// Исключение создания потока в пуле
    /// </summary>
    [Serializable]
    public class CantInitThreadException : Exception
    {
        /// <summary>
        /// Конструктор CantInitThreadException без параметров
        /// </summary>
        public CantInitThreadException() : base("Error during new thread initialization") { }
        /// <summary>
        /// Конструктор CantInitThreadException с сообщением об ошибке
        /// </summary>
        /// <param name="message">Сообщение об ошибке</param>
        public CantInitThreadException(string message) : base(message) { }
        /// <summary>
        /// Конструктор CantInitThreadException с сообщением об ошибке и внутренним исключением
        /// </summary>
        /// <param name="message">Сообщение об ошибке</param>
        /// <param name="innerException">Внутреннее исключение</param>
        public CantInitThreadException(string message, Exception innerException) : base(message, innerException) { }
    }
}
