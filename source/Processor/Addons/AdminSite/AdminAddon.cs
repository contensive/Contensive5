
using Contensive.BaseClasses;
using Contensive.Models.Db;
using Contensive.Processor.Addons.Tools;
using Contensive.Processor.Controllers;
using Contensive.Processor.Exceptions;
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
                    cp.Doc.SetProperty("requirePassword", true);
                    return cp.core.addon.execute(addonGuidLoginPage, new CPUtilsBaseClass.addonExecuteContext {
                        errorContextMessage = "get Login Page for Admin Site",
                        addonType = CPUtilsBaseClass.addonContext.ContextPage
                    });
                }
                if (!cp.core.session.isAuthenticatedContentManager()) {
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
                    return HtmlController.div(result, "container-fluid ccBodyAdmin ccCon");
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
                // -- get layout
                string layout = cp.Layout.GetLayout(Constants.guidLayoutAdminSite, "Admin Site Layout", @"adminsite\AdminSiteLayout.html");
                if (string.IsNullOrEmpty(layout)) { layout = Processor.Properties.Resources.AdminSiteLayoutBackup; };
                //
                // -- get static data
                // todo -- move this to the dataViewModel to create content during render
                AdminSiteDataModel data = new AdminSiteDataModel(cp) {
                    adminExceptions = cp.core.session.user.developer ? ErrorController.getDocExceptionHtmlList(cp.core) : "",
                    leftSideMessage = cp.Site.GetText("AdminHeaderHTML", "Administration Site"),
                    navBrand = cp.Site.Name,
                    rightSideMessage = cp.Html5.A(cp.User.Name, new CPBase.BaseModels.HtmlAttributesA() {
                        href = "?af=4&cid=" + cp.Content.GetID("people") + "&id=" + cp.User.Id
                    }),
                    rightSideNavHtml = ""
                        + "<form class=\"form-inline\" method=post action=\"?method=logout\">"
                        + "<button class=\"btn btn-warning btn-sm ml-2\" type=\"submit\">Logout</button>"
                        + "</form>",
                    adminNav = cp.core.addon.execute(DbBaseModel.create<AddonModel>(cp, AdminNavigatorGuid), new CPUtilsBaseClass.addonExecuteContext {
                        addonType = CPUtilsBaseClass.addonContext.ContextAdmin,
                        errorContextMessage = "executing Admin Navigator in Admin"
                    })
                };
                return cp.Mustache.render(layout, data);
            } catch (Exception) {
                throw;
            }
        }
    }
    //
    // -- admin site data model
    internal class AdminSiteDataModel {
        /// <summary>
        /// callback added during construct
        /// </summary>
        private readonly CPClass cp;
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="cp"></param>
        public AdminSiteDataModel(CPClass cp) {
            this.cp = cp;
        }
        /// <summary>
        /// exceptions to display at top of admin site
        /// </summary>
        public string adminExceptions { get; set; }
        /// <summary>
        /// navigator
        /// </summary>
        public string adminNav { get; set; }
        /// <summary>
        /// content
        /// </summary>
        public string adminContent { 
            get {
                return AdminContentController.getAdminContent(cp);
            }
        }
        /// <summary>
        /// footer
        /// </summary>
        public string adminFooter { get; set; }
        /// <summary>
        /// header left side (typically "administration site")
        /// </summary>
        public string leftSideMessage { get; set; }
        /// <summary>
        /// header brand (typicall the site name)
        /// </summary>
        public string navBrand { get; set; }
        /// <summary>
        /// header text on the right side to the left, typically user name 
        /// </summary>
        public string rightSideMessage { get; set; }
        /// <summary>
        /// header nav, typcially login/logout buttons
        /// </summary>
        public string rightSideNavHtml { get; set; }
    }
}
