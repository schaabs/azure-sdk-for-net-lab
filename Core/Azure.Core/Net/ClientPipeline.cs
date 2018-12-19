using Azure.Core.Diagnostics;
using Azure.Core.Net.Pipeline;
using System;
using System.Buffers;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.Core.Net
{
    public struct ClientPipeline
    {
        PipelinePolicy[] _pipeline;
        int _pipelineCount;

        ReadOnlyMemory<PipelinePolicy> Pipeline {
            get {
                var index = _pipeline.Length - _pipelineCount;
                return _pipeline.AsMemory(index, _pipelineCount);
            }
        }

        PipelineTransport Transport {
            get => (PipelineTransport)_pipeline[_pipeline.Length - 1];
            set {
                _pipeline[_pipeline.Length - 1] = value;
            }
        }

        internal ClientPipeline(PipelineTransport transport)
        {
            _pipeline = new PipelinePolicy[4];
            _pipeline[_pipeline.Length - 1] = transport;
            _pipelineCount = 1;
        }

        internal ClientPipeline(PipelineTransport transport, params PipelinePolicy[] policies)
            : this(transport)
        {
            foreach (var policy in policies) AddPolicy(policy);
        }

        internal PipelinePolicy AddPolicy(PipelinePolicy policy)
        {
            var index = _pipeline.Length - _pipelineCount - 1;
            if (index < 0)
            {
                var larger = new PipelinePolicy[_pipeline.Length * 2];
                Array.Copy(_pipeline, 0, larger, _pipeline.Length, _pipeline.Length);
                _pipeline = larger;
                index = _pipeline.Length - _pipelineCount - 1;
            }
            _pipeline[index] = policy;
            _pipelineCount++;
            return policy;
        }

        public static ClientPipeline Create(PipelineOptions options, string sdkName, string sdkVersion)
        {
            var ua = Header.Common.CreateUserAgent(sdkName, sdkVersion, options.ApplicationId);

            var pipeline = new ClientPipeline(
                options.Transport,
                options.LoggingPolicy,
                options.RetryPolicy,
                new TelemetryPolicy(ua)
            );
            return pipeline;
        }

        public PipelineCallContext CreateContext(PipelineOptions options, CancellationToken cancellation, ServiceMethod method, Uri uri)
            => Transport.CreateContext(ref options, cancellation, method, uri);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task ProcessAsync(PipelineCallContext context)
            => await PipelinePolicy.ProcessNextAsync(Pipeline, context).ConfigureAwait(false);
    }
}

