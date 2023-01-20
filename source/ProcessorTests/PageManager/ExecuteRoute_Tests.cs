﻿
using Contensive.Models.Db;
using Contensive.Processor;
using Contensive.Processor.Models.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Tests.TestConstants;

namespace Tests {
    /// <summary>
    /// 
    /// </summary>
    [TestClass()]
    public class ExecuteRoute_Tests {
        /// <summary>
        /// plain remote method
        /// </summary>
        [TestMethod]
        public void ExeuteRoute_RemoteMethod_Test() {
            using (CPClass cp = new(testAppName)) {
                //
                // arrange
                cp.Site.SetProperty("ALLOW HTML MINIFY", false);
                //
                // arrange
                const string guidBaseCollection = "{7C6601A7-9D52-40A3-9570-774D0D43D758}";
                AddonCollectionModel baseCollection = DbBaseModel.create<AddonCollectionModel>(cp, guidBaseCollection);
                //
                AddonModel addon = DbBaseModel.addDefault<AddonModel>(cp);
                addon.name = cp.Utils.GetRandomInteger().ToString();
                addon.dotNetClass = "Contensive.Processor.Addons.TestAddon";
                addon.remoteMethod = true;
                addon.collectionId = baseCollection.id;
                addon.save(cp);
                string testString = cp.Utils.GetRandomInteger().ToString();
                cp.Doc.SetProperty("test-in", testString);
                // act
                string doc = cp.executeRoute("/" + addon.name);
                // assert 
                Assert.AreEqual(testString, cp.Doc.GetText("test-out"));
                Assert.IsTrue(doc.Contains(testString));
                // no html
                Assert.IsFalse(doc.ToLower().Contains("<html>"));
                Assert.IsFalse(doc.ToLower().Contains("<head>"));
                Assert.IsFalse(doc.ToLower().Contains("<body>"));
                // cleanup
                DbBaseModel.delete<AddonModel>(cp, addon.id);
            }
        }
        /// <summary>
        /// plain remote method
        /// </summary>
        [TestMethod]
        public void ExeuteRoute_RemoteMethod_Html_Test() {
            using (CPClass cp = new(testAppName)) {
                //
                // arrange
                cp.Site.SetProperty("ALLOW HTML MINIFY", false);
                //
                const string guidBaseCollection = "{7C6601A7-9D52-40A3-9570-774D0D43D758}";
                AddonCollectionModel baseCollection = DbBaseModel.create<AddonCollectionModel>(cp, guidBaseCollection);
                //
                AddonModel addon = DbBaseModel.addDefault<AddonModel>(cp);
                addon.name = cp.Utils.GetRandomInteger().ToString();
                addon.dotNetClass = "Contensive.Processor.Addons.TestAddon";
                addon.remoteMethod = true;
                addon.collectionId = baseCollection.id;
                addon.htmlDocument = true;
                addon.save(cp);
                string testString = cp.Utils.GetRandomInteger().ToString();
                cp.Doc.SetProperty("test-in", testString);
                //
                // act
                string doc = cp.executeRoute("/" + addon.name);
                //
                // assert 
                Assert.AreEqual(testString, cp.Doc.GetText("test-out"));
                Assert.IsTrue(doc.Contains(testString));
                // no html
                Assert.IsTrue(doc.ToLower().Contains("<html"));
                Assert.IsTrue(doc.ToLower().Contains("<head"));
                Assert.IsTrue(doc.ToLower().Contains("<body"));
                // cleanup
                DbBaseModel.delete<AddonModel>(cp, addon.id);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        [TestMethod]
        public void ExeuteRoute_Page_Test() {
            using (CPClass cp = new(testAppName)) {
                //
                // arrange
                cp.Site.SetProperty("ALLOW HTML MINIFY", false);
                //
                PageContentModel testPage = DbBaseModel.addDefault<PageContentModel>(cp);
                testPage.name = cp.Utils.GetRandomInteger().ToString();
                testPage.addonList = "";
                testPage.save(cp);
                //
                // act
                string doc = cp.executeRoute("/" + testPage.name);
                //
                // assert 
                Assert.AreEqual("", doc);
                // no html
                Assert.IsTrue(doc.ToLower().Contains("<html"));
                Assert.IsTrue(doc.ToLower().Contains("<head"));
                Assert.IsTrue(doc.ToLower().Contains("<body"));
                // cleanup
                DbBaseModel.delete<PageContentModel>(cp, testPage.id);
            }
        }

    }
}