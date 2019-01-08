using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.Core.Net.Pipeline
{
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

        public abstract PipelineCallContext CreateContext(PipelineOptions options, CancellationToken cancellation);

        public sealed override async Task ProcessAsync(PipelineCallContext context, ReadOnlyMemory<PipelinePolicy> next)
        {
            if (next.Length == 0) await ProcessAsync(context).ConfigureAwait(false);
            else throw new ArgumentOutOfRangeException(nameof(next));
        }
    }
}
