using Contensive.BaseClasses;
using Contensive.Processor.Controllers;
using Contensive.Processor.Models.Domain;
using Microsoft.Web.Administration;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;
using System.Windows.Media.TextFormatting;

namespace Contensive.Processor.Models.View {
    /// <summary>
    /// 
    /// </summary>
    public class DashboardViewModel {
        //
        private CPBaseClass cp;
        /// <summary>
        /// The portalGuid, set in a hidden and used in the ajax cmd
        /// </summary>
        public string portalGuid { get; set; }
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
        public List<DashboardWidgetViewModel> widgets { get; set; }
        // 
        // ====================================================================================================
        /// <summary>
        /// Create the dashboard viewModel from the config file of the user and render the htmlContent.
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public static DashboardViewModel create(CPBaseClass cp, string portalGuid) {
            try {
                string portalName = string.IsNullOrEmpty(portalGuid) ? "Admin Dashboard" : getPortalName(cp, portalGuid);
                //
                // -- render the htmlcontent and return
                DashboardViewModel result = new() {
                    portalGuid = portalGuid,
                    dashboardName = portalName,
                    title = portalName,
                    widgets = [],
                    addWidgetList = []
                };
                DashboardUserConfigModel userConfig = DashboardUserConfigModel.loadUserConfig(cp, portalName);
                bool needsConfigSave = false;
                if (userConfig?.widgets != null && userConfig.widgets.Count > 0) {
                    //
                    // -- verify unique keys
                    foreach (var widget in userConfig.widgets) {
                        if (string.IsNullOrEmpty(widget.widgetHtmlId)) {
                            widget.widgetHtmlId = $"js-{GenericController.getRandomString(6)}";
                            needsConfigSave = true;
                        }
                    }
                    //
                    // -- create an empty hidden widget used to add widgets
                    result.widgets.Add(new DashboardWidgetViewModel());
                    //
                    needsConfigSave = needsConfigSave || !DashboardWidgetRenderController.buildDashboardWidgets(cp, result, userConfig);
                    //
                    if (needsConfigSave) { userConfig.save(cp, portalName); }
                    buildAddWidgetList(cp, portalGuid, result);
                    return result;
                }
                //
                // -- initialize with default widgets
                userConfig = new();
                DashboardViewModel tmp = new();
                buildAddWidgetList(cp, portalGuid, tmp);
                foreach (var widget in tmp.addWidgetList) {
                    userConfig.widgets.Add(new DashboardWidgetUserConfigModel() {
                        widgetHtmlId = $"js-{GenericController.getRandomString(6)}",
                        addonGuid = widget.guid,
                        refreshSeconds = 0
                    });
                }
                //
                // -- save the new view model before rendering the htmlcontent
                userConfig.save(cp, portalName);
                //
                // -- after save, render the htmlContent and get the widget list
                if (!DashboardWidgetRenderController.buildDashboardWidgets(cp, result, userConfig)) {
                    userConfig.save(cp, portalName);
                }
                result.addWidgetList = tmp.addWidgetList;
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

        private static void buildAddWidgetList(CPBaseClass cp, string portalGuid, DashboardViewModel result) {
            //
            if (cp.Db.IsTableField("ccAggregateFunctions", "dashboardWidget")) {
                result.addWidgetList = [];
                string sql;
                if (string.IsNullOrEmpty(portalGuid)) {
                    sql = $@"
                        select distinct
                            'Admin Dashboard' as portalName,
                            a.name as addonName, a.ccguid as addonGuid
                        from
                            ccAggregateFunctions a  
                        where
                            a.dashboardWidget>0
                        order by 
                            a.name";
                } else {
                    sql = $@"
                        select distinct
                            p.name as portalName,
                            a.name as addonName, a.ccguid as addonGuid
                        from
                            ccPortals p
                            left join ccPortalFeatures f  on p.id=f.portalid
                            left join  ccAggregateFunctions a on f.addonid=a.id 
                        where
                            a.dashboardWidget>0
                            and p.ccguid={cp.Db.EncodeSQLText(portalGuid)}
                        order by 
                            a.name";
                }
                using (DataTable dt = cp.Db.ExecuteQuery(sql)) {
                    if (dt.Rows.Count > 0) {
                        result.title = cp.Utils.EncodeText(dt.Rows[0]["portalName"]);
                        result.dashboardName = result.title;
                        foreach (DataRow row in dt.Rows) {
                            result.addWidgetList.Add(new addWidget() {
                                name = cp.Utils.EncodeText(row["addonName"]),
                                guid = cp.Utils.EncodeText(row["addonGuid"])
                            });
                        }
                    }
                }
            }
        }
        ////
        //// ====================================================================================================
        ///// <summary>
        ///// load config for a user. Returns null if config file not found
        ///// </summary>
        ///// <param name="cp"></param>
        ///// <param name="userId"></param>
        ///// <param name="portalName">unique name of this dash. </param>
        ///// <returns></returns>
        //private static DashboardViewModel loadUserConfig(CPBaseClass cp, string portalName) {
        //    string jsonConfigText = cp.PrivateFiles.Read(getConfigFilename(cp, portalName));
        //    if (string.IsNullOrWhiteSpace(jsonConfigText)) { return null; }
        //    return cp.JSON.Deserialize<DashboardViewModel>(jsonConfigText);
        //}
        //// 
        //// ====================================================================================================
        ///// <summary>
        ///// save config for the current user
        ///// </summary>
        ///// <param name="cp"></param>
        //public void save(CPBaseClass cp, string portalName) {
        //    cp.PrivateFiles.Save(getConfigFilename(cp, portalName), cp.JSON.Serialize(this));
        //}
        ////
        //// ====================================================================================================
        ///// <summary>
        ///// create the config filename for the current user and this dashboard type
        ///// </summary>
        ///// <param name="cp"></param>
        ///// <param name="foldername"></param>
        ///// <returns></returns>
        //private static string getConfigFilename(CPBaseClass cp, string dashboardName) {
        //    string foldername = normalizeDashboardName(dashboardName);
        //    return @$"dashboard\{(string.IsNullOrEmpty(foldername) ? "" : @$"{foldername}\")}config.{cp.User.Id}.json";
        //}
        //// 
        //// ====================================================================================================
        ///// <summary>
        ///// normalize the dashboard name to a valid folder name
        ///// </summary>
        ///// <param name="dashboardName"></param>
        ///// <returns></returns>
        //private static string normalizeDashboardName(string dashboardName) {
        //    string result = Regex.Replace(dashboardName.ToLower(), "[^a-zA-Z0-9]", "");
        //    return result;
        //}
    }
    //
    public class addWidget {
        public string name { get; set; }
        public string guid { get; set; }
    }
}
