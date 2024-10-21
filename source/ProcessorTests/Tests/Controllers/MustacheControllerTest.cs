using Contensive.Processor.Controllers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests {
    [TestClass()]
    public class MustacheControllerTest {

        [TestMethod()]
        public void renderStringToString_Test() {
            string source = $"{{Name}}-{{#Phones}}x{{Phones}}y{{/Phones}}";
            string expect = $"Krishna-x555-555-5555yx666-666-6666y";
            var testobj = new { Name = "Krishna", Phones = new[] { "555-555-5555", "666-666-6666" } };
            string result = MustacheController.renderStringToString(source, testobj);
            Assert.AreEqual(expect, result);
        }
    }
}