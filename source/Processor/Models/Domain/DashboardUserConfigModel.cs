using Contensive.BaseClasses;
using Contensive.Processor.Controllers;
using Microsoft.Web.Administration;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;

namespace Contensive.Processor.Models.Domain {
    public class DashboardUserConfigModel {
        //
        private CPBaseClass cp;
        /// <summary>
        /// the name of the dashboard. This is used to create the folder name for the config file.
        /// It has to be sent to the UI so it can be returned in commands and used to load the config file.
        /// </summary>
        public string dashboardName { get; set; }
        /// <summary>
        /// the title that apears on the dashboard at the top.
        /// </summary>
        public string title { get; set; }
        /// <summary>
        /// the list of widgets that can be added to the dashbaord
        /// </summary>
        public List<addWidget> addWidgetList { get; set; }
        /// <summary>
        /// the current list of widgets this user sees on the dashboard
        /// </summary>
        public List<DashboardWidgetUserConfigModel> widgets { get; set; }
        //// 
        //// ====================================================================================================
        ///// <summary>
        ///// Create the dashboard viewModel from the config file of the user and render the htmlContent.
        ///// </summary>
        ///// <param name="cp"></param>
        ///// <param name="userId"></param>
        ///// <returns></returns>
        //public static DashboardUserConfigModel create(CPBaseClass cp, string portalGuid) {
        //    try {
        //        string portalName = string.IsNullOrEmpty(portalGuid) ? "Admin Dashboard" : getPortalName(cp, portalGuid);
        //        DashboardUserConfigModel userConfig = loadUserConfig(cp, portalName);
        //        DashboardUserConfigModel result = null;
        //        if (userConfig?.widgets != null && userConfig.widgets.Count > 0) {
        //            //
        //            // -- render the htmlcontent and return
        //            result = DashboardWidgetRenderController.renderWidgets(cp, userConfig);
        //            result.dashboardName = portalName;
        //            result.title = portalName;
        //            buildAddWidgetList(cp, portalGuid, result);
        //            return result;
        //        }
        //        //
        //        // -- iniitalize with default widgets
        //        userConfig = new DashboardUserConfigModel() {
        //            widgets = [
        //                new DashboardWidgetUserConfigModel() {
        //                    widgetName = "Sample",
        //                    x=0,
        //                    y=0,
        //                    width = 2,
        //                    height = 2,
        //                    htmlContent = cp.CdnFiles.Read("dashboard\\sampleWidget.html"),
        //                    key="E928",
        //                    remove_url="https://www.contensive.com",
        //                    addonGuid = Constants.sampleDashboardWidgetGuid
        //                },
        //                new DashboardWidgetUserConfigModel() { widgetName="Widget 2",x=2,y=0, width = 2, height = 2, htmlContent = "Content 2", key="6E52", remove_url="https://www.contensive.com" },
        //                new DashboardWidgetUserConfigModel() { widgetName="Widget 3",x=4,y=0, width = 2, height = 2, htmlContent = "Content 3", key="D512", remove_url="https://www.contensive.com" },
        //                new DashboardWidgetUserConfigModel() { widgetName="Widget 4",x=6,y=0, width = 2, height = 2, htmlContent = "Content 4", key="0380", remove_url="https://www.contensive.com" },
        //                new DashboardWidgetUserConfigModel() { widgetName="Widget 5",x=0,y=2, width = 2, height = 2, htmlContent = "Content 5", key="AC55", remove_url="https://www.contensive.com" }
        //            ]
        //        };
        //        //
        //        // -- save the new view model before rendering the htmlcontent
        //        string dashboardName = getPortalName(cp, portalGuid);
        //        userConfig.save(cp, dashboardName);
        //        //
        //        // -- after save, render the htmlContent and get the widget list
        //        result = DashboardWidgetRenderController.renderWidgets(cp, userConfig);
        //        buildAddWidgetList(cp, portalGuid, result);
        //        result.dashboardName = portalName;
        //        result.title = portalName;
        //        return result;
        //    } catch (Exception ex) {
        //        cp.Site.ErrorReport(ex);
        //        throw;
        //    }
        //}

