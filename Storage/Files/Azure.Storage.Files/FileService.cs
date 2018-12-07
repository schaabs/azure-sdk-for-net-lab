using Azure.Core;
using Azure.Core.Net;
using Azure.Core.Net.Pipeline;
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

        static readonly Func<ServiceResponse, Stream> s_parser = (response) => {
            return new ResponseStream(response);
            throw new Exception("invalid response content");
        };

        readonly Uri _baseUri;

        public ServicePipeline Pipeline { get; }

        public FileUri(string file)
        {
            _baseUri = new Uri(file);
            Pipeline = ServicePipeline.Create(SdkName, SdkVersion);
        }

        public async Task<Response> CreateAsync(CancellationToken cancellation)
        {
            Url url = new Url(_baseUri);

            ServiceCallContext context = null;
            try {
                context = Pipeline.CreateContext(cancellation, ServiceMethod.Put, url);

                await Pipeline.ProcessAsync(context).ConfigureAwait(false);

                return new Response(context.Response);
            }
            catch {
                if (context != null) context.Dispose();
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

            Url url = new Url(_baseUri);

            ServiceCallContext context = null;
            try {
                context = Pipeline.CreateContext(cancellation, ServiceMethod.Put, url);

                context.AddHeader(Header.Common.CreateContentLength(content.Length));
                context.AddHeader(Header.Common.OctetStreamContentType);
                context.RequestContentSource = content;

                await Pipeline.ProcessAsync(context).ConfigureAwait(false);

                return new Response(context.Response);
            }
            catch {
                if (context != null) context.Dispose();
                throw;
            }
        }

        public async Task<Response<Stream>> GetAsync(CancellationToken cancellation)
        {
            Url url = new Url(_baseUri);

            ServiceCallContext context = null;
            try {
                context = Pipeline.CreateContext(cancellation, ServiceMethod.Get, url);

                await Pipeline.ProcessAsync(context).ConfigureAwait(false);

                return new Response<Stream>(context.Response, s_parser);
            }
            catch {
                if (context != null) context.Dispose();
                throw;
            }
        }
    }

    class ResponseStream : Stream
    {
        ServiceResponse _response;
        long _length;
        public ResponseStream(ServiceResponse response)
        {
            _response = response;
            if (!_response.TryGetHeader(Header.Constants.ContentLength, out _length)) {
                throw new Exception("no content length");
            }
        }

        public override bool CanRead => true;

        public override bool CanSeek => throw new NotImplementedException();

        public override bool CanWrite => throw new NotImplementedException();

        public override long Length => throw new NotImplementedException();

        public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var task = ReadAsync(buffer, offset, count);
            return task.Result;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            // TODO (pri 1): how do I pass the cancellation token?
            var result = await _response.ReadContentAsync(1); // TODO (pri 0): this 1 is a hack
            result.CopyTo(buffer.AsSpan(offset, count));
            // TODO (pri 0): how do I advance?
            return (int)result.Length;
        }
    }
}
