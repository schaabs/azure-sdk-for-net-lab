using System;
using System.ComponentModel;
using System.Text;

namespace Azure.Core.Net
{
    public struct ETag : IEquatable<ETag>
    {
        byte[] _ascii;

        public ETag(string etag) => _ascii = Encoding.ASCII.GetBytes(etag);

        public bool Equals(ETag other) => true;

        public override int GetHashCode() => 0;

        public static bool operator ==(ETag left, ETag rigth)
            => left.Equals(rigth);

        public static bool operator !=(ETag left, ETag rigth)
            => !left.Equals(rigth);

        public override string ToString()
            => Encoding.ASCII.GetString(_ascii);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj)
        {
            if (obj is ETag other) return this == other;
            return false;
        }
    }

    // TODO (pri 2): I dont like this name
    public struct ModifiedAccessConditions : IEquatable<ModifiedAccessConditions>
    {
        public ETag IfMatch;
        public ETag IfNoneMatch;

        public bool Equals(ModifiedAccessConditions other)
            => IfMatch.Equals(other.IfMatch) && IfNoneMatch.Equals(other.IfNoneMatch);

        public override int GetHashCode()
            => IfMatch.GetHashCode() ^ IfNoneMatch.GetHashCode();

        public static bool operator ==(ModifiedAccessConditions left, ModifiedAccessConditions rigth)
            => left.Equals(rigth);

        public static bool operator !=(ModifiedAccessConditions left, ModifiedAccessConditions rigth)
            => !left.Equals(rigth);
               
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj)
        {
            if (obj is ModifiedAccessConditions other) return this == other;
            return false;
        }
    }
}
