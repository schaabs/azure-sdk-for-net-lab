using Azure.Core.Net;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Azure.Core.Testing
{
    public class TestLoggingPipe : PipelinePolicy
    {
        StringBuilder _logged = new StringBuilder();

        public override async Task ProcessAsync(PipelineCallContext context, ReadOnlyMemory<PipelinePolicy> pipeline)
        {
            _logged.Append($"REQUEST: {context.ToString()}\n");
            await ProcessNextAsync(pipeline, context).ConfigureAwait(false);
            _logged.Append($"RESPONSE: {context.Response.Status}\n");
        }

        public override string ToString()
            => _logged.ToString();
    }
}
