﻿using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Buffers.Text.Encodings;

namespace Azure.Core.Net.Pipeline
{
    // TODO (pri 2): implement chunked encoding
    public class HttpPipelineTransport : PipelineTransport
    {
        static readonly HttpClient s_client = new HttpClient();

        public sealed override PipelineCallContext CreateContext(PipelineOptions options, CancellationToken cancellation)
            => new Context(cancellation);

        public sealed override async Task ProcessAsync(PipelineCallContext context)
        {
            var httpTransportContext = context as Context;
            if (httpTransportContext == null) throw new InvalidOperationException("the context is not compatible with the transport");

            HttpRequestMessage httpRequest = httpTransportContext.BuildRequestMessage();

            HttpResponseMessage responseMessage = await ProcessCoreAsync(context.Cancellation, httpRequest).ConfigureAwait(false);
            httpTransportContext.ProcessResponseMessage(responseMessage);
        }

        protected virtual async Task<HttpResponseMessage> ProcessCoreAsync(CancellationToken cancellation, HttpRequestMessage httpRequest)
            => await s_client.SendAsync(httpRequest, cancellation).ConfigureAwait(false);

        sealed class Context : PipelineCallContext
        {
            string _contentTypeHeaderValue;
            string _contentLengthHeaderValue;
            PipelineContent _requestContent;
            HttpRequestMessage _requestMessage;
            HttpResponseMessage _responseMessage;

            public Context(CancellationToken cancellation) : base(cancellation)
                => _requestMessage = new HttpRequestMessage();

            #region Request
            public override void SetRequestLine(ServiceMethod method, Uri uri)
            {
                _requestMessage.Method = ToHttpClientMethod(method);
                _requestMessage.RequestUri = uri;
            }

            public override void AddHeader(Header header)
            {
                var valueString = Utf8.ToString(header.Value);
                var nameString = Utf8.ToString(header.Name);
                AddHeader(nameString, valueString);
            }

            public override void AddHeader(string name, string value)
            {
                // TODO (pri 1): any other headers must be added to content?
                if (name.Equals("Content-Type", StringComparison.InvariantCulture)) {
                    _contentTypeHeaderValue = value;
                }
                else if (name.Equals("Content-Length", StringComparison.InvariantCulture)) {
                    _contentLengthHeaderValue = value;
                }
                else {
                    if (!_requestMessage.Headers.TryAddWithoutValidation(name, value)) {
                        throw new InvalidOperationException();
                    }
                }
            }

            public override void SetContent(PipelineContent content)
                => _requestContent = content;

            public HttpRequestMessage BuildRequestMessage()
            {
                // A copy of a message needs to be made because HttpClient does not allow sending the same message twice,
                // and so the retry logic fails.
                var message = new HttpRequestMessage(_requestMessage.Method, _requestMessage.RequestUri);
                foreach (var header in _requestMessage.Headers) {
                    if (!message.Headers.TryAddWithoutValidation(header.Key, header.Value)) {
                        throw new Exception("could not add header " + header.ToString());
                    }
                }

                if (_requestContent != null) {
                    message.Content = new PipelineContentAdapter(_requestContent, Cancellation);
                    if (_contentTypeHeaderValue != null) message.Content.Headers.Add("Content-Type", _contentTypeHeaderValue);
                    if (_contentLengthHeaderValue != null) message.Content.Headers.Add("Content-Length", _contentLengthHeaderValue);
                }

                return message;
            }
            #endregion

            #region Response
            internal void ProcessResponseMessage(HttpResponseMessage response) {
                _responseMessage = response;
                _requestMessage.Dispose();
            }

            protected internal override int Status => (int)_responseMessage.StatusCode;

            protected internal override bool TryGetHeader(ReadOnlySpan<byte> name, out ReadOnlySpan<byte> value)
            {
                string nameString = Utf8.ToString(name);
                if (!_responseMessage.Headers.TryGetValues(nameString, out var values)) {
                    if (!_responseMessage.Content.Headers.TryGetValues(nameString, out values)) {
                        value = default;
                        return false;
                    }
                }

                var all = string.Join(",", values);
                value = Encoding.ASCII.GetBytes(all);
                return true;
            }

            // TODO (pri 1): is it ok to just call .Result here?
            protected internal override Stream ResponseContentStream => _responseMessage.Content.ReadAsStreamAsync().Result;
            #endregion

            public override void Dispose()
            {
                _requestContent?.Dispose();
                _requestMessage?.Dispose();
                _responseMessage?.Dispose();
                base.Dispose();
            }

            public override string ToString() =>
                _responseMessage!=null? _responseMessage.ToString() : _requestMessage.ToString();

            public static HttpMethod ToHttpClientMethod(ServiceMethod method)
            {
                switch (method) {
                    case ServiceMethod.Get: return HttpMethod.Get;
                    case ServiceMethod.Post: return HttpMethod.Post;
                    case ServiceMethod.Put: return HttpMethod.Put;
                    case ServiceMethod.Delete: return HttpMethod.Delete;

                    default: throw new NotImplementedException();
                }
            }

            sealed class PipelineContentAdapter : HttpContent
            {
                PipelineContent _content;
                CancellationToken _cancellation;

                public PipelineContentAdapter(PipelineContent content, CancellationToken cancellation)
                {
                    Debug.Assert(content != null);

                    _content = content;
                    _cancellation = cancellation;
                }

                protected async override Task SerializeToStreamAsync(Stream stream, TransportContext context)
                    => await _content.WriteTo(stream, _cancellation).ConfigureAwait(false);

                protected override bool TryComputeLength(out long length)
                    => _content.TryComputeLength(out length);

                protected override void Dispose(bool disposing)
                {
                    _content.Dispose();
                    base.Dispose(disposing);
                }
            }
        }
    }
}
