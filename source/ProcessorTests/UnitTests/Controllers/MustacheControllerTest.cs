using Contensive.Processor;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Contensive.Processor.Tests.UnitTests.Controllers;

[TestClass()]
public class MustacheControllerTest {

    [TestMethod()]
    public void renderStringToString_Test() {
        string source = "{{Name}}-{{#Phones}}x{{.}}y{{/Phones}}";
        string expect = $"Krishna-x555-555-5555yx666-666-6666y";
        var testobj = new { Name = "Krishna", Phones = new[] { "555-555-5555", "666-666-6666" } };
        using (CPClass cp = new()) {
            string result = cp.Mustache.Render(source, testobj);
            Assert.AreEqual(expect, result);
        }
    }
}
