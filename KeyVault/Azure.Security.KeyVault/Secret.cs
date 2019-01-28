using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace Azure.Security.KeyVault
{

    public sealed class Secret : Model
    {        
        public string Id { get; private set; }

        public string Value { get; set; }

        public string ContentType { get; set; }

        public VaultObjectAttributes Attributes { get; set; }

        public IDictionary<string, string> Tags { get; set; }

        public string Kid { get; private set; }

        public bool? Managed { get; private set; }

        internal override void ReadProperties(JsonElement json)
        {
            Id = json.GetProperty("id").GetString();
            
            if(json.TryGetProperty("value", out JsonElement value))
            {
                Value = value.GetString();
            }
            
            if(json.TryGetProperty("contentType", out JsonElement contentType))
            {
                ContentType = contentType.GetString();
            }

            if(json.TryGetProperty("attributes", out JsonElement attributes))
            {
                Attributes = new VaultObjectAttributes();

                Attributes.ReadProperties(attributes);
            }

            if(json.TryGetProperty("tags", out JsonElement tags))
            {
                Tags = new Dictionary<string, string>();

                foreach(var prop in tags.EnumerateObject())
                {
                    tags[prop.Name] = prop.Value.GetString();
                }
            }

            if(json.TryGetProperty("kid", out JsonElement kid))
            {
                Kid = kid.GetString();
            }

            if(json.TryGetProperty("managed", out JsonElement managed))
            {
                Managed = managed.GetBoolean();
            }
        }

        internal override void WriteProperties(Utf8JsonWriter json)
        {
            // Id is read-only don't serialize

            if (Value != null)
            {
                json.WriteString("value", Value);
            }

            if (ContentType != null)
            {
                json.WriteString("contentType", ContentType);
            }

            if (Attributes != null)
            {
                json.WriteStartObject("attributes");

                Attributes.WriteProperties(json);

                json.WriteEndObject();
            }

            if (Tags != null)
            {
                json.WriteStartObject("tags");

                foreach(var kvp in Tags)
                {
                    json.WriteString(kvp.Key, kvp.Value);
                }

                json.WriteEndObject();
            }

            // Kid is read-only don't serialize

            // Managed is read-only don't serialize
        }

    }

    public sealed class VaultObjectAttributes : Model
    {
        public bool? Enabled { get; set; }

        /// <summary>
        /// Gets or sets not before date in UTC.
        /// </summary>
        public System.DateTime? NotBefore { get; set; }

        /// <summary>
        /// Gets or sets expiry date in UTC.
        /// </summary>
        public System.DateTime? Expires { get; set; }

        /// <summary>
        /// Gets creation time in UTC.
        /// </summary>
        public System.DateTime? Created { get; private set; }

        /// <summary>
        /// Gets last updated time in UTC.
        /// </summary>
        public System.DateTime? Updated { get; private set; }

        /// <summary>
        /// Gets reflects the deletion recovery level currently in effect for
        /// secrets in the current vault. If it contains 'Purgeable', the
        /// secret can be permanently deleted by a privileged user; otherwise,
        /// only the system can purge the secret, at the end of the retention
        /// interval. Possible values include: 'Purgeable',
        /// 'Recoverable+Purgeable', 'Recoverable',
        /// 'Recoverable+ProtectedSubscription'
        /// </summary>
        public string RecoveryLevel { get; private set; }

        internal override void ReadProperties(JsonElement json)
        {
            if(json.TryGetProperty("enabled", out JsonElement enabled))
            {
                Enabled = enabled.GetBoolean();
            }

            if(json.TryGetProperty("nbf", out JsonElement nbf))
            {
                NotBefore = DateTimeOffset.FromUnixTimeMilliseconds(nbf.GetInt64()).DateTime;
            }

            if(json.TryGetProperty("exp", out JsonElement exp))
            {
                Expires = DateTimeOffset.FromUnixTimeMilliseconds(exp.GetInt64()).DateTime;
            }

            if(json.TryGetProperty("created", out JsonElement created))
            {
                Created = DateTimeOffset.FromUnixTimeMilliseconds(created.GetInt64()).DateTime;
            }

            if(json.TryGetProperty("updated", out JsonElement updated))
            {
                Updated = DateTimeOffset.FromUnixTimeMilliseconds(updated.GetInt64()).DateTime;
            }

            if(json.TryGetProperty("recoveryLevel", out JsonElement recoveryLevel))
            {
                RecoveryLevel = recoveryLevel.GetString();
            }
        }

        internal override WriteProperties(Utf8JsonWriter json)
        {
            if (Enabled.HasValue)
            {
                json.WriteBoolean("enabled", Enabled.Value);
            }

            if (NotBefore.HasValue)
            {
                json.WriteNumber("nbf", new DateTimeOffset(NotBefore.Value).ToUnixTimeMilliseconds());
            }
            
            if (Expires.HasValue)
            {
                json.WriteNumber("exp", new DateTimeOffset(Expires.Value).ToUnixTimeMilliseconds());
            }

            // Created is read-only don't serialize
            // Updated is read-only don't serialize
            // RecoveryLevel is read-only don't serialize
        }
    }

    public abstract class Model
    {
        public void Deserialize(Utf8JsonReader reader)
        {
        }

        public ReadOnlyMemory<byte> Serialize()
        {
            byte[] buffer = new byte[1024];

            var writer = new FixedSizedBufferWriter(buffer);
            
            var json = new Utf8JsonWriter(writer);
            
            json.WriteStartObject();

            WriteProperties(json);

            json.WriteEndObject();

            return buffer.AsMemory(0, json.BytesWritten);
        }

        internal abstract void WriteProperties(Utf8JsonWriter json);

        internal abstract void ReadProperties(JsonElement json);
    }
    

}
