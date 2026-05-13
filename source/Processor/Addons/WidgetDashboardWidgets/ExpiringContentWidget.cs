using Contensive.BaseClasses;
using Contensive.Processor.Models;
using System;
using System.Data;
using System.Text;

namespace Contensive.Processor.Addons.WidgetDashboardWidgets {
    public class ExpiringContentWidget : AddonBaseClass {
        public override object Execute(CPBaseClass cp) {
            try {
                //
                // -- query pages with expiration or archive dates within the next 30 days
                string today = cp.Db.EncodeSQLDate(DateTime.Now);
                string thirtyDaysOut = cp.Db.EncodeSQLDate(DateTime.Now.AddDays(30));
                string sql = $"select id, name, dateExpires, dateArchive from ccpagecontent"
                    + $" where (active=1)"
                    + $" and ((dateExpires>={today} and dateExpires<={thirtyDaysOut})"
                    + $" or (dateArchive>={today} and dateArchive<={thirtyDaysOut}))"
                    + $" order by"
                    + $" case when dateExpires is not null and dateArchive is not null"
                    + $" then case when dateExpires<dateArchive then dateExpires else dateArchive end"
                    + $" when dateExpires is not null then dateExpires"
                    + $" else dateArchive end";
                //
                string adminUrl = cp.Site.GetText("adminurl");
                var html = new StringBuilder();
                html.Append("<div style=\"max-height:300px;overflow-y:auto;\">");
                html.Append("<table class=\"table table-sm table-striped mb-0\">");
                html.Append("<thead><tr><th>Page</th><th>Expires</th><th>Archive</th></tr></thead>");
                html.Append("<tbody>");
                //
                int rowCount = 0;
                using (DataTable dt = cp.Db.ExecuteQuery(sql)) {
                    if (dt?.Rows != null) {
                        foreach (DataRow row in dt.Rows) {
                            int pageId = cp.Utils.EncodeInteger(row["id"]);
                            string pageName = cp.Utils.EncodeText(row["name"]);
                            if (string.IsNullOrWhiteSpace(pageName)) { pageName = "(untitled)"; }
                            //
                            DateTime expiresDate = cp.Utils.EncodeDate(row["dateExpires"]);
                            DateTime archiveDate = cp.Utils.EncodeDate(row["dateArchive"]);
                            string expiresDisplay = (expiresDate > DateTime.MinValue) ? expiresDate.ToShortDateString() : "";
                            string archiveDisplay = (archiveDate > DateTime.MinValue) ? archiveDate.ToShortDateString() : "";
                            //
                            string editUrl = $"{adminUrl}?cid={cp.Content.GetID("page content")}&id={pageId}&af=4";
                            html.Append("<tr>");
                            html.Append($"<td><a href=\"{editUrl}\" target=\"_blank\">{cp.Utils.EncodeHTML(pageName)}</a></td>");
                            html.Append($"<td>{expiresDisplay}</td>");
                            html.Append($"<td>{archiveDisplay}</td>");
                            html.Append("</tr>");
                            rowCount++;
                        }
                    }
                }
                //
                if (rowCount == 0) {
                    html.Append("<tr><td colspan=\"3\" class=\"text-center text-muted\">No pages expiring in the next 30 days</td></tr>");
                }
                //
                html.Append("</tbody></table></div>");
                //
                return new DashboardWidgetHtmlModel() {
                    widgetName = "Expiring Content",
                    width = 2,
                    htmlContent = html.ToString(),
                    refreshSeconds = 0
                };
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                throw;
            }
        }
    }
}
