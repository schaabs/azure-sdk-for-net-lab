using Azure.Core.Http;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using System.Collections.Generic;
using System.IO;

namespace Azure.Security.KeyVault.Test
{
    public class SecretTests : KeyVaultTestBase
    {

        public SecretTests()
        {
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
    
    public class SecretGetVersionsTests : KeyVaultTestBase, IDisposable
    {
        private const int VersionCount = 50;
        private readonly string SecretName = Guid.NewGuid().ToString("N");

        private readonly Dictionary<string, Secret> _versions = new Dictionary<string, Secret>(VersionCount);
        private readonly KeyVaultClient _client;
        
        public SecretGetVersionsTests()
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
        public async Task SecretsPagedCollectionIterateAll()
        {
            PagedCollection<Secret> versions = await _client.Secrets.ListVersionsAsync(SecretName);

            Secret current;

            int actVersionCount = 0;

            while((current = await versions.GetNextAsync()) != null)
            {
                Assert.True(_versions.TryGetValue(current.Id, out Secret exp));
                 
                AssertSecretsEqual(exp, current);

                actVersionCount++;
            }

            Assert.Equal(VersionCount, actVersionCount);
        }

        [Fact]
        public async Task SecretsPageCollectionPagedIteration()
        {
            PagedCollection<Secret> versions = await _client.Secrets.ListVersionsAsync(SecretName);

            Page<Secret> currentPage = versions.CurrentPage;

            int actVersionCount = 0;

            while (currentPage != null)
            {
                for(int i = 0; i < currentPage.Items.Length; i++)
                {
                    Assert.True(_versions.TryGetValue(currentPage.Items[i].Id, out Secret exp));

                    AssertSecretsEqual(exp, currentPage.Items[i]);

                    actVersionCount++;
                }
                
                currentPage = await currentPage.GetNextPageAsync();
            }

            Assert.Equal(VersionCount, actVersionCount);
        }

        [Fact]
        public async Task PagedIterationLimitPageSize()
        {
            PagedCollection<Secret> versions = await _client.Secrets.ListVersionsAsync(SecretName, maxPageSize: 5);

            Page<Secret> currentPage = versions.CurrentPage;

            int actVersionCount = 0;

            while (currentPage != null)
            {
                Assert.True(currentPage.Items.Length <= 5);

                for (int i = 0; i < currentPage.Items.Length; i++)
                {
                    Assert.True(_versions.TryGetValue(currentPage.Items[i].Id, out Secret exp));

                    AssertSecretsEqual(exp, currentPage.Items[i]);

                    actVersionCount++;
                }

                currentPage = await currentPage.GetNextPageAsync();
            }

            Assert.Equal(VersionCount, actVersionCount);
        }

    }

    public class KeyVaultTestBase
    {
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
