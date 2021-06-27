
using Contensive.BaseClasses;
using Contensive.Processor;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using static Tests.TestConstants;

namespace Tests {

    [TestClass()]
    public class CpCSTests {
        //====================================================================================================
        /// <summary>
        /// cp.cs
        /// </summary>
        [TestMethod]
        public void views_cpCS_OpenClose_test() {
            // arrange
            using (CPClass cp = new(testAppName)) {
                using (CPCSBaseClass cs = cp.CSNew()) {
                    int newuserId = cp.Content.AddRecord("people");
                    int testuserId;
                    //
                    // act
                    //
                    // open without criteria must return at least one record
                    testuserId = 0;
                    if (cs.Open("people")) {
                        testuserId = cs.GetInteger("id");
                    }
                    Assert.AreNotEqual(0, testuserId);
                    // open with id criteria must return exactly one record
                    testuserId = 0;
                    if (cs.Open("people", "id=" + cp.Db.EncodeSQLNumber(newuserId))) {
                        Assert.AreEqual(newuserId, cs.GetInteger("id"));
                        Assert.AreEqual(true, cs.OK());
                        cs.GoNext();
                        Assert.AreEqual(false, cs.OK());
                    }
                    //
                    // dispose
                    //
                    cp.Content.Delete("people", "id=" + newuserId);
                }
            }
        }
        //====================================================================================================
        /// <summary>
        /// cp.cs.insert
        /// </summary>
        [TestMethod]
        public void views_cpCS_Insert_Test() {
            // arrange
            using (CPClass cp = new(testAppName)) {
                using (CPCSBaseClass cs = cp.CSNew()) {
                    int newuserId = 0;
                    //
                    // act
                    //
                    if (cs.Insert("people")) {
                        newuserId = cs.GetInteger("id");
                    }
                    cs.Close();
                    Assert.AreNotEqual(0, newuserId);
                    //
                    // dispose
                    //
                    cp.Content.Delete("people", "id=" + newuserId);
                }
            }
        }
        //====================================================================================================
        /// <summary>
        /// cp.cs.insert
        /// </summary>
        [TestMethod]
        public void views_cpCS_OpenRecord_Test() {
            // arrange
            using (CPClass cp = new(testAppName)) {
                using (CPCSBaseClass cs = cp.CSNew()) {
                    int newuserId = 0;
                    //
                    // act
                    //
                    if (cs.Insert("people")) {
                        newuserId = cs.GetInteger("id");
                    }
                    cs.Close();
                    Assert.AreNotEqual(0, newuserId);
                    if (cs.OpenRecord("people", newuserId)) {
                        Assert.AreEqual(newuserId, cs.GetInteger("id"));
                    }
                    cs.Close();
                    //
                    // dispose
                    //
                    cp.Content.Delete("people", "id=" + newuserId);
                }
            }
        }
        //====================================================================================================
        /// <summary>
        /// cp.cs.OpenGroupUsers
        /// </summary>
        [TestMethod]
        public void views_cpCS_OpenGroupUsers_Test() {
            // arrange
            using (CPClass cp = new(testAppName)) {
                using (CPCSBaseClass cs = cp.CSNew()) {
                    string groupName = "testGroup" + cp.Utils.GetRandomInteger().ToString();
                    string user1Name = "testUser1" + cp.Utils.GetRandomInteger().ToString();
                    string user2Name = "testUser2" + cp.Utils.GetRandomInteger().ToString();
                    int user1Id = 0;
                    int user2Id = 0;
                    //
                    // act
                    //
                    user2Id = cp.Content.AddRecord("people", user2Name);
                    cp.Group.AddUser(groupName, user2Id);
                    user1Id = cp.Content.AddRecord("people", user1Name);
                    cp.Group.AddUser(groupName, user1Id);
                    // assert
                    Assert.AreNotEqual(0, user1Id);
                    Assert.AreNotEqual(0, user2Id);
                    // order by id will be 2 then 1
                    Assert.IsTrue(cs.OpenGroupUsers(groupName, "", "id"));
                    Assert.AreEqual(user2Name, cs.GetText("name"));
                    Assert.IsTrue(cs.NextOK());
                    Assert.AreEqual(user1Name, cs.GetText("name"));
                    Assert.IsFalse(cs.NextOK());
                    cs.Close();
                    // order by name will be 1 then 2
                    Assert.IsTrue(cs.OpenGroupUsers(groupName, "", "name"));
                    Assert.AreEqual(user1Name, cs.GetText("name"));
                    Assert.IsTrue(cs.NextOK());
                    Assert.AreEqual(user2Name, cs.GetText("name"));
                    Assert.IsFalse(cs.NextOK());
                    cs.Close();
                    //
                    // dispose
                    //
                    cp.Content.Delete("groups", "name=" + cp.Db.EncodeSQLText(groupName));
                    cp.Content.Delete("people", "id=" + user1Id);
                    cp.Content.Delete("people", "id=" + user2Id);
                }
            }
        }
        //====================================================================================================
        /// <summary>
        /// cp.cs.FieldOK
        /// </summary>
        [TestMethod]
        public void views_cpCS_FieldOK_Test() {
            // arrange
            using (CPClass cp = new(testAppName)) {
                using (CPCSBaseClass cs = cp.CSNew()) {
                    string user1Name = "testUser1" + cp.Utils.GetRandomInteger().ToString();
                    int user1Id = 0;
                    //
                    // act
                    //
                    user1Id = cp.Content.AddRecord("people", user1Name);
                    Assert.AreNotEqual(0, user1Id);
                    //
                    Assert.IsTrue(cs.OpenRecord("people", user1Id));
                    Assert.IsTrue(cs.FieldOK("id"));
                    Assert.IsTrue(cs.FieldOK("ID"));
                    Assert.IsTrue(cs.FieldOK("name"));
                    Assert.IsTrue(cs.FieldOK("firstname"));
                    cs.Close();
                    //
                    Assert.IsTrue(cs.OpenRecord("people", user1Id, "id,name"));
                    Assert.IsTrue(cs.FieldOK("id"));
                    Assert.IsTrue(cs.FieldOK("ID"));
                    Assert.IsTrue(cs.FieldOK("name"));
                    Assert.IsTrue(cs.FieldOK("NaMe"));
                    Assert.IsFalse(cs.FieldOK("firstname"));
                    cs.Close();
                    //
                    //
                    // dispose
                    //
                    cp.Content.Delete("people", "id=" + user1Id);
                }
            }
        }
        //====================================================================================================
        /// <summary>
        /// cp.cs.Delete
        /// </summary>
        [TestMethod]
        public void views_cpCS_Delete_Test() {
            // arrange
            using (CPClass cp = new(testAppName)) {
                using (CPCSBaseClass cs = cp.CSNew()) {
                    string user1Name = "testUser1" + cp.Utils.GetRandomInteger().ToString();
                    int user1Id = 0;
                    //
                    // act
                    //
                    user1Id = cp.Content.AddRecord("people", user1Name);
                    Assert.AreNotEqual(0, user1Id);
                    //
                    Assert.IsTrue(cs.OpenRecord("people", user1Id));
                    cs.Delete();
                    cs.Close();
                    Assert.IsFalse(cs.OpenRecord("people", user1Id));
                    cs.Close();
                    //
                    // dispose
                    //
                    cp.Content.Delete("people", "id=" + user1Id);
                }
            }
        }
        //====================================================================================================
        /// <summary>
        /// cp.cs.Delete
        /// </summary>
        [TestMethod]
        public void views_cpCS_SetGetField_Test() {
            // arrange
            using (CPClass cp = new(testAppName)) {
                using (CPCSBaseClass cs = cp.CSNew()) {
                    string user1Name = "testUser1" + cp.Utils.GetRandomInteger().ToString();
                    int user1Id = 0;
                    Random rnd = new Random();
                    DateTime testDate = new DateTime(1900 + rnd.Next(1, 100), rnd.Next(1, 13), rnd.Next(1, 28));
                    int testinteger = rnd.Next(1, 2000);
                    //
                    // act
                    //
                    user1Id = cp.Content.AddRecord("people", user1Name);
                    Assert.AreNotEqual(0, user1Id);
                    //
                    Assert.IsTrue(cs.OpenRecord("people", user1Id));
                    cs.SetField("name", user1Name);
                    cs.SetField("excludefromanalytics", true);
                    cs.SetField("lastVisit", testDate);
                    cs.SetField("birthdayyear", testinteger);
                    // eventually fill out all the rest
                    cs.Close();
                    //
                    Assert.IsTrue(cs.OpenRecord("people", user1Id));
                    Assert.AreEqual(user1Name, cs.GetText("name"));
                    Assert.AreEqual(true, cs.GetBoolean("excludefromanalytics"));
                    Assert.AreEqual(testDate, cs.GetDate("lastVisit"));
                    Assert.AreEqual(testinteger, cs.GetInteger("birthdayyear"));
                    cs.Close();
                    // eventually fill out all the rest

                    //
                    // dispose
                    //
                    cp.Content.Delete("people", "id=" + user1Id);
                }
            }
        }
        //====================================================================================================
        /// <summary>
        /// cp.cs.Delete
        /// </summary>
        [TestMethod]
        public void views_cpCS_GetRowCount_Test() {
            // arrange
            using (CPClass cp = new(testAppName)) {
                using (CPCSBaseClass cs = cp.CSNew()) {
                    string user1Name = "testUser1" + cp.Utils.GetRandomInteger().ToString();
                    string user2Name = "testUser2" + cp.Utils.GetRandomInteger().ToString();
                    string user3Name = "testUser3" + cp.Utils.GetRandomInteger().ToString();
                    string user4Name = "testUser4" + cp.Utils.GetRandomInteger().ToString();
                    int user1Id = 0;
                    int user2Id = 0;
                    int user3Id = 0;
                    int user4Id = 0;
                    //
                    // act
                    //
                    user1Id = cp.Content.AddRecord("people", user1Name);
                    Assert.AreNotEqual(0, user1Id);
                    user2Id = cp.Content.AddRecord("people", user2Name);
                    Assert.AreNotEqual(0, user2Id);
                    user3Id = cp.Content.AddRecord("people", user3Name);
                    Assert.AreNotEqual(0, user3Id);
                    user4Id = cp.Content.AddRecord("people", user4Name);
                    Assert.AreNotEqual(0, user4Id);
                    //
                    Assert.IsTrue(cs.Open("people", "(id in (" + user1Id + "," + user2Id + "," + user3Id + "," + user4Id + "))"));
                    Assert.AreEqual(4, cs.GetRowCount());
                    cs.Delete();
                    Assert.IsTrue(cs.NextOK());
                    cs.Delete();
                    Assert.IsTrue(cs.NextOK());
                    cs.Delete();
                    Assert.IsTrue(cs.NextOK());
                    cs.Delete();
                    Assert.IsFalse(cs.NextOK());
                    cs.Close();
                }
            }
        }
        //====================================================================================================
        /// <summary>
        /// cp.cs.getSql
        /// </summary>
        [TestMethod]
        public void views_cpCS_GetSql_Test() {
            // arrange
            using (CPClass cp = new(testAppName)) {
                // todo - finish test
                using (CPCSBaseClass cs = cp.CSNew()) { }
                //
                // act
                //
                // assert }
            }
        }
    }
}
