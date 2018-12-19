using Azure.Core.Diagnostics;
using Azure.Core.Net.Pipeline;
using System;
using System.Buffers;
using System.Buffers.Text;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Buffers.Text.Encodings;

namespace Azure.Core.Net
{
    public abstract class PipelineCallContext  : IDisposable
    {
        internal OptionsStore _options = new OptionsStore();

        public CancellationToken Cancellation { get; }

        public ServiceLogger Logger { get; set; }

        public PipelineCallOptions Options => new PipelineCallOptions(this);

        protected PipelineCallContext(Url url, CancellationToken cancellation)
        {
            Url = url;
            Cancellation = cancellation;
            Logger = new NullLogger();
        }

        // request
        public readonly Url Url;

        public abstract void AddHeader(Header header);

        public virtual void AddHeader(string name, string value)
            => AddHeader(new Header(name, value));

        public ContentWriter ContentWriter => new ContentWriter(this);

        // TODO (pri 0): this should not be here. It cannot be supported for some streams.
        protected internal abstract ReadOnlySequence<byte> RequestContent { get; }

        internal Stream RequestContentSource { get; set; }

        protected internal abstract Memory<byte> GetRequestBuffer(int minimumSize);
        protected internal abstract void CommitRequestBuffer(int size);
        protected internal abstract Task FlushAsync();

        // response
        public ServiceResponse Response => new ServiceResponse(this);

        // make many of these protected internal
        protected internal abstract int Status { get; }

        protected internal abstract bool TryGetHeader(ReadOnlySpan<byte> name, out ReadOnlySpan<byte> value);

        protected internal abstract Task<ReadOnlySequence<byte>> ReadContentAsync(long minimumLength = 0);

        protected internal abstract void DisposeResponseContent(long bytes);

        protected internal abstract ReadOnlySequence<byte> ResponseContent { get; }

        protected internal abstract Stream ResponseStream { get; }

        public virtual void Dispose() => _options.Clear();

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj) => base.Equals(obj);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode() => base.GetHashCode();

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override string ToString() => base.ToString();
    }

    public readonly struct ServiceResponse
    {
        readonly PipelineCallContext _context;

        public ServiceResponse(PipelineCallContext context)
            => _context = context;

        public int Status => _context.Status;

        public ContentReader Content => new ContentReader(_context);
        
        public bool TryGetHeader(ReadOnlySpan<byte> name, out long value)
        {
            value = default;
            if (!TryGetHeader(name, out ReadOnlySpan<byte> bytes)) return false;
            if (!Utf8Parser.TryParse(bytes, out value, out int consumed) || consumed != bytes.Length)
                throw new Exception("bad content-length value");
            return true;
        }

        public bool TryGetHeader(ReadOnlySpan<byte> name, out ReadOnlySpan<byte> value)
            => _context.TryGetHeader(name, out value);

        public bool TryGetHeader(ReadOnlySpan<byte> name, out string value)
        {
            if(TryGetHeader(name, out ReadOnlySpan<byte> span)) {
                value = Utf8.ToString(span);
                return true;
            }
            value = default;
            return false;
        }

        public void Dispose() => _context.Dispose();

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj) => base.Equals(obj);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode() => base.GetHashCode();

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override string ToString()
        {
            var contentText = Encoding.UTF8.GetString(_context.ResponseContent.ToArray());
            return $"{Status} {contentText}";
        }
    }
}
