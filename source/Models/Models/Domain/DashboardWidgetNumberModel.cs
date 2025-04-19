namespace Contensive.Processor.Models {
    /// <summary>
    /// This dash widget simple returns html for the content
    /// </summary>
    public class DashboardWidgetNumberModel : DashboardWidgetBaseModel {
        public DashboardWidgetNumberModel() : base() {
            widgetType = WidgetTypeEnum.number;
        }
        public string number { get; set; }
        public string subhead { get; set; }
        public string description { get; set; }

    }
}
