using Azure.Core.Net;
using System;
using System.Buffers;
using System.ComponentModel;
using System.Text;
using System.Text.JsonLab;

namespace Azure.Face
{
    public struct FaceDetectResult
    {
        public bool IsSuccess;

        // face attributes
        public float Age;
        public float Smile;
        public string Gender;

        public string FaceId;

        internal static FaceDetectResult Parse(ReadOnlySequence<byte> content)
            => FaceDetectResultParser.Parse(content);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj) => base.Equals(obj);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode() => base.GetHashCode();

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override string ToString() => base.ToString();
    }

    static class FaceDetectResultParser
    {
        static byte[][] s_nameTable;
        static JsonState[] s_valueTable;

        public enum JsonState : byte
        {
            Other = 0,

            gender,
            age,
            smile,
            faceId,
        }

        static void SetValue(ref Utf8JsonReader json, JsonState state, ref FaceDetectResult result)
        {
            switch (state)
            {
                // floats
                case JsonState.age: result.Age = json.GetValueAsSingle(); break;
                case JsonState.smile: result.Smile = json.GetValueAsSingle(); break;

                // strings
                case JsonState.gender: result.Gender = json.GetValueAsString(); break;
                case JsonState.faceId: result.FaceId = json.GetValueAsString(); break;

                default: break;
            }
        }

        public static FaceDetectResult Parse(ReadOnlySequence<byte> content)
        {
            var result = new FaceDetectResult();
            var json = new Utf8JsonReader(content, true);
            JsonState state = JsonState.Other;
            while (json.Read())
            {
                switch (json.TokenType)
                {
                    case JsonTokenType.PropertyName:
                        state = json.Value.ToJsonState();
                        break;
                    case JsonTokenType.Number:
                    case JsonTokenType.String:
                        SetValue(ref json, state, ref result);
                        break;
                }
            }

            result.IsSuccess = true;
            return result;
        }

        static JsonState ToJsonState(this ReadOnlySpan<byte> propertyName)
        {
            for (int i = 0; i < s_nameTable.Length; i++)
            {
                if (propertyName.SequenceEqual(s_nameTable[i]))
                {
                    return s_valueTable[i];
                }
            }
            return JsonState.Other;
        }

        static FaceDetectResultParser()
        {
            var names = Enum.GetNames(typeof(JsonState));
            s_nameTable = new byte[names.Length][];
            s_valueTable = new JsonState[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                var name = names[i];
                s_nameTable[i] = Encoding.UTF8.GetBytes(name);
                Enum.TryParse<JsonState>(name, out var value);
                s_valueTable[i] = value;
            }
        }
    }
}
