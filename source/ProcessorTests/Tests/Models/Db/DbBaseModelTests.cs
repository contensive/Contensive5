
using Contensive.Models.Db;
using Contensive.Processor;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using static Tests.TestConstants;

namespace Tests {
    //
    //
    [TestClass()]
    public class DbBaseModelTests {
        //
        //
        [TestMethod()]
        public void addDefault_Test1() {
            using (CPClass cp = new(testAppName)) {
                //
                // -- set default value for person name to random test
                List<ContentFieldModel> testField = DbBaseModel.createList<ContentFieldModel>(cp, $"(name='name')and(contentid={cp.Content.GetID("people")})");
                Assert.AreEqual(1, testField.Count);
                ContentFieldModel field = testField[0];
                string oldValue = field.defaultValue;
                string testValue = cp.Utils.GetRandomString(255);
                //
                // -- act
                field.defaultValue = testValue;
                field.save(cp);
                PersonModel testRecord = DbBaseModel.addDefault<PersonModel>(cp);
                //
                // -- assert
                Assert.AreEqual(testValue, testRecord.name);
                //
                // -- repair
                field.defaultValue = oldValue;
                field.save(cp);
            }
        }
        //
        //
        [TestMethod()]
        public void reload_Test() {
            using (CPClass cp = new(testAppName)) {
                //
                AddonModel addon = DbBaseModel.addDefault<AddonModel>(cp);
                addon.name = "test1";
                addon.save(cp);
                //
                cp.Db.ExecuteNonQuery($"update {AddonModel.tableMetadata.tableNameLower} set name='test2' where id={addon.id}");
                Assert.AreNotEqual("test2", addon.name);
                //
                addon.reload(cp);
                using (var dt = cp.Db.ExecuteQuery($"select * from {AddonModel.tableMetadata.tableNameLower} where id={addon.id}")) {
                    Assert.IsNotNull(dt);
                    Assert.IsTrue(dt.Rows.Count == 1);
                    addon.load<AddonModel>(cp, dt.Rows[0]);
                }
                Assert.AreEqual("test2", addon.name);
            }
        }
        //
        //
        [TestMethod()]
        public void getDefaultValues_Test1() {
            using (CPClass cp = new(testAppName)) {
                //
                // -- set default value for person name to random test
                List<ContentFieldModel> testField = DbBaseModel.createList<ContentFieldModel>(cp, $"(name='name')and(contentid={cp.Content.GetID("people")})");
                Assert.AreEqual(1, testField.Count);
                ContentFieldModel field = testField[0];
                string oldValue = field.defaultValue;
                string testValue = cp.Utils.GetRandomString(255);
                //
                // -- act
                field.defaultValue = testValue;
                field.save(cp);
                //
                // -- assert
                var defaultValues = DbBaseModel.getDefaultValues<PersonModel>(cp, 0);
                Assert.AreEqual(testValue, defaultValues["name"]);
                //
                // -- repair
                field.defaultValue = oldValue;
                field.save(cp);
                //
                // -- assert
                var defaultValues2 = DbBaseModel.getDefaultValues<PersonModel>(cp, 0);
                Assert.AreEqual(oldValue, defaultValues2["name"]);
            }
        }
        //
        //
        [TestMethod()]
        public void getDefaultValues_Test2() {
            using (CPClass cp = new(testAppName)) {
                //
                // -- set default value for person name to random test
                List<ContentFieldModel> testField = DbBaseModel.createList<ContentFieldModel>(cp, $"(name='name')and(contentid={cp.Content.GetID("people")})");
                Assert.AreEqual(1, testField.Count);
                ContentFieldModel field = testField[0];
                string oldValue = field.defaultValue;
                string testValue = cp.Utils.GetRandomString(255);
                //
                // -- act
                field.defaultValue = testValue;
                field.save(cp);
                //
                // -- assert
                var defaultValues = DbBaseModel.getDefaultValues<PersonModel>(cp);
                Assert.AreEqual(testValue, defaultValues["name"]);
                //
                // -- repair
                field.defaultValue = oldValue;
                field.save(cp);
                //
                // -- assert
                var defaultValues2 = DbBaseModel.getDefaultValues<PersonModel>(cp);
                Assert.AreEqual(oldValue, defaultValues2["name"]);
            }
        }
        //
        //
        [TestMethod()]
        public void load_Test() {
            using (CPClass cp = new(testAppName)) {
                //
                AddonModel addon = DbBaseModel.addDefault<AddonModel>(cp);
                addon.name = "test1";
                addon.save(cp);
                cp.Db.ExecuteNonQuery($"update {AddonModel.tableMetadata.tableNameLower} set name='test2' where id={addon.id}");
                Assert.AreNotEqual("test2", addon.name);
                using (var dt = cp.Db.ExecuteQuery($"select * from {AddonModel.tableMetadata.tableNameLower} where id={addon.id}")) {
                    Assert.IsNotNull(dt);
                    Assert.IsTrue(dt.Rows.Count == 1);
                    addon.load<AddonModel>(cp, dt.Rows[0]);
                }
                Assert.AreEqual("test2", addon.name);
            }
        }
        [TestMethod()]
        public void derivedContentNameTest() {
            Assert.AreEqual("Add-on Collections".ToLower(CultureInfo.InvariantCulture), DbBaseModel.derivedContentName(typeof(AddonCollectionModel)).ToLower(CultureInfo.InvariantCulture));
        }

