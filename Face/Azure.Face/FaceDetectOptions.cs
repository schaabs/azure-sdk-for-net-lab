using System;
using System.ComponentModel;
using System.Text;

namespace Azure.Face
{
    public class FaceDetectOptions
    {
        public bool FaceId = true;
        public bool FaceLandmarks = false;
        public bool Age = true;
        public bool Gender = true;
        public bool HeadPose = true;
        public bool Smile = true;
        public bool FacialHair = true;
        public bool Glasses = true;
        public bool Emotion = true;
        public bool Hair = true;
        public bool Makeup = true;
        public bool Occlusion = true;
        public bool Accessories = true;
        public bool Blur = true;
        public bool Exposure = true;
        public bool Noise = true;

        static readonly byte[] s_returFaceId = Encoding.ASCII.GetBytes("returnFaceId=");
        static readonly byte[] s_returnFaceLandmarks = Encoding.ASCII.GetBytes("&returnFaceLandmarks=");
        static readonly byte[] s_returnFaceAttributes = Encoding.ASCII.GetBytes("&returnFaceAttributes=");

        static readonly byte[] s_age = Encoding.ASCII.GetBytes("age,");
        static readonly byte[] s_gender = Encoding.ASCII.GetBytes("gender,");
        static readonly byte[] s_headPose = Encoding.ASCII.GetBytes("headPose,");
        static readonly byte[] s_smile = Encoding.ASCII.GetBytes("smile,");
        static readonly byte[] s_facialHair = Encoding.ASCII.GetBytes("facialHair,");
        static readonly byte[] s_glasses = Encoding.ASCII.GetBytes("glasses,");
        static readonly byte[] s_emotion = Encoding.ASCII.GetBytes("emotion,");

        static readonly byte[] s_hair = Encoding.ASCII.GetBytes("hair,");
        static readonly byte[] s_makeup = Encoding.ASCII.GetBytes("makeup,");
        static readonly byte[] s_occlusion = Encoding.ASCII.GetBytes("occlusion,");
        static readonly byte[] s_accessories = Encoding.ASCII.GetBytes("accessories,");
        static readonly byte[] s_blur = Encoding.ASCII.GetBytes("blur,");
        static readonly byte[] s_exposure = Encoding.ASCII.GetBytes("exposure,");
        static readonly byte[] s_noise = Encoding.ASCII.GetBytes("noise,");

        internal void BuildRequestParameters(ref Utf8StringBuilder builder)
        {
            builder.Append(s_returFaceId);
            builder.Append(FaceId);
            builder.Append(s_returnFaceLandmarks);
            builder.Append(FaceLandmarks);

            if (Age | Gender | HeadPose | Smile | FacialHair | Glasses | Emotion |
                Hair | Makeup | Occlusion | Accessories | Blur | Exposure | Noise)
            {
                builder.Append(s_returnFaceAttributes);
                if (Age) builder.Append(s_age);
                if (Gender) builder.Append(s_gender);
                if (HeadPose) builder.Append(s_headPose);
                if (Smile) builder.Append(s_smile);
                if (FacialHair) builder.Append(s_facialHair);
                if (Glasses) builder.Append(s_glasses);
                if (Emotion) builder.Append(s_emotion);

                if (Hair) builder.Append(s_hair);
                if (Makeup) builder.Append(s_makeup);
                if (Occlusion) builder.Append(s_occlusion);
                if (Accessories) builder.Append(s_accessories);
                if (Blur) builder.Append(s_blur);
                if (Exposure) builder.Append(s_exposure);
                if (Noise) builder.Append(s_noise);
                builder.Written = builder.Written - 1; // removes last comma
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj) => base.Equals(obj);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode() => base.GetHashCode();

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override string ToString() => base.ToString();
    }
    struct Utf8StringBuilder
    {
        byte[] _buffer;

        public int Written { get; set; }

        public Utf8StringBuilder(int size)
        {
            _buffer = new byte[size];
            Written = 0;
        }

        public void Append(byte[] utf8)
        {
            if (_buffer.Length < Written + utf8.Length)
            {
                var largerBuffer = new byte[_buffer.Length * 2];
                _buffer.AsSpan(0, Written).CopyTo(largerBuffer);
                _buffer = largerBuffer;
            }
            utf8.CopyTo(_buffer, Written);
            Written += utf8.Length;
        }

        public void Append(ReadOnlySpan<byte> utf8)
        {
            if (_buffer.Length < Written + utf8.Length)
            {
                var largerBuffer = new byte[_buffer.Length * 2];
                _buffer.AsSpan(0, Written).CopyTo(largerBuffer);
                _buffer = largerBuffer;
            }
            utf8.CopyTo(_buffer.AsSpan(Written));
            Written += utf8.Length;
        }

        static readonly byte[] s_true = Encoding.ASCII.GetBytes("true");
        static readonly byte[] s_false = Encoding.ASCII.GetBytes("false");
        public void Append(bool value)
        {
            if (value) Append(s_true);
            else Append(s_false);
        }

        public (byte[] Buffer, int Length) Build()
        {
            var result = (_buffer, Written);
            _buffer = Array.Empty<byte>();
            Written = 0;
            return result;
        }
    }
}
