using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Queues.DiskQueueComponents
{
    class NonPersistentDiskQueueSegment<T> : CountingDiskQueueSegment<T>
    {
        private const int DefaultWriteBufferSize = 16;

        private readonly string _fileName;

        private readonly object _writeLock;
        private readonly FileStream _writeStream;

        private readonly object _readLock;
        private readonly FileStream _readStream;

        private readonly ConcurrentQueue<T> _writeBuffer;
        private volatile int _writeBufferSize;
        private readonly int _maxWriteBufferSize;

        private volatile bool _isDisposed;

        public NonPersistentDiskQueueSegment(long segmentNumber, string fileName, int capacity, int writeBufferSize)
            : base(segmentNumber, capacity, 0, 0)
        {
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException(nameof(fileName));
            if (File.Exists(fileName))
                throw new ArgumentException($"Can't create NonPersistentDiskQueueSegment on existing file '{fileName}'", nameof(fileName));

            _fileName = fileName;
            _writeStream = new FileStream(_fileName, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
            _readStream = new FileStream(_fileName, FileMode.Open, FileAccess.Read, FileShare.Read);

            _writeLock = new object();
            _readLock = new object();

            _writeBufferSize = 0;
            _maxWriteBufferSize = writeBufferSize >= 0 ? writeBufferSize : DefaultWriteBufferSize;
            if (_maxWriteBufferSize > 0)
                _writeBuffer = new ConcurrentQueue<T>();


            _isDisposed = false;
        }

        public int WriteBufferSize { get { return _maxWriteBufferSize; } }

        private bool AddToWriteBuffer(T item)
        {
            Debug.Assert(_writeBuffer != null);
            Debug.Assert(_maxWriteBufferSize > 0);

            _writeBuffer.Enqueue(item);
            int writeBufferSize = Interlocked.Increment(ref _writeBufferSize);
            return (writeBufferSize % _maxWriteBufferSize) == 0;
        }

        private void DumpWriteBuffer()
        {
            Debug.Assert(_writeBuffer != null);
            Debug.Assert(_maxWriteBufferSize > 0);

            lock (_writeLock)
            {
                int itemCount = 0;
                T item = default(T);
                while (itemCount < _maxWriteBufferSize && _writeBuffer.TryDequeue(out item))
                {
                    // write item to mem stream
                }
            }
        }


        private void SerializeDataToMemoryStream(T item)
        {
        }


        protected override void AddCore(T item)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(this.GetType().Name);

            if (_writeBuffer != null)
            {
                if (!AddToWriteBuffer(item))
                    return;

                // Should dump write buffer

            }
            else
            {

            }



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
            if (!_isDisposed)
            {
                _isDisposed = true;

                if (disposeBehaviour == DiskQueueSegmentDisposeBehaviour.Delete)
                    Debug.Assert(this.Count == 0);


            }
        }
    }
}
