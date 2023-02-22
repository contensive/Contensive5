
using System;
using Contensive.Processor.Controllers;
//
namespace Contensive.Processor.Addons.RobotsTxt {
    public class RobotsTxtClass : Contensive.BaseClasses.AddonBaseClass {
        //
        //====================================================================================================
        /// <summary>
        /// Return robots.exe. NOTE - this route requires an exception added to web.config
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public override object Execute(Contensive.BaseClasses.CPBaseClass cp) {
            string result = "";
            try {
                CoreController core = ((CPClass)cp).core;
                result = core.siteProperties.robotsTxt;
                result += core.cacheStore.addonCache.robotsTxt;
                core.webServer.responseContentType = "text/plain";
                core.doc.continueProcessing = false;
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
            }
            return result;
        }
    }
}
