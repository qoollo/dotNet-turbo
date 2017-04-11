using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Queues.DiskQueueComponents
{
    class NonPersistentDiskQueueSegment<T> : CountingDiskQueueSegment<T>
    {
        public NonPersistentDiskQueueSegment(int capacity, long segmentNumber)
            : base(capacity, 0, 0, segmentNumber)
        {

        }


        protected override void AddCore(T item)
        {
            throw new NotImplementedException();
        }

        protected override bool TryTakeCore(out T item)
        {
            throw new NotImplementedException();
        }

        protected override bool TryPeekCore(out T item)
        {
            throw new NotImplementedException();
        }


        protected override void Dispose(DiskQueueSegmentDisposeBehaviour disposeBehaviour, bool isUserCall)
        {
            throw new NotImplementedException();
        }
    }
}
