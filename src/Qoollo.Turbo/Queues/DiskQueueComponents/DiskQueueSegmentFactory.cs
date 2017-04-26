﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Queues.DiskQueueComponents
{
    /// <summary>
    /// Factory that creates DiskQueueSegments
    /// </summary>
    /// <typeparam name="T">Type of the item stored in DiskQueueSegment</typeparam>
    public abstract class DiskQueueSegmentFactory<T>
    {
        /// <summary>
        /// Information about disk queue segment file
        /// </summary>
        protected struct SegmentFileInfo
        {
            private readonly string _fileName;
            private readonly long _segmentNumber;

            /// <summary>
            /// SegmentFileInfo constructor
            /// </summary>
            /// <param name="fileName">Full file name</param>
            /// <param name="segmentNumber">Segment number</param>
            public SegmentFileInfo(string fileName, long segmentNumber)
            {
                if (fileName == null)
                    throw new ArgumentNullException(nameof(fileName));
                if (segmentNumber <= 0)
                    throw new ArgumentOutOfRangeException(nameof(segmentNumber));

                _fileName = fileName;
                _segmentNumber = segmentNumber;
            }
            /// <summary>
            /// Full file name
            /// </summary>
            public string FileName { get { return _fileName; } }
            /// <summary>
            /// Segment number
            /// </summary>
            public long SegmentNumber { get { return _segmentNumber; } }
        }

        /// <summary>
        /// Generates default file name for disk queue segment ([namePrefix]_[number][extension])
        /// </summary>
        /// <param name="namePrefix">File name prefix (part befor segment number)</param>
        /// <param name="number">Segment number</param>
        /// <param name="extension">File extension (should be prefixed with point symbol)</param>
        /// <returns>Generated file name</returns>
        protected static string GenerateFileName(string namePrefix, long number, string extension)
        {
            if (string.IsNullOrEmpty(namePrefix))
                throw new ArgumentNullException(nameof(namePrefix), "Name prefix should be specified");
            if (number < 0)
                throw new ArgumentOutOfRangeException(nameof(number), "Segment number should be positive");
            if (string.IsNullOrEmpty(extension))
                throw new ArgumentNullException(nameof(extension), "File extension should be specified");
            if (extension[0] != '.')
                throw new ArgumentException("Extension should start from point symbol", nameof(extension));

            return namePrefix + "_" + number.ToString() + extension;
        }
        /// <summary>
        /// Discovers segment files on disk which names was generated by <see cref="GenerateFileName(string, long, string)"/> 
        /// </summary>
        /// <param name="path">Path to directory to scan</param>
        /// <param name="namePrefix">File name prefix (part befor segment number)</param>
        /// <param name="extension">File extension (should be prefixed with point symbol)</param>
        /// <returns>Discovered files</returns>
        protected static SegmentFileInfo[] DiscoverSegmentFiles(string path, string namePrefix, string extension)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));
            if (string.IsNullOrEmpty(namePrefix))
                throw new ArgumentNullException(nameof(namePrefix), "Name prefix should be specified");
            if (string.IsNullOrEmpty(extension))
                throw new ArgumentNullException(nameof(extension), "File extension should be specified");
            if (extension[0] != '.')
                throw new ArgumentException("Extension should start from point symbol", nameof(extension));

            if (!System.IO.Directory.Exists(path))
                return new SegmentFileInfo[0];

            string pattern = namePrefix + "_*" + extension;
            var files = System.IO.Directory.GetFiles(path, pattern);

            List<SegmentFileInfo> fileInfos = new List<SegmentFileInfo>(files.Length);
            string namePrefixWithUnder = namePrefix + "_";
            for (int i = 0; i < files.Length; i++)
            {
                string fileName = System.IO.Path.GetFileName(files[i]);
                if (!fileName.StartsWith(namePrefixWithUnder) || !fileName.EndsWith(extension))
                    continue;

                string numberStr = fileName.Substring(namePrefixWithUnder.Length, fileName.Length - extension.Length - namePrefixWithUnder.Length);
                long number = 0;
                if (!long.TryParse(numberStr, out number))
                    continue;

                Debug.Assert(fileName == GenerateFileName(namePrefix, number, extension));
                fileInfos.Add(new SegmentFileInfo(files[i], number));
            }

            return fileInfos.ToArray();
        }


        // ==================================


        /// <summary>
        /// Capacity of a single segment (informational)
        /// </summary>
        public virtual int SegmentCapacity { get { return -1; } }
        /// <summary>
        /// Creates a new segment
        /// </summary>
        /// <param name="path">Path to the folder where the new segment will be allocated</param>
        /// <param name="number">Number of a segment (should be part of a segment name)</param>
        /// <returns>Created DiskQueueSegment</returns>
        public abstract DiskQueueSegment<T> CreateSegment(string path, long number);
        /// <summary>
        /// Discovers existing segments in specified path
        /// </summary>
        /// <param name="path">Path to the folder for the segments</param>
        /// <returns>Segments loaded from disk (can be empty)</returns>
        public abstract DiskQueueSegment<T>[] DiscoverSegments(string path);
    }
}
