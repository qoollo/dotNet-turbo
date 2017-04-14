using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Queues.DiskQueueComponents
{
    /// <summary>
    /// BinaryWriter that stores typed reference to RegionMemoryStream
    /// </summary>
    internal class RegionBinaryWriter: BinaryWriter
    {
        private readonly RegionMemoryStream _stream;
        public RegionBinaryWriter(RegionMemoryStream stream)
            : base(stream)
        {
            _stream = stream;
        }
        public RegionBinaryWriter(int capacity)
            : this(new RegionMemoryStream(capacity))
        {
        }

        /// <summary>
        /// Gets the underlying stream
        /// </summary>
        internal new RegionMemoryStream BaseStream { get { return _stream; } }
    }
}
