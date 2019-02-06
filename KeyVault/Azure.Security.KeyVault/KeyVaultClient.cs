using System;
using Azure.Core;
using Azure.Core.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;

namespace Azure.Security.KeyVault
{ 
    public sealed class KeyClient : KeyVaultClientBase
    {
        private const string keyRoute = "/keys/";

        public KeyClient(Uri vaultUri, TokenCredential credentials, PipelineOptions options = null)
            : base(vaultUri, credentials, options ?? new PipelineOptions())
        {

        }

        public async Task<Response<Key>> GetAsync(string name, string version = null, CancellationToken cancellation = default)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            Uri secretUri = BuildVaultUri(keyRoute + name + "/" + (version ?? string.Empty));

            using (HttpMessage message = _pipeline.CreateMessage(_options, cancellation))
            {
                message.SetRequestLine(PipelineMethod.Get, secretUri);
                message.AddHeader("Host", secretUri.Host);
                message.AddHeader("Accept", "application/json");
                message.AddHeader("Content-Type", "application/json; charset=utf-8");
                message.AddHeader("Authorization", "Bearer " + _credentials.Token);

                await _pipeline.ProcessAsync(message);

                Response response = message.Response;

                if (response.Status != 200)
                {
                    throw new ResponseFailedException(response);
                }

                var key = new Key();

                key.Deserialize(response.ContentStream);

                return new Response<Key>(response, key);
            }
        }
    }


    public sealed class SecretClient : KeyVaultClientBase
    {
        private const string SecretRoute = "/secrets/";

        public SecretClient(Uri vaultUri, TokenCredential credentials, PipelineOptions options = null)
            : base(vaultUri, credentials, options ?? new PipelineOptions())
        {

        }

        public async Task<Response<Secret>> GetAsync(string name, string version = null, CancellationToken cancellation = default)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            
            Uri secretUri = BuildVaultUri(SecretRoute + name + "/" + (version ?? string.Empty));

            using (HttpMessage message = _pipeline.CreateMessage(_options, cancellation))
            {
                message.SetRequestLine(PipelineMethod.Get, secretUri);
                message.AddHeader("Host", secretUri.Host);
                message.AddHeader("Accept", "application/json");
                message.AddHeader("Content-Type", "application/json; charset=utf-8");
                message.AddHeader("Authorization", "Bearer " + _credentials.Token);

                await _pipeline.ProcessAsync(message);

                Response response = message.Response;

                if (response.Status != 200)
                {
                    throw new ResponseFailedException(response);
                }

                var secret = new Secret();

                secret.Deserialize(response.ContentStream);

                return new Response<Secret>(response, secret);
            }
        }

        public async Task<Response<Secret>> SetAsync(string name, string value, string contentType = null, VaultEntityAttributes attributes = null, IDictionary<string, string> tags = null, CancellationToken cancellation = default)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            if (string.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(value));

            var secretUri = BuildVaultUri(SecretRoute + name);

            using (HttpMessage message = _pipeline.CreateMessage(_options, cancellation))
            {
                message.SetRequestLine(PipelineMethod.Put, secretUri);
                message.AddHeader("Host", secretUri.Host);
                message.AddHeader("Accept", "application/json");
                message.AddHeader("Content-Type", "application/json; charset=utf-8");
                message.AddHeader("Authorization", "Bearer " + _credentials.Token);

                var secret = new Secret()
                {
                    Value = value,
                    ContentType = contentType,
                    Attributes = attributes,
                    Tags = tags
                };

                var content = secret.Serialize();

                // TODO: remove debugging code
                var strContent = Encoding.UTF8.GetString(content.ToArray());

                message.SetContent(PipelineContent.Create(content));

                await _pipeline.ProcessAsync(message);

                Response response = message.Response;

                if (response.Status != 200)
                {
                    return new Response<Secret>(response);
                }

                secret.Deserialize(response.ContentStream);

                return new Response<Secret>(response, secret);
            }
        }


        //public async Task<IEnumerable<Secret>> GetSecretsAsync(CancellationToken cancellation = default)
        //{

        //}

        //public async Task<IEnumerable<Secret>> GetSecretVersionsAsync(string name, CancellationToken cancellation = default)
        //{
        //    if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));



        //}

    }

    public abstract class KeyVaultClientBase
    {
        protected readonly Uri _vaultUri;
        protected const string ApiVersion = "7.0";
        protected const string SdkName = "Azure.Security.KeyVault";

        protected const string SdkVersion = "1.0.0";

        protected readonly TokenCredential _credentials;
        protected readonly PipelineOptions _options;
        protected readonly HttpPipeline _pipeline;

        protected KeyVaultClientBase(Uri vaultUri, TokenCredential credentials, PipelineOptions options)
        {

            if (credentials == null) throw new ArgumentNullException(nameof(credentials));
            if (options == null) throw new ArgumentNullException(nameof(options));

            _vaultUri = vaultUri;
            _credentials = credentials;
            _options = options;
            _pipeline = HttpPipeline.Create(_options, SdkName, SdkVersion);
        }

        protected Uri BuildVaultUri(string path)
        {
            var uriBuilder = new UriBuilder(_vaultUri);

            uriBuilder.Path = path;

            uriBuilder.AppendQuery("api-version", ApiVersion);

            return uriBuilder.Uri;
        }
    }

    public class KeyVaultClient : KeyVaultClientBase
    {
        public KeyVaultClient(Uri vaultUri, TokenCredential credentials)
            : this(vaultUri, credentials, new PipelineOptions())
        {

        }

        public KeyVaultClient(Uri vaultUri, TokenCredential credentials, PipelineOptions options = null)
            : base(vaultUri, credentials, options ?? new PipelineOptions())
        {
            Secrets = new SecretClient(vaultUri, credentials, options);
        }

        public SecretClient Secrets { get; private set; }
        
    }
}
