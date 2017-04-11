using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Queues.DiskQueueComponents
{
    /// <summary>
    /// Factory that creates DiskQueueSegments
    /// </summary>
    /// <typeparam name="T">Type of the item stored in DiskQueueSegment</typeparam>
    public abstract class DiskQueueSegmentFactory<T>
    {
        /// <summary>
        /// Capacity of a single segment (informational)
        /// </summary>
        public virtual int SegmentCapacity { get { return -1; } }
        /// <summary>
        /// Creates a new segment
        /// </summary>
        /// <param name="path">Path to the folder where the new segment will be allocated</param>
        /// <param name="number">Number of a segment (should be part of a segment name)</param>
        /// <returns>Created DiskQueueSegment</returns>
        public abstract DiskQueueSegment<T> CreateSegment(string path, long number);
        /// <summary>
        /// Discovers existing segments in specified path
        /// </summary>
        /// <param name="path">Path to the folder for the segments</param>
        /// <returns>Segments loaded from disk (can be empty)</returns>
        public abstract DiskQueueSegment<T>[] DiscoverSegments(string path);
    }
}
