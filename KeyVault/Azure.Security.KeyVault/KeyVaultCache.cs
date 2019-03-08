using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.Security.KeyVault
{

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
