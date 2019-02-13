using Azure.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.Security.KeyVault
{
    public interface IRefreshable<T>
    {
        Task<T> GetValueAsync(CancellationToken cancellation = default);

        Task<T> RefreshAsync(CancellationToken cancellation = default);
    }

    static class IRefreshableExtensions
    {
        public static async Task<Response<IRefreshable<Secret>>> GetRefreshableAsync(this SecretClient client, string name, TimeSpan refreshPeriod = default, CancellationToken cancellation = default)
        {
            var response = await client.GetAsync(name, cancellation: cancellation);

            response.Deconstruct(out Secret secret, out Response rawResponse);

            var refreshable = new Refreshable<Secret>(secret, async (cancel) => await client.GetAsync(name, cancellation: cancel), refreshPeriod);

            return new Response<IRefreshable<Secret>>(rawResponse, refreshable);
        }

        private class Refreshable<T> : IRefreshable<T>
        {
            private T _value;
            private DateTime _exp;
            private TimeSpan _refreshPeriod;
            private Func<CancellationToken, Task<T>> _refresh;

            public Refreshable(T value, Func<CancellationToken, Task<T>> refresh, TimeSpan refreshPeriod)
            {
                _refreshPeriod = refreshPeriod;

                _refresh = refresh;

                Refresh(value);
            }

            public async Task<T> GetValueAsync(CancellationToken cancellation = default)
            {
                if (_exp >= DateTime.UtcNow)
                {
                    return _value;
                }

                return await RefreshAsync(cancellation);
            }

            public async Task<T> RefreshAsync(CancellationToken cancellation = default)
            {
                return Refresh(await _refresh(cancellation));
            }

            private T Refresh(T value)
            {
                _value = value;

                if (_refreshPeriod != default(TimeSpan))
                {
                    _exp = DateTime.UtcNow + _refreshPeriod;
                }

                return value;
            }
        }
    }
}
