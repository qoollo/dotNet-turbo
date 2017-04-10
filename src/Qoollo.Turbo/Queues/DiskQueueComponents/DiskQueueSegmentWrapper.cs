using Qoollo.Turbo.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Queues.DiskQueueComponents
{
    internal sealed class DiskQueueSegmentWrapper<T> : IDisposable
    {
        private readonly DiskQueueSegment<T> _segment;
        private readonly EntryCountingEvent _entryCounter;

        public DiskQueueSegmentWrapper(DiskQueueSegment<T> segment)
        {
            if (segment == null)
                throw new ArgumentNullException(nameof(segment), "Created DiskQueueSegment cannot be null");

            _segment = segment;
            _entryCounter = new EntryCountingEvent();
        }

      
        public long Number { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _segment.Number; } }

        public int Count { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _segment.Count; } }
        public bool IsFull { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _segment.IsFull; } }
        public bool IsCompleted { [MethodImpl(MethodImplOptions.AggressiveInlining)]  get { return _segment.IsCompleted; } }

        public void AddForced(T item)
        {
            using (var guard = _entryCounter.TryEnterClientGuarded())
            {
                if (!guard.IsAcquired)
                    return;

                _segment.AddForced(item);
            }
        }
        public bool TryAdd(T item)
        {
            using (var guard = _entryCounter.TryEnterClientGuarded())
            {
                if (!guard.IsAcquired)
                    return false;

                return _segment.TryAdd(item);
            }
        }
        public bool TryTake(out T item)
        {
            using (var guard = _entryCounter.TryEnterClientGuarded())
            {
                if (!guard.IsAcquired)
                {
                    item = default(T);
                    return false;
                }

                return _segment.TryTake(out item);
            }
        }
        public bool TryPeek(out T item)
        {
            using (var guard = _entryCounter.TryEnterClientGuarded())
            {
                if (!guard.IsAcquired)
                {
                    item = default(T);
                    return false;
                }

                return _segment.TryPeek(out item);
            }
        }


        public void Dispose(DiskQueueSegmentDisposeBehaviour disposeBehaviour)
        {
            _entryCounter.TerminateAndWait();
            _segment.Dispose(disposeBehaviour);
        }

        public void Dispose()
        {
            Dispose(DiskQueueSegmentDisposeBehaviour.None);
        }
    }
}
