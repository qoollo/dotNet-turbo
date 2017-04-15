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
        private const int DefaultReadBufferSize = 32;
        private const int DefaultMaxCachedMemoryStreamSize = 512 * 1024;

        private readonly string _fileName;
        private readonly IDiskQueueItemSerializer<T> _serializer;

        private readonly object _writeLock;
        private readonly FileStream _writeStream;

        private readonly object _readLock;
        private readonly FileStream _readStream;

        private readonly ConcurrentQueue<T> _writeBuffer;
        private volatile int _writeBufferSize;
        private readonly int _maxWriteBufferSize;

        private volatile RegionBinaryWriter _cachedMemoryWriteStream;
        private readonly int _maxCachedMemoryWriteStreamSize;

        private readonly ConcurrentQueue<T> _readBuffer;
        private readonly int _maxReadBufferSize;

        private volatile bool _isDisposed;


        public NonPersistentDiskQueueSegment(long segmentNumber, string fileName, IDiskQueueItemSerializer<T> serializer, int capacity, 
            int writeBufferSize, int memoryWriteStreamSize, int readBufferSize)
            : base(segmentNumber, capacity, 0, 0)
        {
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException(nameof(fileName));
            if (serializer == null)
                throw new ArgumentNullException(nameof(serializer));
            if (File.Exists(fileName))
                throw new ArgumentException($"Can't create NonPersistentDiskQueueSegment on existing file '{fileName}'", nameof(fileName));

            _fileName = fileName;
            _serializer = serializer;

            _writeStream = new FileStream(_fileName, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
            _readStream = new FileStream(_fileName, FileMode.Open, FileAccess.Read, FileShare.Read);

            _writeLock = new object();
            _readLock = new object();

            _writeBufferSize = 0;
            _maxWriteBufferSize = writeBufferSize >= 0 ? writeBufferSize : DefaultWriteBufferSize;
            if (_maxWriteBufferSize > 0)
                _writeBuffer = new ConcurrentQueue<T>();

            _maxCachedMemoryWriteStreamSize = memoryWriteStreamSize >= 0 ? memoryWriteStreamSize : DefaultMaxCachedMemoryStreamSize;
            _cachedMemoryWriteStream = null;

            _maxReadBufferSize = readBufferSize >= 0 ? readBufferSize : DefaultReadBufferSize;
            if (_maxReadBufferSize > 0)
                _readBuffer = new ConcurrentQueue<T>();

            // Prepare file header
            WriteSegmentHeader(_writeStream, Number, Capacity);
            _readStream.Seek(0, SeekOrigin.End); // Offset readStream

            _isDisposed = false;
        }

        public int WriteBufferSize { get { return _maxWriteBufferSize; } }
        public int MemoryWriteStreamSize { get { return _maxCachedMemoryWriteStreamSize; } }
        public int ReadBufferSize { get { return _maxReadBufferSize; } }

        /// <summary>
        /// Writes segment header when file is created (should be called from constructor)
        /// </summary>
        private static void WriteSegmentHeader(FileStream stream, long number, int capacity)
        {
            BinaryWriter writer = new BinaryWriter(stream);

            // Write 4-byte segment identifier
            writer.Write((byte)'N');
            writer.Write((byte)'P');
            writer.Write((byte)'S');
            writer.Write((byte)1);

            // Write 8-byte segment number
            writer.Write((long)number);

            // Write 4-byte capacity
            writer.Write((int)capacity);

            writer.Flush();
        }


        /// <summary>
        /// Gets cached memory stream to write (if possible)
        /// </summary>
        /// <returns>BinaryWriter with stream for in-memory writing</returns>
        private RegionBinaryWriter GetMemoryWriteStream(int itemCount)
        {
            Debug.Assert(Monitor.IsEntered(_writeLock));
            Debug.Assert(itemCount > 0);

            RegionBinaryWriter result = _cachedMemoryWriteStream;
            _cachedMemoryWriteStream = null;
            if (result == null)
            {
                int expectedItemSize = _serializer.ExpectedSizeInBytes;
                if (expectedItemSize < 0)
                    result = new RegionBinaryWriter(itemCount * 8);
                else
                    result = new RegionBinaryWriter(Math.Max(itemCount * 8, Math.Min(itemCount * (expectedItemSize + 4), _maxCachedMemoryWriteStreamSize)));
            }
            else
            {
                result.BaseStream.SetOriginLength(0, -1);
                result.BaseStream.SetLength(0);
            }
            return result;
        }
        /// <summary>
        /// Release taken memory stream (set it back to <see cref="_cachedMemoryWriteStream"/>)
        /// </summary>
        /// <param name="bufferingWriteStream">Buffered stream to release</param>
        private void ReleaseMemoryWriteStream(RegionBinaryWriter bufferingWriteStream)
        {
            Debug.Assert(bufferingWriteStream != null);
            Debug.Assert(Monitor.IsEntered(_writeLock));

            if (bufferingWriteStream.BaseStream.InnerStream.Length <= _maxCachedMemoryWriteStreamSize)
            {
                if (bufferingWriteStream.BaseStream.InnerStream.Capacity > _maxCachedMemoryWriteStreamSize)
                {
                    bufferingWriteStream.BaseStream.SetOriginLength(0, -1);
                    bufferingWriteStream.BaseStream.InnerStream.SetLength(0);
                    bufferingWriteStream.BaseStream.InnerStream.Capacity = _maxCachedMemoryWriteStreamSize;
                }

                _cachedMemoryWriteStream = bufferingWriteStream;
            }
        }


        /// <summary>
        /// Writes items from <see cref="_writeBuffer"/> to disk
        /// </summary>
        private void SaveWriteBufferToDisk()
        {
            Debug.Assert(_writeBuffer != null);
            Debug.Assert(_maxWriteBufferSize > 0);

            lock (_writeLock)
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(this.GetType().Name);
                if (_writeBuffer.IsEmpty)
                    return;

                RegionBinaryWriter writer = GetMemoryWriteStream(_maxWriteBufferSize);

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

                ReleaseMemoryWriteStream(writer);
            }
        }

        /// <summary>
        /// Writes single item to disk
        /// </summary>
        /// <param name="item">Item</param>
        private void SaveSingleItemToDisk(T item)
        {
            lock (_writeLock)
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(this.GetType().Name);

                RegionBinaryWriter writer = GetMemoryWriteStream(1);

                // Write all data to disk
                writer.BaseStream.InnerStream.WriteTo(_writeStream);
                _writeStream.Flush(flushToDisk: false);

                ReleaseMemoryWriteStream(writer);
            }
        }

        /// <summary>
        /// Build segment record with specified item in memory stream.
        /// (writes length + item bytes)
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


        /// <summary>
        /// Adds new item to the tail of the segment (core implementation)
        /// </summary>
        /// <param name="item">New item</param>
        protected override void AddCore(T item)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(this.GetType().Name);

            if (_writeBuffer != null)
            {
                if (AddToWriteBuffer(item))
                {
                    // Should dump write buffer
                    SaveWriteBufferToDisk();
                }
            }
            else
            {
                SaveSingleItemToDisk(item);
            }
        }





        private bool TryPeekOrTakeThroughReadBuffer(out T item, bool take)
        {
            Debug.Assert(_readBuffer != null);

            lock (_readLock)
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(this.GetType().Name);

                // retry read from buffer
                if (take && _readBuffer.TryDequeue(out item))
                    return true;
                if (!take && _readBuffer.TryPeek(out item))
                    return true;

                // Buffer is empty => should read from disk


                item = default(T);
                return false;
            }
        }


        protected override bool TryTakeCore(out T item)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(this.GetType().Name);
           
            if (_readBuffer != null)
            {
                // Read from buffer first
                if (_readBuffer.TryDequeue(out item))
                    return true;
            }
            else
            {

            }


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
