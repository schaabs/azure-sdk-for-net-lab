using Azure.Core.Buffers;
using Azure.Core.Diagnostics;
using Azure.Core.Net;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
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

    public class MockTransport : ServiceTransport
    {
        int[] _statusCodes;
        int _index;

        public MockTransport(params int[] statusCodes)
            => _statusCodes = statusCodes;

        public override ServiceCallContext CreateContext(ref ServicePipeline client, CancellationToken cancellation, ServiceMethod method, Url url)
            => new Context(ref client, cancellation, method, url);

        public override Task ProcessAsync(ServiceCallContext context)
        {
            var mockContext = context as Context;
            if (mockContext == null) throw new InvalidOperationException("the context is not compatible with the transport");

            mockContext.SetStatus(_statusCodes[_index++]);
            if (_index >= _statusCodes.Length) _index = 0;
            return Task.CompletedTask;
        }

        class Context : ServiceCallContext
        {
            string _uri;
            int _status;
            ServiceMethod _method;

            protected override int Status => _status;

            protected override ReadOnlySequence<byte> Content => throw new NotImplementedException();

            public Context(ref ServicePipeline client, CancellationToken cancellation, ServiceMethod method, Url url)
                : base(url, cancellation, client.Logger)
                => SetRequestLine(method, url);

            protected override Task ReadContentAsync(long minimumLength)
                => Task.FromResult(ReadOnlySequence<byte>.Empty);

            public void SetStatus(int status) => _status = status;

            void SetRequestLine(ServiceMethod method, Url url)
            {
                _uri = url.ToString();
                _method = method;
            }

            public override string ToString()
                => $"{_method} {_uri}";

            protected override bool TryGetHeader(ReadOnlySpan<byte> name, out ReadOnlySpan<byte> value)
            {
                throw new NotImplementedException();
            }

            public override void AddHeader(Header header)
            {
                throw new NotImplementedException();
            }

            protected override void DisposeResponseContent(long bytes)
            {
            }

            protected override Memory<byte> GetRequestBuffer(int minimumSize)
            {
                throw new NotImplementedException();
            }

            protected override void CommitRequestBuffer(int size)
            {
                throw new NotImplementedException();
            }

            protected override Task FlushAsync()
            {
                throw new NotImplementedException();
            }
        }
    }
}
