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
            Assert.AreEqual(300, test300.Length);
            //
            Assert.AreEqual(0, test.substringSafe(0, 0).Length);
            //
            Assert.AreEqual(5, test.substringSafe(0, 5).Length);
            //
            Assert.AreEqual(10, test.substringSafe(0, 10).Length);
            //
            Assert.AreEqual(10, test.substringSafe(0, 100).Length);
            //
            Assert.AreEqual(299, test300.substringSafe(0, 299).Length);
            //
            Assert.AreEqual(300, test300.substringSafe(0, 300).Length);
            //
            Assert.AreEqual(300, test300.substringSafe(0, 301).Length);
            //
        }
    }
}
