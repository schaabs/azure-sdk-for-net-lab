using Azure.Core.Diagnostics;
using System;
using System.Buffers;
using System.Buffers.Text;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.Core.Net
{
    public abstract class ServiceCallContext  : IDisposable
    {
        internal OptionsStore _options = new OptionsStore();

        public CancellationToken Cancellation { get; }

        public ServiceLogger Logger { get; set; }

        public ContextOptions Options => new ContextOptions(this);

        public ServiceCallContext(Url url, CancellationToken cancellation, ServiceLogger logger)
        {
            Url = url;
            Cancellation = cancellation;
            Logger = logger;
        }

        // request
        public readonly Url Url;

        public abstract void AddHeader(Header header);

        public ContentWriter ContentWriter => new ContentWriter(this);

        protected internal abstract Memory<byte> GetRequestBuffer(int minimumSize);
        protected internal abstract void CommitRequestBuffer(int size);
        protected internal abstract Task FlushAsync();

        // response
        public ServiceResponse Response => new ServiceResponse(this);

        // make many of these protected internal
        protected internal abstract int Status { get; }

        protected internal abstract bool TryGetHeader(ReadOnlySpan<byte> name, out ReadOnlySpan<byte> value);

        protected internal abstract Task ReadContentAsync(long minimumLength = 0);

        protected internal abstract void DisposeResponseContent(long bytes);

        protected internal abstract ReadOnlySequence<byte> Content { get; }

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
        readonly ServiceCallContext _context;

        public ServiceResponse(ServiceCallContext context)
            => _context = context;

        public int Status => _context.Status;

        public ReadOnlySequence<byte> Content => _context.Content;

        public async Task ReadContentAsync(long minimumLength = 0) => await _context.ReadContentAsync(minimumLength).ConfigureAwait(false);

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

        public void Dispose() => _context.Dispose();

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj) => base.Equals(obj);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode() => base.GetHashCode();

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override string ToString() => base.ToString();
    }

    public readonly struct ContextOptions
    {
        readonly ServiceCallContext _context;

        public ContextOptions(ServiceCallContext context)
            => _context = context;

        public void SetOption(object key, long value)
            => _context._options.SetOption(key, value);

        public void SetOption(object key, object value)
            => _context._options.SetOption(key, value);

        public bool TryGetOption(object key, out object value)
            => _context._options.TryGetOption(key, out value);

        public bool TryGetOption(object key, out long value)
            => _context._options.TryGetOption(key, out value);

        public long GetInt64(object key)
            => _context._options.GetInt64(key);

        public object GetObject(object key)
            => _context._options.GetInt64(key);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj) => base.Equals(obj);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode() => base.GetHashCode();

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override string ToString() => base.ToString();
    }

    public struct ContentReader
    {
        ServiceCallContext _context;

        public ContentReader(ServiceCallContext context) => _context = context;

        public async Task<ReadOnlySequence<byte>> ReadAsync(long minBytes = 0)
        {
            await _context.ReadContentAsync(minBytes).ConfigureAwait(false);
            return _context.Content;
        }

        public void Advance(long bytes) => _context.DisposeResponseContent(bytes);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj) => base.Equals(obj);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode() => base.GetHashCode();

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override string ToString() => base.ToString();
    }

    // TODO (pri 2): does this need FlushAsync?
    public struct ContentWriter : IBufferWriter<byte>
    {
        ServiceCallContext _context;

        public ContentWriter(ServiceCallContext context) => _context = context;

        public void Advance(int bytes) => _context.CommitRequestBuffer(bytes);

        public Memory<byte> GetMemory(int sizeHint = 0) => _context.GetRequestBuffer(sizeHint);

        public Span<byte> GetSpan(int sizeHint = 0) => GetMemory(sizeHint).Span;

        public async Task FlushAsync() => await _context.FlushAsync().ConfigureAwait(false);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj) => base.Equals(obj);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode() => base.GetHashCode();

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override string ToString() => base.ToString();
    }

    public enum ServiceMethod : byte
    {
        Get,
        Post,
        Put
    }

    public enum ServiceProtocol : byte
    {
        Http,
        Https,
        Other,
    }
}
