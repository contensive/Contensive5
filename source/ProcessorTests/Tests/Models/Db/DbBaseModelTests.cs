
using Contensive.Models.Db;
using Contensive.Processor;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using static Tests.TestConstants;

namespace Tests {
    [TestClass()]
    public class DbBaseModelTests {
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

        [TestMethod()]
        public void addDefaultTest() {
            using (CPClass cp = new(testAppName)) {
                //
                AddonModel test_withoutUser = DbBaseModel.addDefault<AddonModel>(cp);
                AddonModel test_withUser = DbBaseModel.addDefault<AddonModel>(cp, 99);
                //
                Assert.AreEqual(true, test_withoutUser.active);
                Assert.AreEqual(0, test_withoutUser.createdBy);
                Assert.AreEqual(0, test_withoutUser.modifiedBy);
                Assert.AreNotEqual(0, test_withoutUser.contentControlId);
                //
                Assert.AreEqual(true, test_withUser.active);
                Assert.AreEqual(99, test_withUser.createdBy);
                Assert.AreEqual(99, test_withUser.modifiedBy);
                Assert.AreNotEqual(0, test_withUser.contentControlId);
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
                if(root == null ) {
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