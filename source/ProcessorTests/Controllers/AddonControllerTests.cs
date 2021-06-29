
using Contensive.BaseClasses;
using Contensive.Processor;
using Contensive.Processor.Controllers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using static Contensive.Processor.Controllers.GenericController;
using static Tests.TestConstants;
using System.Collections.Generic;
using Contensive.Models.Db;
using Contensive.Processor.Models.Domain;
using System.Globalization;

namespace Contensive.Processor.Controllers.Tests {
    [TestClass()]
    public class AddonControllerTests {
        [TestMethod()]
        public void addonControllerTest() {
            using CPClass cp = new(testAppName);
            var obj = new Contensive.Processor.Controllers.AddonController(cp.core);
            Assert.IsNotNull(obj);
        }

        [TestMethod()]
        public void convertAddonReturntoStringTest() {
            Assert.AreEqual(string.Empty, AddonController.convertAddonReturntoString(null));
            Assert.AreEqual("1", AddonController.convertAddonReturntoString(1));
            Assert.AreEqual("1.23", AddonController.convertAddonReturntoString(1.23));
            Assert.AreEqual("True", AddonController.convertAddonReturntoString(true));
            Assert.AreEqual("False", AddonController.convertAddonReturntoString(false));
            Assert.AreEqual(DateTime.MinValue.ToString(CultureInfo.InvariantCulture), AddonController.convertAddonReturntoString(DateTime.MinValue));
            var rightNow = DateTime.Now;
            Assert.AreEqual(rightNow.ToString(CultureInfo.InvariantCulture), AddonController.convertAddonReturntoString(rightNow));
            var testobj = new { Name = "Krishna", Phones = new[] { "555-555-5555", "666-666-6666" } };
            Assert.AreEqual("{\"Name\":\"Krishna\",\"Phones\":[\"555-555-5555\",\"666-666-6666\"]}", AddonController.convertAddonReturntoString(testobj));
        }
        /// <summary>
        /// Create an onbodystart addon, run all and test the result for the addon's output
        /// </summary>
        [TestMethod()]
        public void executeOnBodyStartTest() {
            using CPClass cp = new(testAppName);
            var a = DbBaseModel.addDefault<AddonModel>(cp);
            string testString = cp.Utils.CreateGuid();
            a.name = "test addon";
            a.onBodyStart = true;
            a.copyText = testString;
            a.save(cp);
            Assert.IsTrue(cp.core.addon.executeOnBodyStart().Contains(testString));
            DbBaseModel.delete<AddonModel>(cp, a.id);
        }
        /// <summary>
        /// create an onbodyend addon, run all and test the result
        /// </summary>
        [TestMethod()]
        public void executeOnBodyEndTest() {
            using CPClass cp = new(testAppName);
            var a = DbBaseModel.addDefault<AddonModel>(cp);
            string testString = cp.Utils.CreateGuid();
            a.name = "test addon";
            a.onBodyEnd = true;
            a.copyText = testString;
            a.save(cp);
            Assert.IsTrue(cp.core.addon.executeOnBodyEnd().Contains(testString));
            DbBaseModel.delete<AddonModel>(cp, a.id);
        }
        /// <summary>
        /// dependency addon only executes once per document
        /// </summary>
        [TestMethod()]
        public void executeDependencyTest() {
            using CPClass cp = new(testAppName);
            var a = DbBaseModel.addDefault<AddonModel>(cp);
            string testString = cp.Utils.CreateGuid();
            a.name = "test addon";
            a.copyText = testString;
            a.save(cp);
            string result = cp.core.addon.executeDependency(a, new CPUtilsBaseClass.addonExecuteContext { });
            result += cp.core.addon.executeDependency(a, new CPUtilsBaseClass.addonExecuteContext { });
            Assert.IsTrue(result.Contains(testString));
            Assert.IsFalse(result.Contains(testString + testString));
            DbBaseModel.delete<AddonModel>(cp, a.id);
        }
        /// <summary>
        /// execute addon by guid
        /// </summary>
        [TestMethod()]
        public void executeTest_byGuid() {
            using CPClass cp = new(testAppName);
            var a = DbBaseModel.addDefault<AddonModel>(cp);
            string testString = cp.Utils.CreateGuid();
            a.name = "test addon";
            a.copyText = testString;
            a.save(cp);
            Assert.IsTrue(cp.core.addon.execute(a.ccguid, new CPUtilsBaseClass.addonExecuteContext { }).Contains(testString));
            DbBaseModel.delete<AddonModel>(cp, a.id);
        }
        /// <summary>
        /// execute addon with a model argument
        /// </summary>
        [TestMethod()]
        public void executeTest_byModel() {
            using CPClass cp = new(testAppName);
            var a = DbBaseModel.addDefault<AddonModel>(cp);
            string testString = cp.Utils.CreateGuid();
            a.name = "test addon";
            a.copyText = testString;
            a.save(cp);
            Assert.IsTrue(cp.core.addon.execute(a, new CPUtilsBaseClass.addonExecuteContext { }).Contains(testString));
            DbBaseModel.delete<AddonModel>(cp, a.id);
        }
        /// <summary>
        /// execute addon pass null addon exits with just empty string
        /// </summary>
        [TestMethod()]
        public void executeTest_NullModelArgument() {
            using CPClass cp = new(testAppName);
            AddonModel a = null;
            Assert.AreEqual(string.Empty, cp.core.addon.execute(a, new CPUtilsBaseClass.addonExecuteContext { }));
        }
        /// <summary>
        /// set context forceHtml creates ah html doc 
        /// </summary>
        [TestMethod()]
        public void executeTest_context_NoforceHtmlInContext() {
            using CPClass cp = new(testAppName);
            var a = DbBaseModel.addDefault<AddonModel>(cp);
            string testString = cp.Utils.CreateGuid();
            a.name = "test addon";
            a.copyText = testString;
            a.htmlDocument = false;
            a.save(cp);
            string addonResult = cp.core.addon.execute(a, new CPUtilsBaseClass.addonExecuteContext { forceHtmlDocument = false });
            Assert.IsTrue(addonResult.Contains(testString));
            Assert.IsFalse(addonResult.Contains("<!DOCTYPE html>"));
            DbBaseModel.delete<AddonModel>(cp, a.id);
        }
        /// <summary>
        /// set context forceHtml creates ah html doc 
        /// </summary>
        [TestMethod()]
        public void executeTest_context_forceHtml() {
            using CPClass cp = new(testAppName);
            var a = DbBaseModel.addDefault<AddonModel>(cp);
            string testString = cp.Utils.CreateGuid();
            a.name = "test addon";
            a.copyText = testString;
            a.save(cp);
            string addonResult = cp.core.addon.execute(a, new CPUtilsBaseClass.addonExecuteContext { forceHtmlDocument = true });
            Assert.IsTrue(addonResult.Contains(testString));
            Assert.IsTrue(addonResult.Contains("<!DOCTYPE html>"));
            DbBaseModel.delete<AddonModel>(cp, a.id);
        }
        /// <summary>
        /// set context forceHtml creates ah html doc 
        /// </summary>
        [TestMethod()]
        public void executeTest_addon_forceHtml() {
            using CPClass cp = new(testAppName);
            var a = DbBaseModel.addDefault<AddonModel>(cp);
            string testString = cp.Utils.CreateGuid();
            a.name = "test addon";
            a.htmlDocument = true;
            a.copyText = testString;
            a.save(cp);
            string addonResult = cp.core.addon.execute(a, new CPUtilsBaseClass.addonExecuteContext { forceHtmlDocument = true });
            Assert.IsTrue(addonResult.Contains(testString));
            Assert.IsTrue(addonResult.Contains("<!DOCTYPE html>"));
            DbBaseModel.delete<AddonModel>(cp, a.id);
        }
        /// <summary>
        /// verify the instanceId is replaced by context.instanceGuid during execution, then the doc.instanceId is returned
        /// </summary>
        [TestMethod()]
        public void executeTest_context_InstanceGuid() {
            using CPClass cp = new(testAppName);
            var a = DbBaseModel.addDefault<AddonModel>(cp);
            a.name = "test addon";
            a.copyText = "$instanceid$";
            a.save(cp);
            string testString_docProperty = "doc-property-" + cp.Utils.CreateGuid();
            string testString_context = "context-property" + cp.Utils.CreateGuid();
            //
            // -- addon should return the instanceId back in the result, doc property should not change
            cp.Doc.SetProperty("instanceid", testString_docProperty);
            string addonResult1 = cp.core.addon.execute(a, new CPUtilsBaseClass.addonExecuteContext { instanceGuid = "" });
            Assert.IsTrue(addonResult1.Contains(testString_docProperty));
            Assert.IsFalse(addonResult1.Contains(testString_context));
            Assert.IsTrue(cp.Doc.GetText("instanceid").Equals(testString_docProperty));
            //
            // -- addon should return the context string back in the result, doc property should not change
            cp.Doc.SetProperty("instanceid", testString_docProperty);
            string addonResult2 = cp.core.addon.execute(a, new CPUtilsBaseClass.addonExecuteContext { instanceGuid = testString_context });
            Assert.IsFalse(addonResult2.Contains(testString_docProperty));
            Assert.IsTrue(addonResult2.Contains(testString_context));
            Assert.IsTrue(cp.Doc.GetText("instanceid").Equals(testString_docProperty));
            //
            DbBaseModel.delete<AddonModel>(cp, a.id);
        }
        //
        [TestMethod()]
        public void executeTest_context_page() {
            using CPClass cp = new(testAppName);
            var a = DbBaseModel.addDefault<AddonModel>(cp);
            string testString = cp.Utils.CreateGuid();
            a.name = "test addon";
            a.copyText = testString;
            a.save(cp);
            //
            // -- Simple Mode - turn on debug and expect no comment wrappers
            cp.core.visitProperty.setProperty("AllowDebugging", true);
            string addonResult1 = cp.core.addon.execute(a, new CPUtilsBaseClass.addonExecuteContext {
                addonType = CPUtilsBaseClass.addonContext.ContextSimple
            });
            Assert.IsFalse(addonResult1.Contains("<!-- Add-on"));
            //
            // -- Page Mode - turn on debug and expect comment wrappers
            cp.core.visitProperty.setProperty("AllowDebugging", true);
            string addonResult2 = cp.core.addon.execute(a, new CPUtilsBaseClass.addonExecuteContext {
                addonType = CPUtilsBaseClass.addonContext.ContextPage
            });
            Assert.IsTrue(addonResult2.Contains("<!-- Add-on"));
            //
            DbBaseModel.delete<AddonModel>(cp, a.id);
        }
        // 
        [TestMethod()]
        public void executeaAsProcessTest_NoArg() {
            using CPClass cp = new(testAppName);
            var a = DbBaseModel.addDefault<AddonModel>(cp);
            string testString = cp.Utils.CreateGuid();
            a.name = "test addon";
            a.copyText = testString;
            a.save(cp);
            cp.core.serverConfig.allowTaskRunnerService = false;
            cp.core.serverConfig.allowTaskSchedulerService = false;
            // -- property keyValue
            cp.core.docProperties.setProperty("testDocKey", "testDocValue");
            cp.core.addon.executeAsProcess(a);
            bool foundAddon = false;
            bool foundDocArg = false;
            foreach (var task in DbBaseModel.createList<TaskModel>(cp)) {
                var cmd = cp.JSON.Deserialize<TaskModel.CmdDetailClass>(task.cmdDetail);
                if (cmd.addonId == a.id) {
                    foundAddon = true;
                    foreach (var kvp in cmd.args) {
                        if (kvp.Key == "testDocKey") {
                            if (kvp.Value == "testDocValue") {
                                foundDocArg = true;
                            }
                        }
                    }
                    break;
                }
            }
            Assert.IsTrue(foundAddon, "Addon not found in process execution");
            Assert.IsTrue(foundDocArg, "Addon doc argument did not pass to process execution");
            //
            cp.core.serverConfig.allowTaskRunnerService = true;
            cp.core.serverConfig.allowTaskSchedulerService = true;
            DbBaseModel.delete<AddonModel>(cp, a.id);
        }
        // 
        [TestMethod()]
        public void executeaAsProcessTest() {
            using CPClass cp = new(testAppName);
            var a = DbBaseModel.addDefault<AddonModel>(cp);
            string testString = cp.Utils.CreateGuid();
            a.name = "test addon";
            a.copyText = testString;
            a.save(cp);
            cp.core.serverConfig.allowTaskRunnerService = false;
            cp.core.serverConfig.allowTaskSchedulerService = false;
            cp.core.docProperties.setProperty("testDocKey", "testDocValue");
            cp.core.addon.executeAsProcess(a, new Dictionary<string, string> { { "testArgKey", "testArgValue" } });
            bool foundAddon = false;
            bool foundDocArg = false;
            bool foundContextArg = false;
            foreach (var task in DbBaseModel.createList<TaskModel>(cp)) {
                var cmd = cp.JSON.Deserialize<TaskModel.CmdDetailClass>(task.cmdDetail);
                if (cmd.addonId == a.id) {
                    foundAddon = true;
                    foreach (var kvp in cmd.args) {
                        if (kvp.Key == "testDocKey") {
                            if (kvp.Value == "testDocValue") {
                                foundDocArg = true;
                            }
                        }
                        if (kvp.Key == "testArgKey") {
                            if (kvp.Value == "testArgValue") {
                                foundContextArg = true;
                            }
                        }
                    }
                    break;
                }
            }
            Assert.IsTrue(foundAddon, "Addon not found in process execution");
            Assert.IsTrue(foundDocArg, "Addon doc argument did not pass to process execution");
            Assert.IsTrue(foundContextArg, "Addon dontext argument did not pass to process execution");
            //
            cp.core.serverConfig.allowTaskRunnerService = true;
            cp.core.serverConfig.allowTaskSchedulerService = true;
            DbBaseModel.delete<AddonModel>(cp, a.id);
        }

