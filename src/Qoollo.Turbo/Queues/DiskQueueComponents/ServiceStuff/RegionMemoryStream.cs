using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Queues.DiskQueueComponents
{
    /// <summary>
    /// Memory stream with limited observation window
    /// </summary>
    internal class RegionMemoryStream: Stream
    {
        private readonly MemoryStream _innerStream;
        private int _origin;
        private int _length;

        public RegionMemoryStream(int capacity)
        {
            _innerStream = new MemoryStream(capacity);
            _origin = 0;
            _length = -1;
        }
        public RegionMemoryStream() : this(0) { }
        public RegionMemoryStream(MemoryStream innerStream, int origin, int length)
        {
            if (innerStream == null)
                throw new ArgumentNullException(nameof(innerStream));
            if (origin < 0)
                throw new ArgumentOutOfRangeException(nameof(origin));
            if (length >= 0 && innerStream.Length < origin + length)
                throw new ArgumentException("Length specifies region that not presented in 'innerStream'", nameof(length));

            _innerStream = innerStream;
            _origin = origin;
            _length = length >= 0 ? length : -1;
            _innerStream.Position = origin;
        }
        public RegionMemoryStream(MemoryStream innerStream, int origin) : this(innerStream, origin, -1) { }
        public RegionMemoryStream(MemoryStream innerStream) : this(innerStream, 0, -1) { }


        /// <summary>
        /// Gets the inner MemoryStream
        /// </summary>
        internal MemoryStream InnerStream { get { return _innerStream; } }
        /// <summary>
        /// Current origin position
        /// </summary>
        internal int Origin { get { return _origin; } }
        /// <summary>
        /// Gets position for inner MemoryStream
        /// </summary>
        internal int InnerStreamPosition { get { return checked((int)_innerStream.Position); } }

        /// <summary>
        /// Gets a value indicating whether the current stream supports reading
        /// </summary>
        public sealed override bool CanRead { get { return true; } }
        /// <summary>
        /// Gets a value indicating whether the current stream supports seeking
        /// </summary>
        public sealed override bool CanSeek { get { return true; } }
        /// <summary>
        /// Gets a value indicating whether the current stream supports writing
        /// </summary>
        public sealed override bool CanWrite { get { return _length < 0; } }
        /// <summary>
        /// Gets the length of the stream in bytes
        /// </summary>
        public sealed override long Length { get { return _length >= 0 ? _length : Math.Max(0, _innerStream.Length - _origin); } }
        /// <summary>
        /// Gets or sets the current position within the stream
        /// </summary>
        public sealed override long Position
        {
            get
            {
                return _innerStream.Position - _origin;
            }
            set
            {
                if (value < 0L || value > int.MaxValue)
                    throw new ArgumentOutOfRangeException(nameof(value));

                _innerStream.Position = value + _origin;
            }
        }

        /// <summary>
        /// Updates origin and length
        /// </summary>
        /// <param name="origin">New origin</param>
        /// <param name="length">New length</param>
        internal void SetOriginLength(int origin, int length)
        {
            if (origin < 0)
                throw new ArgumentOutOfRangeException(nameof(origin));
            if (length >= 0 && _innerStream.Length < origin + length)
                throw new ArgumentException("Length specifies region that not presented in 'innerStream'", nameof(length));

            _origin = origin;
            _length = length >= 0 ? length : -1;
            _innerStream.Position = origin;
        }
        /// <summary>
        /// Updates current origin
        /// </summary>
        /// <param name="origin">New origin</param>
        internal void SetOrigin(int origin)
        {
            SetOriginLength(origin, _length);
        }
        /// <summary>
        /// Updates origin to current position
        /// </summary>
        /// <param name="length">New length</param>
        internal void SetCurrentPositionAsOrigin(int length)
        {
            SetOriginLength((int)_innerStream.Position, length);
        }
        /// <summary>
        /// Updates origin to current position
        /// </summary>
        internal void SetCurrentPositionAsOrigin()
        {
            SetOriginLength((int)_innerStream.Position, _length);
        }


        public override void Flush()
        {
        }
        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return _innerStream.FlushAsync(cancellationToken);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (offset > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(offset));

            long newPos = 0;
            switch (origin)
            {
                case SeekOrigin.Begin:
                    newPos = _origin + offset;
                    break;
                case SeekOrigin.Current:
                    newPos = _innerStream.Position + offset;
                    break;
                case SeekOrigin.End:
                    newPos = _origin + this.Length + offset;
                    break;
                default:
                    throw new ArgumentException("Invalid SeekOrigin value");
            }

            if (newPos < _origin)
                throw new IOException("Seek before begin of the stream");
            return _innerStream.Seek(newPos, SeekOrigin.Begin) - _origin;
        }

        public override void SetLength(long value)
        {
            if (value < 0L || value > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(value));
            if (!CanWrite)
                throw new NotSupportedException("Write is not supported");

            _innerStream.SetLength(_origin + value);
        }

        public override int ReadByte()
        {
            if (_length >= 0 && (int)Position >= _length)
                return -1;

            return base.ReadByte();
        }
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            if (_length >= 0)
            {
                int position = (int)this.Position;
                count = Math.Max(0, Math.Min(position + count, _length) - position);
            }
            return _innerStream.Read(buffer, offset, count);
        }
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            if (_length >= 0)
            {
                int position = (int)this.Position;
                count = Math.Max(0, Math.Min(position + count, _length) - position);
            }

            return _innerStream.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override void WriteByte(byte value)
        {
            if (!CanWrite)
                throw new NotSupportedException("Write is not supported");

            _innerStream.WriteByte(value);
        }
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (!CanWrite)
                throw new NotSupportedException("Write is not supported");

            _innerStream.Write(buffer, offset, count);
        }
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (!CanWrite)
                throw new NotSupportedException("Write is not supported");

            return _innerStream.WriteAsync(buffer, offset, count, cancellationToken);
        }



        /// <summary>
        /// Cleans-up resources
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}
