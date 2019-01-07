using Azure.Core.Buffers;
using System;
using System.Buffers;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
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

        public sealed override PipelineCallContext CreateContext(ref PipelineOptions options, CancellationToken cancellation, ServiceMethod method, Uri uri)
            => new Context(options.Pool, cancellation, method, uri);
            
        public sealed override async Task ProcessAsync(PipelineCallContext context)
        {
            var httpTransportContext = context as Context;
            if (httpTransportContext == null) throw new InvalidOperationException("the context is not compatible with the transport");

            var httpRequest = httpTransportContext.BuildRequestMessage();

            HttpResponseMessage responseMessage = await ProcessCoreAsync(context.Cancellation, httpRequest).ConfigureAwait(false);
            httpTransportContext.ProcessResponseMessage(responseMessage);
        }

        protected virtual async Task<HttpResponseMessage> ProcessCoreAsync(CancellationToken cancellation, HttpRequestMessage httpRequest)
            => await s_client.SendAsync(httpRequest, cancellation).ConfigureAwait(false);

        class Context : PipelineCallContext
        {
            string _contentTypeHeaderValue;
            string _contentLengthHeaderValue;
            PipelineContent _requestContent;
            HttpRequestMessage _requestMessage;
            HttpResponseMessage _responseMessage;
            
            public Context(ArrayPool<byte> pool, CancellationToken cancellation, ServiceMethod method, Uri uri) 
                : base(uri, cancellation)
            {
                _requestMessage = new HttpRequestMessage();
                _requestMessage.Method = ToHttpClientMethod(method);
                _requestMessage.RequestUri = uri;

                string hostString = uri.Host;
                AddHeader("Host", hostString);
            }

            #region Request
            public override void AddHeader(Header header)
            {
                var valueString = Utf8.ToString(header.Value);
                var nameString = Utf8.ToString(header.Name);
                AddHeader(nameString, valueString);
            }

            public override void AddHeader(string name, string value)
            {
                if (name.Equals("Content-Type", StringComparison.InvariantCulture)) {
                    _contentTypeHeaderValue = value;
                }
                else if (name.Equals("Content-Length", StringComparison.InvariantCulture))
                {
                    _contentLengthHeaderValue = value;
                }
                else {
                    if(!_requestMessage.Headers.TryAddWithoutValidation(name, value))
                    {
                        throw new NotImplementedException();
                    }
                }
            }

            public override void AddContent(PipelineContent content)
                => _requestContent = content;

            internal HttpRequestMessage BuildRequestMessage()
            {
                // A copy of a message needs to be made because HttpClient does not allow sending the same message twice,
                // and so the retry logic fails.
                var message = new HttpRequestMessage(_requestMessage.Method, _requestMessage.RequestUri);
                foreach (var header in _requestMessage.Headers) {
                    if(!message.Headers.TryAddWithoutValidation(header.Key, header.Value)) {
                        throw new Exception("could not add header " + header.ToString());
                    }
                }

                if(_requestContent != null)
                {
                    message.Content = new PipelineContentAdapter(_requestContent);
                    message.Content.Headers.Add("Content-Type", _contentTypeHeaderValue);
                    if(_contentLengthHeaderValue!=null) message.Content.Headers.Add("Content-Length", _contentLengthHeaderValue);
                }

                return message;
            }
            #endregion

            class PipelineContentAdapter : HttpContent
            {
                PipelineContent _content;
                public PipelineContentAdapter(PipelineContent content)
                    => _content = content;

                protected async override Task SerializeToStreamAsync(Stream stream, TransportContext context)
                    => await _content.WriteTo(stream);

                protected override bool TryComputeLength(out long length)
                    => _content.TryComputeLength(out length);
            }
            #region Response
            internal void ProcessResponseMessage(HttpResponseMessage response) { _responseMessage = response; }

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
                // TODO (pri 1): we need to decide what to do with duplicated headers
                var e = values.GetEnumerator();
                if(!e.MoveNext()) {
                    value = default;
                    return false;
                }

                string first = e.Current;
                if(!e.MoveNext()) {
                    value = Encoding.UTF8.GetBytes(first);
                    return true;
                }

                var all = string.Join(",", values);
                value = Encoding.ASCII.GetBytes(all);
                return true;
            }

            protected internal override Stream ResponseContent => _responseMessage.Content.ReadAsStreamAsync().Result;
            #endregion

            public override void Dispose()
            {
                _requestContent?.Dispose();
                _requestMessage?.Dispose();
                _responseMessage?.Dispose();
                base.Dispose();
            }

            public override string ToString() => _requestMessage.ToString();

            public static HttpMethod ToHttpClientMethod(ServiceMethod method)
            {
                switch (method)
                {
                    case ServiceMethod.Get: return HttpMethod.Get;
                    case ServiceMethod.Post: return HttpMethod.Post;
                    case ServiceMethod.Put: return HttpMethod.Put;
                    case ServiceMethod.Delete: return HttpMethod.Delete;

                    default: throw new NotImplementedException();
                }
            }
        }
    }
}
