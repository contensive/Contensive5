
using Contensive.BaseClasses;
using Contensive.Models.Db;
using Contensive.Processor.Addons.Tools;
using Contensive.Processor.Controllers;
using Contensive.Exceptions;
using Contensive.Processor.Models.Domain;
using System;
using System.Collections.Generic;
using System.Text;
using static Contensive.Processor.Constants;
using static Contensive.Processor.Controllers.GenericController;

namespace Contensive.Processor.Addons.AdminSite {
    //
    //====================================================================================================
    /// <summary>
    /// Admin site addon
    /// </summary>
    public class AdminAddon : AddonBaseClass {
        //
        //====================================================================================================
        /// <summary>
        /// Admin site addon
        /// </summary>
        /// <param name="cpBase"></param>
        /// <returns></returns>
        public override object Execute(CPBaseClass cpBase) {
            CPClass cp = (CPClass)cpBase;
            try {
                if (!cp.core.session.isAuthenticated) {
                    //
                    // --- must be authenticated to continue. Force a local login
                    return LoginController.getLoginPage(((CPClass)cp).core, true, true);
                }
                if (!cp.core.session.isAuthenticatedAdmin()) {
                    //
                    // --- member must have proper access to continue
                    string result = ""
                        + "<p>You are attempting to enter an area in which your account does not have access.</p>"
                        + "<ul class=\"ccList\">"
                        + "<li class=\"ccListItem\">To return to the public web site, use your back button, or <a href=\"" + "/" + "\">Click Here</A>."
                        + "<li class=\"ccListItem\">To login under a different account, <a href=\"/" + cp.core.appConfig.adminRoute + "?method=logout\" rel=\"nofollow\">Click Here</A>"
                        + "<li class=\"ccListItem\">To have your account access changed to include this area, please contact the <a href=\"mailto:" + cp.core.siteProperties.getText("EmailAdmin") + "\">system administrator</A>. "
                        + "\r</ul>"
                        + "";
                    result = ""
                        + "<div style=\"display:table;padding:100px 0 0 0;margin:0 auto;\">"
                        + cp.core.html.getPanelHeader("Unauthorized Access")
                        + cp.core.html.getPanel(result, "ccPanel", "ccPanelHilite", "ccPanelShadow", "400", 15)
                        + "</div>";
                    cp.core.html.addTitle("Unauthorized Access", "adminSite");
                    return Processor.Controllers.HtmlController.div(result, "container-fluid ccBodyAdmin ccCon");
                }
                //
                // get admin site
                return getAdminSite(cp);
            } catch (Exception ex) {
                LogController.logError(cp.core, ex);
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Create the admin site from a layout + adminHeader + adminNav + adminBody + adminFooter
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public string getAdminSite(CPClass cp) {
            try {
                //
                // -- disable tool panel for /help pages
                cp.Doc.SetProperty("AllowToolPanel", false);
                //
                // -- block search engines. This should be blocked anyway.
                cp.Doc.AddHeadTag("<meta name=\"robots\" content=\"noindex,nofollow\">");
                //
                // -- get layout - first layout record, then layout-file, then layout-resource
                // todo -- plan is to include layout-files with base51.zip file, instead of base51.xml file -- need to review the build process to better understand first
                string layout = cp.Layout.GetLayout(Constants.guidLayoutAdminSite, "Admin Site Layout", @"adminsite\AdminSiteLayout.html");
                if (string.IsNullOrEmpty(layout)) { layout = Processor.Properties.Resources.AdminSiteLayoutBackup; }
                //
                // -- get static data
                AdminSiteViewModel viewModel = new(cp);
                return cp.Mustache.Render(layout, viewModel);
            } catch (Exception) {
                throw;
            }
        }
    }
    //
}
