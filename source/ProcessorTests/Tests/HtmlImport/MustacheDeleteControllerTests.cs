using Contensive.Processor.Controllers;
using HtmlAgilityPack;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests {
    [TestClass()]
    public class MustacheDeleteControllerTests {
        [TestMethod()]
        public void processTest() {
            string test1Src = "<p class=\"mustache-delete\">ERROR</p>";
            string test1Expect = "";
            //
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(test1Src);
            //htmlDoc.GlobalAttributeValueQuote = AttributeValueQuote.Initial;
            DataDeleteController.process(htmlDoc);
            string test1Result = htmlDoc.DocumentNode.OuterHtml;
            //
            Assert.AreEqual(test1Expect, test1Result);

        }
    }
}