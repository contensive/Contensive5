using HtmlAgilityPack;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests {
    [TestClass()]
    public class MustacheInvertedSectionControllerTests {
        [TestMethod()]
        public void processTest() {
            string test1Src = "<div data-mustache-inverted-section=\"showIfFalse\">Sample Name</div>";
            string test1Expect = "<div>{{^showIfFalse}}Sample Name{{/showIfFalse}}</div>";
            //
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(test1Src);
            Contensive.HtmlImport.Controllers.MustacheInvertedSectionController.process(htmlDoc);
            string test1Result = htmlDoc.DocumentNode.OuterHtml;
            //
            Assert.AreEqual(test1Expect, test1Result);
        }
    }
}