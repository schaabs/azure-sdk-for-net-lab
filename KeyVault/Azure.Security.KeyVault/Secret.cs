using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;

namespace Azure.Security.KeyVault
{
    public class Secret : SecretAttributes
    {
        public Secret()
        {
        }

        public Secret(string name, string value)
            : base(name)
        {
            Value = value;
        }

        public string Value { get; private set; }

        internal override void ReadProperties(JsonElement json)
        {
            if (json.TryGetProperty("value", out JsonElement value))
            {
                Value = value.GetString();
            }

            base.ReadProperties(json);
        }

        internal override void WriteProperties(ref Utf8JsonWriter json)
        {
            if (Value != null)
            {
                json.WriteString("value", Value);
            }

            base.WriteProperties(ref json);
        }
    }

    public class SecretAttributes : Model
    {
        private ObjectId _identifier;
        private VaultAttributes _attributes;

        public SecretAttributes()
        {

        }

        public SecretAttributes(string name)
        {
            _identifier.Name = name;
        }


        public Uri Id => _identifier.Id;

        public Uri Vault => _identifier.Vault;

        public string Name => _identifier.Name;

        public string Version => _identifier.Version;

        public string ContentType { get; set; }

        public bool? Managed { get; private set; }

        public string KeyId { get; private set; }

        public bool? Enabled { get => _attributes.Enabled; set => _attributes.Enabled = value; }

        public DateTime? NotBefore { get => _attributes.NotBefore; set => _attributes.NotBefore = value; }

        public DateTime? Expires { get => _attributes.Expires; set => _attributes.Expires = value; }

        public DateTime? Created => _attributes.Created;

        public DateTime? Updated => _attributes.Updated;

        public string RecoveryLevel => _attributes.RecoveryLevel;

        public IDictionary<string, string> Tags { get; set; }

        internal override void ReadProperties(JsonElement json)
        {
            _identifier.ParseId("secrets", json.GetProperty("id").GetString());

            if (json.TryGetProperty("contentType", out JsonElement contentType))
            {
                ContentType = contentType.GetString();
            }

            if (json.TryGetProperty("kid", out JsonElement kid))
            {
                KeyId = kid.GetString();
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
            if (ContentType != null)
            {
                json.WriteString("contentType", ContentType);
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

            // Kid is read-only don't serialize

            // Managed is read-only don't serialize
        }
    }
}
