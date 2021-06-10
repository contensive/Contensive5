using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Tests {
    [TestClass]
    public class ExtensionUnitTests {
        //
        [TestMethod]
        public void String_substringSafe() {
            string test = "0123456789";
            Assert.AreEqual(10, test.Length);
            //
            string test300 = string.Concat(Enumerable.Repeat(test, 30));
            Assert.AreEqual(300, test.Length);
            //
            Assert.AreEqual(5, test.substringSafe(0, 5).Length);
            //
            Assert.AreEqual(254, test300.substringSafe(0, 254).Length);
            //
        }
    }
}
