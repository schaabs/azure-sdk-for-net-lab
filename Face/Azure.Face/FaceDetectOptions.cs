// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for
// license information.

using System;
using System.ComponentModel;
using System.Text;
using Azure.Core;

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

        internal void BuildRequestParameters(UriBuilder builder)
        {
            builder.Query = $"returnFaceId={FaceId}&returnFaceAttributes=age,gender";
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj) => base.Equals(obj);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode() => base.GetHashCode();

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override string ToString() => base.ToString();
    }
}
