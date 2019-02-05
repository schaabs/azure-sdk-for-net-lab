using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace Azure.Security.KeyVault
{
    internal static class Base64Url
    {
    //    public static void WriteProperty(this Utf8JsonWriter json, string propertyName, string value, bool writeNull = false)
    //    {
    //        if (value != null)
    //        {
    //            json.WriteString(propertyName, value);
    //        }
    //    }

    //    public static void WriteProperty(this Utf8JsonWriter json, string propertyName, Model value, bool writeNull = false)
    //    {
    //        if (value != null)
    //        {
    //            json.WriteStartObject(propertyName);

    //            value.WriteProperties(json);

    //            json.WriteEndObject();
    //        }
    //    }

    //    public static void WriteProperty(this Utf8JsonWriter json, string propertyName, int value)
    //    {
    //        json.WriteNumber(propertyName, value);
    //    }

    //    public static void WriteProperty(this Utf8JsonWriter json, string propertyName, long value)
    //    {
    //        json.WriteNumber(propertyName, value);
    //    }

    //    public static void WriteProperty(this Utf8JsonWriter json, string propertyName, float value)
    //    {
    //        json.WriteNumber(propertyName, value);
    //    }

    //    public static void WriteProperty(this Utf8JsonWriter json, string propertyName, double value)
    //    {
    //        json.WriteNumber(propertyName, value);
    //    }

    //    public static void ReadProperty(this JsonElement json, string propertyName, out string value)
    //    {
    //    }
        
        public static byte[] Decode(string str)
        {

            str = new StringBuilder(str).Replace('-', '+').Replace('_', '/').Append('=', (str.Length % 4 == 0) ? 0 : 4 - (str.Length % 4)).ToString();

            return Convert.FromBase64String(str);
        }

        public static string Encode(byte[] bytes)
        {
            return new StringBuilder(Convert.ToBase64String(bytes)).Replace('+', '-').Replace('/', '_').Replace("=", "").ToString();
        }
        
    }

    public sealed class Key : VaultEntity
    {
        public JsonWebKey KeyMaterial { get; set; }

        public bool? Managed { get; private set; }

        internal override void ReadProperties(JsonElement json)
        {
            if (json.TryGetProperty("key", out JsonElement key))
            {
                KeyMaterial = new JsonWebKey();

                Attributes.ReadProperties(key);
            }

            if (json.TryGetProperty("managed", out JsonElement managed))
            {
                Managed = managed.GetBoolean();
            }

            base.ReadProperties(json);
        }

        internal override void WriteProperties(Utf8JsonWriter json)
        {
            if(KeyMaterial != null)
            {
                json.WriteStartObject("key");

                Attributes.WriteProperties(json);

                json.WriteEndObject();
            }

            // managed is read only don't serialize

            base.WriteProperties(json);
        }
    }

    public sealed class JsonWebKey : Model
    {
        private string _kid;
        /// <summary>
        /// Key Identifier
        /// </summary>
        public string Kid { get => _kid; set => _kid = value; }

        /// <summary>
        /// Gets or sets supported JsonWebKey key types (kty) for Elliptic
        /// Curve, RSA, HSM, Octet, usually RSA. Possible values include:
        /// 'EC', 'RSA', 'RSA-HSM', 'oct'
        /// </summary>
        public string Kty { get; set; }

        /// <summary>
        /// Supported Key Operations
        /// </summary>
        public IList<string> KeyOps { get; set; }

        #region RSA Public Key Parameters

        /// <summary>
        /// RSA modulus, in Base64.
        /// </summary>
        public byte[] N { get; set; }

        /// <summary>
        /// RSA public exponent, in Base64.
        /// </summary>
        public byte[] E { get; set; }

        #endregion

        #region RSA Private Key Parameters

        /// <summary>
        /// RSA Private Key Parameter
        /// </summary>
        public byte[] DP { get; set; }

        /// <summary>
        /// RSA Private Key Parameter
        /// </summary>
        public byte[] DQ { get; set; }

        /// <summary>
        /// RSA Private Key Parameter
        /// </summary>
        public byte[] QI { get; set; }

        /// <summary>
        /// RSA secret prime
        /// </summary>
        public byte[] P { get; set; }

        /// <summary>
        /// RSA secret prime, with p &lt; q
        /// </summary>
        public byte[] Q { get; set; }

        #endregion

        #region EC Public Key Parameters

        /// <summary>
        /// The curve for Elliptic Curve Cryptography (ECC) algorithms
        /// </summary>
        public string Crv { get; set; }

        /// <summary>
        /// X coordinate for the Elliptic Curve point.
        /// </summary>
        public byte[] X { get; set; }

        /// <summary>
        /// Y coordinate for the Elliptic Curve point.
        /// </summary>
        public byte[] Y { get; set; }

        #endregion

        #region EC and RSA Private Key Parameters

        /// <summary>
        /// RSA private exponent or ECC private key.
        /// </summary>
        public byte[] D { get; set; }

        #endregion

        #region Symmetric Key Parameters

        /// <summary>
        /// Symmetric key
        /// </summary>
        public byte[] K { get; set; }

        #endregion

        /// <summary>
        /// HSM Token, used with "Bring Your Own Key"
        /// </summary>
        public byte[] T { get; set; }

        internal override void ReadProperties(JsonElement json)
        {
            if (json.TryGetProperty("kid", out JsonElement kid))
            {
                Kid = kid.GetString();
            }

            if (json.TryGetProperty("kty", out JsonElement kty))
            {
                Kty = kty.GetString();
            }

            if (json.TryGetProperty("key_ops", out JsonElement keyOps))
            {
                KeyOps = new List<string>();

                foreach(var op in keyOps.EnumerateArray())
                {
                    KeyOps.Add(op.GetString());
                }
            }

            if (json.TryGetProperty("n", out JsonElement n))
            {
                N = Base64Url.Decode(n.GetString());
            }

            if (json.TryGetProperty("e", out JsonElement e))
            {
                E = Base64Url.Decode(e.GetString());
            }

            if (json.TryGetProperty("dp", out JsonElement dp))
            {
                DP = Base64Url.Decode(dp.GetString());
            }

            if (json.TryGetProperty("dq", out JsonElement dq))
            {
                DQ = Base64Url.Decode(dq.GetString());
            }

            if (json.TryGetProperty("qi", out JsonElement qi))
            {
                QI = Base64Url.Decode(qi.GetString());
            }

            if (json.TryGetProperty("p", out JsonElement p))
            {
                P = Base64Url.Decode(p.GetString());
            }

            if (json.TryGetProperty("q", out JsonElement q))
            {
                Q = Base64Url.Decode(q.GetString());
            }

            if (json.TryGetProperty("crv", out JsonElement crv))
            {
                Crv = crv.GetString();
            }

            if (json.TryGetProperty("x", out JsonElement x))
            {
                X = Base64Url.Decode(x.GetString());
            }
            
            if (json.TryGetProperty("y", out JsonElement y))
            {
                Y = Base64Url.Decode(y.GetString());
            }

            if (json.TryGetProperty("d", out JsonElement d))
            {
                D = Base64Url.Decode(d.GetString());
            }

            if (json.TryGetProperty("k", out JsonElement k))
            {
                K = Base64Url.Decode(k.GetString());
            }

            if (json.TryGetProperty("t", out JsonElement t))
            {
                T = Base64Url.Decode(t.GetString());
            }
        }

        internal override void WriteProperties(Utf8JsonWriter json)
        {
            if (Kid != null)
            {
                json.WriteString("kid", Kid);
            }

            if (Kty != null)
            {
                json.WriteString("kty", Kty);
            }

            if (KeyOps != null)
            {
                json.WriteStartArray("key_ops");

                foreach(var op in KeyOps)
                {
                    json.WriteStringValue(op);
                }

                json.WriteEndArray();
            }

            if(N != null)
            {
                json.WriteString("n", Base64Url.Encode(N));
            }

            if (E != null)
            {
                json.WriteString("e", Base64Url.Encode(E));
            }

            if (DP != null)
            {
                json.WriteString("dp", Base64Url.Encode(DP));
            }

            if(DQ != null)
            {
                json.WriteString("dq", Base64Url.Encode(DQ));
            }

            if(QI != null)
            {
                json.WriteString("qi", Base64Url.Encode(QI));
            }

            if(P != null)
            {
                json.WriteString("p", Base64Url.Encode(P));
            }

            if (Q != null)
            {
                json.WriteString("q", Base64Url.Encode(Q));
            }

            if (Crv != null)
            {
                json.WriteString("crv", Crv);
            }

            if (X != null)
            {
                json.WriteString("x", Base64Url.Encode(X));
            }

            if (Y != null)
            {
                json.WriteString("y", Base64Url.Encode(Y));
            }

            if (D != null)
            {
                json.WriteString("d", Base64Url.Encode(D));
            }

            if (K != null)
            {
                json.WriteString("k", Base64Url.Encode(K));
            }

            if (T != null)
            {
                json.WriteString("t", Base64Url.Encode(T));
            }
        }
    }


    public sealed class Secret : VaultEntity
    {        
        public string Value { get; set; }

        public string ContentType { get; set; }

        public string Kid { get; private set; }

        public bool? Managed { get; private set; }

        internal override void ReadProperties(JsonElement json)
        {            
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

            if(json.TryGetProperty("managed", out JsonElement managed))
            {
                Managed = managed.GetBoolean();
            }

            base.ReadProperties(json);
        }

        internal override void WriteProperties(Utf8JsonWriter json)
        {
            if (Value != null)
            {
                json.WriteString("value", Value);
            }

            if (ContentType != null)
            {
                json.WriteString("contentType", ContentType);
            }

            base.WriteProperties(json);

            // Kid is read-only don't serialize

            // Managed is read-only don't serialize
        }

    }

    public abstract class VaultEntity : Model
    {
        public string Id { get; private set; }

        public IDictionary<string, string> Tags { get; set; }

        public VaultEntityAttributes Attributes { get; set; }

        internal override void ReadProperties(JsonElement json)
        {
            Id = json.GetProperty("id").GetString();

            if (json.TryGetProperty("attributes", out JsonElement attributes))
            {
                Attributes = new VaultEntityAttributes();

                Attributes.ReadProperties(attributes);
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

        internal override void WriteProperties(Utf8JsonWriter json)
        {
            // Id is read-only don't serialize

            if (Attributes != null)
            {
                json.WriteStartObject("attributes");

                Attributes.WriteProperties(json);

                json.WriteEndObject();
            }

            if (Tags != null)
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

    public sealed class VaultEntityAttributes : Model
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

        internal override void WriteProperties(Utf8JsonWriter json)
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
        public void Deserialize(Stream content)
        {
            using (JsonDocument json = JsonDocument.Parse(content, default))
            {
                this.ReadProperties(json.RootElement);
            }
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


        // TODO (pri 3): CoreFx will soon have a type like this. We should remove this one then.
        internal class FixedSizedBufferWriter : IBufferWriter<byte>
        {
            private readonly byte[] _buffer;
            private int _count;

            public FixedSizedBufferWriter(byte[] buffer)
            {
                _buffer = buffer;
            }

            public Memory<byte> GetMemory(int minimumLength = 0) => _buffer.AsMemory(_count);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Span<byte> GetSpan(int minimumLength = 0) => _buffer.AsSpan(_count);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Advance(int bytes)
            {
                _count += bytes;
                if (_count > _buffer.Length)
                {
                    throw new InvalidOperationException("Cannot advance past the end of the buffer.");
                }
            }
        }
        
    }
    

}
