//
using System;
using Contensive.Processor.Controllers;
using static Contensive.Processor.Constants;
//
namespace Contensive.Processor.Addons.Primitives {
    public class ProcessRedirectMethodClass : Contensive.BaseClasses.AddonBaseClass {
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
                // ----- Redirect with RC and RI
                //
                core.doc.redirectContentID = core.docProperties.getInteger(rnRedirectContentId);
                core.doc.redirectRecordID = core.docProperties.getInteger(rnRedirectRecordId);
                if (core.doc.redirectContentID != 0 && core.doc.redirectRecordID != 0) {
                    string ContentName = MetadataController.getContentNameByID(core, core.doc.redirectContentID);
                    if (!string.IsNullOrEmpty(ContentName)) {
                        core.webServer.redirectByRecord_ReturnStatus( ContentName, core.doc.redirectRecordID,"");
                        result = "";
                        core.doc.continueProcessing = false;
                    }
                }
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
            }
            return result;
        }
    }
}
