using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Azure.Security.KeyVault
{
    internal sealed class KeyCreateParameters : Model
    {
        private VaultAttributes _attributes;

        /// <summary>
        /// Gets or sets the type of key to create. For valid values, see
        /// Microsoft.Azure.KeyVault.WebKey.JsonWebKeyType. Possible values include: 'EC', 'EC-HSM', 'RSA',
        /// 'RSA-HSM', 'oct'
        /// </summary>
        public string Kty { get; set; }

        /// <summary>
        /// Gets or sets the key size in bits. For example: 2048, 3072, or 4096
        /// for RSA.
        /// </summary>
        public int? KeySize { get; set; }

        /// <summary>
        /// </summary>
        public IList<string> KeyOps { get; set; }

        /// <summary>
        /// Gets or sets elliptic curve name. For valid values, see
        /// Microsoft.Azure.KeyVault.WebKey.JsonWebKeyCurveName. Possible values include: 'P-256', 'P-384',
        /// 'P-521', 'P-256K'
        /// </summary>
        public string Crv { get; set; }

        public bool? Enabled { get => _attributes.Enabled; set => _attributes.Enabled = value; }

        public DateTime? NotBefore { get => _attributes.NotBefore; set => _attributes.NotBefore = value; }

        public DateTime? Expires { get => _attributes.Expires; set => _attributes.Expires = value; }


        public IDictionary<string, string> Tags { get; set; }

        internal override void ReadProperties(JsonElement json)
        {
            throw new NotSupportedException("KeyCreateParameters is a internal class for serialization of createKey requests.  Deserialization is not supported.");
        }

        internal override void WriteProperties(ref Utf8JsonWriter json)
        {
            if (Kty != null)
            {
                json.WriteString("kty", Kty);
            }

            if (KeyOps != null)
            {
                json.WriteStartArray("key_ops");

                foreach (var op in KeyOps)
                {
                    json.WriteStringValue(op);
                }

                json.WriteEndArray();
            }

            if (Crv != null)
            {
                json.WriteString("crv", Crv);
            }

            if (KeySize.HasValue)
            {
                json.WriteNumber("key_size", KeySize.Value);
            }

            if (_attributes.Enabled.HasValue || _attributes.NotBefore.HasValue || _attributes.Expires.HasValue)
            {
                json.WriteStartObject("attributes");

                _attributes.WriteProperties(ref json);

                json.WriteEndObject();
            }


            if (Tags != null && Tags.Count > 0)
            {
                json.WriteStartObject("tags");

                foreach (var kvp in Tags)
                {
                    json.WriteString(kvp.Key, kvp.Value);
                }

                json.WriteEndObject();
            }
        }
    }

}
