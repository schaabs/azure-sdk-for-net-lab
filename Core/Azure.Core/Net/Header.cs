using System;
using System.Runtime.InteropServices;
using System.Text;
using static System.Buffers.Text.Encodings;
using System.Buffers;

namespace Azure.Core.Net
{
    public struct Header
    {
        byte[] _utf8;

        public Header(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
        {
            int length = name.Length + value.Length + 3;
            _utf8 = new byte[length];
            var span = _utf8.AsSpan();
            name.CopyTo(_utf8);
            _utf8[name.Length] = (byte)':';
            value.CopyTo(span.Slice(name.Length + 1));
            _utf8[length - 2] = (byte)'\r';
            _utf8[length - 1] = (byte)'\n';
        }

        public Header(ReadOnlySpan<byte> name, string value)
        {
            int length = name.Length + value.Length + 3;
            _utf8 = new byte[length];
            var utf8 = _utf8.AsSpan();

            name.CopyTo(_utf8);
            _utf8[name.Length] = (byte)':';

            utf8 = utf8.Slice(name.Length + 1);
            if (Utf8.FromUtf16(MemoryMarshal.AsBytes(value.AsSpan()), utf8, out var consumed, out int written) != OperationStatus.Done)
            {
                throw new Exception("value is not ASCII");
            }
            _utf8[length - 2] = (byte)'\r';
            _utf8[length - 1] = (byte)'\n';
        }

        public Header(string name, string value)
        {
            int length = name.Length + value.Length + 3;
            _utf8 = new byte[length];
            var utf8 = _utf8.AsSpan();

            if (Utf8.FromUtf16(MemoryMarshal.AsBytes(name.AsSpan()), utf8, out var consumed, out int written) != OperationStatus.Done)
            {
                throw new Exception("name is not ASCII");
            }
            _utf8[written] = (byte)':';
            utf8 = utf8.Slice(written + 1);
            if (Utf8.FromUtf16(MemoryMarshal.AsBytes(value.AsSpan()), utf8, out consumed, out written) != OperationStatus.Done)
            {
                throw new Exception("name or value is not ASCII");
            }
            _utf8[length - 2] = (byte)'\r';
            _utf8[length - 1] = (byte)'\n';
        }

        public ReadOnlySpan<byte> Value
        {
            get {
                var span = _utf8.AsSpan();
                var index = span.IndexOf((byte)':');
                while (span[index] == ' ') index++;
                return span.Slice(index + 1, span.Length - index - 3);
            }
        }

        public ReadOnlySpan<byte> Name
        {
            get {
                var span = _utf8.AsSpan();
                var index = span.IndexOf((byte)':');
                while (span[index] == ' ') index--;
                return span.Slice(0, index);
            }
        }

        public override string ToString() => Utf8.ToString(_utf8.AsSpan(0, _utf8.Length - 2));

        public bool TryWrite(Span<byte> buffer, out int written, StandardFormat format = default)
        {
            if (!format.IsDefault) throw new NotSupportedException("format not supported");
            written = 0;
            var utf8 = _utf8.AsSpan();
            if (!utf8.TryCopyTo(buffer)) return false;
            written += utf8.Length;
            return true;
        }

        // TODO (pri 3): eliminate this allocations
        static readonly string PlatfromInformation = $"({RuntimeInformation.FrameworkDescription}; {RuntimeInformation.OSDescription})";

        public static Header CreateUserAgent(string sdkName, string sdkVersion, string applicationId = default)
        {
            byte[] utf8 = null;
            if (applicationId == default) utf8 = Encoding.ASCII.GetBytes($"User-Agent:{sdkName}/{sdkVersion} {PlatfromInformation}\r\n");
            else utf8 = Encoding.ASCII.GetBytes($"User-Agent:{applicationId} {sdkName}/{sdkVersion} {PlatfromInformation}\r\n");
            return new Header() { _utf8 = utf8 };
        }
        public static Header CreateContentLength(long length)
        {
            byte[] utf8 = Encoding.ASCII.GetBytes($"Content-Length:{length}\r\n");
            return new Header() { _utf8 = utf8 };
        }

        static readonly byte[] s_Host = Encoding.ASCII.GetBytes("Host");
        static readonly byte[] s_ContentLength = Encoding.ASCII.GetBytes("Content-Length");
        static readonly byte[] s_ContentType = Encoding.ASCII.GetBytes("Content-Type");

        public static readonly Header JsonContentType = new Header(s_ContentType, "application/json");
        public static readonly Header StreamContentType = new Header(s_ContentType, "application/octet-stream");

        public static Header CreateHost(ReadOnlySpan<byte> hostName)
        {
            var buffer = new byte[s_Host.Length + hostName.Length + 3];
            s_Host.AsSpan().CopyTo(buffer);
            buffer[s_Host.Length] = (byte)':';
            hostName.CopyTo(buffer.AsSpan(s_Host.Length + 1));
            buffer[buffer.Length - 1] = (byte)'\n';
            buffer[buffer.Length - 2] = (byte)'\r';
            return new Header() { _utf8 = buffer };
        }
    }
}
