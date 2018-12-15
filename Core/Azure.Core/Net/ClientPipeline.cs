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
    public class ClientOptions
    {
        static readonly PipelinePolicy s_defaultLoggingPolicy = new LoggingPolicy();
        static readonly PipelinePolicy s_defaultRetryPolicy = new RetryPolicy();
        static readonly PipelineTransport s_defaultTransport = new HttpPipelineTransport();
        static readonly ServiceLogger s_defaultLogger = new NullLogger();

        public ArrayPool<byte> Pool { get; set; } = ArrayPool<byte>.Shared;

        public ServiceLogger Logger { get; set; } = new NullLogger();

        public PipelineTransport Transport { get; set; } = s_defaultTransport;

        public PipelinePolicy LoggingPolicy { get; set; } = s_defaultLoggingPolicy;

        public PipelinePolicy RetryPolicy { get; set; } = s_defaultRetryPolicy;

        public string ApplicationId { get; set; }

        public ClientPipeline Create(string sdkName, string sdkVersion)
        {
            var ua = Header.Common.CreateUserAgent(sdkName, sdkVersion, ApplicationId);

            var pipeline = new ClientPipeline(
                Transport,
                LoggingPolicy,
                RetryPolicy,
                new TelemetryPolicy(ua)
            );
            return pipeline;
        }

        #region nobody wants to see these
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj) => base.Equals(obj);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode() => base.GetHashCode();

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override string ToString() => base.ToString();
        #endregion
    }

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

        public PipelineTransport Transport {
            get => (PipelineTransport)_pipeline[_pipeline.Length - 1];
            set {
                _pipeline[_pipeline.Length - 1] = value;
            }
        }

        public ClientPipeline(PipelineTransport transport)
        {
            _pipeline = new PipelinePolicy[4];
            _pipeline[_pipeline.Length - 1] = transport;
            _pipelineCount = 1;
        }

        public ClientPipeline(PipelineTransport transport, params PipelinePolicy[] policies)
            : this(transport)
        {
            foreach (var policy in policies) AddPolicy(policy);
        }

        public PipelinePolicy AddPolicy(PipelinePolicy policy)
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

        public PipelineCallContext CreateContext(ClientOptions options, CancellationToken cancellation, ServiceMethod method, Url url)
            => Transport.CreateContext(ref options, cancellation, method, url);

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

        public abstract PipelineCallContext CreateContext(ref ClientOptions clinet, CancellationToken cancellation, ServiceMethod method, Url url);

        public sealed override async Task ProcessAsync(PipelineCallContext context, ReadOnlyMemory<PipelinePolicy> next)
        {
            if (next.Length == 0) await ProcessAsync(context).ConfigureAwait(false);
            else throw new ArgumentOutOfRangeException(nameof(next));
        }
    }
}

