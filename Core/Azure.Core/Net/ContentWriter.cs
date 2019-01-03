using System;
using System.Buffers;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;

namespace Azure.Core.Net
{
    public readonly struct ContentWriter : IBufferWriter<byte>
    {
        readonly PipelineCallContext _context;

        internal ContentWriter(PipelineCallContext context) => _context = context;

        public void WriteFrom(Stream stream)
        {
            if (_context.RequestContentSource != null) throw new InvalidOperationException("only one stream can be pumped");
            _context.RequestContentSource = stream;
        }

        public void Advance(int bytes) => _context.CommitRequestBuffer(bytes);

        public Memory<byte> GetMemory(int sizeHint = 0) => _context.GetRequestBuffer(sizeHint);

        public Span<byte> GetSpan(int sizeHint = 0) => GetMemory(sizeHint).Span; // TODO (pri2): should there be a way to get span directly?

        public ReadOnlySequence<byte> Written => _context.RequestContent;

        public async Task FlushAsync() => await _context.FlushAsync().ConfigureAwait(false);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj) => base.Equals(obj);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode() => base.GetHashCode();

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override string ToString() => base.ToString();
    }
}
