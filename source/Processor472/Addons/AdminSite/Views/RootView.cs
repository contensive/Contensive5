
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
                StringBuilderLegacyController Stream = new StringBuilderLegacyController();
                int addonId = 0;
                string AddonIDText = null;
                //
                // This is really messy -- there must be a better way
                //
                addonId = 0;
                if (core.session.visit.id == core.docProperties.getInteger(RequestNameDashboardReset)) {
                    //$$$$$ cache this
                    using (var csData = new CsModel(core)) {
                        csData.open(AddonModel.tableMetadata.contentName, "ccguid=" + DbController.encodeSQLText(addonGuidDashboard));
                        if (csData.ok()) {
                            addonId = csData.getInteger("id");
                            core.siteProperties.setProperty("AdminRootAddonID", GenericController.encodeText(addonId));
                        }
                    }
                }
                if (addonId == 0) {
                    //
                    // Get AdminRootAddon
                    //
                    AddonIDText = core.siteProperties.getText("AdminRootAddonID", "");
                    if (string.IsNullOrEmpty(AddonIDText)) {
                        //
                        // the desktop is likely unset, auto set it to dashboard
                        //
                        addonId = -1;
                    } else if (AddonIDText == "0") {
                        //
                        // the desktop has been set to none - go with default desktop
                        //
                        addonId = 0;
                    } else if (AddonIDText.isNumeric()) {
                        //
                        // it has been set to a non-zero number
                        //
                        addonId = GenericController.encodeInteger(AddonIDText);
                        //
                        // Verify it so there is no error when it runs
                        if (core.cacheRuntime.addonCache.create(addonId) == null) {
                            addonId = -1;
                            core.siteProperties.setProperty("AdminRootAddonID", "");
                        }
                    }
                    if (addonId == -1) {
                        //
                        // This has never been set, try to get the dashboard ID
                        var addon = core.cacheRuntime.addonCache.create(addonGuidDashboard);
                        if (addon != null) {
                            addonId = addon.id;
                            core.siteProperties.setProperty("AdminRootAddonID", addonId);
                        }
                    }
                }
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
                    + Environment.NewLine + "<div><a href=http://www.Contensive.com target=_blank><img style=\"border:1px solid #000;\" src=\"" + cdnPrefix + "images/ContensiveAdminLogo.GIF\" border=0 ></A></div>"
                    + Environment.NewLine + "<div><strong>Contensive/" + CoreController.codeVersion() + "</strong></div>"
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
