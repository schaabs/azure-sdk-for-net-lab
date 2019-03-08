using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Azure.Security.KeyVault
{   internal sealed class KeyImportParameters : Model
    {
        public Key Key { get; set; }

        public bool? Hsm { get; set; }

        internal override void ReadProperties(JsonElement json)
        {
            throw new NotSupportedException("KeyImportParameters is a internal class for serialization of createKey requests.  Deserialization is not supported.");
        }

        internal override void WriteProperties(ref Utf8JsonWriter json)
        {
            if (Key != null)
            {
                Key.WriteProperties(ref json);
            }

            if (Hsm != null)
            {
                json.WriteBoolean("hsm", Hsm.Value);
            }
        }
    }
}
