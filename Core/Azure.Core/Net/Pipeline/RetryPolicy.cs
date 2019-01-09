using System;
using System.Threading.Tasks;

namespace Azure.Core.Net.Pipeline
{
    public abstract class RetryPolicy : PipelinePolicy
    {
        public static RetryPolicy CreateFixed(int maxRetries, TimeSpan delay, params int[] retriableCodes)
            => new FixedPolicy(retriableCodes, maxRetries, delay);

        public override async Task ProcessAsync(PipelineCallContext context, ReadOnlyMemory<PipelinePolicy> pipeline)
        {
            int attempt = 1;
            while (true)
            {
                await ProcessNextAsync(pipeline, context).ConfigureAwait(false);
                if (!ShouldRetry(context, attempt++, out var delay)) return;
                if (delay > TimeSpan.Zero) await Task.Delay(delay, context.Cancellation).ConfigureAwait(false);
            }
        }

        protected abstract bool ShouldRetry(PipelineCallContext context, int attempted, out TimeSpan delay);
    }

    class FixedPolicy : RetryPolicy {
        int _maxRetries;
        TimeSpan _delay;
        int[] _retriableCodes;

        public FixedPolicy(int[] retriableCodes, int maxRetries, TimeSpan delay)
        {
            if (retriableCodes == null) throw new ArgumentNullException(nameof(retriableCodes));

            _maxRetries = maxRetries;
            _delay = delay;
            _retriableCodes = retriableCodes;
            Array.Sort(_retriableCodes);
        }

        protected override bool ShouldRetry(PipelineCallContext context, int attempted, out TimeSpan delay)
        {
            delay = _delay;
            if (attempted > _maxRetries) return false;
            if(Array.BinarySearch(_retriableCodes, context.Response.Status) < 0) return false;
            return true;
        }
    }
}
