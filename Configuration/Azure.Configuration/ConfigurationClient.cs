﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for
// license information.

using Azure.Core;
using Azure.Core.Http;
using System;
using System.Threading;
using System.Threading.Tasks;

// TODO (pri 1): Add all functionality from the spec: https://msazure.visualstudio.com/Azure%20AppConfig/Azure%20AppConfig%20Team/_git/AppConfigService?path=%2Fdocs%2Fprotocol&version=GBdev
// TODO (pri 1): Support "List subset of keys" 
// TODO (pri 1): Support "Time-Based Access" 
// TODO (pri 1): Support "KeyValue Revisions"
// TODO (pri 1): Support "Real-time Consistency"
// TODO (pri 2): Add support for filters (fields, label, etc.)
// TODO (pri 2): Make sure the whole object gets deserialized/serialized.
// TODO (pri 3): Add retry policy with automatic throttling
namespace Azure.ApplicationModel.Configuration
{
    public partial class ConfigurationClient
    {
        const string SdkName = "Azure.Configuration";
        const string SdkVersion = "1.0.0";

        readonly Uri _baseUri;
        readonly string _credential;
        readonly byte[] _secret;
        PipelineOptions _options;
        HttpPipeline Pipeline;

        public ConfigurationClient(string connectionString)
            : this(connectionString, options: new PipelineOptions())
        {
        }

        public ConfigurationClient(string connectionString, PipelineOptions options)
        {
            if (connectionString == null) throw new ArgumentNullException(nameof(connectionString));
            if (options == null) throw new ArgumentNullException(nameof(options));

            _options = options;
            Pipeline = HttpPipeline.Create(_options, SdkName, SdkVersion);
            ParseConnectionString(connectionString, out _baseUri, out _credential, out _secret);
        }

        public async Task<Response<ConfigurationSetting>> AddAsync(ConfigurationSetting setting, CancellationToken cancellation = default)
        {
            if (setting == null) throw new ArgumentNullException(nameof(setting));
            if (string.IsNullOrEmpty(setting.Key)) throw new ArgumentNullException($"{nameof(setting)}.{nameof(setting.Key)}");

            Uri uri = BuildUriForKvRoute(setting);

            using (HttpMessage message = Pipeline.CreateMessage(_options, cancellation))
            {
                ReadOnlyMemory<byte> content = Serialize(setting);

                message.SetRequestLine(PipelineMethod.Put, uri);

                message.AddHeader("Host", uri.Host);
                message.AddHeader(IfNoneMatchWildcard);
                message.AddHeader(MediaTypeKeyValueApplicationHeader);
                message.AddHeader(HttpHeader.Common.JsonContentType);
                message.AddHeader(HttpHeader.Common.CreateContentLength(content.Length));
                AddAuthenticationHeaders(message, uri, PipelineMethod.Put, content, _secret, _credential);

                message.SetContent(PipelineContent.Create(content));

                await Pipeline.ProcessAsync(message).ConfigureAwait(false);

                return await CreateResponse(message);
            }
        }

        public async Task<Response<ConfigurationSetting>> SetAsync(ConfigurationSetting setting, CancellationToken cancellation = default)
        {
            if (setting == null) throw new ArgumentNullException(nameof(setting));
            if (string.IsNullOrEmpty(setting.Key)) throw new ArgumentNullException($"{nameof(setting)}.{nameof(setting.Key)}");

            Uri uri = BuildUriForKvRoute(setting);

            using (HttpMessage message = Pipeline.CreateMessage(_options, cancellation))
            {
                ReadOnlyMemory<byte> content = Serialize(setting);

                message.SetRequestLine(PipelineMethod.Put, uri);

                message.AddHeader("Host", uri.Host);
                message.AddHeader(MediaTypeKeyValueApplicationHeader);
                message.AddHeader(HttpHeader.Common.JsonContentType);
                message.AddHeader(HttpHeader.Common.CreateContentLength(content.Length));
                AddAuthenticationHeaders(message, uri, PipelineMethod.Put, content, _secret, _credential);

                message.SetContent(PipelineContent.Create(content));

                await Pipeline.ProcessAsync(message).ConfigureAwait(false);

                return await CreateResponse(message);
            }
        }

        public async Task<Response<ConfigurationSetting>> UpdateAsync(ConfigurationSetting setting, CancellationToken cancellation = default)
        {
            if (setting == null) throw new ArgumentNullException(nameof(setting));
            if (string.IsNullOrEmpty(setting.Key)) throw new ArgumentNullException($"{nameof(setting)}.{nameof(setting.Key)}");
            if (string.IsNullOrEmpty(setting.ETag)) throw new ArgumentNullException($"{nameof(setting)}.{nameof(setting.ETag)}");

            Uri uri = BuildUriForKvRoute(setting);

            using (HttpMessage message = Pipeline.CreateMessage(_options, cancellation))
            {
                ReadOnlyMemory<byte> content = Serialize(setting);

                message.SetRequestLine(PipelineMethod.Put, uri);

                message.AddHeader("Host", uri.Host);
                message.AddHeader(IfMatchName, $"\"{setting.ETag}\"");
                message.AddHeader(MediaTypeKeyValueApplicationHeader);
                message.AddHeader(HttpHeader.Common.JsonContentType);
                message.AddHeader(HttpHeader.Common.CreateContentLength(content.Length));
                AddAuthenticationHeaders(message, uri, PipelineMethod.Put, content, _secret, _credential);

                message.SetContent(PipelineContent.Create(content));

                await Pipeline.ProcessAsync(message).ConfigureAwait(false);

                return await CreateResponse(message);
            }
        }

