using System;
using Azure.Core;
using Azure.Core.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;

namespace Azure.Security.KeyVault
{ 
    public sealed class KeyClient
    {
        private const string KeysRoute = "/keys/";

        public KeyClient(Uri vaultUri, ITokenCredentialProvider credentialProvider, PipelineOptions options = null)
        {

        }

        public KeyClient(Uri vaultUri, ITokenCredential credentials, PipelineOptions options = null)
        {

        }

        public async Task<Response<Key>> ImportAsync(string name, Key key, bool? hardwareProtected = default, CancellationToken cancellation = default)
        {
            throw new NotImplementedException();
            //if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            //if (key == null) throw new ArgumentNullException(nameof(key));

            //var keysUri = BuildVaultUri(KeysRoute + name);

            //using (HttpMessage message = _pipeline.CreateMessage(_options, cancellation))
            //{
            //    message.SetRequestLine(PipelineMethod.Put, keysUri);
            //    message.AddHeader("Host", keysUri.Host);
            //    message.AddHeader("Accept", "application/json");
            //    message.AddHeader("Content-Type", "application/json; charset=utf-8");
            //    message.AddHeader("Authorization", "Bearer " + _credentials.Token);

            //    var keyImportParameters = new KeyImportParameters()
            //    {
            //        Key = key,
            //        Hsm = hsm
            //    };

            //    var content = keyImportParameters.Serialize();

            //    // TODO: remove debugging code
            //    var strContent = Encoding.UTF8.GetString(content.ToArray());

            //    message.SetContent(PipelineContent.Create(content));

            //    await _pipeline.ProcessAsync(message);

            //    Response response = message.Response;

            //    if (response.Status != 200)
            //    {
            //        throw new ResponseFailedException(response);
            //    }

            //    key.Deserialize(response.ContentStream);

            //    return new Response<Key>(response, key);
            //}
        }

        public async Task<Response<Key>> CreateAsync(string name, string kty, string crv = null, int? keySize = null, bool? enabled = null, DateTime? notBefore = null, DateTime? expires = null, IList<string> keyOps = null, IDictionary<string, string> tags = null, CancellationToken cancellation = default)
        {
            throw new NotImplementedException();
            //if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            //if (string.IsNullOrEmpty(kty)) throw new ArgumentNullException(nameof(kty));

            //var keysUri = BuildVaultUri(KeysRoute + name);

            //using (HttpMessage message = _pipeline.CreateMessage(_options, cancellation))
            //{
            //    message.SetRequestLine(PipelineMethod.Put, keysUri);
            //    message.AddHeader("Host", keysUri.Host);
            //    message.AddHeader("Accept", "application/json");
            //    message.AddHeader("Content-Type", "application/json; charset=utf-8");
            //    message.AddHeader("Authorization", "Bearer " + _credentials.Token);

            //    var keyCreateParams = new KeyCreateParameters()
            //    {
            //        Kty = kty,
            //        Crv = crv,
            //        KeySize = keySize,
            //        KeyOps = keyOps,
            //        Attributes = attributes,
            //        Tags = tags
            //    };

            //    var content = keyCreateParams.Serialize();

            //    // TODO: remove debugging code
            //    var strContent = Encoding.UTF8.GetString(content.ToArray());

            //    message.SetContent(PipelineContent.Create(content));

            //    await _pipeline.ProcessAsync(message);

            //    Response response = message.Response;

            //    if (response.Status != 200)
            //    {
            //        throw new ResponseFailedException(response);
            //    }

            //    var key = new Key();

            //    key.Deserialize(response.ContentStream);

            //    return new Response<Key>(response, key);
            //}
        }
        
        public async Task<Response<Key>> GetAsync(Uri keyUri, CancellationToken cancellation = default)
        {
            throw new NotImplementedException();

            //if (keyUri == null) throw new ArgumentNullException(nameof(keyUri));

            //using (HttpMessage message = _pipeline.CreateMessage(_options, cancellation))
            //{
            //    message.SetRequestLine(PipelineMethod.Get, keyUri);
            //    message.AddHeader("Host", keyUri.Host);
            //    message.AddHeader("Accept", "application/json");
            //    message.AddHeader("Content-Type", "application/json; charset=utf-8");
            //    message.AddHeader("Authorization", "Bearer " + _credentials.Token);

            //    await _pipeline.ProcessAsync(message);

            //    Response response = message.Response;

            //    if (response.Status != 200)
            //    {
            //        throw new ResponseFailedException(response);
            //    }

            //    var key = new Key();

            //    key.Deserialize(response.ContentStream);

            //    return new Response<Key>(response, key);
            //}
        }

