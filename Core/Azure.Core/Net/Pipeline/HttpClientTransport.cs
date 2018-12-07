using Azure.Core.Buffers;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Buffers.Text.Encodings;

namespace Azure.Core.Net.Pipeline
{
    public class HttpClientTransport : ServiceTransport
    {
        static readonly HttpClient s_client = new HttpClient();

        public sealed override ServiceCallContext CreateContext(ServicePipeline client, CancellationToken cancellation, ServiceMethod method, Url url)
            => new HttpClientContext(ref client, cancellation, method, url);
            
        public sealed override async Task ProcessAsync(ServiceCallContext context)
        {
            var httpTransportContext = context as HttpClientContext;
            if (httpTransportContext == null) throw new InvalidOperationException("the context is not compatible with the transport");

            var httpRequest = httpTransportContext.BuildRequestMessage();

            HttpResponseMessage responseMessage = await ProcessCoreAsync(context.Cancellation, httpRequest).ConfigureAwait(false);
            httpTransportContext.ProcessResponseMessage(responseMessage);
        }

        protected virtual async Task<HttpResponseMessage> ProcessCoreAsync(CancellationToken cancellation, HttpRequestMessage httpRequest)
        {
            return await s_client.SendAsync(httpRequest, cancellation).ConfigureAwait(false);
        }

        class HttpClientContext : ServiceCallContext
        {
            List<(string Name, string Value)> _headers = new List<(string, string)>();
            Sequence<byte> _content = new Sequence<byte>();

            HttpRequestMessage _requestMessage;
            string _contentTypeHeaderValue; // TODO (pri 2): move this to _headers
            string _contentLengthHeaderValue;

            HttpResponseMessage _responseMessage;
            
            public HttpClientContext(ref ServicePipeline client, CancellationToken cancellation, ServiceMethod method, Url url) 
                : base(url, cancellation, client.Logger)
            {
                _content = new Sequence<byte>(client.Pool);
                _requestMessage = new HttpRequestMessage();
                _requestMessage.Method = ToHttpClientMethod(method);
                _requestMessage.RequestUri = new Uri(url.ToString());
            }

            #region Request
            public override void AddHeader(Header header)
            {
                var valueString = Utf8.ToString(header.Value);
                var name = header.Name;

                if (name.SequenceEqual(Header.Constants.ContentType)) {
                    _contentTypeHeaderValue = valueString;
                }
                else if (name.SequenceEqual(Header.Constants.ContentLength)) {
                    _contentLengthHeaderValue = valueString;
                }
                else {
                    var nameString = Utf8.ToString(header.Name);
                    _requestMessage.Headers.Add(nameString, valueString);
                }
            }

            public override void AddHeader(string name, string value)
            {
                if (name.Equals("Content-Type", StringComparison.InvariantCulture)) {
                    _contentTypeHeaderValue = value;
                }
                if (name.Equals("Content-Length", StringComparison.InvariantCulture)) {
                    _contentLengthHeaderValue = value;
                }
                else {
                    _requestMessage.Headers.Add(name, value);
                }
            }

            protected internal override Memory<byte> GetRequestBuffer(int minimumSize) => _content.GetMemory(minimumSize);

            protected internal override void CommitRequestBuffer(int size) => _content.Advance(size);

            protected internal override Task FlushAsync()
                => Task.CompletedTask;

            internal HttpRequestMessage BuildRequestMessage()
            {
                // A copy of a message needs to be made because HttpClient does not allow sending the same message twice,
                // and so the retry logic fails.
                var message = new HttpRequestMessage(_requestMessage.Method, _requestMessage.RequestUri);
                foreach (var header in _requestMessage.Headers)
                {
                    message.Headers.Add(header.Key, header.Value);
                }

                if (_content.Length != 0)
                {
                    if (RequestContentSource != null) throw new InvalidOperationException("cannot both write to content and specify content source");
                    message.Content = new ReadOnlySequenceContent(_content.AsReadOnly(), Cancellation);
                    message.Content.Headers.Add("Content-Type", _contentTypeHeaderValue);
                    message.Content.Headers.Add("Content-Length", _contentLengthHeaderValue);
                }
                else if(RequestContentSource != null) {
                    message.Content = new StreamContent(RequestContentSource);
                    message.Content.Headers.Add("Content-Type", _contentTypeHeaderValue);
                    message.Content.Headers.Add("Content-Length", _contentLengthHeaderValue);
                }

                return message;
            }
            #endregion

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

            protected internal override ReadOnlySequence<byte> ResponseContent => _content.AsReadOnly();

            protected internal override ReadOnlySequence<byte> RequestContent => _content.AsReadOnly();

            protected internal async override Task<ReadOnlySequence<byte>> ReadContentAsync(long minimumLength)
            { 
                _content.Dispose();
                var contentStream = await _responseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false);
                while (true)
                {
                    var length = _content.Length;
                    if (length >= minimumLength) return _content.AsReadOnly(); // TODO (pri 3): is this ok when minimumLength is zero?
                    _content = await contentStream.ReadAsync(_content, Cancellation).ConfigureAwait(false);
                }
            }

            protected internal override void DisposeResponseContent(long bytes)
            {
                // TODO (pri 2): this should dispose already read segments
            }
            #endregion

            public override void Dispose()
            {
                _headers.Clear();
                _content.Dispose();
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
