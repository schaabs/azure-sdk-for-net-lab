﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for
// license information.

using System;
using System.ComponentModel;

namespace Azure.Core.Http
{
    public readonly struct PipelineMessageOptions
    {
        readonly HttpMessage _message;

        public PipelineMessageOptions(HttpMessage message)
            => _message = message;

        public void SetOption(object key, long value)
            => _message._options.SetOption(key, value);

        public void SetOption(object key, object value)
            => _message._options.SetOption(key, value);

        public bool TryGetOption(object key, out object value)
            => _message._options.TryGetOption(key, out value);

        public bool TryGetOption(object key, out long value)
            => _message._options.TryGetOption(key, out value);

        public long GetInt64(object key)
            => _message._options.GetInt64(key);

        public object GetObject(object key)
            => _message._options.GetInt64(key);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj) => base.Equals(obj);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode() => base.GetHashCode();

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override string ToString() => base.ToString();
    }
}
