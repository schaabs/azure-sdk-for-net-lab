using NUnit.Framework;

namespace Microsoft.Azure.Template.Test
{
    public class HelloTest
    {
        [Test]
        public void MessageTest() {
            Assert.AreEqual("hello", (new Hello()).Message);
        }
    }
}
