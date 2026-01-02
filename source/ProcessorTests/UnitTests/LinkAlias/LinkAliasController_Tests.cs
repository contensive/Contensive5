using Contensive.Models.Db;
using Contensive.Processor;
using Contensive.Processor.Controllers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Tests.TestConstants;

namespace Tests {
    //
    //====================================================================================================
    //
    [TestClass()]
    public class LinkAliasController_Tests {
        //
        //====================================================================================================
        //
        [TestMethod]
        public void test_GetLinkAlias_simple() {
            using (CPClass cp = new(testAppName)) {
                CoreController core = cp.core;
                //
                // arrange
                core.db.executeNonQuery("delete from " + LinkAliasModel.tableMetadata.tableNameLower);
                cp.Cache.InvalidateTable(LinkAliasModel.tableMetadata.tableNameLower);
                //
                int pageId = 1;
                string pageQs = "this=that&one=another";
                string pageName = "test";
                string pageLink = "https://" + cp.GetAppConfig().domainList[0] + "/" + pageName;
                //
                // act
                LinkAliasController.addLinkAlias(core, pageName, pageId, pageQs);
                var linkAliasList = DbBaseModel.createList<LinkAliasModel>(cp, "(name='/test')");
                //
                var resultPageLink = PageManagerController.getPageLink(core, pageId, pageQs);
                //
                // assert
                Assert.AreEqual(pageLink, resultPageLink);
            }
        }
        //
        //====================================================================================================
        //
        [TestMethod]
        public void test_AddLinkAlias_simple() {
            using (CPClass cp = new(testAppName)) {
                // arrange
                CoreController core = cp.core;
                core.db.executeNonQuery("delete from " + LinkAliasModel.tableMetadata.tableNameLower);
                // act
                LinkAliasController.addLinkAlias(core, "test", 1, "");
                var linkAliasList = DbBaseModel.createList<LinkAliasModel>(cp, "(name='/test')");

                // assert
                Assert.AreEqual(1, linkAliasList.Count);
                Assert.AreEqual("/test", linkAliasList[0].name);
                Assert.AreEqual(1, linkAliasList[0].pageId);
                Assert.AreEqual("", linkAliasList[0].queryStringSuffix);

            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Adding the same link alias with different pageId - the second replaces the first
        /// </summary>
        [TestMethod]
        public void test_AddLinkAlias_SameLink_NewPage() {
            using (CPClass cp = new(testAppName)) {
                // arrange
                CoreController core = cp.core;
                core.db.executeNonQuery("delete from " + LinkAliasModel.tableMetadata.tableNameLower);
                // act
                LinkAliasController.addLinkAlias(core, "test", 1, "");
                LinkAliasController.addLinkAlias(core, "test", 2, "");
                var linkAliasList = DbBaseModel.createList<LinkAliasModel>(cp, "(name='/test')");
                // assert
                Assert.AreEqual(1, linkAliasList.Count);
                Assert.AreEqual("/test", linkAliasList[0].name);
                Assert.AreEqual(2, linkAliasList[0].pageId);
                Assert.AreEqual("", linkAliasList[0].queryStringSuffix);
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Add two, same link, same page, different qs -- second overrides first
        /// </summary>
        [TestMethod]
        public void test_AddLinkAlias_SameLink_SamePage_NewQS() {
            using (CPClass cp = new(testAppName)) {
                // arrange
                CoreController core = cp.core;
                core.db.executeNonQuery("delete from " + LinkAliasModel.tableMetadata.tableNameLower);
                // act
                LinkAliasController.addLinkAlias(core, "test", 1, "a=1");
                LinkAliasController.addLinkAlias(core, "test", 1, "a=2");
                var linkAliasList = DbBaseModel.createList<LinkAliasModel>(cp, "(name='/test')");
                // assert
                Assert.AreEqual(1, linkAliasList.Count);
                Assert.AreEqual("/test", linkAliasList[0].name);
                Assert.AreEqual(1, linkAliasList[0].pageId);
                Assert.AreEqual("a=2", linkAliasList[0].queryStringSuffix);
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Add two, different link, same page, different qs -- link an addon on a page
        /// </summary>
        [TestMethod]
        public void test_AddLinkAlias_NewLink_SamePage_NewQS() {
            using (CPClass cp = new(testAppName)) {
                // arrange
                CoreController core = cp.core;
                core.db.executeNonQuery("delete from " + LinkAliasModel.tableMetadata.tableNameLower);
                // -- also have to delete pages created, because link-alias are automatically created for pages
                core.db.executeNonQuery($"delete from {PageContentModel.tableMetadata.tableNameLower}");
                // act
                LinkAliasController.addLinkAlias(core, "test1", 1, "a=1");
                LinkAliasController.addLinkAlias(core, "test2", 1, "a=2");
                var linkAliasList = DbBaseModel.createList<LinkAliasModel>(cp, "","id");
                // assert
                Assert.AreEqual(2, linkAliasList.Count);
                //
                Assert.AreEqual("/test1", linkAliasList[0].name);
                Assert.AreEqual(1, linkAliasList[0].pageId);
                Assert.AreEqual("a=1", linkAliasList[0].queryStringSuffix);
                //
                Assert.AreEqual("/test2", linkAliasList[1].name);
                Assert.AreEqual(1, linkAliasList[1].pageId);
                Assert.AreEqual("a=2", linkAliasList[1].queryStringSuffix);
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Add a second link to the same page/qs -> adds the newest as the second as highest id
        /// </summary>
        [TestMethod]
        public void test_AddLinkAlias_NewLink_SamePage_SameQS() {
            using (CPClass cp = new(testAppName)) {
                // arrange
                CoreController core = cp.core;
                core.db.executeNonQuery("delete from " + LinkAliasModel.tableMetadata.tableNameLower);
                // -- also have to delete pages created, because link-alias are automatically created for pages
                core.db.executeNonQuery($"delete from {PageContentModel.tableMetadata.tableNameLower}");
                // act
                LinkAliasController.addLinkAlias(core, "test1", 1, "a=1");
                LinkAliasController.addLinkAlias(core, "test2", 1, "a=1");
                var linkAliasList = DbBaseModel.createList<LinkAliasModel>(cp, "");
                // assert
                Assert.AreEqual(2, linkAliasList.Count);
                Assert.AreEqual("/test1", linkAliasList[0].name);
                Assert.AreEqual(1, linkAliasList[0].pageId);
                Assert.AreEqual("a=1", linkAliasList[0].queryStringSuffix);
                //
                Assert.AreEqual("/test2", linkAliasList[1].name);
                Assert.AreEqual(1, linkAliasList[1].pageId);
                Assert.AreEqual("a=1", linkAliasList[1].queryStringSuffix);
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Re-adding a link moves it to the highest id order
        /// </summary>
        [TestMethod]
        public void test_AddLinkAlias_ReAddLink_SamePageAndQS() {
            using (CPClass cp = new(testAppName)) {
                // arrange
                CoreController core = cp.core;
                core.db.executeNonQuery("delete from " + LinkAliasModel.tableMetadata.tableNameLower);
                //
                // -- also have to delete pages created, because link-alias are automatically created for pages
                core.db.executeNonQuery($"delete from {PageContentModel.tableMetadata.tableNameLower}");
                // act
                LinkAliasController.addLinkAlias(core, "test1", 1, "a=1");
                LinkAliasController.addLinkAlias(core, "test2", 1, "a=1");
                LinkAliasController.addLinkAlias(core, "test1", 1, "a=1");
                var linkAliasList = DbBaseModel.createList<LinkAliasModel>(cp, "");
                // assert
                Assert.AreEqual(2, linkAliasList.Count);
                //
                Assert.AreEqual("/test2", linkAliasList[0].name);
                Assert.AreEqual(1, linkAliasList[0].pageId);
                Assert.AreEqual("a=1", linkAliasList[0].queryStringSuffix);
                //
                Assert.AreEqual("/test1", linkAliasList[1].name);
                Assert.AreEqual(1, linkAliasList[1].pageId);
                Assert.AreEqual("a=1", linkAliasList[1].queryStringSuffix);
            }
        }


    }
}