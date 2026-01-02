
using Contensive.Models.Db;
using Contensive.Processor;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using static Tests.TestConstants;

namespace Contensive.Tests.Models.Db {
    //
    //
    [TestClass()]
    public class LinkAliasControllerTests {
        //
        //
        [TestMethod()]
        public void addDefault_Test1() {
            using (CPClass cp = new(testAppName)) {
                //
                Assert.AreEqual("/", LinkAliasModel.normalizeLinkAlias(cp, null));
                Assert.AreEqual("/", LinkAliasModel.normalizeLinkAlias(cp, ""));
                //
                Assert.AreEqual("/aaaa", LinkAliasModel.normalizeLinkAlias(cp, "aaaa"));
                Assert.AreEqual("/aaaa", LinkAliasModel.normalizeLinkAlias(cp, "\\aaaa"));
                Assert.AreEqual("/aaaa", LinkAliasModel.normalizeLinkAlias(cp, "\\aaaa\\"));
                Assert.AreEqual("/aaaa", LinkAliasModel.normalizeLinkAlias(cp, "aaaa\\"));
                //
                Assert.AreEqual("/abc", LinkAliasModel.normalizeLinkAlias(cp, "ABC"));
                Assert.AreEqual("/abc", LinkAliasModel.normalizeLinkAlias(cp, "Abc"));
                Assert.AreEqual("/abc", LinkAliasModel.normalizeLinkAlias(cp, "abc"));
                Assert.AreEqual("/abc", LinkAliasModel.normalizeLinkAlias(cp, "/abc"));
                //
                Assert.AreEqual("/a-b-c", LinkAliasModel.normalizeLinkAlias(cp, "a b c"));
                Assert.AreEqual("/a-b-c", LinkAliasModel.normalizeLinkAlias(cp, " a b c "));
                //
                Assert.AreEqual("/dont", LinkAliasModel.normalizeLinkAlias(cp, "don't"));
                Assert.AreEqual("/dont", LinkAliasModel.normalizeLinkAlias(cp, "Don't"));

            }
        }
    }
}