using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Qoollo.Turbo
{
    /// <summary>
    /// Исключение при обработке элемента в QueueAsyncProcessor
    /// </summary>
    [Serializable]
    public class QueueAsyncProcessorException : Exception
    {
        /// <summary>
        /// Конструктор QueueAsyncProcessorException без параметров
        /// </summary>
        public QueueAsyncProcessorException() : base("Exception was thrown during processing in QueueAsyncProcessor") { }
        /// <summary>
        /// Конструктор QueueAsyncProcessorException с сообщением об ошибке
        /// </summary>
        /// <param name="message">Сообщение об ошибке</param>
        public QueueAsyncProcessorException(string message) : base(message) { }
        /// <summary>
        /// Конструктор QueueAsyncProcessorException с сообщением об ошибке и внутренним исключением
        /// </summary>
        /// <param name="message">Сообщение об ошибке</param>
        /// <param name="innerException">Внутреннее исключение</param>
        public QueueAsyncProcessorException(string message, Exception innerException) : base(message, innerException) { }
    }
}