        //private static void buildAddWidgetList(CPBaseClass cp, string portalGuid, DashboardUserConfigModel result) {
        //    //
        //    if (cp.Db.IsTableField("ccAggregateFunctions", "dashboardWidget")) {
        //        result.addWidgetList = [];
        //        string sql;
        //        if (string.IsNullOrEmpty(portalGuid)) {
        //            sql = $@"
        //                select
        //                    'Admin Dashboard' as portalName,
        //                    a.name as addonName, a.ccguid as addonGuid
        //                from
        //                    ccAggregateFunctions a  
        //                where
        //                    a.dashboardWidget>0
        //                order by 
        //                    a.name";
        //        } else {
        //            sql = $@"
        //                select
        //                    p.name as portalName,
        //                    a.name as addonName, a.ccguid as addonGuid
        //                from
        //                    ccPortals p
        //                    left join ccPortalFeatures f  on p.id=f.portalid
        //                    left join  ccAggregateFunctions a on f.addonid=a.id 
        //                where
        //                    a.dashboardWidget>0
        //                    and p.ccguid={cp.Db.EncodeSQLText(portalGuid)}
        //                order by 
        //                    a.name";
        //        }
        //        using (DataTable dt = cp.Db.ExecuteQuery(sql)) {
        //            if (dt.Rows.Count > 0) {
        //                result.title = cp.Utils.EncodeText(dt.Rows[0]["portalName"]);
        //                result.dashboardName = result.title;
        //                foreach (DataRow row in dt.Rows) {
        //                    result.addWidgetList.Add(new addWidget() {
        //                        name = cp.Utils.EncodeText(row["addonName"]),
        //                        guid = cp.Utils.EncodeText(row["addonGuid"])
        //                    });
        //                }
        //            }
        //        }
        //    }
        //}
        //
        // ====================================================================================================
        /// <summary>
        /// load config for a user. Returns null if config file not found
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="userId"></param>
        /// <param name="portalName">unique name of this dash. </param>
        /// <returns></returns>
        public static DashboardUserConfigModel loadUserConfig(CPBaseClass cp, string portalName) {
            string jsonConfigText = cp.PrivateFiles.Read(getConfigFilename(cp, portalName));
            if (string.IsNullOrWhiteSpace(jsonConfigText)) { return null; }
            var userConfig = cp.JSON.Deserialize<DashboardUserConfigModel>(jsonConfigText);
            userConfig.widgets = userConfig.widgets.OrderBy((x) => x.sort).ToList();
            return userConfig;
        }
        // 
        // ====================================================================================================
        /// <summary>
        /// save config for the current user
        /// </summary>
        /// <param name="cp"></param>
        public void save(CPBaseClass cp, string portalName) {
            cp.PrivateFiles.Save(getConfigFilename(cp, portalName), cp.JSON.Serialize(this));
        }
        //
        // ====================================================================================================
        /// <summary>
        /// create the config filename for the current user and this dashboard type
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="foldername"></param>
        /// <returns></returns>
        private static string getConfigFilename(CPBaseClass cp, string dashboardName) {
            string foldername = normalizeDashboardName(dashboardName);
            return @$"dashboard\{(string.IsNullOrEmpty(foldername) ? "" : @$"{foldername}\")}config.{cp.User.Id}.json";
        }
        // 
        // ====================================================================================================
        /// <summary>
        /// normalize the dashboard name to a valid folder name
        /// </summary>
        /// <param name="dashboardName"></param>
        /// <returns></returns>
        private static string normalizeDashboardName(string dashboardName) {
            string result = Regex.Replace(dashboardName.ToLower(), "[^a-zA-Z0-9]", "");
            return result;
        }
    }
    //
    public class addWidget {
        public string name { get; set; }
        public string guid { get; set; }
    }
}
