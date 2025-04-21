using Contensive.BaseClasses;
using Contensive.Processor.Models;
using Contensive.Processor.Models.Domain;
using System;

namespace Contensive.Processor.Addons.WidgetDashboardWidgets {
    public class SamplePieChartWidget : AddonBaseClass {
        public override object Execute(CPBaseClass cp) {
            try {
                DashboardWidgetPieChartModel result = new() {
                    widgetName = "Sample Pie Chart Widget",
                    subhead = "Sample Pie Chart Widget",
                    description = "This is a sample pie chart widget. It is used to demonstrate how to create a pie chart widget in Contensive.",
                    uniqueId = cp.Utils.GetRandomString(4),
                    width = 1,
                    refreshSeconds = 0,
                    url = "https://www.contensive.com",
                    dataLabels = ["Active", "Inactive"],
                    dataValues = [90, 30],
                    widgetType = WidgetTypeEnum.pie
                };
                return result;
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                throw;
            }
        }
    }
}
