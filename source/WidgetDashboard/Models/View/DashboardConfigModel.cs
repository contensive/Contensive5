using Contensive.BaseClasses;
using Contensive.WidgetDashboard.Controllers;
using Contensive.WidgetDashboard.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;

namespace Contensive.WidgetDashboard.Models {
    public class DashboardConfigModel {
        //
        private CPBaseClass cp;
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
        public List<DashboardConfigWidgetModel> widgets { get; set; }
        // 
        // ====================================================================================================
        /// <summary>
        /// Create the dashboard viewModel from the config file of the user and render the htmlContent.
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public static DashboardConfigModel create(CPBaseClass cp, string dashboardName) {
            try {
                DashboardConfigModel config = loadUsersConfig(cp, dashboardName);
                if (config?.widgets != null && config.widgets.Count > 0) {
                    //
                    // -- render the htmlcontent and return
                    DashboardConfigModel result = WidgetRenderController.renderWidgets(cp, config);
                    buildAddWidgetList(cp, result);
                    return result;
                }
                //
                // -- iniitalize with default widgets
                config = new DashboardConfigModel() {
                    widgets = [
                        new DashboardConfigWidgetModel() {
                            x=0,
                            y=0,
                            width = 2,
                            height = 2,
                            htmlContent = cp.CdnFiles.Read("dashboard\\sampleWidget.html"),
                            key="E928",
                            link="https://www.contensive.com",
                            addonGuid = Constants.sampleDashboardWidgetGuid
                        },
                        new DashboardConfigWidgetModel() { x=2,y=0, width = 2, height = 2, htmlContent = "Widget 2", key="6E52", link="https://www.contensive.com" },
                        new DashboardConfigWidgetModel() { x=4,y=0, width = 1, height = 1, htmlContent = "Widget 3", key="D512", link="https://www.contensive.com" },
                        new DashboardConfigWidgetModel() { x=4,y=1, width = 1, height = 1, htmlContent = "Widget 4", key="0380", link="https://www.contensive.com" },
                        new DashboardConfigWidgetModel() { x=5,y=0, width = 2, height = 2, htmlContent = "Widget 5", key="AC55", link="https://www.contensive.com" }
                    ]
                };
                //
                // -- save the new view model before rendering the htmlcontent
                config.save(cp, dashboardName);
                //
                // -- after save, render the htmlContent and get the widget list
                DashboardConfigModel result2 = WidgetRenderController.renderWidgets(cp, config);
                buildAddWidgetList(cp, result2);
                return result2;
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                throw;
            }
        }

        private static void buildAddWidgetList(CPBaseClass cp, DashboardConfigModel result) {
            //
            if (cp.Db.IsTableField("ccAggregateFunctions", "dashboardWidget")) {
                result.addWidgetList = new List<addWidget>();
                using (DataTable dt = cp.Db.ExecuteQuery("select name,ccguid from ccAggregateFunctions where dashboardWidget>0 order by name")) {
                    if (dt.Rows.Count > 0) {
                        result.addWidgetList = new List<addWidget>();
                        foreach (DataRow row in dt.Rows) {
                            result.addWidgetList.Add(new addWidget() {
                                name = cp.Utils.EncodeText(row["name"]),
                                guid = cp.Utils.EncodeText(row["ccguid"])
                            });
                        }
                    }
                }
            }
        }

        //
        // ====================================================================================================
        /// <summary>
        /// load config for a user. Returns null if config file not found
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="userId"></param>
        /// <param name="dashboardName">unique name of this dash. </param>
        /// <returns></returns>
        private static DashboardConfigModel loadUsersConfig(CPBaseClass cp, string dashboardName) {
            string jsonConfigText = cp.PrivateFiles.Read(getConfigFilename(cp, dashboardName));
            if (string.IsNullOrWhiteSpace(jsonConfigText)) { return null; }
            return cp.JSON.Deserialize<DashboardConfigModel>(jsonConfigText);
        }
        // 
        // ====================================================================================================
        /// <summary>
        /// save config for the current user
        /// </summary>
        /// <param name="cp"></param>
        public void save(CPBaseClass cp, string dashboardName) {
            cp.PrivateFiles.Save(getConfigFilename(cp, dashboardName), cp.JSON.Serialize(this));
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
