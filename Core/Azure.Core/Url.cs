using Azure.Core.Net;
using System;
using System.Text;

namespace Azure.Core
{
    public readonly struct Url
    {
        static readonly byte[] s_http = Encoding.ASCII.GetBytes("http");
        static readonly byte[] s_https = Encoding.ASCII.GetBytes("https");
                
        readonly byte[] _url;
        readonly int _count;

        public bool IsHttps => Bytes.StartsWith(s_https);

        public Url(Uri uri) : this(uri.ToString()) { }

        public Url(string url)
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

        public static implicit operator Url(string url) => new Url(url);

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
}
