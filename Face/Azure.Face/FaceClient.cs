// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for
// license information.

using Azure.Core;
using Azure.Core.Buffers;
using Azure.Core.Net;
using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Buffers;

namespace Azure.Face
{
    public class FaceClient
    {
        const string SdkName = "Azure.Face";
        const string SdkVersion = "1.0.0";

        ClientPipeline _client;
        PipelineOptions _options;
        Uri _baseUrl;
        Header _keyHeader;

        // TODO (pri 2): changing Application ID is not really working
        public readonly FaceServiceOptions Options = new FaceServiceOptions("v1.0");

        public FaceClient(string baseUri, string key)
            : this(baseUri, key, new PipelineOptions())
        { }

        public FaceClient(string baseUri, string key, PipelineOptions options) 
            : this(new Uri(baseUri), key, options)
        { }

        public FaceClient(Uri baseUrl, string key)
            : this(baseUrl.ToString(), key, new PipelineOptions())
        { }

        private FaceClient(Uri baseUrl, string key, PipelineOptions options)
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
            Uri uri = BuildUri(options);

            PipelineCallContext context = null;
            try
            {
                context = _client.CreateContext(_options, cancellation);

                context.SetRequestLine(ServiceMethod.Post, uri);
                context.AddHeader(_keyHeader);
                context.AddHeader(Header.Common.JsonContentType);
                context.SetContent(new FaceContent(image, context));

                await _client.ProcessAsync(context).ConfigureAwait(false);

                ServiceResponse response = context.Response;
                if (!response.TryGetHeader(s_contentLength, out long contentLength)) {
                    throw new Exception("bad response: no content length header");
                }

                var buffer = new byte[contentLength];
                var read = await response.ContentStream.ReadAsync(buffer, cancellation);

                Func<ServiceResponse, FaceDetectResult> contentParser = null;
                if (response.Status == 200) {
                    contentParser = (rsp) => { return FaceDetectResult.Parse(new ReadOnlySequence<byte>(buffer, 0, read)); };
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
            Uri uri = BuildUri(options);

            PipelineCallContext context = null;
            try
            {
                context = _client.CreateContext(_options, cancellation);
                context.SetRequestLine(ServiceMethod.Post, uri);

                context.AddHeader(_keyHeader);
                context.AddHeader(Header.Common.OctetStreamContentType);

                SetContentStream(context, imagePath);

                await _client.ProcessAsync(context).ConfigureAwait(false);

                ServiceResponse response = context.Response;
                if (!response.TryGetHeader(s_contentLength, out long contentLength)) {
                    throw new Exception("bad response: no content length header");
                }

                var buffer = new byte[contentLength];
                var read = await response.ContentStream.ReadAsync(buffer, cancellation);

                Func<ServiceResponse, FaceDetectResult> contentParser = null;
                if (response.Status == 200)
                {
                    contentParser = (rsp) => { return FaceDetectResult.Parse(new ReadOnlySequence<byte>(buffer, 0, read)); };
                }
                return new Response<FaceDetectResult>(response, contentParser);
            }
            catch
            {
                if (context != null) context.Dispose();
                throw;
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public async Task<Response<Stream>> DetectLazyAsync(CancellationToken cancellation, Uri image, FaceDetectOptions options = default)
        {
            if (options == null) options = new FaceDetectOptions();
            Uri uri = BuildUri(options);

            PipelineCallContext context = null;
            try
            {
                context = _client.CreateContext(_options, cancellation);
                context.SetRequestLine(ServiceMethod.Post, uri);

                context.AddHeader(_keyHeader);
                context.AddHeader(Header.Common.JsonContentType);
                context.SetContent(new FaceContent(image, context));

                await _client.ProcessAsync(context).ConfigureAwait(false);

                return new Response<Stream>(context.Response, context.Response.ContentStream);
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
            internal string ApiVersion;
            string _applicationId;

            public FaceServiceOptions(string apiVersion)
            {
                ApiVersion = apiVersion;
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

        Uri BuildUri(FaceDetectOptions options)
        {
            var ub = new UriBuilder(_baseUrl);
            ub.Path = ub.Path + Options.ApiVersion + s_detectMethod;
            options.BuildRequestParameters(ub);
            return ub.Uri;
        }

        class FaceContent : PipelineContent
        {
            Uri _image;
            PipelineCallContext _context;
            static int s_len = @"{""url"": """.Length + @"""}".Length;

            public FaceContent(Uri image, PipelineCallContext context)
            {
                _image = image;
                _context = context;
            }

            public override bool TryComputeLength(out long length)
            {
                length = s_len + _image.ToString().Length;
                return true;
            }

            public async override Task WriteTo(Stream stream, CancellationToken cancellation)
            {
                var writer = new StreamWriter(stream);
                writer.Write(@"{""url"": """);
                writer.Write(_image);
                writer.Write(@"""}");
                await writer.FlushAsync().ConfigureAwait(false); 
                await stream.FlushAsync().ConfigureAwait(false);
            }

            public override void Dispose()
            {
            }
        }

        static void SetContentStream(PipelineCallContext context, string imagePath)
        {
            byte[] temp = new byte[4096];
            var stream = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
            context.SetContent(PipelineContent.Create(stream));
            context.AddHeader(Header.Common.CreateContentLength(stream.Length));
        }

        #region string table
        // this won't be needed once we have UTF8 string literals
        readonly static byte[] s_keyHeaderName = Encoding.ASCII.GetBytes("Ocp-Apim-Subscription-Key");
        readonly static string s_detectMethod = "/detect";
        static readonly byte[] s_contentLength = Encoding.ASCII.GetBytes("Content-Length");
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
