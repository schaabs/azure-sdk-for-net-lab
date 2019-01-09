using Azure.Core.Net;
using Azure.Core.Net.Pipeline;
using Azure.Core.Testing;
using NUnit.Framework;
using System;

namespace Azure.Core.Tests
{
    public class PipelineTests
    {
        [Test]
        public void Basics() {

            var options = new PipelineOptions();
            options.Transport = new MockTransport(500, 1);
            options.RetryPolicy = new CustomRetryPolicy();
            options.LoggingPolicy = new TestLoggingPolicy();
            options.Logger = new MockLogger();

            var pipeline = ClientPipeline.Create(options, "test", "1.0.0");

            using (var context = pipeline.CreateContext(options, cancellation: default))
            {
                context.SetRequestLine(ServiceMethod.Get, new Uri("https://contoso.a.io"));
                pipeline.ProcessAsync(context).Wait();

                Assert.AreEqual(1, context.Response.Status);
                var result = options.LoggingPolicy.ToString();
                Assert.AreEqual("REQUEST: Get https://contoso.a.io/\nRESPONSE: 500\nREQUEST: Get https://contoso.a.io/\nRESPONSE: 1\n", result);
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
