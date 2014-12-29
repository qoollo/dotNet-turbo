using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo
{
    /// <summary>
    /// Исключение, когда не найден элемент
    /// </summary>
    [Serializable]
    public class ItemNotFoundException: SystemException
    {
        /// <summary>
        /// Конструктор ItemNotFoundException
        /// </summary>
        public ItemNotFoundException() : base("Item was not found") { }
        /// <summary>
        /// Конструктор ItemNotFoundException
        /// </summary>
        /// <param name="message">Сообщение</param>
        public ItemNotFoundException(string message) : base(message) { }
        /// <summary>
        /// Конструктор ItemNotFoundException
        /// </summary>
        /// <param name="message">Сообщение</param>
        /// <param name="innerException">Внутреннее исключение</param>
        public ItemNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
        /// <summary>
        ///  Конструктор ItemNotFoundException для деериализации
        /// </summary>
        /// <param name="info">info</param>
        /// <param name="context">context</param>
        protected ItemNotFoundException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
        }
    }
}
