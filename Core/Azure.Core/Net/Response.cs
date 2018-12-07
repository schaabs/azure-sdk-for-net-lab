using System;
using System.Buffers;
using System.ComponentModel;
using System.Text;

namespace Azure.Core.Net
{
    public struct Response : IDisposable
    {
        ServiceResponse _response;

        public Response(ServiceResponse response) => _response = response;

        public int Status => _response.Status;

        public void Dispose() => _response.Dispose();       

        public bool TryGetHeader(ReadOnlySpan<byte> name, out ReadOnlySpan<byte> value)
            => _response.TryGetHeader(name, out value);

        public bool TryGetHeader(ReadOnlySpan<byte> name, out long value)
            => _response.TryGetHeader(name, out value);

        public bool TryGetHeader(string name, out string value)
        {
            if (_response.TryGetHeader(Encoding.ASCII.GetBytes(name), out ReadOnlySpan<byte> valueUtf8)) {
                value = Encoding.ASCII.GetString(valueUtf8.ToArray());
                return true;
            }
            value = default;
            return false;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj) => base.Equals(obj);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode() => base.GetHashCode();

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override string ToString() => base.ToString();
    }

    public struct Response<T> : IDisposable
    {
        ServiceResponse _response;
        Func<ServiceResponse, T> _contentParser;
        T _parsedContent;

        public Response(ServiceResponse response)
        {
            _response = response;
            _contentParser = null;
            _parsedContent = default;
        }

        public Response(ServiceResponse response, Func<ServiceResponse, T> parser)
        {
            _response = response;
            _contentParser = parser;
            _parsedContent = default;
        }

        public Response(ServiceResponse response, T parsed)
        {
            _response = response;
            _contentParser = null;
            _parsedContent = parsed;
        }

        public T Result
        {
            get {
                if (_contentParser != null) {
                    _parsedContent = _contentParser(_response);
                    _contentParser = null;
                }
                return _parsedContent;
            }
        }

        public int Status => _response.Status;

        public void Dispose()
        {
            _response.Dispose();
            _contentParser = default;
        }

        public bool TryGetHeader(ReadOnlySpan<byte> name, out ReadOnlySpan<byte> value)
            => _response.TryGetHeader(name, out value);

        public bool TryGetHeader(ReadOnlySpan<byte> name, out long value)
            => _response.TryGetHeader(name, out value);

        public bool TryGetHeader(string name, out string value)
        {
            if (_response.TryGetHeader(Encoding.ASCII.GetBytes(name), out ReadOnlySpan<byte> valueUtf8))
            {
                value = Encoding.ASCII.GetString(valueUtf8.ToArray());
                return true;
            }
            value = default;
            return false;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj) => base.Equals(obj);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode() => base.GetHashCode();

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override string ToString() => base.ToString();
    }
}