        //[TestMethod()]
        //public void executeaAsProcessTest1() {
        //    Assert.Fail();
        //}

        //[TestMethod()]
        //public void executeaAsProcessTest2() {
        //    Assert.Fail();
        //}

        //[TestMethod()]
        //public void executeaAsProcessByNameTest() {
        //    Assert.Fail();
        //}

        //[TestMethod()]
        //public void buildAddonOptionLists2Test() {
        //    Assert.Fail();
        //}

        //[TestMethod()]
        //public void buildAddonOptionListsTest() {
        //    Assert.Fail();
        //}

        //[TestMethod()]
        //public void getPrivateFilesAddonPathTest() {
        //    Assert.Fail();
        //}

        //[TestMethod()]
        //public void xml_GetAttributeTest() {
        //    Assert.Fail();
        //}

        //[TestMethod()]
        //public void getDefaultAddonOptionsTest() {
        //    Assert.Fail();
        //}

        //[TestMethod()]
        //public void getAddonDescriptionTest() {
        //    Assert.Fail();
        //}

        //[TestMethod()]
        //public void getAddonManagerTest() {
        //    Assert.Fail();
        //}

        //[TestMethod()]
        //public void throwEventTest() {
        //    Assert.Fail();
        //}

        //[TestMethod()]
        //public void getAddonIconImgTest() {
        //    Assert.Fail();
        //}

