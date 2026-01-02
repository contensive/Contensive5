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

        [TestMethod()]
        public void GetLayout_Transform_href() {
            // -- arrange
            // -- act
            using CPClass cp = new(testAppName);
            string href = cp.Utils.CreateGuid();
            string source = $"<div><a href=\"old\" data-href=\"{href}\"><div>headline</div></a></div>";
            string expect1 = $"<div><a href=\"{href}\"><div>headline</div></a></div>";
            string result1 = cp.Layout.Transform(source);
            // -- assert
            Assert.AreEqual(expect1, result1);
        }

        [TestMethod()]
        public void GetLayout_Transform_value() {
            // -- arrange
            // -- act
            using CPClass cp = new(testAppName);
            string value = cp.Utils.CreateGuid();
            string source = $"<div><input name=\"asdfadsf\" value=\"sdfgsdfg\" data-value=\"{value}\"><div>headline</div></div>";
            string expect1 = $"<div><input name=\"asdfadsf\" value=\"{value}\"><div>headline</div></div>";
            string result1 = cp.Layout.Transform(source);
            // -- assert
            Assert.AreEqual(expect1, result1);
        }

        [TestMethod()]
        public void GetLayout_Transform_src() {
            // -- arrange
            // -- act
            using CPClass cp = new(testAppName);
            string value = cp.Utils.CreateGuid();
            string source = $"<div><img src=\"asdfadsf\" data-src=\"{value}\"><div>headline</div></div>";
            string expect1 = $"<div><img src=\"{value}\"><div>headline</div></div>";
            string result1 = cp.Layout.Transform(source);
            // -- assert
            Assert.AreEqual(expect1, result1);
        }

        [TestMethod()]
        public void GetLayout_Transform_alt() {
            // -- arrange
            // -- act
            using CPClass cp = new(testAppName);
            string value = cp.Utils.CreateGuid();
            string source = $"<div><img alt=\"asdfadsf\" data-alt=\"{value}\"><div>headline</div></div>";
            string expect1 = $"<div><img alt=\"{value}\"><div>headline</div></div>";
            string result1 = cp.Layout.Transform(source);
            // -- assert
            Assert.AreEqual(expect1, result1);
        }

        [TestMethod()]
        public void GetLayout_Transform_addon() {
            using CPClass cp = new(testAppName);
            string value = cp.Utils.CreateGuid();
            string source = $"<div data-addon=\"{value}\"><h1>headline</h1></div>";
            string expect1 = "<div>{% \"" + value + "\" %}</div>";
            string result1 = cp.Layout.Transform(source);
            // -- assert
            Assert.AreEqual(expect1, result1);
        }

        [TestMethod()]
        public void GetLayout_Transform_innertext() {
            using CPClass cp = new(testAppName);
            string value = cp.Utils.CreateGuid();
            string source = $"<div data-innertext=\"{value}\"><h1>headline</h1></div>";
            string expect1 = $"<div>{value}</div>";
            string result1 = cp.Layout.Transform(source);
            // -- assert
            Assert.AreEqual(expect1, result1);
        }

        [TestMethod()]
        public void GetLayout_Transform_delete() {
            using CPClass cp = new(testAppName);
            // -- arrange
            string value = cp.Utils.CreateGuid();
            string source1 = $" <p>1<span data-delete>headline</span>2</p>";
            string expect1 = $" <p>12</p>";
            string source2 = $" <p>1<span data-delete>23</span>4</p>";
            string expect2 = $" <p>14</p>";
            string source3 = $" <p>This is in the layout.<span data-delete>This is not.</span></p>";
            string expect3 = $" <p>This is in the layout.</p>";
            // -- act
            string result1 = cp.Layout.Transform(source1);
            string result2 = cp.Layout.Transform(source2);
            string result3 = cp.Layout.Transform(source3);
            // -- assert
            Assert.AreEqual(expect3, result3);
            Assert.AreEqual(expect2, result2);
            Assert.AreEqual(expect1, result1);
        }
    }
}
