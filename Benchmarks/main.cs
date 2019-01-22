// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for
// license information.

using Azure.Core;
using Azure.Core.Http;
using Azure.Core.Http.Pipeline;
using Azure.Face;
using Microsoft.ProjectOxford.Face;
using System;
using System.Buffers;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    static readonly string s_image = @"https://upload.wikimedia.org/wikipedia/commons/5/50/Albert_Einstein_%28Nobel%29.png";
    static readonly Uri s_imageUrl = new Uri(s_image);
    static readonly string s_account = "https://westcentralus.api.cognitive.microsoft.com/face/";
    static readonly string s_key = "7f31d0b1e62b4b5cb985bcdb966204f9";
    static readonly CancellationTokenSource s_cancellation = new CancellationTokenSource();
    static readonly string s_mockResponse =
    #region MockResponse
@"HTTP/1.1 200 OK
Cache-Control: no-cache
Pragma: no-cache
Content-Length: 979
Content-Type: application/json; charset=utf-8
Expires: -1
X-AspNet-Version: 4.0.30319
X-Powered-By: ASP.NET
apim-request-id: 07651c03-5749-426e-965c-04f5ef7138a1
Strict-Transport-Security: max-age=31536000; includeSubDomains; preload
x-content-type-options: nosniff
Date: Thu, 15 Nov 2018 17:49:19 GMT

[{
""faceId"":""b5689a64-3ee1-4b5c-a857-c222f9e4b0c6"",
""faceRectangle"": { 
    ""top"":109,
    ""left"":89,
    ""width"":139,
    ""height"":139
},
""faceAttributes"": {
    ""smile"":0.0,
    ""headPose"":{
    ""pitch"":0.0,
    ""roll"":-0.9,
    ""yaw"":-1.0
},
""gender"":""male"",
""age"":55.0,
""facialHair"": {
    ""moustache"":0.1,
    ""beard"":0.6,
    ""sideburns"":0.1
},
""glasses"":""NoGlasses"",
""emotion"":{
    ""anger"":0.0,
    ""contempt"":0.0,
    ""disgust"":0.0,
    ""fear"":0.0,
    ""happiness"":0.0,
    ""neutral"":0.998,
    ""sadness"":0.002,
    ""surprise"":0.0
},
""blur"":{
    ""blurLevel"":""low"",
    ""value"":0.09
},
""exposure"":{
    ""exposureLevel"":""goodExposure"",
    ""value"":0.65},
    ""noise"":{""noiseLevel"":""high"",""value"":0.86},
    ""makeup"":{""eyeMakeup"":false,""lipMakeup"":false},
    ""accessories"":[],
    ""occlusion"":{
        ""foreheadOccluded"":false,
        ""eyeOccluded"":false,
        ""mouthOccluded"":false
    },
    ""hair"":{
        ""bald"":0.02,
        ""invisible"":false,
        ""hairColor"":[
            {""color"":""gray"",""confidence"":0.99},
            {""color"":""blond"",""confidence"":0.97},
            {""color"":""brown"",""confidence"":0.44},
            {""color"":""other"",""confidence"":0.18},
            {""color"":""black"",""confidence"":0.07},
            {""color"":""red"",""confidence"":0.02}
        ]
    }
}}]}";
    #endregion
    static readonly byte[] s_mockResponseBytes = System.Text.Encoding.UTF8.GetBytes(s_mockResponse);

    static FaceClient s_httpService = new FaceClient(s_account, s_key);
    static FaceClient s_socketService = new FaceClient(s_account, s_key, new PipelineOptions() { Transport = new SocketClientTransport() });
    static FaceClient s_socketMockService = new FaceClient(s_account, s_key, new PipelineOptions() { Transport = new MockSocketTransport(s_mockResponseBytes) });
    static FaceServiceClient s_sdkService = new FaceServiceClient(s_key, s_account + "v1.0");

    public static async Task<int> DetectOverHttpClient()
    {
        var response = await s_httpService.DetectAsync(s_cancellation.Token, s_imageUrl);
        var status = response.Status;
        if (status != 200) throw new Exception();
        response.Dispose();
        return status;
    }

    public static async Task<int> DetectOverSockets()
    {
        var response = await s_socketService.DetectAsync(s_cancellation.Token, s_imageUrl);
        var status = response.Status;
        if (status != 200) throw new Exception();
        response.Dispose();
        return status;
    }

    public static async Task<int> DetectOverSdk()
    {
        var attributes = new FaceAttributeType[] {
            FaceAttributeType.Gender,
            FaceAttributeType.Age,
            FaceAttributeType.Smile,
            FaceAttributeType.Glasses,
            FaceAttributeType.HeadPose,
            FaceAttributeType.FacialHair,
            FaceAttributeType.Emotion,
            FaceAttributeType.Hair,
            FaceAttributeType.Makeup,
            FaceAttributeType.Occlusion,
            FaceAttributeType.Accessories,
            FaceAttributeType.Noise,
            FaceAttributeType.Exposure,
            FaceAttributeType.Blur
        };

        var results = await s_sdkService.DetectAsync(s_image, true, false, attributes);
        var result = results[0];
        if (result.FaceAttributes.Age < 30 || result.FaceAttributes.Age > 100) throw new Exception();
        return result.GetHashCode();
    }

    public static async Task<int> DetectOverMock()
    {
        var response = await s_socketMockService.DetectAsync(s_cancellation.Token, s_imageUrl);
        var status = response.Status;
        if (status != 200) throw new Exception();
        response.Dispose();
        return status;
    }

    static void Main()
    {
        Thread.Sleep(1000);
        //prime the pool
        for (int i = 0; i < 100; i++)
        {
            var array = ArrayPool<byte>.Shared.Rent(4096);
            ArrayPool<byte>.Shared.Return(array);
        }
        var stopwatch = new Stopwatch();

        Run(stopwatch, "MOCK  ", ()=> { var result = DetectOverMock().Result; });
        Run(stopwatch, "SOCKET", () => { var result = DetectOverSockets().Result; });
        Run(stopwatch, "HTTP  ", () => { var result = DetectOverHttpClient().Result; });
        Run(stopwatch, "SDK   ", ()=> { var result = DetectOverSdk().Result; });

        Console.WriteLine("Note that the allocation size is misleading; it counts only the current thread.");
        Console.WriteLine("Press ENTER to exit ...");
        Console.ReadLine();
    }

    static void Run(Stopwatch stopwatch, string name, Action test)
    {
        int itterations = 5;
        // warmup
        {
            stopwatch.Restart();
            var allocated = GC.GetAllocatedBytesForCurrentThread();
            test();
            allocated = GC.GetAllocatedBytesForCurrentThread() - allocated;
            var ms = stopwatch.ElapsedMilliseconds;
        }
        {
            // run
            var allocated = GC.GetAllocatedBytesForCurrentThread();
            stopwatch.Restart();
            for (int i = 0; i < itterations; i++)
            {
                test();
            }
            var ms = stopwatch.ElapsedMilliseconds/ itterations;
            var bytes = (GC.GetAllocatedBytesForCurrentThread() - allocated) / itterations;
            if(bytes > 1000)
            {
                Console.WriteLine($"{name}\t: {ms}ms\t{((float)bytes)/1000.0:N2}KB");
            }
            else
            {
                Console.WriteLine($"{name}\t: {ms}ms\t{bytes}B");
            }

        }
    }
}