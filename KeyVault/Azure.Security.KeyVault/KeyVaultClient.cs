using System;
using Azure.Core;
using Azure.Core.Net;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core.Http;
using System.Collections.Generic;

namespace Azure.Security.KeyVault
{
    public class KeyVaultClient
    {
        private readonly Uri _vaultUri;
        private const string SdkName = "Azure.Security.KeyVault";
        
        private const string SdkVersion = "1.0.0";

        private const string secretRoute = "/secrets/";

        private readonly TokenCredential _credentials;
        
        private readonly PipelineOptions _options;

        private readonly HttpPipeline _pipeline;

        public KeyVaultClient(TokenCredential credentials)
            : this (credentials, new PipelineOptions())
        {

        }

        public KeyVaultClient(TokenCredential credentials, PipelineOptions options)
        {
            if (credentials == null) throw new ArgumentNullException(nameof(credentials));
            if (options == null) throw new ArgumentNullException(nameof(options));

            _options = options;
            _pipeline = HttpPipeline.Create(_options, SdkName, SdkVersion);
        }

        public async Task<Response<Secret>> SetSecretAsync(string name, string value, string contentType = null, VaultObjectAttributes attributes = null, IDictionary<string, string> tags = null, CancellationToken cancellation = default)
        {
            if(string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            if(string.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(value));

            Uri secretUri = new Uri(_vaultUri, secretRoute + name);
            
            using (HttpMessage message = _pipeline.CreateMessage(_options, cancellation))
            {
                message.SetRequestLine(PipelineMethod.Put, secretUri);
                message.AddHeader("Host", secretUri.Host);
                message.AddHeader("Accept", "application/json");
                message.AddHeader("Content-Type", "application/json; charset=utf-8");
                message.AddHeader("Authorization", "Bearer " + _credentials.GetToken());

                var secret = new Secret() 
                { 
                    Value = value, 
                    ContentType = contentType,
                    Attributes = attributes,
                    Tags = tags
                };

                message.SetContent(PipelineContent.Create(secret.Serialize()));

                // todo: shouldn't this take in the cancellation token
                await _pipeline.ProcessAsync(message);

                PipelineResponse response = message.Response;

                if (response.Status != 200)
                {
                    return new Response<Secret>(response);
                }

                secret.Deserialize(response.ContentStream);

                return new Response<Secret>(response, secret);
            }


        }


    }
}
