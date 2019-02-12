using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.Security.KeyVault
{

    public class SecretCache
    {
        private SecretClient _client;
        private Dictionary<string, CacheEntry<Secret>> _cache;
        private TimeSpan _expPeriod;

        public async Task<Secret> GetAsync(Uri secretUri, CancellationToken cancellation = default)
        {
            var key = secretUri.ToString();

            if(!_cache.TryGetValue(key, out CacheEntry<Secret> entry) || entry.IsExpired())
            {
                var secret = await _client.GetAsync(secretUri, cancellation);

                entry = new CacheEntry<Secret>(secret, _expPeriod);

                _cache[key] = entry;
            }

            return entry.Value;
        }
        
    }

    internal struct CacheEntry<T>
    {
        private readonly DateTime _expiration;
        private readonly T _value;

        public CacheEntry(T value, TimeSpan validityPeriod)
        {
            _value = value;
            _expiration = DateTime.UtcNow + validityPeriod;
        }

        public bool IsExpired() => (_expiration >= DateTime.UtcNow); 

        public T Value { get => _value; }
    }
}
