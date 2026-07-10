using Contensive.BaseClasses;
using Contensive.Processor.Models;
using Contensive.Processor.Models.Domain;
using Contensive.Processor.Models.View;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Contensive.Processor.Controllers {
    internal class DashboardWidgetRenderController {
        //
        // ====================================================================================================
        /// <summary>
        /// render the widgets in teh userConfig, and add a blank used with the add button
        /// returns false, there is a problem with a widget in the userConfig and needs to be saved
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="userConfig"></param>
        /// <returns></returns>
        public static bool buildDashboardWidgets(CPBaseClass cp, DashboardViewModel view, DashboardUserConfigModel userConfig) {
            bool configOk = true;
            //
            // -- add a blank widget used for the add button
            view.widgets.Add(buildDashboardWidgetView(cp, new DashboardWidgetUserConfigModel() {
                widgetHtmlId = "newWidgetTemplate"
            }));
            //
            // -- add widgets from the userConfig
            List<DashboardWidgetUserConfigModel> removeWidgets = [];
            foreach (DashboardWidgetUserConfigModel userConfigWidget in userConfig.widgets) {
                //
                // -- skip widgets with no addonGuid (they will be removed below)
                if (string.IsNullOrEmpty(userConfigWidget.addonGuid)) {
                    removeWidgets.Add(userConfigWidget);
                    continue;
                }
                //
                // -- verify the addon exists in the database
                if (!addonExists(cp, userConfigWidget.addonGuid)) {
                    removeWidgets.Add(userConfigWidget);
                    continue;
                }
                //
                // -- for portal dashboards, verify the portal feature linking this addon still exists
                if (!string.IsNullOrEmpty(view.portalGuid) && !portalFeatureExists(cp, view.portalGuid, userConfigWidget.addonGuid)) {
                    removeWidgets.Add(userConfigWidget);
                    continue;
                }
                //
                DashboardWidgetViewModel widget = buildDashboardWidgetView(cp, userConfigWidget);
                if (!string.IsNullOrEmpty(widget.htmlContent)) {
                    //
                    // -- add to output only if the widget has content
                    view.widgets.Add(widget);
                } else {
                    //
                    // -- remove the widget from the config if it has no content
                    removeWidgets.Add(userConfigWidget);
                }
            }
            foreach (var removeWidget in removeWidgets) {
                userConfig.widgets.Remove(removeWidget);
                configOk = false;
            }
            return configOk;
        }
        //
        // ====================================================================================================
        /// <summary>
        /// render the the htmlContent property for the widget
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="userConfigWidget"></param>
        /// <returns></returns>
        public static DashboardWidgetViewModel buildDashboardWidgetView(CPBaseClass cp, DashboardWidgetUserConfigModel userConfigWidget) {
            DashboardWidgetViewModel result = new();
            try {
                //
                // -- create the widget view model
                string widgetAddonResultJson = "";
                //
                // -- if addonGuid not valid, default to htmlContent with no content. Used for add-template
                int widgetType = 0;
                if (userConfigWidget.widgetHtmlId == "newWidgetTemplate") {
                    //
                    // -- add widget case, create an htmlContent widtget with empty content
                    widgetType = (int)WidgetTypeEnum.htmlContent;
                    widgetAddonResultJson = cp.JSON.Serialize(new DashboardWidgetHtmlModel() {
                        widgetHtmlId = userConfigWidget.widgetHtmlId,
                        widgetName = "Add Widget",
                        width = 1,
                        refreshSeconds = 0,
                        widgetSmall = true,
                        filterOptions = new List<DashboardWidgetBaseModel_FilterOptions>(),
                        htmlContent = "",
                        isNewWidgetTemplate = true,
                        url = "",
                        widgetType = WidgetTypeEnum.htmlContent
                    });
                } else if (string.IsNullOrEmpty(userConfigWidget.addonGuid)) {
                    //
                    // -- empty guid
                    return result;
                } else {
                    //
                    // -- execute the widget addon and populate the result from the addon
                    // -- the result is a json string that is deserialized into the WidgetBaseModel
                    cp.Doc.SetProperty("widgetFilter", userConfigWidget.filterValue);
                    cp.Doc.SetProperty("widgetId", userConfigWidget.widgetHtmlId);
                    //
                    // -- pass named filter values as widgetFilter_{filterName} for multi-filter widgets
                    if (userConfigWidget.filterValues != null) {
                        foreach (var kvp in userConfigWidget.filterValues) {
                            cp.Doc.SetProperty($"widgetFilter_{kvp.Key}", kvp.Value);
                        }
                    }
                    widgetAddonResultJson = cp.Addon.Execute(userConfigWidget.addonGuid);
                    if (string.IsNullOrEmpty(widgetAddonResultJson)) { return result; }
                    var addonResultJObj = Newtonsoft.Json.Linq.JObject.Parse(widgetAddonResultJson);
                    widgetType = (int)addonResultJObj["widgetType"];
                }
                try {
                    //
                    // -- populate the type-dependent properties
                    if (widgetType == (int)WidgetTypeEnum.htmlContent) {
                        //
                        // -- html content provided by the addon
                        DashboardWidgetHtmlModel widgetAddonResult = cp.JSON.Deserialize<DashboardWidgetHtmlModel>(widgetAddonResultJson);
                        widgetAddonResult.widgetHtmlId = userConfigWidget.widgetHtmlId;
                        widgetAddonResult.addonGuid = userConfigWidget.addonGuid;
                        widgetAddonResult.widgetSmall = widgetAddonResult.width < 2;
                        normalizeFilters(widgetAddonResult);
                        var layout = cp.Layout.GetLayout(Constants.dashboardWidgetHtmlContentLayoutGuid, Constants.dashboardWidgetHtmlContentLayoutName, Constants.dashboardWidgetHtmlContentLayoutPathFilename);
                        result.htmlContent = cp.Mustache.Render(layout, widgetAddonResult);
                    } else if (widgetType == (int)WidgetTypeEnum.number) {
                        //
                        // -- number widget
                        DashboardWidgetNumberModel widgetAddonResult = cp.JSON.Deserialize<DashboardWidgetNumberModel>(widgetAddonResultJson);
                        widgetAddonResult.widgetHtmlId = userConfigWidget.widgetHtmlId;
                        widgetAddonResult.addonGuid = userConfigWidget.addonGuid;
                        widgetAddonResult.widgetSmall = widgetAddonResult.width < 2;
                        normalizeFilters(widgetAddonResult);
                        var layout = cp.Layout.GetLayout(Constants.dashboardWidgetNumberLayoutGuid, Constants.dashboardWidgetNumberLayoutName, Constants.dashboardWidgetNumberLayoutPathFilename);
                        result.htmlContent = cp.Mustache.Render(layout, widgetAddonResult);
                    } else if (widgetType == (int)WidgetTypeEnum.pie) {
                        //
                        // -- pie widget
                        DashboardWidgetPieChartModel widgetAddonResult = cp.JSON.Deserialize<DashboardWidgetPieChartModel>(widgetAddonResultJson);
                        widgetAddonResult.widgetHtmlId = userConfigWidget.widgetHtmlId;
                        widgetAddonResult.addonGuid = userConfigWidget.addonGuid;
                        widgetAddonResult.widgetSmall = widgetAddonResult.width < 2;
                        normalizeFilters(widgetAddonResult);
                        var layout = cp.Layout.GetLayout(Constants.dashboardWidgetPieChartLayoutGuid, Constants.dashboardWidgetPieChartLayoutName, Constants.dashboardWidgetPieChartLayoutPathFilename);
                        result.htmlContent = cp.Mustache.Render(layout, widgetAddonResult);
                    } else if (widgetType == (int)WidgetTypeEnum.bar) {
                        //
                        // -- bar widget
                        DashboardWidgetBarChartModel widgetAddonResult = cp.JSON.Deserialize<DashboardWidgetBarChartModel>(widgetAddonResultJson);
                        widgetAddonResult.widgetHtmlId = userConfigWidget.widgetHtmlId;
                        widgetAddonResult.addonGuid = userConfigWidget.addonGuid;
                        widgetAddonResult.widgetSmall = widgetAddonResult.width < 2;
                        normalizeFilters(widgetAddonResult);
                        var layout = cp.Layout.GetLayout(Constants.dashboardWidgetBarChartLayoutGuid, Constants.dashboardWidgetBarChartLayoutName, Constants.dashboardWidgetBarChartLayoutPathFilename);
                        result.htmlContent = cp.Mustache.Render(layout, widgetAddonResult);
                    } else if (widgetType == (int)WidgetTypeEnum.line) {
                        //
                        // -- line widget
                        DashboardWidgetLineChartModel widgetAddonResult = cp.JSON.Deserialize<DashboardWidgetLineChartModel>(widgetAddonResultJson);
                        widgetAddonResult.widgetHtmlId = userConfigWidget.widgetHtmlId;
                        widgetAddonResult.addonGuid = userConfigWidget.addonGuid;
                        widgetAddonResult.widgetSmall = widgetAddonResult.width < 2;
                        normalizeFilters(widgetAddonResult);
                        var layout = cp.Layout.GetLayout(Constants.dashboardWidgetLineChartLayoutGuid, Constants.dashboardWidgetLineChartLayoutName, Constants.dashboardWidgetLineChartLayoutPathFilename);
                        result.htmlContent = cp.Mustache.Render(layout, widgetAddonResult);
                    } else {
                        //
                        // -- future growth
                        result.htmlContent = "";
                    }
                    return result;
                } catch (Exception) {
                    cp.Site.ErrorReport($"Error deserializing widget data for widget {userConfigWidget.addonGuid}");
                    return result;
                }
            } catch (Exception ex0) {
                cp.Site.ErrorReport(ex0, $"Error in buildDashboardWidgetView");
                return result;
            }
        }
        //
        // ====================================================================================================
        /// <summary>
        /// Normalize filters on a widget model so that the Mustache templates can always use the filters list.
        /// If the addon returned filterOptions but no filters, auto-promote filterOptions into a single filter group.
        /// Also propagates filterName from each group down to its individual options for use in data-filtername attributes.
        /// </summary>
        private static void normalizeFilters(DashboardWidgetBaseModel model) {
            if ((model.filters == null || model.filters.Count == 0) && model.filterOptions != null && model.filterOptions.Count > 0) {
                //
                // -- legacy single-filter: wrap filterOptions into a default filter group
                foreach (var option in model.filterOptions) {
                    option.filterName = "default";
                }
                model.filters = [
                    new DashboardWidgetFilterGroup {
                        filterName = "default",
                        filterLabel = "Filter",
                        options = model.filterOptions
                    }
                ];
            }
            //
            // -- propagate filterName from group to each option (for Mustache data-filtername attribute)
            // -- if filterLabel is blank, fall back to filterName
            if (model.filters != null) {
                foreach (var group in model.filters) {
                    if (string.IsNullOrEmpty(group.filterLabel)) {
                        group.filterLabel = group.filterName;
                    }
                    if (group.options == null) { continue; }
                    foreach (var option in group.options) {
                        option.filterName = group.filterName;
                    }
                }
            }
        }
        //
        // ====================================================================================================
        /// <summary>
        /// Return true if an addon with the given guid exists in the database.
        /// </summary>
        private static bool addonExists(CPBaseClass cp, string addonGuid) {
            using var dt = cp.Db.ExecuteQuery($"select top 1 id from ccAggregateFunctions where ccguid={cp.Db.EncodeSQLText(addonGuid)}");
            return dt?.Rows.Count > 0;
        }
        //
        // ====================================================================================================
        /// <summary>
        /// Return true if a portal feature exists linking the portal to an addon with the given guid.
        /// </summary>
        private static bool portalFeatureExists(CPBaseClass cp, string portalGuid, string addonGuid) {
            string sql = $@"
                select top 1 f.id
                from ccPortalFeatures f
                inner join ccPortals p on p.id = f.portalid
                inner join ccAggregateFunctions a on a.id = f.addonid
                where p.ccguid = {cp.Db.EncodeSQLText(portalGuid)}
                and a.ccguid = {cp.Db.EncodeSQLText(addonGuid)}";
            using var dt = cp.Db.ExecuteQuery(sql);
            return dt?.Rows.Count > 0;
        }
    }
}
