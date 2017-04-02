using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Queues.DiskQueueComponents
{
    /// <summary>
    /// Base class for the segment of the DiskQueue
    /// </summary>
    /// <typeparam name="T">The type of elements in segment</typeparam>
    public abstract class DiskQueueSegment<T>: IDisposable
    {
        public DiskQueueSegment(long number)
        {
            if (number < 0)
                throw new ArgumentOutOfRangeException(nameof(number));

            Number = number;
        }

        /// <summary>
        /// Unique number of the segment that represents its position in the queue
        /// </summary>
        public long Number { get; private set; }


        public abstract int Count { get; }
        public abstract bool IsFull { get; }
        public abstract bool IsCompleted { get; }

        public abstract bool TryAdd(T item);
        public abstract bool TryTake(out T item, int timeout, CancellationToken token);
        public abstract bool TryPeek(out T item, int timeout, CancellationToken token);


        protected virtual void Dispose(bool isUserCall)
        {
        }
        /// <summary>
        /// Cleans-up resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
