using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace Azure.Security.KeyVault
{
    public sealed class Secret
    {        
        public string Id { get; set; }

        public string Value { get; set; }

        public string ContentType { get; set; }

        public SecretAttributes Attributes { get; set; }

        public IDictionary<string, string> Tags { get; set; }

        public string Kid { get; private set; }

        public bool? Managed { get; private set; }

        public void Serialize(Utf8JsonWriter2 writer, ReadOnlySpan<byte> propertyName = null)
        {

            if (Id != null)
            {
                writer.WriteString("id", Id);
            }

            if (Value != null)
            {
                writer.WriteString("value", Value);
            }

            if (ContentType != null)
            {
                writer.WriteString("contentType", ContentType);
            }

            if (Tags != null)
            {
                writer.WriteStartObject("tags");
                
                foreach(var tag in Tags)
                {
                    writer.writeString(tag.Key, tag.Value);
                }
            }
        }

        public void Deserialize(Utf8Json.Reader reader)
        {
            if (!reader.Read() || reader.JsonTokenType != JsonTokenType.StartObject)
            {
                throw new SerializationException();
            }

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.JsonTokenType != JsonTokenType.PropertyName)
                {
                    throw new SerializationException();
                }


            }
        }
    }

    public abstract class SecretAttributes
    {
        public bool? Enabled { get; set; }
        public DateTime? Created { get; set; }
                
        public int Serialize(Utf8JsonWriter2 writer, string propertyName = null)
        {
            if(string.IsNullOrEmpty(suppressObject))
            {
                writer.WriteStartObject();
            }
            else
            {
                writer.WriteStartObject(propertyName);
            }
            
            writer.WriteString("id", Id);
            writer.WriteString("value", Value);
            writer.WriteString("contentType", ContentType);
            writer.WriteStartObject("attributes");
            writer.WriteEndObject();

        }
    }

    internal abstract class Model
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
                        // filter out properties without the attribute or attributes with Ignored set
                        // sort the attributes by Utf8Name so the properties can be looked up with binary search
                        // when deserializing
                        s_jsonProperties = this.GetType().GetProperties()
                                                         .Select( prop => prop.GetCustomAttributes<JsonAttribute>().FirstOrDefault())
                                                         .Where(attr => attr != null && !attr.Ignored)
                                                         .OrderBy(attr => attr.Utf8Name, ByteSpanComparer.Singleton);
                        s_init = true;
                    }
                }
            }
        }

        public void Deserialize(Utf8JsonReader reader)
        {
            // ensure that the begining of the json text is startObject
            if (!reader.Read() || reader.JsonTokenType != JsonTokenType.StartObject)
            {
                throw new SerializationException();
            }

            // read until the end of the object
            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                // the first token in each loop iteration should be a property
                if (reader.JsonTokenType != JsonTokenType.PropertyName)
                {
                    throw new SerializationException();
                }


            }
        }

        public void Serialize(Utf8JsonWriter2 writer, ReadOnlySpan<byte> propertyName = null)
        {

            if(string.IsNullOrEmpty(propertyName))
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

        // Making this an abstract method for now so that individual classes can explicitly write properties
        // without the need for reflection.  If we determin that the performance hit of reflection is acceptable
        // this could be implemented here.
        protected abstract void WriteProperty(Utf8JsonWriter writer, JsonAttribute attr);

        // Making this an abstract method for now so that individual classes can explicitly read properties
        // without the need for reflection.  If we determin that the performance hit of reflection is acceptable
        // this could be implemented here.
        protected abstract void ReadProperty(Utf8JsonReader reader, JsonAttribute attr);
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
