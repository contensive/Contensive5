
using System;
using Contensive.Processor.Controllers;
using NLog;
using static Contensive.Processor.Constants;

namespace Contensive.Processor.Addons.Reports {
    public class UsersOnlineReport : BaseClasses.AddonBaseClass {
        //
        // constants
        internal const string guidUsersOnlineReport = "{A5439430-ED28-4D72-A9ED-50FB36145955}";
        //
        // static logger
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        //
        //====================================================================================================
        /// <summary>
        /// addon method 
        /// blank return on OK or cancel button
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public override object Execute(BaseClasses.CPBaseClass cpBase) {
            return get(((CPClass)cpBase).core);
        }
        //
        //
        //====================================================================================================
        //
        public static string get(CoreController core) {
            try {
                //
                // -- get the button clicked
                string  button = core.cpParent.Doc.GetText("button", "");
                if (button == ButtonCancel) { return ""; }
                //
                StringBuilderLegacyController body = new();
                //
                // --- Indented part (Title Area plus page)
                //
                body.add("<table border=\"0\" cellpadding=\"20\" cellspacing=\"0\" width=\"100%\"><tr><td>" + SpanClassAdminNormal);
                //
                // --- set column width
                //
                body.add("<h2>Visits Today</h2>");
                body.add("<table border=\"0\" cellpadding=\"3\" cellspacing=\"0\" width=\"100%\" style=\"background-color:white;border-top:1px solid #888;\">");
                //
                // ----- All Visits Today
                //
                using (var csData = new CsModel(core)) {
                    string sql = "SELECT Count(ccVisits.ID) AS VisitCount, Avg(ccVisits.PageVisits) AS PageCount FROM ccVisits WHERE ((ccVisits.StartTime)>" + DbController.encodeSQLDate(core.doc.profileStartTime.Date) + ");";
                    csData.openSql(sql);
                    if (csData.ok()) {
                        int VisitCount = csData.getInteger("VisitCount");
                        double PageCount = csData.getNumber("pageCount");
                        body.add("<tr>");
                        body.add("<td style=\"border-bottom:1px solid #888;\" valign=top>All Visits</td>");
                        body.add("<td style=\"width:150px;border-bottom:1px solid #888;\" valign=top><a target=\"_blank\" href=\"/" + HtmlController.encodeHtml(core.appConfig.adminRoute + "?" + rnAdminForm + "=" + AdminFormReports + "&rid=3&DateFrom=" + core.doc.profileStartTime + "&DateTo=" + core.doc.profileStartTime.ToShortDateString()) + "\">" + VisitCount + "</A>, " + string.Format("{0:N2}", PageCount) + " pages/visit.</td>");
                        body.add("<td style=\"border-bottom:1px solid #888;\" valign=top>This includes all visitors to the website, including guests, bots and administrators. Pages/visit includes page hits and not ajax or remote method hits.</td>");
                        body.add("</tr>");
                    }
                }
                //
                // ----- Non-Bot Visits Today
                //
                using (var csData = new CsModel(core)) {
                    string sql = "SELECT Count(ccVisits.ID) AS VisitCount, Avg(ccVisits.PageVisits) AS PageCount FROM ccVisits WHERE (ccVisits.CookieSupport=1)and((ccVisits.StartTime)>" + DbController.encodeSQLDate(core.doc.profileStartTime.Date) + ");";
                    csData.openSql(sql);
                    if (csData.ok()) {
                        int VisitCount = csData.getInteger("VisitCount");
                        double PageCount = csData.getNumber("pageCount");
                        body.add("<tr>");
                        body.add("<td style=\"border-bottom:1px solid #888;\" valign=top>Non-bot Visits</td>");
                        body.add("<td style=\"border-bottom:1px solid #888;\" valign=top><a target=\"_blank\" href=\"/" + HtmlController.encodeHtml(core.appConfig.adminRoute + "?" + rnAdminForm + "=" + AdminFormReports + "&rid=3&DateFrom=" + core.doc.profileStartTime.ToShortDateString() + "&DateTo=" + core.doc.profileStartTime.ToShortDateString()) + "\">" + VisitCount + "</A>, " + string.Format("{0:N2}", PageCount) + " pages/visit.</td>");
                        body.add("<td style=\"border-bottom:1px solid #888;\" valign=top>This excludes hits from visitors identified as bots. Pages/visit includes page hits and not ajax or remote method hits.</td>");
                        body.add("</tr>");
                    }
                }
                //
                // ----- Visits Today by new visitors
                //
                using (var csData = new CsModel(core)) {
                    string sql = "SELECT Count(ccVisits.ID) AS VisitCount, Avg(ccVisits.PageVisits) AS PageCount FROM ccVisits WHERE (ccVisits.CookieSupport=1)and(ccVisits.StartTime>" + DbController.encodeSQLDate(core.doc.profileStartTime.Date) + ")AND(ccVisits.VisitorNew<>0);";
                    csData.openSql(sql);
                    if (csData.ok()) {
                        int VisitCount = csData.getInteger("VisitCount");
                        double PageCount = csData.getNumber("pageCount");
                        body.add("<tr>");
                        body.add("<td style=\"border-bottom:1px solid #888;\" valign=top>Visits by New Visitors</td>");
                        body.add("<td style=\"border-bottom:1px solid #888;\" valign=top><a target=\"_blank\" href=\"/" + HtmlController.encodeHtml(core.appConfig.adminRoute + "?" + rnAdminForm + "=" + AdminFormReports + "&rid=3&ExcludeOldVisitors=1&DateFrom=" + core.doc.profileStartTime.ToShortDateString() + "&DateTo=" + core.doc.profileStartTime.ToShortDateString()) + "\">" + VisitCount + "</A>, " + string.Format("{0:N2}", PageCount) + " pages/visit.</td>");
                        body.add("<td style=\"border-bottom:1px solid #888;\" valign=top>This includes only new visitors not identified as bots. Pages/visit includes page hits and not ajax or remote method hits.</td>");
                        body.add("</tr>");
                    }
                    csData.close();
                }
                //
                body.add("</table>");
                //
                // ----- Visits currently online
                //
                {
                    string Panel = "";
                    body.add("<h2>Current Visits</h2>");
                    using (var csData = new CsModel(core)) {
                        string sql = "SELECT ccVisits.HTTP_REFERER as referer,ccVisits.remote_addr as Remote_Addr, ccVisits.LastVisitTime as LastVisitTime, ccVisits.PageVisits as PageVisits, ccMembers.Name as MemberName, ccVisits.ID as VisitID, ccMembers.ID as MemberID"
                            + " FROM ccVisits LEFT JOIN ccMembers ON ccVisits.memberId = ccMembers.ID"
                            + " WHERE (((ccVisits.LastVisitTime)>" + DbController.encodeSQLDate(core.doc.profileStartTime.AddHours(-1)) + "))"
                            + " ORDER BY ccVisits.LastVisitTime DESC;";
                        csData.openSql(sql);
                        if (csData.ok()) {
                            Panel += "<table width=\"100%\" border=\"0\" cellspacing=\"1\" cellpadding=\"2\">";
                            Panel += "<tr bgcolor=\"#B0B0B0\">";
                            Panel += "<td width=\"20%\" align=\"left\">User</td>";
                            Panel += "<td width=\"20%\" align=\"left\">IP&nbsp;Address</td>";
                            Panel += "<td width=\"20%\" align=\"left\">Last&nbsp;Page&nbsp;Hit</td>";
                            Panel += "<td width=\"10%\" align=\"right\">Page&nbsp;Hits</td>";
                            Panel += "<td width=\"10%\" align=\"right\">Visit</td>";
                            Panel += "<td width=\"30%\" align=\"left\">Referer</td>";
                            Panel += "</tr>";
                            string RowColor = "ccPanelRowEven";
                            while (csData.ok()) {
                                int VisitID = csData.getInteger("VisitID");
                                Panel += "<tr class=\"" + RowColor + "\">";
                                Panel += "<td align=\"left\"><a target=\"_blank\" href=\"/" + HtmlController.encodeHtml(core.appConfig.adminRoute + "?" + rnAdminForm + "=" + AdminFormReports + "&rid=16&MemberID=" + csData.getInteger("MemberID")) + "\">" + csData.getText("MemberName") + "</A></td>";
                                Panel += "<td align=\"left\">" + csData.getText("Remote_Addr") + "</td>";
                                Panel += "<td align=\"left\">" + csData.getDate("LastVisitTime").ToString("") + "</td>";
                                Panel += "<td align=\"right\">" + csData.getText("PageVisits") + "</td>";
                                Panel += "<td align=\"right\">" + VisitID + "</td>";
                                Panel += "<td align=\"left\">&nbsp;" + csData.getText("referer") + "</td>";
                                Panel += "</tr>";
                                if (RowColor == "ccPanelRowEven") {
                                    RowColor = "ccPanelRowOdd";
                                } else {
                                    RowColor = "ccPanelRowEven";
                                }
                                csData.goNext();
                            }
                            Panel += "</table>";
                        }
                        csData.close();
                    }
                    body.add(core.html.getPanel(Panel, "ccPanel", "ccPanelShadow", "ccPanelHilite", "100%", 0));
                }
                body.add("</td></tr></table>");
                //
                //
                var layout = core.cpParent.AdminUI.CreateLayoutBuilder();
                layout.title = "Users Online report";
                layout.description = "";
                layout.body = HtmlController.form(core, body.text);
                ////
                //layout.addFormHidden(rnAdminSourceForm, AdminFormQuickStats);
                //
                layout.addFormButton(ButtonCancel);
                layout.addFormButton(ButtonRefresh);
                //
                return layout.getHtml();
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }
        }
    }
}