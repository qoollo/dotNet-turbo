using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo
{
    /// <summary>
    /// The exception that is thrown when blocking operation is interrupted by the external event (not by CancallationToken)
    /// </summary>
    [Serializable]
    public class OperationInterruptedException : SystemException
    {
        /// <summary>
        /// OperationInterruptedException constructor
        /// </summary>
        public OperationInterruptedException() : base("Opearion was interrupted by some external event") { }
        /// <summary>
        /// OperationInterruptedException constructor with error message
        /// </summary>
        /// <param name="message">Error message</param>
        public OperationInterruptedException(string message) : base(message) { }
        /// <summary>
        /// OperationInterruptedException constructor with error message and innerException
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="innerException">Inner exception</param>
        public OperationInterruptedException(string message, Exception innerException) : base(message, innerException) { }
#if HAS_SERIALIZABLE
        /// <summary>
        /// OperationInterruptedException constructor for deserialization
        /// </summary>
        /// <param name="info">SerializationInfo</param>
        /// <param name="context">StreamingContext</param>
        protected OperationInterruptedException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
#endif
    }
}
