using Contensive.Processor.Controllers;
using HtmlAgilityPack;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests {
    [TestClass()]
    public class MustacheTruthyControllerTests {
        [TestMethod()]
        public void processTest() {
            string test1Src = "<div class=\"mustache-truthy showIfTrue\">Sample Name</div>";
            string test1Expect = "<div>{{{#showIfTrue}}}Sample Name{{{/showIfTrue}}}</div>";
            //
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(test1Src);
            htmlDoc.GlobalAttributeValueQuote = AttributeValueQuote.Initial;
            MustacheTruthyController.process(htmlDoc);
            string test1Result = htmlDoc.DocumentNode.OuterHtml;
            //
            Assert.AreEqual(test1Expect, test1Result);
        }
    }
}