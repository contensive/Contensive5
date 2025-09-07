
using System;
using Contensive.Processor.Controllers;
using static Contensive.Processor.Constants;
using Contensive.Models.Db;
using NLog;
//
namespace Contensive.Processor.Addons.Tools {
    //
    public class WebsiteFileManagerToolClass : Contensive.BaseClasses.AddonBaseClass {
        //
        // static logger
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        //
        //====================================================================================================
        /// <summary>
        /// addon method, deliver complete Html admin site
        /// blank return on OK or cancel button
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public override object Execute(Contensive.BaseClasses.CPBaseClass cpBase) {
            return GetForm_WebsiteFileManager(((CPClass)cpBase).core);
        }
        //
        //=============================================================================
        //   Print the manual query form
        //=============================================================================
        //
        private string GetForm_WebsiteFileManager(CoreController core) {
            string result = "";
            try {
                string InstanceOptionString = "AdminLayout=1&filesystem=website files";
                AddonModel addon = core.cacheRuntime.addonCache.create("{B966103C-DBF4-4655-856A-3D204DEF6B21}");
                string Content = core.addon.execute(addon, new BaseClasses.CPUtilsBaseClass.addonExecuteContext {
                    addonType = Contensive.BaseClasses.CPUtilsBaseClass.addonContext.ContextAdmin,
                    argumentKeyValuePairs = GenericController.convertQSNVAArgumentstoDocPropertiesList(core, InstanceOptionString),
                    instanceGuid = "-2",
                    errorContextMessage = "executing File Manager within website file manager"
                });
                string ButtonList = ButtonApply + "," + ButtonCancel;
                //
                var layout = core.cpParent.AdminUI.CreateLayoutBuilder();
                layout.title = "Website File Manager";
                layout.description = "Manage files and folders within the Website's file area.";
                layout.body = Content;
                foreach (string button in (ButtonList).Split(',')) {
                    if (string.IsNullOrWhiteSpace(button)) continue;
                    layout.addFormButton(button.Trim());
                }
                return layout.getHtml();
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
            }
            return result;
        }
        //
    }
}

