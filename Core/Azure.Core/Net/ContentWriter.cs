using System;
using System.Buffers;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Azure.Core.Net
{
    public struct ContentWriter : IBufferWriter<byte>
    {
        ServiceCallContext _context;

        public ContentWriter(ServiceCallContext context) => _context = context;

        public void Advance(int bytes) => _context.CommitRequestBuffer(bytes);

        public Memory<byte> GetMemory(int sizeHint = 0) => _context.GetRequestBuffer(sizeHint);

        public Span<byte> GetSpan(int sizeHint = 0) => GetMemory(sizeHint).Span;

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
