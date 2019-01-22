// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for
// license information.

using Azure.Core.Http.Pipeline;
using System;
using System.Buffers;
using System.ComponentModel;

namespace Azure.Core.Http
{
    public class PipelineOptions
    {
        static readonly PipelinePolicy s_defaultLoggingPolicy = new LoggingPolicy();
        // TODO (pri 2): what are the default status codes to retry?
        static readonly PipelinePolicy s_defaultRetryPolicy = Pipeline.RetryPolicy.CreateFixed(3, TimeSpan.Zero, 
            500, // Internal Server Error 
            504  // Gateway Timeout
        );

        static readonly PipelineTransport s_defaultTransport = new HttpPipelineTransport();

        public ArrayPool<byte> Pool { get; set; } = ArrayPool<byte>.Shared;

        public PipelineTransport Transport { get; set; } = s_defaultTransport;

        public PipelinePolicy LoggingPolicy { get; set; } = s_defaultLoggingPolicy;

        public PipelinePolicy RetryPolicy { get; set; } = s_defaultRetryPolicy;

        public string ApplicationId { get; set; }

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

