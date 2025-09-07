
using Contensive.Processor.Controllers;
using System;

namespace Contensive.Processor.Addons.Primitives {
    /// <summary>
    /// remote method to export a table. requiires admin role
    /// </summary>
    public class ProcessExportAsciiMethodClass : BaseClasses.AddonBaseClass {
        //
        //====================================================================================================
        /// <summary>
        /// addon
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public override object Execute(Contensive.BaseClasses.CPBaseClass cp) {
            try {
                string result = "";
                CoreController core = ((CPClass)cp).core;
                //
                // -- Should be a remote method in commerce
                if (!core.session.isAuthenticatedAdmin()) {
                    //
                    // Administrator required
                    //
                    core.doc.userErrorList.Add("Error: You must be an administrator to use the ExportAscii method");
                    return "";
                }
                string ContentName = core.docProperties.getText("content");
                int PageSize = core.docProperties.getInteger("PageSize");
                if (PageSize == 0) {
                    PageSize = 20;
                }
                int PageNumber = core.docProperties.getInteger("PageNumber");
                if (PageNumber == 0) {
                    PageNumber = 1;
                }
                if (string.IsNullOrEmpty(ContentName)) {
                    core.doc.userErrorList.Add("Error: ExportAscii method requires ContentName");
                } else {
                    result = ExportAsciiController.exportAscii_GetAsciiExport(core, ContentName, PageSize, PageNumber);
                    core.doc.continueProcessing = false;
                }
                return result;
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                throw;
            }
        }
    }
}
