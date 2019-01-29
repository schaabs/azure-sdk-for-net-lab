
using Azure.Core.Http.Pipeline;
using System;
using System.Buffers;
using System.ComponentModel;

namespace Azure.Core.Http
{
    public delegate string TokenRefreshDelegate(out TimeSpan delay);

    public class TokenCredential
    {
        public TokenCredential(string initialToken=null, TokenRefreshDelegate refreshDelegate=null)
        {

        }

        public string Token { get; set; }
    }
}