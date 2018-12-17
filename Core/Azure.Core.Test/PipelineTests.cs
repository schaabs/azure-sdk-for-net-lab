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

            var pipeline = new ClientPipeline(new MockTransport(500, 1));
            var loggingPolicy = pipeline.AddPolicy(new TestLoggingPolicy());
            pipeline.AddPolicy(new RetryPolicy());

            var options = new Options();
            options.Logger = new MockLogger();
            

            using (var context = pipeline.CreateContext(options, cancellation: default, ServiceMethod.Get, new Uri("https://contoso.a.io")))
            {
                context.Options.SetOption(typeof(RetryPolicy), new CustomRetryPolicy());
                pipeline.ProcessAsync(context).Wait();
                Assert.True(context.Response.Status == 1);
                var result = loggingPolicy.ToString();
                Assert.AreEqual("REQUEST: Get https://contoso.a.io/\nRESPONSE: 500\nREQUEST: Get https://contoso.a.io/\nRESPONSE: 1\n", result);
            }
        }

        class CustomRetryPolicy : RetryPolicy.Settings
        {
            public override int MaxRetries => 5;
            public override bool IsSuccess(int status) => status == 1;
        }

        class Options : ClientOptions { }
    }
}