        public async Task<Response<Key>> GetAsync(string name, string version = null, CancellationToken cancellation = default)
        {
            throw new NotImplementedException();
            //if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            //Uri keyUri = BuildVaultUri(KeysRoute + name + "/" + (version ?? string.Empty));

            //return await GetAsync(keyUri, cancellation);
        }

        public AsyncEnumerator<Key> ListVersionsAsync(string name, int? maxPageSize = default, CancellationToken cancellation = default)
        {
            throw new NotImplementedException();
            //if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            //var query = maxPageSize.HasValue ? new KeyValuePair<string, string>[] { new KeyValuePair<string, string>("maxresults", maxPageSize.Value.ToString()) } : null;

            //Uri firstPageUri = BuildVaultUri(KeysRoute + name + "/versions", query);

            //return new Page<Key>.AsyncItemEnumerator(firstPageUri, this.GetPageAsync<Key>, cancellation);
        }

        public AsyncEnumerator<Key> ListAsync(int? maxPageSize = default, CancellationToken cancellation = default)
        {
            throw new NotImplementedException();
        }

        // TODO: add in attribute values
        public async Task<Response<Key>> UpdateAsync(string name, IList<string> keyOps = default, IDictionary<string, string> tags = null, CancellationToken cancellation = default)
        {
            throw new NotImplementedException();
        }
        
        public async Task<Response<DeletedKey>> DeleteAsync(string name, CancellationToken cancellation = default)
        {
            throw new NotImplementedException();
            //if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            //var secretUri = BuildVaultUri(KeysRoute + name);

            //using (HttpMessage message = _pipeline.CreateMessage(_options, cancellation))
            //{
            //    message.SetRequestLine(PipelineMethod.Delete, secretUri);
            //    message.AddHeader("Host", secretUri.Host);
            //    message.AddHeader("Accept", "application/json");
            //    message.AddHeader("Content-Type", "application/json; charset=utf-8");
            //    message.AddHeader("Authorization", "Bearer " + _credentials.Token);

            //    await _pipeline.ProcessAsync(message);

            //    Response response = message.Response;

            //    if (response.Status != 200)
            //    {
            //        throw new ResponseFailedException(response);
            //    }

            //    DeletedKey deleted = new DeletedKey();

            //    deleted.Deserialize(response.ContentStream);

            //    return new Response<DeletedKey>(response, deleted);
            //}
        }

        public async Task<Response<DeletedKey>> GetDeletedAsync(Uri recoveryId, CancellationToken cancellation = default)
        {
            throw new NotImplementedException();
        }

        public async Task<Response<DeletedKey>> GetDeletedAsync(string name, CancellationToken cancellation = default)
        {
            throw new NotImplementedException();
        }

        public AsyncEnumerator<DeletedKey> ListDeletedAsync(int? maxPageSize = default, CancellationToken cancellation = default)
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


    public sealed class SecretClient
    {

        private KeyVaultClientPipeline _pipeline;

        private const string SecretRoute = "/secrets/";

        public SecretClient(Uri vaultUri, ITokenCredentialProvider credentialProvider, PipelineOptions options = null)
        {
            _pipeline = new KeyVaultClientPipeline(vaultUri, credentialProvider, options ?? new PipelineOptions());
        }

        public SecretClient(Uri vaultUri, ITokenCredential credential, PipelineOptions options = null)
        {
            _pipeline = new KeyVaultClientPipeline(vaultUri, credential, options ?? new PipelineOptions());
        }

        public async Task<Response<Secret>> GetAsync(string name, string version = null, CancellationToken cancellation = default)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            Uri secretUri = BuildSecretUri(name, version);

            return await _pipeline.RequestAsync<Secret>(PipelineMethod.Get, secretUri, cancellation);
        }

        public AsyncEnumerator<SecretAttributes> GetAllVersionsAsync(string name, int? maxPageSize = default, CancellationToken cancellation = default)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            var query = maxPageSize.HasValue ? new KeyValuePair<string, string>[] { new KeyValuePair<string, string>("maxresults", maxPageSize.Value.ToString()) } : null;

            Uri firstPageUri = BuildSecretUri(query, name, "versions");

