using System.Buffers;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;

namespace Azure.Core.Net
{
    public struct ContentReader
    {
        ServiceCallContext _context;

        public ContentReader(ServiceCallContext context) => _context = context;

        public Task<ReadOnlySequence<byte>> ReadAsync(long minBytes = 0)
            => _context.ReadContentAsync(minBytes);

        public void Advance(long bytes) => _context.DisposeResponseContent(bytes);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj) => base.Equals(obj);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode() => base.GetHashCode();

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override string ToString() => base.ToString();
    }
}
