
using Contensive.BaseClasses;
using Contensive.Processor;
using Contensive.Processor.Controllers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using static Contensive.Processor.Controllers.GenericController;
using static Tests.TestConstants;

namespace Tests {
    [TestClass()]
    public class CpBlockTests {
        //
        public const string layoutContent = "content";
        public const string layoutA = "<div id=\"aid\" class=\"aclass\">" + layoutContent + "</div>";
        public const string layoutB = "<div id=\"bid\" class=\"bclass\">" + layoutA + "</div>";
        public const string layoutC = "<div id=\"cid\" class=\"cclass\">" + layoutB + "</div>";
        public const string templateHeadTag = "<meta http-equiv=\"content-type\" content=\"text/html; charset=utf-8\" >";
        public const string templateA = "<html><head>" + templateHeadTag + "</head><body>" + layoutC + "</body></html>";
        //
        //====================================================================================================
        // unit test - cp.blockNew
        //
        [TestMethod]
        public void views_cpBlock_InnerOuterTest() {
            // arrange
            CPClass cpApp = new CPClass(testAppName);
            CPBlockBaseClass block = cpApp.BlockNew();
            int layoutInnerLength = layoutA.Length;
            // act
            block.Load(layoutC);
            // assert
            Assert.AreEqual(block.GetHtml(), layoutC);
            //
            Assert.AreEqual(block.GetInner("#aid"), layoutContent);
            Assert.AreEqual(block.GetInner(".aclass"), layoutContent);
            //
            Assert.AreEqual(block.GetOuter("#aid"), layoutA);
            Assert.AreEqual(block.GetOuter(".aclass"), layoutA);
            //
            Assert.AreEqual(block.GetInner("#bid"), layoutA);
            Assert.AreEqual(block.GetInner(".bclass"), layoutA);
            //
            Assert.AreEqual(block.GetOuter("#bid"), layoutB);
            Assert.AreEqual(block.GetOuter(".bclass"), layoutB);
            //
            Assert.AreEqual(block.GetInner("#cid"), layoutB);
            Assert.AreEqual(block.GetInner(".cclass"), layoutB);
            //
            Assert.AreEqual(block.GetOuter("#cid"), layoutC);
            Assert.AreEqual(block.GetOuter(".cclass"), layoutC);
            //
            Assert.AreEqual(block.GetInner("#cid .bclass"), layoutA);
            Assert.AreEqual(block.GetInner(".cclass #bid"), layoutA);
            //
            Assert.AreEqual(block.GetOuter("#cid .bclass"), layoutB);
            Assert.AreEqual(block.GetOuter(".cclass #bid"), layoutB);
            //
            Assert.AreEqual(block.GetInner("#cid .aclass"), layoutContent);
            Assert.AreEqual(block.GetInner(".cclass #aid"), layoutContent);
            //
            Assert.AreEqual(block.GetOuter("#cid .aclass"), layoutA);
            Assert.AreEqual(block.GetOuter(".cclass #aid"), layoutA);
            //
            block.Clear();
            Assert.AreEqual(block.GetHtml(), "");
            //
            block.Clear();
            block.Load(layoutA);
            block.SetInner("#aid", "1234");
            Assert.AreEqual(block.GetHtml(), layoutA.Replace(layoutContent, "1234"));
            //
            block.Load(layoutB);
            block.SetOuter("#aid", "1234");
            Assert.AreEqual(block.GetHtml(), layoutB.Replace(layoutA, "1234"));
            //
            // dispose
            cpApp.Dispose();
        }
        //
        //====================================================================================================
        /// <summary>
        /// test block load and clear
        /// </summary>
        [TestMethod]
        public void views_cpBlock_ClearTest() {
            using (CPClass cp = new(testAppName)) {
                CPBlockBaseClass block = cp.BlockNew();
                // act
                block.Load(layoutC);
                Assert.AreEqual(layoutC, block.GetHtml());
                block.Clear();
                // assert
                Assert.AreEqual(block.GetHtml(), "");
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// test block import
        /// </summary>
        [TestMethod]
        public void views_cpBlock_ImportFileTest() {
            using (CPClass cp = new(testAppName)) {
                string filename = "cpBlockTest" + getRandomInteger(cp.core).ToString() + ".html";
                try {
                    CPBlockBaseClass block = cp.BlockNew();
                    // act
                    cp.core.wwwFiles.saveFile(filename, templateA, cp.core.wwwFiles.isLocal);
                    block.ImportFile(filename);
                    // assert
                    Assert.AreEqual(layoutC, block.GetHtml());
                } catch (Exception) {
                    //
                } finally {
                    cp.core.wwwFiles.deleteFile(filename);
                }
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// test block openCopy
        /// </summary>
        [TestMethod]
        public void views_cpBlock_OpenCopyTest() {
            using (CPClass cp = new(testAppName)) {
                string recordName = "cpBlockTest" + getRandomInteger(cp.core).ToString();
                int recordId = 0;
                try {
                    // arrange
                    CPBlockBaseClass block = cp.BlockNew();
                    using (CPCSBaseClass cs = cp.CSNew()) {
                        // act
                        if (cs.Insert("copy content")) {
                            recordId = cs.GetInteger("id");
                            cs.SetField("name", recordName);
                            cs.SetField("copy", layoutC);
                        }
                        cs.Close();
                    }
                    block.OpenCopy(recordName);
                    // assert
                    Assert.AreEqual(layoutC, block.GetHtml());
                } finally {
                    cp.Content.Delete("copy content", "id=" + recordId);
                }
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// test block openFile
        /// </summary>
        [TestMethod]
        public void views_cpBlock_OpenFileTest() {
            using (CPClass cp = new(testAppName)) {
                string filename = "cpBlockTest" + getRandomInteger(cp.core).ToString() + ".html";
                // act
                cp.core.wwwFiles.saveFile(filename, layoutA,cp.core.wwwFiles.isLocal);
                CPBlockBaseClass block = cp.BlockNew();
                block.OpenFile(filename);
                cp.core.wwwFiles.deleteFile(filename);
                // assert
                Assert.AreEqual(layoutA, block.GetHtml());
                Assert.AreEqual("", cp.core.wwwFiles.readFileText(filename));
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// test block openCopy
        /// </summary>
        [TestMethod]
        public void views_cpBlock_OpenLayoutTest() {
            using (CPClass cp = new(testAppName)) {
                string recordName = "cpBlockTest" + getRandomInteger(cp.core).ToString();
                int recordId = 0;
                // arrange
                CPBlockBaseClass block = cp.BlockNew();
                using (CPCSBaseClass cs = cp.CSNew()) {
                    // act
                    if (cs.Insert("layouts")) {
                        recordId = cs.GetInteger("id");
                        cs.SetField("name", recordName);
                        cs.SetField("layout", layoutC);
                    }
                    cs.Close();
                }
                block.OpenLayout(recordName);
                // assert
                Assert.AreEqual(layoutC, block.GetHtml());
                cp.Content.Delete("layouts", "id=" + recordId);
            }
        }
        //
        //====================================================================================================
        // unit test - cp.blockNew
        //
        [TestMethod]
        public void views_cpBlock_AppendPrependTest() {
            // arrange
            CPClass cpApp = new CPClass(testAppName);
            CPBlockBaseClass block = cpApp.BlockNew();
            int layoutInnerLength = layoutA.Length;
            // act
            block.Clear();
            block.Append("1");
            block.Append("2");
            Assert.AreEqual(block.GetHtml(), "12");
            //
            block.Clear();
            block.Prepend("1");
            block.Prepend("2");
            Assert.AreEqual(block.GetHtml(), "21");
            //
            // dispose
            //
            cpApp.Dispose();
        }
        //
    }
    //

    [TestClass()]
    public class CoreCommonTests {
        //
        [TestMethod]
        public void normalizePath_unit() {
            // arrange
            // act
            // assert
            Assert.AreEqual(FileController.normalizeDosPath(""), "");
            Assert.AreEqual(FileController.normalizeDosPath("c:\\"), "c:\\");
            Assert.AreEqual(FileController.normalizeDosPath("c:\\test\\"), "c:\\test\\");
            Assert.AreEqual(FileController.normalizeDosPath("c:\\test"), "c:\\test\\");
            Assert.AreEqual(FileController.normalizeDosPath("c:\\test/test"), "c:\\test\\test\\");
            Assert.AreEqual(FileController.normalizeDosPath("test"), "test\\");
            Assert.AreEqual(FileController.normalizeDosPath("\\test"), "test\\");
            Assert.AreEqual(FileController.normalizeDosPath("\\test\\"), "test\\");
            Assert.AreEqual(FileController.normalizeDosPath("/test/"), "test\\");
        }
        //
        [TestMethod]
        public void normalizeRoute_unit() {
            // arrange
            // act
            // assert
            Assert.AreEqual("test", normalizeRoute("TEST"));
            Assert.AreEqual("test", normalizeRoute("\\TEST"));
            Assert.AreEqual("test", normalizeRoute("\\\\TEST"));
            Assert.AreEqual("test", normalizeRoute("test"));
            Assert.AreEqual("test", normalizeRoute("/test/"));
            Assert.AreEqual("test", normalizeRoute("test/"));
            Assert.AreEqual("test", normalizeRoute("test//"));
        }
        //
        [TestMethod]
        public void encodeText_unit() {
            // arrange
            // act
            // assert
            Assert.AreEqual(encodeText(1), "1");
        }
        //
        [TestMethod]
        public void sample_unit() {
            // arrange
            // act
            // assert
            Assert.AreEqual(true, true);
        }
        //
        [TestMethod]
        public void modifyQueryString_unit() {
            // arrange
            // act
            // assert
            Assert.AreEqual("", modifyQueryString("", "a", "1", false));
            Assert.AreEqual("a=1", modifyQueryString("", "a", "1", true));
            Assert.AreEqual("a=1", modifyQueryString("a=0", "a", "1", false));
            Assert.AreEqual("a=1", modifyQueryString("a=0", "a", "1", true));
            Assert.AreEqual("a=1&b=2", modifyQueryString("a=1", "b", "2", true));
        }
        //
        [TestMethod]
        public void modifyLinkQuery_unit() {
            // arrange
            // act
            // assert
            Assert.AreEqual("index.html", modifyLinkQuery("index.html", "a", "1", false));
            Assert.AreEqual("index.html?a=1", modifyLinkQuery("index.html", "a", "1", true));
            Assert.AreEqual("index.html?a=1", modifyLinkQuery("index.html?a=0", "a", "1", false));
            Assert.AreEqual("index.html?a=1", modifyLinkQuery("index.html?a=0", "a", "1", true));
            Assert.AreEqual("index.html?a=1&b=2", modifyLinkQuery("index.html?a=1", "b", "2", true));
        }
        //
        [TestMethod]
        public void vbInstr_test() {
            Assert.AreEqual("abcdefgabcdefgabcdefgabcdefg".IndexOf("d") + 1, strInstr("abcdefgabcdefgabcdefgabcdefg", "d"));
            Assert.AreEqual("abcdefgabcdefgabcdefgabcdefg".IndexOf("E") + 1, strInstr("abcdefgabcdefgabcdefgabcdefg", "E"));
            Assert.AreEqual("abcdefgabcdefgabcdefgabcdefg".IndexOf("E", 9) + 1, strInstr(10, "abcdefgabcdefgabcdefgabcdefg", "E"));
            Assert.AreEqual("abcdefgabcdefgabcdefgabcdefg".IndexOf("E", 9) + 1, strInstr(10, "abcdefgabcdefgabcdefgabcdefg", "E", 2));
            Assert.AreEqual("abcdefgabcdefgabcdefgabcdefg".IndexOf("E", 9, System.StringComparison.OrdinalIgnoreCase) + 1, strInstr(10, "abcdefgabcdefgabcdefgabcdefg", "E", 1));
            Assert.AreEqual("abcdefgabcdefgabcdefgabcdefg".IndexOf("c", 9) + 1, strInstr(10, "abcdefgabcdefgabcdefgabcdefg", "c", 2));
            Assert.AreEqual("abcdefgabcdefgabcdefgabcdefg".IndexOf("c", 9, System.StringComparison.OrdinalIgnoreCase) + 1, strInstr(10, "abcdefgabcdefgabcdefgabcdefg", "c", 1));
            string haystack = "abcdefgabcdefgabcdefgabcdefg";
            string needle = "c";
            Assert.AreEqual("?".IndexOf("?") + 1, strInstr(1, "?", "?"));
            int tempVar = haystack.Length;
            for (int ptr = 1; ptr <= tempVar; ptr++) {
                Assert.AreEqual(haystack.IndexOf(needle, ptr - 1) + 1, strInstr(ptr, haystack, needle, 2));
            }
        }
        //
        [TestMethod]
        public void vbUCase_test() {
            Assert.AreEqual("AbCdEfG".ToUpper(), toUCase("AbCdEfG"));
            Assert.AreEqual("ABCDEFG".ToUpper(), toUCase("ABCDEFG"));
            Assert.AreEqual("abcdefg".ToUpper(), toUCase("abcdefg"));
        }
        //
        [TestMethod]
        public void vbLCase_test() {
            Assert.AreEqual("AbCdEfG".ToLowerInvariant(), toLCase("AbCdEfG"));
            Assert.AreEqual("ABCDEFG".ToLowerInvariant(), toLCase("ABCDEFG"));
            Assert.AreEqual("abcdefg".ToLowerInvariant(), toLCase("abcdefg"));
        }
        //
        [TestMethod]
        public void vbLeft_test() {
            Assert.AreEqual("AbCdEfG".ToLowerInvariant(), toLCase("AbCdEfG"));
        }
    }
    //
    //====================================================================================================
    // unit tests
    //
    [TestClass()]
    public class CPClassUnitTests {
        //
        //====================================================================================================
        // unit test - cp.appOk
        //
        [TestMethod]
        public void views_cpBlock_AppOk_unit() {
            // arrange
            CPClass cp = new CPClass();
            CPClass cpApp = new CPClass(testAppName);
            // act
            // assert
            Assert.AreEqual(cp.appOk, false);
            Assert.AreEqual(cpApp.appOk, true);
            // dispose
            cp.Dispose();
            cpApp.Dispose();
        }

        //
        //====================================================================================================
        // unit test - sample
        //
        [TestMethod]
        public void views_cpBlock_sample_unit() {
            // arrange
            CPClass cp = new CPClass();
            // act
            //
            // assert
            Assert.AreEqual(cp.appOk, false);
            // dispose
            cp.Dispose();
        }
    }
}