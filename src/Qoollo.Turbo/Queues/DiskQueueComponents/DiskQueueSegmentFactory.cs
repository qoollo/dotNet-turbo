using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Queues.DiskQueueComponents
{
    public abstract class DiskQueueSegmentFactory<T>
    {
        public abstract DiskQueueSegment<T> CreateSegment(string path, int number, int capacity);
        public abstract DiskQueueSegment<T>[] DiscoverSegments(string path);
    }
}
