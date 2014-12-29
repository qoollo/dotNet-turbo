using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo
{
    /// <summary>
    /// Исключение, когда элемент уже присутсвует
    /// </summary>
    [Serializable]
    public class ItemAlreadyExistsException: SystemException
    {
        /// <summary>
        /// Конструктор ItemAlreadyExistsException
        /// </summary>
        public ItemAlreadyExistsException() : base("Item already exists") { }
        /// <summary>
        /// Конструктор ItemAlreadyExistsException
        /// </summary>
        /// <param name="message">Сообщение</param>
        public ItemAlreadyExistsException(string message) : base(message) { }
        /// <summary>
        /// Конструктор ItemAlreadyExistsException
        /// </summary>
        /// <param name="message">Сообщение</param>
        /// <param name="innerException">Внутреннее исключение</param>
        public ItemAlreadyExistsException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
        /// <summary>
        ///  Конструктор ItemAlreadyExistsException для деериализации
        /// </summary>
        /// <param name="info">info</param>
        /// <param name="context">context</param>
        protected ItemAlreadyExistsException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
        }
    }
}
