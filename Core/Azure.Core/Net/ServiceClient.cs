using Azure.Core.Diagnostics;
using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.Core.Net
{
    public class ServicePipeline
    {
        ServicePolicy[] _pipeline;
        int _pipelineCount;

        public ReadOnlyMemory<ServicePolicy> Pipeline {
            get {
                var index = _pipeline.Length - _pipelineCount;
                return _pipeline.AsMemory(index, _pipelineCount);
            }
        }

        public ServiceTransport Transport {
            get => (ServiceTransport)_pipeline[_pipeline.Length - 1];
            set {
                _pipeline[_pipeline.Length - 1] = value;
            }
        }

        public ArrayPool<byte> Pool { get; set; }

        public ServiceLogger Logger { get; set; }

        public ServicePipeline(ServiceTransport transport)
        {
            _pipeline = new ServicePolicy[4];
            _pipeline[_pipeline.Length - 1] = transport;
            _pipelineCount = 1;
            Logger = ServiceLogger.NullLogger;
            Pool = ArrayPool<byte>.Shared;
        }

        public ServicePipeline(ServiceTransport transport, params ServicePolicy[] policies)
            : this(transport)
        {
            foreach (var policy in policies) Add(policy);
        }

        public void Add(ServicePolicy policy)
        {
            var index = _pipeline.Length - _pipelineCount - 1;
            if (index < 0)
            {
                var larger = new ServicePolicy[_pipeline.Length * 2];
                Array.Copy(_pipeline, 0, larger, _pipeline.Length, _pipeline.Length);
                _pipeline = larger;
                index = _pipeline.Length - _pipelineCount - 1;
            }
            _pipeline[index] = policy;
            _pipelineCount++;
        }

        public ServiceCallContext CreateContext(CancellationToken cancellation, ServiceMethod method, Url url)
            => Transport.CreateContext(this, cancellation, method, url);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task ProcessAsync(ServiceCallContext context)
            => await ServicePolicy.ProcessNextAsync(Pipeline, context).ConfigureAwait(false);
    }

    public abstract class ServicePolicy
    {
        public abstract Task ProcessAsync(ServiceCallContext context, ReadOnlyMemory<ServicePolicy> pipeline);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected internal static async Task ProcessNextAsync(ReadOnlyMemory<ServicePolicy> pipeline, ServiceCallContext context)
        {
            var next = pipeline.Span[0];
            await next.ProcessAsync(context, pipeline.Slice(1)).ConfigureAwait(false);
        }
    }

    public abstract class ServiceTransport : ServicePolicy
    {
        public abstract Task ProcessAsync(ServiceCallContext context);

        public abstract ServiceCallContext CreateContext(ServicePipeline clinet, CancellationToken cancellation, ServiceMethod method, Url url);

        public sealed override async Task ProcessAsync(ServiceCallContext context, ReadOnlyMemory<ServicePolicy> next)
        {
            if (next.Length == 0) await ProcessAsync(context).ConfigureAwait(false);
            else throw new ArgumentOutOfRangeException(nameof(next));
        }
    }
}

