using HtmlAgilityPack;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests {
    [TestClass()]
    public class MustacheSectionController_Tests {
        //
        // -- class test
        [TestMethod()]
        public void loopClassTest() {
            string test1Src = "<ul class=\"mustache-loop staff\"><li>Sample Name</li></ul>";
            string test1Expect = "<ul>{{#staff}}<li>Sample Name</li>{{/staff}}</ul>";
            //
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(test1Src);
            Contensive.HtmlImport.Controllers.MustacheSectionController.process(htmlDoc);
            string test1Result = htmlDoc.DocumentNode.OuterHtml;
            //
            Assert.AreEqual(test1Expect, test1Result);

        }
        //
        // -- data test
        [TestMethod()]
        public void loopDataTest() {
            string test1Src = "<ul data-mustache-section=\"staff\"><li>Sample Name</li></ul>";
            string test1Expect = "<ul>{{#staff}}<li>Sample Name</li>{{/staff}}</ul>";
            //
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(test1Src);
            Contensive.HtmlImport.Controllers.MustacheSectionController.process(htmlDoc);
            string test1Result = htmlDoc.DocumentNode.OuterHtml;
            //
            Assert.AreEqual(test1Expect, test1Result);

        }
    }
}