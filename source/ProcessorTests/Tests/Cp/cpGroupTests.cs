using Contensive.Models.Db;
using Contensive.Processor;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Tests.TestConstants;

namespace Tests {
    [TestClass()]
    public class cpGroupTests {
        //
        //====================================================================================================
        //
        [TestMethod]
        public void group_Verify_FindsGroup() {
            using (CPClass cp = new(testAppName)) {
                //
                string groupGuid = cp.Utils.CreateGuid();
                string groupName = cp.Utils.GetRandomString(255);
                string groupCaption = cp.Utils.GetRandomString(255);
                //
                var groupNew = DbBaseModel.addDefault<GroupModel>(cp);
                groupNew.name = groupName;
                groupNew.ccguid = groupGuid;
                groupNew.caption = groupCaption;
                groupNew.save(cp);
                //
                int groupId = cp.Group.verifyGroup(groupGuid, groupName);
                var groupFoundById = DbBaseModel.create<GroupModel>(cp, groupId);
                //
                Assert.AreEqual(groupNew.id, groupId);
                Assert.AreEqual(groupNew.ccguid, groupFoundById.ccguid);
                Assert.AreEqual(groupNew.name, groupFoundById.name);
                Assert.AreEqual(groupNew.caption, groupFoundById.caption);
            }
        }
        //
        //====================================================================================================
        //
        [TestMethod]
        public void group_Verify_AddsGroup() {
            using (CPClass cp = new(testAppName)) {
                //
                string groupGuid = cp.Utils.CreateGuid();
                string groupName = cp.Utils.GetRandomString(255);
                string groupCaption = cp.Utils.GetRandomString(255);
                //
                var groupNotFound = DbBaseModel.create<GroupModel>(cp, groupGuid);
                Assert.IsNull(groupNotFound);
                //
                int groupId = cp.Group.verifyGroup(groupGuid, groupName);
                var groupFoundById = DbBaseModel.create<GroupModel>(cp, groupId);
                var groupFoundByGuid = DbBaseModel.create<GroupModel>(cp, groupGuid);
                var groupFoundByName = DbBaseModel.createByUniqueName<GroupModel>(cp, groupName);
                //
                Assert.IsNotNull(groupFoundById);
                Assert.IsNotNull(groupFoundByGuid);
                Assert.IsNotNull(groupFoundByName);
                //
                Assert.AreEqual(groupId, groupFoundByGuid.id);
                Assert.AreEqual(groupId, groupFoundByName.id);
                //
                Assert.AreEqual(groupId, groupFoundById.id);
                Assert.AreEqual(groupGuid, groupFoundById.ccguid);
                Assert.AreEqual(groupName, groupFoundById.name);
                Assert.AreEqual(groupName, groupFoundById.caption);
            }
        }
        //
        //====================================================================================================
        //
        [TestMethod]
        public void group_Verify_Caption() {
            using (CPClass cp = new(testAppName)) {
                //
                string groupGuid = cp.Utils.CreateGuid();
                string groupName = cp.Utils.GetRandomString(255);
                string groupCaption = cp.Utils.GetRandomString(255);
                //
                int groupId = cp.Group.verifyGroup(groupGuid, groupName, groupCaption);
                var groupFoundById = DbBaseModel.create<GroupModel>(cp, groupId);
                //
                Assert.IsNotNull(groupFoundById);
                Assert.AreEqual(groupId, groupFoundById.id);
                Assert.AreEqual(groupGuid, groupFoundById.ccguid);
                Assert.AreEqual(groupName, groupFoundById.name);
                Assert.AreEqual(groupCaption, groupFoundById.caption);
            }
        }
        //
        //====================================================================================================
        //
        [TestMethod]
        public void group_Exists() {
            using (CPClass cp = new(testAppName)) {
                //
                string groupGuid = cp.Utils.CreateGuid();
                string groupName = cp.Utils.GetRandomString(255);
                string groupCaption = cp.Utils.GetRandomString(255);
                //
                // -- group does not exist
                Assert.IsFalse(cp.Group.exists(groupGuid));
                //
                var groupNew = DbBaseModel.addDefault<GroupModel>(cp);
                groupNew.name = groupName;
                groupNew.ccguid = groupGuid;
                groupNew.caption = groupCaption;
                groupNew.save(cp);
                //
                // -- group does exist
                Assert.IsTrue(cp.Group.exists(groupGuid));
            }
        }
        //
        //====================================================================================================
        //
        [TestMethod]
        public void group_Exists_ReturnGroupId() {
            using (CPClass cp = new(testAppName)) {
                //
                string groupGuid = cp.Utils.CreateGuid();
                string groupName = cp.Utils.GetRandomString(255);
                string groupCaption = cp.Utils.GetRandomString(255);
                //
                // -- group does not exist
                Assert.IsFalse(cp.Group.exists(groupGuid, out int notExistsGroupId));
                Assert.AreEqual(0, notExistsGroupId);
                //
                var groupNew = DbBaseModel.addDefault<GroupModel>(cp);
                groupNew.name = groupName;
                groupNew.ccguid = groupGuid;
                groupNew.caption = groupCaption;
                groupNew.save(cp);
                //
                // -- group does exist
                Assert.IsTrue(cp.Group.exists(groupGuid, out int existsGroupId));
                Assert.AreNotEqual(0, existsGroupId);
                var groupFoundById = DbBaseModel.create<GroupModel>(cp, existsGroupId);
                //
                Assert.IsNotNull(groupFoundById);
                Assert.AreEqual(groupGuid, groupFoundById.ccguid);
                Assert.AreEqual(groupName, groupFoundById.name);
                Assert.AreEqual(groupCaption, groupFoundById.caption);
            }
        }
    }
}