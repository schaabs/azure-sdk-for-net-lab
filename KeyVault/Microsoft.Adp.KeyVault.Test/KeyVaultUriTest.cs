using NUnit.Framework;

namespace Microsoft.Adp.KeyVault.Test
{
    public class KeyVaultUriTest
    {
        [Test]
        public void ConstructorTest() {
            Assert.NotNull(new KeyVaultUri());
        }
    }
}
