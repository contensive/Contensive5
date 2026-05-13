using Contensive.BaseClasses;
using Contensive.Processor.Models;
using System;
using System.Collections.Generic;
using System.Data;

namespace Contensive.DashboardWidgets {
    public class TopPagesByPageViewsWidget : AddonBaseClass {
        public override object Execute(CPBaseClass cp) {
            try {
                //
                // -- read in id passed from widgetcontroller, and filter passed from widget ajax.
                string widgetId = cp.Doc.GetText("widgetId");
                int filterValue = cp.Doc.GetInteger("widgetFilter");
                int savedFilter = cp.User.GetInteger($"TopPagesByPageViewsWidget {widgetId} filter");
                if (filterValue == 0) { filterValue = savedFilter; }
                if (filterValue == 0) { filterValue = 30; }
                if (filterValue != savedFilter) { cp.User.SetProperty($"TopPagesByPageViewsWidget {widgetId} filter", filterValue); }
                //
                // -- build date filter criteria
                string dateCriteria = "";
                if (filterValue != 9999) {
                    int cutoffDateNumber = (int)DateTime.Now.AddDays(-filterValue).ToOADate();
                    dateCriteria = $" where DateNumber>={cutoffDateNumber}";
                }
                //
                // -- query top 10 pages by total page views
                string sql = $"select top 10 PageTitle, sum(PageViews) as TotalViews from ccViewingSummary{dateCriteria} group by PageTitle having sum(PageViews)>0 order by TotalViews desc";
                //
                var dataLabels = new List<string>();
                var dataValues = new List<double>();
                using (DataTable dt = cp.Db.ExecuteQuery(sql)) {
                    if (dt?.Rows != null) {
                        foreach (DataRow row in dt.Rows) {
                            string pageTitle = cp.Utils.EncodeText(row["PageTitle"]);
                            if (string.IsNullOrWhiteSpace(pageTitle)) { pageTitle = "(untitled)"; }
                            dataLabels.Add(pageTitle);
                            dataValues.Add(cp.Utils.EncodeNumber(row["TotalViews"]));
                        }
                    }
                }
                //
                // -- build filter label
                string filterLabel = filterValue switch {
                    1 => "1 Day",
                    7 => "1 Week",
                    30 => "1 Month",
                    _ => "All Time"
                };
                //
                return new DashboardWidgetBarChartModel() {
                    widgetName = "Top Pages By Page Views",
                    subhead = $"Top Pages ({filterLabel})",
                    description = "Top 10 pages ranked by total page views.",
                    uniqueId = cp.Utils.GetRandomString(4),
                    width = 2,
                    refreshSeconds = 0,
                    dataLabels = dataLabels,
                    dataSets = new List<DashboardWidgetBarChartModel_DataSets>() {
                        new() {
                            label = "Page Views",
                            data = dataValues
                        }
                    },
                    widgetType = WidgetTypeEnum.bar,
                    filterOptions = new List<DashboardWidgetBaseModel_FilterOptions>() {
                        new() {
                            filterCaption = "1 Day",
                            filterValue = "1",
                            filterActive = (filterValue == 1)
                        },
                        new() {
                            filterCaption = "1 Week",
                            filterValue = "7",
                            filterActive = (filterValue == 7)
                        },
                        new() {
                            filterCaption = "1 Month",
                            filterValue = "30",
                            filterActive = (filterValue == 30)
                        },
                        new() {
                            filterCaption = "All Time",
                            filterValue = "9999",
                            filterActive = (filterValue == 9999)
                        }
                    }
                };
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                throw;
            }
        }
    }
}
