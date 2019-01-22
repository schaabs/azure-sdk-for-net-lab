// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for
// license information.

using NUnit.Framework;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core.Testing;
using System.Net.Http;
using System.IO;
using System.Text;
using Azure.Core.Http;
using Azure.ApplicationModel.Configuration.Test;

namespace Azure.Storage.Files.Tests
{
    public class StorageFilesTests
    {
        const string baseUri = "https://myaccount.file.core.windows.net";

        [Test]
        public async Task Create()
        {
            var (service, pool) = CreateTestService(new CreateMock());

            var response = await service.CreateAsync(CancellationToken.None);

            response.Dispose();
            Assert.AreEqual(0, pool.CurrentlyRented);
        }

        [Test]
        public async Task PutRange()
        {
            var payload = "Hello, Universe!";

            var (service, pool) = CreateTestService(new PutRangeMock(payload));

            byte[] contnet = Encoding.ASCII.GetBytes(payload);
            Stream stream = new MemoryStream(contnet);

            var response = await service.PutRangeAsync(0, (int)stream.Length, stream, CancellationToken.None);

            response.Dispose();
            Assert.AreEqual(0, pool.CurrentlyRented);
        }

        [Test]
        public async Task Get()
        {
            var payload = "Hello World";

            byte[] responseBytes = Encoding.ASCII.GetBytes(payload);

            var (service, pool) = CreateTestService(new GetMock(new MemoryStream(responseBytes)));

            var response = await service.GetAsync(CancellationToken.None);
            if (response.Status == 200)
            {
                using (Stream content = response.Result)
                {
                    byte[] buffer = new byte[1024];
                    var read = await content.ReadAsync(buffer);
                    var responseText = Encoding.ASCII.GetString(buffer, 0, read);
                    Assert.AreEqual(payload, responseText);
                }
            }

            response.Dispose();
            Assert.AreEqual(0, pool.CurrentlyRented);
        }

        private static (FileUri service, TestPool<byte> pool) CreateTestService(MockHttpClientTransport transport)
        {
            var options = new PipelineOptions();
            var pool = new TestPool<byte>();
            if (transport.Responses.Count == 0) {
                transport.Responses.Add(HttpStatusCode.GatewayTimeout);
                transport.Responses.Add(HttpStatusCode.OK);
            }
            options.Transport = transport;
            options.Pool = pool;

            var service = new FileUri(baseUri, options);

            return (service, pool);
        }
    }

    class CreateMock : MockHttpClientTransport
    {
        public CreateMock()
        {
            _expectedMethod = HttpMethod.Put;
        }
        protected override void WriteResponseCore(HttpResponseMessage response)
        {
        }
    }

    class PutRangeMock : MockHttpClientTransport
    {
        public PutRangeMock(string expectedContent)
        {
            _expectedMethod = HttpMethod.Put;
            _expectedRequestContent = expectedContent;
        }

        protected override void WriteResponseCore(HttpResponseMessage response)
        {
        }
    }

    class GetMock : MockHttpClientTransport
    {
        Stream _responseContent;

        public GetMock(Stream responseContent)
        {
            _expectedMethod = HttpMethod.Get;
            _responseContent = responseContent;
        }
        protected override void WriteResponseCore(HttpResponseMessage response)
        {
            response.Content = new StreamContent(_responseContent);
            response.Content.Headers.Add("Content-Length", ((int)_responseContent.Length).ToString());
        }
    }
}
