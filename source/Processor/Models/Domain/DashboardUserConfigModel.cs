﻿using Contensive.BaseClasses;
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
        /// the current list of widgets this user sees on the dashboard
        /// </summary>
        public List<DashboardWidgetUserConfigModel> widgets { get; set; } = [];
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
            //
            // -- fix widgetHtmlId created without js- prefix
            bool needsSave = false;
            foreach (var widget in userConfig.widgets) {
                if (!string.IsNullOrEmpty(widget.widgetHtmlId) && !widget.widgetHtmlId.StartsWith("js-")) {
                    widget.widgetHtmlId = "js-" + widget.widgetHtmlId;
                    needsSave = true;
                }
            }
            if(needsSave) { userConfig.save(cp, portalName); }
            //
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
