using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Qoollo.Turbo
{
    /// <summary>
    /// Исключение получения элемента (из пула)
    /// </summary>
    [Serializable]
    public class CantRetrieveElementException : Exception
    {
        /// <summary>
        /// Конструктор CantRetrieveElementException без параметров
        /// </summary>
        public CantRetrieveElementException() : base("Element was not retrieved due to some error") { }
        /// <summary>
        /// Конструктор CantRetrieveElementException с сообщением об ошибке
        /// </summary>
        /// <param name="message">Сообщение об ошибке</param>
        public CantRetrieveElementException(string message) : base(message) { }
        /// <summary>
        /// Конструктор CantRetrieveElementException с сообщением об ошибке и внутренним исключением
        /// </summary>
        /// <param name="message">Сообщение об ошибке</param>
        /// <param name="innerException">Внутреннее исключение</param>
        public CantRetrieveElementException(string message, Exception innerException) : base(message, innerException) { }
    }
}
