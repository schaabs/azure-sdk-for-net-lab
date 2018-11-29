using System;

namespace Azure.Core.Net
{
    public enum ServiceMethod : byte
    {
        Get,
        Post,
        Put
    }

    public enum ServiceProtocol : byte
    {
        Http,
        Https,
        Other,
    }
}
