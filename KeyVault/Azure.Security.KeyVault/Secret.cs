using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace Azure.Security.KeyVault
{

    public sealed class Secret : Model
    {        
        public string Id { get; set; }

        public string Value { get; set; }

        public string ContentType { get; set; }

        public VaultObjectAttributes Attributes { get; set; }

        public IDictionary<string, string> Tags { get; set; }

        public string Kid { get; private set; }

        public bool? Managed { get; private set; }

        protected override void ReadProperties(JsonElement json)
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

            if(json.TryGetProperty("kid", out JsonElement kid))
            {
                Kid = kid.GetString();
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

            if(json.TryGetProperty("managed", out JsonElement managed))
            {
                Managed = managed.GetBoolean();
            }

        }

    }

    public class VaultObjectAttributes : Model
    { [JsonProperty(PropertyName = "enabled")]
        public bool? Enabled { get; set; }

        /// <summary>
        /// Gets or sets not before date in UTC.
        /// </summary>
        [JsonConverter(typeof(UnixTimeJsonConverter))]
        [JsonProperty(PropertyName = "nbf")]
        public System.DateTime? NotBefore { get; set; }

        /// <summary>
        /// Gets or sets expiry date in UTC.
        /// </summary>
        [JsonConverter(typeof(UnixTimeJsonConverter))]
        [JsonProperty(PropertyName = "exp")]
        public System.DateTime? Expires { get; set; }

        /// <summary>
        /// Gets creation time in UTC.
        /// </summary>
        [JsonConverter(typeof(UnixTimeJsonConverter))]
        [JsonProperty(PropertyName = "created")]
        public System.DateTime? Created { get; private set; }

        /// <summary>
        /// Gets last updated time in UTC.
        /// </summary>
        [JsonConverter(typeof(UnixTimeJsonConverter))]
        [JsonProperty(PropertyName = "updated")]
        public System.DateTime? Updated { get; private set; }

        public bool? Enabled { get; set; }
        public DateTime? Created { get; set; }
                
        protected override void ReadProperties(JsonElement json)
        {
            if(json.TryGetProperty("enabled", out JsonElement enabled))
            {
                Enabled = enabled.GetBoolean();
            }

            if(json.TryGetProperty("nbf", out JsonElement notBefore)
            {
                
            }

        }
    }

    internal abstract class Model
    {
        public void Deserialize(Utf8JsonReader reader)
        {
        }

        public void Serialize(Utf8JsonWriter2 writer, ReadOnlySpan<byte> propertyName = null)
        {

        }

        // Making this an abstract method for now so that individual classes can explicitly write properties
        // without the need for reflection.  If we determin that the performance hit of reflection is acceptable
        // this could be implemented here.
        protected abstract void WriteProperty(Utf8JsonWriter writer, JsonAttribute attr);

        protected abstract void ReadProperties(JsonElement json);
    }

    [System.AttributeUsage(System.AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    sealed class JsonAttribute : System.Attribute
    {
        // See the attribute guidelines at
        //  http://go.microsoft.com/fwlink/?LinkId=85236
        readonly string _name;
        readonly ReadOnlySpan<byte> _utf8Name;
        readonly bool _ignored;
        readonly int _id;
        
        // This is a positional argument
        public JsonAttribute(int id = 0, string name = null, bool ignored = false)
        {
            _name = name;
            _id = id;
            _ignored = ignored;         

        }
        
        public int Id
        {
            get { return _id; }
        }

        public string Name
        {
            get { return _name; }
        }

        public ReadOnlySpan<byte> Utf8Name
        {
            get { return _utf8Name; }
        }
        
        public string PropertyName { get; set; }

        public bool Ignored
        {
            get { return _ignored; }
        }
    }

    public class ByteSpanComparer : IComparer<ReadOnlySpan<byte>>
    {
        public static ByteSpanComparer Singleton = new ByteSpanComparer();

        public int Compare(ReadOnlySpan<byte> x, ReadOnlySpan<byte> y)
        {
            int comp = 0;

            for(int i = 0; i < x.Length && i < y.Length && comp == 0; i++)
            {
                comp = x[i].CompareTo(y[i]);
            }

            if (comp == 0)
            {
                comp = x.Length.CompareTo(y.Length);
            }

            return comp;
        }
    }

}
