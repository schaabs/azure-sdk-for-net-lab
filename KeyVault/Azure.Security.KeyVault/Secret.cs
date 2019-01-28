using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;

namespace Azure.Security.KeyVault
{
    internal static class JsonExtensions
    {
        internal static IDictionary<string, string> GetStringDictionary(this Utf8JsonReader reader)
        {
            reader.Read();

            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new SerializationException();
            }

            Dictionary<string, string> dict = new Dictionary<string, string>();

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                var key = reader.GetString();

                reader.Read();

                dict[key] = reader.GetString();
            }

            return dict;
        }

        internal static void WriteStringDictionary(this Utf8JsonWriter writer, ReadOnlySpan<byte> propertyName, IDictionary<string, string> dictionary)
        {
            writer.WriteStartObject(propertyName);

            foreach (var kvp in dictionary)
            {
                writer.WriteString(kvp.Key, kvp.Value);
            }

            writer.WriteEndObject();
        }

        internal static DateTime GetUnixMillisecondTimestamp(this Utf8JsonReader reader)
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(reader.GetInt64()).DateTime;
        }

        internal static void WriteUnixMillisecondTimestamp(this Utf8JsonWriter writer, ReadOnlySpan<byte> propertyName, DateTime datetime)
        {
            writer.WriteNumber(propertyName, new DateTimeOffset(datetime).ToUnixTimeMilliseconds());
        }
    }

    public sealed class Secret : Model
    {
        private const int IdPropId = 1;
        private const int ValuePropId = 2;
        private const int ContentTypePropId = 3;
        private const int AttributesPropId = 4;
        private const int TagsPropId = 5;
        private const int KidPropId = 6;
        private const int ManagedPropId = 7;

        [Json("id", IdPropId)]
        public string Id { get; set; }

        [Json("value", ValuePropId)]
        public string Value { get; set; }
        
        [Json("contenType", ContentTypePropId)]
        public string ContentType { get; set; }

        [Json("attributes", AttributesPropId)]
        public VaultObjectAttributes Attributes { get; set; }

        [Json("tags", TagsPropId)]
        public IDictionary<string, string> Tags { get; set; }

        [Json("kid", KidPropId)]
        public string Kid { get; private set; }

        [Json("managed", ManagedPropId)]
        public bool? Managed { get; private set; }

        internal override void ReadProperty(Utf8JsonReader reader, JsonAttribute attr)
        {
            switch (attr.Id)
            {
                case IdPropId:
                    reader.Read();
                    Id = reader.GetString();
                    break;
                case ValuePropId:
                    reader.Read();
                    Value = reader.GetString();
                    break;
                case ContentTypePropId:
                    reader.Read();
                    ContentType = reader.GetString();
                    break;
                case AttributesPropId:
                    Attributes = new VaultObjectAttributes();
                    Attributes.Deserialize(reader);
                    break;
                case TagsPropId:
                    Tags = reader.GetStringDictionary();
                    break;
                case KidPropId:
                    reader.Read();
                    Kid = reader.GetString();
                    break;
                case ManagedPropId:
                    reader.Read();
                    Managed = reader.GetBoolean();
                    break;
                default:
                    throw new SerializationException();
            }
        }

        internal override void WriteProperty(Utf8JsonWriter writer, JsonAttribute attr)
        {
            switch (attr.Id)
            {
                case IdPropId:
                    if (Id != null)
                    {
                        writer.WriteString(attr.Utf8Name, Id);
                    }
                    break;
                case ValuePropId:
                    if (Value != null)
                    {
                        writer.WriteString(attr.Utf8Name, Value);
                    }
                    break;
                case ContentTypePropId:
                    if (ContentType != null)
                    {
                        writer.WriteString(attr.Utf8Name, ContentType);
                    }
                    break;
                case AttributesPropId:
                    if (Attributes != null)
                    {
                        Attributes.Serialize(writer, attr.Utf8Name);
                    }
                    break;
                case TagsPropId:
                    if(Tags != null)
                    {
                        writer.WriteStringDictionary(attr.Utf8Name, Tags);
                    }
                    break;
                case KidPropId:
                    if (Kid != null)
                    {
                        writer.WriteString(attr.Utf8Name, Kid);
                    }
                    break;
                case ManagedPropId:
                    if (Managed != null)
                    {
                        writer.WriteBoolean(attr.Utf8Name, Managed.Value);
                    }
                    break;
                default:
                    throw new SerializationException();
            }
        }
    }

    public sealed class VaultObjectAttributes : Model
    {
        private const int EnabledPropId = 1;
        private const int NotBeforePropId = 2;
        private const int ExpiresPropId = 3;
        private const int CreatedPropId = 4;
        private const int UpdatedPropId = 5;
        private const int RecoveryLevelPropId = 6;

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

        internal override void ReadProperty(Utf8JsonReader reader, JsonAttribute attr)
        {
            switch (attr.Id)
            {
                case EnabledPropId:
                    reader.Read();
                    Enabled = reader.GetBoolean();
                    break;
                case NotBeforePropId:
                    reader.Read();
                    NotBefore = reader.GetUnixMillisecondTimestamp();
                    break;
                case ExpiresPropId:
                    reader.Read();
                    Expires = reader.GetUnixMillisecondTimestamp();
                    break;
                case CreatedPropId:
                    reader.Read();
                    Created = reader.GetUnixMillisecondTimestamp();
                    break;
                case UpdatedPropId:
                    reader.Read();
                    Updated = reader.GetUnixMillisecondTimestamp();
                    break;
                case RecoveryLevelPropId:
                    reader.Read();
                    RecoveryLevel = reader.GetString();
                    break;
                default:
                    throw new SerializationException();
            }
        }

        internal override void WriteProperty(Utf8JsonWriter writer, JsonAttribute attr)
        {
            switch(attr.Id)
            {
                case EnabledPropId:
                    if(Enabled != null)
                    {
                        writer.WriteBoolean(attr.Utf8Name, Enabled.Value);
                    }
                    break;
                case NotBeforePropId:
                    if(NotBefore != null)
                    {
                        writer.WriteUnixMillisecondTimestamp(attr.Utf8Name, NotBefore.Value);
                    }
                    break;
                case ExpiresPropId:
                    if (Expires != null)
                    {
                        writer.WriteUnixMillisecondTimestamp(attr.Utf8Name, Expires.Value);
                    }
                    break;
                case CreatedPropId:
                    if (Created != null)
                    {
                        writer.WriteUnixMillisecondTimestamp(attr.Utf8Name, Created.Value);
                    }
                    break;
                case UpdatedPropId:
                    if (Updated != null)
                    {
                        writer.WriteUnixMillisecondTimestamp(attr.Utf8Name, Updated.Value);
                    }
                    break;
                case RecoveryLevelPropId:
                    if (RecoveryLevel != null)
                    {
                        writer.WriteString(attr.Utf8Name, RecoveryLevel);
                    }
                    break;
                default:
                    throw new SerializationException();
            }
        }
    }

    public abstract class Model
    {
        private static JsonAttribute[] s_jsonProperties;
        private static Object s_initLock = new Object();
        private static bool s_init = false;

        public Model()
        {
            InitializeJsonProperties();
        }

        private void InitializeJsonProperties()
        {
            if (!s_init)
            {
                lock(s_initLock)
                {
                    if(!s_init)
                    {
                        // get the JsonAttribute for all public properties on the current type
                        // filter out properties without the attribute
                        s_jsonProperties = this.GetType().GetProperties()
                                                         .Select(prop => prop.GetCustomAttributes<JsonAttribute>().FirstOrDefault())
                                                         .Where(attr => attr != null).ToArray();
                        s_init = true;
                    }
                }
            }
        }
        

        internal void Serialize(Utf8JsonWriter writer, ReadOnlySpan<byte> propertyName = default)
        {
            if(propertyName.Length == 0)
            {
                writer.WriteStartObject();
            }
            else
            {
                writer.WriteStartObject(propertyName);
            }

            for (int i = 0; i < s_jsonProperties.Length; i++)
            {
                WriteProperty(writer, s_jsonProperties[i]);
            }

            writer.WriteEndObject();
        }
        
        internal void Deserialize(Utf8JsonReader reader)
        {
            ulong visited = 0;
            ulong visiting = 1;

            reader.Read();

            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new SerializationException();
            }

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {            
                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new SerializationException();
                }

                var utf8PropName = reader.ValueSpan;

                for (int i = 0; i < s_jsonProperties.Length; i++)
                {
                    if (((visiting & visited) != 0) && (utf8PropName.SequenceEqual(s_jsonProperties[i].Utf8Name)))
                    {
                        visited &= visiting;

                        ReadProperty(reader, s_jsonProperties[i]);

                        break;
                    }
                }
            }
        }

        // Making this an abstract method for now so that individual classes can explicitly write properties
        // without the need for reflection.  If we determin that the performance hit of reflection is acceptable
        // this could be implemented here.
        internal abstract void WriteProperty(Utf8JsonWriter writer, JsonAttribute attr);

        // Making this an abstract method for now so that individual classes can explicitly read properties
        // without the need for reflection.  If we determin that the performance hit of reflection is acceptable
        // this could be implemented here.
        internal abstract void ReadProperty(Utf8JsonReader reader, JsonAttribute attr);
    }

    [System.AttributeUsage(System.AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    sealed class JsonAttribute : System.Attribute
    {
        // See the attribute guidelines at
        //  http://go.microsoft.com/fwlink/?LinkId=85236
        readonly string _name;
        readonly byte[] _utf8Name;
        readonly int _id;
        
        // This is a positional argument
        public JsonAttribute(string name, int id = 0)
        {
            _name = name;
            _id = id;
            _utf8Name = Encoding.UTF8.GetBytes(name);

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
        
    }
}
