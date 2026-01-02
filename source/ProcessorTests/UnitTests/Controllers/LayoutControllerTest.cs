using Amazon.SimpleEmail;
using Contensive.Models.Db;
using Contensive.Processor;
using Contensive.Processor.Controllers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using static Tests.TestConstants;

namespace Tests {
    [TestClass()]
    public class LayoutControllerTest {

        [TestMethod()]
        public void UpdateLayout_modeBS5_setDefault_Test() {
            using (CPClass cp = new(testAppName)) {
                string testGuid = "{test-guid}";
                string testName = "test-name";
                string testPathFilename = $"testfile{cp.Utils.GetRandomString(10)}.html";
                string testContent = cp.Utils.GetRandomString(10);
                int layoutContentId = cp.Content.GetID("Layouts");
                cp.CdnFiles.Save(testPathFilename, testContent);
                cp.core.siteProperties.setProperty("html platform version", 5);
                //
                DbBaseModel.delete<LayoutModel>(cp, testGuid);
                LayoutModel before = DbBaseModel.create<LayoutModel>(cp, testGuid);
                Assert.IsNull(before);
                //
                cp.Layout.updateLayout(testGuid, testName, testPathFilename);
                //
                LayoutModel after = DbBaseModel.create<LayoutModel>(cp, testGuid);
                Assert.AreEqual(testContent, after.layout.content);
                Assert.AreEqual("", after.layoutPlatform5.content);
                Assert.AreEqual(testGuid, after.ccguid);
                Assert.AreEqual(testName, after.name);
                Assert.AreEqual(layoutContentId, after.contentControlId);
                //
                DbBaseModel.delete<LayoutModel>(cp, testGuid);
            }
        }

        [TestMethod()]
        public void UpdateLayout_modeBS5_setDefault_setContentId_Test() {
            using (CPClass cp = new(testAppName)) {
                string testGuid = "{test-guid}";
                string testName = "test-name";
                string testPathFilename = $"testfile{cp.Utils.GetRandomString(10)}.html";
                string testContent = cp.Utils.GetRandomString(10);

                ContentModel layoutSub = DbBaseModel.addDefault<ContentModel>(cp );
                layoutSub.name = $"Layout sub {cp.Utils.GetRandomString(10)}";
                layoutSub.parentId = cp.Content.GetID("Layouts");
                layoutSub.save(cp);
                //
                cp.CdnFiles.Save(testPathFilename, testContent);
                cp.core.siteProperties.setProperty("html platform version", 5);
                //
                DbBaseModel.delete<LayoutModel>(cp, testGuid);
                LayoutModel before = DbBaseModel.create<LayoutModel>(cp, testGuid);
                Assert.IsNull(before);
                //
                cp.Layout.updateLayout(testGuid, layoutSub.id, testName, testPathFilename);
                //
                LayoutModel after = DbBaseModel.create<LayoutModel>(cp, testGuid);
                Assert.AreEqual(testContent, after.layout.content);
                Assert.AreEqual("", after.layoutPlatform5.content);
                Assert.AreEqual(testGuid, after.ccguid);
                Assert.AreEqual(testName, after.name);
                Assert.AreEqual(layoutSub.id, after.contentControlId);
                //
                DbBaseModel.delete<LayoutModel>(cp, testGuid);
                DbBaseModel.delete<ContentModel>(cp, layoutSub.id);
            }
        }

        [TestMethod()]
        public void UpdateLayout_modeBS4_setDefault_Test() {
            using (CPClass cp = new(testAppName)) {
                string testGuid = "{test-guid}";
                string testName = "test-name";
                string testPathFilename = $"testfile{cp.Utils.GetRandomString(10)}.html";
                string testContent = cp.Utils.GetRandomString(10);
                int layoutContentId = cp.Content.GetID("Layouts");
                cp.CdnFiles.Save(testPathFilename, testContent);
                cp.core.siteProperties.setProperty("html platform version", 4);
                //
                DbBaseModel.delete<LayoutModel>(cp, testGuid);
                LayoutModel before = DbBaseModel.create<LayoutModel>(cp, testGuid);
                Assert.IsNull(before);
                //
                cp.Layout.updateLayout(testGuid, testName, testPathFilename);
                //
                LayoutModel after = DbBaseModel.create<LayoutModel>(cp, testGuid);
                Assert.AreEqual(testContent, after.layout.content);
                Assert.AreEqual("", after.layoutPlatform5.content);
                Assert.AreEqual(testGuid, after.ccguid);
                Assert.AreEqual(testName, after.name);
                Assert.AreEqual(layoutContentId, after.contentControlId);
                //
                //
                DbBaseModel.delete<LayoutModel>(cp, testGuid);
            }
        }

        [TestMethod()]
        public void UpdateLayout_modeBS5_setBoth_Test() {
            using (CPClass cp = new(testAppName)) {
                string testGuid = "{test-guid}";
                string testName = "test-name";
                string testPathFilename4 = $"testfile{cp.Utils.GetRandomString(10)}.html";
                string testContent4 = cp.Utils.GetRandomString(10);
                int layoutContentId = cp.Content.GetID("Layouts");
                cp.CdnFiles.Save(testPathFilename4, testContent4);
                string testPathFilename5 = $"testfile{cp.Utils.GetRandomString(10)}.html";
                string testContent5 = cp.Utils.GetRandomString(10);
                cp.CdnFiles.Save(testPathFilename5, testContent5);
                cp.core.siteProperties.setProperty("html platform version", 5);
                //
                DbBaseModel.delete<LayoutModel>(cp, testGuid);
                LayoutModel before = DbBaseModel.create<LayoutModel>(cp, testGuid);
                Assert.IsNull(before);
                //
                cp.Layout.updateLayout(testGuid, testName, testPathFilename4, testPathFilename5);
                //
                LayoutModel after = DbBaseModel.create<LayoutModel>(cp, testGuid);
                Assert.AreEqual(testContent4, after.layout.content);
                Assert.AreEqual(testContent5, after.layoutPlatform5.content);
                Assert.AreEqual(testGuid, after.ccguid);
                Assert.AreEqual(testName, after.name);
                Assert.AreEqual(layoutContentId, after.contentControlId);
                //
                //
                DbBaseModel.delete<LayoutModel>(cp, testGuid);
            }
        }
    }
}