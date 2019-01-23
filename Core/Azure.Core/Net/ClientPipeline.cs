// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for
// license information.

using Azure.Core.Http.Pipeline;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.Core.Http
{
    public struct HttpPipeline
    {
        PipelinePolicy[] _pipeline;
        int _pipelineCount;

        ReadOnlyMemory<PipelinePolicy> Pipeline {
            get {
                var index = _pipeline.Length - _pipelineCount;
                return _pipeline.AsMemory(index, _pipelineCount);
            }
        }

        PipelineTransport Transport {
            get => (PipelineTransport)_pipeline[_pipeline.Length - 1];
            set {
                _pipeline[_pipeline.Length - 1] = value;
            }
        }

        internal HttpPipeline(PipelineTransport transport)
        {
            _pipeline = new PipelinePolicy[4];
            _pipeline[_pipeline.Length - 1] = transport;
            _pipelineCount = 1;
        }

        internal HttpPipeline(PipelineTransport transport, params PipelinePolicy[] policies)
            : this(transport)
        {
            foreach (var policy in policies) AddPolicy(policy);
        }

        internal PipelinePolicy AddPolicy(PipelinePolicy policy)
        {
            var index = _pipeline.Length - _pipelineCount - 1;
            if (index < 0)
            {
                var larger = new PipelinePolicy[_pipeline.Length * 2];
                Array.Copy(_pipeline, 0, larger, _pipeline.Length, _pipeline.Length);
                _pipeline = larger;
                index = _pipeline.Length - _pipelineCount - 1;
            }
            _pipeline[index] = policy;
            _pipelineCount++;
            return policy;
        }

        public static HttpPipeline Create(PipelineOptions options, string sdkName, string sdkVersion)
        {
            var ua = HttpHeader.Common.CreateUserAgent(sdkName, sdkVersion, options.ApplicationId);

            var pipeline = new HttpPipeline(
                options.Transport,
                options.LoggingPolicy,
                options.RetryPolicy,
                new TelemetryPolicy(ua)
            );
            return pipeline;
        }

        public HttpMessage CreateMessage(PipelineOptions options, CancellationToken cancellation)
            => Transport.CreateMessage(options, cancellation);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task ProcessAsync(HttpMessage message)
            => await PipelinePolicy.ProcessNextAsync(Pipeline, message).ConfigureAwait(false);
    }
}

