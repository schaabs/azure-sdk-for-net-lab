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
            options.RetryPolicy = new RetryPolicy();
            options.LoggingPolicy = new TestLoggingPolicy();
            options.Logger = new MockLogger();

            var pipeline = ClientPipeline.Create(options, "test", "1.0.0");

            using (var context = pipeline.CreateContext(options, cancellation: default, ServiceMethod.Get, new Uri("https://contoso.a.io")))
            {
                context.Logger = new MockLogger();
                context.Options.SetOption(typeof(RetryPolicy), new CustomRetryPolicy());
                pipeline.ProcessAsync(context).Wait();
                Assert.True(context.Response.Status == 1);
                var result = options.LoggingPolicy.ToString();
                Assert.AreEqual("REQUEST: Get https://contoso.a.io/\nRESPONSE: 500\nREQUEST: Get https://contoso.a.io/\nRESPONSE: 1\n", result);
            }
        }

        class CustomRetryPolicy : RetryPolicy.Settings
        {
            public override int MaxRetries => 5;
            public override bool IsSuccess(int status) => status == 1;
        }

        class Options : PipelineOptions { }
    }
}
