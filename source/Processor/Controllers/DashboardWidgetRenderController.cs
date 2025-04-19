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
        /// render the htmlContent property for all the addons in the view model
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="userConfig"></param>
        /// <returns></returns>
        public static DashboardViewModel renderWidgets(CPBaseClass cp, DashboardViewModel view, DashboardUserConfigModel userConfig) {
            view.widgets = [];
            foreach (DashboardWidgetUserConfigModel userConfigWidget in userConfig.widgets) {
                view.widgets.Add(renderWidget(cp, userConfigWidget));
            }
            return view;
        }
        //
        // ====================================================================================================
        /// <summary>
        /// render the the htmlContent property for the widget
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="userConfigWidget"></param>
        /// <returns></returns>
        public static DashboardWidgetViewModel renderWidget(CPBaseClass cp, DashboardWidgetUserConfigModel userConfigWidget) {
            DashboardWidgetViewModel result = new() {
                widgetName = userConfigWidget.widgetName,
                url = userConfigWidget.remove_url,
                key = userConfigWidget.key,
                x = userConfigWidget.x,
                y = userConfigWidget.y,
                width = userConfigWidget.width,
                height = userConfigWidget.height,
                refreshSeconds = userConfigWidget.refreshSeconds,
                addonGuid = userConfigWidget.addonGuid
            };
            if (string.IsNullOrWhiteSpace(userConfigWidget.addonGuid)) { return result; }
            //
            // -- execute the widget addon, the result is a json string that is deserialized into the WidgetBaseModel
            string widgetAddonResultJson = cp.Addon.Execute(userConfigWidget.addonGuid);
            if (string.IsNullOrEmpty(widgetAddonResultJson)) { return result; }
            //
            // -- apply the addon result to the widget
            DashboardWidgetBaseModel addonResult = null;
            try {
                addonResult = cp.JSON.Deserialize<DashboardWidgetBaseModel>(widgetAddonResultJson);
                //
                // -- populate the type-indpendent properties
                result.width = result.width > addonResult.minWidth ? result.width : addonResult.minWidth;
                result.height = result.height > addonResult.minHeight ? result.height : addonResult.minHeight;
                result.refreshSeconds = addonResult.refreshSeconds;
                result.widgetName = addonResult.widgetName;
                result.url = addonResult.url;
                //
                // -- populate the type-dependent properties
                if (addonResult.widgetType == WidgetTypeEnum.htmlContent) {
                    //
                    // -- html content provided by the addon
                    DashboardWidgetHtmlModel widgetData = cp.JSON.Deserialize<DashboardWidgetHtmlModel>(widgetAddonResultJson);
                    var layout = cp.Layout.GetLayout(Constants.dashboardWidgetHtmlContentLayoutGuid, Constants.dashboardWidgetHtmlContentLayoutName, Constants.dashboardWidgetHtmlContentLayoutPathFilename);
                    result.htmlContent = cp.Mustache.Render(layout, widgetData);
                } else if (addonResult.widgetType == WidgetTypeEnum.number) {
                    //
                    // -- simple number widget
                    DashboardWidgetNumberModel widgetData = cp.JSON.Deserialize<DashboardWidgetNumberModel>(widgetAddonResultJson);
                    var layout = cp.Layout.GetLayout(Constants.dashboardWidgetNumberLayoutGuid, Constants.dashboardWidgetNumberLayoutName, Constants.dashboardWidgetNumberLayoutPathFilename);
                    result.htmlContent = cp.Mustache.Render(layout, widgetData);
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
