using Azure.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.Security.KeyVault
{
    public interface IPoller<T> : IDisposable
    {
        T Value { get; }

        event Action<T> Updated;
    }

    public static class PollerExtensions
    {
        public static async Task<Response<IPoller<Secret>>> GetPollerAsync(this SecretClient client, string name, TimeSpan refreshPeriod = default, CancellationToken cancellation = default)
        {
            var response = await client.GetAsync(name, cancellation: cancellation);

            response.Deconstruct(out Secret secret, out Response rawResponse);

            var poller = new Poller<Secret>(secret, async (cancel) => await client.GetAsync(name, cancellation: cancel), refreshPeriod);

            return new Response<IPoller<Secret>>(rawResponse, poller);
        }

        private class Poller<T> : IPoller<T>
        {
            private T _value;
            private CancellationTokenSource _cancellationSource;
            private Func<CancellationToken, Task<T>> _poll;
            private TimeSpan _pollPeriod;

            public T Value => _value;

            public event Action<T> Updated;
            public Poller(T value, Func<CancellationToken, Task<T>> poll, TimeSpan pollPeriod)
            {
                _value = value;

                _poll = poll;

                _pollPeriod = pollPeriod;

                ScheduleNextPoll();
            }

            public void Dispose()
            {
                _poll = null;

                if (_cancellationSource != null)
                {
                    _cancellationSource.Cancel();
                }
            }

            private async Task Poll()
            {
                if (_poll != null)
                {
                    _value = await _poll(_cancellationSource.Token);

                    if (Updated != null)
                    {
                        // todo: use BeginInvoke here? should callback be sync or async?
                        Updated(_value);
                    }

                    ScheduleNextPoll();
                }
            }

            private void ScheduleNextPoll()
            {
                var _ = Task.Delay(_pollPeriod, _cancellationSource.Token).ContinueWith(t => Poll());
            }
        }
    }

}
