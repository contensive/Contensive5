
using Contensive.BaseClasses;
using Contensive.Models.Db;
using Contensive.Processor;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Tests.TestConstants;

namespace Tests {
    [TestClass()]
    public class CpTests {
        //====================================================================================================
        /// <summary>
        ///  Test 1 - cp ok without application (cluster mode).
        /// </summary>
        [TestMethod]
        public void views_cp_ConstructorWithoutApp() {
            // arrange
            CPClass cp = new CPClass();
            // act
            bool clusterOK = cp.serverOk;
            bool appOK = cp.appOk;
            // assert
            Assert.AreEqual(clusterOK, true);
            Assert.AreEqual(appOK, false);
            cp.Dispose();
        }
        //====================================================================================================
        /// <summary>
        /// Test 2 - cp ok with application
        /// </summary>
        [TestMethod]
        public void views_cp_ConstructorWithApp() {
            // arrange
            using (CPClass cp = new(testAppName)) {
                // act
                bool clusterOK = cp.serverOk;
                bool appOK = cp.appOk;
                // assert
                Assert.AreEqual(clusterOK, true);
                Assert.AreEqual(appOK, true);
            }
        }
        //====================================================================================================
        /// <summary>
        /// cpExecuteAddontest
        /// </summary>
        [TestMethod]
        public void views_cp_ExecuteAddontest() {
            // arrange
            using (CPClass cp = new(testAppName)) {
                using (CPCSBaseClass cs = cp.CSNew()) {
                    string addonName = "testAddon-1-" + cp.Utils.GetRandomInteger().ToString();
                    int recordId = 0;
                    string htmlText = "12345";
                    string wysiwygText = "<b>abcde</b>";
                    string addonGuid = cp.Utils.CreateGuid();
                    string activeScript = "function m\nm=cp.doc.getText(\"echo\")\nend function";
                    string echoText = "text added to document";
                    //
                    if (cs.Insert(AddonModel.tableMetadata.contentName)) {
                        recordId = cs.GetInteger("id");
                        cs.SetField("name", addonName);
                        cs.SetField("copytext", htmlText);
                        cs.SetField("copy", wysiwygText);
                        cs.SetField("ccGuid", addonGuid);
                        cs.SetField("scriptingcode", activeScript);
                    }
                    cs.Close();
                    cp.Doc.SetProperty("echo", echoText);
                    // act
                    // assert
                    Assert.AreEqual(htmlText + wysiwygText + echoText, cp.executeAddon(addonName));
                    //
                    Assert.AreEqual(htmlText + wysiwygText + echoText, cp.executeAddon(addonGuid));
                    //
                    Assert.AreEqual(htmlText + wysiwygText + echoText, cp.executeAddon(recordId.ToString()));
                    cp.Content.Delete(AddonModel.tableMetadata.contentName, "id=" + recordId.ToString());
                }
            }
        }
        //====================================================================================================
        /// <summary>
        /// cpExecuteAddontest
        /// </summary>
        [TestMethod]
        public void views_cp_ExecuteRouteTest() {
            //
            // arrange
            using (CPClass cp = new(testAppName)) {
                string addonName = "testAddon-2-" + cp.Utils.GetRandomInteger().ToString();
                int recordId = 0;
                string htmlText = "12345";
                string wysiwygText = "<b>abcde</b>";
                string addonGuid = cp.Utils.CreateGuid();
                // string activeScript = "cp.doc.getText(\"echo\")";
                string activeScript = "function m\nm=cp.doc.getText(\"echo\")\nend function";
                string echoText = "text added to document";
                //
                using (CPCSBaseClass cs = cp.CSNew()) {
                    if (cs.Insert(AddonModel.tableMetadata.contentName)) {
                        recordId = cs.GetInteger("id");
                        cs.SetField("name", addonName);
                        cs.SetField("copytext", htmlText);
                        cs.SetField("copy", wysiwygText);
                        cs.SetField("ccGuid", addonGuid);
                        cs.SetField("scriptingcode", activeScript);
                        cs.SetField("remotemethod", "1");
                    }
                }
                // -- invalidate route map cache
                //Contensive.Processor.Models.Domain.RouteMapModel.invalidateCache(cp.core);
                // - clear local route map cache 
                cp.core.routeMapRebuild();
                //
                cp.Doc.SetProperty("echo", echoText);
                // act
                string result = cp.executeRoute(addonName);
                // assert
                Assert.AreEqual(htmlText + wysiwygText + echoText, result);
            }
        }
    }
}
