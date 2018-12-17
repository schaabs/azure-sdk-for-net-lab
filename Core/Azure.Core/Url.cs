using Azure.Core.Net;
using System;
using System.Buffers.Text;
using System.Text;

namespace Azure.Core.Net
{
    public readonly struct Url
    {
        static readonly byte[] s_http = Encoding.ASCII.GetBytes("http");
        static readonly byte[] s_https = Encoding.ASCII.GetBytes("https");
                
        readonly byte[] _url;
        readonly int _count;

        public bool IsHttps => Bytes.StartsWith(s_https);

        public Url(Uri uri) : this(uri.ToString()) { }

        internal Url(string url)
        {
            _url = Encoding.ASCII.GetBytes(url);
            _count = _url.Length;
        }

        public Url(ReadOnlySpan<byte> utf8)
        {
            _url = utf8.ToArray();
            _count = _url.Length;
        }

        // TODO (pri 2): this should be reamoved and/or cleaned up
        public Url((byte[] buffer, int length) slice)
        {
            _url = slice.buffer;
            _count = slice.length;
        }

        public ReadOnlySpan<byte> Bytes =>_url.AsSpan(0, _count);
        
        public override string ToString()
            => Encoding.ASCII.GetString(_url, 0, _count);

        public static implicit operator Url(string uri) => new Url(new Uri(uri));
        public static implicit operator Url(Uri uri) => new Url(uri);

        public void Deconstruct(out ReadOnlySpan<byte> Host, out ReadOnlySpan<byte> Path)
        {
            var url = Bytes;

            int protocolEnd = url.IndexOf(s_protocolSeparator) + s_protocolSeparator.Length;
            ReadOnlySpan<byte> hostPathAndQuery = url.Slice(protocolEnd);
            int pathStart = hostPathAndQuery.IndexOf((byte)'/');
            Path = hostPathAndQuery.Slice(pathStart);
            Host = hostPathAndQuery.Slice(0, pathStart);
        }

        public void Deconstruct(out ServiceProtocol Protocol, out ReadOnlySpan<byte> Host, out ReadOnlySpan<byte> Path)
        {
            var url = Bytes;

            int protocolStart = url.IndexOf(s_protocolSeparator);
            int protocolEnd = protocolStart + s_protocolSeparator.Length;
            ReadOnlySpan<byte> hostPathAndQuery = url.Slice(protocolEnd);
            int pathStart = hostPathAndQuery.IndexOf((byte)'/');

            Path = hostPathAndQuery.Slice(pathStart);
            Host = hostPathAndQuery.Slice(0, pathStart);

            var protocol = url.Slice(0, protocolStart);
            if (protocol.SequenceEqual(s_https)) Protocol = ServiceProtocol.Https;
            else if (protocol.SequenceEqual(s_http)) Protocol = ServiceProtocol.Http;
            else Protocol = ServiceProtocol.Other;
        }

        public ReadOnlySpan<byte> Host
        {
            get {
                var url = Bytes;
                int protocolEnd = url.IndexOf(s_protocolSeparator) + s_protocolSeparator.Length;
                ReadOnlySpan<byte> hostPathAndQuery = url.Slice(protocolEnd);
                int pathStart = hostPathAndQuery.IndexOf((byte)'/');
                return hostPathAndQuery.Slice(0, pathStart);
            }
        }

        static readonly byte[] s_protocolSeparator = Encoding.ASCII.GetBytes("://");
    }

    // TODO (pri 1): this whole type needs to escape/validate data when it builds
    // TODO (pri 2): this type should not be calling Encoding.Ascii.GetBytes. It should transcode into the buffer
    public struct UrlWriter
    {
        byte[] _buffer;
        int _commited;
        bool _hasQuery;

        public UrlWriter(int capacity)
        {
            _buffer = new byte[capacity];
            _commited = 0;
            _hasQuery = false;
        }

        public UrlWriter(Url baseUri, int additionalCapacity)
            : this(baseUri.Bytes.Length + additionalCapacity)
        {
            if (additionalCapacity < 0) throw new ArgumentOutOfRangeException(nameof(additionalCapacity));

            var baseUriBytes = baseUri.Bytes;
            baseUriBytes.CopyTo(_buffer);
            _commited = baseUriBytes.Length;
        }

        public void AppendPath(ReadOnlySpan<byte> path)
        {
            if (_hasQuery) throw new InvalidOperationException("query is already written");
            if (path.Length < 1) return;
            bool bufferHasSlash = _buffer[_commited - 1] == '/';
            bool pathHasSlash = path[0] == '/';
            if (bufferHasSlash) {
                if (pathHasSlash) path = path.Slice(1);
                Append(path);
            }
            else
            {
                if (!pathHasSlash) Append('/');
                Append(path);
            }
        }

        public void AppendPath(string path)
            => AppendPath(Encoding.ASCII.GetBytes(path));

        public void AppendQuery(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
        {
            AppendStartQuery(name);
            Append(value);
        }

        public void AppendQuery(ReadOnlySpan<byte> name, string value)
        {
            AppendStartQuery(name);
            Append(Encoding.ASCII.GetBytes(value));
        }

        public void AppendQuery(ReadOnlySpan<byte> name, int value)
        {
            AppendStartQuery(name);
            Append(value);
        }

        private void AppendStartQuery(ReadOnlySpan<byte> name)
        {
            if (_hasQuery)
            {
                Append('&');
            }
            else
            {
                Append('?');
                _hasQuery = true;
            }
            Append(name);
            Append('=');
        }

        public Url ToUrl()
        {
            var url = new Url((_buffer, _commited));
            _buffer = Array.Empty<byte>();
            _commited = 0;
            return url;
        }

        void Append(char ascii)
        {
            if (!IsAscii(ascii)) throw new ArgumentOutOfRangeException(nameof(ascii));
            if (_buffer.Length < _commited - 1) Resize();
            _buffer[_commited++] = (byte)ascii;

            bool IsAscii(char c) => c < 128;
        }

        void Append(ReadOnlySpan<byte> value)
        {
            while (!value.TryCopyTo(Free))
            {
                Resize();
            }
            _commited += value.Length;
        }

        void Append(int value)
        {
            int written;
            while (!Utf8Formatter.TryFormat(value, Free, out written))
            {
                Resize();
            }
            _commited += written;
        }

        Span<byte> Free => _buffer.AsSpan(_commited);
        void Resize()
        {
            var larger = new byte[_buffer.Length * 2];
            _buffer.CopyTo(larger, 0);
            _buffer = larger;
        }

        public override string ToString()
            => Encoding.ASCII.GetString(_buffer, 0, _commited);
    }

    public static class UriExtensions
    {
        public static void AppendQuery(this UriBuilder builder, string name, string value)
        {
            if(!string.IsNullOrEmpty(builder.Query)) {
                builder.Query = builder.Query + "&" + name + "=" + value;
            } 
            else {
                builder.Query = name + "=" + value;
            }
        }

        public static void AppendQuery(this UriBuilder builder, string name, long value)
            => AppendQuery(builder, name, value.ToString());
    }
}
