using Azure.Core.Buffers;
using Azure.Core.Diagnostics;
using Azure.Core.Net;
using Azure.Core.Net.Pipeline;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.Core.Testing
{
    public class MockLogger : ServiceLogger
    {
        public readonly List<string> Logged = new List<string>();
        public TraceLevel Level = TraceLevel.Verbose;
        public override bool IsEnabledFor(TraceLevel level) => Level >= level;

        public override void Log(string message, TraceLevel level = TraceLevel.Info)
            => Logged.Add($"{level} : {message}");
    }

    public class MockTransport : PipelineTransport
    {
        int[] _statusCodes;
        int _index;

        public MockTransport(params int[] statusCodes)
            => _statusCodes = statusCodes;

        public override PipelineCallContext CreateContext(ref PipelineOptions options, CancellationToken cancellation, ServiceMethod method, Uri url)
            => new Context(ref options, cancellation, method, url);

        public override Task ProcessAsync(PipelineCallContext context)
        {
            var mockContext = context as Context;
            if (mockContext == null) throw new InvalidOperationException("the context is not compatible with the transport");

            mockContext.SetStatus(_statusCodes[_index++]);
            if (_index >= _statusCodes.Length) _index = 0;
            return Task.CompletedTask;
        }

        class Context : PipelineCallContext
        {
            string _uri;
            int _status;
            ServiceMethod _method;

            protected override int Status => _status;

            protected override ReadOnlySequence<byte> ResponseContent => throw new NotImplementedException();

            protected override Stream ResponseStream => throw new NotImplementedException();

            public Context(ref PipelineOptions client, CancellationToken cancellation, ServiceMethod method, Uri uri)
                : base(uri, cancellation)
                => SetRequestLine(method, uri);

            protected override Task<ReadOnlySequence<byte>> ReadContentAsync(long minimumLength)
                => Task.FromResult(ReadOnlySequence<byte>.Empty);

            public void SetStatus(int status) => _status = status;

            void SetRequestLine(ServiceMethod method, Uri uri)
            {
                _uri = uri.ToString();
                _method = method;
            }

            public override string ToString()
                => $"{_method} {_uri}";

            protected override bool TryGetHeader(ReadOnlySpan<byte> name, out ReadOnlySpan<byte> value)
            {
                value = default;
                return false;
            }

            public override void AddHeader(Header header)
            {
            }

            protected override void DisposeResponseContent(long bytes)
            {
            }

            public override void AddContent(PipelineContent content)
            {
                throw new NotImplementedException();
            }
        }
    }
}
