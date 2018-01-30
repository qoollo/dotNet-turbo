using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Threading.ThreadPools
{
    /// <summary>
    /// Indicates error happend in ThreadPool
    /// </summary>
    [Serializable]
    public class ThreadPoolException : TurboException
    {
        /// <summary>
        /// ThreadPoolException constructor
        /// </summary>
        public ThreadPoolException() : base("Error in ThreadPool") { }
        /// <summary>
        /// ThreadPoolException constructor with error message
        /// </summary>
        /// <param name="message">Error message</param>
        public ThreadPoolException(string message) : base(message) { }
        /// <summary>
        /// ThreadPoolException constructor with error message and innerException
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="innerException">Inner exception</param>
        public ThreadPoolException(string message, Exception innerException) : base(message, innerException) { }

#if !NETSTANDARD1_X
        /// <summary>
        /// ThreadPoolException constructor for deserialization
        /// </summary>
        /// <param name="info">SerializationInfo</param>
        /// <param name="context">StreamingContext</param>
        protected ThreadPoolException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
#endif
    }


    /// <summary>
    /// Indicates error during a thread initialization in ThreadPool
    /// </summary>
    [Serializable]
    public class CantInitThreadException : ThreadPoolException
    {
        /// <summary>
        /// CantInitThreadException constructor
        /// </summary>
        public CantInitThreadException() : base("Error during new thread initialization") { }
        /// <summary>
        /// CantInitThreadException constructor with error message
        /// </summary>
        /// <param name="message">Error message</param>
        public CantInitThreadException(string message) : base(message) { }
        /// <summary>
        /// CantInitThreadException constructor with error message and innerException
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="innerException">Inner exception</param>
        public CantInitThreadException(string message, Exception innerException) : base(message, innerException) { }

#if !NETSTANDARD1_X
        /// <summary>
        /// CantInitThreadException constructor for deserialization
        /// </summary>
        /// <param name="info">SerializationInfo</param>
        /// <param name="context">StreamingContext</param>
        protected CantInitThreadException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
#endif
    }
}
