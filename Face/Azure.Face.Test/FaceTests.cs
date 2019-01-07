using Azure.Core;
using Azure.Core.Net;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.Face.Tests
{
    public class FaceServiceTests
    {
        static readonly string s_account = "https://westcentralus.api.cognitive.microsoft.com/face/";
        static readonly string s_key = "7f31d0b1e62b4b5cb985bcdb966204f9";
        
        [Test]
        public async Task DetectLocalPath()
        {
            if(NoAccountSettings()) return;
            var cancellation = new CancellationTokenSource();

            var service = new FaceClient(s_account, s_key);
            var response = await service.DetectAsync(cancellation.Token, @"images\face2.jpg");

            Assert.AreEqual("male", response.Result.Gender);
            Assert.Greater(100, response.Result.Age);
            Assert.Less(10, response.Result.Age);

            response.Dispose();

        }

        [Test]
        public async Task DetectUrl()
        {
            if (NoAccountSettings()) return;
            var cancellation = new CancellationTokenSource();

            var service = new FaceClient(s_account, s_key);
            var response = await service.DetectAsync(cancellation.Token, new Uri(@"https://upload.wikimedia.org/wikipedia/commons/5/50/Albert_Einstein_%28Nobel%29.png"));

            Assert.AreEqual("male", response.Result.Gender);
            Assert.Greater(100, response.Result.Age);
            Assert.Less(10, response.Result.Age);

            response.Dispose();
        }

        [Test]
        public async Task DetectOverSockets()
        {
            if (NoAccountSettings()) return;
            
            var options = new PipelineOptions();
            options.Transport = new SocketClientTransport();
            var service = new FaceClient(s_account, s_key, options);

            var cancellation = new CancellationTokenSource();
            var response = await service.DetectAsync(cancellation.Token, new Uri(@"https://upload.wikimedia.org/wikipedia/commons/5/50/Albert_Einstein_%28Nobel%29.png"));

            Assert.AreEqual(200, response.Status);

            Assert.AreEqual("male", response.Result.Gender);
            Assert.Greater(100, response.Result.Age);
            Assert.Less(10, response.Result.Age);
            response.Dispose();
        }

        [Test]
        public async Task DetectResponseHeaders()
        {
            if (NoAccountSettings()) return;
            var cancellation = new CancellationTokenSource();

            var service = new FaceClient(s_account, s_key);
            var response = await service.DetectAsync(cancellation.Token, new Uri(@"https://upload.wikimedia.org/wikipedia/commons/5/50/Albert_Einstein_%28Nobel%29.png"));

            Assert.IsTrue(response.TryGetHeader("Content-Length", out var value));
            Assert.IsTrue(response.TryGetHeader("Content-Type", out var type));
            Assert.AreEqual("application/json; charset=utf-8", type);

            // TODO (pri 2): is this the best we can do?
            response.Dispose();
        }

        [Test]
        public async Task DetectStreaming()
        {
            if (NoAccountSettings()) return;
            var cancellation = new CancellationTokenSource();

            var options = new PipelineOptions();
            options.Transport = new SocketClientTransport(); // TODO (pri 1): streaming does not work with HttpTransport

            var service = new FaceClient(s_account, s_key, options);

            Response<Stream> response = await service.DetectLazyAsync(cancellation.Token, new Uri(@"https://upload.wikimedia.org/wikipedia/commons/5/50/Albert_Einstein_%28Nobel%29.png"));

            Assert.IsTrue(response.TryGetHeader(Encoding.ASCII.GetBytes("Content-Length"), out long contentLength));
            Assert.IsTrue(response.TryGetHeader("Content-Type", out string type));
            Assert.AreEqual("application/json; charset=utf-8", type);

            var content = ReadOnlySequence<byte>.Empty;
            while (content.Length != contentLength)
            {
                var buffer = new byte[contentLength];
                var read = await response.Result.ReadAsync(buffer, cancellation.Token);
                content = new ReadOnlySequence<byte>(buffer, 0, read);
            }
                  
            if (content.IsSingleSegment)
            {
                var array = content.First.Span.ToArray();
                var stream = new StreamReader(new MemoryStream(array));
                JsonSerializer serializer = new JsonSerializer();
                var json = (DetectResult[])serializer.Deserialize(stream, typeof(DetectResult[]));
                DetectResult result = json[0];
                Assert.AreEqual("male", result.FaceAttributes.Gender);
                Assert.Greater(100, result.FaceAttributes.Age);
                Assert.Less(10, result.FaceAttributes.Age);
            }
                              
            response.Dispose();
        }

        struct DetectResult
        {
            [JsonProperty(PropertyName = "faceId")]
            public string Id;

            [JsonProperty(PropertyName = "faceAttributes")]
            public Attributes FaceAttributes;

            public struct Attributes
            {
                [JsonProperty(PropertyName = "age")]
                public float Age;
                [JsonProperty(PropertyName = "gender")]
                public string Gender;
            }
        }

        static bool NoAccountSettings()
        {
            if (string.IsNullOrEmpty(s_key) || string.IsNullOrEmpty(s_account))
            {
                TestContext.Error.WriteLine("TEST SKIPPED");
                TestContext.Error.WriteLine("*************************************************************************");
                TestContext.Error.WriteLine("ERROR: Please set FaceServiceTests.s_key and FaceServiceTests.s_account");
                TestContext.Error.WriteLine("*************************************************************************\n");
                return true;
            }
            return false;
        }
    }
}
