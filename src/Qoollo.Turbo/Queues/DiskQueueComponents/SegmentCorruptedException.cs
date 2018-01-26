using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Queues.DiskQueueComponents
{
    /// <summary>
    /// Indicates that DiskQueueSegment is corrupted
    /// </summary>
    [Serializable]
    public class SegmentCorruptedException: TurboException
    {
        /// <summary>
        /// SegmentCorruptedException constructor
        /// </summary>
        public SegmentCorruptedException() : base("Segment is corrupted") { }
        /// <summary>
        /// SegmentCorruptedException constructor
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        public SegmentCorruptedException(string message) : base(message) { }
        /// <summary>
        /// SegmentCorruptedException constructor
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        /// <param name="innerException">Inner exception</param>
        public SegmentCorruptedException(string message, Exception innerException) : base(message, innerException) { }

#if HAS_SERIALIZABLE
        /// <summary>
        /// SegmentCorruptedException constructor
        /// </summary>
        /// <param name="info">SerializationInfo</param>
        /// <param name="context">StreamingContext</param>
        protected SegmentCorruptedException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
#endif
    }
}
