﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for
// license information.

using System;
using System.Buffers.Text;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading;
using static System.Buffers.Text.Encodings;

namespace Azure.Core.Http
{
    public abstract class HttpMessage  : IDisposable
    {
        internal OptionsStore _options = new OptionsStore();

        public CancellationToken Cancellation { get; }

        public PipelineMessageOptions Options => new PipelineMessageOptions(this);

        protected HttpMessage(CancellationToken cancellation)
        {
            Cancellation = cancellation;
        }

        // TODO (pri 1): what happens if this is called after AddHeader? Especially for SocketTransport
        public abstract void SetRequestLine(PipelineMethod method, Uri uri);

        public abstract void AddHeader(HttpHeader header);

        public virtual void AddHeader(string name, string value)
            => AddHeader(new HttpHeader(name, value));

        public abstract void SetContent(PipelineContent content);

        // response
        public PipelineResponse Response => new PipelineResponse(this);

        // make many of these protected internal
        protected internal abstract int Status { get; }

        protected internal abstract bool TryGetHeader(ReadOnlySpan<byte> name, out ReadOnlySpan<byte> value);

        protected internal abstract Stream ResponseContentStream { get; }

        public virtual void Dispose() => _options.Clear();

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj) => base.Equals(obj);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode() => base.GetHashCode();

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override string ToString() => base.ToString();
    }

    public readonly struct PipelineResponse
    {
        readonly HttpMessage _message;

        public PipelineResponse(HttpMessage message)
            => _message = message;

        public int Status => _message.Status;

        public Stream ContentStream => _message.ResponseContentStream;
        
        public bool TryGetHeader(ReadOnlySpan<byte> name, out long value)
        {
            value = default;
            if (!TryGetHeader(name, out ReadOnlySpan<byte> bytes)) return false;
            if (!Utf8Parser.TryParse(bytes, out value, out int consumed) || consumed != bytes.Length)
                throw new Exception("bad content-length value");
            return true;
        }

        public bool TryGetHeader(ReadOnlySpan<byte> name, out ReadOnlySpan<byte> value)
            => _message.TryGetHeader(name, out value);

        public bool TryGetHeader(ReadOnlySpan<byte> name, out string value)
        {
            if(TryGetHeader(name, out ReadOnlySpan<byte> span)) {
                value = Utf8.ToString(span);
                return true;
            }
            value = default;
            return false;
        }

        public bool TryGetHeader(string name, out long value)
        {
            value = default;
            if (!TryGetHeader(name, out string valueString)) return false;
            if (!long.TryParse(valueString, out value))
                throw new Exception("bad content-length value");
            return true;
        }

        public bool TryGetHeader(string name, out string value)
        {
            var utf8Name = Encoding.ASCII.GetBytes(name);
            return TryGetHeader(utf8Name, out value);
        }

        public void Dispose() => _message.Dispose();

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj) => base.Equals(obj);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode() => base.GetHashCode();

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override string ToString()
        {
            var responseStream = _message.ResponseContentStream;
            if (responseStream.CanSeek)
            {
                var position = responseStream.Position;
                var reader = new StreamReader(responseStream);
                var result = $"{Status} {reader.ReadToEnd()}";
                responseStream.Seek(position, SeekOrigin.Begin);
                return result;
            }

            return $"Status : {Status.ToString()}";
        }
    }
}
