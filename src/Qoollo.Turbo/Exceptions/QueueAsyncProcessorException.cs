using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Qoollo.Turbo
{
    /// <summary>
    /// The exception that is thrown on any unhandled error during element processing by QueueAsyncProcessor
    /// </summary>
    [Serializable]
    public class QueueAsyncProcessorException : TurboException
    {
        /// <summary>
        /// QueueAsyncProcessorException constructor
        /// </summary>
        public QueueAsyncProcessorException() : base("Exception was thrown during processing in QueueAsyncProcessor") { }
        /// <summary>
        /// QueueAsyncProcessorException constructor with error message
        /// </summary>
        /// <param name="message">Error message</param>
        public QueueAsyncProcessorException(string message) : base(message) { }
        /// <summary>
        /// QueueAsyncProcessorException constructor with error message and innerException
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="innerException">Inner exception</param>
        public QueueAsyncProcessorException(string message, Exception innerException) : base(message, innerException) { }
        /// <summary>
        /// QueueAsyncProcessorException constructor for deserialization
        /// </summary>
        /// <param name="info">SerializationInfo</param>
        /// <param name="context">StreamingContext</param>
        protected QueueAsyncProcessorException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
