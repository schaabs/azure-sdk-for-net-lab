using System;
using System.Threading.Tasks;

namespace Azure.Core.Net.Pipeline
{
    public class TelemetryPolicy : PipelinePolicy
    {
        Header _uaHeader;

        public TelemetryPolicy(string sdkName, string sdkVersion, string applicationId)
        {
            _uaHeader = Header.Common.CreateUserAgent(sdkName, sdkVersion, applicationId);
        }

        public override async Task ProcessAsync(PipelineCallContext context, ReadOnlyMemory<PipelinePolicy> pipeline)
        {
            context.AddHeader(_uaHeader);
            await ProcessNextAsync(pipeline, context).ConfigureAwait(false);
        }
    }
}
