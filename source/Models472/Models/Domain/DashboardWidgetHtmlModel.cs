namespace Contensive.Processor.Models {
    /// <summary>
    /// This dash widget simple returns html for the content
    /// </summary>
    public class DashboardWidgetHtmlModel : DashboardWidgetBaseModel {
        public DashboardWidgetHtmlModel() : base() {
            widgetType = WidgetTypeEnum.htmlContent;
        }
        //
        /// <summary>
        /// The html with css and javascript to render the widget
        /// </summary>
        public string htmlContent { get; set; }
    }
}
