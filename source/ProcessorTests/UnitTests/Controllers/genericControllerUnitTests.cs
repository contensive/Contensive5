﻿
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Contensive.Processor.Controllers;
using static Contensive.Processor.Tests.testConstants;
using System.Collections.Generic;

namespace Contensive.Processor.Tests.UnitTests.Controllers {
    [TestClass]
    public class genericControllerUnitTests {
        //
        [TestMethod]
        public void Controllers_empty_test() {
            // arrange
            // act
            // assert
        }
        //
        [TestMethod]
        public void Controllers_encodeJavascript_test() {
            // arrange
            // act
            string result1 = GenericController.EncodeJavascriptStringSingleQuote("a'b");
            // assert
            Assert.AreEqual("a\\u0027b", result1);
        }
        //
        [TestMethod]
        public void Controllers_getSingular_test() {
            // arrange
            // act
            // assert
            Assert.AreEqual("toy", GenericController.getSingular_Sortof("toys"));
            Assert.AreEqual("toy", GenericController.getSingular_Sortof("toies"));
            Assert.AreEqual("TOY", GenericController.getSingular_Sortof("TOYS"));
            Assert.AreEqual("TOY", GenericController.getSingular_Sortof("TOIES"));
        }
        //
        [TestMethod]
        public void Controllers_encodeInitialCaps_test() {
            // arrange
            // act
            // assert
            Assert.AreEqual("Asdf Asdf 1234 Abcd", GenericController.encodeInitialCaps("asdf asdf 1234 ABCD"));
        }
        //
        [TestMethod]
        public void Controllers_getLinkedText_test() {
            // arrange
            // act
            string result1 = GenericController.getLinkedText("<a href=\"goHere\">", "abc<link>zzzz</link>def");
            // assert
            Assert.AreEqual("abc<a href=\"goHere\">zzzz</A>def", result1);
        }
        //
        [TestMethod]
        public void Controllers_ConvertLinkToShortLink_test() {
            // arrange
            // act
            string result1 = GenericController.ConvertLinkToShortLink("/c/d.html", "domain.com", "/a/b");
            string result2 = GenericController.ConvertLinkToShortLink("HTTP://ServerHost/ServerVirtualPath/folder/page", "ServerHost", "/ServerVirtualPath");
            // assert
            Assert.AreEqual("/c/d.html", result1);
            Assert.AreEqual("/folder/page", result2);
        }
        //
        [TestMethod]
        public void Controllers_getYesNo_test() {
            // arrange
            // act
            // assert
            Assert.AreEqual("Yes", GenericController.getYesNo(true));
            Assert.AreEqual("No", GenericController.getYesNo(false));
        }
        //
        [TestMethod]
        public void Controllers_getFirstNonzeroInteger_test() {
            // arrange            

            // act
            // assert
            Assert.AreEqual(1, GenericController.getFirstNonZeroInteger(1,2));
            Assert.AreEqual(1, GenericController.getFirstNonZeroInteger(2, 1));
            Assert.AreEqual(2, GenericController.getFirstNonZeroInteger(0, 2));
        }
        //
        [TestMethod]
        public void Controllers_getFirstNonzeroDate_test() {
            // arrange            
            DateTime zeroDate = new DateTime(1990, 8, 7);
            DateTime newBeginning = new DateTime(1999, 2, 2);
            DateTime theRenaissance = new DateTime(2003, 8, 5);

            // act
            DateTime result1 = GenericController.getFirstNonZeroDate(zeroDate, newBeginning);
            DateTime result2 = GenericController.getFirstNonZeroDate(newBeginning, theRenaissance);
            DateTime result3 = GenericController.getFirstNonZeroDate(newBeginning, DateTime.MinValue);
            // assert
            Assert.AreEqual(zeroDate, result1);
            Assert.AreEqual(newBeginning, result2);
            Assert.AreEqual(newBeginning, result3);
        }
        //
        [TestMethod]
        public void Controllers_decodeUrl_test() {
            const string pattern1 = "a/b c";
            // arrange
            string test1 = GenericController.encodeURL(pattern1);
            // act
            string result1 = GenericController.decodeURL(test1);
            // assert
            Assert.AreEqual(pattern1, result1);
        }
        //
        [TestMethod]
        public void Controllers_decodeRequestString_test() {
            // arrange
            // act

            // assert
            Assert.AreEqual("abc", GenericController.decodeResponseVariable(GenericController.encodeRequestVariable("abc")));
            Assert.AreEqual("a b", GenericController.decodeResponseVariable(GenericController.encodeRequestVariable("a b")));
            Assert.AreEqual("a=b", GenericController.decodeResponseVariable(GenericController.encodeRequestVariable("a=b")));
        }
        //
        [TestMethod]
        public void Controllers_encodeRequestVariable_test() {
            // arrange
            // act
            string result1 = GenericController.encodeRequestVariable("abc");
            string result2 = GenericController.encodeRequestVariable("a b=c");
            // assert
            Assert.AreEqual("abc", result1);
            Assert.AreEqual("a%20b%3Dc", result2);
        }
        //
        [TestMethod]
        public void Controllers_encodeQuerySting_test() {
            // arrange
            // act
            string result1 = GenericController.encodeQueryString("a=b&c=d");
            string result2 = GenericController.encodeQueryString("a=b 2&c=d");
            // assert
            Assert.AreEqual("a=b&c=d", result1);
            Assert.AreEqual("a=b%202&c=d", result2);
        }
        //
        [TestMethod]
        public void Controllers_encodeUrl_test() {
            // arrange
            // act
            string result1 = GenericController.encodeURL("http://www.a.com/b/c.html");
            string result2 = GenericController.encodeURL("1 2.html");
            // assert
            Assert.AreEqual("http%3A%2F%2Fwww.a.com%2Fb%2Fc.html", result1);
            Assert.AreEqual("1+2.html", result2);
        }
        //
        [TestMethod]
        public void Controllers_isInDelimitedString_test() {
            // arrange
            // act
            // assert
            Assert.IsTrue(GenericController.isInDelimitedString("1,2,3,4,5", "1", ","));
            Assert.IsTrue(GenericController.isInDelimitedString("1,2,3,4,5", "3", ","));
            Assert.IsTrue(GenericController.isInDelimitedString("1,2,3,4,5", "5", ","));
            Assert.IsFalse(GenericController.isInDelimitedString("1,2,3,4,5", "6", ","));
            Assert.IsTrue(GenericController.isInDelimitedString("1 2 3 4 5", "1", " "));
            Assert.IsTrue(GenericController.isInDelimitedString("1\r\n2\r\n3", "2", "\r\n"));
        }
        //
        [TestMethod]
        public void Controllers_getIntegerString_test() {
            // arrange
            // act
            // assert
            Assert.AreEqual("0123", GenericController.getIntegerString(123, 4));
        }
        //
        [TestMethod]
        public void Controllers_getRandomInteger_test() {
            using (Contensive.Processor.CPClass cp = new Contensive.Processor.CPClass(testAppName)) {
                // arrange
                // act
                int test1 = GenericController.GetRandomInteger(cp.core);
                int test2 = GenericController.GetRandomInteger(cp.core);
                // assert
                Assert.AreNotEqual(test1, test2);
            }
        }
        //
        //
        [TestMethod]
        public void Controllers_getValueFromNameValueString_test() {
            // arrange
            const string test1 = "a=1&b=2&c=3";
            // act
            string result1 = GenericController.getValueFromNameValueString("a", test1, "0", "&");
            string result2 = GenericController.getValueFromNameValueString("b", test1, "0", "&");
            string result3 = GenericController.getValueFromNameValueString("c", test1, "0", "&");
            string result4 = GenericController.getValueFromNameValueString("d", test1, "0", "&");
            // assert
            Assert.AreEqual("1", result1);
            Assert.AreEqual("2", result2);
            Assert.AreEqual("3", result3);
            Assert.AreEqual("0", result4);
        }
        //
        [TestMethod]
        public void Controllers_splitUrl_test() {
            // arrange
            const string test1 = "http://www.a.com/b/c.html?d=e";
            //
            string expect1 = Newtonsoft.Json.JsonConvert.SerializeObject( new GenericController.urlDetailsClass() {
                filename = "c.html",
                host = "www.a.com",
                port = "80",
                pathSegments  = new List<string>() { "b" },
                protocol = "http://",
                queryString = "?d=e"
            });
            // act
            string result1 = Newtonsoft.Json.JsonConvert.SerializeObject(GenericController.splitUrl(test1));
            // assert
            Assert.AreEqual(expect1, result1);
        }
        //
        [TestMethod]
        public void Controllers_splitUrl2_test() {
            // arrange
            const string url1 = "http://www.a.com/b/c.html?d=e";
            //
            string host = "";
            string path = "";
            string page = "";
            string protocol = "";
            string querystring = "";
            string port = "";
            // act
            GenericController.splitUrl(url1, ref protocol, ref host, ref port, ref path, ref page, ref querystring);
            // assert
            Assert.AreEqual("http://", protocol);
            Assert.AreEqual("www.a.com", host);
            Assert.AreEqual("80", port);
            Assert.AreEqual("/b/", path);
            Assert.AreEqual("c.html", page);
            Assert.AreEqual("?d=e", querystring);
        }
        //
        [TestMethod]
        public void Controllers_splitNewLine_test() {
            // arrange
            const string test1 = "1\r2\n3\r\n4";
            const string test2 = "1 \r 2\n 3 \r\n4";
            const string test3 = "1\r\n\r\n3";
            const string test4 = "1\r\n\n\r4";
            //
            string expect1 = Newtonsoft.Json.JsonConvert.SerializeObject(new string[] { "1", "2", "3", "4" });
            string expect2 = Newtonsoft.Json.JsonConvert.SerializeObject(new string[] { "1 ", " 2", " 3 ", "4" });
            string expect3 = Newtonsoft.Json.JsonConvert.SerializeObject(new string[] { "1", "", "3" });
            string expect4 = Newtonsoft.Json.JsonConvert.SerializeObject(new string[] { "1", "", "", "4" });
            // act
            string result1 = Newtonsoft.Json.JsonConvert.SerializeObject(GenericController.splitNewLine(test1));
            string result2 = Newtonsoft.Json.JsonConvert.SerializeObject(GenericController.splitNewLine(test2));
            string result3 = Newtonsoft.Json.JsonConvert.SerializeObject(GenericController.splitNewLine(test3));
            string result4 = Newtonsoft.Json.JsonConvert.SerializeObject(GenericController.splitNewLine(test4));
            // assert
            Assert.AreEqual(result1, expect1);
            Assert.AreEqual(result2, expect2);
            Assert.AreEqual(result3, expect3);
            Assert.AreEqual(result4, expect4);
        }
        //
        [TestMethod]
        public void Controllers_convertLinksToAbsolute_test() {
            // arrange
            const string link1 = "http://www.good.com/";
            const string link2 = "https://www.good.com/";
            const string content1 = "<div><a href=\"/page.html\"></div>";
            const string content2 = "<div><a href=\"http://www.bad.com/page.html\"></div>";
            string expect1 = "<div><a href=\"http://www.good.com/page.html\"></div>";
            string expect2 = "<div><a href=\"http://www.bad.com/page.html\"></div>";
            string expect3 = "<div><a href=\"https://www.good.com/page.html\"></div>";
            // act
            string result1 = GenericController.convertLinksToAbsolute(content1, link1);
            string result2 = GenericController.convertLinksToAbsolute(content2, link1);
            string result3 = GenericController.convertLinksToAbsolute(content1, link2);
            // assert
            Assert.AreEqual(expect1, result1);
            Assert.AreEqual(expect2, result2);
            Assert.AreEqual(expect3, result3);
        }
        //
        [TestMethod]
        public void Controllers_decodeHtml_test() {
            // arrange
            string test1 = "&quot; &amp; &apos; &minus; &lt; &gt;";
            string test2 = "&#34; &#35; &#36; &#37;";
            // act
            string result1 = HtmlController.decodeHtml(test1);
            string result2 = HtmlController.decodeHtml(test2);
            // assert
            Assert.AreEqual(result1, "\" & ' − < >");
            Assert.AreEqual(result2, "\" # $ %");
        }
        //
        [TestMethod]
        public void Controllers_modifyLinkQuery_test() {
            // arrange
            string testLink1 = "http://www.test.com/path/page.aspx?a=1&b=2";
            // act
            string result1 = GenericController.modifyLinkQuery(testLink1, "c", "3", false);
            string result2 = GenericController.modifyLinkQuery(testLink1, "c", "3", true);
            string result3 = GenericController.modifyLinkQuery(testLink1, "a", "3", true);
            string result4 = GenericController.modifyLinkQuery(testLink1, "a", "3", false);
            // assert
            Assert.AreEqual("http://www.test.com/path/page.aspx?a=1&b=2", result1);
            Assert.AreEqual("http://www.test.com/path/page.aspx?a=1&b=2&c=3", result2);
            Assert.AreEqual("http://www.test.com/path/page.aspx?a=3&b=2", result3);
            Assert.AreEqual("http://www.test.com/path/page.aspx?a=3&b=2", result4);
        }
        //
        [TestMethod]
        public void Controllers_modifyQueryString_test3() {
            // arrange
            string testLink1 = "a=1&b=2";
            // act
            string result1 = GenericController.modifyQueryString(testLink1, "c", true, false);
            string result2 = GenericController.modifyQueryString(testLink1, "c", false, false);
            string result3 = GenericController.modifyQueryString(testLink1, "c", true, true);
            string result4 = GenericController.modifyQueryString(testLink1, "c", false, true);
            string result5 = GenericController.modifyQueryString(testLink1, "a", true, false);
            string result6 = GenericController.modifyQueryString(testLink1, "a", true, true);
            // assert
            Assert.AreEqual("a=1&b=2", result1);
            Assert.AreEqual("a=1&b=2", result2);
            Assert.AreEqual("a=1&b=2&c=True", result3);
            Assert.AreEqual("a=1&b=2&c=False", result4);
            Assert.AreEqual("a=True&b=2", result5);
            Assert.AreEqual("a=True&b=2", result6);
        }
        //
        [TestMethod]
        public void Controllers_modifyQueryString_test2() {
            // arrange
            string testLink1 = "a=1&b=2";
            // act
            string result1 = GenericController.modifyQueryString(testLink1, "c", 3, false);
            string result2 = GenericController.modifyQueryString(testLink1, "c", 3, true);
            string result3 = GenericController.modifyQueryString(testLink1, "a", 3, false);
            string result4 = GenericController.modifyQueryString(testLink1, "a", 3, true);
            string result5 = GenericController.modifyQueryString(testLink1, "b", 3, false);
            string result6 = GenericController.modifyQueryString(testLink1, "b", 3, true);
            // assert
            Assert.AreEqual("a=1&b=2", result1);
            Assert.AreEqual("a=1&b=2&c=3", result2);
            Assert.AreEqual("a=3&b=2", result3);
            Assert.AreEqual("a=3&b=2", result4);
            Assert.AreEqual("a=1&b=3", result5);
            Assert.AreEqual("a=1&b=3", result6);
        }
        //
        [TestMethod]
        public void Controllers_modifyQueryString_test() {
            // arrange
            string testLink1 = "a=1&b=2";
            // act
            string result1 = GenericController.modifyQueryString(testLink1, "c", "3", false);
            string result2 = GenericController.modifyQueryString(testLink1, "c", "3", true);
            string result3 = GenericController.modifyQueryString(testLink1, "a", "3", false);
            string result4 = GenericController.modifyQueryString(testLink1, "a", "3", true);
            string result5 = GenericController.modifyQueryString(testLink1, "b", "3", false);
            string result6 = GenericController.modifyQueryString(testLink1, "b", "3", true);
            // assert
            Assert.AreEqual("a=1&b=2", result1);
            Assert.AreEqual("a=1&b=2&c=3", result2);
            Assert.AreEqual("a=3&b=2", result3);
            Assert.AreEqual("a=3&b=2", result4);
            Assert.AreEqual("a=1&b=3", result5);
            Assert.AreEqual("a=1&b=3", result6);
        }
        //
        [TestMethod]
        public void Controllers_encodeEmptyDate_test() {
            // arrange
            DateTime zeroDate = new DateTime(1990, 8, 7);
            DateTime newBeginning = new DateTime(1999, 2, 2);
            DateTime theRenaissance = new DateTime(2003, 8, 5);
            // act
            DateTime date1 = GenericController.encodeEmptyDate("", zeroDate);
            DateTime date2 = GenericController.encodeEmptyDate("8/7/1990", newBeginning);
            DateTime date3 = GenericController.encodeEmptyDate("123245", theRenaissance);
            // assert
            Assert.AreEqual(zeroDate, date1);
            Assert.AreEqual(zeroDate, date2);
            Assert.AreEqual(DateTime.MinValue, date3);
        }
        //
        [TestMethod]
        public void Controllers_encodeEmptyInteger_test() {
            // arrange
            // act
            int test1 = GenericController.encodeEmptyInteger("1", 2);
            int test2 = GenericController.encodeEmptyInteger("", 2);
            int test3 = GenericController.encodeEmptyInteger(" ", 3);
            int test4 = GenericController.encodeEmptyInteger("abcdefg", 4);
            // assert
            Assert.AreEqual(1, test1);
            Assert.AreEqual(2, test2);
            Assert.AreEqual(0, test3);
            Assert.AreEqual(0, test4);
        }
        //
        [TestMethod]
        public void Controllers_encodeEmpty_test() {
            // arrange
            // act
            string test1 = GenericController.encodeEmpty("1", "2");
            string test2 = GenericController.encodeEmpty("", "3");
            string test3 = GenericController.encodeEmpty("4", "");
            // assert
            Assert.AreEqual("1", test1);
            Assert.AreEqual("3", test2);
            Assert.AreEqual("4", test3);
        }
        //
        [TestMethod]
        public void Controllers_isGUID_test() {
            // arrange
            // act
            // assert
            Assert.IsFalse(GenericController.isGuid(""));
            Assert.IsFalse(GenericController.isGuid(" "));
            Assert.IsTrue(GenericController.isGuid("{C70BA82B-B314-466E-B29C-EAAD9C788C86}"));
            Assert.IsTrue(GenericController.isGuid("C70BA82B-B314-466E-B29C-EAAD9C788C86"));
            Assert.IsTrue(GenericController.isGuid("C70BA82BB314466EB29CEAAD9C788C86"));
            Assert.IsFalse(GenericController.isGuid("C70BA82BB314466EB29CEAAD9C788C860"));
        }
        //
        [TestMethod]
        public void Controllers_getGUID_test() {
            // arrange
            // act
            string test1 = GenericController.getGUID();
            string test2 = GenericController.getGUID(true);
            string test3 = GenericController.getGUID(false);
            // assert
            Assert.IsTrue(GenericController.isGuid(test1));
            Assert.AreEqual(38, test1.Length);
            //
            Assert.IsTrue(GenericController.isGuid(test2));
            Assert.AreEqual(38, test2.Length);
            Assert.AreEqual("{", test2.Substring(0, 1));
            Assert.AreEqual("}", test2.Substring(37, 1));
            //
            Assert.IsTrue(GenericController.isGuid(test3));
            Assert.AreNotEqual("{", test3.Substring(0, 1));
        }
        //
        [TestMethod]
        public void Controllers_span_test1() {
            // arrange
            // act
            string result = HtmlController.span("test", "testClass");
            string resultNoClass = HtmlController.span("test");
            // assert
            Assert.AreEqual("<span class=\"testClass\">test</span>", result);
            Assert.AreEqual("<span>test</span>", resultNoClass);
        }
        //
        [TestMethod]
        public void Controllers_SplitDelimited_test1() {
            // arrange
            string in1 = "this and that";
            string in2 = "this and \"that and another\"";
            string in3 = "1,2,3,4,5,6,7,8,9,0";
            string in4 = "1,,2";
            string in5 = "1 \" 2 \" 3 \" 4\" \"5 \"";
            // act
            string[] out1 = GenericController.SplitDelimited(in1, " ");
            string[] out2 = GenericController.SplitDelimited(in2, " ");
            string[] out3 = GenericController.SplitDelimited(in3, ",");
            string[] out4 = GenericController.SplitDelimited(in4, ",");
            string[] out5 = GenericController.SplitDelimited(in5, " ");
            // assert
            Assert.AreEqual(3, out1.Length);
            Assert.AreEqual(3, out2.Length);
            Assert.AreEqual(10, out3.Length);
            Assert.AreEqual(3, out4.Length);
            Assert.AreEqual(5, out5.Length);
        }
    }
}
