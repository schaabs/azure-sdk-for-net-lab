using Azure.Core.Diagnostics;
using Azure.Core.Net.Pipeline;
using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.Core.Net
{
    public class ClientPipeline
    {
        PipelinePolicy[] _pipeline;
        int _pipelineCount;
        ReadOnlyMemory<PipelinePolicy> Pipeline {
            get {
                var index = _pipeline.Length - _pipelineCount;
                return _pipeline.AsMemory(index, _pipelineCount);
            }
        }

        public PipelineTransport Transport {
            get => (PipelineTransport)_pipeline[_pipeline.Length - 1];
            set {
                _pipeline[_pipeline.Length - 1] = value;
            }
        }

        public ArrayPool<byte> Pool { get; set; }

        public ServiceLogger Logger { get; set; }

        public ClientPipeline(PipelineTransport transport)
        {
            _pipeline = new PipelinePolicy[4];
            _pipeline[_pipeline.Length - 1] = transport;
            _pipelineCount = 1;
            Logger = ServiceLogger.NullLogger;
            Pool = ArrayPool<byte>.Shared;
        }

        public ClientPipeline(PipelineTransport transport, params PipelinePolicy[] policies)
            : this(transport)
        {
            foreach (var policy in policies) Add(policy);
        }

        public static ClientPipeline Create(string sdkName, string sdkVersion, HttpClientTransport transport = null)
        {
            if (transport == null) transport = new HttpClientTransport();
            var pipeline = new ClientPipeline(
                transport,
                new LoggingPolicy(),
                new RetryPolicy(),
                new TelemetryPolicy(sdkName, sdkVersion, null)
            );
            return pipeline;
        }

        public void Add(PipelinePolicy policy)
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
        }

        public PipelineCallContext CreateContext(CancellationToken cancellation, ServiceMethod method, Url url)
            => Transport.CreateContext(this, cancellation, method, url);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task ProcessAsync(PipelineCallContext context)
            => await PipelinePolicy.ProcessNextAsync(Pipeline, context).ConfigureAwait(false);
    }

    public abstract class PipelinePolicy
    {
        public abstract Task ProcessAsync(PipelineCallContext context, ReadOnlyMemory<PipelinePolicy> pipeline);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected internal static async Task ProcessNextAsync(ReadOnlyMemory<PipelinePolicy> pipeline, PipelineCallContext context)
        {
            var next = pipeline.Span[0];
            await next.ProcessAsync(context, pipeline.Slice(1)).ConfigureAwait(false);
        }
    }

    public abstract class PipelineTransport : PipelinePolicy
    {
        public abstract Task ProcessAsync(PipelineCallContext context);

        public abstract PipelineCallContext CreateContext(ClientPipeline clinet, CancellationToken cancellation, ServiceMethod method, Url url);

        public sealed override async Task ProcessAsync(PipelineCallContext context, ReadOnlyMemory<PipelinePolicy> next)
        {
            if (next.Length == 0) await ProcessAsync(context).ConfigureAwait(false);
            else throw new ArgumentOutOfRangeException(nameof(next));
        }
    }
}

