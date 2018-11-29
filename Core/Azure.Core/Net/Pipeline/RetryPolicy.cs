using System;
using System.Threading.Tasks;

namespace Azure.Core.Net.Pipeline
{
    public class RetryPolicy : ServicePolicy
    {
        public readonly static Settings DefaultSettings = new Settings();

        public override async Task ProcessAsync(ServiceCallContext call, ReadOnlyMemory<ServicePolicy> pipeline)
        {
            Settings retrySettings = DefaultSettings;
            if (call.Options.TryGetOption(typeof(RetryPolicy), out object option)) {
                retrySettings = (Settings)option;
            }

            int retried = 0;
            while (true)
            {
                await ProcessNextAsync(pipeline, call).ConfigureAwait(false);
                if (retrySettings.IsSuccess(call.Response.Status)) return;
                if (retried++ > retrySettings.MaxRetries) return;
            }
        }

        public class Settings
        {
            public virtual int MaxRetries { get; } = 3;
            public virtual bool IsSuccess(int status) => status == 200;
            public virtual bool ShouldRetry(int statusCode) => true;
        }
    }
}
