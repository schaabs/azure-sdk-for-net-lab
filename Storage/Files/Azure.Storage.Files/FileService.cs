// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for
// license information.

using Azure.Core;
using Azure.Core.Http;
using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.Storage.Files
{
    public partial class FileUri
    {
        const string SdkName = "Azure.Storage.Files";
        const string SdkVersion = "v3.0demo";
        static HttpHeader s_defaultUAHeader = HttpHeader.Common.CreateUserAgent(SdkName, SdkVersion, null);
    
        static readonly Func<PipelineResponse, Stream> s_parser = (response) => {
            return response.ContentStream;
        };

        readonly Uri _baseUri;
        PipelineOptions _options;
        HttpPipeline Pipeline;

        public FileUri(string file, PipelineOptions options = default)
        {
            if (options == default) _options = new PipelineOptions();
            else _options = options;

            Pipeline = HttpPipeline.Create(_options, SdkName, SdkVersion);
            _baseUri = new Uri(file);
        }

        public async Task<Response> CreateAsync(CancellationToken cancellation)
        {
            HttpMessage message = null;
            try {
                message = Pipeline.CreateMessage(_options, cancellation);
                message.SetRequestLine(PipelineMethod.Put, _baseUri);

                await Pipeline.ProcessAsync(message).ConfigureAwait(false);

                return new Response(message.Response);
            }
            catch {
                if (message != null) message.Dispose();
                throw;
            }
        }

        public async Task<Response> PutRangeAsync(long index, int length, Stream content, CancellationToken cancellation)
        {
            if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));
            if (length < 0 || length > 4 * 1024 * 1024) throw new ArgumentOutOfRangeException(nameof(length));
            if (content == null) throw new ArgumentNullException(nameof(content));
            if (content.CanRead == false) throw new ArgumentOutOfRangeException(nameof(content));
            if (content.CanSeek == false) throw new ArgumentOutOfRangeException(nameof(content));

            HttpMessage message = null;
            try {
                message = Pipeline.CreateMessage(_options, cancellation);
                message.SetRequestLine(PipelineMethod.Put, _baseUri);

                message.AddHeader(HttpHeader.Common.CreateContentLength(content.Length));
                message.AddHeader(HttpHeader.Common.OctetStreamContentType);
                message.SetContent(PipelineContent.Create(content));

                await Pipeline.ProcessAsync(message).ConfigureAwait(false);

                return new Response(message.Response);
            }
            catch {
                if (message != null) message.Dispose();
                throw;
            }
        }

        public async Task<Response<Stream>> GetAsync(CancellationToken cancellation)
        {
            HttpMessage message = null;
            try {
                message = Pipeline.CreateMessage(_options, cancellation);
                message.SetRequestLine(PipelineMethod.Get, _baseUri);

                await Pipeline.ProcessAsync(message).ConfigureAwait(false);

                return new Response<Stream>(message.Response, s_parser);
            }
            catch {
                if (message != null) message.Dispose();
                throw;
            }
        }
    }
}
