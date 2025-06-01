using Contensive.BaseClasses;
using Contensive.Processor.Models;
using Contensive.Processor.Models.Domain;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Contensive.Processor.Addons.WidgetDashboardWidgets {
    public class SamplePieChartWidget : AddonBaseClass {
        // Fix for CA1861: Use static readonly for the array to avoid recreating it repeatedly  
        private static readonly string[] DefaultDataLabels = {
               "Lifetime", "Gold", "Premium", "Silver", "Associate",
               "Part-Time", "Foreign", "Canadian", "Misc", "Other", ""
           };

        private static readonly double[] DefaultDataValues = {
               45, 20, 10, 8, 5, 4, 3, 2, 2, 1, 0
           };

        public override object Execute(CPBaseClass cp) {
            try {
                //
                // -- read in id passed from widgetcontroller and filter passed from widget ajax.
                string widgetId = cp.Doc.GetText("widgetId");
                int segments = cp.Doc.GetInteger("widgetFilter");
                int savedFilter = cp.User.GetInteger($"SampleLineChartWidget {widgetId} filter");
                if (segments < 4) { segments = 2; } else if (segments < 8) { segments = 6; } else { segments = 10; }
                if (segments != savedFilter) { cp.User.SetProperty($"SampleLineChartWidget {widgetId} filter", segments); }
                //
                DashboardWidgetPieChartModel result = new() {
                    widgetName = "Sample Pie Chart Widget",
                    subhead = "Sample Pie Chart Widget",
                    description = "This is a sample pie chart widget. It is used to demonstrate how to create a pie chart widget.",
                    uniqueId = cp.Utils.GetRandomString(4),
                    width = 1,
                    refreshSeconds = 0,
                    url = "https://www.contensive.com",
                    dataLabels = DefaultDataLabels.Take(segments).ToList(),
                    dataValues = DefaultDataValues.Take(segments).ToList(),
                    widgetType = WidgetTypeEnum.pie,
                    filterOptions = [
                           new() {
                               filterCaption = "2 Segment",
                               filterValue = "2",
                               filterActive = (segments == 2)
                           },
                           new() {
                               filterCaption = "6 Segments",
                               filterValue = "6",
                               filterActive = (segments == 6 )
                           },
                           new() {
                               filterCaption = "10 Segments",
                               filterValue = "10",
                               filterActive = (segments == 10)
                           }
                       ]
                };
                return result;
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                throw;
            }
        }
    }
}
