using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Queues.DiskQueueComponents
{
    /// <summary>
    /// Factory to create and discover PersistentDiskQueueSegment
    /// </summary>
    /// <typeparam name="T">Type of items stored in segment</typeparam>
    public class PersistentDiskQueueSegmentFactory<T> : DiskQueueSegmentFactory<T>
    {
        /// <summary>
        /// Segment file extension
        /// </summary>
        public const string SegmentFileExtension = ".pdqs";

        private readonly int _capacity;
        private readonly string _fileNamePrefix;
        private readonly IDiskQueueItemSerializer<T> _serializer;
        private readonly bool _fixSegmentDataErrors;
        private readonly int _flushToDiskOnItem;
        private readonly int _cachedMemoryWriteStreamSize;
        private readonly int _readBufferSize;
        private readonly int _cachedMemoryReadStreamSize;

        /// <summary>
        /// PersistentDiskQueueSegmentFactory constructor
        /// </summary>
        /// <param name="capacity">Maximum number of stored items inside the segement (overall capacity)</param>
        /// <param name="fileNamePrefix">Prefix for the segment file name</param>
        /// <param name="serializer">Items serializing/deserializing logic</param>
        /// <param name="fixSegmentDataErrors">Allows fixing errors inside segment when scanning it before open</param>
        /// <param name="flushToDiskOnItem">Determines the number of processed items, after that the flushing to disk should be performed (flushing to OS is always performed) (-1 - set to default value, 0 - never flush to disk, 1 - flush on every item)</param>
        /// <param name="cachedMemoryWriteStreamSize">Maximum size of the cached byte stream that used to serialize items in memory (-1 - set to default value, 0 - disable byte stream caching)</param>
        /// <param name="readBufferSize">Determines the number of items, that are stored in memory for read purposes (-1 - set to default value, 0 - disable read buffer)</param>
        /// <param name="cachedMemoryReadStreamSize">Maximum size of the cached byte stream that used to deserialize items in memory (-1 - set to default value, 0 - disable byte stream caching)</param>
        public PersistentDiskQueueSegmentFactory(int capacity, string fileNamePrefix, IDiskQueueItemSerializer<T> serializer,
            bool fixSegmentDataErrors, int flushToDiskOnItem, int cachedMemoryWriteStreamSize, int readBufferSize, int cachedMemoryReadStreamSize)
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

            _fixSegmentDataErrors = fixSegmentDataErrors;

            _flushToDiskOnItem = flushToDiskOnItem;
            _cachedMemoryWriteStreamSize = cachedMemoryWriteStreamSize;
            _readBufferSize = readBufferSize;
            _cachedMemoryReadStreamSize = cachedMemoryReadStreamSize;
        }
        /// <summary>
        /// PersistentDiskQueueSegmentFactory constructor
        /// </summary>
        /// <param name="capacity">Maximum number of stored items inside the segement (overall capacity)</param>
        /// <param name="fileNamePrefix">Prefix for the segment file name</param>
        /// <param name="serializer">Items serializing/deserializing logic</param>
        /// <param name="fixSegmentDataErrors">Allows fixing errors inside segment when scanning it before open</param>
        /// <param name="flushToDiskOnItem">Determines the number of processed items, after that the flushing to disk should be performed (flushing to OS is always performed) (-1 - set to default value, 0 - never flush to disk, 1 - flush on every item)</param>
        /// <param name="readBufferSize">Determines the number of items, that are stored in memory for read purposes (-1 - set to default value, 0 - disable read buffer)</param>
        public PersistentDiskQueueSegmentFactory(int capacity, string fileNamePrefix, IDiskQueueItemSerializer<T> serializer,
            bool fixSegmentDataErrors, int flushToDiskOnItem, int readBufferSize)
            : this(capacity, fileNamePrefix, serializer, fixSegmentDataErrors, flushToDiskOnItem, -1, readBufferSize, -1)
        {
        }
        /// <summary>
        /// PersistentDiskQueueSegmentFactory constructor
        /// </summary>
        /// <param name="capacity">Maximum number of stored items inside the segement (overall capacity)</param>
        /// <param name="fileNamePrefix">Prefix for the segment file name</param>
        /// <param name="serializer">Items serializing/deserializing logic</param>
        /// <param name="fixSegmentDataErrors">Allows fixing errors inside segment when scanning it before open</param>
        public PersistentDiskQueueSegmentFactory(int capacity, string fileNamePrefix, IDiskQueueItemSerializer<T> serializer, bool fixSegmentDataErrors)
            : this(capacity, fileNamePrefix, serializer, fixSegmentDataErrors, -1, -1, -1, -1)
        {
        }
        /// <summary>
        /// PersistentDiskQueueSegmentFactory constructor
        /// </summary>
        /// <param name="capacity">Maximum number of stored items inside the segement (overall capacity)</param>
        /// <param name="fileNamePrefix">Prefix for the segment file name</param>
        /// <param name="serializer">Items serializing/deserializing logic</param>
        public PersistentDiskQueueSegmentFactory(int capacity, string fileNamePrefix, IDiskQueueItemSerializer<T> serializer)
            : this(capacity, fileNamePrefix, serializer, false, -1, -1, -1, -1)
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
            return PersistentDiskQueueSegment<T>.CreateNew(number, fileName, _serializer, _capacity, _flushToDiskOnItem,
                _cachedMemoryWriteStreamSize, _readBufferSize, _cachedMemoryReadStreamSize);
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
            DiskQueueSegment<T>[] result = new DiskQueueSegment<T>[files.Length];
            for (int i = 0; i < files.Length; i++)
                result[i] = PersistentDiskQueueSegment<T>.Open(files[i].SegmentNumber, files[i].FileName, 
                    _serializer, _fixSegmentDataErrors, _flushToDiskOnItem, _cachedMemoryWriteStreamSize, _readBufferSize, _cachedMemoryReadStreamSize);

            return result;
        }
    }


    /// <summary>
    /// Persistent disk queue segment (preserve items between restarts)
    /// </summary>
    /// <typeparam name="T">The type of elements in segment</typeparam>
    public class PersistentDiskQueueSegment<T> : CountingDiskQueueSegment<T>
    {
        /// <summary>
        /// Item state on disk
        /// </summary>
        private enum ItemState : byte
        {
            /// <summary>
            /// New item during write process (when writign completed the state will be changed to 'Written')
            /// </summary>
            New = 0,
            /// <summary>
            /// Item correctly written to disk (available for read)
            /// </summary>
            Written = 1,
            /// <summary>
            /// Item correctly read from disk
            /// </summary>
            Read = 2,
            /// <summary>
            /// Item was corrupted (should skip it)
            /// </summary>
            Corrupted = 3
        }


        /// <summary>
        /// Item header structure
        /// </summary>
        private struct ItemHeader
        {
            public const int OffsetToStateByte = 7;
            public const int Size = 8;

            /// <summary>
            /// Convert 4-byte checksum to 3-byte
            /// </summary>
            /// <param name="original">Original checksum value</param>
            /// <returns></returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static int CoerceChecksum(int original)
            {            
                return (original & ((1 << 24) - 1));
            }

            /// <summary>
            /// Creates ItemHeader by main paramteres
            /// </summary>
            /// <param name="length">Size of the item</param>
            /// <param name="state">Item state</param>
            /// <param name="checkSum">Item checksum</param>
            /// <returns>Created ItemHeader</returns>
            public static ItemHeader Init(int length, ItemState state, int checkSum)
            {
                return new ItemHeader(length, ((byte)state << 24) | CoerceChecksum(checkSum));
            }
            /// <summary>
            /// Creates ItemHeader from byte array
            /// </summary>
            /// <param name="bytes">Array with data</param>
            /// <param name="startIndex">Starting index inside array</param>
            /// <returns>Created ItemHeader</returns>
            public static ItemHeader Init(byte[] bytes, int startIndex)
            {
                Debug.Assert(bytes != null);
                Debug.Assert(startIndex >= 0);
                Debug.Assert(startIndex + Size <= bytes.Length);

                return new ItemHeader(BitConverter.ToInt32(bytes, startIndex), BitConverter.ToInt32(bytes, startIndex + sizeof(int)));
            }

            private readonly int _length;
            private readonly int _info;

            /// <summary>
            /// ItemHeader constructor
            /// </summary>
            /// <param name="length">Size of the item</param>
            /// <param name="info">Item info (state + checksum)</param>
            public ItemHeader(int length, int info)
            {
                _length = length;
                _info = info;

                Debug.Assert(Enum.IsDefined(typeof(ItemState), State));
                Debug.Assert(Checksum <= (1 << 24));
            }

            /// <summary>
            /// Size of the item in bytes
            /// </summary>
            public int Length { get { return _length; } }
            /// <summary>
            /// Item info (state + checksum)
            /// </summary>
            public int Info { get { return _info; } }
            /// <summary>
            /// Item state
            /// </summary>
            public ItemState State { get { return (ItemState)((_info >> 24) & 255); } }
            /// <summary>
            /// Item checksum
            /// </summary>
            public int Checksum { get { return (_info & ((1 << 24) - 1)); } }

            /// <summary>
            /// Writes header to stream
            /// </summary>
            /// <param name="writer">Writer for the stream</param>
            public void WriteToStream(BinaryWriter writer)
            {
                writer.Write(_length);
                writer.Write(_info);
            }
        }

        /// <summary>
        /// Item info (item + its position on disk)
        /// </summary>
        private struct ItemReadInfo
        {
            private readonly T _item;
            private readonly long _position;

            public ItemReadInfo(T item, long position)
            {
                Debug.Assert(position >= 0);
                _item = item;
                _position = position;
            }

            /// <summary>
            /// Item
            /// </summary>
            public T Item { get { return _item; } }
            /// <summary>
            /// Item position whithin the file on disk
            /// </summary>
            public long Position { get { return _position; } }
        }


        /// <summary>
        /// Segment information
        /// </summary>
        public struct SegmentInformation
        {
            private readonly long _segmentNumber;
            private readonly int _totalItemCount;
            private readonly int _notTakenItemCount;
            private readonly int _corruptedItemCount;
            private readonly int _capacity;
            private readonly long _startPosition;

            /// <summary>
            /// SegmentInformation constructor
            /// </summary>
            /// <param name="segmentNumber">Number of the segment stored in header</param>
            /// <param name="totalItemCount">Total number of items stored inside segment</param>
            /// <param name="notTakenItemCount">Number of active items</param>
            /// <param name="corruptedItemCount">Number of corrupted items</param>
            /// <param name="capacity">Capacity of the segment stored in header</param>
            /// <param name="startPosition">Position of the first item to read</param>
            internal SegmentInformation(long segmentNumber, int totalItemCount, int notTakenItemCount, int corruptedItemCount, int capacity, long startPosition)
            {
                if (totalItemCount < 0)
                    throw new ArgumentOutOfRangeException(nameof(totalItemCount));
                if (notTakenItemCount < 0 || notTakenItemCount > totalItemCount)
                    throw new ArgumentOutOfRangeException(nameof(notTakenItemCount));
                if (corruptedItemCount < 0 || corruptedItemCount > totalItemCount)
                    throw new ArgumentOutOfRangeException(nameof(corruptedItemCount));
                if (capacity <= 0)
                    throw new ArgumentOutOfRangeException(nameof(capacity));

                _segmentNumber = segmentNumber;
                _totalItemCount = totalItemCount;
                _notTakenItemCount = notTakenItemCount;
                _corruptedItemCount = corruptedItemCount;
                _capacity = capacity;
                _startPosition = startPosition;
            }
            /// <summary>
            /// SegmentInformation constructor
            /// </summary>
            /// <param name="segmentNumber">Number of the segment stored in header</param>
            /// <param name="totalItemCount">Total number of items stored inside segment</param>
            /// <param name="notTakenItemCount">Number of active items</param>
            /// <param name="corruptedItemCount">Number of corrupted items</param>
            /// <param name="capacity">Capacity of the segment stored in header</param>
            public SegmentInformation(long segmentNumber, int totalItemCount, int notTakenItemCount, int corruptedItemCount, int capacity)
            {
                if (totalItemCount < 0)
                    throw new ArgumentOutOfRangeException(nameof(totalItemCount));
                if (notTakenItemCount < 0 || notTakenItemCount > totalItemCount)
                    throw new ArgumentOutOfRangeException(nameof(notTakenItemCount));
                if (corruptedItemCount < 0 || corruptedItemCount > totalItemCount)
                    throw new ArgumentOutOfRangeException(nameof(corruptedItemCount));
                if (capacity <= 0)
                    throw new ArgumentOutOfRangeException(nameof(capacity));

                _segmentNumber = segmentNumber;
                _totalItemCount = totalItemCount;
                _notTakenItemCount = notTakenItemCount;
                _corruptedItemCount = corruptedItemCount;
                _capacity = capacity;
                _startPosition = 0;
            }

            /// <summary>
            /// Number of the segment stored in header
            /// </summary>
            public long SegmentNumber { get { return _segmentNumber; } }
            /// <summary>
            /// Total number of items stored inside segment
            /// </summary>
            public int TotalItemCount { get { return _totalItemCount; } }
            /// <summary>
            /// Number of active item
            /// </summary>
            public int NotTakenItemCount { get { return _notTakenItemCount; } }
            /// <summary>
            /// Number of corrupted items
            /// </summary>
            public int CorruptedItemCount { get { return _corruptedItemCount; } }
            /// <summary>
            /// Capacity of the segment stored in header
            /// </summary>
            public int Capacity { get { return _capacity; } }
            /// <summary>
            /// Position of the first item to read
            /// </summary>
            internal long StartPosition { get { return _startPosition; } }
        }



        // =================

        /// <summary>
        /// Creates new instance of <see cref="PersistentDiskQueueSegmentFactory{T}"/>
        /// </summary>
        /// <param name="capacity">Maximum number of stored items inside the segement (overall capacity)</param>
        /// <param name="fileNamePrefix">Prefix for the segment file name</param>
        /// <param name="serializer">Items serializing/deserializing logic</param>
        /// <param name="fixSegmentDataErrors">Allows fixing errors inside segment when scanning it before open</param>
        /// <returns>Created <see cref="PersistentDiskQueueSegmentFactory{T}"/></returns>
        public static PersistentDiskQueueSegmentFactory<T> CreateFactory(int capacity, string fileNamePrefix, IDiskQueueItemSerializer<T> serializer, bool fixSegmentDataErrors)
        {
            return new PersistentDiskQueueSegmentFactory<T>(capacity, fileNamePrefix, serializer, fixSegmentDataErrors);
        }

        // =================


        /// <summary>
        /// Scans existed segment on disk and fix errors when <paramref name="fixSegmentDataErrors"/> is specified
        /// </summary>
        /// <param name="fileName">Full file name for the segment</param>
        /// <param name="fixSegmentDataErrors">Allows fixing errors inside segment</param>
        /// <returns>Segment information</returns>
        public static SegmentInformation ScanSegment(string fileName, bool fixSegmentDataErrors)
        {
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException(nameof(fileName));
            if (!File.Exists(fileName))
                throw new ArgumentException($"PersistentDiskQueueSegment file is not found '{fileName}'", nameof(fileName));

            using (var exclusivityCheckStream = new FileStream(fileName, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }

            int totalItemCount = 0;
            int validItemCount = 0;
            int corruptedItemCount = 0;
            long initialPosition = 0;

            long segmentNumber = 0;
            int segmentCapacity = 0;

            using (var scanStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var fileUpdatingStream = new FileStream(fileName, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite, bufferSize: 4))
            {
                long streamLength = scanStream.Length;

                ReadAndValidateSegmentHeader(scanStream, out segmentNumber, out segmentCapacity);

                MemoryStream targetStream = new MemoryStream();

                while (true)
                {
                    ItemHeader itemHeader;
                    long itemPosition = scanStream.Position;
                    targetStream.Position = 0;

                    // Read item header
                    if (!TryReadItemHeaderFromDisk(scanStream, targetStream, out itemHeader))
                    {
                        if (scanStream.Position > itemPosition)
                        {
                            corruptedItemCount++;
                            if (fixSegmentDataErrors)
                                fileUpdatingStream.SetLength(itemPosition); // Trim incorrect end of the segment
                        }
                        break;
                    }

                    // Skip content
                    if (fixSegmentDataErrors)
                    {
                        if (!TryReadItemBytesFromDisk(scanStream, targetStream, ref itemHeader))
                        {
                            if (scanStream.Position + itemHeader.Length > streamLength)
                            {
                                corruptedItemCount++;
                                fileUpdatingStream.SetLength(itemPosition); // Trim incorrect end of the segment
                            }

                            break;
                        }

                        // Validate checksum
                        int checkSum = ItemHeader.CoerceChecksum(CalculateChecksum(targetStream.GetBuffer(), ItemHeader.Size, itemHeader.Length));
                        if (checkSum != itemHeader.Checksum)
                        {
                            // Mark item as Corrupted
                            fileUpdatingStream.Seek(itemPosition + ItemHeader.OffsetToStateByte, SeekOrigin.Begin);
                            fileUpdatingStream.WriteByte((byte)ItemState.Corrupted);
                            fileUpdatingStream.Flush(true);
                            itemHeader = ItemHeader.Init(itemHeader.Length, ItemState.Corrupted, itemHeader.Checksum);
                            corruptedItemCount++;
                        }
                    }
                    else
                    {
                        if (scanStream.Position + itemHeader.Length > streamLength)
                        {
                            corruptedItemCount++;
                            break;
                        }

                        // Just skip the item
                        scanStream.Seek(scanStream.Position + itemHeader.Length, SeekOrigin.Begin);
                    }


                    // Fix bad state
                    if (itemHeader.State == ItemState.New && fixSegmentDataErrors)
                    {
                        fileUpdatingStream.Seek(itemPosition + ItemHeader.OffsetToStateByte, SeekOrigin.Begin);
                        fileUpdatingStream.WriteByte((byte)ItemState.Corrupted);
                        fileUpdatingStream.Flush(true);
                        itemHeader = ItemHeader.Init(itemHeader.Length, ItemState.Corrupted, itemHeader.Checksum);
                        corruptedItemCount++;
                    }


                    if (itemHeader.State == ItemState.Written)
                    {
                        if (initialPosition <= 0)
                            initialPosition = itemPosition;
                        validItemCount++;
                    }
                    totalItemCount++;
                }
            }

            return new SegmentInformation(segmentNumber, totalItemCount, validItemCount, corruptedItemCount, segmentCapacity, initialPosition);
        }
        

        /// <summary>
        /// Creates PersistentDiskQueueSegment by opening existing segment on disk
        /// </summary>
        /// <param name="segmentNumber">Segment number</param>
        /// <param name="fileName">Full file name for the segment</param>
        /// <param name="serializer">Items serializing/deserializing logic</param>
        /// <param name="fixSegmentDataErrors">Allows fixing errors inside segment</param>
        /// <param name="flushToDiskOnItem">Determines the number of processed items, after that the flushing to disk should be performed (flushing to OS is always performed) (-1 - set to default value, 0 - never flush to disk, 1 - flush on every item)</param>
        /// <param name="cachedMemoryWriteStreamSize">Maximum size of the cached byte stream that used to serialize items in memory (-1 - set to default value, 0 - disable byte stream caching)</param>
        /// <param name="readBufferSize">Determines the number of items, that are stored in memory for read purposes (-1 - set to default value, 0 - disable read buffer)</param>
        /// <param name="cachedMemoryReadStreamSize">Maximum size of the cached byte stream that used to deserialize items in memory (-1 - set to default value, 0 - disable byte stream caching)</param>
        /// <returns>Created segment</returns>
        public static PersistentDiskQueueSegment<T> Open(long segmentNumber, string fileName, IDiskQueueItemSerializer<T> serializer,
                                          bool fixSegmentDataErrors,
                                          int flushToDiskOnItem, int cachedMemoryWriteStreamSize, int readBufferSize, int cachedMemoryReadStreamSize)
        {
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException(nameof(fileName));
            if (serializer == null)
                throw new ArgumentNullException(nameof(serializer));
            if (!File.Exists(fileName))
                throw new ArgumentException($"PersistentDiskQueueSegment file is not found '{fileName}'. To create new use another method", nameof(fileName));

            var segmentInfo = ScanSegment(fileName, fixSegmentDataErrors);

            return new PersistentDiskQueueSegment<T>(segmentNumber, true, 
                segmentInfo.Capacity, segmentInfo.NotTakenItemCount, segmentInfo.Capacity, fileName, segmentInfo.StartPosition, 
                serializer, 
                flushToDiskOnItem, cachedMemoryWriteStreamSize, readBufferSize, cachedMemoryReadStreamSize);
        }
        /// <summary>
        /// Creates PersistentDiskQueueSegment by opening existing segment on disk
        /// </summary>
        /// <param name="segmentNumber">Segment number</param>
        /// <param name="fileName">Full file name for the segment</param>
        /// <param name="serializer">Items serializing/deserializing logic</param>
        /// <param name="fixSegmentDataErrors">Allows fixing errors inside segment</param>
        /// <param name="flushToDiskOnItem">Determines the number of processed items, after that the flushing to disk should be performed (flushing to OS is always performed) (-1 - set to default value, 0 - never flush to disk, 1 - flush on every item)</param>
        /// <param name="readBufferSize">Determines the number of items, that are stored in memory for read purposes (-1 - set to default value, 0 - disable read buffer)</param>
        /// <returns>Created segment</returns>
        public static PersistentDiskQueueSegment<T> Open(long segmentNumber, string fileName, IDiskQueueItemSerializer<T> serializer,
                                          bool fixSegmentDataErrors,
                                          int flushToDiskOnItem, int readBufferSize)
        {
            return Open(segmentNumber, fileName, serializer, fixSegmentDataErrors, flushToDiskOnItem, -1, readBufferSize, -1);
        }
        /// <summary>
        /// Creates PersistentDiskQueueSegment by opening existing segment on disk
        /// </summary>
        /// <param name="segmentNumber">Segment number</param>
        /// <param name="fileName">Full file name for the segment</param>
        /// <param name="serializer">Items serializing/deserializing logic</param>
        /// <param name="fixSegmentDataErrors">Allows fixing errors inside segment</param>
        /// <returns>Created segment</returns>
        public static PersistentDiskQueueSegment<T> Open(long segmentNumber, string fileName, IDiskQueueItemSerializer<T> serializer,
                                          bool fixSegmentDataErrors)
        {
            return Open(segmentNumber, fileName, serializer, fixSegmentDataErrors, -1, -1, -1, -1);
        }
        /// <summary>
        /// Creates PersistentDiskQueueSegment by opening existing segment on disk
        /// </summary>
        /// <param name="segmentNumber">Segment number</param>
        /// <param name="fileName">Full file name for the segment</param>
        /// <param name="serializer">Items serializing/deserializing logic</param>
        /// <returns>Created segment</returns>
        public static PersistentDiskQueueSegment<T> Open(long segmentNumber, string fileName, IDiskQueueItemSerializer<T> serializer)
        {
            return Open(segmentNumber, fileName, serializer, false, -1, -1, -1, -1);
        }



        /// <summary>
        /// Creates new PersistentDiskQueueSegment on disk. Can't be used to open an existing segment.
        /// </summary>
        /// <param name="segmentNumber">Segment number</param>
        /// <param name="fileName">Full file name for the segment</param>
        /// <param name="serializer">Items serializing/deserializing logic</param>
        /// <param name="capacity">Maximum number of stored items inside the segement (overall capacity)</param>
        /// <param name="flushToDiskOnItem">Determines the number of processed items, after that the flushing to disk should be performed (flushing to OS is always performed) (-1 - set to default value, 0 - never flush to disk, 1 - flush on every item)</param>
        /// <param name="cachedMemoryWriteStreamSize">Maximum size of the cached byte stream that used to serialize items in memory (-1 - set to default value, 0 - disable byte stream caching)</param>
        /// <param name="readBufferSize">Determines the number of items, that are stored in memory for read purposes (-1 - set to default value, 0 - disable read buffer)</param>
        /// <param name="cachedMemoryReadStreamSize">Maximum size of the cached byte stream that used to deserialize items in memory (-1 - set to default value, 0 - disable byte stream caching)</param>
        /// <returns>Created segment</returns>
        public static PersistentDiskQueueSegment<T> CreateNew(long segmentNumber, string fileName, IDiskQueueItemSerializer<T> serializer, int capacity,
                                          int flushToDiskOnItem, int cachedMemoryWriteStreamSize, int readBufferSize, int cachedMemoryReadStreamSize)
        {
            return new PersistentDiskQueueSegment<T>(segmentNumber, fileName, serializer, capacity, 
                flushToDiskOnItem, cachedMemoryReadStreamSize, readBufferSize, cachedMemoryReadStreamSize);
        }
        /// <summary>
        /// Creates new PersistentDiskQueueSegment on disk. Can't be used to open an existing segment.
        /// </summary>
        /// <param name="segmentNumber">Segment number</param>
        /// <param name="fileName">Full file name for the segment</param>
        /// <param name="serializer">Items serializing/deserializing logic</param>
        /// <param name="capacity">Maximum number of stored items inside the segement (overall capacity)</param>
        /// <param name="flushToDiskOnItem">Determines the number of processed items, after that the flushing to disk should be performed (flushing to OS is always performed) (-1 - set to default value, 0 - never flush to disk, 1 - flush on every item)</param>
        /// <param name="readBufferSize">Determines the number of items, that are stored in memory for read purposes (-1 - set to default value, 0 - disable read buffer)</param>
        /// <returns>Created segment</returns>
        public static PersistentDiskQueueSegment<T> CreateNew(long segmentNumber, string fileName, IDiskQueueItemSerializer<T> serializer, int capacity,
                                          int flushToDiskOnItem, int readBufferSize)
        {
            return new PersistentDiskQueueSegment<T>(segmentNumber, fileName, serializer, capacity,
                flushToDiskOnItem, -1, readBufferSize, -1);
        }
        /// <summary>
        /// Creates new PersistentDiskQueueSegment on disk. Can't be used to open an existing segment.
        /// </summary>
        /// <param name="segmentNumber">Segment number</param>
        /// <param name="fileName">Full file name for the segment</param>
        /// <param name="serializer">Items serializing/deserializing logic</param>
        /// <param name="capacity">Maximum number of stored items inside the segement (overall capacity)</param>
        /// <returns>Created segment</returns>
        public static PersistentDiskQueueSegment<T> CreateNew(long segmentNumber, string fileName, IDiskQueueItemSerializer<T> serializer, int capacity)
        {
            return new PersistentDiskQueueSegment<T>(segmentNumber, fileName, serializer, capacity, -1, -1, -1, -1);
        }


        // =================

        private const int DefaultFlushToDiskOnItem = 32;
        private const int DefaultReadBufferSize = 32;
      
        private const int InitialCacheSizePerItem = 4;
        private const int MaxChacheSizePerItem = 1024;
        private const int MaxCachedMemoryStreamSize = 32 * 1024;


        private readonly string _fileName;
        private readonly IDiskQueueItemSerializer<T> _serializer;

        private readonly object _writeLock;
        private readonly FileStream _writeStream;

        private readonly object _readLock;
        private readonly FileStream _readStream;

        private readonly object _readMarkerLock;
        private readonly FileStream _readMarkerStream;

        private readonly int _flushToDiskOnItem;
        private volatile int _operationsToFlushCount;

        private volatile RegionBinaryWriter _cachedMemoryWriteStream;
        private readonly int _maxCachedMemoryWriteStreamSize;

        private readonly ConcurrentQueue<ItemReadInfo> _readBuffer;
        private readonly int _maxReadBufferSize;

        private volatile RegionBinaryReader _cachedMemoryReadStream;
        private readonly int _maxCachedMemoryReadStreamSize;

        private volatile bool _isDisposed;


        /// <summary>
        /// PersistentDiskQueueSegment constructor
        /// </summary>
        /// <param name="segmentNumber">Segment number</param>
        /// <param name="openExisted">True - open existed segment, False - create new segment</param>
        /// <param name="itemCount">Count of already presented items inside the segment (required when openExisted = true)</param>
        /// <param name="fillCount">Number of items that was stored inside segment (number of filled slots for items) (required when openExisted = true)</param>
        /// <param name="fileName">Full file name for the segment</param>
        /// <param name="initialFilePosition">Initial file position to skip already read items (used when openExisted = true)</param>
        /// <param name="serializer">Items serializing/deserializing logic</param>
        /// <param name="capacity">Maximum number of stored items inside the segement (overall capacity)</param>
        /// <param name="flushToDiskOnItem">Determines the number of processed items, after that the flushing to disk should be performed (flushing to OS is always performed) (-1 - set to default value, 0 - never flush to disk, 1 - flush on every item)</param>
        /// <param name="cachedMemoryWriteStreamSize">Maximum size of the cached byte stream that used to serialize items in memory (-1 - set to default value, 0 - disable byte stream caching)</param>
        /// <param name="readBufferSize">Determines the number of items, that are stored in memory for read purposes (-1 - set to default value, 0 - disable read buffer)</param>
        /// <param name="cachedMemoryReadStreamSize">Maximum size of the cached byte stream that used to deserialize items in memory (-1 - set to default value, 0 - disable byte stream caching)</param>
        protected PersistentDiskQueueSegment(long segmentNumber, bool openExisted, int capacity, int itemCount, int fillCount, 
                                          string fileName, long initialFilePosition, 
                                          IDiskQueueItemSerializer<T> serializer,
                                          int flushToDiskOnItem, int cachedMemoryWriteStreamSize, int readBufferSize, int cachedMemoryReadStreamSize)
            : base(segmentNumber, capacity, itemCount, fillCount)
        {
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException(nameof(fileName));
            if (serializer == null)
                throw new ArgumentNullException(nameof(serializer));
            if (!openExisted && File.Exists(fileName))
                throw new ArgumentException($"Can't create PersistentDiskQueueSegment on existing file '{fileName}'. To open existed use another consturctor.", nameof(fileName));
            if (openExisted && !File.Exists(fileName))
                throw new ArgumentException($"Can't create PersistentDiskQueueSegment on non existing file '{fileName}'. To create new segment use another consturctor.", nameof(fileName));
            if (!openExisted && itemCount != 0)
                throw new ArgumentException("ItemCount should be zero when new segment is created", nameof(itemCount));
            if (!openExisted && fillCount != 0)
                throw new ArgumentException("FillCount should be zero when new segment is created", nameof(itemCount));

            _fileName = fileName;
            _serializer = serializer;

            try
            {
                if (openExisted)
                    _writeStream = new FileStream(_fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                else
                    _writeStream = new FileStream(_fileName, FileMode.CreateNew, FileAccess.Write, FileShare.ReadWrite);
                _readStream = new FileStream(_fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                _readMarkerStream = new FileStream(_fileName, FileMode.Open, FileAccess.Write, FileShare.ReadWrite, bufferSize: 4);

                _writeLock = new object();
                _readLock = new object();
                _readMarkerLock = new object();

                _flushToDiskOnItem = flushToDiskOnItem;
                _operationsToFlushCount = 0;
                if (flushToDiskOnItem < 0)
                {
                    checked
                    {
                        if (serializer.ExpectedSizeInBytes > 0)
                            _flushToDiskOnItem = Math.Max(DefaultFlushToDiskOnItem, 8192 / (serializer.ExpectedSizeInBytes + ItemHeader.Size));
                        else
                            _flushToDiskOnItem = DefaultFlushToDiskOnItem;
                    }
                }

                _cachedMemoryWriteStream = null;
                _maxCachedMemoryWriteStreamSize = cachedMemoryWriteStreamSize;
                if (cachedMemoryWriteStreamSize < 0)
                {
                    checked
                    {
                        int expectedItemSize = MaxChacheSizePerItem;
                        if (serializer.ExpectedSizeInBytes > 0)
                            expectedItemSize = Math.Min(serializer.ExpectedSizeInBytes + ItemHeader.Size, MaxChacheSizePerItem);
                        _maxCachedMemoryWriteStreamSize = Math.Min(MaxCachedMemoryStreamSize, expectedItemSize);
                    }
                }


                _maxReadBufferSize = readBufferSize >= 0 ? readBufferSize : DefaultReadBufferSize;
                if (_maxReadBufferSize > 0)
                    _readBuffer = new ConcurrentQueue<ItemReadInfo>();

                _cachedMemoryReadStream = null;
                _maxCachedMemoryReadStreamSize = cachedMemoryReadStreamSize;
                if (cachedMemoryReadStreamSize < 0)
                {
                    checked
                    {
                        int expectedItemSize = MaxChacheSizePerItem;
                        if (serializer.ExpectedSizeInBytes > 0)
                            expectedItemSize = Math.Min(serializer.ExpectedSizeInBytes + ItemHeader.Size, MaxChacheSizePerItem);
                        _maxCachedMemoryReadStreamSize = Math.Min(MaxCachedMemoryStreamSize, expectedItemSize);
                    }
                }


                if (!openExisted)
                {
                    // Prepare file header
                    WriteSegmentHeader(_writeStream, Number, Capacity);
                    _readStream.Seek(0, SeekOrigin.End); // Offset readStream after the header
                }
                else
                {
                    // Read file header
                    ReadAndValidateSegmentHeader(_readStream);
                    _writeStream.Seek(0, SeekOrigin.End); // Offset write stream after the header
                    if (initialFilePosition > _readStream.Position)
                        _readStream.Seek(initialFilePosition, SeekOrigin.Begin); // Offset to expected position
                }

                _isDisposed = false;

                Debug.Assert(_flushToDiskOnItem >= 0);
                Debug.Assert(_maxCachedMemoryWriteStreamSize >= 0);
                Debug.Assert(_maxReadBufferSize >= 0);
                Debug.Assert(_maxCachedMemoryReadStreamSize >= 0);
                Debug.Assert(openExisted || _writeStream.Position == _readStream.Position);
            }
            catch
            {
                // Should close streams to make created files available to delete
                if (_writeStream != null)
                    _writeStream.Dispose();
                if (_readStream != null)
                    _readStream.Dispose();
                if (_readMarkerStream != null)
                    _readMarkerStream.Dispose();

                throw;
            }
        }


        /// <summary>
        /// PersistentDiskQueueSegment constructor that creates a new segment on disk. Can't be used to open an existing segment.
        /// </summary>
        /// <param name="segmentNumber">Segment number</param>
        /// <param name="fileName">Full file name for the segment</param>
        /// <param name="serializer">Items serializing/deserializing logic</param>
        /// <param name="capacity">Maximum number of stored items inside the segement (overall capacity)</param>
        /// <param name="flushToDiskOnItem">Determines the number of processed items, after that the flushing to disk should be performed (flushing to OS is always performed) (-1 - set to default value, 0 - never flush to disk, 1 - flush on every item)</param>
        /// <param name="cachedMemoryWriteStreamSize">Maximum size of the cached byte stream that used to serialize items in memory (-1 - set to default value, 0 - disable byte stream caching)</param>
        /// <param name="readBufferSize">Determines the number of items, that are stored in memory for read purposes (-1 - set to default value, 0 - disable read buffer)</param>
        /// <param name="cachedMemoryReadStreamSize">Maximum size of the cached byte stream that used to deserialize items in memory (-1 - set to default value, 0 - disable byte stream caching)</param>
        public PersistentDiskQueueSegment(long segmentNumber, string fileName, IDiskQueueItemSerializer<T> serializer, int capacity,
                                          int flushToDiskOnItem, int cachedMemoryWriteStreamSize, int readBufferSize, int cachedMemoryReadStreamSize)
            : this(segmentNumber, false, capacity, 0, 0, fileName, -1, serializer, flushToDiskOnItem, cachedMemoryWriteStreamSize, readBufferSize, cachedMemoryReadStreamSize)
        {
        }


        /// <summary>
        /// Determines the number of processed items, after that the flushing to disk should be performed (flushing to OS is always performed)
        /// </summary>
        public int FlushToDiskOnItem { get { return _flushToDiskOnItem; } }
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
        /// Is segment is in read-only mode
        /// </summary>
        public bool IsReadOnly { get { return !_writeStream.CanWrite; } }


        /// <summary>
        /// Writes segment header when file is created (should be called from constructor)
        /// </summary>
        private static void WriteSegmentHeader(FileStream stream, long number, int capacity)
        {
            BinaryWriter writer = new BinaryWriter(stream);

            // Write 4-byte segment identifier
            writer.Write((byte)'P');
            writer.Write((byte)'D');
            writer.Write((byte)'S');
            writer.Write((byte)1);

            // Write 8-byte segment number
            writer.Write((long)number);

            // Write 4-byte capacity
            writer.Write((int)capacity);

            writer.Flush();
        }

        /// <summary>
        /// Reads segment header and validate signature
        /// </summary>
        private static void ReadAndValidateSegmentHeader(FileStream stream, out long number, out int capacity)
        {
            BinaryReader reader = new BinaryReader(stream);

            // Read segment identifier
            char b1 = (char)reader.ReadByte();
            char b2 = (char)reader.ReadByte();
            char b3 = (char)reader.ReadByte();
            byte b4 = reader.ReadByte();

            if (b1 != 'P' || b2 != 'D' || b3 != 'S' || b4 != 1)
                throw new InvalidOperationException($"Incorrect segment header. Expected signature: 'PDS1'. Found: '{b1}{b2}{b3}{b4}'");

            // Read segment number
            number = reader.ReadInt64();

            // Read segment capacity
            capacity = reader.ReadInt32();
        }
        /// <summary>
        /// Reads segment header and validate signature
        /// </summary>
        private static void ReadAndValidateSegmentHeader(FileStream stream)
        {
            long tmpNum = 0;
            int tmpCapacity = 0;
            ReadAndValidateSegmentHeader(stream, out tmpNum, out tmpCapacity);
        }


        /// <summary>
        /// Calculates simple checksum
        /// </summary>
        /// <param name="data">Data array</param>
        /// <param name="startIndex">Start index</param>
        /// <param name="length">Length of the data</param>
        /// <returns>Checksum (lower 3 bytes)</returns>
        private static int CalculateChecksum(byte[] data, int startIndex, int length)
        {
            Debug.Assert(data != null);
            Debug.Assert(startIndex >= 0 && startIndex < data.Length);
            Debug.Assert(length >= 0);
            Debug.Assert(startIndex + length <= data.Length);

            if (length == 0)
                return 0;

            uint firstByte = data[startIndex];
            uint middleByte = data[startIndex + length / 2];
            uint lastByte = data[startIndex + length - 1];

            return (int)((firstByte << 16) | (middleByte << 8) | (lastByte));
        }


        // ================= MemoryWriteStream ==================

        /// <summary>
        /// Gets cached memory stream to write
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
                if (expectedItemSize <= 0)
                    expectedItemSize = InitialCacheSizePerItem;
                result = new RegionBinaryWriter(Math.Min(MaxCachedMemoryStreamSize, Math.Max(0, itemCount * (expectedItemSize + ItemHeader.Size)))); // Max used to protect from overflow
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
            Debug.Assert(_cachedMemoryWriteStream == null);

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
            Debug.Assert(Monitor.IsEntered(_readLock));
            Debug.Assert(itemCount > 0);

            RegionBinaryReader result = _cachedMemoryReadStream;
            _cachedMemoryReadStream = null;
            if (result == null)
            {
                int expectedItemSize = _serializer.ExpectedSizeInBytes;
                if (expectedItemSize <= 0)
                    expectedItemSize = InitialCacheSizePerItem;
                result = new RegionBinaryReader(Math.Min(MaxCachedMemoryStreamSize, Math.Max(0, itemCount * (expectedItemSize + ItemHeader.Size)))); // Max used to protect from overflow
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
            Debug.Assert(bufferingReadStream != null);
            Debug.Assert(Monitor.IsEntered(_readLock));
            Debug.Assert(_cachedMemoryReadStream == null);

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
        /// Build segment record with specified item in memory stream.
        /// (writes header + item bytes)
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
                stream.SetOriginLength(origin + ItemHeader.Size, -1); // Offset 4 to store length later
                Debug.Assert(stream.Length == 0);

                _serializer.Serialize(writer, item);
                writer.Flush();
                Debug.Assert(stream.Length >= 0);

                int length = (int)stream.Length;
                int checkSum = CalculateChecksum(stream.InnerStream.GetBuffer(), origin + ItemHeader.Size, length);
                var header = ItemHeader.Init(length, ItemState.New, checkSum);

                stream.SetOrigin(origin); // Offset back to the beggining
                header.WriteToStream(writer); // Write header

                stream.Seek(0, SeekOrigin.End); // Seek to the end of the stream
            }
        }


        /// <summary>
        /// Adds new item to the tail of the segment (core implementation)
        /// </summary>
        /// <param name="item">New item</param>
        protected override void AddCore(T item)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(this.GetType().Name);
            if (IsReadOnly)
                throw new InvalidOperationException($"Segment is in readonly mode ('{_fileName}')");

            lock (_writeLock)
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(this.GetType().Name);

                RegionBinaryWriter writer = GetMemoryWriteStream(1);
                Debug.Assert(writer.BaseStream.Length == 0);
                Debug.Assert(writer.BaseStream.InnerStream.Length == 0);

                SerializeItemToStream(item, writer);

                // Write in two passes to prevent item corruption when app terminated unexpectadly

                // Write item bytes to disk
                long initialStreamPosition = _writeStream.Position;
                long streamPositionAfterWriteCompleted = initialStreamPosition + writer.BaseStream.InnerStream.Length;
                try
                {
                    writer.BaseStream.InnerStream.WriteTo(_writeStream);
                    _writeStream.Flush(flushToDisk: false);

                    Debug.Assert(streamPositionAfterWriteCompleted == _writeStream.Position);

                    // Mark record as written
                    _writeStream.Position = initialStreamPosition + ItemHeader.OffsetToStateByte;
                    _writeStream.WriteByte((byte)ItemState.Written);

                    int writtenItemCount = Interlocked.Increment(ref _operationsToFlushCount);
                    _writeStream.Flush(flushToDisk: _flushToDiskOnItem > 0 && (writtenItemCount % _flushToDiskOnItem) == 0); // Flush to disk periodically
                }
                finally
                {
                    // Always move position to the end of the item
                    _writeStream.Position = streamPositionAfterWriteCompleted;
                }

                ReleaseMemoryWriteStream(writer);
            }
        }


        // ======================

        /// <summary>
        /// Reads item header from readStream to targetStream.
        /// readStream position is always moves forward. 
        /// targetStream position moves forward on success
        /// </summary>
        private static bool TryReadItemHeaderFromDisk(FileStream readStream, MemoryStream targetStream, out ItemHeader itemHeader)
        {
            Debug.Assert(readStream != null);
            Debug.Assert(targetStream != null);

            // Ensure capacity on MemoryStream
            targetStream.SetLength(targetStream.Position + ItemHeader.Size);

            // Read item header
            var rawBuffer = targetStream.GetBuffer();
            int readCount = readStream.Read(rawBuffer, (int)targetStream.Position, ItemHeader.Size);
            if (readCount != ItemHeader.Size) // No space for item header
            {
                itemHeader = default(ItemHeader);
                return false;
            }

            itemHeader = ItemHeader.Init(rawBuffer, (int)targetStream.Position);
            targetStream.Position += ItemHeader.Size;
            return true;
        }
        /// <summary>
        /// Reads item bytes from readStream to targetStream according to itemHeader.
        /// readStream position is always moves forward. 
        /// targetStream position moves forward on success
        /// </summary>
        private static bool TryReadItemBytesFromDisk(FileStream readStream, MemoryStream targetStream, ref ItemHeader itemHeader)
        {
            Debug.Assert(readStream != null);
            Debug.Assert(targetStream != null);

            // Ensure capacity on MemoryStream
            targetStream.SetLength(targetStream.Position + itemHeader.Length);

            // Read item
            var rawBuffer = targetStream.GetBuffer();
            var readCount = readStream.Read(rawBuffer, (int)targetStream.Position, itemHeader.Length);
            if (readCount != itemHeader.Length)
                return false;

            targetStream.Position += itemHeader.Length;
            return true;
        }

        /// <summary>
        /// Read single item bytes from disk to <paramref name="targetStream"/> incuding header. 
        /// </summary>
        /// <param name="targetStream">Target stream to read bytes (Position is not changed after read)</param>
        /// <param name="itemHeader">Header of the readed item</param>
        /// <param name="itemPosition">Item position inside <see cref="_readStream"/></param>
        /// <param name="stopOnNewState">True - stop reading on item in 'New' state</param>
        /// <param name="take">Should move readStream position</param>
        /// <returns>Is read</returns>
        private bool TryTakeOrPeekSingleItemBytesFromDisk(MemoryStream targetStream, out ItemHeader itemHeader, out long itemPosition, bool stopOnNewState, bool take)
        {
            Debug.Assert(targetStream != null);
            Debug.Assert(Monitor.IsEntered(_readLock));

            itemPosition = _readStream.Position;
            long targetStreamPosition = targetStream.Position;

            try
            {
                while (true)
                {
                    itemPosition = _readStream.Position;
                    targetStream.Position = targetStreamPosition;

                    // Read item header
                    if (!TryReadItemHeaderFromDisk(_readStream, targetStream, out itemHeader))
                    {
                        if (_readStream.Position != itemPosition)
                            _readStream.Seek(itemPosition, SeekOrigin.Begin);

                        Debug.Assert(_readStream.Position == itemPosition);
                        return false;
                    }

                    // Skip read/corrupted
                    if (itemHeader.State == ItemState.Read || itemHeader.State == ItemState.Corrupted)
                    {
                        _readStream.Seek(itemHeader.Length, SeekOrigin.Current); // Skip position forward
                        continue;
                    }
                    // Stop on new state
                    if (itemHeader.State == ItemState.New && stopOnNewState) 
                    {
                        _readStream.Seek(itemPosition, SeekOrigin.Begin); // Rewind back the position
                        Debug.Assert(_readStream.Position == itemPosition);
                        return false;
                    }
                    // Skip or throw on New state when it is invalid
                    if (itemHeader.State == ItemState.New && !stopOnNewState)
                    {
                        _readStream.Seek(itemHeader.Length, SeekOrigin.Current); // Move position forward
                        throw new ItemCorruptedException($"Incorrect item state (New). That indicates that the file is corrupted ({_fileName}). To fix the problem you can use '{nameof(ScanSegment)}' method.");
                    }

                    break;
                }

                // Read item
                if (!TryReadItemBytesFromDisk(_readStream, targetStream, ref itemHeader))
                {
                    _readStream.Seek(itemPosition, SeekOrigin.Begin); // Rewind back the position
                    Debug.Assert(_readStream.Position == itemPosition);
                    return false;
                }


                if (!take)
                {
                    // For peek should rewind stream position
                    _readStream.Seek(itemPosition, SeekOrigin.Begin); // Rewind back the position
                }
            }
            catch
            {
                _readStream.Position = itemPosition; // Correct strem position in exceptional case
                throw;
            }

            Debug.Assert(targetStream.Position == targetStream.Length);
            Debug.Assert(take || _readStream.Position == itemPosition);
            return true;
        }


        /// <summary>
        /// Read signle item from disk and deserialize it
        /// </summary>
        /// <param name="itemInfo">Taken item</param>
        /// <param name="buffer">Buffer</param>
        /// <param name="canNewStateBeObserved">Can we meet item in New state (True - stop read before that item, False - read it and throw exception)</param>
        /// <param name="take">True = take, False = peek</param>
        /// <returns>Success or not</returns>
        private bool TryTakeOrPeekItemFromDisk(out ItemReadInfo itemInfo, RegionBinaryReader buffer, bool canNewStateBeObserved, bool take)
        {
            Debug.Assert(buffer != null);
            Debug.Assert(Monitor.IsEntered(_readLock));

            ItemHeader header = default(ItemHeader);
            long itemPosition = 0;

            buffer.BaseStream.SetOriginLength(0, -1);

            if (!TryTakeOrPeekSingleItemBytesFromDisk(buffer.BaseStream.InnerStream, out header, out itemPosition, canNewStateBeObserved, take)) // Read from disk
            {
                itemInfo = default(ItemReadInfo);
                return false;
            }

            Debug.Assert(header.State == ItemState.Written);

            int checkSum = ItemHeader.CoerceChecksum(CalculateChecksum(buffer.BaseStream.InnerStream.GetBuffer(), ItemHeader.Size, header.Length));
            if (checkSum != header.Checksum)
                throw new ItemCorruptedException($"Checksum mismatch on item read. That indicates that the file is corrupted ({_fileName}). To fix the problem you can use '{nameof(ScanSegment)}' method.");

            buffer.BaseStream.SetOriginLength(ItemHeader.Size, header.Length);
            Debug.Assert(buffer.BaseStream.Length == header.Length);

            var item = _serializer.Deserialize(buffer); // Deserialize
            itemInfo = new ItemReadInfo(item, itemPosition);
            return true;
        }


        /// <summary>
        /// Marks item as 'Read' on disk
        /// </summary>
        private void MarkItemAsRead(ref ItemReadInfo itemInfo)
        {
            Debug.Assert(itemInfo.Position > 0);

            lock (_readMarkerLock)
            {
                _readMarkerStream.Position = itemInfo.Position + ItemHeader.OffsetToStateByte;
                _readMarkerStream.WriteByte((byte)ItemState.Read);

                int markedAsReadItemCount = Interlocked.Increment(ref _operationsToFlushCount);
                _readMarkerStream.Flush(flushToDisk: _flushToDiskOnItem > 0 && (markedAsReadItemCount % _flushToDiskOnItem) == 0); // Flush to disk periodically
            }
        }

        /// <summary>
        /// Invalidate read buffer
        /// </summary>
        private void InvalidateReadBuffer()
        {
            Debug.Assert(Monitor.IsEntered(_readLock));
            _readStream.Flush(false); // This invalidate read buffer
        }


        /// <summary>
        /// Take or peek item inside readLock
        /// Steps:
        /// - Reads item from disk
        /// - Reads item from disk in exclusive mode (lock on _writeLock)
        /// </summary>
        private bool TryTakeOrPeek(out ItemReadInfo itemInfo, bool take)
        {
            lock (_readLock)
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(this.GetType().Name);

                RegionBinaryReader memoryBuffer = GetMemoryReadStream();
                Debug.Assert(memoryBuffer.BaseStream.Length == 0);
                Debug.Assert(memoryBuffer.BaseStream.InnerStream.Length == 0);

                try
                {
                    if (TryTakeOrPeekItemFromDisk(out itemInfo, memoryBuffer, canNewStateBeObserved: true, take: take))
                        return true;

                    // Should enter write lock to observe fully saved items
                    lock (_writeLock)
                    {
                        InvalidateReadBuffer(); // Should invalidate as states can be rewritten in parallel
                        // Retry read from disk
                        if (TryTakeOrPeekItemFromDisk(out itemInfo, memoryBuffer, canNewStateBeObserved: false, take: take))
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
        /// Helper method to take or peek item from _readBuffer
        /// </summary>
        private bool TryTakeOrPeekFromReadBuffer(out ItemReadInfo itemInfo, bool take)
        {
            Debug.Assert(_readBuffer != null);
            if (take)
                return _readBuffer.TryDequeue(out itemInfo);
            return _readBuffer.TryPeek(out itemInfo);
        }


        /// <summary>
        /// Take or peek item inside readLock through read buffer. Also populate readBuffer with items
        /// Steps:
        /// - Reads item from readBuffer;
        /// - Reads items from disk
        /// - Reads items from disk in exclusive mode (lock on _writeLock)
        /// </summary>
        private bool TryTakeOrPeekThroughReadBuffer(out ItemReadInfo itemInfo, bool take)
        {
            Debug.Assert(_readBuffer != null);
            Debug.Assert(_maxReadBufferSize > 0);

            lock (_readLock)
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(this.GetType().Name);

                // retry read from buffer
                if (TryTakeOrPeekFromReadBuffer(out itemInfo, take))
                    return true;

                // Read buffer is empty => should read from disk
                RegionBinaryReader memoryBuffer = GetMemoryReadStream();
                Debug.Assert(memoryBuffer.BaseStream.Length == 0);
                Debug.Assert(memoryBuffer.BaseStream.InnerStream.Length == 0);

                try
                {
                    int itemTransfered = 0;
                    ItemReadInfo tmpItem = default(ItemReadInfo);
                    while (itemTransfered < _maxReadBufferSize && TryTakeOrPeekItemFromDisk(out tmpItem, memoryBuffer, canNewStateBeObserved: true, take: true)) // take = true as we transfer items to buffer
                    {
                        if (itemTransfered == 0)
                            itemInfo = tmpItem;

                        if (itemTransfered > 0 || !take) // First item should always be ours
                            _readBuffer.Enqueue(tmpItem);

                        itemTransfered++;
                    }

                    if (itemTransfered < _maxReadBufferSize)
                    {
                        // Should enter write lock to observe fully saved items
                        lock (_writeLock)
                        {
                            InvalidateReadBuffer(); // Should invalidate as states can be rewritten in parallel

                            // Retry read from disk
                            while (itemTransfered < _maxReadBufferSize && TryTakeOrPeekItemFromDisk(out tmpItem, memoryBuffer, canNewStateBeObserved: false, take: true)) // take = true as we transfer items to buffer
                            {
                                if (itemTransfered == 0)
                                    itemInfo = tmpItem;

                                if (itemTransfered > 0 || !take) // First item should always be ours
                                    _readBuffer.Enqueue(tmpItem);

                                itemTransfered++;
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
        /// Removes item from the head of the segment (core implementation)
        /// </summary>
        /// <param name="item">The item removed from segment</param>
        /// <returns>True if the item was removed</returns>
        protected override bool TryTakeCore(out T item)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(this.GetType().Name);

            ItemReadInfo itemInfo = default(ItemReadInfo);
            bool result = false;

            if (_readBuffer != null)
            {
                // Read from buffer first
                result = _readBuffer.TryDequeue(out itemInfo);
                if (!result)
                    result = TryTakeOrPeekThroughReadBuffer(out itemInfo, take: true);
            }
            else
            {
                result = TryTakeOrPeek(out itemInfo, take: true);
            }

            if (result)
            {
                item = itemInfo.Item;
                MarkItemAsRead(ref itemInfo);
                return true;
            }

            item = default(T);
            return false;
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

            ItemReadInfo itemInfo = default(ItemReadInfo);
            bool result = false;

            if (_readBuffer != null)
            {
                // Read from buffer first
                result = _readBuffer.TryPeek(out itemInfo);
                if (!result)
                    result = TryTakeOrPeekThroughReadBuffer(out itemInfo, take: false);
            }
            else
            {
                result = TryTakeOrPeek(out itemInfo, take: false);
            }

            if (result)
            {
                item = itemInfo.Item;
                return true;
            }

            item = default(T);
            return false;
        }


        // ===============================

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
                _readMarkerStream.Dispose();
                _writeStream.Dispose();

                if (disposeBehaviour == DiskQueueSegmentDisposeBehaviour.Delete)
                {
                    Debug.Assert(this.Count == 0);
                    File.Delete(_fileName);
                }
            }
        }
    }
}
