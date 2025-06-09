
using System;
using Contensive.Processor.Controllers;
using NLog;
using static Contensive.Processor.Constants;

namespace Contensive.Processor.Addons.Tools {
    public class UsersOnlineReportAddon : BaseClasses.AddonBaseClass {
        //
        internal const int AdminFormQuickStats = 18; // Quick Stats (from Admin root)
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
            string result = null;
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
                        body.add("<td style=\"border-bottom:1px solid #888;\" valign=top>" + SpanClassAdminNormal + "All Visits</span></td>");
                        body.add("<td style=\"width:150px;border-bottom:1px solid #888;\" valign=top>" + SpanClassAdminNormal + "<a target=\"_blank\" href=\"/" + HtmlController.encodeHtml(core.appConfig.adminRoute + "?" + rnAdminForm + "=" + AdminFormReports + "&rid=3&DateFrom=" + core.doc.profileStartTime + "&DateTo=" + core.doc.profileStartTime.ToShortDateString()) + "\">" + VisitCount + "</A>, " + string.Format("{0:N2}", PageCount) + " pages/visit.</span></td>");
                        body.add("<td style=\"border-bottom:1px solid #888;\" valign=top>" + SpanClassAdminNormal + "This includes all visitors to the website, including guests, bots and administrators. Pages/visit includes page hits and not ajax or remote method hits.</span></td>");
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
                        body.add("<td style=\"border-bottom:1px solid #888;\" valign=top>" + SpanClassAdminNormal + "Non-bot Visits</span></td>");
                        body.add("<td style=\"border-bottom:1px solid #888;\" valign=top>" + SpanClassAdminNormal + "<a target=\"_blank\" href=\"/" + HtmlController.encodeHtml(core.appConfig.adminRoute + "?" + rnAdminForm + "=" + AdminFormReports + "&rid=3&DateFrom=" + core.doc.profileStartTime.ToShortDateString() + "&DateTo=" + core.doc.profileStartTime.ToShortDateString()) + "\">" + VisitCount + "</A>, " + string.Format("{0:N2}", PageCount) + " pages/visit.</span></td>");
                        body.add("<td style=\"border-bottom:1px solid #888;\" valign=top>" + SpanClassAdminNormal + "This excludes hits from visitors identified as bots. Pages/visit includes page hits and not ajax or remote method hits.</span></td>");
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
                        body.add("<td style=\"border-bottom:1px solid #888;\" valign=top>" + SpanClassAdminNormal + "Visits by New Visitors</span></td>");
                        body.add("<td style=\"border-bottom:1px solid #888;\" valign=top>" + SpanClassAdminNormal + "<a target=\"_blank\" href=\"/" + HtmlController.encodeHtml(core.appConfig.adminRoute + "?" + rnAdminForm + "=" + AdminFormReports + "&rid=3&ExcludeOldVisitors=1&DateFrom=" + core.doc.profileStartTime.ToShortDateString() + "&DateTo=" + core.doc.profileStartTime.ToShortDateString()) + "\">" + VisitCount + "</A>, " + string.Format("{0:N2}", PageCount) + " pages/visit.</span></td>");
                        body.add("<td style=\"border-bottom:1px solid #888;\" valign=top>" + SpanClassAdminNormal + "This includes only new visitors not identified as bots. Pages/visit includes page hits and not ajax or remote method hits.</span></td>");
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
                            Panel += "<td width=\"20%\" align=\"left\">" + SpanClassAdminNormal + "User</td>";
                            Panel += "<td width=\"20%\" align=\"left\">" + SpanClassAdminNormal + "IP&nbsp;Address</td>";
                            Panel += "<td width=\"20%\" align=\"left\">" + SpanClassAdminNormal + "Last&nbsp;Page&nbsp;Hit</td>";
                            Panel += "<td width=\"10%\" align=\"right\">" + SpanClassAdminNormal + "Page&nbsp;Hits</td>";
                            Panel += "<td width=\"10%\" align=\"right\">" + SpanClassAdminNormal + "Visit</td>";
                            Panel += "<td width=\"30%\" align=\"left\">" + SpanClassAdminNormal + "Referer</td>";
                            Panel += "</tr>";
                            string RowColor = "ccPanelRowEven";
                            while (csData.ok()) {
                                int VisitID = csData.getInteger("VisitID");
                                Panel += "<tr class=\"" + RowColor + "\">";
                                Panel += "<td align=\"left\">" + SpanClassAdminNormal + "<a target=\"_blank\" href=\"/" + HtmlController.encodeHtml(core.appConfig.adminRoute + "?" + rnAdminForm + "=" + AdminFormReports + "&rid=16&MemberID=" + csData.getInteger("MemberID")) + "\">" + csData.getText("MemberName") + "</A></span></td>";
                                Panel += "<td align=\"left\">" + SpanClassAdminNormal + csData.getText("Remote_Addr") + "</span></td>";
                                Panel += "<td align=\"left\">" + SpanClassAdminNormal + csData.getDate("LastVisitTime").ToString("") + "</span></td>";
                                Panel += "<td align=\"right\">" + SpanClassAdminNormal + "<a target=\"_blank\" href=\"/" + core.appConfig.adminRoute + "?" + rnAdminForm + "=" + AdminFormReports + "&rid=10&VisitID=" + VisitID + "\">" + csData.getText("PageVisits") + "</A></span></td>";
                                Panel += "<td align=\"right\">" + SpanClassAdminNormal + "<a target=\"_blank\" href=\"/" + core.appConfig.adminRoute + "?" + rnAdminForm + "=" + AdminFormReports + "&rid=17&VisitID=" + VisitID + "\">" + VisitID + "</A></span></td>";
                                Panel += "<td align=\"left\">" + SpanClassAdminNormal + "&nbsp;" + csData.getText("referer") + "</span></td>";
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
                //
                layout.addFormHidden("asf", AdminFormQuickStats);
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