        [TestMethod()]
        public void derivedTableNameTest() {
            Assert.AreEqual("ccaddoncollections", DbBaseModel.derivedTableName(typeof(AddonCollectionModel)).ToLower(CultureInfo.InvariantCulture));
        }

        [TestMethod()]
        public void derivedDataSourceNameTest() {
            Assert.AreEqual("default", DbBaseModel.derivedDataSourceName(typeof(AddonCollectionModel)).ToLower(CultureInfo.InvariantCulture));
        }

        [TestMethod()]
        public void derivedNameFieldIsUniqueTest() {
            Assert.AreEqual(true, DbBaseModel.derivedNameFieldIsUnique(typeof(AddonModel)));
            Assert.AreEqual(false, DbBaseModel.derivedNameFieldIsUnique(typeof(AddonContentFieldTypeRulesModel)));
        }
        ///// <summary>
        ///// If not current session (a process, or trackvisit disabled), test that a default value is created by teh argument-user, or 0
        ///// </summary>
        //[TestMethod()]
        //public void addDefaultTest_noSession() {
        //    using (CPClass cp = new(testAppName)) {
        //        //
        //        // -- verify this is a non-tracked session (a process not a webhit)
        //        Assert.AreEqual(0, cp.User.IdInSession);
        //        //
        //        AddonModel test_withoutUser = DbBaseModel.addDefault<AddonModel>(cp);
        //        AddonModel test_withUser = DbBaseModel.addDefault<AddonModel>(cp, 99);
        //        //
        //        Assert.AreEqual(true, test_withoutUser.active);
        //        Assert.AreEqual(0, test_withoutUser.createdBy);
        //        Assert.AreEqual(0, test_withoutUser.modifiedBy);
        //        Assert.AreNotEqual(0, test_withoutUser.contentControlId);
        //        //
        //        Assert.AreEqual(true, test_withUser.active);
        //        Assert.AreEqual(99, test_withUser.createdBy);
        //        Assert.AreEqual(99, test_withUser.modifiedBy);
        //        Assert.AreNotEqual(0, test_withUser.contentControlId);
        //    }
        //}

        [TestMethod()]
        public void addDefaultTest_withSession() {
            using (CPClass cp = new(testAppName)) {
                //
                // -- initialize visit tracking
                cp.User.Track();
                //
                // -- establish the visit
                //
                AddonModel test_withoutUser = DbBaseModel.addDefault<AddonModel>(cp);
                AddonModel test_withUser = DbBaseModel.addDefault<AddonModel>(cp, 99);
                //
                Assert.AreEqual(true, test_withoutUser.active);
                Assert.AreEqual(cp.User.Id, test_withoutUser.createdBy);
                Assert.AreEqual(cp.User.Id, test_withoutUser.modifiedBy);
                Assert.AreNotEqual(0, test_withoutUser.contentControlId);
                //
                Assert.AreEqual(true, test_withUser.active);
                Assert.AreEqual(99, test_withUser.createdBy);
                Assert.AreEqual(99, test_withUser.modifiedBy);
                Assert.AreNotEqual(0, test_withUser.contentControlId);
            }
        }

