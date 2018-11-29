using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Azure.Core.Net.Pipeline
{
    public class LoggingPolicy : ServicePolicy
    {
        static readonly long s_warningThreshold = 3;
        static readonly long s_frequency = Stopwatch.Frequency;

        int[] _excludeErrors = Array.Empty<int>();

        public LoggingPolicy(params int[] excludeErrors)
            => _excludeErrors = excludeErrors;

        public override async Task ProcessAsync(ServiceCallContext context, ReadOnlyMemory<ServicePolicy> pipeline)
        {
            if (context.Logger.IsEnabledFor(TraceLevel.Info)) {
                LogRequest(context);
            }

            var before = Stopwatch.GetTimestamp();
            await ProcessNextAsync(pipeline, context).ConfigureAwait(false);
            var after = Stopwatch.GetTimestamp();

            var status = context.Response.Status;
            // if error status
            if (status >= 400 && status <= 599 && (Array.IndexOf(_excludeErrors, status) == -1))
            {
                context.Logger.Log($"ERROR : response status {status}", TraceLevel.Error);
            }

            if (context.Logger.IsEnabledFor(TraceLevel.Info)) {
                LogResponse(context);
            }

            var elapsedMilliseconds = (after - before) * 1000 / s_frequency;
            if (elapsedMilliseconds > s_warningThreshold) {
                if (context.Logger.IsEnabledFor(TraceLevel.Warning)) {
                    context.Logger.Log($"SLOW: {elapsedMilliseconds}ms");
                }
            }
        }

        private static void LogResponse(ServiceCallContext context)
            => context.Logger.Log(context);

        private static void LogRequest(ServiceCallContext context)
            => context.Logger.Log(context);
    }
}
