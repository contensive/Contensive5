
using Contensive.Processor;
using Contensive.Processor.Controllers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using static Tests.TestConstants;

namespace Tests {
    //
    //====================================================================================================
    //
    [TestClass()]
    public class HtmlControllerTests {
        //
        //====================================================================================================
        //
        [TestMethod]
        public void selectFromList_Test() {
            using (CPClass cp = new(testAppName)) {
                // arrange
                string expect = "<select size=1 id=\"htmlid\" class=\"htmlclass\" name=\"menuname\"><option value=\"\">nonecaption</option><option value=\"2\">a</option><option value=\"1\" selected>b</option><option value=\"3\">c</option></select>";
                string menuName = "menuname";
                int currentIndex = 1;
                List<string> lookupList = new List<string>() { "b", "a", "c" };
                string noneCaption = "nonecaption";
                string htmlId = "htmlid";
                string HtmlClass = "htmlclass";
                string result = HtmlController.selectFromList(cp.core, menuName, currentIndex, lookupList, noneCaption, htmlId, HtmlClass);
                // act
                Assert.AreEqual(expect, result);
                // assert
            }
        }
        //
        //====================================================================================================
        //
        [TestMethod]
        public void selectFromList_Test_CaptionInput() {
            using (CPClass cp = new(testAppName)) {
                // arrange
                string expect = "<select size=1 id=\"htmlid\" class=\"htmlclass\" name=\"menuname\"><option value=\"\">nonecaption</option><option value=\"2\" selected>a</option><option value=\"1\">b</option><option value=\"3\">c</option></select>";
                string menuName = "menuname";
                string currentCaption = "a";
                List<string> lookupList = new List<string>() { "b", "a", "c" };
                string noneCaption = "nonecaption";
                string htmlId = "htmlid";
                string HtmlClass = "htmlclass";
                string result = HtmlController.selectFromList(cp.core, menuName, currentCaption, lookupList, noneCaption, htmlId, HtmlClass);
                // act
                Assert.AreEqual(expect, result);
                // assert
            }
        }
        //
        //====================================================================================================
        //
        [TestMethod]
        public void convertHtmlToText_variations() {
            using (CPClass cp = new(testAppName)) {
                {
                    // arrange
                    string src = "This is plain text";
                    string expect = src;
                    // act
                    string result = HtmlController.convertHtmlToText(cp.core, src);
                    // assert
                    Assert.AreEqual(expect, result, "plain text");
                }
                {
                    // arrange
                    string src = "";
                    string expect = src;
                    // act
                    string result = HtmlController.convertHtmlToText(cp.core, src);
                    // assert
                    Assert.AreEqual(expect, result, "Empty text");
                }
                {
                    // arrange
                    string src = null;
                    string expect = "";
                    // act
                    string result = HtmlController.convertHtmlToText(cp.core, src);
                    // assert
                    Assert.AreEqual(expect, result, "Null text");
                }
                {
                    // arrange
                    string src = "line1<br>line2";
                    string expect = "line1\nline2";
                    // act
                    string result = HtmlController.convertHtmlToText(cp.core, src);
                    // assert
                    Assert.AreEqual(expect, result, "br to crlf");
                }
                {
                    // arrange
                    string src = "<p>line1</p><p>line2</p>";
                    string expect = "line1\nline2";
                    // act
                    string result = HtmlController.convertHtmlToText(cp.core, src);
                    // assert
                    Assert.AreEqual(expect, result, "<p> to crlf");
                }
                {
                    // arrange
                    string src = "<div>line1</div><div>line2</div>";
                    string expect = "line1\nline2";
                    // act
                    string result = HtmlController.convertHtmlToText(cp.core, src);
                    // assert
                    Assert.AreEqual(expect, result, "<div> to crlf");
                }
                {
                    // arrange
                    string src = "<h1>line1</h1><p>line2</p>";
                    string expect = "line1\nline2";
                    // act
                    string result = HtmlController.convertHtmlToText(cp.core, src);
                    // assert
                    Assert.AreEqual(expect, result, "h1+p to crlf");
                }
            }
        }

    }
}