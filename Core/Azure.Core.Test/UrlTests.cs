using Azure.Core.Buffers;
using Azure.Core.Testing;
using NUnit.Framework;
using System;
using System.Text;

namespace Azure.Core.Tests
{
    public class UrlTests
    {
        TestPool<byte> pool = new TestPool<byte>();

        [Test]
        public void DeconstructBuilder() {
            Url url = "https://westcentralus.api.cognitive.microsoft.com/face/";

            var (path, host, protocol) = url;
            Assert.IsTrue(protocol == Net.ServiceProtocol.Https);
            Assert.IsTrue(path.SequenceEqual(Encoding.ASCII.GetBytes("/face/")));
            Assert.IsTrue(host.SequenceEqual(Encoding.ASCII.GetBytes("westcentralus.api.cognitive.microsoft.com")));
        }
    }
}
