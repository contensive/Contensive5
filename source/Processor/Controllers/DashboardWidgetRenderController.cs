using Contensive.BaseClasses;
using Contensive.Processor.Models;
using Contensive.Processor.Models.Domain;
using Contensive.Processor.Models.View;
using System;
using System.Collections.Generic;
using System.Windows.Documents;

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
                        var layout = cp.Layout.GetLayout(Constants.dashboardWidgetHtmlContentLayoutGuid, Constants.dashboardWidgetHtmlContentLayoutName, Constants.dashboardWidgetHtmlContentLayoutPathFilename);
                        result.htmlContent = cp.Mustache.Render(layout, widgetAddonResult);
                    } else if (widgetType == (int)WidgetTypeEnum.number) {
                        //
                        // -- number widget
                        DashboardWidgetNumberModel widgetAddonResult = cp.JSON.Deserialize<DashboardWidgetNumberModel>(widgetAddonResultJson);
                        widgetAddonResult.widgetHtmlId = userConfigWidget.widgetHtmlId;
                        widgetAddonResult.addonGuid = userConfigWidget.addonGuid;
                        widgetAddonResult.widgetSmall = widgetAddonResult.width < 2;
                        var layout = cp.Layout.GetLayout(Constants.dashboardWidgetNumberLayoutGuid, Constants.dashboardWidgetNumberLayoutName, Constants.dashboardWidgetNumberLayoutPathFilename);
                        result.htmlContent = cp.Mustache.Render(layout, widgetAddonResult);
                    } else if (widgetType == (int)WidgetTypeEnum.pie) {
                        //
                        // -- pie widget
                        DashboardWidgetPieChartModel widgetAddonResult = cp.JSON.Deserialize<DashboardWidgetPieChartModel>(widgetAddonResultJson);
                        widgetAddonResult.widgetHtmlId = userConfigWidget.widgetHtmlId;
                        widgetAddonResult.addonGuid = userConfigWidget.addonGuid;
                        widgetAddonResult.widgetSmall = widgetAddonResult.width < 2;
                        var layout = cp.Layout.GetLayout(Constants.dashboardWidgetPieChartLayoutGuid, Constants.dashboardWidgetPieChartLayoutName, Constants.dashboardWidgetPieChartLayoutPathFilename);
                        result.htmlContent = cp.Mustache.Render(layout, widgetAddonResult);
                    } else if (widgetType == (int)WidgetTypeEnum.bar) {
                        //
                        // -- bar widget
                        DashboardWidgetBarChartModel widgetAddonResult = cp.JSON.Deserialize<DashboardWidgetBarChartModel>(widgetAddonResultJson);
                        widgetAddonResult.widgetHtmlId = userConfigWidget.widgetHtmlId;
                        widgetAddonResult.addonGuid = userConfigWidget.addonGuid;
                        widgetAddonResult.widgetSmall = widgetAddonResult.width < 2;
                        var layout = cp.Layout.GetLayout(Constants.dashboardWidgetBarChartLayoutGuid, Constants.dashboardWidgetBarChartLayoutName, Constants.dashboardWidgetBarChartLayoutPathFilename);
                        result.htmlContent = cp.Mustache.Render(layout, widgetAddonResult);
                    } else if (widgetType == (int)WidgetTypeEnum.line) {
                        //
                        // -- line widget
                        DashboardWidgetLineChartModel widgetAddonResult = cp.JSON.Deserialize<DashboardWidgetLineChartModel>(widgetAddonResultJson);
                        widgetAddonResult.widgetHtmlId = userConfigWidget.widgetHtmlId;
                        widgetAddonResult.addonGuid = userConfigWidget.addonGuid;
                        widgetAddonResult.widgetSmall = widgetAddonResult.width < 2;
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
    }
}
