using System;
using System.Threading.Tasks;

namespace Azure.Core.Net.Pipeline
{
    public class TelemetryPolicy : PipelinePolicy
    {
        Header _uaHeader;

        public TelemetryPolicy(Header userAgentHeader)
        {
            _uaHeader = userAgentHeader;
        }

        public override async Task ProcessAsync(PipelineCallContext context, ReadOnlyMemory<PipelinePolicy> pipeline)
        {
            context.AddHeader(_uaHeader);
            await ProcessNextAsync(pipeline, context).ConfigureAwait(false);
        }
    }
}
