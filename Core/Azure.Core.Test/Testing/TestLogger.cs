// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for
// license information.

using Azure.Core.Net;
using Azure.Core.Net.Pipeline;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Azure.Core.Testing
{
    public class TestLoggingPolicy : PipelinePolicy
    {
        StringBuilder _logged = new StringBuilder();

        public override async Task ProcessAsync(PipelineCallContext context, ReadOnlyMemory<PipelinePolicy> pipeline)
        {
            _logged.Append($"REQUEST: {context.ToString()}\n");
            await ProcessNextAsync(pipeline, context).ConfigureAwait(false);
            _logged.Append($"RESPONSE: {context.Response.Status}\n");
        }

        public override string ToString()
            => _logged.ToString();
    }
}
