using Contensive.Processor;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Contensive.Processor.Tests.UnitTests.Controllers;

[TestClass]
public class MustacheControllerTests {
    [TestMethod]
    public void renderStringToString_ShouldRenderTemplateWithDataSet() {
        // Arrange
        string template = "Hello, {{name}}!";
        var dataSet = new { name = "John" };

        // Act
        using (CPClass cp = new()) {
            string result = cp.Mustache.Render(template, dataSet);

            // Assert
            Assert.AreEqual("Hello, John!", result);
        }
    }
}
