using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo
{
    /// <summary>
    /// The exception that is thrown when item already exists in collection
    /// </summary>
    [Serializable]
    public class ItemAlreadyExistsException: Exception
    {
        /// <summary>
        /// ItemAlreadyExistsException constructor
        /// </summary>
        public ItemAlreadyExistsException() : base("Item already exists") { }
        /// <summary>
        /// ItemAlreadyExistsException constructor with error message
        /// </summary>
        /// <param name="message">Error message</param>
        public ItemAlreadyExistsException(string message) : base(message) { }
        /// <summary>
        /// ItemAlreadyExistsException constructor with error message and innerException
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="innerException">Inner exception</param>
        public ItemAlreadyExistsException(string message, Exception innerException) : base(message, innerException) { }

#if !NETSTANDARD1_X
        /// <summary>
        /// ItemAlreadyExistsException constructor for deserialization
        /// </summary>
        /// <param name="info">SerializationInfo</param>
        /// <param name="context">StreamingContext</param>
        protected ItemAlreadyExistsException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
#endif
    }
}
