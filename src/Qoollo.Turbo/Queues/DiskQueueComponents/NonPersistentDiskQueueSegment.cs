﻿using System;
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
    /// <summary>
    /// Factory to create and discover NonPersistentDiskQueueSegment
    /// </summary>
    /// <typeparam name="T">Type of items stored in segment</typeparam>
    public class NonPersistentDiskQueueSegmentFactory<T> : DiskQueueSegmentFactory<T>
    {
        /// <summary>
        /// Segment file extension
        /// </summary>
        public const string SegmentFileExtension = ".npdqs";

        private readonly int _capacity;
        private readonly string _fileNamePrefix;
        private readonly IDiskQueueItemSerializer<T> _serializer;
        private readonly int _writeBufferSize;
        private readonly int _cachedMemoryWriteStreamSize;
        private readonly int _readBufferSize;
        private readonly int _cachedMemoryReadStreamSize;

        /// <summary>
        /// NonPersistentDiskQueueSegmentFactory constructor
        /// </summary>
        /// <param name="capacity">Maximum number of stored items inside the segement (overall capacity)</param>
        /// <param name="fileNamePrefix">Prefix for the segment file name</param>
        /// <param name="serializer">Items serializing/deserializing logic</param>
        /// <param name="writeBufferSize">Determines the number of items, that are stored in memory before save them to disk (-1 - set to default value, 0 - disable write buffer)</param>
        /// <param name="cachedMemoryWriteStreamSize">Maximum size of the cached byte stream that used to serialize items in memory (-1 - set to default value, 0 - disable byte stream caching)</param>
        /// <param name="readBufferSize">Determines the number of items, that are stored in memory for read purposes (-1 - set to default value, 0 - disable read buffer)</param>
        /// <param name="cachedMemoryReadStreamSize">Maximum size of the cached byte stream that used to deserialize items in memory (-1 - set to default value, 0 - disable byte stream caching)</param>
        public NonPersistentDiskQueueSegmentFactory(int capacity, string fileNamePrefix, IDiskQueueItemSerializer<T> serializer,
            int writeBufferSize, int cachedMemoryWriteStreamSize, int readBufferSize, int cachedMemoryReadStreamSize)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity should be positive");
            if (string.IsNullOrEmpty(fileNamePrefix))
                throw new ArgumentNullException(nameof(fileNamePrefix));
            if (serializer == null)
                throw new ArgumentNullException(nameof(serializer));

            _capacity = capacity;
            _fileNamePrefix = fileNamePrefix;
            _serializer = serializer;

            _writeBufferSize = writeBufferSize;
            _cachedMemoryWriteStreamSize = cachedMemoryWriteStreamSize;
            _readBufferSize = readBufferSize;
            _cachedMemoryReadStreamSize = cachedMemoryReadStreamSize;
        }
        /// <summary>
        /// NonPersistentDiskQueueSegmentFactory constructor
        /// </summary>
        /// <param name="capacity">Maximum number of stored items inside the segement (overall capacity)</param>
        /// <param name="fileNamePrefix">Prefix for the segment file name</param>
        /// <param name="serializer">Items serializing/deserializing logic</param>
        /// <param name="writeBufferSize">Determines the number of items, that are stored in memory before save them to disk (-1 - set to default value, 0 - disable write buffer)</param>
        /// <param name="readBufferSize">Determines the number of items, that are stored in memory for read purposes (-1 - set to default value, 0 - disable read buffer)</param>
        public NonPersistentDiskQueueSegmentFactory(int capacity, string fileNamePrefix, IDiskQueueItemSerializer<T> serializer,
            int writeBufferSize, int readBufferSize)
            :this (capacity, fileNamePrefix, serializer, writeBufferSize, -1, readBufferSize, -1)
        {
        }
        /// <summary>
        /// NonPersistentDiskQueueSegmentFactory constructor
        /// </summary>
        /// <param name="capacity">Maximum number of stored items inside the segement (overall capacity)</param>
        /// <param name="fileNamePrefix">Prefix for the segment file name</param>
        /// <param name="serializer">Items serializing/deserializing logic</param>
        public NonPersistentDiskQueueSegmentFactory(int capacity, string fileNamePrefix, IDiskQueueItemSerializer<T> serializer)
            : this(capacity, fileNamePrefix, serializer, -1, -1, -1, -1)
        {
        }

        /// <summary>
        /// Capacity of a single segment
        /// </summary>
        public override int SegmentCapacity { get { return _capacity; } }

        /// <summary>
        /// Creates a new segment
        /// </summary>
        /// <param name="path">Path to the folder where the new segment will be allocated</param>
        /// <param name="number">Number of a segment (should be part of a segment name)</param>
        /// <returns>Created NonPersistentDiskQueueSegment</returns>
        public override DiskQueueSegment<T> CreateSegment(string path, long number)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            string fileName = Path.Combine(path, GenerateFileName(_fileNamePrefix, number, SegmentFileExtension));
            return new NonPersistentDiskQueueSegment<T>(number, fileName, _serializer, _capacity,
                _writeBufferSize, _cachedMemoryWriteStreamSize, _readBufferSize, _cachedMemoryReadStreamSize);
        }

        /// <summary>
        /// Discovers existing segments in specified path
        /// </summary>
        /// <param name="path">Path to the folder for the segments</param>
        /// <returns>Segments loaded from disk (can be empty)</returns>
        public override DiskQueueSegment<T>[] DiscoverSegments(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            var files = DiscoverSegmentFiles(path, _fileNamePrefix, SegmentFileExtension);
            // Should delete all existing segments
            foreach (var file in files)
                File.Delete(file.FileName);

            return new DiskQueueSegment<T>[0];
        }
    }



    /// <summary>
    /// Non persistent disk queue segment (not preserve items between restarts)
    /// </summary>
    /// <typeparam name="T">The type of elements in segment</typeparam>
    public class NonPersistentDiskQueueSegment<T> : CountingDiskQueueSegment<T>
    {
        /// <summary>
        /// Creates new instance of <see cref="NonPersistentDiskQueueSegmentFactory{T}"/>
        /// </summary>
        /// <param name="capacity">Maximum number of stored items inside the segement (overall capacity)</param>
        /// <param name="fileNamePrefix">Prefix for the segment file name</param>
        /// <param name="serializer">Items serializing/deserializing logic</param>
        /// <returns>Created <see cref="NonPersistentDiskQueueSegmentFactory{T}"/></returns>
        public static NonPersistentDiskQueueSegmentFactory<T> CreateFactory(int capacity, string fileNamePrefix, IDiskQueueItemSerializer<T> serializer)
        {
            return new NonPersistentDiskQueueSegmentFactory<T>(capacity, fileNamePrefix, serializer);
        }


        // ========================

        private const int DefaultWriteBufferSize = 32;
        private const int DefaultReadBufferSize = 32;

        private const int ItemHeaderSize = 4;
        private const int InitialCacheSizePerItem = 4;
        private const int MaxChacheSizePerItem = 1024;
        private const int MaxCachedMemoryStreamSize = 32 * 1024;
        private const int WriteBufferCountOvehead = 16;

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

        private volatile RegionBinaryReader _cachedMemoryReadStream;
        private readonly int _maxCachedMemoryReadStreamSize;

        private volatile bool _isDisposed;


        /// <summary>
        /// NonPersistentDiskQueueSegment constructor
        /// </summary>
        /// <param name="segmentNumber">Segment number</param>
        /// <param name="fileName">Full file name for the segment</param>
        /// <param name="serializer">Items serializing/deserializing logic</param>
        /// <param name="capacity">Maximum number of stored items inside the segement (overall capacity)</param>
        /// <param name="writeBufferSize">Determines the number of items, that are stored in memory before save them to disk (-1 - set to default value, 0 - disable write buffer)</param>
        /// <param name="cachedMemoryWriteStreamSize">Maximum size of the cached byte stream that used to serialize items in memory (-1 - set to default value, 0 - disable byte stream caching)</param>
        /// <param name="readBufferSize">Determines the number of items, that are stored in memory for read purposes (-1 - set to default value, 0 - disable read buffer)</param>
        /// <param name="cachedMemoryReadStreamSize">Maximum size of the cached byte stream that used to deserialize items in memory (-1 - set to default value, 0 - disable byte stream caching)</param>
        public NonPersistentDiskQueueSegment(long segmentNumber, string fileName, IDiskQueueItemSerializer<T> serializer, int capacity, 
            int writeBufferSize, int cachedMemoryWriteStreamSize, int readBufferSize, int cachedMemoryReadStreamSize)
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

            try
            {
                _writeStream = new FileStream(_fileName, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
                _readStream = new FileStream(_fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                _writeLock = new object();
                _readLock = new object();

                _writeBufferSize = 0;
                _maxWriteBufferSize = writeBufferSize >= 0 ? writeBufferSize : DefaultWriteBufferSize;
                if (writeBufferSize < 0 && serializer.ExpectedSizeInBytes > 0)
                    _maxWriteBufferSize = checked(Math.Max(DefaultWriteBufferSize, 4096 / Math.Min(serializer.ExpectedSizeInBytes + ItemHeaderSize, MaxChacheSizePerItem)));

                if (_maxWriteBufferSize > 0)
                    _writeBuffer = new ConcurrentQueue<T>();

                _cachedMemoryWriteStream = null;
                _maxCachedMemoryWriteStreamSize = cachedMemoryWriteStreamSize;
                if (cachedMemoryWriteStreamSize < 0)
                {
                    checked
                    {
                        int expectedItemSize = MaxChacheSizePerItem;
                        if (serializer.ExpectedSizeInBytes > 0)
                            expectedItemSize = Math.Min(serializer.ExpectedSizeInBytes + ItemHeaderSize, MaxChacheSizePerItem);
                        _maxCachedMemoryWriteStreamSize = Math.Min(MaxCachedMemoryStreamSize, (_maxWriteBufferSize + 1) * expectedItemSize);
                    }
                }


                _maxReadBufferSize = readBufferSize >= 0 ? readBufferSize : Math.Max(_writeBufferSize / 2, DefaultReadBufferSize);
                if (_maxReadBufferSize > 0)
                    _readBuffer = new ConcurrentQueue<T>();

                _cachedMemoryReadStream = null;
                _maxCachedMemoryReadStreamSize = cachedMemoryReadStreamSize;
                if (cachedMemoryReadStreamSize < 0)
                {
                    checked
                    {
                        int expectedItemSize = MaxChacheSizePerItem;
                        if (serializer.ExpectedSizeInBytes > 0)
                            expectedItemSize = Math.Min(serializer.ExpectedSizeInBytes + ItemHeaderSize, MaxChacheSizePerItem);
                        _maxCachedMemoryReadStreamSize = Math.Min(MaxCachedMemoryStreamSize, expectedItemSize);
                    }
                }


                // Prepare file header
                WriteSegmentHeader(_writeStream, Number, Capacity);
                _readStream.Seek(0, SeekOrigin.End); // Offset readStream after the header

                _isDisposed = false;

                TurboContract.Assert(_maxWriteBufferSize >= 0, conditionString: "_maxWriteBufferSize >= 0");
                TurboContract.Assert(_maxCachedMemoryWriteStreamSize >= 0, conditionString: "_maxCachedMemoryWriteStreamSize >= 0");
                TurboContract.Assert(_maxReadBufferSize >= 0, conditionString: "_maxReadBufferSize >= 0");
                TurboContract.Assert(_maxCachedMemoryReadStreamSize >= 0, conditionString: "_maxCachedMemoryReadStreamSize >= 0");
                TurboContract.Assert(_writeStream.Position == _readStream.Position, conditionString: "_writeStream.Position == _readStream.Position");
            }
            catch
            {
                // Should close streams to make created files available to delete
                if (_writeStream != null)
                    _writeStream.Dispose();
                if (_readStream != null)
                    _readStream.Dispose();

                throw;
            }
        }
        /// <summary>
        /// NonPersistentDiskQueueSegment constructor
        /// </summary>
        /// <param name="segmentNumber">Segment number</param>
        /// <param name="fileName">Full file name for the segment</param>
        /// <param name="serializer">Items serializing/deserializing logic</param>
        /// <param name="capacity">Maximum number of stored items inside the segement (overall capacity)</param>
        /// <param name="writeBufferSize">Determines the number of items, that are stored in memory before save them to disk (-1 - set to default value, 0 - disable write buffer)</param>
        /// <param name="readBufferSize">Determines the number of items, that are stored in memory for read purposes (-1 - set to default value, 0 - disable read buffer)</param>
        public NonPersistentDiskQueueSegment(long segmentNumber, string fileName, IDiskQueueItemSerializer<T> serializer, int capacity,
            int writeBufferSize, int readBufferSize)
            : this(segmentNumber, fileName, serializer, capacity, writeBufferSize, -1, readBufferSize, -1)
        {
        }
        /// <summary>
        /// NonPersistentDiskQueueSegment constructor
        /// </summary>
        /// <param name="segmentNumber">Segment number</param>
        /// <param name="fileName">Full file name for the segment</param>
        /// <param name="serializer">Items serializing/deserializing logic</param>
        /// <param name="capacity">Maximum number of stored items inside the segement (overall capacity)</param>
        public NonPersistentDiskQueueSegment(long segmentNumber, string fileName, IDiskQueueItemSerializer<T> serializer, int capacity)
            : this(segmentNumber, fileName, serializer, capacity, -1, -1, -1, -1)
        {
        }


        /// <summary>
        /// Size of the write buffer (determines the number of items, that are stored in memory before save them to disk)
        /// </summary>
        public int WriteBufferSize { get { return _maxWriteBufferSize; } }
        /// <summary>
        /// Maximum size of the cached byte stream that used to serialize items in memory
        /// </summary>
        public int CachedMemoryWriteStreamSize { get { return _maxCachedMemoryWriteStreamSize; } }
        /// <summary>
        /// Size of the read buffer (determines the number of items, that are stored in memory for read purposes)
        /// </summary>
        public int ReadBufferSize { get { return _maxReadBufferSize; } }
        /// <summary>
        /// Maximum size of the cached byte stream that used to deserialize items in memory
        /// </summary>
        public int CachedMemoryReadStreamSize { get { return _maxCachedMemoryReadStreamSize; } }
        /// <summary>
        /// Full file name for the segment
        /// </summary>
        public string FileName { get { return _fileName; } }

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


        // ================= MemoryWriteStream ==================

        /// <summary>
        /// Gets cached memory stream to write
        /// </summary>
        /// <returns>BinaryWriter with stream for in-memory writing</returns>
        private RegionBinaryWriter GetMemoryWriteStream(int itemCount)
        {
            TurboContract.Requires(itemCount > 0, conditionString: "itemCount > 0");
            TurboContract.Assert(Monitor.IsEntered(_writeLock), conditionString: "Monitor.IsEntered(_writeLock)");

            RegionBinaryWriter result = _cachedMemoryWriteStream;
            _cachedMemoryWriteStream = null;
            if (result == null)
            {
                int expectedItemSize = _serializer.ExpectedSizeInBytes;
                if (expectedItemSize <= 0)
                    expectedItemSize = InitialCacheSizePerItem;
                result = new RegionBinaryWriter(Math.Min(MaxCachedMemoryStreamSize, Math.Max(0, itemCount * (expectedItemSize + ItemHeaderSize)))); // Max used to protect from overflow
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
            TurboContract.Requires(bufferingWriteStream != null, conditionString: "bufferingWriteStream != null");
            TurboContract.Assert(Monitor.IsEntered(_writeLock), conditionString: "Monitor.IsEntered(_writeLock)");
            TurboContract.Assert(_cachedMemoryWriteStream == null, conditionString: "_cachedMemoryWriteStream == null");

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


        // ============= MemoryReadStream =================

        /// <summary>
        /// Gets cached memory stream to read
        /// </summary>
        /// <returns>BinaryReader with stream for in-memory reading</returns>
        private RegionBinaryReader GetMemoryReadStream(int itemCount = 1)
        {
            TurboContract.Requires(itemCount > 0, conditionString: "itemCount > 0");
            TurboContract.Assert(Monitor.IsEntered(_readLock), conditionString: "Monitor.IsEntered(_readLock)");

            RegionBinaryReader result = _cachedMemoryReadStream;
            _cachedMemoryReadStream = null;
            if (result == null)
            {
                int expectedItemSize = _serializer.ExpectedSizeInBytes;
                if (expectedItemSize <= 0)
                    expectedItemSize = InitialCacheSizePerItem;
                result = new RegionBinaryReader(Math.Min(MaxCachedMemoryStreamSize, Math.Max(0, itemCount * (expectedItemSize + ItemHeaderSize)))); // Max used to protect from overflow
            }
            else
            {
                result.BaseStream.SetOriginLength(0, -1);
                result.BaseStream.SetLength(0);
            }
            return result;
        }
        /// <summary>
        /// Release taken memory stream (set it back to <see cref="_cachedMemoryReadStream"/>)
        /// </summary>
        /// <param name="bufferingReadStream">Buffered stream to release</param>
        private void ReleaseMemoryReadStream(RegionBinaryReader bufferingReadStream)
        {
            TurboContract.Requires(bufferingReadStream != null, conditionString: "bufferingReadStream != null");
            TurboContract.Assert(Monitor.IsEntered(_readLock), conditionString: "Monitor.IsEntered(_readLock)");
            TurboContract.Assert(_cachedMemoryReadStream == null, conditionString: "_cachedMemoryReadStream == null");

            if (bufferingReadStream.BaseStream.InnerStream.Length <= _maxCachedMemoryReadStreamSize)
            {
                if (bufferingReadStream.BaseStream.InnerStream.Capacity > _maxCachedMemoryReadStreamSize)
                {
                    bufferingReadStream.BaseStream.SetOriginLength(0, -1);
                    bufferingReadStream.BaseStream.InnerStream.SetLength(0);
                    bufferingReadStream.BaseStream.InnerStream.Capacity = _maxCachedMemoryReadStreamSize;
                }

                _cachedMemoryReadStream = bufferingReadStream;
            }
        }


        // =================================


        /// <summary>
        /// Writes items from <see cref="_writeBuffer"/> to disk
        /// </summary>
        private void SaveWriteBufferToDisk()
        {
            TurboContract.Assert(_writeBuffer != null, conditionString: "_writeBuffer != null");
            TurboContract.Assert(_maxWriteBufferSize > 0, conditionString: "_maxWriteBufferSize > 0");

            lock (_writeLock)
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(this.GetType().Name);

                // Nothing to do check
                if (_writeBuffer.IsEmpty || _writeBufferSize < _maxWriteBufferSize)
                    return;

                RegionBinaryWriter writer = GetMemoryWriteStream(_maxWriteBufferSize);
                TurboContract.Assert(writer.BaseStream.Length == 0, conditionString: "writer.BaseStream.Length == 0");
                TurboContract.Assert(writer.BaseStream.InnerStream.Length == 0, conditionString: "writer.BaseStream.InnerStream.Length == 0");

                int itemCount = 0;
                T item = default(T);
                while (itemCount < _maxWriteBufferSize && _writeBuffer.TryDequeue(out item))
                {
                    // write item to mem stream
                    SerializeItemToStream(item, writer);
                    itemCount++;
                }

                if (itemCount > 0)
                {
                    // Reduce counter safely
                    int curWriteBufferSize = Interlocked.Add(ref _writeBufferSize, -itemCount);
                    TurboContract.Assert(curWriteBufferSize >= 0, conditionString: "curWriteBufferSize >= 0");
                    TurboContract.Assert(curWriteBufferSize <= _writeBuffer.Count, conditionString: "curWriteBufferSize <= _writeBuffer.Count");

                    // Write all data to disk
                    writer.BaseStream.InnerStream.WriteTo(_writeStream);
                    _writeStream.Flush(flushToDisk: false);
                }

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
                TurboContract.Assert(writer.BaseStream.Length == 0, conditionString: "writer.BaseStream.Length == 0");
                TurboContract.Assert(writer.BaseStream.InnerStream.Length == 0, conditionString: "writer.BaseStream.InnerStream.Length == 0");

                SerializeItemToStream(item, writer);

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
            TurboContract.Requires(writer != null, conditionString: "writer != null");
            TurboContract.Assert(Monitor.IsEntered(_writeLock), conditionString: "Monitor.IsEntered(_writeLock)");

            checked
            {
                var stream = writer.BaseStream;

                int origin = stream.InnerStreamPosition;
                stream.SetOriginLength(origin + 4, -1); // Offset 4 to store length later
                TurboContract.Assert(stream.Length == 0, conditionString: "stream.Length == 0");

                _serializer.Serialize(writer, item);
                writer.Flush();
                TurboContract.Assert(stream.Length >= 0, conditionString: "stream.Length >= 0");

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
            TurboContract.Assert(_writeBuffer != null, conditionString: "_writeBuffer != null");
            TurboContract.Assert(_maxWriteBufferSize > 0, conditionString: "_maxWriteBufferSize > 0");

            _writeBuffer.Enqueue(item);
            int writeBufferSize = Interlocked.Increment(ref _writeBufferSize);
            // Start dumping when 'writeBufferSize == _maxWriteBufferSize' and force waiting when 'writeBufferSize >= _maxWriteBufferSize + WriteBufferCountOvehead'
            return (writeBufferSize % _maxWriteBufferSize) == 0 || writeBufferSize >= _maxWriteBufferSize + WriteBufferCountOvehead; 
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


        // =============================

        
        /// <summary>
        /// Read single item bytes from disk to <paramref name="targetStream"/> (incuding header)
        /// </summary>
        /// <param name="targetStream">Target stream to read bytes (Position is not changed after read)</param>
        /// <param name="itemSize">Size of the readed item (without header)</param>
        /// <param name="take">Should move readStream position</param>
        /// <returns>Is read</returns>
        private bool TryTakeOrPeekSingleItemBytesFromDisk(MemoryStream targetStream, out int itemSize, bool take)
        {
            TurboContract.Requires(targetStream != null, conditionString: "targetStream != null");
            TurboContract.Assert(Monitor.IsEntered(_readLock), conditionString: "Monitor.IsEntered(_readLock)");

            itemSize = 0;

            long readStreamPosition = _readStream.Position;

            // Ensure capacity on MemoryStream
            targetStream.SetLength(targetStream.Position + ItemHeaderSize);

            // Read item header
            var rawBuffer = targetStream.GetBuffer();
            int readCount = _readStream.Read(rawBuffer, (int)targetStream.Position, ItemHeaderSize);
            if (readCount != ItemHeaderSize) // No space for item header
            {
                if (readCount != 0)
                    _readStream.Seek(-readCount, SeekOrigin.Current); // Rewind back the position
                TurboContract.Assert(_readStream.Position == readStreamPosition, conditionString: "_readStream.Position == readStreamPosition");
                return false;
            }

            itemSize = BitConverter.ToInt32(rawBuffer, (int)targetStream.Position);

            // Ensure capacity on MemoryStream
            targetStream.SetLength(targetStream.Position + ItemHeaderSize + itemSize);

            // Read item
            rawBuffer = targetStream.GetBuffer();
            readCount = _readStream.Read(rawBuffer, (int)targetStream.Position + ItemHeaderSize, itemSize);
            if (readCount != itemSize)
            {
                _readStream.Seek(-ItemHeaderSize - readCount, SeekOrigin.Current); // Rewind back the position
                TurboContract.Assert(_readStream.Position == readStreamPosition, conditionString: "_readStream.Position == readStreamPosition");
                return false;
            }

            if (!take)
            {
                // For peek should rewind stream position
                _readStream.Seek(-ItemHeaderSize - itemSize, SeekOrigin.Current);
            }

            TurboContract.Assert(targetStream.Position + ItemHeaderSize + itemSize == targetStream.Length, conditionString: "targetStream.Position + ItemHeaderSize + itemSize == targetStream.Length");
            TurboContract.Assert(take || _readStream.Position == readStreamPosition, conditionString: "take || _readStream.Position == readStreamPosition");
            return true;
        }

        /// <summary>
        /// Read signle item from disk and deserialize it
        /// </summary>
        /// <param name="item">Taken item</param>
        /// <param name="buffer">Buffer</param>
        /// <param name="take">True = take, False = peek</param>
        /// <returns>Success or not</returns>
        private bool TryTakeOrPeekItemFromDisk(out T item, RegionBinaryReader buffer, bool take)
        {
            TurboContract.Requires(buffer != null, conditionString: "buffer != null");
            TurboContract.Assert(Monitor.IsEntered(_readLock), conditionString: "Monitor.IsEntered(_readLock)");

            buffer.BaseStream.SetOriginLength(0, -1);

            int itemSize = 0;
            if (!TryTakeOrPeekSingleItemBytesFromDisk(buffer.BaseStream.InnerStream, out itemSize, take)) // Read from disk
            {
                item = default(T);
                return false;
            }

            buffer.BaseStream.SetOriginLength(ItemHeaderSize, itemSize);
            TurboContract.Assert(buffer.BaseStream.Length == itemSize, conditionString: "buffer.BaseStream.Length == itemSize");

            item = _serializer.Deserialize(buffer); // Deserialize
            return true;
        }


        /// <summary>
        /// Helper method to take or peek item from _writeBuffer
        /// </summary>
        private bool TryTakeOrPeekFromWriteBuffer(out T item, bool take)
        {
            TurboContract.Assert(_writeBuffer != null, conditionString: "_writeBuffer != null");
            TurboContract.Assert(Monitor.IsEntered(_writeLock), conditionString: "Monitor.IsEntered(_writeLock)");
            if (take)
            {
                if (_writeBuffer.TryDequeue(out item))
                {
                    Interlocked.Decrement(ref _writeBufferSize);
                    return true;
                }
                return false;
            }
            else
            {
                return _writeBuffer.TryPeek(out item);
            }
        }
        /// <summary>
        /// Helper method to take or peek item from _readBuffer
        /// </summary>
        private bool TryTakeOrPeekFromReadBuffer(out T item, bool take)
        {
            TurboContract.Assert(_readBuffer != null, conditionString: "_readBuffer != null");
            if (take)
                return _readBuffer.TryDequeue(out item);
            return _readBuffer.TryPeek(out item);
        }

        /// <summary>
        /// Take or peek item inside readLock through read buffer. Also populate readBuffer with items
        /// Steps:
        /// - Reads item from readBuffer;
        /// - Reads items from disk
        /// - Reads items from disk in exclusive mode (lock on _writeLock)
        /// - Reads items from writeBuffer (with lock on _writeLock)
        /// </summary>
        private bool TryTakeOrPeekThroughReadBuffer(out T item, bool take)
        {
            TurboContract.Assert(_readBuffer != null, conditionString: "_readBuffer != null");
            TurboContract.Assert(_maxReadBufferSize > 0, conditionString: "_maxReadBufferSize > 0");

            lock (_readLock)
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(this.GetType().Name);

                // retry read from buffer
                if (TryTakeOrPeekFromReadBuffer(out item, take))
                    return true;

                // Read buffer is empty => should read from disk
                RegionBinaryReader memoryBuffer = GetMemoryReadStream();
                TurboContract.Assert(memoryBuffer.BaseStream.Length == 0, conditionString: "memoryBuffer.BaseStream.Length == 0");
                TurboContract.Assert(memoryBuffer.BaseStream.InnerStream.Length == 0, conditionString: "memoryBuffer.BaseStream.InnerStream.Length == 0");

                try
                {
                    int itemTransfered = 0;
                    T tmpItem = default(T);
                    while (itemTransfered < _maxReadBufferSize && TryTakeOrPeekItemFromDisk(out tmpItem, memoryBuffer, take: true)) // take = true as we transfer items to buffer
                    {
                        if (itemTransfered == 0)
                            item = tmpItem;
                        
                        if (itemTransfered > 0 || !take) // First item should always be ours
                            _readBuffer.Enqueue(tmpItem);

                        itemTransfered++;
                    }

                    if (itemTransfered < _maxReadBufferSize)
                    {
                        // Should enter write lock to observe fully saved items
                        lock (_writeLock)
                        {
                            // Retry read from disk
                            while (itemTransfered < _maxReadBufferSize && TryTakeOrPeekItemFromDisk(out tmpItem, memoryBuffer, take: true)) // take = true as we transfer items to buffer
                            {
                                if (itemTransfered == 0)
                                    item = tmpItem;

                                if (itemTransfered > 0 || !take) // First item should always be ours
                                    _readBuffer.Enqueue(tmpItem);

                                itemTransfered++;
                            }

                            // attempt to read from write buffer
                            if (itemTransfered < _maxReadBufferSize && _writeBuffer != null)
                            {
                                while (itemTransfered < _maxReadBufferSize && TryTakeOrPeekFromWriteBuffer(out tmpItem, take: true)) // take = true as we transfer items to buffer
                                {
                                    if (itemTransfered == 0)
                                        item = tmpItem;

                                    if (itemTransfered > 0 || !take) // First item should always be ours
                                        _readBuffer.Enqueue(tmpItem);

                                    itemTransfered++;
                                }
                            }
                        }
                    }

                    return itemTransfered > 0;
                }
                finally
                {
                    ReleaseMemoryReadStream(memoryBuffer);
                }
            }
        }


        /// <summary>
        /// Take or peek item inside readLock
        /// Steps:
        /// - Reads item from disk
        /// - Reads item from disk in exclusive mode (lock on _writeLock)
        /// - Reads item from writeBuffer (with lock on _writeLock)
        /// </summary>
        private bool TryTakeOrPeek(out T item, bool take)
        {
            lock (_readLock)
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(this.GetType().Name);

                RegionBinaryReader memoryBuffer = GetMemoryReadStream();
                TurboContract.Assert(memoryBuffer.BaseStream.Length == 0, conditionString: "memoryBuffer.BaseStream.Length == 0");
                TurboContract.Assert(memoryBuffer.BaseStream.InnerStream.Length == 0, conditionString: "memoryBuffer.BaseStream.InnerStream.Length == 0");

                try
                {
                    if (TryTakeOrPeekItemFromDisk(out item, memoryBuffer, take))
                        return true;

                    // Should enter write lock to observe fully saved items
                    lock (_writeLock)
                    {
                        // Retry read from disk
                        if (TryTakeOrPeekItemFromDisk(out item, memoryBuffer, take))
                            return true;

                        // Now attempt to read from write buffer
                        if (_writeBuffer != null && TryTakeOrPeekFromWriteBuffer(out item, take))
                            return true;
                    }
                }
                finally
                {
                    ReleaseMemoryReadStream(memoryBuffer);
                }

                return false;
            }
        }



        /// <summary>
        /// Removes item from the head of the segment (core implementation)
        /// </summary>
        /// <param name="item">The item removed from segment</param>
        /// <returns>True if the item was removed</returns>
        protected override bool TryTakeCore(out T item)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(this.GetType().Name);
           
            if (_readBuffer != null)
            {
                // Read from buffer first
                if (_readBuffer.TryDequeue(out item))
                    return true;

                return TryTakeOrPeekThroughReadBuffer(out item, take: true);
            }
            else
            {
                return TryTakeOrPeek(out item, take: true);
            }
        }
        /// <summary>
        /// Returns the item at the head of the segment without removing it (core implementation)
        /// </summary>
        /// <param name="item">The item at the head of the segment</param>
        /// <returns>True if the item was read</returns>
        protected override bool TryPeekCore(out T item)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(this.GetType().Name);

            if (_readBuffer != null)
            {
                // Read from buffer first
                if (_readBuffer.TryPeek(out item))
                    return true;

                return TryTakeOrPeekThroughReadBuffer(out item, take: false);
            }
            else
            {
                return TryTakeOrPeek(out item, take: false);
            }
        }


        /// <summary>
        /// Cleans-up resources
        /// </summary>
        /// <param name="disposeBehaviour">Flag indicating whether the segment can be removed from disk</param>
        /// <param name="isUserCall">Was called explicitly by user</param>
        protected override void Dispose(DiskQueueSegmentDisposeBehaviour disposeBehaviour, bool isUserCall)
        {
            if (!_isDisposed)
            {
                _isDisposed = true;

                _readStream.Dispose();
                _writeStream.Dispose();

                if (disposeBehaviour == DiskQueueSegmentDisposeBehaviour.Delete)
                {
                    TurboContract.Assert(this.Count == 0, conditionString: "this.Count == 0");
                    File.Delete(_fileName);
                }
            }
        }
    }
}
