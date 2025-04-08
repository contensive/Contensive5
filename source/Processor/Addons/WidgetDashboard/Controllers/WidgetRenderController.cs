using Contensive.BaseClasses;
using Contensive.Processor.Addons.WidgetDashboard.Models;
using System;

namespace Contensive.Processor.Addons.WidgetDashboard.Controllers {
    internal class WidgetRenderController {
        //
        // ====================================================================================================
        /// <summary>
        /// render the htmlContent property for all the addons in the view model
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="viewModel"></param>
        /// <returns></returns>
        public static DashboardConfigModel renderWidgets(CPBaseClass cp, DashboardConfigModel viewModel) {
            DashboardConfigModel result = new() {
                widgets = []
            };
            foreach (var widget in viewModel.widgets) {
                result.widgets.Add(renderWidget(cp, widget));
            }
            return result;
        }
        //
        // ====================================================================================================
        /// <summary>
        /// render the the htmlContent property for the widget
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="widget"></param>
        /// <returns></returns>
        public static DashboardConfigWidgetModel renderWidget(CPBaseClass cp, DashboardConfigWidgetModel widget) {
            if (string.IsNullOrWhiteSpace(widget.addonGuid)) { return widget; }
            //
            string addonWidgetJson = cp.Addon.Execute(widget.addonGuid);
            if (string.IsNullOrEmpty(addonWidgetJson)) { return widget; }
            //
            WidgetBaseModel addonBaseWidget = null;
            try {
                addonBaseWidget = cp.JSON.Deserialize<WidgetBaseModel>(addonWidgetJson);
                widget.width = widget.width > addonBaseWidget.minWidth ? widget.width : addonBaseWidget.minWidth;
                widget.height = widget.height > addonBaseWidget.minHeight ? widget.height : addonBaseWidget.minHeight;
                widget.refreshSeconds = addonBaseWidget.refreshSeconds;
                // Check the type
                if (addonBaseWidget.widgetType == WidgetTypeEnum.htmlContent) {
                    WidgetHtmlContentModel widgetData = cp.JSON.Deserialize<WidgetHtmlContentModel>(addonWidgetJson);
                    var layout = cp.Layout.GetLayout(Constants.dashboardWidgetHtmlContentLayoutGuid, Constants.dashboardWidgetHtmlContentLayoutName, Constants.dashboardWidgetHtmlContentLayoutPathFilename);
                    widget.htmlContent = cp.Mustache.Render(layout, widgetData);
                } else if (addonBaseWidget.widgetType == WidgetTypeEnum.number) {
                    WidgetNumberModel widgetData = cp.JSON.Deserialize<WidgetNumberModel>(addonWidgetJson);
                    var layout = cp.Layout.GetLayout(Constants.dashboardWidgetNumberLayoutGuid, Constants.dashboardWidgetNumberLayoutName, Constants.dashboardWidgetNumberLayoutPathFilename);
                    widget.htmlContent = cp.Mustache.Render(layout, widgetData);
                } else {
                    widget.htmlContent = "";
                }



                //if (addonBaseWidget.widgetType == WidgetTypeEnum.htmlContent) {
                //    //
                //    // --  html content Widget
                //    WidgetHtmlContentModel widgetData = (WidgetHtmlContentModel)addonBaseWidget;
                //    var layout = cp.Layout.GetLayout(Constants.dashboardWidgetHtmlContentLayoutGuid, Constants.dashboardWidgetHtmlContentLayoutName, Constants.dashboardWidgetHtmlContentLayoutPathFilename);
                //    widget.htmlContent= cp.Mustache.Render(layout, widgetData); 
                //} else if (addonBaseWidget.widgetType == WidgetTypeEnum.number) {
                //    //
                //    // -- number Widget
                //    WidgetNumberModel htmlContentWidget = (WidgetNumberModel)addonBaseWidget;
                //    var layout = cp.Layout.GetLayout(Constants.dashboardWidgetNumberLayoutGuid, Constants.dashboardWidgetNumberLayoutName, Constants.dashboardWidgetNumberLayoutPathFilename);
                //    widget.htmlContent = cp.Mustache.Render(layout, htmlContentWidget);
                //} else {
                //    widget.htmlContent = "";
                //}
                return widget;
            } catch (Exception) {
                cp.Site.ErrorReport($"Error deserializing widget data for widget {widget.addonGuid}");
                return widget;
            }
        }
    }
}
