using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo
{
    /// <summary>
    /// The exception that is thrown when item is not found in collection
    /// </summary>
    [Serializable]
    public class ItemNotFoundException: Exception
    {
        /// <summary>
        /// ItemNotFoundException constructor
        /// </summary>
        public ItemNotFoundException() : base("Item was not found") { }
        /// <summary>
        /// ItemNotFoundException constructor with error message
        /// </summary>
        /// <param name="message">Error message</param>
        public ItemNotFoundException(string message) : base(message) { }
        /// <summary>
        /// ItemNotFoundException constructor with error message and innerException
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="innerException">Inner exception</param>
        public ItemNotFoundException(string message, Exception innerException) : base(message, innerException) { }

#if !HAS_NO_SERIALIZABLE_ATTRIBUTE
        /// <summary>
        /// ItemNotFoundException constructor for deserialization
        /// </summary>
        /// <param name="info">SerializationInfo</param>
        /// <param name="context">StreamingContext</param>
        protected ItemNotFoundException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
#endif
    }
}
