// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for
// license information.

using Azure.Core;
using Azure.Core.Buffers;
using Azure.Core.Http;
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

        HttpPipeline _client;
        PipelineOptions _options;
        Uri _baseUrl;
        HttpHeader _keyHeader;

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
            _keyHeader = new HttpHeader(s_keyHeaderName, key);
            _client = HttpPipeline.Create(options, SdkName, SdkVersion);
        }

        // TODO (pri 2): I think I want to change it such that response details are a property on the deserialized type, but then the deserialization would be eager.
        public Task<Response<FaceDetectResult>> DetectAsync(CancellationToken cancellation, Uri image)
            => DetectAsync(cancellation, image, default);

        public async Task<Response<FaceDetectResult>> DetectAsync(CancellationToken cancellation, Uri image, FaceDetectOptions options)
        {
            if (options == null) options = new FaceDetectOptions();
            Uri uri = BuildUri(options);

            HttpMessage message = null;
            try
            {
                message = _client.CreateMessage(_options, cancellation);

                message.SetRequestLine(PipelineMethod.Post, uri);
                message.AddHeader(_keyHeader);
                message.AddHeader(HttpHeader.Common.JsonContentType);
                message.SetContent(new FaceContent(image, message));

                await _client.ProcessAsync(message).ConfigureAwait(false);

                Response response = message.Response;
                if (!response.TryGetHeader(s_contentLength, out long contentLength)) {
                    throw new Exception("bad response: no content length header");
                }

                var buffer = new byte[contentLength];
                var read = await response.ContentStream.ReadAsync(buffer, cancellation);

                Func<Response, FaceDetectResult> contentParser = null;
                if (response.Status == 200) {
                    contentParser = (rsp) => { return FaceDetectResult.Parse(new ReadOnlySequence<byte>(buffer, 0, read)); };
                }
                return new Response<FaceDetectResult>(response, contentParser);
            }
            catch
            {
                if (message != null) message.Dispose();
                throw;
            }
        }

        public Task<Response<FaceDetectResult>> DetectAsync(CancellationToken cancellation, string imagePath)
            => DetectAsync(cancellation, imagePath, default);

        public async Task<Response<FaceDetectResult>> DetectAsync(CancellationToken cancellation, string imagePath, FaceDetectOptions options)
        {
            if (options == null) options = new FaceDetectOptions();
            Uri uri = BuildUri(options);

            HttpMessage message = null;
            try
            {
                message = _client.CreateMessage(_options, cancellation);
                message.SetRequestLine(PipelineMethod.Post, uri);

                message.AddHeader(_keyHeader);
                message.AddHeader(HttpHeader.Common.OctetStreamContentType);

                SetContentStream(message, imagePath);

                await _client.ProcessAsync(message).ConfigureAwait(false);

                Response response = message.Response;
                if (!response.TryGetHeader(s_contentLength, out long contentLength)) {
                    throw new Exception("bad response: no content length header");
                }

                var buffer = new byte[contentLength];
                var read = await response.ContentStream.ReadAsync(buffer, cancellation);

                Func<Response, FaceDetectResult> contentParser = null;
                if (response.Status == 200)
                {
                    contentParser = (rsp) => { return FaceDetectResult.Parse(new ReadOnlySequence<byte>(buffer, 0, read)); };
                }
                return new Response<FaceDetectResult>(response, contentParser);
            }
            catch
            {
                if (message != null) message.Dispose();
                throw;
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public async Task<Response<Stream>> DetectLazyAsync(CancellationToken cancellation, Uri image, FaceDetectOptions options = default)
        {
            if (options == null) options = new FaceDetectOptions();
            Uri uri = BuildUri(options);

            HttpMessage message = null;
            try
            {
                message = _client.CreateMessage(_options, cancellation);
                message.SetRequestLine(PipelineMethod.Post, uri);

                message.AddHeader(_keyHeader);
                message.AddHeader(HttpHeader.Common.JsonContentType);
                message.SetContent(new FaceContent(image, message));

                await _client.ProcessAsync(message).ConfigureAwait(false);

                return new Response<Stream>(message.Response, message.Response.ContentStream);
            }
            catch
            {
                if (message != null) message.Dispose();
                throw;
            }
        }

        public struct FaceServiceOptions
        {
            internal HttpHeader UserAgentHeader;
            internal string ApiVersion;
            string _applicationId;

            public FaceServiceOptions(string apiVersion)
            {
                ApiVersion = apiVersion;
                _applicationId = default;
                UserAgentHeader = HttpHeader.Common.CreateUserAgent(sdkName: "Azure-CognitiveServices-Face", sdkVersion: "1.0.0", _applicationId);
            }

            public string ApplicationId
            {
                get { return _applicationId; }
                set {
                    if (string.Equals(_applicationId, value, StringComparison.Ordinal)) return;
                    _applicationId = value;
                    UserAgentHeader = HttpHeader.Common.CreateUserAgent(sdkName: "Azure-CognitiveServices-Face", sdkVersion: "1.0.0", _applicationId);
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
            HttpMessage _message;
            static int s_len = @"{""url"": """.Length + @"""}".Length;

            public FaceContent(Uri image, HttpMessage message)
            {
                _image = image;
                _message = message;
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

        static void SetContentStream(HttpMessage message, string imagePath)
        {
            byte[] temp = new byte[4096];
            var stream = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
            message.SetContent(PipelineContent.Create(stream));
            message.AddHeader(HttpHeader.Common.CreateContentLength(stream.Length));
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
