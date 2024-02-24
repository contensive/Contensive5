
using Contensive.BaseClasses;
using Contensive.Processor.Controllers;
using System;
//
namespace Contensive.Processor.Addons.PageManager {
    //
    public class PageManagerClass : AddonBaseClass {
        //
        //====================================================================================================
        /// <summary>
        /// pageManager addon interface
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public override object Execute(CPBaseClass cp) {
            try {
                CoreController core = ((CPClass)cp).core;
                //
                // -- check for authenticated, but password expired
                if (core.session.isAuthenticated && (core.siteProperties.passwordAgeLockoutDays > 0) && (core.session.user.passwordModifiedDate < DateTime.Now.AddDays(-core.siteProperties.passwordAgeLockoutDays))) {
                    //
                    // -- authenticated, and password-age is too old, force to change-password form
                    if (core.webServer.requestPathPage != "/password-age-lockout") {
                        core.webServer.redirect("/password-age-lockout", "password age lockout.");
                        return "";
                    }
                }
                string result = PageManagerController.getHtmlBody(core);
                if ((core.doc.pageController?.page != null) && (core?.webServer?.httpContext?.Response != null) && string.IsNullOrEmpty(core.webServer.httpContext.Response.redirectUrl)) {
                    //
                    // -- add page# wrapper. This helps create targetted styles, like active style for menu active
                    result = "<div id=\"page" + core.doc.pageController.page.id + "\">" + result + "</div>";
                }
                return result;
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                return "<div style=\"width:600px;margin:20px auto;\"><h1>Server Error</h1><p>There was an issue on this site that blocked your content. Thank you for your patience.</p></div>";
            }
        }
    }
    /// <summary>
    /// 230120 tmp class to handle transition from incorrect class name to PageManager namespace
    /// </summary>
    public class GetHtmlBodyClass : AddonBaseClass {
        public override object Execute(BaseClasses.CPBaseClass cp) {
            PageManagerClass pageManager = new();
            return pageManager.Execute(cp);
        }
    }
}