        [TestMethod()]
        public void addDefaultTest_withSessionById() {
            using (CPClass cp = new(testAppName)) {
                //
                // -- initialize visit tracking
                int userId = cp.User.Id;
                //
                // -- establish the visit
                //
                AddonModel test_withoutUser = DbBaseModel.addDefault<AddonModel>(cp);
                AddonModel test_withUser = DbBaseModel.addDefault<AddonModel>(cp, 99);
                //
                Assert.AreEqual(true, test_withoutUser.active);
                Assert.AreEqual(userId, test_withoutUser.createdBy);
                Assert.AreEqual(userId, test_withoutUser.modifiedBy);
                Assert.AreNotEqual(0, test_withoutUser.contentControlId);
                //
                Assert.AreEqual(true, test_withUser.active);
                Assert.AreEqual(99, test_withUser.createdBy);
                Assert.AreEqual(99, test_withUser.modifiedBy);
                Assert.AreNotEqual(0, test_withUser.contentControlId);
            }
        }

        //[TestMethod()]
        //public void visitTracking_IdInSession_DoesNotCreate() {
        //    using (CPClass cp = new(testAppName,false)) {
        //        //
        //        // -- initialize visit tracking
        //        Assert.AreEqual(0, cp.User.IdInSession);
        //    }
        //}

        [TestMethod()]
        public void visitTracking_Id_DoesCreate() {
            using (CPClass cp = new(testAppName)) {
                //
                // -- initialize visit tracking
                Assert.AreNotEqual(0, cp.User.Id);
            }
        }

