using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo
{
    /// <summary>
    /// The exception that is thrown when attempting to modify a frozen object
    /// </summary>
    [Serializable]
    public class ObjectFrozenException : InvalidOperationException
    {
        /// <summary>
        /// ObjectFrozenException constructor
        /// </summary>
        public ObjectFrozenException() : base("Object can't be modified. It is in frozen state.") { }
        /// <summary>
        /// ObjectFrozenException constructor with error message
        /// </summary>
        /// <param name="message">Error message</param>
        public ObjectFrozenException(string message) : base(message) { }
        /// <summary>
        /// ObjectFrozenException constructor with error message and innerException
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="innerException">Inner exception</param>
        public ObjectFrozenException(string message, Exception innerException) : base(message, innerException) { }
#if HAS_SERIALIZABLE
        /// <summary>
        /// ObjectFrozenException constructor for deserialization
        /// </summary>
        /// <param name="info">SerializationInfo</param>
        /// <param name="context">StreamingContext</param>
        protected ObjectFrozenException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
#endif
    }
}
