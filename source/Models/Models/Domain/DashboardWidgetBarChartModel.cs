using System;
using System.Collections.Generic;
using System.Linq;

namespace Contensive.Processor.Models {
    /// <summary>
    /// This dash widget simple returns html for the content
    /// </summary>
    public class DashboardWidgetBarChartModel : DashboardWidgetBaseModel {
        public DashboardWidgetBarChartModel() : base() {
            widgetType = WidgetTypeEnum.bar;
            uniqueId = getRandomString(4);
        }
        /// <summary>
        /// used to identify the chart in the javascript
        /// </summary>
        public string uniqueId { get; set; }
        /// <summary>
        /// The widget subhead that appears at the top of the widget.
        /// </summary>
        public string subhead { get; set; }
        /// <summary>
        /// The widget description that appears at the top of the widget.
        /// </summary>
        public string description { get; set; }
        /// <summary>
        /// labels for each section of the Pie chart
        /// </summary>
        public List<string> dataLabels { get; set; }
        public List<DashboardWidgetBarChartModel_DataSets> dataValues { get; set; }
        //
        /// <summary>
        /// 
        /// </summary>
        /// <param name="length"></param>
        private static string  getRandomString(int length) {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            Random random = new();
            string randomString = new(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
            return randomString;
        }
    }
    public class DashboardWidgetBarChartModel_DataSets {
        public string label { get; set; }
        public List<double> data { get; set; }
        public string backgroundColor { get; set; }
        public string borderColor { get; set; }
        public int borderWidth { get; set; }
    }
}