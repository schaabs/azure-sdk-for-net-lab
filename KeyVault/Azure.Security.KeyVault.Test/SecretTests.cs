using Azure.Core.Http;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Azure.Security.KeyVault.Test
{
    public class SecretTests : KeyVaultTestBase
    {

        public SecretTests()
        {
        }

        [Fact]
        public async Task SetGetAsyncBasic()
        {
            var client = new KeyVaultClient(VaultUri, TestCredential);

            var setResult = await client.Secrets.SetAsync("SetGetAsyncBasic", "SetGetAsyncBasicValue1");

            var getResult = await client.Secrets.GetAsync("SetGetAsyncBasic");

            AssertSecretEqual(setResult, getResult);
        }

        [Fact]
        public async Task SetGetAsyncWithExtendedProps()
        {
            var client = new KeyVaultClient(VaultUri, TestCredential);

            var attr = new VaultEntityAttributes() { NotBefore = UtcNowMs() + TimeSpan.FromDays(1), Expires = UtcNowMs() + TimeSpan.FromDays(90) };

            var setResult = await client.Secrets.SetAsync("SetGetAsyncWithExtendedProps", "SetGetAsyncWithExtendedPropsValue1", contentType:"password", attributes:attr);

            var setSecret = (Secret)setResult;

            Assert.Equal(attr.NotBefore, setSecret.Attributes.NotBefore);

            Assert.Equal(attr.Expires, setSecret.Attributes.Expires);

            Assert.Equal("password", setSecret.ContentType);

            var getResult = await client.Secrets.GetAsync("SetGetAsyncWithExtendedProps");

            AssertSecretEqual(setResult, getResult);
        }

        private DateTime UtcNowMs()
        {
            return DateTime.MinValue.ToUniversalTime() + TimeSpan.FromMilliseconds(new TimeSpan(DateTime.UtcNow.Ticks).TotalMilliseconds);
        }

        private void AssertSecretEqual(Secret exp, Secret act)
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
    }
}
