using Azure.Core.Net;
using Azure.Core.Net.Pipeline;
using Azure.Core.Testing;
using NUnit.Framework;
using System;
using System.Diagnostics.Tracing;

namespace Azure.Core.Tests
{
    // TODO (pri 2): Do use the EventRegister NuGet package or the standalone eventRegister.exe tool, to run build-time validation of the event source classes defined in your assemblies.
    public class PipelineTests
    {
        string expected = @"ProcessingRequest : Get https://contoso.a.io/ # ErrorResponse : 500 # ProcessingResponse : Get https://contoso.a.io/ # ProcessingRequest : Get https://contoso.a.io/ # ProcessingResponse : Get https://contoso.a.io/";

        [Test]
        public void Basics() {

            var options = new PipelineOptions();
            options.Transport = new MockTransport(500, 1);
            options.RetryPolicy = new CustomRetryPolicy();

            var listener = new TestEventListener();
            listener.EnableEvents(EventLevel.LogAlways);

            var pipeline = ClientPipeline.Create(options, "test", "1.0.0");

            using (var context = pipeline.CreateContext(options, cancellation: default))
            {
                context.SetRequestLine(ServiceMethod.Get, new Uri("https://contoso.a.io"));
                pipeline.ProcessAsync(context).Wait();

                Assert.AreEqual(1, context.Response.Status);
                var result = listener.ToString();
                Assert.AreEqual(expected, result);
            }
        }

        class CustomRetryPolicy : RetryPolicy
        {
            protected override bool ShouldRetry(PipelineCallContext context, int retry, out TimeSpan delay)
            {
                delay = TimeSpan.Zero;
                if (retry > 5) return false;
                if (context.Response.Status == 1) return false;
                return true;
            }
        }
    }
}
