﻿
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Contensive.Processor.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Tests.TestConstants;
using Contensive.Processor;
using System.Data;
using Contensive.Models.Db;

namespace Contensive.ProcessorTests.UnitTests.ControllerTests {
    //
    //====================================================================================================
    //
    [TestClass()]
    public class GroupControllerTests {
        //
        //====================================================================================================
        //
        [TestMethod]
        public void controllers_Group_blank() {
            using (CPClass cp = new CPClass(testAppName)) {
                // arrange
                CoreController core = ((CPClass)cp).core;
                core.cache.invalidateAll();
                core.session.verifyUser();
                // act
                // assert
                Assert.AreEqual("", "");
            }
        }
        //
        //====================================================================================================
        //
        [TestMethod]
        public void controllers_Group_Add_StringString() {
            using (CPClass cp = new CPClass(testAppName)) {
                // arrange
                CoreController core = ((CPClass)cp).core;
                core.cache.invalidateAll();
                core.session.verifyUser();
                core.db.executeNonQuery("delete from ccgroups");
                // act
                GroupController.add(core, "groupName", "groupCaption");
                //
                // assert
                DataTable dt = core.db.executeQuery("select * from ccgroups");
                Assert.IsNotNull(dt);
                Assert.AreEqual(1, dt.Rows.Count);
                Assert.AreEqual("groupName", dt.Rows[0]["name"]);
                Assert.AreEqual("groupCaption", dt.Rows[0]["caption"]);
                Assert.AreEqual(true, GenericController.encodeBoolean(dt.Rows[0]["active"]));
            }
        }
        //
        //====================================================================================================
        //
        [TestMethod]
        public void controllers_Group_Add_String() {
            using (CPClass cp = new CPClass(testAppName)) {
                // arrange
                CoreController core = ((CPClass)cp).core;
                core.cache.invalidateAll();
                core.session.verifyUser();
                core.db.executeNonQuery("delete from ccgroups");
                // act
                GroupController.add(core, "test1");
                //
                // assert
                DataTable dt = core.db.executeQuery("select * from ccgroups");
                Assert.IsNotNull(dt);
                Assert.AreEqual(1, dt.Rows.Count);
                Assert.AreEqual("test1", dt.Rows[0]["name"]);
                Assert.AreEqual("test1", dt.Rows[0]["caption"]);
                Assert.AreEqual(true, GenericController.encodeBoolean(dt.Rows[0]["active"]));
            }
        }
        //
        //====================================================================================================
        //
        [TestMethod]
        public void controllers_Group_AddUser_String() {
            using (CPClass cp = new CPClass(testAppName)) {
                //
                // arrange
                CoreController core = ((CPClass)cp).core;
                core.cache.invalidateAll();
                core.session.verifyUser();
                DbBaseModel.deleteRows<GroupModel>(cp, "");
                DbBaseModel.deleteRows<MemberRuleModel>(cp, "");
                GroupController.add(core, "Group1");
                //
                // act - add current use to new group
                GroupController.addUser(core, "Group1");
                //
                // assert
                GroupModel group = DbBaseModel.createByUniqueName<GroupModel>(cp, "Group1");
                Assert.IsNotNull(group);
                List<MemberRuleModel> ruleList = DbBaseModel.createList<MemberRuleModel>(cp, "(groupId=" + group.id + ")and(memberid=" + cp.User.Id + ")");
                Assert.AreEqual(1, ruleList.Count);
            }
        }
        //
        //====================================================================================================
        //
        [TestMethod]
        public void controllers_Group_AddUser_Group_Person_Date() {
            using (CPClass cp = new CPClass(testAppName)) {
                //
                // arrange
                CoreController core = ((CPClass)cp).core;
                core.cache.invalidateAll();
                core.session.verifyUser();
                DbBaseModel.deleteRows<GroupModel>(cp, "");
                DbBaseModel.deleteRows<MemberRuleModel>(cp, "");
                GroupController.add(core, "GroupInclude");
                GroupModel groupToInclude = DbBaseModel.createByUniqueName<GroupModel>(cp, "GroupInclude");
                GroupController.add(core, "GroupExclude");
                GroupModel groupToExclude = DbBaseModel.createByUniqueName<GroupModel>(cp, "GroupExclude");
                PersonModel user = DbBaseModel.addEmpty<PersonModel>(cp);
                user.name = "User1";
                user.save(cp);
                DateTime tomorrow = core.dateTimeNowMockable.AddDays(1);
                DateTime yesterday = core.dateTimeNowMockable.AddDays(-1);
                Assert.IsNotNull(groupToInclude);
                Assert.IsNotNull(user);
                //
                // act - add current use to new group
                GroupController.addUser(core, groupToInclude, user, tomorrow);
                GroupController.addUser(core, groupToExclude, user, yesterday);
                //
                // assert
                List<MemberRuleModel> ruleList = DbBaseModel.createList<MemberRuleModel>(cp, "(groupId=" + groupToInclude.id + ")and(memberid=" + user.id + ")");
                Assert.AreEqual(1, ruleList.Count);
                //
                ruleList = DbBaseModel.createList<MemberRuleModel>(cp, "(groupId=" + groupToExclude.id + ")and(memberid=" + user.id + ")");
                Assert.AreEqual(1, ruleList.Count);
                //
                Assert.AreEqual(true, cp.User.IsInGroup("groupinclude", user.id));
                Assert.AreEqual(false, cp.User.IsInGroup("GroupExclude", user.id));
            }
        }
        //
        //====================================================================================================
        //
        [TestMethod]
        public void controllers_Group_AddUser_Group_Person() {
            using (CPClass cp = new CPClass(testAppName)) {
                //
                // arrange
                CoreController core = ((CPClass)cp).core;
                core.cache.invalidateAll();
                core.session.verifyUser();
                DbBaseModel.deleteRows<GroupModel>(cp, "");
                DbBaseModel.deleteRows<MemberRuleModel>(cp, "");
                GroupController.add(core, "Group1");
                GroupModel group = DbBaseModel.createByUniqueName<GroupModel>(cp, "Group1");
                PersonModel user = DbBaseModel.addEmpty<PersonModel>(cp);
                user.name = "User1";
                user.save(cp);
                Assert.IsNotNull(group);
                Assert.IsNotNull(user);
                //
                // act - add current use to new group
                GroupController.addUser(core, group, user);
                //
                // assert
                List<MemberRuleModel> ruleList = DbBaseModel.createList<MemberRuleModel>(cp, "(groupId=" + group.id + ")and(memberid=" + user.id + ")");
                Assert.AreEqual(1, ruleList.Count);
            }
        }

    }
}