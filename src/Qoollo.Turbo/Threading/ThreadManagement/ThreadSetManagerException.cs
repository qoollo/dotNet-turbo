using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Qoollo.Turbo.Threading.ThreadManagement
{
    /// <summary>
    /// The exception that is thrown on any unhandled error in processing thread of ThreadSetManager
    /// </summary>
    [Serializable]
    public class ThreadSetManagerException : TurboException
    {
        /// <summary>
        /// ThreadSetManagerException constructor
        /// </summary>
        public ThreadSetManagerException() : base("Exception was thrown during processing in ThreadManager") { }
        /// <summary>
        /// ThreadSetManagerException constructor with error message
        /// </summary>
        /// <param name="message">Error message</param>
        public ThreadSetManagerException(string message) : base(message) { }
        /// <summary>
        /// ThreadSetManagerException constructor with error message and innerException
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="innerException">Inner exception</param>
        public ThreadSetManagerException(string message, Exception innerException) : base(message, innerException) { }

#if !NETSTANDARD1_X
        /// <summary>
        /// ThreadSetManagerException constructor for deserialization
        /// </summary>
        /// <param name="info">SerializationInfo</param>
        /// <param name="context">StreamingContext</param>
        protected ThreadSetManagerException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
#endif
    }
}
