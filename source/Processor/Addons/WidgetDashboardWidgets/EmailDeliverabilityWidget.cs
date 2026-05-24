using Contensive.BaseClasses;
using Contensive.Processor.Models;
using System;
using System.Collections.Generic;
using System.Data;

namespace Contensive.Processor.Addons.WidgetDashboardWidgets {
    public class EmailDeliverabilityWidget : AddonBaseClass {

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
                // -- query send attempts: total, successful sends, and failed sends by date
                // -- logType 1=Drop, 6=ImmediateSend are both send attempts
                // -- sendStatus='ok' means successful, anything else is a failure
                string startDate = cp.Db.EncodeSQLDate(DateTime.Now.AddDays(-days));
                string sqlSends = $"select cast(dateAdded as date) as logDate"
                    + $",count(*) as total"
                    + $",sum(case when sendStatus='ok' then 1 else 0 end) as sent"
                    + $",sum(case when sendStatus<>'ok' then 1 else 0 end) as failed"
                    + $" from ccemaillog"
                    + $" where dateAdded>={startDate}"
                    + $" and logType in (1,6) and emailId>0"
                    + $" group by cast(dateAdded as date)"
                    + $" order by cast(dateAdded as date)";
                //
                // -- query deduplicated opens by date (one open per recipient per drop)
                string sqlOpens = $"select cast(min(dateAdded) as date) as logDate, count(*) as cnt"
                    + $" from ("
                    + $"select emailDropId, memberId, min(dateAdded) as dateAdded"
                    + $" from ccemaillog"
                    + $" where dateAdded>={startDate} and logType=2 and emailId>0"
                    + $" group by emailDropId, memberId"
                    + $") as uniqueOpens"
                    + $" group by cast(dateAdded as date)"
                    + $" order by cast(dateAdded as date)";
                //
                // -- query deduplicated clicks by date (one click per recipient per drop)
                string sqlClicks = $"select cast(min(dateAdded) as date) as logDate, count(*) as cnt"
                    + $" from ("
                    + $"select emailDropId, memberId, min(dateAdded) as dateAdded"
                    + $" from ccemaillog"
                    + $" where dateAdded>={startDate} and logType=3 and emailId>0"
                    + $" group by emailDropId, memberId"
                    + $") as uniqueClicks"
                    + $" group by cast(dateAdded as date)"
                    + $" order by cast(dateAdded as date)";
                //
                // -- build date-indexed dictionaries for each series
                var totalByDate = new Dictionary<DateTime, double>();
                var sentByDate = new Dictionary<DateTime, double>();
                var failedByDate = new Dictionary<DateTime, double>();
                var openedByDate = new Dictionary<DateTime, double>();
                var clickedByDate = new Dictionary<DateTime, double>();
                //
                // -- initialize all dates in range
                DateTime startDt = DateTime.Now.Date.AddDays(-days + 1);
                DateTime endDt = DateTime.Now.Date;
                for (DateTime dt = startDt; dt <= endDt; dt = dt.AddDays(1)) {
                    totalByDate[dt] = 0;
                    sentByDate[dt] = 0;
                    failedByDate[dt] = 0;
                    openedByDate[dt] = 0;
                    clickedByDate[dt] = 0;
                }
                //
                // -- populate sends (total, sent, failed)
                using (DataTable dt = cp.Db.ExecuteQuery(sqlSends)) {
                    if (dt?.Rows != null) {
                        foreach (DataRow row in dt.Rows) {
                            DateTime logDate = cp.Utils.EncodeDate(row["logDate"]).Date;
                            if (!totalByDate.ContainsKey(logDate)) { continue; }
                            totalByDate[logDate] = cp.Utils.EncodeNumber(row["total"]);
                            sentByDate[logDate] = cp.Utils.EncodeNumber(row["sent"]);
                            failedByDate[logDate] = cp.Utils.EncodeNumber(row["failed"]);
                        }
                    }
                }
                //
                // -- populate deduplicated opens
                using (DataTable dt = cp.Db.ExecuteQuery(sqlOpens)) {
                    if (dt?.Rows != null) {
                        foreach (DataRow row in dt.Rows) {
                            DateTime logDate = cp.Utils.EncodeDate(row["logDate"]).Date;
                            if (!openedByDate.ContainsKey(logDate)) { continue; }
                            openedByDate[logDate] = cp.Utils.EncodeNumber(row["cnt"]);
                        }
                    }
                }
                //
                // -- populate deduplicated clicks
                using (DataTable dt = cp.Db.ExecuteQuery(sqlClicks)) {
                    if (dt?.Rows != null) {
                        foreach (DataRow row in dt.Rows) {
                            DateTime logDate = cp.Utils.EncodeDate(row["logDate"]).Date;
                            if (!clickedByDate.ContainsKey(logDate)) { continue; }
                            clickedByDate[logDate] = cp.Utils.EncodeNumber(row["cnt"]);
                        }
                    }
                }
                //
                // -- build labels and data arrays
                var dataLabels = new List<string>();
                var totalData = new List<double>();
                var failedData = new List<double>();
                var sentData = new List<double>();
                var openedData = new List<double>();
                var clickedData = new List<double>();
                for (DateTime dt = startDt; dt <= endDt; dt = dt.AddDays(1)) {
                    dataLabels.Add(dt.ToString("M/d"));
                    totalData.Add(totalByDate[dt]);
                    failedData.Add(failedByDate[dt]);
                    sentData.Add(sentByDate[dt]);
                    openedData.Add(openedByDate[dt]);
                    clickedData.Add(clickedByDate[dt]);
                }
                //
                DashboardWidgetLineChartModel result = new() {
                    widgetName = "Email Deliverability",
                    subhead = "Email Deliverability",
                    description = "Email send totals, failures, successful sends, unique opens, and unique clicks by day.",
                    uniqueId = cp.Utils.GetRandomString(4),
                    width = 2,
                    refreshSeconds = 0,
                    url = "",
                    dataLabels = dataLabels,
                    dataSets = [
                        new Models.DataSet() { label = "Total", data = totalData },
                        new Models.DataSet() { label = "Failed", data = failedData },
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
