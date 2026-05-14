
using System;
using Contensive.Processor.Controllers;
using static Contensive.Processor.Constants;
using Contensive.Models.Db;
using NLog;

namespace Contensive.Processor.Addons.AdminSite {
    //
    //========================================================================
    /// <summary>
    /// Root page view
    /// </summary>
    public static class RootView {
        //
        // static logger
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        //
        //========================================================================
        /// <summary>
        /// Root page view
        /// </summary>
        /// <param name="core"></param>
        /// <returns></returns>
        public static string getForm_Root(CoreController core) {
            string returnHtml = "";
            try {
                int addonId = 0;
                //
                // -- select dashboard based on per-user preference (default to widget dashboard)
                //
                bool useIconDashboard = core.userProperty.getBoolean("Admin Nav Icon Dashboard", false);
                string dashboardGuid = useIconDashboard ? addonGuidIconDashboard : addonGuidWidgetDashboard;
                var dashboardAddon = DbBaseModel.create<AddonModel>(core.cpParent, dashboardGuid);
                addonId = dashboardAddon?.id ?? 0;
                if (addonId != 0) {
                    //
                    // Display the Addon
                    //
                    if (!core.doc.userErrorList.Count.Equals(0)) {
                        returnHtml = returnHtml + "<div style=\"clear:both;margin-top:20px;\">&nbsp;</div>"
                        + "<div style=\"clear:both;margin-top:20px;\">" + Processor.Controllers.ErrorController.getUserError(core) + "</div>";
                    }
                    returnHtml += core.addon.execute(core.cacheRuntime.addonCache.create(addonId), new BaseClasses.CPUtilsBaseClass.addonExecuteContext {
                        addonType = BaseClasses.CPUtilsBaseClass.addonContext.ContextAdmin,
                        errorContextMessage = "executing addon id:" + addonId + " set as Admin Root addon"
                    });
                }
                if (string.IsNullOrEmpty(returnHtml)) {
                    //
                    // Nothing Displayed, show default root page
                    //
                    returnHtml = returnHtml + Environment.NewLine + "<div style=\"padding:20px;height:450px\">"
                    + Environment.NewLine + "<div><a href=http://www.Contensive.com target=_blank><strong>Contensive/\" + CoreController.codeVersion() + \"</strong></A></div>"
                    + Environment.NewLine + "<div style=\"clear:both;height:18px;margin-top:10px\"><div style=\"float:left;width:200px;\">Domain Name</div><div style=\"float:left;\">" + core.webServer.requestDomain + "</div></div>"
                    + Environment.NewLine + "<div style=\"clear:both;height:18px;\"><div style=\"float:left;width:200px;\">Login Member Name</div><div style=\"float:left;\">" + core.session.user.name + "</div></div>"
                    + Environment.NewLine + "<div style=\"clear:both;height:18px;\"><div style=\"float:left;width:200px;\">Quick Reports</div><div style=\"float:left;\"><a Href=\"?addonGuid={A5439430-ED28-4D72-A9ED-50FB36145955}\">Real-Time Activity</A></div></div>"
                    + Environment.NewLine + "<div style=\"clear:both;height:18px;\"><div style=\"float:left;width:200px;\"><a Href=\"?" + RequestNameDashboardReset + "=" + core.session.visit.id + "\">Run Dashboard</A></div></div>"
                    + Environment.NewLine + "<div style=\"clear:both;height:18px;\"><div style=\"float:left;width:200px;\"><a Href=\"?addonguid=" + addonGuidAddonManager + "\">Add-on Manager</A></div></div>";
                    //
                    if (!core.doc.userErrorList.Count.Equals(0)) {
                        returnHtml = returnHtml + "<div style=\"clear:both;margin-top:20px;\">&nbsp;</div>"
                        + "<div style=\"clear:both;margin-top:20px;\">" + Processor.Controllers.ErrorController.getUserError(core) + "</div>";
                    }
                    //
                    returnHtml = returnHtml + Environment.NewLine + "</div>"
                    + "";
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }
            return returnHtml;
        }
        //
    }
}
