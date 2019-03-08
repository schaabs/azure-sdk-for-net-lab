using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Azure.Security.KeyVault
{
    public class JsonWebKey : Model
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

                foreach (var op in keyOps.EnumerateArray())
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

        internal override void WriteProperties(ref Utf8JsonWriter json)
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

                foreach (var op in KeyOps)
                {
                    json.WriteStringValue(op);
                }

                json.WriteEndArray();
            }

            if (N != null)
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

            if (DQ != null)
            {
                json.WriteString("dq", Base64Url.Encode(DQ));
            }

            if (QI != null)
            {
                json.WriteString("qi", Base64Url.Encode(QI));
            }

            if (P != null)
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
}
