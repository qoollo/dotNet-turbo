using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Queues.DiskQueueComponents
{
    class NonPersistentDiskQueueSegment<T> : CountingDiskQueueSegment<T>
    {
        private readonly string _fileName;
        private readonly FileStream _writeStream;
        private readonly FileStream _readStream;

        public NonPersistentDiskQueueSegment(string fileName, int capacity, long segmentNumber)
            : base(capacity, 0, 0, segmentNumber)
        {
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException(nameof(fileName));
            if (File.Exists(fileName))
                throw new ArgumentException($"Can't create NonPersistentDiskQueueSegment on existing file '{fileName}'", nameof(fileName));

            _fileName = fileName;
            _writeStream = new FileStream(_fileName, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
            _readStream = new FileStream(_fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
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
