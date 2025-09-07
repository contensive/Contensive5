using Contensive.BaseClasses;
using Contensive.Processor.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Contensive.Processor.Addons.WidgetDashboardWidgets {
    public class SampleBarChartWidget : AddonBaseClass {
        /// <summary>
        /// add-on interface
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public override object Execute(CPBaseClass cp) {
            try {
                //
                // -- read in id passed from widgetcontroller, and filter passed from widget ajax.
                string widgetId = cp.Doc.GetText("widgetId");
                int segments = cp.Doc.GetInteger("widgetFilter");
                int savedFilter = cp.User.GetInteger($"SampleBarChartWidget {widgetId} filter");
                if (segments == 0) { segments = savedFilter; }
                if (segments == 0) { segments = 6; }
                if (segments != savedFilter) { cp.User.SetProperty($"SampleBarChartWidget {widgetId} filter", segments); }
                //
                string[] DefaultDataLabels = {
                   "Lifetime", "Gold", "Premium", "Silver", "Associate","Part-Time", "Foreign", "Canadian", "Misc", "Other", ""
                };
                //
                double[] DefaultDataValueList = {
                    45, 20, 10, 8, 5, 4, 3, 2, 2, 1, 0
                };
                //
                DashboardWidgetBarChartModel_DataSets[] DefaultDataSets = {
                    new() {
                            label = "Data Set",
                            data = DefaultDataValueList.Take(segments).ToList()
                   }
                };
                //
                DashboardWidgetBarChartModel result = new() {
                    widgetName = "Sample Bar Chart Widget",
                    subhead = "Sample Bar Chart Widget",
                    description = "This is a sample Bar chart widget. It is used to demonstrate how to create a Bar chart widget.",
                    uniqueId = cp.Utils.GetRandomString(4),
                    width = 2,
                    refreshSeconds = 0,
                    url = "https://www.contensive.com",
                    dataLabels = DefaultDataLabels.Take(segments).ToList(),
                    dataSets = DefaultDataSets.ToList(),
                    widgetType = WidgetTypeEnum.bar,
                    filterOptions = new List<DashboardWidgetBaseModel_FilterOptions>() {
                           new() {
                               filterCaption = "2 Segment",
                               filterValue = "2",
                               filterActive = (segments == 2)
                           },
                           new() {
                               filterCaption = "6 Segments",
                               filterValue = "6",
                               filterActive = (segments == 6)
                           },
                           new() {
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
