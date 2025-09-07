//
using System;
using Contensive.Processor.Controllers;
//
namespace Contensive.Processor.Addons.FaviconIco {
    public class FaviconIcoClass : Contensive.BaseClasses.AddonBaseClass {
        //
        //====================================================================================================
        /// <summary>
        /// Add meta data for favicon to meta data
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public override object Execute(Contensive.BaseClasses.CPBaseClass cp) {
            string result = "";
            try {
                CoreController core = ((CPClass)cp).core;
                string Filename = core.siteProperties.getText("FaviconFilename", "");
                if (string.IsNullOrEmpty(Filename)) {
                    //
                    // no favicon, 404 the call
                    //
                    core.webServer.setResponseStatus(WebServerController.httpResponseStatus404_NotFound);
                    core.webServer.responseContentType = "image/gif";
                    core.doc.continueProcessing = false;
                    return string.Empty;
                } else {
                    core.doc.continueProcessing = false;
                    return core.webServer.redirect(GenericController.getCdnFileLink(core, Filename), "favicon request", false, false);
                }
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
            }
            return result;
        }
    }
}
