
using System;
using Contensive.Processor.Controllers;

namespace Contensive.Processor.Addons.AdminSite {
    public class OpenAjaxIndexFilterClass : Contensive.BaseClasses.AddonBaseClass {
        //
        //====================================================================================================
        /// <summary>
        /// getFieldEditorPreference remote method
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public override object Execute(Contensive.BaseClasses.CPBaseClass cp) {
            string returnHtml = "";
            try {
                CoreController core = ((CPClass)cp).core;
                //
                core.visitProperty.setProperty("IndexFilterOpen", "1");
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
            }
            return returnHtml;
        }
    }
}
