
using System;
using Contensive.Processor.Controllers;
using static Contensive.Processor.Constants;
//
namespace Contensive.Processor.Addons.Tools {
    //
    public class CreateGUIDToolClass : Contensive.BaseClasses.AddonBaseClass {
        //
        //====================================================================================================
        /// <summary>
        /// addon method, deliver complete Html admin site
        /// blank return on OK or cancel button
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public override object Execute(Contensive.BaseClasses.CPBaseClass cpBase) {
            return get(((CPClass)cpBase).core);
        }
        //
        public static string get(CoreController core) {
            try {
                var result_guid = new StringBuilderLegacyController();
                result_guid.add(AdminUIController.getHeaderTitleDescription("Create GUID", "Use this tool to create a GUID. This is useful when creating new Addons."));
                //
                // Process the form
                string Button = core.docProperties.getText("button");
                if (Button.Equals(ButtonCancel)) { return string.Empty; }
                //
                result_guid.add(HtmlController.inputText_Legacy(core, "GUID", GenericController.getGUID(), 1, 80));
                //
                // Display form
                return AdminUIController.getToolForm(core, result_guid.text, ButtonCancel + "," + ButtonCreateGUId);
            } catch (Exception ex) {
                LogController.logError(core, ex);
                return string.Empty;
            }
        }
    }
}

