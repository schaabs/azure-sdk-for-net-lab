using System;
using System.Buffers;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.Core.Net
{
    public readonly struct ContentReader
    {
        readonly PipelineCallContext _context;

        internal ContentReader(PipelineCallContext context) => _context = context;

        public Task<ReadOnlySequence<byte>> ReadAsync(long minBytes = 0)
            => _context.ReadContentAsync(minBytes);

        public ReadOnlySequence<byte> Bytes => _context.ResponseContent;
        public void Advance(long bytes) => _context.DisposeResponseContent(bytes);

        public Stream Stream => new ResponseStream(_context);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj) => base.Equals(obj);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode() => base.GetHashCode();

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override string ToString() => base.ToString();
    }

    // TODO (pri 0): implement stream support for content reader
    class ResponseStream : Stream
    {
        private PipelineCallContext _context;

        public ResponseStream(PipelineCallContext context)
        {
            _context = context;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotImplementedException();

        public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public override void Flush() => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
