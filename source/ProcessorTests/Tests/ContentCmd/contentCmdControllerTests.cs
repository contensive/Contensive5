﻿
using Contensive.BaseClasses;
using Contensive.Models.Db;
using Contensive.Processor;
using Contensive.Processor.Controllers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Tests.TestConstants;

namespace Tests {
    [TestClass()]
    public class contentCmdControllerTests {
        //
        /// <summary>
        /// simple syntax, one command, run addon
        /// </summary>
        [TestMethod]
        public void contentCmdController_SimpleSyntax_runAddon()  {
            using (CPClass cp = new(testAppName)) {
                // arrange
                var addon = AddonModel.addEmpty<AddonModel>(cp);
                addon.name = "testaddon-4-" + GenericController.getRandomInteger().ToString() ;
                addon.copyText = "foo";
                addon.save(cp);
                string cmd = "<div>{% \"" + addon.name + "\" %}</div>";
                CPUtilsBaseClass.addonContext context = CPUtilsBaseClass.addonContext.ContextSimple;
                // act
                string result = ContentCmdController.executeContentCommands(cp.core, cmd, context);
                // assert
                Assert.AreEqual("<div>foo</div>", result);
                // clean up
                DbBaseModel.delete<AddonModel>(cp, addon.id);
            }
        }
        //
        /// <summary>
        /// simple syntax, two commands, run addon
        /// </summary>
        [TestMethod]
        public void contentCmdController_SimpleSyntax_mulipleContextSwitch()  {
            using (CPClass cp = new(testAppName)) {
                // arrange
                AddonModel addonFoo = AddonModel.addEmpty<AddonModel>(cp);
                addonFoo.name = "addonFoo" + GenericController.getRandomInteger().ToString();
                addonFoo.copyText = "foo";
                addonFoo.save(cp);
                //
                AddonModel addonBar = AddonModel.addEmpty<AddonModel>(cp);
                addonBar.name = "addonBar" + GenericController.getRandomInteger().ToString();
                addonBar.copyText = "Bar";
                addonBar.save(cp);
                string cmd = "<div>{% \"" + addonFoo.name + "\" %}{% \"" + addonBar.name + "\" %}+{% \"" + addonFoo.name + "\" %}\n{% \"" + addonBar.name + "\" %}</div>";
                CPUtilsBaseClass.addonContext context = CPUtilsBaseClass.addonContext.ContextSimple;
                // act
                string result = ContentCmdController.executeContentCommands(cp.core, cmd, context);
                // assert
                Assert.AreEqual("<div>fooBar+foo\nBar</div>", result);
                // clean up
                DbBaseModel.delete<AddonModel>(cp, addonBar.id);
                DbBaseModel.delete<AddonModel>(cp, addonFoo.id);
            }
        }
        //
        /// <summary>
        /// Multiple Command Syntax, one command, {%[{"command1":"commandArgument"},{"command2":"commandArgument"}]%}
        /// </summary>
        [TestMethod]
        public void contentCmdController_SimpleSyntax_CommandList()  {
            using (CPClass cp = new(testAppName)) {
                // arrange
                AddonModel addonFoo = AddonModel.addEmpty<AddonModel>(cp);
                addonFoo.name = "addonFoo" + GenericController.getRandomInteger().ToString();
                addonFoo.copyText = "foo";
                addonFoo.save(cp);
                //
                AddonModel addonBar = AddonModel.addEmpty<AddonModel>(cp);
                addonBar.name = "addonBar" + GenericController.getRandomInteger().ToString();
                addonBar.copyText = "Bar";
                addonBar.save(cp);
                string cmd = "<div>{%[{\"" + addonFoo.name + "\":\"commandArgument\"}]%}</div>";
                CPUtilsBaseClass.addonContext context = CPUtilsBaseClass.addonContext.ContextSimple;
                // act
                string result = ContentCmdController.executeContentCommands(cp.core, cmd, context);
                // assert
                Assert.AreEqual("<div>foo</div>", result);
                // clean up
                DbBaseModel.delete<AddonModel>(cp, addonFoo.id);
                DbBaseModel.delete<AddonModel>(cp, addonBar.id);
            }
        }
        //
        /// <summary>
        /// Multiple Command Syntax, two commands, {%[{"command1":"commandArgument"},{"command2":"commandArgument"}]%}
        /// </summary>
        [TestMethod]
        public void contentCmdController_SimpleSyntax_CommandList_MultipleCommands()  {
            using (CPClass cp = new(testAppName)) {
                // arrange
                AddonModel addonFoo = AddonModel.addEmpty<AddonModel>(cp);
                addonFoo.name = "addonFoo" + GenericController.getRandomInteger().ToString();
                addonFoo.copyText = "foo";
                addonFoo.save(cp);
                //
                AddonModel addonBar = AddonModel.addEmpty<AddonModel>(cp);
                addonBar.name = "addonBar" + GenericController.getRandomInteger().ToString();
                addonBar.copyText = "$cmdAccumulator$Bar";
                addonBar.save(cp);
                string cmd = "<div>{%[{\"" + addonFoo.name + "\":\"commandArgument\"},{\"" + addonBar.name + "\":\"commandArgument\"}]%}</div>";
                CPUtilsBaseClass.addonContext context = CPUtilsBaseClass.addonContext.ContextSimple;
                // act
                string result = ContentCmdController.executeContentCommands(cp.core, cmd, context);
                // assert
                Assert.AreEqual("<div>fooBar</div>", result);
                // clean up
                DbBaseModel.delete<AddonModel>(cp, addonFoo.id);
                DbBaseModel.delete<AddonModel>(cp, addonBar.id);
            }
        }
        //
        /// <summary>
        /// json syntax, one command, run addon
        /// </summary>
        [TestMethod]
        public void contentCmdController_JsonSyntax()  {
            using (CPClass cp = new(testAppName)) {
                // arrange
                AddonModel addonFoo = AddonModel.addEmpty<AddonModel>(cp);
                addonFoo.name = "addonFoo" + GenericController.getRandomInteger().ToString();
                addonFoo.copyText = "foo";
                addonFoo.save(cp);
                string cmd = "<div class=\"sample\">{% {\"addon\":{\"addon\":\"" + addonFoo.name + "\"}} %}</div>";
                CPUtilsBaseClass.addonContext context = CPUtilsBaseClass.addonContext.ContextSimple;
                // act
                string result = ContentCmdController.executeContentCommands(cp.core, cmd, context);
                // assert
                Assert.AreEqual("<div class=\"sample\">foo</div>", result);
                // clean up
                DbBaseModel.delete<AddonModel>(cp, addonFoo.id);
            }
        }
        //
        /// <summary>
        /// json syntax, one command, run addon
        /// </summary>
        [TestMethod]
        public void contentCmdController_JsonSyntax_WithArgument()  {
            using (CPClass cp = new(testAppName)) {
                // arrange
                AddonModel addonFoo = AddonModel.addEmpty<AddonModel>(cp);
                addonFoo.name = "addonFoo" + GenericController.getRandomInteger().ToString();
                addonFoo.copyText = "foo$ReplaceMe$";
                addonFoo.save(cp);
                string cmd = "<div class=\"sample\">{% {\"addon\":{\"addon\":\"" + addonFoo.name + "\",\"ReplaceMe\":\"BAR\"}} %}</div>";
                CPUtilsBaseClass.addonContext context = CPUtilsBaseClass.addonContext.ContextSimple;
                // act
                string result = ContentCmdController.executeContentCommands(cp.core, cmd, context);
                // assert
                Assert.AreEqual("<div class=\"sample\">fooBAR</div>", result);
                // clean up
                DbBaseModel.delete<AddonModel>(cp, addonFoo.id);
            }
        }
        //
        /// <summary>
        /// json syntax, one command, run addon
        /// </summary>
        [TestMethod]
        public void Controllers_JsonSyntax_executeAddonTest_argumentReplacement()  {
            using (CPClass cp = new(testAppName)) {
                // arrange
                AddonModel addon = AddonModel.addEmpty<AddonModel>(cp);
                addon.name = "testaddon-3-" + GenericController.getRandomInteger().ToString();
                addon.copyText = "foo$insert$";
                addon.save(cp);
                string cmd = "<div class=\"sample\">{% {\"addon\":{\"addon\":\"" + addon.name + "\",\"insert\":\"bar\"}} %}</div>";
                CPUtilsBaseClass.addonContext context = CPUtilsBaseClass.addonContext.ContextSimple;
                // act
                string result = ContentCmdController.executeContentCommands(cp.core, cmd, context);
                // assert
                Assert.AreEqual("<div class=\"sample\">foobar</div>", result);
                // clean up
                DbBaseModel.delete<AddonModel>(cp, addon.id);
            }
        }

    }
}