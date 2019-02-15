
using Azure.Core.Http.Pipeline;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.Core.Http
{
    public interface ITokenCredential
    {
        string Token { get; }
    }

    public interface ITokenCredentialProvider
    {
        Task<ITokenCredential> GetCredentialAsync(IEnumerable<string> scopes = default, CancellationToken cancellation = default);
    }

    public delegate Task<TokenRefreshResult> TokenRefreshDelegate(CancellationToken cancellation);

    public struct TokenRefreshResult
    {
        public string Token;
        public TimeSpan Delay;
    }

    public class TokenCredential : ITokenCredential
    {
        private TokenCredentialImpl _impl;

        protected TokenCredential(TokenRefreshDelegate refreshDelegate = null)
        {
            _impl = new TokenCredentialImpl(refreshDelegate);
        }

        public TokenCredential(string token)
            : this(refreshDelegate: null)
        {
            Token = token;
        }

        ~TokenCredential()
        {
            if(_impl != null)
            {
                _impl.Dispose();
            }
        }

        public string Token { get => _impl.Token; set => _impl.Token = value; }

        public static async Task<TokenCredential> CreateCredentialAsync(TokenRefreshDelegate refreshDelegate)
        {
            var cred = new TokenCredential(refreshDelegate);

            await cred._impl.RefreshTokenAsync();

            return cred;
        }

        private class TokenCredentialImpl : ITokenCredential, IDisposable
        {
            TokenRefreshDelegate _refreshDelegate;
            CancellationTokenSource _cancellationSource;


            public TokenCredentialImpl(TokenRefreshDelegate refreshDelegate)
            {
                if (refreshDelegate != null)
                {
                    _refreshDelegate = refreshDelegate;

                    _cancellationSource = new CancellationTokenSource();
                }
            }

            public string Token { get; set; }
            
            // todo full dispose impl
            public void Dispose()
            {
                if (_cancellationSource != null)
                {
                    _cancellationSource.Cancel();

                    _cancellationSource.Dispose();
                }
            }
            
            public async Task RefreshTokenAsync()
            {
                if (!_cancellationSource.IsCancellationRequested)
                {
                    var result = await _refreshDelegate(_cancellationSource.Token);

                    Token = result.Token;

                    // we don't want to await the call because we want the refresh method to return
                    // and then be reinvoked after the specified delay.  
                    var _ = Task.Delay(result.Delay, _cancellationSource.Token).ContinueWith(t => RefreshTokenAsync());
                }
            }
        }
    }
}