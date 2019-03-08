using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Azure.Security.KeyVault
{
    public class KeyOperationResult : Model
    {
        public string Kid { get; private set; }

        public byte[] Result { get; private set; }

        internal override void ReadProperties(JsonElement json)
        {
            throw new NotImplementedException();
        }

        internal override void WriteProperties(ref Utf8JsonWriter json)
        {
            throw new NotImplementedException();
        }
    }
}
