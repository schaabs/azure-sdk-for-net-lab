using Azure.Core.Buffers;
using System;
using System.Buffers;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.Core.Net
{
    class ReadOnlySequenceContent : HttpContent
    {
        public static readonly ReadOnlySequenceContent Empty = new ReadOnlySequenceContent(ReadOnlySequence<byte>.Empty, CancellationToken.None);

        ReadOnlySequence<byte> _sequence;
        CancellationToken _cancel;

        public ReadOnlySequenceContent(ReadOnlySequence<byte> sequence, CancellationToken cancel)
        {
            _sequence = sequence;
            _cancel = cancel;
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
            => await stream.WriteAsync(_sequence, _cancel).ConfigureAwait(false);

        protected override bool TryComputeLength(out long length)
        {
            length = _sequence.Length;
            return true;
        }

        public override string ToString()
            => Encoding.UTF8.GetString(_sequence.Slice(Math.Min(1000, _sequence.Length)).ToArray()) + "...";
    }
}
