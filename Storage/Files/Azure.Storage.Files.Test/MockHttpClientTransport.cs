using Azure.Core.Net;
using Azure.Core.Net.Pipeline;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Buffers.Text.Encodings;

namespace Azure.Configuration.Test
{
    abstract class MockHttpClientTransport : HttpClientTransport
    {
        string s_sdkName = "Azure.Storage.Files";
        string s_sdkVersion = "v3.0demo";

        protected HttpMethod _expectedMethod;
        protected string _expectedUri;
        protected string _expectedRequestContent;
        int _nextResponse;

        public List<Response> Responses = new List<Response>();

        protected override Task<HttpResponseMessage> ProcessCoreAsync(CancellationToken cancellation, HttpRequestMessage request)
        {
            VerifyRequestLine(request);
            VerifyRequestContent(request);
            VerifyUserAgentHeader(request);
            VerifyRequestCore(request);
            HttpResponseMessage response = new HttpResponseMessage();
            if (WriteResponse(response))
            {
                WriteResponseCore(response);
            }
            return Task.FromResult(response);
        }

        protected virtual void VerifyRequestCore(HttpRequestMessage request) { }
        protected abstract void WriteResponseCore(HttpResponseMessage response);

        void VerifyRequestLine(HttpRequestMessage request)
        {
            Assert.AreEqual(_expectedMethod, request.Method);
            if(_expectedUri != null) Assert.AreEqual(_expectedUri, request.RequestUri.OriginalString);
            Assert.AreEqual(new Version(2, 0), request.Version);
        }

        void VerifyRequestContent(HttpRequestMessage request)
        {
            if (_expectedRequestContent == null) {
                Assert.IsNull(request.Content);
            }
            else {
                var content = request.Content;
                Assert.NotNull(content);
                var contentString = content.ReadAsStringAsync().Result;
                Assert.AreEqual(_expectedRequestContent, contentString);
            }
        }

        void VerifyUserAgentHeader(HttpRequestMessage request)
        {
            var expected = Utf8.ToString(Header.Common.CreateUserAgent(s_sdkName, s_sdkVersion).Value);

            Assert.True(request.Headers.Contains("User-Agent"));
            var userAgentValues = request.Headers.GetValues("User-Agent");

            foreach (var value in userAgentValues)
            {
                if (expected.StartsWith(value)) return;
            }
            Assert.Fail("could not find User-Agent header value " + expected);
        }

        bool WriteResponse(HttpResponseMessage response)
        {
            if (_nextResponse >= Responses.Count) _nextResponse = 0;
            var mockResponse = Responses[_nextResponse++];
            response.StatusCode = mockResponse.ResponseCode;
            return response.StatusCode == HttpStatusCode.OK;
        }

        public class Response
        {
            public HttpStatusCode ResponseCode;

            public static implicit operator Response(HttpStatusCode status) => new Response() {  ResponseCode = status };
        }
    }
}
