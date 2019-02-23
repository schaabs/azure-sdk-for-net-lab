using Azure.Core.Http;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Azure.Security.KeyVault.Test
{
    public class SecretTests : KeyVaultTestBase
    {

        public SecretTests()
        {
        }

        [Fact]
        public async Task CredentialProvider()
        {
            var client = new KeyVaultClient(VaultUri, new MockMsalCredentialProvider());

            Secret setResult = await client.Secrets.SetAsync("CrudBasic", "CrudBasicValue1");

            Secret getResult = await client.Secrets.GetAsync("CrudBasic");

            AssertSecretsEqual(setResult, getResult);

            DeletedSecret deleteResult = await client.Secrets.DeleteAsync("CrudBasic");

            // remove the value which is not set on the deleted response
            setResult.Value = null;

            AssertSecretsEqual(setResult, deleteResult);
        }

        [Fact]
        public async Task CrudBasic()
        {
            var client = new KeyVaultClient(VaultUri, TestCredential);

            Secret setResult = await client.Secrets.SetAsync("CrudBasic", "CrudBasicValue1");

            Secret getResult = await client.Secrets.GetAsync("CrudBasic");

            AssertSecretsEqual(setResult, getResult);

            DeletedSecret deleteResult = await client.Secrets.DeleteAsync("CrudBasic");

            // remove the value which is not set on the deleted response
            setResult.Value = null;

            AssertSecretsEqual(setResult, deleteResult);
        }

        [Fact]
        public async Task CrudWithExtendedProps()
        {
            var client = new KeyVaultClient(VaultUri, TestCredential);

            var attr = new VaultEntityAttributes() { NotBefore = UtcNowMs() + TimeSpan.FromDays(1), Expires = UtcNowMs() + TimeSpan.FromDays(90) };

            Secret setResult = await client.Secrets.SetAsync("CrudWithExtendedProps", "CrudWithExtendedPropsValue1", contentType:"password", attributes:attr);
            
            Assert.Equal(attr.NotBefore, setResult.Attributes.NotBefore);

            Assert.Equal(attr.Expires, setResult.Attributes.Expires);

            Assert.Equal("password", setResult.ContentType);

            Secret getResult = await client.Secrets.GetAsync("CrudWithExtendedProps");

            AssertSecretsEqual(setResult, getResult);

            DeletedSecret deleteResult = await client.Secrets.DeleteAsync("CrudWithExtendedProps");

            // remove the value which is not set on the deleted response
            setResult.Value = null;

            AssertSecretsEqual(setResult, deleteResult);
        }

        [Fact]
        public async Task BackupRestore()
        {
            var backupPath = Path.GetTempFileName();

            try
            {
                var client = new KeyVaultClient(VaultUri, TestCredential);

                Secret setResult = await client.Secrets.SetAsync("BackupRestore", "BackupRestore");

                await File.WriteAllBytesAsync(backupPath, await client.Secrets.BackupAsync("BackupRestore"));

                await client.Secrets.DeleteAsync("BackupRestore");

                Secret restoreResult = await client.Secrets.RestoreAsync(await File.ReadAllBytesAsync(backupPath));

                // remove the vaule which is not set in the restore response
                setResult.Value = null;

                AssertSecretsEqual(setResult, restoreResult);
            }
            finally
            {
                File.Delete(backupPath);
            }
        }

        private DateTime UtcNowMs()
        {
            return DateTime.MinValue.ToUniversalTime() + TimeSpan.FromMilliseconds(new TimeSpan(DateTime.UtcNow.Ticks).TotalMilliseconds);
        }

    }

    public class SecretListTests : KeyVaultTestBase, IDisposable
    {
        private const int VersionCount = 50;
        private readonly string SecretName = Guid.NewGuid().ToString("N");

        private readonly Dictionary<string, Secret> _versions = new Dictionary<string, Secret>(VersionCount);
        private readonly KeyVaultClient _client;

        public SecretListTests()
        {
            _client = new KeyVaultClient(VaultUri, TestCredential);

            for (int i = 0; i < VersionCount; i++)
            {
                Secret secret = _client.Secrets.SetAsync(SecretName, Guid.NewGuid().ToString("N")).GetAwaiter().GetResult();

                secret.Value = null;

                _versions[secret.Id] = secret;
            }
        }

        public void Dispose()
        {
            var deleteResult = _client.Secrets.DeleteAsync(SecretName);
        }

        [Fact]
        public async Task ListVersionAsyncForEach()
        {
            int actVersionCount = 0;

            await foreach (var secret in _client.Secrets.ListVersionsAsync(SecretName))
            {
                Assert.True(_versions.TryGetValue(secret.Id, out Secret exp));

                AssertSecretsEqual(exp, secret);

                actVersionCount++;
            }

            Assert.Equal(VersionCount, actVersionCount);
        }

        [Fact]
        public async Task ListVersionEnumeratorMoveNext()
        {
            int actVersionCount = 0;

            var enumerator = _client.Secrets.ListVersionsAsync(SecretName);

            while (await enumerator.MoveNextAsync())
            {
                Assert.True(_versions.TryGetValue(enumerator.Current.Id, out Secret exp));

                AssertSecretsEqual(exp, enumerator.Current);

                actVersionCount++;
            }

            Assert.Equal(VersionCount, actVersionCount);
        }


        [Fact]
        public async Task ListVersionByPageAsyncForEach()
        {
            int actVersionCount = 0;

            await foreach (Page<Secret> currentPage in _client.Secrets.ListVersionsByPageAsync(SecretName))
            {

                Assert.True(currentPage.Items.Length <= 5);

                for (int i = 0; i < currentPage.Items.Length; i++)
                {
                    Assert.True(_versions.TryGetValue(currentPage.Items[i].Id, out Secret exp));

                    AssertSecretsEqual(exp, currentPage.Items[i]);

                    actVersionCount++;
                }
            }

            Assert.Equal(VersionCount, actVersionCount);
        }

        [Fact]
        public async Task ListVersionByPageEnumeratorMoveNext()
        {
            int actVersionCount = 0;

            var enumerator = _client.Secrets.ListVersionsByPageAsync(SecretName, maxPageSize: 5);

            while (await enumerator.MoveNextAsync())
            {
                Page<Secret> currentPage = enumerator.Current;

                Assert.True(currentPage.Items.Length <= 5);

                for (int i = 0; i < currentPage.Items.Length; i++)
                {
                    Assert.True(_versions.TryGetValue(currentPage.Items[i].Id, out Secret exp));

                    AssertSecretsEqual(exp, currentPage.Items[i]);

                    actVersionCount++;
                }
            }

            Assert.Equal(VersionCount, actVersionCount);
        }
    }

    public class KeyVaultTestBase
    {

        protected class MockMsalCredentialProvider : ITokenCredentialProvider
        {
            public async Task<ITokenCredential> GetCredentialAsync(IEnumerable<string> scopes = null, CancellationToken cancellation = default)
            {
                var resource = scopes?.FirstOrDefault()?.Replace("/.Default", string.Empty);
                
                return await TokenCredential.CreateCredentialAsync(async (cancel) => { return await this.RefreshToken(resource, cancel); });
            }

            private async Task<TokenRefreshResult> RefreshToken(string resource, CancellationToken cancellation)
            {
                var authResult = await s_authContext.Value.AcquireTokenAsync(resource, s_clientCredential.Value);

                return new TokenRefreshResult() { Delay = authResult.ExpiresOn.AddMinutes(-5) - DateTime.UtcNow, Token = authResult.AccessToken };
            }
        }

        private static Lazy<string> s_tenantId = new Lazy<string>(() => { return Environment.GetEnvironmentVariable("AZURE_TENANT_ID"); });

        private static Lazy<string> s_clientId = new Lazy<string>(() => { return Environment.GetEnvironmentVariable("AZURE_CLIENT_ID"); });

        private static Lazy<string> s_clientSecret = new Lazy<string>(() => { return Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET"); });

        private static Lazy<ClientCredential> s_clientCredential = new Lazy<ClientCredential>(() => { return new ClientCredential(s_clientId.Value, s_clientSecret.Value); });

        private static Lazy<AuthenticationContext> s_authContext = new Lazy<AuthenticationContext>(() => { return new AuthenticationContext("https://login.microsoftonline.com/" + s_tenantId.Value); });

        private static Lazy<TokenCredential> s_credential = new Lazy<TokenCredential>(() => { return TokenCredential.CreateCredentialAsync(RefreshTokenWithAuthContext).GetAwaiter().GetResult(); });

        private static Lazy<Uri> s_vaultUri = new Lazy<Uri>(() => { return new Uri(Environment.GetEnvironmentVariable("AZURE_KEYVAULT_URL")); });

        protected TokenCredential TestCredential { get => s_credential.Value; }

        protected Uri VaultUri { get => s_vaultUri.Value; }

        private static async Task<TokenRefreshResult> RefreshTokenWithAuthContext(CancellationToken cancellation)
        {
            var authResult = await s_authContext.Value.AcquireTokenAsync("https://vault.azure.net", s_clientCredential.Value);
            
            return new TokenRefreshResult() { Delay = authResult.ExpiresOn.AddMinutes(-5) - DateTime.UtcNow, Token = authResult.AccessToken };
        }

        protected void AssertSecretsEqual(Secret exp, Secret act)
        {
            Assert.Equal(exp.Id, act.Id);
            Assert.Equal(exp.Value, act.Value);
            Assert.Equal(exp.ContentType, act.ContentType);
            Assert.Equal(exp.Kid, act.Kid);
            Assert.Equal(exp.Managed, act.Managed);

            if (exp.Attributes == null)
            {
                Assert.Null(act.Attributes);
            }
            else
            {
                Assert.Equal(exp.Attributes.Created, act.Attributes.Created);
                Assert.Equal(exp.Attributes.Enabled, act.Attributes.Enabled);
                Assert.Equal(exp.Attributes.Expires, act.Attributes.Expires);
                Assert.Equal(exp.Attributes.NotBefore, act.Attributes.NotBefore);
                Assert.Equal(exp.Attributes.RecoveryLevel, act.Attributes.RecoveryLevel);
                Assert.Equal(exp.Attributes.Updated, act.Attributes.Updated);
            }
        }
    }
}
