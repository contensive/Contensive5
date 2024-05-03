using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Contensive.Processor.Controllers;

namespace Tests {
    [TestClass()]
    public class MustacheVariableController_Tests {
        [TestMethod()]
        public void class_Test() {
            string test1Src = "<p><span class=\"mustache-basic abc\">ERROR</span>.</p>";
            string test1Expect = "<p><span>{{{abc}}}</span>.</p>";
            //
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(test1Src);
            //htmlDoc.GlobalAttributeValueQuote = AttributeValueQuote.Initial;
            MustacheVariableController.process(htmlDoc);
            string test1Result = htmlDoc.DocumentNode.OuterHtml;
            //
            Assert.AreEqual(test1Expect, test1Result);

        }

        [TestMethod()]
        public void data_Test() {
            string test1Src = "<p><span data-mustache-variable=\"abc\">ERROR</span>.</p>";
            string test1Expect = "<p><span>{{{abc}}}</span>.</p>";
            //
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(test1Src);
            //htmlDoc.GlobalAttributeValueQuote = AttributeValueQuote.Initial;
            MustacheVariableController.process(htmlDoc);
            string test1Result = htmlDoc.DocumentNode.OuterHtml;
            //
            Assert.AreEqual(test1Expect, test1Result);

        }
    }
    //

}