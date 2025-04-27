using Contensive.BaseClasses;
using Contensive.Processor.Models;
using Contensive.Processor.Models.Domain;
using System;
using System.Collections.Generic;

namespace Contensive.Processor.Addons.WidgetDashboardWidgets {
    public class SamplePieChartWidget : AddonBaseClass {
        public override object Execute(CPBaseClass cp) {
            try {
                int segments = cp.Doc.GetInteger("widgetFilter");
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
                    widgetType = WidgetTypeEnum.pie,
                    filterOptions = new List<DashboardWidgetBaseModel_FilterOptions>() {
                        new DashboardWidgetBaseModel_FilterOptions() {
                            filterCaption = "1 Segment",
                            filterValue = "1",
                            filterActive = (segments == 1)
                        },
                        new DashboardWidgetBaseModel_FilterOptions() {
                            filterCaption = "2 Segments",
                            filterValue = "2",
                            filterActive = (segments == 2)
                        },
                        new DashboardWidgetBaseModel_FilterOptions() {
                            filterCaption = "10 Segments",
                            filterValue = "10",
                            filterActive = (segments == 10)
                        }
                    }
                };
                return result;
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                throw;
            }
        }
    }
}