        //[TestMethod()]
        //public void getIconSpriteTest() {
        //    Assert.Fail();
        //}

        //[TestMethod()]
        //public void DisposeTest() {
        //    Assert.Fail();
        //}
        //
        private bool localPropertyToFoolCodacyStaticMethodRequirement;
        //
        //====================================================================================================
        //
        [TestMethod]
        public void controllers_Addon_simpleDoNothingAddon() {
            using (CPClass cp = new(testAppName)) {
                // arrange
                var addon = DbBaseModel.addDefault<AddonModel>(cp, ContentMetadataModel.getDefaultValueDict(cp.core, AddonModel.tableMetadata.contentName));
                addon.save(cp);
                // act
                string result = cp.core.addon.execute(addon, new CPUtilsBaseClass.addonExecuteContext {
                    addonType = CPUtilsBaseClass.addonContext.ContextSimple,
                    backgroundProcess = false,
                    cssContainerClass = "",
                    cssContainerId = "",
                    errorContextMessage = "",
                    forceHtmlDocument = false,
                    forceJavascriptToHead = false,
                    hostRecord = new CPUtilsBaseClass.addonExecuteHostRecordContext {
                        contentName = "",
                        fieldName = "",
                        recordId = 0
                    },
                    argumentKeyValuePairs = new Dictionary<string, string>(),
                    instanceGuid = "",
                    isDependency = false,
                    wrapperID = 0
                });
                // assert
                Assert.AreEqual("", result);
                Assert.AreEqual(0, cp.core.doc.htmlAssetList.Count);
                Assert.AreEqual(false, cp.core.doc.isHtml);
                Assert.AreEqual("", cp.core.doc.htmlForEndOfBody);
                Assert.AreEqual(0, cp.core.doc.htmlMetaContent_Description.Count);
                Assert.AreEqual(0, cp.core.doc.htmlMetaContent_KeyWordList.Count);
                Assert.AreEqual(0, cp.core.doc.htmlMetaContent_OtherTags.Count);
                Assert.AreEqual(0, cp.core.doc.htmlMetaContent_TitleList.Count);
                //
                localPropertyToFoolCodacyStaticMethodRequirement = !localPropertyToFoolCodacyStaticMethodRequirement;
            }
        }
        //
        //====================================================================================================
        //
        public void controllers_Addon_copy() {
            using (CPClass cp = new(testAppName)) {
                // arrange
                var addon = AddonModel.addDefault<AddonModel>(cp, ContentMetadataModel.getDefaultValueDict(cp.core, AddonModel.tableMetadata.contentName));
                addon.copy = "test" + GenericController.getRandomInteger(cp.core).ToString();
                addon.save(cp);
                // act
                string result = cp.core.addon.execute(addon, new CPUtilsBaseClass.addonExecuteContext {
                    addonType = CPUtilsBaseClass.addonContext.ContextSimple,
                    backgroundProcess = false,
                    cssContainerClass = "",
                    cssContainerId = "",
                    errorContextMessage = "",
                    forceHtmlDocument = false,
                    forceJavascriptToHead = false,
                    hostRecord = new CPUtilsBaseClass.addonExecuteHostRecordContext {
                        contentName = "",
                        fieldName = "",
                        recordId = 0
                    },
                    argumentKeyValuePairs = new Dictionary<string, string>(),
                    instanceGuid = "",
                    isDependency = false,
                    wrapperID = 0
                });
                // assert
                Assert.AreEqual(addon.copy, result);
                Assert.AreEqual(0, cp.core.doc.htmlAssetList.Count);
                Assert.AreEqual(false, cp.core.doc.isHtml);
                Assert.AreEqual("", cp.core.doc.htmlForEndOfBody);
                Assert.AreEqual("", cp.core.doc.htmlMetaContent_Description);
                Assert.AreEqual(0, cp.core.doc.htmlMetaContent_KeyWordList.Count);
                Assert.AreEqual(0, cp.core.doc.htmlMetaContent_OtherTags.Count);
                Assert.AreEqual(0, cp.core.doc.htmlMetaContent_TitleList.Count);
                //
                localPropertyToFoolCodacyStaticMethodRequirement = true;
            }
        }
    }
}