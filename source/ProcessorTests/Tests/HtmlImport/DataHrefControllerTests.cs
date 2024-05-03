using HtmlAgilityPack;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Contensive.Processor.Controllers;

namespace Tests {
    [TestClass()]
    public class DataHrefControllerTests {
        [TestMethod()]
        public void processTest() {
            string test1Src = "<p><a href=\"bad.html\" data-href=\"/good-link\">Click here</a></p>";
            string test1Expect = "<p><a href=\"/good-link\">Click here</a></p>";
            //
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(test1Src);
            //htmlDoc.GlobalAttributeValueQuote = AttributeValueQuote.Initial;
            DataHrefController.process(htmlDoc);
            string test1Result = htmlDoc.DocumentNode.OuterHtml;
            //
            Assert.AreEqual(test1Expect, test1Result);
        }
    }
}