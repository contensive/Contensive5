﻿
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Contensive.Processor.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Contensive.Processor.Tests.testConstants;
using Contensive.BaseClasses;

namespace Contensive.Processor.Tests.UnitTests.Controllers {
    //
    //====================================================================================================
    //
    [TestClass()]
    public class dbControllerTests {
        [TestMethod]
        public void Controllers_db_encodeSqlTableNameTest() {
            // arrange
            // act
            // assert
            Assert.AreEqual("", DbController.encodeSqlTableName(""));
            Assert.AreEqual("", DbController.encodeSqlTableName("-----"));
            Assert.AreEqual("", DbController.encodeSqlTableName("01234567879"));
            Assert.AreEqual("a", DbController.encodeSqlTableName("a"));
            Assert.AreEqual("aa", DbController.encodeSqlTableName("a a"));
            Assert.AreEqual("aA", DbController.encodeSqlTableName(" aA"));
            Assert.AreEqual("aA", DbController.encodeSqlTableName(" aA "));
            Assert.AreEqual("aA", DbController.encodeSqlTableName("aA "));
            Assert.AreEqual("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ", DbController.encodeSqlTableName("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ"));
            Assert.AreEqual("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ_@#", DbController.encodeSqlTableName("0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ_@#"));
            //
        }
        //
        //====================================================================================================
        /// <summary>
        /// set a value, close, open, and get it back
        /// </summary>
        [TestMethod]
        public void Controllers_db_csSetCloseOpenGetTest() {
            using (Contensive.Processor.CPClass cp = new Contensive.Processor.CPClass(testAppName)) {
                // arrange
                int testId_test1 = 0;
                int testId_test2 = 0;
                cp.core.db.executeNonQuery("delete from ccMembers where (username='a')or(username='c')");
                //
                using (var csData = new CsModel(cp.core)) {
                    csData.insert("people");
                    testId_test1 = csData.getInteger("id");
                    //
                    csData.insert("people");
                    testId_test2 = csData.getInteger("id");
                    //
                    csData.open("people", "(id=" + testId_test1 + ")or(id=" + testId_test2 + ")");
                    if (!csData.ok()) {
                        Assert.Fail("new people records not valid");
                    } else {
                        csData.set("username", "a");
                        csData.set("password", "b");
                        csData.goNext();
                    }
                    if (!csData.ok()) {
                        Assert.Fail("new people records not valid");
                    } else {
                        csData.set("username", "c");
                        csData.goNext();
                    }
                }
                // assert
                var person_test1 = Contensive.Processor.Models.Db.PersonModel.create(cp.core, testId_test1);
                var person_test2 = Contensive.Processor.Models.Db.PersonModel.create(cp.core, testId_test2);
                //
                Assert.AreEqual("a", person_test1.username);
                Assert.AreEqual("b", person_test1.password);
                Assert.AreEqual("c", person_test2.username);
                Assert.AreEqual("", person_test2.password); // original error this=b
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// set a value and get it back
        /// </summary>
        [TestMethod]
        public void Controllers_db_csSetGetTest() {
            using (Contensive.Processor.CPClass cp = new Contensive.Processor.CPClass(testAppName)) {
                // arrange
                cp.core.db.executeNonQuery("delete from ccMembers where (username='a')or(username='c')");
                //
                using (var csData = new CsModel(cp.core)) {
                    csData.insert("people");
                    if (!csData.ok()) {
                        Assert.Fail("new people records not valid");
                    } else {
                        Assert.AreNotEqual("a", csData.getText("username"));
                        csData.set("username", "a");
                        Assert.AreEqual("a", csData.getText("username"));
                    }
                }
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// set a value and get it back
        /// </summary>
        [TestMethod]
        public void Controllers_db_csSetSaveGetTest() {
            using (Contensive.Processor.CPClass cp = new Contensive.Processor.CPClass(testAppName)) {
                // arrange
                cp.core.db.executeNonQuery("delete from ccMembers where (username='a')or(username='c')");
                //
                using (var csData = new CsModel(cp.core)) {
                    csData.insert("people");
                    if (!csData.ok()) {
                        Assert.Fail("new people records not valid");
                    } else {
                        Assert.AreNotEqual("a", csData.getText("username"));
                        csData.set("username", "a");
                        csData.save();
                        string result1 = csData.getText("username");
                        Assert.AreEqual("a", result1);
                    }
                }
            }
        }
        //
        //====================================================================================================
        //
        [TestMethod]
        public void Controllers_db_csGetRowCount() {
            using (Contensive.Processor.CPClass cp = new Contensive.Processor.CPClass(testAppName)) {
                // arrange
                CPCSBaseClass cs = cp.CSNew();
                string impossibleName = cp.Utils.CreateGuid();
                // act
                cs.Open(Contensive.Processor.Models.Db.PersonModel.contentName, "(name=" + cp.Db.EncodeSQLText(impossibleName) + ")");
                int resultNoData = cs.GetRowCount();
                cs.Close();
                // assert
                Assert.AreEqual(0, resultNoData);
            }
        }
        //
        //====================================================================================================
        //
        [TestMethod]
        public void Controllers_db_isSqlTableField() {
            using (Contensive.Processor.CPClass cp = new Contensive.Processor.CPClass(testAppName)) {
                // arrange
                // act
                // assert
                Assert.IsTrue(cp.core.db.isSQLTableField("", "ccmembers", "name"));
                Assert.IsTrue(cp.core.db.isSQLTableField("", "ccmembers", "NaMe"));
                Assert.IsFalse(cp.core.db.isSQLTableField("", "ccmembers", "namex"));
            }
        }
        //
        //====================================================================================================
        //
        [TestMethod]
        public void Controllers_db_csGoNext() {
            using (Contensive.Processor.CPClass cp = new Contensive.Processor.CPClass(testAppName)) {
                // arrange
                string name = GenericController.getGUID();
                //
                // -- add three records
                using (var csData = new CsModel(cp.core)) {
                    csData.insert("people");
                    csData.set("name", name);
                    csData.save();
                    int id0 = csData.getInteger("id");
                    //
                    csData.insert("people");
                    csData.set("name", name);
                    csData.save();
                    int id1 = csData.getInteger("id");
                    //
                    csData.insert("people");
                    csData.set("name", name);
                    csData.save();
                    int id2 = csData.getInteger("id");
                    //
                    // act
                    csData.open("people", "name=" + DbController.encodeSQLText(name), "id");
                    Assert.IsTrue(csData.ok(), "csOpen");
                    Assert.AreEqual(id0, csData.getInteger("id"), "correct id0 after open");
                    csData.goNext();
                    Assert.AreEqual(id1, csData.getInteger("id"), "goNext id1, id correct");
                    csData.goNext();
                    Assert.AreEqual(id2, csData.getInteger("id"), "goNext id2, id correct");
                    csData.goNext();
                    Assert.IsFalse(csData.ok(), "csOk false after all records");
                }
            }
        }
        //
        //====================================================================================================
        //
        [TestMethod]
        public void Controllers_db_csGoFirst() {
            using (Contensive.Processor.CPClass cp = new Contensive.Processor.CPClass(testAppName)) {
                // arrange
                string name = GenericController.getGUID();
                //
                // -- add three records
                using (var csData = new CsModel(cp.core)) {
                    csData.insert("people");
                    csData.set("name", name);
                    csData.save();
                    int id0 = csData.getInteger("id");
                    //
                    csData.insert("people");
                    csData.set("name", name);
                    csData.save();
                    int id1 = csData.getInteger("id");
                    //
                    csData.insert("people");
                    csData.set("name", name);
                    csData.save();
                    int id2 = csData.getInteger("id");
                    //
                    // act
                    csData.open("people", "name=" + DbController.encodeSQLText(name), "id");
                    Assert.IsTrue(csData.ok(), "csOpen");
                    csData.goNext();
                    csData.goNext();
                    csData.goNext();
                    Assert.IsFalse(csData.ok(), "csOK false after last record");
                    csData.goFirst();
                    Assert.IsTrue(csData.ok(), "csOK true after goFirst (back at first record");
                    //
                    Assert.AreEqual(id0, csData.getInteger("id"), "correct id0 after goFirst");
                    csData.goNext();
                    Assert.AreEqual(id1, csData.getInteger("id"), "goNext id1, id correct");
                    csData.goNext();
                    Assert.AreEqual(id2, csData.getInteger("id"), "goNext id2, id correct");
                    csData.goNext();
                    Assert.IsFalse(csData.ok(), "csOk false after all records");
                }
            }
        }
    }
}