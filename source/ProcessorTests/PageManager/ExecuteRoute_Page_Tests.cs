
using Contensive.Models.Db;
using Contensive.Processor;
using Contensive.Processor.Models.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using static Tests.TestConstants;

namespace Tests {
    /// <summary>
    /// 
    /// </summary>
    [TestClass()]
    public class ExecuteRoute_Page_Tests {
        /// <summary>
        /// 
        /// </summary>
        [TestMethod]
        public void ExecuteRoute_Page_Test() {
            using (CPClass cp = new(testAppName)) {
                //
                // arrange
                cp.Site.SetProperty("ALLOW HTML MINIFY", false);
                cp.core.siteProperties.anonymousUserResponseID = 0;
                // -- addon collection
                const string guidBaseCollection = "{7C6601A7-9D52-40A3-9570-774D0D43D758}";
                AddonCollectionModel baseCollection = DbBaseModel.create<AddonCollectionModel>(cp, guidBaseCollection);
                // -- test addon returns testString
                AddonModel testAddon = DbBaseModel.addDefault<AddonModel>(cp);
                testAddon.name = cp.Utils.GetRandomInteger().ToString();
                testAddon.dotNetClass = "Contensive.Processor.Addons.TestAddon";
                testAddon.remoteMethod = true;
                testAddon.collectionId = baseCollection.id;
                testAddon.htmlDocument = true;
                testAddon.save(cp);
                string testString = cp.Utils.GetRandomInteger().ToString();
                cp.Doc.SetProperty("test-in", testString);
                // -- addonList for page
                List<AddonListItemModel_Dup> testAddonList = new();
                testAddonList.Add(new AddonListItemModel_Dup() {
                    designBlockTypeGuid = testAddon.ccguid,
                    designBlockTypeName = "test addon"
                });
                // -- page to render
                PageContentModel testPage = DbBaseModel.addDefault<PageContentModel>(cp);
                testPage.name = cp.Utils.GetRandomInteger().ToString();
                testPage.addonList = cp.JSON.Serialize(testAddonList);
                testPage.save(cp);
                // -- link alias for page
                LinkAliasModel linkAlias = DbBaseModel.addDefault<LinkAliasModel>(cp);
                linkAlias.name = testPage.name;
                linkAlias.pageId = testPage.id;
                linkAlias.save(cp);
                // -- rebuild routes after adding new page
                cp.core.routeMapRebuild();
                //
                // act
                string doc = cp.executeRoute("/" + testPage.name);
                //
                // assert 
                Assert.IsTrue(doc.Contains(testString));
                // full html page
                Assert.IsTrue(doc.ToLower().Contains("<html"));
                Assert.IsTrue(doc.ToLower().Contains("<head"));
                Assert.IsTrue(doc.ToLower().Contains("<body"));
                // cleanup
                DbBaseModel.delete<AddonModel>(cp, testAddon.id);
                DbBaseModel.delete<PageContentModel>(cp, testPage.id);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        [TestMethod]
        public void ExecuteRoute_Page_AnonymousBlock_Redirect_Test() {
            HttpContextModel httpContext = new HttpContextModel();
            using (CPClass cp = new(testAppName, httpContext)) {
                //
                // arrange
                cp.Site.SetProperty("ALLOW HTML MINIFY", false);
                //
                // -- addon collection
                const string guidBaseCollection = "{7C6601A7-9D52-40A3-9570-774D0D43D758}";
                AddonCollectionModel baseCollection = DbBaseModel.create<AddonCollectionModel>(cp, guidBaseCollection);
                //
                // -- test addon returns testString
                AddonModel renderPageAddon = DbBaseModel.addDefault<AddonModel>(cp);
                renderPageAddon.name = cp.Utils.GetRandomInteger().ToString();
                renderPageAddon.dotNetClass = "Contensive.Processor.Addons.TestAddon";
                renderPageAddon.remoteMethod = true;
                renderPageAddon.collectionId = baseCollection.id;
                renderPageAddon.htmlDocument = true;
                renderPageAddon.save(cp);
                //
                string renderPageContent = cp.Utils.GetRandomInteger().ToString();
                cp.Doc.SetProperty("test-in", renderPageContent);
                //
                // -- addonList for page
                List<AddonListItemModel_Dup> testAddonList = new();
                testAddonList.Add(new AddonListItemModel_Dup() {
                    designBlockTypeGuid = renderPageAddon.ccguid,
                    designBlockTypeName = "test addon"
                });
                // -- page to render
                PageContentModel testPage = DbBaseModel.addDefault<PageContentModel>(cp);
                testPage.name = cp.Utils.GetRandomInteger().ToString();
                testPage.addonList = cp.JSON.Serialize(testAddonList);
                testPage.save(cp);
                //
                // -- link alias for render page
                LinkAliasModel renderLinkAlias = DbBaseModel.addDefault<LinkAliasModel>(cp);
                renderLinkAlias.name = testPage.name;
                renderLinkAlias.pageId = testPage.id;
                renderLinkAlias.save(cp);
                //
                // -- login form addon
                AddonModel loginAddon = DbBaseModel.addDefault<AddonModel>(cp);
                loginAddon.name = cp.Utils.GetRandomInteger().ToString();
                loginAddon.collectionId = baseCollection.id;
                loginAddon.copyText = cp.Utils.GetRandomInteger().ToString();
                loginAddon.save(cp);
                //
                // -- addonList for login form
                List<AddonListItemModel_Dup> loginAddonList = new();
                loginAddonList.Add(new AddonListItemModel_Dup() {
                    designBlockTypeGuid = loginAddon.ccguid,
                    designBlockTypeName = "login addon"
                });
                //
                // -- page for login
                PageContentModel loginPage = DbBaseModel.addDefault<PageContentModel>(cp);
                loginPage.name = cp.Utils.GetRandomInteger().ToString();
                loginPage.addonList = cp.JSON.Serialize(loginAddonList);
                loginPage.save(cp);
                //
                // -- link alias for login page
                LinkAliasModel loginLinkAlias = DbBaseModel.addDefault<LinkAliasModel>(cp);
                loginLinkAlias.name = loginPage.name;
                loginLinkAlias.pageId = loginPage.id;
                loginLinkAlias.save(cp);
                //
                // -- rebuild routes after adding new page
                cp.core.routeMapRebuild();
                //
                // -- anonymouse redirect to login page
                cp.core.siteProperties.anonymousUserResponseID = 3;
                cp.core.siteProperties.loginPageId = loginPage.id;
                //
                // act - test page should be rediredct to login page
                string doc = cp.executeRoute("/" + testPage.name);
                //
                // assert neither page is returned, just a redirect to the login page
                string redirectProtocol = "";
                string redirectDomain = "";
                string redirectPath = "";
                string redirectPage = "";
                string redirectQueryString = "";
                cp.Utils.SeparateURL(httpContext.Response.redirectUrl, ref redirectProtocol, ref redirectDomain, ref redirectPath, ref redirectPage, ref redirectQueryString);
                Assert.AreEqual(loginLinkAlias.name, redirectPath + redirectPage);
                // cleanup
                DbBaseModel.delete<AddonModel>(cp, renderPageAddon.id);
                DbBaseModel.delete<PageContentModel>(cp, testPage.id);
                DbBaseModel.delete<PageContentModel>(cp, loginPage.id);
                DbBaseModel.delete<LinkAliasModel>(cp, loginLinkAlias.id);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        [TestMethod]
        public void ExecuteRoute_Page_AnonymousBlock_LoginForm_Test() {
            HttpContextModel httpContext = new HttpContextModel();
            using (CPClass cp = new(testAppName, httpContext)) {
                //
                // arrange
                cp.Site.SetProperty("ALLOW HTML MINIFY", false);
                //
                // -- addon collection
                const string guidBaseCollection = "{7C6601A7-9D52-40A3-9570-774D0D43D758}";
                AddonCollectionModel baseCollection = DbBaseModel.create<AddonCollectionModel>(cp, guidBaseCollection);
                //
                // -- test addon returns testString
                AddonModel renderPageAddon = DbBaseModel.addDefault<AddonModel>(cp);
                renderPageAddon.name = cp.Utils.GetRandomInteger().ToString();
                renderPageAddon.dotNetClass = "Contensive.Processor.Addons.TestAddon";
                renderPageAddon.remoteMethod = true;
                renderPageAddon.collectionId = baseCollection.id;
                renderPageAddon.htmlDocument = true;
                renderPageAddon.save(cp);
                //
                string renderPageContent = cp.Utils.GetRandomInteger().ToString();
                cp.Doc.SetProperty("test-in", renderPageContent);
                //
                // -- addonList for page
                List<AddonListItemModel_Dup> testAddonList = new();
                testAddonList.Add(new AddonListItemModel_Dup() {
                    designBlockTypeGuid = renderPageAddon.ccguid,
                    designBlockTypeName = "test addon"
                });
                // -- page to render
                PageContentModel testPage = DbBaseModel.addDefault<PageContentModel>(cp);
                testPage.name = cp.Utils.GetRandomInteger().ToString();
                testPage.addonList = cp.JSON.Serialize(testAddonList);
                testPage.save(cp);
                //
                // -- link alias for render page
                LinkAliasModel renderLinkAlias = DbBaseModel.addDefault<LinkAliasModel>(cp);
                renderLinkAlias.name = testPage.name;
                renderLinkAlias.pageId = testPage.id;
                renderLinkAlias.save(cp);
                //
                // -- login form addon
                AddonModel loginAddon = DbBaseModel.addDefault<AddonModel>(cp);
                loginAddon.name = cp.Utils.GetRandomInteger().ToString();
                loginAddon.collectionId = baseCollection.id;
                loginAddon.copyText = cp.Utils.GetRandomInteger().ToString();
                loginAddon.save(cp);
                //
                // -- addonList for login form
                List<AddonListItemModel_Dup> loginAddonList = new();
                loginAddonList.Add(new AddonListItemModel_Dup() {
                    designBlockTypeGuid = loginAddon.ccguid,
                    designBlockTypeName = "login addon"
                });
                //
                // -- rebuild routes after adding new page
                cp.core.routeMapRebuild();
                //
                // -- block with login form
                cp.core.siteProperties.anonymousUserResponseID = 1;
                cp.core.siteProperties.loginPageAddonId = loginAddon.id;
                //cp.core.siteProperties.loginPageId = loginPage.id;
                //
                // act - test page should be rediredct to login page
                string doc = cp.executeRoute("/" + testPage.name);
                //
                // assert neither page is returned, just a redirect to the login page
                Assert.IsTrue(doc.Contains(loginAddon.copyText));
                // cleanup
                DbBaseModel.delete<AddonModel>(cp, renderPageAddon.id);
                DbBaseModel.delete<PageContentModel>(cp, testPage.id);
                DbBaseModel.delete<AddonModel>(cp, loginAddon.id);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [TestMethod]
        public void ExecuteRoute_Page_AnonymousBlock_Message_Test() {
            HttpContextModel httpContext = new HttpContextModel();
            using (CPClass cp = new(testAppName, httpContext)) {
                //
                // arrange
                cp.Site.SetProperty("ALLOW HTML MINIFY", false);
                //
                // -- addon collection
                const string guidBaseCollection = "{7C6601A7-9D52-40A3-9570-774D0D43D758}";
                AddonCollectionModel baseCollection = DbBaseModel.create<AddonCollectionModel>(cp, guidBaseCollection);
                //
                // -- test addon returns testString
                AddonModel renderPageAddon = DbBaseModel.addDefault<AddonModel>(cp);
                renderPageAddon.name = cp.Utils.GetRandomInteger().ToString();
                renderPageAddon.dotNetClass = "Contensive.Processor.Addons.TestAddon";
                renderPageAddon.remoteMethod = true;
                renderPageAddon.collectionId = baseCollection.id;
                renderPageAddon.htmlDocument = true;
                renderPageAddon.save(cp);
                //
                string renderPageContent = cp.Utils.GetRandomInteger().ToString();
                cp.Doc.SetProperty("test-in", renderPageContent);
                //
                // -- addonList for page
                List<AddonListItemModel_Dup> testAddonList = new();
                testAddonList.Add(new AddonListItemModel_Dup() {
                    designBlockTypeGuid = renderPageAddon.ccguid,
                    designBlockTypeName = "test addon"
                });
                // -- page to render
                PageContentModel testPage = DbBaseModel.addDefault<PageContentModel>(cp);
                testPage.name = cp.Utils.GetRandomInteger().ToString();
                testPage.addonList = cp.JSON.Serialize(testAddonList);
                testPage.save(cp);
                //
                // -- link alias for render page
                LinkAliasModel renderLinkAlias = DbBaseModel.addDefault<LinkAliasModel>(cp);
                renderLinkAlias.name = testPage.name;
                renderLinkAlias.pageId = testPage.id;
                renderLinkAlias.save(cp);
                //
                // -- login form addon
                AddonModel loginAddon = DbBaseModel.addDefault<AddonModel>(cp);
                loginAddon.name = cp.Utils.GetRandomInteger().ToString();
                loginAddon.collectionId = baseCollection.id;
                loginAddon.copyText = cp.Utils.GetRandomInteger().ToString();
                loginAddon.save(cp);
                //
                // -- addonList for login form
                List<AddonListItemModel_Dup> loginAddonList = new();
                loginAddonList.Add(new AddonListItemModel_Dup() {
                    designBlockTypeGuid = loginAddon.ccguid,
                    designBlockTypeName = "login addon"
                });
                //
                // -- rebuild routes after adding new page
                cp.core.routeMapRebuild();
                //
                // -- block with login form
                cp.core.siteProperties.anonymousUserResponseID = 2;
                cp.core.siteProperties.anonymousUserResponseCopy = cp.Utils.GetRandomInteger().ToString();
                //cp.core.siteProperties.loginPageId = loginPage.id;
                //
                // act - test page should be rediredct to login page
                string doc = cp.executeRoute("/" + testPage.name);
                //
                // assert neither page is returned, just a redirect to the login page
                Assert.IsTrue(doc.Contains(cp.core.siteProperties.anonymousUserResponseCopy));
                // cleanup
                DbBaseModel.delete<AddonModel>(cp, renderPageAddon.id);
                DbBaseModel.delete<PageContentModel>(cp, testPage.id);
                //DbBaseModel.delete<AddonModel>(cp, loginAddon.id);
            }
        }
    }
}