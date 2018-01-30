using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo
{
    /// <summary>
    /// Indicates error when specified type is invalid in the context of the operation or as a generic parameter
    /// </summary>
    [Serializable]
    public class InvalidTypeException: Exception
    {
        /// <summary>
        /// InvalidTypeException constructor
        /// </summary>
        public InvalidTypeException() : base("The specified type is invalid in the current context") { }
        /// <summary>
        /// InvalidTypeException constructor with type
        /// </summary>
        /// <param name="type">Specified type</param>
        public InvalidTypeException(Type type) : base("The specified type is invalid in the current context: " + type.Name) { }
        /// <summary>
        /// InvalidTypeException constructor with error message
        /// </summary>
        /// <param name="message">Error message</param>
        public InvalidTypeException(string message) : base(message) { }
        /// <summary>
        /// InvalidTypeException constructor with error message and innerException
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="innerException">Inner exception</param>
        public InvalidTypeException(string message, Exception innerException) : base(message, innerException) { }

#if !HAS_NO_SERIALIZABLE_ATTRIBUTE
        /// <summary>
        /// InvalidTypeException constructor for deserialization
        /// </summary>
        /// <param name="info">SerializationInfo</param>
        /// <param name="context">StreamingContext</param>
        protected InvalidTypeException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
#endif
    }
}
