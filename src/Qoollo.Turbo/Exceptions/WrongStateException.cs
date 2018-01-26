using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Qoollo.Turbo
{
    /// <summary>
    /// The exception that is thrown when attempting to perform an operation that is invalid for the current state of the object
    /// </summary>
    [Serializable]
    public class WrongStateException : InvalidOperationException
    {
        /// <summary>
        /// WrongStateException constructor
        /// </summary>
        public WrongStateException() : base("Object has inappropriate state for the requested operation") { }
        /// <summary>
        /// WrongStateException constructor with error message
        /// </summary>
        /// <param name="message">Error message</param>
        public WrongStateException(string message) : base(message) { }
        /// <summary>
        /// WrongStateException constructor with error message and innerException
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="innerException">Inner exception</param>
        public WrongStateException(string message, Exception innerException) : base(message, innerException) { }

#if HAS_SERIALIZABLE
        /// <summary>
        /// WrongStateException constructor for deserialization
        /// </summary>
        /// <param name="info">SerializationInfo</param>
        /// <param name="context">StreamingContext</param>
        protected WrongStateException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
#endif
    }
}
