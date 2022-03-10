
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
                string cacheKey = cp.Cache.CreateKey( "RobotsTxt Base Text");
                result = cp.Cache.GetText(cacheKey);
                if (string.IsNullOrEmpty(result)) {
                    // -- settings page needs a filename
                    string Filename = core.siteProperties.getText("RobotsTxtFilename", "config/RobotsTxtBase.txt");
                    result = core.privateFiles.readFileText(Filename);
                    if (string.IsNullOrEmpty(result)) {
                        //
                        // save default robots.txt
                        //
                        result = "User-agent: *\r\nDisallow: /admin/\r\nDisallow: /images/";
                        core.privateFiles.saveFile(Filename, result);
                    }
                    //
                    // -- 15 minute cache. Add this to settings page 
                    cp.Cache.Store(cacheKey, result, DateTime.Now.AddMinutes(15));
                }
                result += core.addonCache.robotsTxt;
                core.webServer.responseContentType = "text/plain";
                core.doc.continueProcessing = false;
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
            }
            return result;
        }
    }
}
