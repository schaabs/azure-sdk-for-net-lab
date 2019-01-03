using System;

namespace Azure.Core
{
    public static class UriExtensions
    {
        public static void AppendQuery(this UriBuilder builder, string name, string value)
        {
            if(!string.IsNullOrEmpty(builder.Query)) {
                builder.Query = builder.Query + "&" + name + "=" + value;
            } 
            else {
                builder.Query = name + "=" + value;
            }
        }

        public static void AppendQuery(this UriBuilder builder, string name, long value)
            => AppendQuery(builder, name, value.ToString());
    }
}
