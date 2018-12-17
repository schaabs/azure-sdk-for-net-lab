using Azure.Core;
using Azure.Core.Buffers;
using Azure.Core.Net;
using Azure.Core.Net.Pipeline;
using System;
using System.Buffers;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.Face
{
    public class FaceClient
    {
        const string SdkName = "Azure.Face";
        const string SdkVersion = "1.0.0";

        ClientPipeline _client;
        PipelineOptions _options;
        Url _baseUrl;
        Header _keyHeader;

        // TODO (pri 1): changing Application ID is not really working
        public readonly FaceServiceOptions Options = new FaceServiceOptions("v1.0");

        public FaceClient(string baseUri, string key)
            : this(baseUri, key, new PipelineOptions())
        { }

        public FaceClient(string baseUri, string key, PipelineOptions options) 
            : this((Url)baseUri, key, options)
        { }

        public FaceClient(Uri baseUrl, string key)
            : this(baseUrl.ToString(), key, new PipelineOptions())
        { }

        public FaceClient(Uri baseUri, string key, PipelineOptions options)
            : this(baseUri.ToString(), key, options)
        { }

        private FaceClient(Url baseUrl, string key, PipelineOptions options)
        {
            _options = options;
            _baseUrl = baseUrl;
            _keyHeader = new Header(s_keyHeaderName, key);
            _client = ClientPipeline.Create(options, SdkName, SdkVersion);
        }

        // TODO (pri 2): I think I want to change it such that response details are a property on the deserialized type, but then the deserialization would be eager.
        public Task<Response<FaceDetectResult>> DetectAsync(CancellationToken cancellation, Uri image)
            => DetectAsync(cancellation, image, default);

        public async Task<Response<FaceDetectResult>> DetectAsync(CancellationToken cancellation, Uri image, FaceDetectOptions options)
        {
            if (options == null) options = new FaceDetectOptions();
            Uri url = BuildUrl(options);

            PipelineCallContext context = null;
            try
            {
                context = _client.CreateContext(_options, cancellation, ServiceMethod.Post, url);
                
                context.AddHeader(_keyHeader);
                context.AddHeader(Header.Common.JsonContentType);

                WriteJsonContent(context, image);

                await _client.ProcessAsync(context).ConfigureAwait(false);

                ServiceResponse response = context.Response;
                if (!response.TryGetHeader(s_contentLength, out long contentLength)) {
                    throw new Exception("bad response: no content length header");
                }

                await response.Content.ReadAsync(contentLength).ConfigureAwait(false);

                Func<ServiceResponse, FaceDetectResult> contentParser = null;
                if (response.Status == 200) {
                    contentParser = (rsp) => { return FaceDetectResult.Parse(rsp.Content.Bytes); };
                }
                return new Response<FaceDetectResult>(response, contentParser);
            }
            catch
            {
                if (context != null) context.Dispose();
                throw;
            }
        }

        public Task<Response<FaceDetectResult>> DetectAsync(CancellationToken cancellation, string imagePath)
            => DetectAsync(cancellation, imagePath, default);

        public async Task<Response<FaceDetectResult>> DetectAsync(CancellationToken cancellation, string imagePath, FaceDetectOptions options)
        {
            if (options == null) options = new FaceDetectOptions();
            Uri url = BuildUrl(options);

            PipelineCallContext context = null;
            try
            {
                context = _client.CreateContext(_options, cancellation, ServiceMethod.Post, url);

                context.AddHeader(_keyHeader);
                context.AddHeader(Header.Common.OctetStreamContentType);

                SetContentStream(context, imagePath);

                await _client.ProcessAsync(context).ConfigureAwait(false);

                ServiceResponse response = context.Response;
                if (!response.TryGetHeader(s_contentLength, out long contentLength)) {
                    throw new Exception("bad response: no content length header");
                }

                await response.Content.ReadAsync(contentLength).ConfigureAwait(false);

                Func<ServiceResponse, FaceDetectResult> parser = null;
                if (response.Status == 200)
                {
                    parser = (rsp) => { return FaceDetectResult.Parse(rsp.Content.Bytes); };
                }
                return new Response<FaceDetectResult>(response, parser);
            }
            catch
            {
                if (context != null) context.Dispose();
                throw;
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public async Task<Response<ContentReader>> DetectLazyAsync(CancellationToken cancellation, Uri image, FaceDetectOptions options = default)
        {
            if (options == null) options = new FaceDetectOptions();
            Uri url = BuildUrl(options);

            PipelineCallContext context = null;
            try
            {
                context = _client.CreateContext(_options, cancellation, ServiceMethod.Post, url);

                context.AddHeader(_keyHeader);
                context.AddHeader(Header.Common.JsonContentType);

                WriteJsonContent(context, image);

                await _client.ProcessAsync(context).ConfigureAwait(false);

                return new Response<ContentReader>(context.Response, context.Response.Content);
            }
            catch
            {
                if (context != null) context.Dispose();
                throw;
            }
        }

        public struct FaceServiceOptions
        {
            internal Header UserAgentHeader;
            internal byte[] ApiVersion;
            string _applicationId;

            public FaceServiceOptions(string apiVersion)
            {
                ApiVersion = Encoding.ASCII.GetBytes(apiVersion);
                _applicationId = default;
                UserAgentHeader = Header.Common.CreateUserAgent(sdkName: "Azure-CognitiveServices-Face", sdkVersion: "1.0.0", _applicationId);
            }

            public string ApplicationId
            {
                get { return _applicationId; }
                set {
                    if (string.Equals(_applicationId, value, StringComparison.Ordinal)) return;
                    _applicationId = value;
                    UserAgentHeader = Header.Common.CreateUserAgent(sdkName: "Azure-CognitiveServices-Face", sdkVersion: "1.0.0", _applicationId);
                }
            }

            #region nobody wants to see these
            [EditorBrowsable(EditorBrowsableState.Never)]
            public override bool Equals(object obj) => base.Equals(obj);

            [EditorBrowsable(EditorBrowsableState.Never)]
            public override int GetHashCode() => base.GetHashCode();

            [EditorBrowsable(EditorBrowsableState.Never)]
            public override string ToString() => base.ToString();
            #endregion
        }

        Uri BuildUrl(FaceDetectOptions options)
        {
            var uriBuilder = new Utf8StringBuilder(256);
            uriBuilder.Append(_baseUrl.Bytes);
            uriBuilder.Append(Options.ApiVersion);
            uriBuilder.Append(s_detectMethod);
            options.BuildRequestParameters(ref uriBuilder);
            var uri = new Url(uriBuilder.Build());
            return new Uri(uri.ToString());
        }

        static void WriteJsonContent(PipelineCallContext context, Uri image)
        {
            Url url = image.ToString();
            int contentLength = s_jsonFront.Length + url.Bytes.Length + s_jsonBack.Length;
            context.AddHeader(Header.Common.CreateContentLength(contentLength));

            // write content
            // TODO (pri 3): this should use a writer
            var writer = context.ContentWriter;
            var buffer = writer.GetSpan(contentLength);
            s_jsonFront.CopyTo(buffer);
            url.Bytes.CopyTo(buffer.Slice(s_jsonFront.Length));
            s_jsonBack.CopyTo(buffer.Slice(s_jsonFront.Length + url.Bytes.Length));
            writer.Advance(contentLength);
        }

        static void SetContentStream(PipelineCallContext context, string imagePath)
        {
            byte[] temp = new byte[4096];
            var stream = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
            
            var writer = context.ContentWriter;
            writer.WriteFrom(stream);
            context.AddHeader(Header.Common.CreateContentLength(stream.Length));
        }

        #region string table
        // this won't be needed once we have UTF8 string literals
        readonly static byte[] s_keyHeaderName = Encoding.ASCII.GetBytes("Ocp-Apim-Subscription-Key");
        readonly static byte[] s_detectMethod = Encoding.ASCII.GetBytes("/detect?");
        readonly static byte[] s_jsonFront = Encoding.ASCII.GetBytes(@"{""url"": """);
        readonly static byte[] s_jsonBack = Encoding.ASCII.GetBytes(@"""}");
        static readonly byte[] s_contentLength = Encoding.ASCII.GetBytes("Content-Length");
        static readonly byte[] s_contentType = Encoding.ASCII.GetBytes("Content-Type");
        #endregion
        #region nobody wants to see these
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj) => base.Equals(obj);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode() => base.GetHashCode();

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override string ToString() => base.ToString();
        #endregion
    }
}
