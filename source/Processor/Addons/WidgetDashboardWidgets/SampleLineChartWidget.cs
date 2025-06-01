using Contensive.BaseClasses;
using Contensive.Processor.Models;
using Contensive.Processor.Models.Domain;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Contensive.Processor.Addons.WidgetDashboardWidgets {
    public class SampleLineChartWidget : AddonBaseClass {

        private static readonly double[] DefaultDataValues = {
               45, 20, 10, 8, 5, 4, 3, 2, 2, 1, 0
           };

        public override object Execute(CPBaseClass cp) {
            try {
                //
                // -- read in id passed from widgetcontroller and filter passed from widget ajax.
                string widgetId = cp.Doc.GetText("widgetId");
                int lines = cp.Doc.GetInteger("widgetFilter");
                int savedFilter = cp.User.GetInteger($"SampleLineChartWidget {widgetId} filter");
                if (lines < 4) { lines = 2; } else if (lines < 8) { lines = 6; } else { lines = 10; }
                if (lines != savedFilter) { cp.User.SetProperty($"SampleLineChartWidget {widgetId} filter", lines); }
                //
                string[] defaultDataLabels = {
                    "Sun", "Mon", "Tue", "Wed", "Thur", "Fri", "Sat"
                };
                //
                List<DataSet> defaultDataSet = [
                    new DataSet() {
                        label = "Bob",
                        data = [3, 6, 8, 14, 17, 20, 11]
                    },new DataSet() {
                        label = "Jim",
                        data = [2, 5, 7, 9, 18, 16, 13]
                    },new DataSet() {
                        label = "Sally",
                        data = [3, 4, 6, 10, 12, 15, 19]
                    },new DataSet() {
                        label = "Michael",
                        data = [20, 17, 14, 11, 1, 7, 8]
                    },new DataSet() {
                        label = "Emily",
                        data = [5, 3, 16, 9, 18, 13, 2]
                    },new DataSet() {
                        label = "John",
                        data = [19, 7, 4, 12, 1, 10, 15]
                    },new DataSet() {
                        label = "Jessica",
                        data = [20, 6, 11, 8, 17, 3, 14]
                    },new DataSet() {
                        label = "Ashley",
                        data = [ 16, 5, 13, 2, 18, 9, 7]
                    },new DataSet() {
                        label = "James",
                        data = [12, 19, 10, 4, 15, 1, 6]
                    },new DataSet() {
                        label = "Sarah",
                        data = [2, 4, 8, 13, 16, 17, 17]
                    }
                    ];
                //
                DashboardWidgetLineChartModel result = new() {
                    widgetName = "Sample Line Chart Widget",
                    subhead = "Sample Line Chart Widget",
                    description = "This is a sample line chart widget. It is used to demonstrate how to create a line chart widget.",
                    uniqueId = cp.Utils.GetRandomString(4),
                    width = 2,
                    refreshSeconds = 0,
                    url = "https://www.contensive.com",
                    dataLabels = ["Sun", "Mon", "Tue", "Wed", "Thur", "Fri", "Sat"],
                    dataSets = defaultDataSet.Take(lines).ToList(),
                    widgetType = WidgetTypeEnum.line,
                    filterOptions = [
                                   new() {
                               filterCaption = "2 lines",
                               filterValue = "2",
                               filterActive = (lines == 2)
                           },
                           new() {
                               filterCaption = "6 lines",
                               filterValue = "6",
                               filterActive = (lines == 6 )
                           },
                           new() {
                               filterCaption = "10 lines",
                               filterValue = "10",
                               filterActive = (lines == 10)
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
