using Contensive.BaseClasses;
using Contensive.Processor.Models;
using Contensive.Processor.Models.Domain;
using Contensive.Processor.Models.View;
using System;

namespace Contensive.Processor.Controllers {
    internal class DashboardWidgetRenderController {
        //
        // ====================================================================================================
        /// <summary>
        /// if false, a widget in the userConfig is not valid and has been removed. The config has to be saved
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="userConfig"></param>
        /// <returns></returns>
        public static bool buildDashboardWidgets(CPBaseClass cp, DashboardViewModel view, DashboardUserConfigModel userConfig) {
            foreach (DashboardWidgetUserConfigModel userConfigWidget in userConfig.widgets) {
                DashboardWidgetViewModel widget = buildDashboardWidgetView(cp, userConfigWidget);
                if (!string.IsNullOrEmpty(widget.htmlContent)) {
                    // -- add to output only if the widget has content
                    view.widgets.Add(widget);
                    return true;
                }
            }
            return false;
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
            ////
            //// -- repair key if missing
            //if (string.IsNullOrEmpty(userConfigWidget.widgetHtmlId)) {
            //    userConfigWidget.widgetHtmlId = GenericController.getRandomString(6);
            //}
            //
            // -- create the widget view model
            DashboardWidgetViewModel result = new() {
                widgetHtmlId = userConfigWidget.widgetHtmlId,
                addonGuid = userConfigWidget.addonGuid,
                refreshSeconds = userConfigWidget.refreshSeconds
            };
            if (string.IsNullOrWhiteSpace(userConfigWidget.addonGuid)) { return result; }
            //
            // -- execute the widget addon and populate the result from the addon
            // -- the result is a json string that is deserialized into the WidgetBaseModel
            cp.Doc.SetProperty("widgetFilter", userConfigWidget.filterValue);
            string widgetAddonResultJson = cp.Addon.Execute(userConfigWidget.addonGuid);
            if (string.IsNullOrEmpty(widgetAddonResultJson)) { return result; }
            //
            // -- apply the addon result to the widget
            DashboardWidgetBaseModel addonResult = null;
            try {
                addonResult = cp.JSON.Deserialize<DashboardWidgetBaseModel>(widgetAddonResultJson);
                //
                // -- populate the type-independent properties
                result.refreshSeconds = addonResult.refreshSeconds;
                result.widgetName = addonResult.widgetName;
                result.url = addonResult.url;
                result.widgetSmall = addonResult.width < 2;
                //
                // -- populate filters
                if (addonResult.filterOptions != null && addonResult.filterOptions.Count > 0) {
                    result.hasFilter = true;
                    foreach (var addonFilterOption in addonResult.filterOptions) {
                        result.filterOptions.Add(new DashboardWidgetViewModel_FilterOptions() {
                            filterCaption = addonFilterOption.filterCaption,
                            filterValue = addonFilterOption.filterValue,
                            filterActive = userConfigWidget.filterValue == addonFilterOption.filterValue
                        });
                    };
                };

                //
                // -- populate the type-dependent properties
                if (addonResult.widgetType == WidgetTypeEnum.htmlContent) {
                    //
                    // -- html content provided by the addon
                    DashboardWidgetHtmlModel widgetAddonResult = cp.JSON.Deserialize<DashboardWidgetHtmlModel>(widgetAddonResultJson);
                    var layout = cp.Layout.GetLayout(Constants.dashboardWidgetHtmlContentLayoutGuid, Constants.dashboardWidgetHtmlContentLayoutName, Constants.dashboardWidgetHtmlContentLayoutPathFilename);
                    result.htmlContent = cp.Mustache.Render(layout, widgetAddonResult);
                } else if (addonResult.widgetType == WidgetTypeEnum.number) {
                    //
                    // -- number widget
                    DashboardWidgetNumberModel widgetAddonResult = cp.JSON.Deserialize<DashboardWidgetNumberModel>(widgetAddonResultJson);
                    var layout = cp.Layout.GetLayout(Constants.dashboardWidgetNumberLayoutGuid, Constants.dashboardWidgetNumberLayoutName, Constants.dashboardWidgetNumberLayoutPathFilename);
                    result.htmlContent = cp.Mustache.Render(layout, widgetAddonResult);
                } else if (addonResult.widgetType == WidgetTypeEnum.pie) {
                    //
                    // -- pie widget
                    DashboardWidgetPieChartModel widgetAddonResult = cp.JSON.Deserialize<DashboardWidgetPieChartModel>(widgetAddonResultJson);
                    var layout = cp.Layout.GetLayout(Constants.dashboardWidgetPieChartLayoutGuid, Constants.dashboardWidgetPieChartLayoutName, Constants.dashboardWidgetPieChartLayoutPathFilename);
                    result.htmlContent = cp.Mustache.Render(layout, widgetAddonResult);
                } else if (addonResult.widgetType == WidgetTypeEnum.bar) {
                    //
                    // -- bar widget
                    DashboardWidgetBarChartModel widgetAddonResult = cp.JSON.Deserialize<DashboardWidgetBarChartModel>(widgetAddonResultJson);
                    var layout = cp.Layout.GetLayout(Constants.dashboardWidgetBarChartLayoutGuid, Constants.dashboardWidgetBarChartLayoutName, Constants.dashboardWidgetBarChartLayoutPathFilename);
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
        }
    }
}
