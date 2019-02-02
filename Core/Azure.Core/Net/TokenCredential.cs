
using Azure.Core.Http.Pipeline;
using System;
using System.Buffers;
using System.ComponentModel;
using System.Threading;

namespace Azure.Core.Http
{
    public delegate string TokenRefreshDelegate(out TimeSpan delay);

    public interface ITokenCredential
    {
        string Token { get; set; }
    }

    public class TokenCredential : ITokenCredential
    {
        private TokenCredentialImpl _impl;

        public TokenCredential(TokenRefreshDelegate refreshDelegate = null)
        {
            _impl = new TokenCredentialImpl(refreshDelegate);
        }


        ~TokenCredential()
        {
            if(_impl != null)
            {
                _impl.Dispose();
            }
        }

        public string Token { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        private class TokenCredentialImpl : ITokenCredential, IDisposable
        {
            TokenRefreshDelegate _refreshDelegate;
            Timer _timer;


            public TokenCredentialImpl(TokenRefreshDelegate refreshDelegate)
            {
                if (refreshDelegate != null)
                {
                    _refreshDelegate = refreshDelegate;

                    _timer = new Timer(RefreshToken);

                    RefreshToken(null);
                }
            }

            public string Token { get; set; }
            
            // todo full dispose impl
            public void Dispose()
            {
                if (_timer != null)
                {
                    _timer.Dispose();
                }
            }

            private void RefreshToken(object state)
            {
                Token = _refreshDelegate(out TimeSpan delay);

                // only schedule refresh is the returned delay is a positive time span
                if (delay > TimeSpan.Zero)
                {
                    // change the timer to fire once after delay has elapsed
                    _timer.Change(delay, TimeSpan.FromMilliseconds(-1));
                }
            }
        }
    }
}