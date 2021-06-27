
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

    }
}