            return new AsyncEnumerator<SecretAttributes>(firstPageUri, _pipeline.GetPageAsync<SecretAttributes>, cancellation);
        }

        public AsyncEnumerator<SecretAttributes> GetAllAsync(int? maxPageSize = default, CancellationToken cancellation = default)
        {
            var query = maxPageSize.HasValue ? new KeyValuePair<string, string>[] { new KeyValuePair<string, string>("maxresults", maxPageSize.Value.ToString()) } : null;

            Uri firstPageUri = BuildSecretUri(query);

            return new AsyncEnumerator<SecretAttributes>(firstPageUri, _pipeline.GetPageAsync<SecretAttributes>, cancellation);
        }

        public async Task<Response<SecretAttributes>> UpdateAsync(SecretAttributes secret, CancellationToken cancellation = default)
        {
            if (secret?.Id == null) throw new ArgumentNullException($"{nameof(secret)}.{nameof(secret.Id)}");

            var secretUri = new UriBuilder(secret.Id);

            secretUri.AppendQuery("api-version", KeyVaultClientPipeline.ApiVersion);

            return await _pipeline.RequestAsync<SecretAttributes, SecretAttributes>(PipelineMethod.Patch, secretUri.Uri, secret, cancellation);
        }

        public async Task<Response<Secret>> SetAsync(Secret secret, CancellationToken cancellation = default)
        {
            if (secret == null) throw new ArgumentNullException(nameof(secret));

            if (secret.Name == null) throw new ArgumentNullException($"{nameof(secret)}.{nameof(secret.Name)}");

            if (secret.Value == null) throw new ArgumentNullException($"{nameof(secret)}.{nameof(secret.Value)}");

            var secretUri = BuildSecretUri(secret.Name);

            return await _pipeline.RequestAsync<Secret, Secret>(PipelineMethod.Put, secretUri, secret, cancellation);
        }

        public async Task<Response<Secret>> SetAsync(string name, string value, CancellationToken cancellation = default)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            if (string.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(value));

