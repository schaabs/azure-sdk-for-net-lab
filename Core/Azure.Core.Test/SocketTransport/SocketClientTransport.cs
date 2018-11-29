using Azure.Core.Buffers;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using static System.Buffers.Text.Encodings;

namespace Azure.Core.Net
{
    public class SocketClientTransport : ServiceTransport
    {
        public override ServiceCallContext CreateContext(ServicePipeline client, CancellationToken cancellation, ServiceMethod method, Url url)
            => new SocketClientContext(ref client, cancellation, url, method);

        public override async Task ProcessAsync(ServiceCallContext context)
        {
            var socketTransportContext = context as SocketClientContext;
            if (socketTransportContext == null) throw new InvalidOperationException("the context is not compatible with the transport");
            await socketTransportContext.ProcessAsync().ConfigureAwait(false);
        }

        protected class SocketClientContext : ServiceCallContext
        {
            // TODO (pri 3): refactor connection cache and GC it.
            static readonly Dictionary<string, (Socket Client, SslStream Stream)> s_cache = new Dictionary<string, (Socket client, SslStream stream)>();

            Socket _socket;
            SslStream _sslStream;

            Sequence<byte> _requestBuffer;
            Sequence<byte> _contentBuffer;
            int _statusCode;
            int _headersStart;
            int _contentStart;
            bool _endOfHeadersWritten = false;

            public SocketClientContext(ref ServicePipeline client, CancellationToken cancellation, Url url, ServiceMethod method)
                : base(url, cancellation, client.Logger)
            {
                _contentBuffer = new Sequence<byte>(client.Pool);
                _requestBuffer = new Sequence<byte>(client.Pool);

                var (_, host, path) = url;
                Http.WriteRequestLine(ref _requestBuffer, ServiceProtocol.Https, method, path);
                AddHeader(Header.CreateHost(host));
            }

            protected virtual async Task<Sequence<byte>> ReceiveAsync(Sequence<byte> buffer)
                => await _sslStream.ReadAsync(buffer, Cancellation).ConfigureAwait(false);

            public async Task ProcessAsync()
            {
                // this is needed so the retry does not add this again
                if(!_endOfHeadersWritten) AddEndOfHeaders();
                var merged = Sequence<byte>.Merge(ref _requestBuffer, ref _contentBuffer);
                await SendAsync(merged.AsReadOnly()).ConfigureAwait(false);
                merged.Dispose();
     
                while (true)
                {
                    _contentBuffer = await ReceiveAsync(_contentBuffer).ConfigureAwait(false);
                    OperationStatus result = Http.ParseResponse(_contentBuffer, out _statusCode, out _headersStart, out _contentStart);
                    if (result == OperationStatus.Done) break;
                    if (result == OperationStatus.NeedMoreData) continue;
                    throw new Exception("invalid response");
                }
            }

            protected override async Task<ReadOnlySequence<byte>> ReadContentAsync(long minimumLength)
            {
                while (true)
                {
                    var length = _contentBuffer.Length - _contentStart;
                    if (length >= minimumLength) return ResponseContent;
                    _contentBuffer = await ReceiveAsync(_contentBuffer).ConfigureAwait(false);
                }
            }

            protected virtual async Task SendAsync(ReadOnlySequence<byte> buffer)
            {
                if (_socket == null) // i.e. this is not a retry  
                {             
                    string host = Utf8.ToString(Url.Host);
                    if (s_cache.TryGetValue(host, out var connection))
                    {
                        // TODO (pri 1): this needs to use a real pool and take the connection out, as of now it's very not thread safe
                        _socket = connection.Client;
                        _sslStream = connection.Stream;
                    }
                    else
                    {
                        _socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                        await _socket.ConnectAsync(host, 443).ConfigureAwait(false);
                        var ns = new NetworkStream(_socket);
                        _sslStream = new SslStream(ns, false, new RemoteCertificateValidationCallback(
                            (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) => sslPolicyErrors == SslPolicyErrors.None
                        ));
                        await _sslStream.AuthenticateAsClientAsync(host).ConfigureAwait(false);
                        s_cache.Add(host, (_socket, _sslStream));
                    }
                }
                await _sslStream.WriteAsync(buffer, Cancellation).ConfigureAwait(false);
            }

