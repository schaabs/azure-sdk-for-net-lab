using System;
using System.Threading.Tasks;

namespace Azure.Core.Net.Pipeline
{
    public class TelemetryPolicy : ServicePolicy
    {
        string _sdkName;
        string _sdkVersion;
        string _applicationId;
        Header _uaHeader;

        public TelemetryPolicy(string sdkName, string sdkVersion, string applicationId)
        {
            _sdkName = sdkName;
            _sdkVersion = sdkVersion;
            _applicationId = applicationId;
            _uaHeader = Header.Common.CreateUserAgent(_sdkName, _sdkVersion, _applicationId);
        }

        public override async Task ProcessAsync(ServiceCallContext context, ReadOnlyMemory<ServicePolicy> pipeline)
        {
            context.AddHeader(_uaHeader);
            await ProcessNextAsync(pipeline, context).ConfigureAwait(false);
        }
    }
}