        public async Task<Response<ConfigurationSetting>> DeleteAsync(string key, SettingFilter filter = null, CancellationToken cancellation = default)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            Uri uri = BuildUriForKvRoute(key, filter);

            using (HttpMessage message = Pipeline.CreateMessage(_options, cancellation))
            {
                message.SetRequestLine(PipelineMethod.Delete, uri);

                message.AddHeader("Host", uri.Host);
                AddFilterHeaders(filter, message);
                AddAuthenticationHeaders(message, uri, PipelineMethod.Delete, content: default, _secret, _credential);

                await Pipeline.ProcessAsync(message).ConfigureAwait(false);

                return await CreateResponse(message);
            }
        }

        public async Task<Response<ConfigurationSetting>> LockAsync(string key, SettingFilter filter = null, CancellationToken cancellation = default)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            Uri uri = BuildUriForLocksRoute(key, filter);

            using (HttpMessage message = Pipeline.CreateMessage(_options, cancellation))
            {
                message.SetRequestLine(PipelineMethod.Put, uri);

                message.AddHeader("Host", uri.Host);
                AddFilterHeaders(filter, message);
                AddAuthenticationHeaders(message, uri, PipelineMethod.Put, content: default, _secret, _credential);

                await Pipeline.ProcessAsync(message).ConfigureAwait(false);

                return await CreateResponse(message);
            }
        }

        public async Task<Response<ConfigurationSetting>> UnlockAsync(string key, SettingFilter filter = null, CancellationToken cancellation = default)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            Uri uri = BuildUriForLocksRoute(key, filter);

            using (HttpMessage message = Pipeline.CreateMessage(_options, cancellation))
            {
                message.SetRequestLine(PipelineMethod.Delete, uri);

                message.AddHeader("Host", uri.Host);
                AddFilterHeaders(filter, message);
                AddAuthenticationHeaders(message, uri, PipelineMethod.Delete, content: default, _secret, _credential);

                await Pipeline.ProcessAsync(message).ConfigureAwait(false);

                return await CreateResponse(message);
            }
        }

        public async Task<Response<ConfigurationSetting>> GetAsync(string key, SettingFilter filter = null, CancellationToken cancellation = default)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException($"{nameof(key)}");

            Uri uri = BuildUriForKvRoute(key, filter);

            using (HttpMessage message = Pipeline.CreateMessage(_options, cancellation))
            {
                message.SetRequestLine(PipelineMethod.Get, uri);

                message.AddHeader("Host", uri.Host);
                message.AddHeader(MediaTypeKeyValueApplicationHeader);
                AddFilterHeaders(filter, message);
                message.AddHeader(HttpHeader.Common.JsonContentType);

                AddAuthenticationHeaders(message, uri, PipelineMethod.Get, content: default, _secret, _credential);

                await Pipeline.ProcessAsync(message).ConfigureAwait(false);

                return await CreateResponse(message);
            }
        }

        public async Task<Response<SettingBatch>> GetBatchAsync(SettingBatchFilter filter, CancellationToken cancellation = default)
        {
            var uri = BuildUriForGetBatch(filter);

            using (HttpMessage message = Pipeline.CreateMessage(_options, cancellation))
            {
                message.SetRequestLine(PipelineMethod.Get, uri);

                message.AddHeader("Host", uri.Host);
                message.AddHeader(MediaTypeKeyValueApplicationHeader);
                if (filter.Revision != null)
                {
                    message.AddHeader(AcceptDatetimeHeader, filter.Revision.Value.UtcDateTime.ToString(AcceptDateTimeFormat));
                }

                await Pipeline.ProcessAsync(message).ConfigureAwait(false);

                PipelineResponse response = message.Response;
                if (!response.TryGetHeader(HttpHeader.Constants.ContentLength, out long contentLength))
                {
                    throw new Exception("bad response: no content length header");
                }

                if (response.Status != 200)
                {
                    return new Response<SettingBatch>(response);
                }

                var batch = await ConfigurationServiceSerializer.ParseBatchAsync(response, cancellation);
                return new Response<SettingBatch>(response, batch);
            }
        }

        public async Task<Response<SettingBatch>> GetRevisionsAsync(SettingBatchFilter filter, CancellationToken cancellation = default)
        {
            var uri = BuildUriForRevisions(filter);

            using (HttpMessage message = Pipeline.CreateMessage(_options, cancellation))
            {
                message.SetRequestLine(PipelineMethod.Get, uri);

                message.AddHeader("Host", uri.Host);
                message.AddHeader(MediaTypeKeyValueApplicationHeader);
                if (filter.Revision != null)
                {
                    message.AddHeader(AcceptDatetimeHeader, filter.Revision.Value.UtcDateTime.ToString(AcceptDateTimeFormat));
                }

                AddAuthenticationHeaders(message, uri, PipelineMethod.Get, content: default, _secret, _credential);

                await Pipeline.ProcessAsync(message).ConfigureAwait(false);

                PipelineResponse response = message.Response;
                
                if (response.Status != 200)
                {
                    return new Response<SettingBatch>(response);
                }

                var batch = await ConfigurationServiceSerializer.ParseBatchAsync(response, cancellation);
                return new Response<SettingBatch>(response, batch);
            }
        }
    }
}
