using Azure.Core.Diagnostics;
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

        protected PipelineCallContext(Uri uri, CancellationToken cancellation)
        {
            Uri = uri;
            Cancellation = cancellation;
            Logger = new NullLogger();
        }

        // request
        public readonly Uri Uri;

        public abstract void AddHeader(Header header);

        public virtual void AddHeader(string name, string value)
            => AddHeader(new Header(name, value));

        public abstract void AddContent(PipelineContent content);

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

    public abstract class PipelineContent : IDisposable
    {
        public static PipelineContent Create(Stream stream) => new StreamContent(stream);

        public abstract Task WriteTo(Stream stream);

        public abstract bool TryComputeLength(out long length);

        public abstract void Dispose();

        class StreamContent : PipelineContent
        {
            Stream _stream;

            public StreamContent(Stream stream)
            {
                if (!stream.CanSeek) throw new ArgumentException("stream must be seekable", nameof(stream));
                _stream = stream;
            }

            public sealed override bool TryComputeLength(out long length)
            {
                if (_stream.CanSeek)
                {
                    length = _stream.Length;
                    return true;
                }
                length = 0;
                return false;
            }

            public sealed async override Task WriteTo(Stream stream)
            {
                _stream.Seek(0, SeekOrigin.Begin);
                await _stream.CopyToAsync(stream);
                await stream.FlushAsync();
            }

            public override void Dispose()
            {
                _stream?.Dispose();
                _stream = null;
            }
        }
    }
}