        [TestMethod()]
        public void visitTracking_track_DoesCreate() {
            using (CPClass cp = new(testAppName)) {
                //
                // -- initialize visit tracking
                cp.User.Track();
                Assert.AreNotEqual(0, cp.User.IdInSession);
            }
        }
        //
        //
        //
        [TestMethod()]
        public void addDefaultTest_DefaultValues() {
            using (CPClass cp = new(testAppName)) {
                var defaultValues = new Dictionary<string, string> {
                    //
                    // -- bool
                    { "admin", "true" },
                    { "asAjax", "1" },
                    { "htmlDocument", "false" },
                    { "onPageStartEvent", "" },
                    { "onPageEndEvent", "0" },
                    //
                    // int
                    { "navTypeID", "1" },
                    { "scriptingLanguageID", "" },
                    //
                    // int nullable
                    { "processInterval", "" },
                    //
                    // string
                    { "pageTitle", "asdf" },
                    { "otherHeadTags", "" },
                    //
                    // double
                    //
                    // double nullable
                    //
                    // date
                    { "processNextRun", "" }
                };
                //
                //
                AddonModel test = DbBaseModel.addDefault<AddonModel>(cp, defaultValues);
                //
                //
                Assert.AreEqual(true, test.admin);
                Assert.AreEqual(true, test.asAjax);
                Assert.AreEqual(false, test.htmlDocument);
                Assert.AreEqual(false, test.onPageStartEvent);
                Assert.AreEqual(false, test.onPageEndEvent);
                //
                Assert.AreEqual(1, test.navTypeId);
                Assert.AreEqual(0, test.scriptingLanguageId);
                //
                Assert.AreEqual(null, test.processInterval);
                //
                Assert.AreEqual("asdf", test.pageTitle);
                Assert.AreEqual("", test.otherHeadTags);
                //
                Assert.AreEqual(null, test.processNextRun);
            }
        }
        //
        //
        //
        [TestMethod()]
        public void addDefaultTest_CreatedBy() {
            using (CPClass cp = new(testAppName)) {
                string defaultRootUserGuid = "{4445cd14-904f-480f-a7b7-29d70d0c22ca}";
                var root = Contensive.Models.Db.DbBaseModel.create<Contensive.Models.Db.PersonModel>(cp, defaultRootUserGuid);
                if (root == null) {
                    root = DbBaseModel.addDefault<PersonModel>(cp);
                    root.ccguid = defaultRootUserGuid;
                    root.name = "root";
                    root.save(cp);
                }
                //
                var defaultValues = new Dictionary<string, string> {
                    //
                    // -- bool
                    { "active", "true" },
                    //
                    // string
                    { "name", "1234asdf" }
                };
                //
                //
                AddonModel test = DbBaseModel.addDefault<AddonModel>(cp, defaultValues, root.id);
                //
                //
                Assert.AreEqual(root.id, test.createdBy);
            }
        }
        //
        //
        //
        [TestMethod()]
        public void fieldFileTypes_HtmlFieldTypeTest() {
            using (CPClass cp = new(testAppName)) {
                string contentSaved = new string('*', 65535);
                var pageCreated = PageContentModel.addEmpty<PageContentModel>(cp);
                pageCreated.copyfilename.content = contentSaved;
                pageCreated.save(cp);
                int pageId = pageCreated.id;
                string pageCreatedFilename = pageCreated.copyfilename.filename;
                //
                var pageRead = PageContentModel.create<PageContentModel>(cp, pageId);
                string pageReadFilename = pageRead.copyfilename.filename;
                string contentReadFromModel = pageRead.copyfilename.content;
                //
                string contentReadFromFile = "";
                string dbFilename = "";
                DataTable dbRead = null;
                using (var cs = cp.CSNew()) {
                    //
                    dbRead = cp.Db.ExecuteQuery("select * from " + PageContentModel.tableMetadata.tableNameLower + " where (id=" + pageId + ")");
                    Assert.IsNotNull(dbRead);
                    Assert.AreEqual(1, dbRead.Rows.Count);
                    //
                    dbFilename = dbRead.Rows[0]["copyFilename"].ToString();
                    Assert.IsFalse(string.IsNullOrWhiteSpace(dbFilename));
                    contentReadFromFile = cp.CdnFiles.Read(dbFilename);
                }
                //
                Assert.AreEqual(contentSaved, contentReadFromModel);
                Assert.AreEqual(contentSaved, contentReadFromFile);
                //
                Assert.AreEqual(contentReadFromModel, contentReadFromFile);
            }
        }
        //
        //
        //
        [TestMethod()]
        public void fieldFileTypes_TextFieldTypeTest() {
            using (CPClass cp = new(testAppName)) {
                string contentSaved = new string('*', 65535);
                var contentCreated = DownloadModel.addEmpty<DownloadModel>(cp);
                contentCreated.filename.content = contentSaved;
                contentCreated.save(cp);
                int recordId = contentCreated.id;
                string contentCreatedFilename = contentCreated.filename.filename;
                //
                var contentRead = DownloadModel.create<DownloadModel>(cp, recordId);
                string contentReadFilename = contentRead.filename.filename;
                string contentReadFromModel = contentRead.filename.content;
                //
                string contentReadFromFile = "";
                string dbFilename = "";
                DataTable dbRead = null;
                using (var cs = cp.CSNew()) {
                    //
                    dbRead = cp.Db.ExecuteQuery("select * from " + DownloadModel.tableMetadata.tableNameLower + " where (id=" + recordId + ")");
                    Assert.IsNotNull(dbRead);
                    Assert.AreEqual(1, dbRead.Rows.Count);
                    //
                    dbFilename = dbRead.Rows[0]["filename"].ToString();
                    Assert.IsFalse(string.IsNullOrWhiteSpace(dbFilename));
                    contentReadFromFile = cp.CdnFiles.Read(dbFilename);
                }
                //
                Assert.AreEqual(contentSaved, contentReadFromModel);
                Assert.AreEqual(contentSaved, contentReadFromFile);
                //
                Assert.AreEqual(contentReadFromModel, contentReadFromFile);
            }
        }
        //
        //
        /// <summary>
        /// AddDefault inherits fields
        /// </summary>
        [TestMethod]
        public void addDefaultTest_inheritFields() {
            using (CPClass cp = new(testAppName)) {
                // arrange
                ConditionalEmailModel email = DbBaseModel.addDefault<ConditionalEmailModel>(cp);
                // act
                // assert
                Assert.AreEqual(cp.Content.GetID("Conditional Email"), email.contentControlId);
                Assert.AreEqual(true, email.active);
            }
        }
        //
        //
        /// <summary>
        /// AddDefault inherits fields
        /// </summary>
        [TestMethod]
        public void create_FieldTypeFile_() {
            using (CPClass cp = new(testAppName)) {
                // arrange
                ConditionalEmailModel email = DbBaseModel.addDefault<ConditionalEmailModel>(cp);
                // act
                // assert
                Assert.AreEqual(cp.Content.GetID("Conditional Email"), email.contentControlId);
                Assert.AreEqual(true, email.active);
            }
        }
    }
}