
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
        ValueTask<string> GetTokenAsync(CancellationToken cancellation = default);
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
        private Task _initTokenComplete;

        public TokenCredential(TokenRefreshDelegate refreshDelegate)
        {
            if (refreshDelegate == null) throw new ArgumentNullException(nameof(refreshDelegate));

            _impl = new TokenCredentialImpl(refreshDelegate);
        }

        public TokenCredential(string token)
        {
            _impl = new TokenCredentialImpl(token);
        }

        ~TokenCredential()
        {
            if(_impl != null)
            {
                _impl.Dispose();
            }
        }
        

        public static async Task<TokenCredential> CreateCredentialAsync(TokenRefreshDelegate refreshDelegate)
        {
            var cred = new TokenCredential(refreshDelegate);

            await cred._impl.RefreshTokenAsync();

            return cred;
        }

        public async ValueTask<string> GetTokenAsync(CancellationToken cancellation = default)
        {
            return await _impl.GetTokenAsync();
        }

        private class TokenCredentialImpl : ITokenCredential, IDisposable
        {
            TokenRefreshDelegate _refreshDelegate;
            CancellationTokenSource _cancellationSource;
            Task _initTokenTask;
            bool _initComplete;
            string _token;

            public TokenCredentialImpl(TokenRefreshDelegate refreshDelegate)
            {
                if (refreshDelegate != null)
                {
                    _refreshDelegate = refreshDelegate;

                    _cancellationSource = new CancellationTokenSource();

                    _initTokenTask = RefreshTokenAsync();
                }
            }

            public TokenCredentialImpl(string token)
            {
                _token = token;

                _initComplete = true;
            }
                

            public async ValueTask<string> GetTokenAsync(CancellationToken token = default)
            {
                if (!_initComplete && _initTokenTask != null)
                {
                    await _initTokenTask;
                }

                return _token;
            }

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

                    _token = result.Token;

                    _initComplete = true;

                    // we don't want to await the call because we want the refresh method to return
                    // and then be reinvoked after the specified delay.  
                    var _ = Task.Delay(result.Delay, _cancellationSource.Token).ContinueWith(t => RefreshTokenAsync());
                }
            }
        }
    }
}