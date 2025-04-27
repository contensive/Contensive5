using Contensive.BaseClasses;
using Contensive.Processor.Controllers;
using Contensive.Processor.Models.Domain;
using System;
using System.Collections.Generic;
//using System.Drawing.Printing;

namespace Contensive.Processor.Addons.WidgetDashboard {
    /// <summary>
    /// Remote Method called from the dashboard with commands
    /// </summary>
    public class WidgetDashboardCmdRemote : AddonBaseClass {
        /// <summary>
        /// addon interface
        /// all commands return a result as type DashboardViewModel, with a widget list that needs to be updated on the UI. 
        /// It may be empty if nothing needs to be updated.
        /// see DashboardViewModel
        /// 
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public override object Execute(CPBaseClass cp) {
            try {
                string requestJson = cp.Request.Body;
                WDS_Request request = cp.JSON.Deserialize<WDS_Request>(requestJson);
                if (request == null) { return ""; }
                //
                string portalName = getPortalName(cp, request.portalGuid);
                DashboardUserConfigModel userDashboardConfig = DashboardUserConfigModel.loadUserConfig(cp, portalName);
                if (userDashboardConfig is null) { return ""; }
                //
                List<WDS_Response> result = [];
                //
                if (request.cmd == "delete") {
                    foreach (WDS_Request_Widget requestWidget in request.widgets) {
                        var userDashboardConfigWidget = userDashboardConfig.widgets.Find(row => row.key == requestWidget.widgetHtmlId);
                        if (userDashboardConfigWidget is null) { continue; }
                        userDashboardConfig.widgets.Remove(userDashboardConfigWidget);
                        continue;
                    }
                }
                //
                if (request.cmd == "refresh") {
                    foreach (WDS_Request_Widget requestWidget in request.widgets) {
                        var userDashboardConfigWidget = userDashboardConfig.widgets.Find(row => row.key == requestWidget.widgetHtmlId);
                        if (userDashboardConfigWidget is null) { continue; }
                        var viewModel = DashboardWidgetRenderController.renderWidget(cp, userDashboardConfigWidget);
                        result.Add(new WDS_Response {
                            widgetHtmlId = requestWidget.widgetHtmlId,
                            htmlContent = viewModel.htmlContent,
                            link = viewModel.url,
                            widgetName = viewModel.widgetName
                        });
                        continue;
                    }
                }
                if (request.cmd == "save") {
                    int sort = -1;
                    foreach (WDS_Request_Widget requestWidget in request.widgets) {
                        sort++;
                        var userDashboardConfigWidget = userDashboardConfig.widgets.Find(row => row.key == requestWidget.widgetHtmlId);
                        if (userDashboardConfigWidget is null) {
                            userDashboardConfigWidget = new DashboardWidgetUserConfigModel {
                                key = requestWidget.widgetHtmlId
                            };
                            userDashboardConfig.widgets.Add(userDashboardConfigWidget);
                        }
                        userDashboardConfigWidget.sort = sort;
                        userDashboardConfigWidget.addonGuid = requestWidget.addonGuid;
                        var renderedWidget = DashboardWidgetRenderController.renderWidget(cp, userDashboardConfigWidget);
                        //result.Add(new WDS_Response {
                        //    widgetHtmlId = requestWidget.widgetHtmlId,
                        //    htmlContent = renderedWidget.htmlContent,
                        //    link = renderedWidget.url,
                        //    widgetName = renderedWidget.widgetName
                        //});
                        continue;
                    }
                }
                userDashboardConfig.save(cp, getPortalName(cp, request.portalGuid));
                return result;
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                throw;
            }
        }
        //
        // ====================================================================================================
        /// <summary>
        /// get portal name from portal guid
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="portalGuid"></param>
        /// <returns></returns>
        public static string getPortalName(CPBaseClass cp, string portalGuid) {
            using var dataTable = cp.Db.ExecuteQuery($"select name from ccPortals where ccguid={cp.Db.EncodeSQLText(portalGuid)}");
            if (dataTable == null) { return ""; }
            if (dataTable.Rows.Count > 0) {
                return cp.Utils.EncodeText(dataTable.Rows[0][0]);
            } else {
                return "Admin Dashboard";
            }
        }
    }
    //
    public class WDS_Request {
        //
        /// <summary>
        /// the command to perform on this widget: save, delete
        /// </summary>
        public string cmd { get; set; }
        /// <summary>
        /// dashboard name passed from addon render execution. needed for save folder name
        /// </summary>
        public string portalGuid { get; set; }
        /// <summary>
        /// list of widgets on the dashboard
        /// </summary>
        public List<WDS_Request_Widget> widgets { get; set; }
    }
    public class WDS_Request_Widget {
        //public int x { get; set; }
        //public int y { get; set; }
        //public int h { get; set; }
        //public int w { get; set; }
        public string widgetHtmlId { get; set; }
        public string addonGuid { get; set; }
    }
    //
    public class WDS_Response {
        //
        public string widgetHtmlId { get; set; }
        //
        public string htmlContent { get; set; }
        //
        public string link { get; set; }
        //
        public string widgetName { get; set; }
    }
}
