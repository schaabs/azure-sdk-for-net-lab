using System;
using System.ComponentModel;

namespace Azure.Core.Net
{
    public readonly struct ServiceCallOptions
    {
        readonly ServiceCallContext _context;

        public ServiceCallOptions(ServiceCallContext context)
            => _context = context;

        public void SetOption(object key, long value)
            => _context._options.SetOption(key, value);

        public void SetOption(object key, object value)
            => _context._options.SetOption(key, value);

        public bool TryGetOption(object key, out object value)
            => _context._options.TryGetOption(key, out value);

        public bool TryGetOption(object key, out long value)
            => _context._options.TryGetOption(key, out value);

        public long GetInt64(object key)
            => _context._options.GetInt64(key);

        public object GetObject(object key)
            => _context._options.GetInt64(key);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj) => base.Equals(obj);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode() => base.GetHashCode();

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override string ToString() => base.ToString();
    }
}
