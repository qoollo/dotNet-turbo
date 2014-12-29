using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo
{
    /// <summary>
    /// Исключение, возникающее при внешнем прерывании операции
    /// </summary>
    [Serializable]
    public class OperationInterruptedException : SystemException
    {
        /// <summary>
        /// Конструктор OperationInterruptedException
        /// </summary>
        public OperationInterruptedException() : base("Opearion was interrupted by some external event") { }
        /// <summary>
        /// Конструктор OperationInterruptedException
        /// </summary>
        /// <param name="message">Сообщение</param>
        public OperationInterruptedException(string message) : base(message) { }
        /// <summary>
        /// Конструктор OperationInterruptedException
        /// </summary>
        /// <param name="message">Сообщение</param>
        /// <param name="innerException">Внутреннее исключение</param>
        public OperationInterruptedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
        /// <summary>
        ///  Конструктор OperationInterruptedException для деериализации
        /// </summary>
        /// <param name="info">info</param>
        /// <param name="context">context</param>
        protected OperationInterruptedException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
        }
    }
}
