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

        [TestMethod()]
        public void GetLayoutTest() {
            // -- arrange
            // -- act
            // -- assert
            Assert.Fail();
        }

        [TestMethod()]
        public void GetLayoutTest1() {
            // -- arrange
            // -- act
            // -- assert
            Assert.Fail();
        }

        [TestMethod()]
        public void GetLayoutTest2() {
            // -- arrange
            // -- act
            // -- assert
            Assert.Fail();
        }

        [TestMethod()]
        public void GetLayoutTest3() {
            // -- arrange
            // -- act
            // -- assert
            Assert.Fail();
        }

        [TestMethod()]
        public void TransformTest() {
            // -- arrange
            // -- act
            // -- assert
            Assert.Fail();
        }
    }
}