            return await SetAsync(new Secret(name, value), cancellation);
        }
        
        public async Task<Response<DeletedSecret>> DeleteAsync(string name, CancellationToken cancellation = default)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            var secretUri = BuildSecretUri(name);

            return await _pipeline.RequestAsync<DeletedSecret>(PipelineMethod.Delete, secretUri, cancellation);
        }

        public async Task<Response<DeletedSecret>> GetDeletedAsync(string name, CancellationToken cancellation = default)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            var secretUri = BuildSecretUri(name);

            return await _pipeline.RequestAsync<DeletedSecret>(PipelineMethod.Get, secretUri, cancellation);
        }

        public AsyncEnumerator<DeletedSecret> GetAllDeletedAsync(int? maxPageSize = default, CancellationToken cancellation = default)
        {
            throw new NotImplementedException();
        }

        public async Task<Response<Secret>> RecoverDeletedAsync(string name, CancellationToken cancellation = default)
        {
            throw new NotImplementedException();
        }

        public async Task<Response> PurgeDeletedAsync(string name, CancellationToken cancellation = default)
        {
            throw new NotImplementedException();
        }

        // todo: could this be made better by using Stream or Span vs []?
        public async Task<Response<byte[]>> BackupAsync(string name, CancellationToken cancellation = default)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            var backupUri = BuildSecretUri(name, "/backup");

            var backupResponse = await _pipeline.RequestAsync<VaultBackup>(PipelineMethod.Post, backupUri, cancellation);

            backupResponse.Deconstruct(out VaultBackup backup, out Response response);

            return new Response<byte[]>(response, backup.Value);
        }

        public async Task<Response<Secret>> RestoreAsync(byte[] backup, CancellationToken cancellation = default)
        {
            if (backup == null) throw new ArgumentNullException(nameof(backup));

            var secretUri = BuildSecretUri("restore");

            return await _pipeline.RequestAsync<VaultBackup, Secret>(PipelineMethod.Post, secretUri, new VaultBackup { Value = backup }, cancellation);
        }

        private Uri BuildSecretUri(params string[] path)
        {
            return BuildSecretUri((IEnumerable<KeyValuePair<string, string>>)null, path);

        }

        private Uri BuildSecretUri(IEnumerable<KeyValuePair<string, string>> query, params string[] path)
        {
            return _pipeline.BuildVaultUri(SecretRoute + string.Join("/", path), query);
        }
    }

    internal class KeyVaultClientPipeline
    {
        internal readonly Uri _vaultUri;
        internal const string ApiVersion = "7.0";
        internal const string SdkName = "Azure.Security.KeyVault";
        internal const string SdkVersion = "1.0.0";

        internal readonly ITokenCredential _credentials;
        internal readonly PipelineOptions _options;
        internal readonly HttpPipeline _pipeline;
        internal KeyVaultClientPipeline(Uri vaultUri, ITokenCredentialProvider credentialProvider, PipelineOptions options)
        {
            _vaultUri = vaultUri ?? throw new ArgumentNullException(nameof(vaultUri));
            _credentials = credentialProvider != null ? credentialProvider.GetCredentialAsync(new string[] { "https://vault.azure.net/.Default" }).GetAwaiter().GetResult() : throw new ArgumentNullException(nameof(credentialProvider));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _pipeline = HttpPipeline.Create(_options, SdkName, SdkVersion);
        }
        internal KeyVaultClientPipeline(Uri vaultUri, ITokenCredential credentials, PipelineOptions options)
        {
            _vaultUri = vaultUri ?? throw new ArgumentNullException(nameof(vaultUri));
            _credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _pipeline = HttpPipeline.Create(_options, SdkName, SdkVersion);
        }

        internal async Task<Response<T>> RequestAsync<T>(PipelineMethod method, Uri requestUri, CancellationToken cancellation)
            where T : Model, new()
        {
            using (HttpMessage request = CreateRequest(method, requestUri, cancellation))
            {
                return await ProcessRequestAsync<T>(request);
            }
        }

        internal async Task<Response<TResponse>> RequestAsync<TRequest, TResponse>(PipelineMethod method, Uri requestUri, TRequest body, CancellationToken cancellation)
            where TRequest : Model
            where TResponse : Model, new()
        {
            using (HttpMessage request = CreateRequest(method, requestUri, cancellation))
            {
                var content = body.Serialize();

                request.SetContent(PipelineContent.Create(content));

                return await ProcessRequestAsync<TResponse>(request);
            }
        }

        private async Task<Response<T>> ProcessRequestAsync<T>(HttpMessage request)
            where T : Model, new()
        {
            await _pipeline.ProcessAsync(request);

            Response response = request.Response;

            if (response.Status != 200)
            {
                throw new ResponseFailedException(response);
            }

            var result = new T();

            result.Deserialize(response.ContentStream);

            return new Response<T>(response, result);
        }

        private HttpMessage CreateRequest(PipelineMethod method, Uri requestUri, CancellationToken cancellation)
        {
            HttpMessage message = _pipeline.CreateMessage(_options, cancellation);
            message.SetRequestLine(method, requestUri);
            message.AddHeader("Host", requestUri.Host);
            message.AddHeader("Accept", "application/json");
            message.AddHeader("Content-Type", "application/json; charset=utf-8");
            message.AddHeader("Authorization", "Bearer " + _credentials.Token);
            return message;
        }


        internal async Task<Response<Page<T>>> GetPageAsync<T>(Uri pageUri, CancellationToken cancellation)
            where T : Model, new()
        {
            using (HttpMessage message = _pipeline.CreateMessage(_options, cancellation))
            {
                message.SetRequestLine(PipelineMethod.Get, pageUri);
                message.AddHeader("Host", pageUri.Host);
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

        internal Uri BuildVaultUri(string path, IEnumerable<KeyValuePair<string, string>> query)
        {
            var uriBuilder = new UriBuilder(_vaultUri);

            uriBuilder.Path = path;

            uriBuilder.AppendQuery("api-version", ApiVersion);

            if (query != null)
            {
                foreach (var kvp in query)
                { 
                    uriBuilder.AppendQuery(kvp.Key, kvp.Value);
                }
            }

            return uriBuilder.Uri;
        }
    }

    public class KeyVaultClient
    {
        private KeyVaultClientPipeline _pipeline;

        public KeyVaultClient(Uri vaultUri, ITokenCredentialProvider credentialProvider, PipelineOptions options = null)
        {
            Secrets = new SecretClient(vaultUri, credentialProvider, options);
        }

        public KeyVaultClient(Uri vaultUri, ITokenCredential credentials, PipelineOptions options = null)
        {
            Secrets = new SecretClient(vaultUri, credentials, options);
        }

        public SecretClient Secrets { get; private set; }

        public KeyClient Keys { get; private set; }
        
    }
}
