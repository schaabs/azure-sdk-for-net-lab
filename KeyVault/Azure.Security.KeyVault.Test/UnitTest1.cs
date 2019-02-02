using Azure.Core.Http;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using Xunit;

namespace Azure.Security.KeyVault.Test
{
    public class UnitTest1
    {
        AuthenticationContext _authContext;
        public UnitTest1()
        {
            _authContext = new AuthenticationContext();
        }
        [Fact]
        public void Test1()
        {
            TokenCredential credentials = new TokenCredential()
        }


        private string RefreshToken(out TimeSpan delay)
        {
            _authContext.AcquireTokenAsync()
        }
    }
}
