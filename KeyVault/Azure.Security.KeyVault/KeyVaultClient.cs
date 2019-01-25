using System;
using Azure.Core;
using Azure.Core.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.Security.KeyVault
{
    public class KeyVaultClient
    {
        private readonly Uri _vaultUri;
        private const string SdkName = "Azure.Security.KeyVault";
        
        private const string SdkVersion = "1.0.0";

        private const string secretRoute = "/secrets/";
        public async Task<Response<Secret>> SetSecretAsync(string name, string value, string contentType = null, IDictionary<string, string> tags = null, VaultObjectAttributes attributes = null, CancellationToken cancellation = default)
        {
            if(string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            if(string.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(value));

            Uri secretUri = new Uri(_vaultUri, secretsRoute + name);
            
            using (HttpMessage message = Pipeline.CreateMessage(_options, cancellation))
            {
                message.SetRequestLine(PipelineMethod.Put, secretUri);
                message.AddHeader("Host", uri.Host);
            }


        }


    }
}
