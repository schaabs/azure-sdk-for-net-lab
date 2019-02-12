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
        private const string KeysRoute = "/keys/";

        public KeyClient(Uri vaultUri, TokenCredential credentials, PipelineOptions options = null)
            : base(vaultUri, credentials, options ?? new PipelineOptions())
        {

        }

        public async Task<Response<Key>> ImportAsync(string name, Key key, bool? hsm = default, CancellationToken cancellation = default)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            if (key == null) throw new ArgumentNullException(nameof(key));

            var keysUri = BuildVaultUri(KeysRoute + name);

            using (HttpMessage message = _pipeline.CreateMessage(_options, cancellation))
            {
                message.SetRequestLine(PipelineMethod.Put, keysUri);
                message.AddHeader("Host", keysUri.Host);
                message.AddHeader("Accept", "application/json");
                message.AddHeader("Content-Type", "application/json; charset=utf-8");
                message.AddHeader("Authorization", "Bearer " + _credentials.Token);

                var keyImportParameters = new KeyImportParameters()
                {
                    Key = key,
                    Hsm = hsm
                };

                var content = keyImportParameters.Serialize();

                // TODO: remove debugging code
                var strContent = Encoding.UTF8.GetString(content.ToArray());

                message.SetContent(PipelineContent.Create(content));

                await _pipeline.ProcessAsync(message);

                Response response = message.Response;

                if (response.Status != 200)
                {
                    throw new ResponseFailedException(response);
                }
                
                key.Deserialize(response.ContentStream);

                return new Response<Key>(response, key);
            }
        }

        public async Task<Response<Key>> CreateAsync(string name, string kty, string crv = null, int? keySize = null, IList<string> keyOps = null, VaultEntityAttributes attributes = null, IDictionary<string, string> tags = null, CancellationToken cancellation = default)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            if (string.IsNullOrEmpty(kty)) throw new ArgumentNullException(nameof(kty));

            var keysUri = BuildVaultUri(KeysRoute + name);

            using (HttpMessage message = _pipeline.CreateMessage(_options, cancellation))
            {
                message.SetRequestLine(PipelineMethod.Put, keysUri);
                message.AddHeader("Host", keysUri.Host);
                message.AddHeader("Accept", "application/json");
                message.AddHeader("Content-Type", "application/json; charset=utf-8");
                message.AddHeader("Authorization", "Bearer " + _credentials.Token);

                var keyCreateParams = new KeyCreateParameters()
                {
                    Kty = kty,
                    Crv = crv,
                    KeySize = keySize,
                    KeyOps = keyOps,
                    Attributes = attributes,
                    Tags = tags
                };

                var content = keyCreateParams.Serialize();

                // TODO: remove debugging code
                var strContent = Encoding.UTF8.GetString(content.ToArray());

                message.SetContent(PipelineContent.Create(content));

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
        
        public async Task<Response<Key>> GetAsync(Uri keyUri, CancellationToken cancellation = default)
        {
            if (keyUri == null) throw new ArgumentNullException(nameof(keyUri));

            using (HttpMessage message = _pipeline.CreateMessage(_options, cancellation))
            {
                message.SetRequestLine(PipelineMethod.Get, keyUri);
                message.AddHeader("Host", keyUri.Host);
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

        public async Task<Response<Key>> GetAsync(string name, string version = null, CancellationToken cancellation = default)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            Uri keyUri = BuildVaultUri(KeysRoute + name + "/" + (version ?? string.Empty));

            return await GetAsync(keyUri, cancellation);
        }
    }


    public sealed class SecretClient : KeyVaultClientBase
    {
        private const string SecretRoute = "/secrets/";

        public SecretClient(Uri vaultUri, TokenCredential credentials, PipelineOptions options = null)
            : base(vaultUri, credentials, options ?? new PipelineOptions())
        {

        }
        public async Task<Response<Secret>> GetAsync(Uri secretUri, CancellationToken cancellation = default)
        {
            if (secretUri == null) throw new NullReferenceException(nameof(secretUri));

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

        public async Task<Response<Secret>> GetAsync(string name, string version = null, CancellationToken cancellation = default)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            Uri secretUri = BuildVaultUri(SecretRoute + name + "/" + (version ?? string.Empty));

            return await GetAsync(secretUri, cancellation);
        }

        public async Task<Response<PagedCollection<Secret>>> GetVersionsAsync(string name, int? maxPageSize = default, CancellationToken cancellation = default)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            var query = maxPageSize.HasValue ? new KeyValuePair<string, string>[] { new KeyValuePair<string, string>("maxresults", maxPageSize.Value.ToString()) } : null;

            Uri firstPageUri = BuildVaultUri(SecretRoute + name + "/versions", query);

            var firstResponse = await GetPageAsync<Secret>(firstPageUri, cancellation);

            firstResponse.Deconstruct(out Page<Secret> firstPage, out Response rawResponse);

            return new Response<PagedCollection<Secret>>(rawResponse, new PagedCollection<Secret>(firstPage));
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
                    throw new ResponseFailedException(response);
                }

                secret.Deserialize(response.ContentStream);

                return new Response<Secret>(response, secret);
            }
        }
        

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
            _vaultUri = vaultUri ?? throw new ArgumentNullException(nameof(vaultUri));
            _credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _pipeline = HttpPipeline.Create(_options, SdkName, SdkVersion);
        }

        protected async Task<Response<Page<T>>> GetPageAsync<T>(Uri pageUri, CancellationToken cancellation)
            where T : Model, new()
        {

            using (HttpMessage message = _pipeline.CreateMessage(_options, cancellation))
            {
                message.SetRequestLine(PipelineMethod.Get, pageUri);
                message.AddHeader("Host", _vaultUri.Host);
                message.AddHeader("Accept", "application/json");
                message.AddHeader("Content-Type", "application/json; charset=utf-8");
                message.AddHeader("Authorization", "Bearer " + _credentials.Token);

                await _pipeline.ProcessAsync(message);

                Response response = message.Response;

                if (response.Status != 200)
                {
                    throw new ResponseFailedException(response);
                }

                var page = new Page<T>(this.GetPageAsync<T>, cancellation);

                page.Deserialize(response.ContentStream);

                return new Response<Page<T>>(response, page);
            }
        }

        protected Uri BuildVaultUri(string path, params KeyValuePair<string, string>[] query)
        {
            var uriBuilder = new UriBuilder(_vaultUri);

            uriBuilder.Path = path;

            uriBuilder.AppendQuery("api-version", ApiVersion);

            if (query != null)
            {
                for(int i = 0; i < query.Length; i++)
                {
                    uriBuilder.AppendQuery(query[i].Key, query[i].Value);
                }
            }

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
