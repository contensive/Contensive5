using HtmlAgilityPack;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Contensive.Processor.Controllers;
using Contensive.Processor;
using static Tests.TestConstants;

namespace Tests {
    [TestClass()]
    public class DataCdnControllerTests {
        //
        [TestMethod()]
        public void process_src() {
            using CPClass cp = new(testAppName);
            string pathFilename = $"img/{cp.Utils.GetRandomString(50)}.txt";
            string test1Src = $"<p><img data-cdn=\"src\" src=\"/{pathFilename}\"></p>";
            string test1Expect = $"<p><img src=\"{cp.Http.CdnFilePathPrefixAbsolute}{pathFilename}\"></p>";
            cp.WwwFiles.Save(pathFilename, cp.Utils.GetRandomString(255));
            //
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(test1Src);
            htmlDoc.GlobalAttributeValueQuote = AttributeValueQuote.Initial;
            DataCdnController.process(cp, htmlDoc);
            string test1Result = htmlDoc.DocumentNode.OuterHtml;
            //
            Assert.AreEqual(test1Expect, test1Result);
            Assert.AreEqual(test1Expect, test1Result);
            //
            Assert.AreEqual(cp.WwwFiles.Read(pathFilename), cp.CdnFiles.Read(pathFilename));
            cp.WwwFiles.DeleteFile(pathFilename);
            cp.CdnFiles.DeleteFile(pathFilename);
        }
        //
        [TestMethod()]
        public void process_src_noSlash() {
            using CPClass cp = new(testAppName);
            string pathFilename = $"img/{cp.Utils.GetRandomString(50)}.txt";
            string test1Src = $"<p><img data-cdn=\"src\" src=\"{pathFilename}\"></p>";
            string test1Expect = $"<p><img src=\"{cp.Http.CdnFilePathPrefixAbsolute}{pathFilename}\"></p>";
            cp.WwwFiles.Save(pathFilename, cp.Utils.GetRandomString(255));
            //
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(test1Src);
            htmlDoc.GlobalAttributeValueQuote = AttributeValueQuote.Initial;
            DataCdnController.process(cp, htmlDoc);
            string test1Result = htmlDoc.DocumentNode.OuterHtml;
            //
            Assert.AreEqual(test1Expect, test1Result);
            Assert.AreEqual(test1Expect, test1Result);
            //
            Assert.AreEqual(cp.WwwFiles.Read(pathFilename), cp.CdnFiles.Read(pathFilename));
            cp.WwwFiles.DeleteFile(pathFilename);
            cp.CdnFiles.DeleteFile(pathFilename);
        }
    }
}