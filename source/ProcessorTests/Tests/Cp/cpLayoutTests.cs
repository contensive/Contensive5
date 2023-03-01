using Contensive.Models.Db;
using Contensive.Processor;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using static Tests.TestConstants;

namespace Tests {
    [TestClass()]
    public class cpLayoutTests {
        //
        //====================================================================================================

        [TestMethod()]
        public void getLayoutByName_Platform4_Test() {
            // -- arrange
            string layoutName = "name-" + Guid.NewGuid();
            string layoutContent = "content-" + Guid.NewGuid();
            string layoutContent5 = "content5-" + Guid.NewGuid();
            {
                using CPClass cp = new(testAppName);
                cp.Site.SetProperty("HTML PLATFORM VERSION", 4);
                LayoutModel layout = DbBaseModel.addDefault<LayoutModel>(cp);
                layout.layoutPlatform5.content = layoutContent5;
                layout.layout.content = layoutContent;
                layout.name = layoutName;
                layout.save(cp);
            }
            // -- act
            // -- assert
            {
                using CPClass cp = new(testAppName);
                string layout = cp.Layout.GetLayoutByName(layoutName);
                Assert.AreEqual(layoutContent, cp.Layout.GetLayoutByName(layoutName));
            }
        }
        [TestMethod()]
        public void getLayoutByName_Platform5_Test() {
            // -- arrange
            string layoutName = "name-" + Guid.NewGuid();
            string layoutContent = "content-" + Guid.NewGuid();
            string layoutContent5 = "content5-" + Guid.NewGuid();
            {
                using CPClass cp = new(testAppName);
                cp.Site.SetProperty("HTML PLATFORM VERSION", 5);
                LayoutModel layout = DbBaseModel.addDefault<LayoutModel>(cp);
                layout.layoutPlatform5.content = layoutContent5;
                layout.layout.content = layoutContent;
                layout.name = layoutName;
                layout.save(cp);
            }
            // -- act
            // -- assert
            {
                using CPClass cp = new(testAppName);
                string layout = cp.Layout.GetLayoutByName(layoutName);
                Assert.AreEqual(layoutContent5, cp.Layout.GetLayoutByName(layoutName));
            }
        }

