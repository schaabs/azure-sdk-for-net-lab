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

        public Stream Stream => _context.ResponseStream;

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj) => base.Equals(obj);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode() => base.GetHashCode();

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override string ToString() => base.ToString();
    }
}
