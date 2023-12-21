
using System;
using Contensive.Processor.Controllers;
using static Contensive.Processor.Constants;
using Contensive.Models.Db;
//
namespace Contensive.Processor.Addons.Tools {
    //
    public class ContentFileManagerToolClass : Contensive.BaseClasses.AddonBaseClass {
        //
        //====================================================================================================
        /// <summary>
        /// addon method, deliver complete Html admin site
        /// blank return on OK or cancel button
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public override object Execute(Contensive.BaseClasses.CPBaseClass cpBase) {
            return GetForm_ContentFileManager(((CPClass)cpBase).core);
        }
        //
        //=============================================================================
        //   Print the manual query form
        //=============================================================================
        //
        private string GetForm_ContentFileManager(CoreController core) {
            string result = "";
            try {
                string InstanceOptionString = "AdminLayout=1&filesystem=content files";
                AddonModel addon = core.cacheRuntime.addonCache.create("{B966103C-DBF4-4655-856A-3D204DEF6B21}");
                string Content = core.addon.execute(addon, new BaseClasses.CPUtilsBaseClass.addonExecuteContext {
                    addonType = Contensive.BaseClasses.CPUtilsBaseClass.addonContext.ContextAdmin,
                    argumentKeyValuePairs = GenericController.convertQSNVAArgumentstoDocPropertiesList(core, InstanceOptionString),
                    instanceGuid = "-2",
                    errorContextMessage = "executing File Manager addon within Content File Manager"
                });
                string Description = "Manage files and folders within the virtual content file area.";
                string ButtonList = ButtonApply + "," + ButtonCancel;
                result = AdminUIController.getToolBody(core, "Content File Manager", ButtonList, "", false, false, Description, "", 0, Content);
            } catch (Exception ex) {
                LogController.logError(core, ex);
            }
            return result;
        }
        //
    }
}

