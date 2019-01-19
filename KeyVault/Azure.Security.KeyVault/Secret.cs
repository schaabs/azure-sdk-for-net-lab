using System;


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
            if(string.IsNullOrEmpty(suppressObject))
            {
                writer.WriteStartObject();
            }
            else
            {
                writer.WriteStartObject(propertyName);
            }

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

            writer.WriteEndObject();
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

    [System.AttributeUsage(System.AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
    sealed class JsonAttribute : System.Attribute
    {
        // See the attribute guidelines at
        //  http://go.microsoft.com/fwlink/?LinkId=85236
        readonly string _propertyName;
        readonly ReadOnlySpan<byte> _utf8PropertyName;
        readonly bool _ignored;
        
        // This is a positional argument
        public JsonAttribute(string propertyName = null)
        {
            this._propertyName = propertyName;
            
            // TODO: Implement code here
            throw new System.NotImplementedException();
        }
        
        public string PropertyName
        {
            get { return _propertyName; }
        }

        public ReadOnlySpan<byte> Utf8PropertyName
        {
            get { return _utf8PropertyName; }
        }
        
        public bool Ignored
        {
            get { return }
        }
    }

}
