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
        private const int DefaultWriteBufferSize = 32;
        private const int BufferingStreamLengthThreshold = 1024 * 1024;

        private readonly string _fileName;
        private readonly IDiskQueueItemSerializer<T> _serializer;

        private readonly object _writeLock;
        private readonly FileStream _writeStream;

        private readonly object _readLock;
        private readonly FileStream _readStream;

        private readonly ConcurrentQueue<T> _writeBuffer;
        private volatile int _writeBufferSize;
        private readonly int _maxWriteBufferSize;
        private volatile RegionBinaryWriter _bufferingWriteStream;

        private volatile bool _isDisposed;

        public NonPersistentDiskQueueSegment(long segmentNumber, string fileName, IDiskQueueItemSerializer<T> serializer, int capacity, int writeBufferSize)
            : base(segmentNumber, capacity, 0, 0)
        {
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException(nameof(fileName));
            if (serializer == null)
                throw new ArgumentNullException(nameof(serializer));
            if (File.Exists(fileName))
                throw new ArgumentException($"Can't create NonPersistentDiskQueueSegment on existing file '{fileName}'", nameof(fileName));

            _fileName = fileName;
            _writeStream = new FileStream(_fileName, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
            _readStream = new FileStream(_fileName, FileMode.Open, FileAccess.Read, FileShare.Read);

            _writeLock = new object();
            _readLock = new object();

            _writeBufferSize = 0;
            _maxWriteBufferSize = writeBufferSize >= 0 ? writeBufferSize : DefaultWriteBufferSize;
            _bufferingWriteStream = null;
            if (_maxWriteBufferSize > 0)
                _writeBuffer = new ConcurrentQueue<T>();


            _isDisposed = false;
        }

        public int WriteBufferSize { get { return _maxWriteBufferSize; } }

        /// <summary>
        /// Gets cached buffering stream to write (if possible)
        /// </summary>
        /// <returns>BinaryWriter with stream for in-memory writing</returns>
        private RegionBinaryWriter GetWriteBufferingStream(int itemCount)
        {
            Debug.Assert(Monitor.IsEntered(_writeLock));
            Debug.Assert(itemCount > 0);

            RegionBinaryWriter result = _bufferingWriteStream;
            _bufferingWriteStream = null;
            if (result == null)
            {
                int expectedItemSize = _serializer.ExpectedSizeInBytes;
                result = new RegionBinaryWriter(Math.Min(BufferingStreamLengthThreshold, expectedItemSize < 0 ? itemCount * (expectedItemSize + 4) : itemCount * 8));
            }
            else
            {
                result.BaseStream.SetOriginLength(0, -1);
                result.BaseStream.SetLength(0);
            }
            return result;
        }
        /// <summary>
        /// Release taken buffering stream (set it back to <see cref="_bufferingWriteStream"/>)
        /// </summary>
        /// <param name="bufferingWriteStream">Buffering stream to release</param>
        private void ReleaseWriteBufferingStream(RegionBinaryWriter bufferingWriteStream)
        {
            Debug.Assert(bufferingWriteStream != null);
            Debug.Assert(Monitor.IsEntered(_writeLock));

            if (bufferingWriteStream.BaseStream.InnerStream.Length < BufferingStreamLengthThreshold)
                _bufferingWriteStream = bufferingWriteStream;
        }


        /// <summary>
        /// Writes items from <see cref="_writeBuffer"/> to disk
        /// </summary>
        private void WriteBufferToDisk()
        {
            Debug.Assert(_writeBuffer != null);
            Debug.Assert(_maxWriteBufferSize > 0);

            lock (_writeLock)
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(this.GetType().Name);
                if (_writeBuffer.IsEmpty)
                    return;

                RegionBinaryWriter writer = GetWriteBufferingStream(_maxWriteBufferSize);

                int itemCount = 0;
                T item = default(T);
                while (itemCount < _maxWriteBufferSize && _writeBuffer.TryDequeue(out item))
                {
                    // write item to mem stream
                    SerializeItemToStream(item, writer);
                    itemCount++;
                }

                // Write all data to disk
                writer.BaseStream.InnerStream.WriteTo(_writeStream);
                _writeStream.Flush(flushToDisk: false);

                ReleaseWriteBufferingStream(writer);
            }
        }

        /// <summary>
        /// Writes single item to disk
        /// </summary>
        /// <param name="item">Item</param>
        private void WriteSingleItemToDisk(T item)
        {
            lock (_writeLock)
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(this.GetType().Name);

                RegionBinaryWriter writer = GetWriteBufferingStream(1);

                // Write all data to disk
                writer.BaseStream.InnerStream.WriteTo(_writeStream);
                _writeStream.Flush(flushToDisk: false);

                ReleaseWriteBufferingStream(writer);
            }
        }

        /// <summary>
        /// Serializes record with specified item to memory stream
        /// </summary>
        /// <param name="item">Item</param>
        /// <param name="writer">Writer that wraps memory stream</param>
        private void SerializeItemToStream(T item, RegionBinaryWriter writer)
        {
            Debug.Assert(writer != null);
            Debug.Assert(Monitor.IsEntered(_writeLock));

            checked
            {
                var stream = writer.BaseStream;

                int origin = stream.InnerStreamPosition;
                stream.SetOriginLength(origin + 4, -1); // Offset 4 to store length later
                Debug.Assert(stream.Length == 0);

                _serializer.Serialize(writer, item);
                Debug.Assert(stream.Length >= 0);

                int length = (int)stream.Length;
                stream.SetOrigin(origin); // Offset back to the beggining
                writer.Write((int)length); // Write length

                stream.Seek(0, SeekOrigin.End); // Seek to the end of the stream
            }
        }

        /// <summary>
        /// Adds item to writeBuffer
        /// </summary>
        /// <param name="item">Item</param>
        /// <returns>True when buffer should be dumped to disk</returns>
        private bool AddToWriteBuffer(T item)
        {
            Debug.Assert(_writeBuffer != null);
            Debug.Assert(_maxWriteBufferSize > 0);

            _writeBuffer.Enqueue(item);
            int writeBufferSize = Interlocked.Increment(ref _writeBufferSize);
            return (writeBufferSize % _maxWriteBufferSize) == 0;
        }

        protected override void AddCore(T item)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(this.GetType().Name);

            if (_writeBuffer != null)
            {
                if (AddToWriteBuffer(item))
                {
                    // Should dump write buffer
                    WriteBufferToDisk();
                }
            }
            else
            {
                WriteSingleItemToDisk(item);
            }
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
