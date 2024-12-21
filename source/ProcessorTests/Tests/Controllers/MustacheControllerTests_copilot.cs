using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Contensive.Processor.Controllers.Tests {
    [TestClass]
    public class MustacheControllerTests {
        [TestMethod]
        public void renderStringToString_ShouldRenderTemplateWithDataSet() {
            // Arrange
            string template = "Hello, {{name}}!";
            var dataSet = new { name = "John" };

            // Act
            string result = MustacheController.renderStringToString(template, dataSet);

            // Assert
            Assert.AreEqual("Hello, John!", result);
        }
    }
}