            public sealed override void AddHeader(Header header)
            {
                if (_endOfHeadersWritten) throw new NotImplementedException("need to shift EOH");

                Span<byte> span = _requestBuffer.GetSpan();
                while (true)
                {
                    if (header.TryWrite(span, out var written))
                    {
                        _requestBuffer.Advance(written);
                        return;
                    }
                    span = _requestBuffer.GetSpan(span.Length * 2);
                }
            }

            protected override void DisposeResponseContent(long bytes)
            {
                // TODO (pri 2): this should dispose already read segments
            }

            public void AddEndOfHeaders()
            {
                var buffer = _requestBuffer.GetSpan(Http.CRLF.Length);
                Http.CRLF.CopyTo(buffer);
                _requestBuffer.Advance(Http.CRLF.Length);
                _endOfHeadersWritten = true;
            }

            ReadOnlySequence<byte> Headers => _contentBuffer.AsReadOnly().Slice(_headersStart, _contentStart - Http.CRLF.Length);
            protected override ReadOnlySequence<byte> ResponseContent => _contentBuffer.AsReadOnly().Slice(_contentStart);

            protected override int Status => _statusCode;

            protected override ReadOnlySequence<byte> RequestContent => _contentBuffer.AsReadOnly();

            protected override bool TryGetHeader(ReadOnlySpan<byte> name, out ReadOnlySpan<byte> value)
            {
                var headers = Headers;
                if (!headers.IsSingleSegment) throw new NotImplementedException();
                var span = headers.First.Span;
                int index = span.IndexOf(name);
                if (index < 0)
                {
                    value = default;
                    return false;
                }
                var header = span.Slice(index);
                var headerEnd = header.IndexOf(Http.CRLF);
                var headerStart = header.IndexOf((byte)':') + 1;
                while (header[headerStart] == ' ') headerStart++;
                value = header.Slice(headerStart, headerEnd - headerStart);
                return true;
            }

            protected sealed override Memory<byte> GetRequestBuffer(int minimumSize)
                => _contentBuffer.GetMemory(minimumSize);

            protected sealed override void CommitRequestBuffer(int size) => _contentBuffer.Advance(size);

            public sealed override void Dispose()
            {
                _requestBuffer.Dispose();
                _contentBuffer.Dispose();
                _sslStream = null;
                _socket = null;
                base.Dispose();
            }

            public sealed override string ToString() => _contentBuffer.ToString();

            protected override Task FlushAsync()
            {
                throw new NotImplementedException();
            }
        }
    }

    public class MockSocketTransport : SocketClientTransport
    {
        byte[][] _responses;

        public MockSocketTransport(params byte[][] responses) => _responses = responses;

        public override ServiceCallContext CreateContext(ServicePipeline client, CancellationToken cancellation, ServiceMethod method, Url url)
            => new MockSocketContext(ref client, cancellation, method, url, _responses);

        class MockSocketContext : SocketClientContext
        {
            byte[][] _responses;
            int _responseNumber;

            public MockSocketContext(ref ServicePipeline client, CancellationToken cancellation, ServiceMethod method, Url url, byte[][] responses)
                : base(ref client, cancellation, url, method)
            {
                _responses = responses;
            }

            protected override Task<Sequence<byte>> ReceiveAsync(Sequence<byte> buffer)
            {
                var response = _responses[_responseNumber++];
                if (_responseNumber >= _responses.Length) _responseNumber = 0;
                var segment = buffer.GetMemory(response.Length);
                response.CopyTo(segment);
                buffer.Advance(response.Length);
                return Task.FromResult(buffer);
            }

            protected override Task SendAsync(ReadOnlySequence<byte> buffer)
                => Task.CompletedTask;
        }
    }
}