        [TestMethod()]
        public void getLayoutByName_Default_Test() {
            // -- arrange
            string layoutName = "name-" + Guid.NewGuid();
            string layoutContent = "default-content-" + Guid.NewGuid();
            // -- test if method returns the detault when called
            {
                // -- act
                using CPClass cp = new(testAppName);
                cp.Site.SetProperty("HTML PLATFORM VERSION", 4);
                string readContent = cp.Layout.GetLayoutByName(layoutName, layoutContent);
                // -- assert
                Assert.AreEqual(layoutContent, readContent);
            }
            // -- test if method savewd the content
            {
                // -- act
                using CPClass cp = new(testAppName);
                string readContent = cp.Layout.GetLayoutByName(layoutName);
                // -- assert
                Assert.AreEqual(layoutContent, readContent);
            }
        }
        //
        [TestMethod()]
        public void GetLayout_Guid_P4_Test() {
            // -- arrange
            string layoutName = "name-" + Guid.NewGuid();
            string layoutContent = "content-" + Guid.NewGuid();
            string layoutContent5 = "content5-" + Guid.NewGuid();
            string layoutGuid = "";
            string layoutResult = "";
            using CPClass cp = new(testAppName);
            cp.Site.SetProperty("HTML PLATFORM VERSION", 4);
            LayoutModel layout = DbBaseModel.addDefault<LayoutModel>(cp);
            layout.layoutPlatform5.content = layoutContent5;
            layout.layout.content = layoutContent;
            layout.name = layoutName;
            layout.save(cp);
            layoutGuid = layout.ccguid;
            // -- act
            layoutResult = cp.Layout.GetLayout(layoutGuid);
            // -- assert
            Assert.AreEqual(layoutContent, layoutResult);
        }
        //
        [TestMethod()]
        public void GetLayout_Guid_P5_Test() {
            // -- arrange
            string layoutName = "name-" + Guid.NewGuid();
            string layoutContent = "content-" + Guid.NewGuid();
            string layoutContent5 = "content5-" + Guid.NewGuid();
            string layoutGuid = "";
            string layoutResult = "";
            using CPClass cp = new(testAppName);
            cp.Site.SetProperty("HTML PLATFORM VERSION", 5);
            LayoutModel layout = DbBaseModel.addDefault<LayoutModel>(cp);
            layout.layoutPlatform5.content = layoutContent5;
            layout.layout.content = layoutContent;
            layout.name = layoutName;
            layout.save(cp);
            layoutGuid = layout.ccguid;
            // -- act
            layoutResult = cp.Layout.GetLayout(layoutGuid);
            // -- assert
            Assert.AreEqual(layoutContent5, layoutResult);
        }
        //
        [TestMethod()]
        public void GetLayout_Guid_Name_File() {
            // -- arrange
            string layoutName = "name-" + Guid.NewGuid();
            string layoutContent = "content-" + Guid.NewGuid();
            string layoutGuid = Guid.NewGuid().ToString();
            string layoutResult1 = "";
            string layoutResult2 = "";
            string layoutPathFilename = "a/b/c.txt";
            {
                using CPClass cp = new(testAppName);
                cp.Site.SetProperty("HTML PLATFORM VERSION", 4);
                cp.CdnFiles.Save(layoutPathFilename, layoutContent);
                // -- act
                layoutResult1 = cp.Layout.GetLayout(layoutGuid, layoutName, layoutPathFilename);
                layoutResult2 = cp.Layout.GetLayout(layoutGuid);
            }
            // -- assert
            Assert.AreEqual(layoutContent, layoutResult1);
            Assert.AreEqual(layoutContent, layoutResult2);
        }

        [TestMethod()]
        public void GetLayout_Transform_datamustachesection() {
            // -- arrange
            // -- act
            using CPClass cp = new(testAppName);
            string source = "<div data-mustache-section=\"test\"><h1>headline</h1></div>";
            string expect = "<div>{{#test}}<h1>headline</h1>{{/test}}</div>";
            string result = cp.Layout.Transform(source);
            // -- assert
            Assert.AreEqual(expect, result);
        }

        [TestMethod()]
        public void GetLayout_Transform_datamustacheinvertedsection() {
            // -- arrange
            // -- act
            using CPClass cp = new(testAppName);
            string source = "<div data-mustache-inverted-section=\"test\"><h1>headline</h1></div>";
            string expect = "<div>{{^test}}<h1>headline</h1>{{/test}}</div>";
            string result = cp.Layout.Transform(source);
            // -- assert
            Assert.AreEqual(expect, result);
        }

        [TestMethod()]
        public void GetLayout_Transform_databody() {
            // -- arrange
            // -- act
            using CPClass cp = new(testAppName);
            string source = "<div data-body><h1>headline</h1></div>";
            string expect = "<h1>headline</h1>";
            string result = cp.Layout.Transform(source);
            // -- assert
            Assert.AreEqual(expect, result);
        }

        [TestMethod()]
        public void GetLayout_Transform_layout() {
            // -- arrange
            // -- act
            using CPClass cp = new(testAppName);
            string layoutName = cp.Utils.CreateGuid();
            string source = $"<div data-layout=\"{layoutName}\"><h1>headline</h1></div>";
            string expect1 = "<div><h1>headline</h1></div>";
            string result1 = cp.Layout.Transform(source);
            string expect2 = "<h1>headline</h1>";
            string result2 = cp.Layout.GetLayoutByName(layoutName);
            // -- assert
            Assert.AreEqual(expect1, result1);
            Assert.AreEqual(expect2, result2);
        }
    }
}
