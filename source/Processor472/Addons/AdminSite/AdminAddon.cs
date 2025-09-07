
using Contensive.BaseClasses;
using Contensive.Processor.Controllers;
using NLog;
using System;
using static Contensive.Processor.Constants;

namespace Contensive.Processor.Addons.AdminSite {
    //
    //====================================================================================================
    /// <summary>
    /// Admin site addon
    /// </summary>
    public class AdminAddon : AddonBaseClass {
        //
        // static logger
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
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
                logger.Trace($"{cp.core.logCommonMessage},AdminAddon Enter");
                //
                cp.Doc.AddHeadTag("<meta charset=\"UTF-8\">");
                //
                // -- responsive
                cp.Doc.AddHeadTag("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
                //
                // -- block search engines. This should be blocked anyway.
                cp.Doc.AddHeadTag("<meta name=\"robots\" content=\"noindex,nofollow\">");
                //
                // -- disable tool panel for /help pages
                cp.Doc.SetProperty("AllowToolPanel", false);
                //
                // -- authentication
                if (!cp.core.session.isAuthenticated) {
                    //
                    // --- must be authenticated to continue. Force a local login
                    // -- blank response means login process successful, can continue
                    // -- non-blank is the login form, return it
                    string loginResult = AuthWorkflowController.processGetAuthWorkflow(cp.core, false, true);
                    if (!string.IsNullOrEmpty(loginResult)) {
                        cp.User.Logout();
                        cp.core.html.addTitle("Unauthorized Access", "AdminAddon");
                        return loginResult;
                    }
                }
                if (!cp.core.session.isAuthenticatedAdmin()) {
                    //
                    // -- authorization, admin only
                    cp.core.html.addTitle("Unauthorized Access", "AdminAddon");
                    return Properties.Resources.Layout_AccessDenied;
                }
                //
                // -- get layout - first layout record, then layout-file, then layout-resource
                string layout = cp.Layout.GetLayout(layoutAdminSiteGuid, layoutAdminSiteName, layoutAdminSiteCdnPathFilename);
                if (string.IsNullOrEmpty(layout)) { layout = Processor.Properties.Resources.AdminSiteLayoutBackup; }
                return cp.Mustache.Render(layout, new AdminSiteViewModel(cp));
            } catch (Exception ex) {
                logger.Error(ex, $"{cp.core.logCommonMessage}");
                throw;
            }
        }
    }
}
