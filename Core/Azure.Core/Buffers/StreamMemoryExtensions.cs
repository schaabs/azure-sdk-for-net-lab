using System;
using System.Buffers;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.Core.Buffers
{
    public static class StreamMemoryExtensions
    {
        public static async Task WriteAsync(this Stream stream, ReadOnlySequence<byte> buffer, CancellationToken cancellation)
        {
            if (buffer.Length == 0) return;
            byte[] array = null;
            try
            {
                foreach (var segment in buffer)
                {
                    if (MemoryMarshal.TryGetArray(segment, out var arraySegment))
                    {
                        await stream.WriteAsync(arraySegment.Array, arraySegment.Offset, arraySegment.Count, cancellation).ConfigureAwait(false);
                    }
                    else
                    {
                        if (array == null || buffer.Length < segment.Length)
                        {
                            if (array != null) ArrayPool<byte>.Shared.Return(array);
                            array = ArrayPool<byte>.Shared.Rent(segment.Length);
                        }
                        if (!segment.TryCopyTo(array)) throw new Exception("could not rent buffer large enough");
                        await stream.WriteAsync(array, 0, segment.Length, cancellation).ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                if (array != null) ArrayPool<byte>.Shared.Return(array);
            }
        }

        public static async Task<int> ReadAsync(this Stream stream, Memory<byte> buffer, CancellationToken cancellation)
        {
            if (!MemoryMarshal.TryGetArray(buffer, out ArraySegment<byte> segment))
            {
                throw new NotImplementedException();
            }
            var read = await stream.ReadAsync(segment.Array, 0, segment.Count, cancellation).ConfigureAwait(false);
            return read;
        }
    }
}
