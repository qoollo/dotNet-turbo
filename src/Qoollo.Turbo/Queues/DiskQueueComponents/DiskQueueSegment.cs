using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Queues.DiskQueueComponents
{
    public abstract class DiskQueueSegment<T>
    {
        public abstract int Number { get; }

        public abstract int BoundedCapacity { get; }
        public abstract int Count { get; }
        public abstract bool IsCompleted { get; }

        public abstract bool TryAdd(T item);
        public abstract bool TryTake(out T item, int timeout, CancellationToken token);
        public abstract bool TryPeek(out T item, int timeout, CancellationToken token);


        public abstract void Dispose();
    }
}
