using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Azure.Security.KeyVault
{
    public class Key : Model
    {
        private ObjectId _identifier;
        private VaultAttributes _attributes;

        public Uri Id => _identifier.Id;

        public Uri Vault => _identifier.Vault;

        public string Name => _identifier.Name;

        public string Version => _identifier.Version;

        public JsonWebKey KeyMaterial { get; set; }

        public bool? Managed { get; private set; }

        public bool? Enabled { get => _attributes.Enabled; set => _attributes.Enabled = value; }

        public DateTime? NotBefore { get => _attributes.NotBefore; set => _attributes.NotBefore = value; }

        public DateTime? Expires { get => _attributes.Expires; set => _attributes.Expires = value; }

        public DateTime? Created => _attributes.Created;

        public DateTime? Updated => _attributes.Updated;

        public string RecoveryLevel => _attributes.RecoveryLevel;

        public IDictionary<string, string> Tags { get; set; }


        internal override void ReadProperties(JsonElement json)
        {
            _identifier.ParseId("keys", json.GetProperty("id").GetString());

            if (json.TryGetProperty("key", out JsonElement key))
            {
                KeyMaterial = new JsonWebKey();

                KeyMaterial.ReadProperties(key);
            }

            if (json.TryGetProperty("managed", out JsonElement managed))
            {
                Managed = managed.GetBoolean();
            }

            if (json.TryGetProperty("attributes", out JsonElement attributes))
            {
                _attributes.ReadProperties(attributes);
            }

            if (json.TryGetProperty("tags", out JsonElement tags))
            {
                Tags = new Dictionary<string, string>();

                foreach (var prop in tags.EnumerateObject())
                {
                    Tags[prop.Name] = prop.Value.GetString();
                }
            }
        }

        internal override void WriteProperties(ref Utf8JsonWriter json)
        {
            // managed is read only don't serialize

            if (KeyMaterial != null)
            {
                json.WriteStartObject("key");

                KeyMaterial.WriteProperties(ref json);

                json.WriteEndObject();
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
