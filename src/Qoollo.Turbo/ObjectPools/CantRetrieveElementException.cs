using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Qoollo.Turbo.ObjectPools
{
    /// <summary>
    /// Exception thrown by ObjectPool when element can't be retrieved due to disposing or some other error
    /// </summary>
    [Serializable]
    public class CantRetrieveElementException : TurboException
    {
        /// <summary>
        /// CantRetrieveElementException constructor
        /// </summary>
        public CantRetrieveElementException() : base("Element was not retrieved due to some error") { }
        /// <summary>
        /// CantRetrieveElementException constructor with error message
        /// </summary>
        /// <param name="message">Error message</param>
        public CantRetrieveElementException(string message) : base(message) { }
        /// <summary>
        /// CantRetrieveElementException constructor with error message and innerException
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="innerException">Inner exception</param>
        public CantRetrieveElementException(string message, Exception innerException) : base(message, innerException) { }
        /// <summary>
        /// CantRetrieveElementException constructor for deserialization
        /// </summary>
        /// <param name="info">SerializationInfo</param>
        /// <param name="context">StreamingContext</param>
        protected CantRetrieveElementException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
