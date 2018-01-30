using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo
{
    /// <summary>
    /// The exception that is thrown when some assertion is incorrect
    /// </summary>
    [Serializable]
    public class TurboAssertionException: TurboException
    {
        /// <summary>
        /// TurboAssertionException constructor
        /// </summary>
        public TurboAssertionException() : base("Incorrect assertion") { }
        /// <summary>
        /// TurboAssertionException constructor with error message
        /// </summary>
        /// <param name="message">Error message</param>
        public TurboAssertionException(string message) : base(message) { }
        /// <summary>
        /// TurboAssertionException constructor with error message and innerException
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="innerException">Inner exception</param>
        public TurboAssertionException(string message, Exception innerException) : base(message, innerException) { }

#if !NETSTANDARD1_X
        /// <summary>
        /// TurboAssertionException constructor for deserialization
        /// </summary>
        /// <param name="info">SerializationInfo</param>
        /// <param name="context">StreamingContext</param>
        protected TurboAssertionException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
#endif
    }
}
