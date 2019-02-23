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

        public KeyClient(Uri vaultUri, ITokenCredentialProvider credentialProvider, PipelineOptions options = null)
            : base(vaultUri, credentialProvider, options ?? new PipelineOptions())
        {

        }

        public KeyClient(Uri vaultUri, ITokenCredential credentials, PipelineOptions options = null)
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

        public Page<Key>.AsyncItemEnumerator ListVersionsAsync(string name, int? maxPageSize = default, CancellationToken cancellation = default)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            var query = maxPageSize.HasValue ? new KeyValuePair<string, string>[] { new KeyValuePair<string, string>("maxresults", maxPageSize.Value.ToString()) } : null;

            Uri firstPageUri = BuildVaultUri(KeysRoute + name + "/versions", query);

            return new Page<Key>.AsyncItemEnumerator(firstPageUri, this.GetPageAsync<Key>, cancellation);
        }

        public Page<Key>.AsyncItemEnumerator ListAsync(int? maxPageSize = default, CancellationToken cancellation = default)
        {
            throw new NotImplementedException();
        }

        public async Task<Response<Key>> UpdateAsync(string name, IList<string> keyOps = default, VaultEntityAttributes attributes = null, IDictionary<string, string> tags = null, CancellationToken cancellation = default)
        {
            throw new NotImplementedException();
        }
        
        public async Task<Response<DeletedKey>> DeleteAsync(string name, CancellationToken cancellation = default)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            var secretUri = BuildVaultUri(KeysRoute + name);

            using (HttpMessage message = _pipeline.CreateMessage(_options, cancellation))
            {
                message.SetRequestLine(PipelineMethod.Delete, secretUri);
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

                DeletedKey deleted = new DeletedKey();

                deleted.Deserialize(response.ContentStream);

                return new Response<DeletedKey>(response, deleted);
            }
        }

        public async Task<Response<DeletedKey>> GetDeletedAsync(Uri recoveryId, CancellationToken cancellation = default)
        {
            throw new NotImplementedException();
        }

        public async Task<Response<DeletedKey>> GetDeletedAsync(string name, CancellationToken cancellation = default)
        {
            throw new NotImplementedException();
        }

        public Page<DeletedKey>.AsyncItemEnumerator ListDeletedAsync(int? maxPageSize = default, CancellationToken cancellation = default)
        {
            throw new NotImplementedException();
        }

        public async Task<Response<Key>> RecoverAsync(Uri recoveryId, CancellationToken cancellation = default)
        {
            throw new NotImplementedException();
        }

        public async Task<Response<Key>> RecoverAsync(string name, CancellationToken cancellation = default)
        {
            throw new NotImplementedException();
        }

        // todo: could this be made better by using Stream or Span vs []?
        public async Task<Response<byte[]>> BackupAsync(string name, CancellationToken cancellation = default)
        {
            throw new NotImplementedException();
        }

        // todo: could this be made better by using Stream or Span vs []?
        public async Task<Response<Key>> RestoreAsync(byte[] backup, CancellationToken cancellation = default)
        {
            throw new NotImplementedException();
        }

        public async Task<Response<KeyOperationResult>> EncryptAsync(Uri keyId, string algorithm, byte[] value, CancellationToken cancellation = default)
        {
            throw new NotImplementedException();
        }

        public async Task<Response<KeyOperationResult>> EncryptAsync(string name, string algorithm, byte[] value, CancellationToken cancellation = default)
        {
            throw new NotImplementedException();
        }

        public async Task<Response<KeyOperationResult>> DecryptAsync(Uri keyId, string algorithm, byte[] value, CancellationToken cancellation = default)
        {
            throw new NotImplementedException();
        }
        
        public async Task<Response<KeyOperationResult>> SignAsync(Uri keyId, string algorithm, byte[] digest, CancellationToken cancellation = default)
        {
            throw new NotImplementedException();
        }

        public async Task<Response<KeyOperationResult>> SignAsync(string name, string algorithm, byte[] digest, CancellationToken cancellation = default)
        {
            throw new NotImplementedException();
        }

        public async Task<Response<KeyOperationResult>> VerifyAsync(Uri keyId, string algorithm, byte[] digest, byte[] signature, CancellationToken cancellation = default)
        {
            throw new NotImplementedException();
        }
        
        public async Task<Response<KeyOperationResult>> WrapKeyAsync(Uri keyId, string algorithm, byte[] value, CancellationToken cancellation = default)
        {
            throw new NotImplementedException();
        }

        public async Task<Response<KeyOperationResult>> WrapKeyAsync(string name, string algorithm, byte[] value, CancellationToken cancellation = default)
        {
            throw new NotImplementedException();
        }

        public async Task<Response<KeyOperationResult>> UnwrapKeyAsync(Uri keyId, string algorithm, byte[] value, CancellationToken cancellation = default)
        {
            throw new NotImplementedException();
        }
    }


    public sealed class SecretClient : KeyVaultClientBase
    {
        private const string SecretRoute = "/secrets/";

        public SecretClient(Uri vaultUri, ITokenCredentialProvider credentialProvider, PipelineOptions options = null)
            : base(vaultUri, credentialProvider, options ?? new PipelineOptions())
        {

        }

        public SecretClient(Uri vaultUri, ITokenCredential credentials, PipelineOptions options = null)
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

        public Page<Secret>.AsyncItemEnumerator ListVersionsAsync(string name, int? maxPageSize = default, CancellationToken cancellation = default)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            var query = maxPageSize.HasValue ? new KeyValuePair<string, string>[] { new KeyValuePair<string, string>("maxresults", maxPageSize.Value.ToString()) } : null;

            Uri firstPageUri = BuildVaultUri(SecretRoute + name + "/versions", query);

            return new Page<Secret>.AsyncItemEnumerator(firstPageUri, this.GetPageAsync<Secret>, cancellation);
        }

        public Page<Secret>.AsyncEnumerator ListVersionsByPageAsync(string name, int? maxPageSize = default, CancellationToken cancellation = default)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            var query = maxPageSize.HasValue ? new KeyValuePair<string, string>[] { new KeyValuePair<string, string>("maxresults", maxPageSize.Value.ToString()) } : null;

            Uri firstPageUri = BuildVaultUri(SecretRoute + name + "/versions", query);

            return new Page<Secret>.AsyncEnumerator(firstPageUri, this.GetPageAsync<Secret>, cancellation);
        }

        public Page<Secret>.AsyncItemEnumerator ListAsync(int? maxPageSize = default, CancellationToken cancellation = default)
        {
            var query = maxPageSize.HasValue ? new KeyValuePair<string, string>[] { new KeyValuePair<string, string>("maxresults", maxPageSize.Value.ToString()) } : null;

            Uri firstPageUri = BuildVaultUri(SecretRoute, query);

            return new Page<Secret>.AsyncItemEnumerator(firstPageUri, this.GetPageAsync<Secret>, cancellation);
        }

        public Page<Secret>.AsyncEnumerator ListByPageAsync(int? maxPageSize = default, CancellationToken cancellation = default)
        {
            var query = maxPageSize.HasValue ? new KeyValuePair<string, string>[] { new KeyValuePair<string, string>("maxresults", maxPageSize.Value.ToString()) } : null;

            Uri firstPageUri = BuildVaultUri(SecretRoute, query);

            return new Page<Secret>.AsyncEnumerator(firstPageUri, this.GetPageAsync<Secret>, cancellation);
        }

        public async Task<Response<Secret>> UpdateAsync(string name, string contentType = null, VaultEntityAttributes attributes = null, IDictionary<string, string> tags = null, CancellationToken cancellation = default)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            
            var secretUri = BuildVaultUri(SecretRoute + name);

            using (HttpMessage message = _pipeline.CreateMessage(_options, cancellation))
            {
                message.SetRequestLine(PipelineMethod.Patch, secretUri);
                message.AddHeader("Host", secretUri.Host);
                message.AddHeader("Accept", "application/json");
                message.AddHeader("Content-Type", "application/json; charset=utf-8");
                message.AddHeader("Authorization", "Bearer " + _credentials.Token);

                var secret = new Secret()
                {
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
        
        public async Task<Response<DeletedSecret>> DeleteAsync(string name, CancellationToken cancellation = default)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            var secretUri = BuildVaultUri(SecretRoute + name);

            using (HttpMessage message = _pipeline.CreateMessage(_options, cancellation))
            {
                message.SetRequestLine(PipelineMethod.Delete, secretUri);
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

                DeletedSecret deleted = new DeletedSecret();

                deleted.Deserialize(response.ContentStream);

                return new Response<DeletedSecret>(response, deleted);
            }
        }

        public async Task<Response<DeletedSecret>> GetDeletedAsync(Uri recoveryId, CancellationToken cancellation = default)
        {
            throw new NotImplementedException();
        }

        public async Task<Response<DeletedSecret>> GetDeletedAsync(string name, CancellationToken cancellation = default)
        {
            throw new NotImplementedException();
        }

        public Page<Secret>.AsyncEnumerator ListDeletedAsync(int? maxPageSize = default, CancellationToken cancellation = default)
        {
            throw new NotImplementedException();
        }

        public async Task<Response<Secret>> RecoverAsync(Uri recoveryId, CancellationToken cancellation = default)
        {
            throw new NotImplementedException();
        }

        public async Task<Response<Secret>> RecoverAsync(string name, CancellationToken cancellation = default)
        {
            throw new NotImplementedException();
        }

        // todo: could this be made better by using Stream or Span vs []?
        public async Task<Response<byte[]>> BackupAsync(string name, CancellationToken cancellation = default)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            var secretUri = BuildVaultUri(SecretRoute + name + "/backup");

            using (HttpMessage message = _pipeline.CreateMessage(_options, cancellation))
            {
                message.SetRequestLine(PipelineMethod.Post, secretUri);
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

                VaultBackup backup = new VaultBackup();

                backup.Deserialize(response.ContentStream);

                return new Response<byte[]>(response, backup.Value);
            }
        }

        // todo: could this be made better by using Stream or Span vs []?
        public async Task<Response<Secret>> RestoreAsync(byte[] backup, CancellationToken cancellation = default)
        {
            if (backup == null) throw new ArgumentNullException(nameof(backup));

            var secretUri = BuildVaultUri(SecretRoute + "/restore");

            using (HttpMessage message = _pipeline.CreateMessage(_options, cancellation))
            {
                message.SetRequestLine(PipelineMethod.Post, secretUri);
                message.AddHeader("Host", secretUri.Host);
                message.AddHeader("Accept", "application/json");
                message.AddHeader("Content-Type", "application/json; charset=utf-8");
                message.AddHeader("Authorization", "Bearer " + _credentials.Token);

                VaultBackup model = new VaultBackup() { Value = backup };

                var content = model.Serialize();

                // TODO: remove debugging code
                var strContent = Encoding.UTF8.GetString(content.ToArray());

                message.SetContent(PipelineContent.Create(content));

                await _pipeline.ProcessAsync(message);

                Response response = message.Response;

                if (response.Status != 200)
                {
                    throw new ResponseFailedException(response);
                }

                var restored = new Secret();

                restored.Deserialize(response.ContentStream);

                return new Response<Secret>(response, restored);
            }
        }
    }

    public abstract class KeyVaultClientBase
    {
        protected readonly Uri _vaultUri;
        protected const string ApiVersion = "7.0";
        protected const string SdkName = "Azure.Security.KeyVault";

        protected const string SdkVersion = "1.0.0";

        protected readonly ITokenCredential _credentials;
        protected readonly PipelineOptions _options;
        protected readonly HttpPipeline _pipeline;
        protected KeyVaultClientBase(Uri vaultUri, ITokenCredentialProvider credentialProvider, PipelineOptions options)
        {
            _vaultUri = vaultUri ?? throw new ArgumentNullException(nameof(vaultUri));
            _credentials = credentialProvider != null ? credentialProvider.GetCredentialAsync(new string[] { "https://vault.azure.net/.Default" }).GetAwaiter().GetResult() : throw new ArgumentNullException(nameof(credentialProvider));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _pipeline = HttpPipeline.Create(_options, SdkName, SdkVersion);
        }
        protected KeyVaultClientBase(Uri vaultUri, ITokenCredential credentials, PipelineOptions options)
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
        public KeyVaultClient(Uri vaultUri, ITokenCredentialProvider credentialProvider, PipelineOptions options = null)
            : base(vaultUri, credentialProvider, options ?? new PipelineOptions())
        {
            Secrets = new SecretClient(vaultUri, credentialProvider, options);
        }

        public KeyVaultClient(Uri vaultUri, ITokenCredential credentials, PipelineOptions options = null)
            : base(vaultUri, credentials, options ?? new PipelineOptions())
        {
            Secrets = new SecretClient(vaultUri, credentials, options);
        }

        public SecretClient Secrets { get; private set; }
        
    }
}
