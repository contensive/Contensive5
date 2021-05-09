
using System;
using System.Collections.Generic;
using Contensive.Processor.Controllers;

namespace Contensive.Processor.Addons.AdminSite {
    public class OpenAjaxIndexFilterGetContentClass : Contensive.BaseClasses.AddonBaseClass {
        //
        //====================================================================================================
        /// <summary>
        /// getFieldEditorPreference remote method
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public override object Execute(Contensive.BaseClasses.CPBaseClass cp) {
            string result = "";
            try {
                CoreController core = ((CPClass)cp).core;

                //
                core.visitProperty.setProperty("IndexFilterOpen", "1");
                int ContentId = core.docProperties.getInteger("cid");
                if (ContentId == 0) {
                    result = "No filter is available";
                } else {
                    result = ListView.getForm_IndexFilterContent(core, new AdminDataModel(core, new AdminDataRequest {
                        adminAction = 0,
                        adminButton = "",
                        adminForm = 0,
                        adminSourceForm = 0,
                        contentId = ContentId,
                        fieldEditorPreference = "",
                        guid = "",
                        id = 0,
                        recordsPerPage = 100,
                        recordTop = 0,
                        titleExtension = "",
                        wherePairDict = new Dictionary<string, string>()
                    }));
                }
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
            }
            return result;
        }
    }
}
