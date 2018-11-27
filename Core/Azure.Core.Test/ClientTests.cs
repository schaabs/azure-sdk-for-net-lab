using Azure.Core.Net;
using Azure.Core.Net.Pipeline;
using Azure.Core.Testing;
using NUnit.Framework;

namespace Azure.Core.Tests
{
    public class ServiceClientTests
    {
        [Test]
        public void Basics() {

            var transport = new MockTransport(500, 1); // status codes sequence
            var logging = new TestLoggingPipe();
            var retry = new RetryPolicy();

            var service = new ServicePipeline(transport);
            service.Logger = new MockLogger();

            service.Add(logging);
            service.Add(retry);

            using (var context = service.CreateContext(cancellation: default, ServiceMethod.Get, new Url("<uri>")))
            {
                context.Options.SetOption(typeof(RetryPolicy), new CustomRetryPolicy());
                service.ProcessAsync(context).Wait();
                Assert.True(context.Response.Status == 1);
                var result = logging.ToString();
                Assert.AreEqual("REQUEST: Get <uri>\nRESPONSE: 500\nREQUEST: Get <uri>\nRESPONSE: 1\n", result);
            }
        }

        class CustomRetryPolicy : RetryPolicy.Settings
        {
            public override int MaxRetries => 5;
            public override bool IsSuccess(int status) => status == 1;
        }
    }
}
