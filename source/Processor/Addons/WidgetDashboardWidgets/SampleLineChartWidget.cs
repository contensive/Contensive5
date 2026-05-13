using Contensive.BaseClasses;
using Contensive.Processor.Models;
using System;
using System.Collections.Generic;
using System.Data;

namespace Contensive.Processor.Addons.WidgetDashboardWidgets {
    public class SampleLineChartWidget : AddonBaseClass {

        public override object Execute(CPBaseClass cp) {
            try {
                //
                // -- read in id passed from widgetcontroller and filter passed from widget ajax.
                string widgetId = cp.Doc.GetText("widgetId");
                int days = cp.Doc.GetInteger("widgetFilter");
                int savedFilter = cp.User.GetInteger($"EmailDeliverabilityWidget {widgetId} filter");
                if (days < 45) { days = 30; } else if (days < 75) { days = 60; } else { days = 90; }
                if (days != savedFilter) { cp.User.SetProperty($"EmailDeliverabilityWidget {widgetId} filter", days); }
                //
                // -- query email log grouped by date and logType
                // logType 1=Drop, 6=ImmediateSend (both count as "sent"), 2=Open, 3=Click
                string startDate = cp.Db.EncodeSQLDate(DateTime.Now.AddDays(-days));
                string sql = $"select cast(dateAdded as date) as logDate, logType, count(*) as cnt"
                    + $" from ccemaillog"
                    + $" where dateAdded>={startDate}"
                    + $" and logType in (1,2,3,6)"
                    + $" group by cast(dateAdded as date), logType"
                    + $" order by cast(dateAdded as date)";
                //
                // -- build date-indexed dictionaries for each series
                var sentByDate = new Dictionary<DateTime, double>();
                var openedByDate = new Dictionary<DateTime, double>();
                var clickedByDate = new Dictionary<DateTime, double>();
                //
                // -- initialize all dates in range
                DateTime startDt = DateTime.Now.Date.AddDays(-days + 1);
                DateTime endDt = DateTime.Now.Date;
                for (DateTime dt = startDt; dt <= endDt; dt = dt.AddDays(1)) {
                    sentByDate[dt] = 0;
                    openedByDate[dt] = 0;
                    clickedByDate[dt] = 0;
                }
                //
                // -- populate from query results
                using (DataTable dt = cp.Db.ExecuteQuery(sql)) {
                    if (dt?.Rows != null) {
                        foreach (DataRow row in dt.Rows) {
                            DateTime logDate = cp.Utils.EncodeDate(row["logDate"]).Date;
                            int logType = cp.Utils.EncodeInteger(row["logType"]);
                            double count = cp.Utils.EncodeNumber(row["cnt"]);
                            if (!sentByDate.ContainsKey(logDate)) { continue; }
                            switch (logType) {
                                case 1:
                                case 6:
                                    sentByDate[logDate] += count;
                                    break;
                                case 2:
                                    openedByDate[logDate] = count;
                                    break;
                                case 3:
                                    clickedByDate[logDate] = count;
                                    break;
                            }
                        }
                    }
                }
                //
                // -- build labels and data arrays
                var dataLabels = new List<string>();
                var sentData = new List<double>();
                var openedData = new List<double>();
                var clickedData = new List<double>();
                for (DateTime dt = startDt; dt <= endDt; dt = dt.AddDays(1)) {
                    dataLabels.Add(dt.ToString("M/d"));
                    sentData.Add(sentByDate[dt]);
                    openedData.Add(openedByDate[dt]);
                    clickedData.Add(clickedByDate[dt]);
                }
                //
                DashboardWidgetLineChartModel result = new() {
                    widgetName = "Email Deliverability",
                    subhead = "Email Deliverability",
                    description = "Emails sent, opened, and clicked by day.",
                    uniqueId = cp.Utils.GetRandomString(4),
                    width = 2,
                    refreshSeconds = 0,
                    url = "",
                    dataLabels = dataLabels,
                    dataSets = [
                        new Models.DataSet() { label = "Sent", data = sentData },
                        new Models.DataSet() { label = "Opened", data = openedData },
                        new Models.DataSet() { label = "Clicked", data = clickedData }
                    ],
                    widgetType = WidgetTypeEnum.line,
                    filterOptions = [
                        new() {
                            filterCaption = "30 days",
                            filterValue = "30",
                            filterActive = (days == 30)
                        },
                        new() {
                            filterCaption = "60 days",
                            filterValue = "60",
                            filterActive = (days == 60)
                        },
                        new() {
                            filterCaption = "90 days",
                            filterValue = "90",
                            filterActive = (days == 90)
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
