using Contensive.BaseClasses;
using Contensive.Processor.Models;
using Contensive.Processor.Models.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Markup;

namespace Contensive.Processor.Addons.WidgetDashboardWidgets {
    public class SampleBarChartWidget : AddonBaseClass {
        // Fix for CA1861: Use static readonly for the array to avoid recreating it repeatedly  
        private static readonly string[] DefaultDataLabels = {
               "Lifetime", "Gold", "Premium", "Silver", "Associate",
               "Part-Time", "Foreign", "Canadian", "Misc", "Other", ""
           };
        private static readonly double[] DefaultDataValueList = [45, 20, 10, 8, 5, 4, 3, 2, 2, 1, 0];

        private static readonly DashboardWidgetBarChartModel_DataSets[] DefaultDataetsValues = {
            new() {
                    backgroundColor = "rgba(255, 99, 132, 0.2)",
                    borderColor = "rgba(255, 99, 132, 1)",
                    borderWidth = 1,
                    label = "",
                    data = [45, 20, 10, 8, 5, 4, 3, 2, 2, 1, 0]
           }
        };

        public override object Execute(CPBaseClass cp) {
            try {
                int segments = cp.Doc.GetInteger("widgetFilter");
                if(segments==0) { segments = 6; } // default to 2 segments
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
