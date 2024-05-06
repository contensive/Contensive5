
using System;
using Contensive.Processor.Controllers;
using NLog;
using static Contensive.Processor.Constants;

namespace Contensive.Processor.Addons.AdminSite {
    public class QuickStatsView {
        //
        // static logger
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        //
        //====================================================================================================
        //
        public static string get( CoreController core )  {
            string result = null;
            try {
                string sql = null;
                string RowColor = null;
                string Panel = null;
                int VisitID = 0;
                int VisitCount = 0;
                double PageCount = 0;
                StringBuilderLegacyController Stream = new StringBuilderLegacyController();
                //
                // --- Start a form to make a refresh button
                Stream.add(core.html.getPanelButtons(ButtonCancel + "," + ButtonRefresh));
                Stream.add("<input TYPE=\"hidden\" NAME=\"asf\" VALUE=\"" + AdminFormQuickStats + "\">");
                Stream.add(core.html.getPanel(" "));
                //

                // --- Indented part (Title Area plus page)
                //
                Stream.add("<table border=\"0\" cellpadding=\"20\" cellspacing=\"0\" width=\"100%\"><tr><td>" + SpanClassAdminNormal);
                Stream.add("<h1>Real-Time Activity Report</h1>");
                //
                // --- set column width
                //
                Stream.add("<h2>Visits Today</h2>");
                Stream.add("<table border=\"0\" cellpadding=\"3\" cellspacing=\"0\" width=\"100%\" style=\"background-color:white;border-top:1px solid #888;\">");
                //
                // ----- All Visits Today
                //
                using (var csData = new CsModel(core)) {
                    sql = "SELECT Count(ccVisits.ID) AS VisitCount, Avg(ccVisits.PageVisits) AS PageCount FROM ccVisits WHERE ((ccVisits.StartTime)>" + DbController.encodeSQLDate(core.doc.profileStartTime.Date) + ");";
                    csData.openSql(sql);
                    if (csData.ok()) {
                        VisitCount = csData.getInteger("VisitCount");
                        PageCount = csData.getNumber("pageCount");
                        Stream.add("<tr>");
                        Stream.add("<td style=\"border-bottom:1px solid #888;\" valign=top>" + SpanClassAdminNormal + "All Visits</span></td>");
                        Stream.add("<td style=\"width:150px;border-bottom:1px solid #888;\" valign=top>" + SpanClassAdminNormal + "<a target=\"_blank\" href=\"/" + HtmlController.encodeHtml(core.appConfig.adminRoute + "?" + rnAdminForm + "=" + AdminFormReports + "&rid=3&DateFrom=" + core.doc.profileStartTime + "&DateTo=" + core.doc.profileStartTime.ToShortDateString()) + "\">" + VisitCount + "</A>, " + string.Format("{0:N2}", PageCount) + " pages/visit.</span></td>");
                        Stream.add("<td style=\"border-bottom:1px solid #888;\" valign=top>" + SpanClassAdminNormal + "This includes all visitors to the website, including guests, bots and administrators. Pages/visit includes page hits and not ajax or remote method hits.</span></td>");
                        Stream.add("</tr>");
                    }
                }
                //
                // ----- Non-Bot Visits Today
                //
                using (var csData = new CsModel(core)) {
                    sql = "SELECT Count(ccVisits.ID) AS VisitCount, Avg(ccVisits.PageVisits) AS PageCount FROM ccVisits WHERE (ccVisits.CookieSupport=1)and((ccVisits.StartTime)>" + DbController.encodeSQLDate(core.doc.profileStartTime.Date) + ");";
                    csData.openSql(sql);
                    if (csData.ok()) {
                        VisitCount = csData.getInteger("VisitCount");
                        PageCount = csData.getNumber("pageCount");
                        Stream.add("<tr>");
                        Stream.add("<td style=\"border-bottom:1px solid #888;\" valign=top>" + SpanClassAdminNormal + "Non-bot Visits</span></td>");
                        Stream.add("<td style=\"border-bottom:1px solid #888;\" valign=top>" + SpanClassAdminNormal + "<a target=\"_blank\" href=\"/" + HtmlController.encodeHtml(core.appConfig.adminRoute + "?" + rnAdminForm + "=" + AdminFormReports + "&rid=3&DateFrom=" + core.doc.profileStartTime.ToShortDateString() + "&DateTo=" + core.doc.profileStartTime.ToShortDateString()) + "\">" + VisitCount + "</A>, " + string.Format("{0:N2}", PageCount) + " pages/visit.</span></td>");
                        Stream.add("<td style=\"border-bottom:1px solid #888;\" valign=top>" + SpanClassAdminNormal + "This excludes hits from visitors identified as bots. Pages/visit includes page hits and not ajax or remote method hits.</span></td>");
                        Stream.add("</tr>");
                    }
                }
                //
                // ----- Visits Today by new visitors
                //
                using (var csData = new CsModel(core)) {
                    sql = "SELECT Count(ccVisits.ID) AS VisitCount, Avg(ccVisits.PageVisits) AS PageCount FROM ccVisits WHERE (ccVisits.CookieSupport=1)and(ccVisits.StartTime>" + DbController.encodeSQLDate(core.doc.profileStartTime.Date) + ")AND(ccVisits.VisitorNew<>0);";
                    csData.openSql(sql);
                    if (csData.ok()) {
                        VisitCount = csData.getInteger("VisitCount");
                        PageCount = csData.getNumber("pageCount");
                        Stream.add("<tr>");
                        Stream.add("<td style=\"border-bottom:1px solid #888;\" valign=top>" + SpanClassAdminNormal + "Visits by New Visitors</span></td>");
                        Stream.add("<td style=\"border-bottom:1px solid #888;\" valign=top>" + SpanClassAdminNormal + "<a target=\"_blank\" href=\"/" + HtmlController.encodeHtml(core.appConfig.adminRoute + "?" + rnAdminForm + "=" + AdminFormReports + "&rid=3&ExcludeOldVisitors=1&DateFrom=" + core.doc.profileStartTime.ToShortDateString() + "&DateTo=" + core.doc.profileStartTime.ToShortDateString()) + "\">" + VisitCount + "</A>, " + string.Format("{0:N2}", PageCount) + " pages/visit.</span></td>");
                        Stream.add("<td style=\"border-bottom:1px solid #888;\" valign=top>" + SpanClassAdminNormal + "This includes only new visitors not identified as bots. Pages/visit includes page hits and not ajax or remote method hits.</span></td>");
                        Stream.add("</tr>");
                    }
                    csData.close();
                }
                //
                Stream.add("</table>");
                //
                // ----- Visits currently online
                //
                {
                    Panel = "";
                    Stream.add("<h2>Current Visits</h2>");
                    using (var csData = new CsModel(core)) {
                        sql = "SELECT ccVisits.HTTP_REFERER as referer,ccVisits.remote_addr as Remote_Addr, ccVisits.LastVisitTime as LastVisitTime, ccVisits.PageVisits as PageVisits, ccMembers.Name as MemberName, ccVisits.ID as VisitID, ccMembers.ID as MemberID"
                            + " FROM ccVisits LEFT JOIN ccMembers ON ccVisits.memberId = ccMembers.ID"
                            + " WHERE (((ccVisits.LastVisitTime)>" + DbController.encodeSQLDate(core.doc.profileStartTime.AddHours(-1)) + "))"
                            + " ORDER BY ccVisits.LastVisitTime DESC;";
                        csData.openSql(sql);
                        if (csData.ok()) {
                            Panel +=  "<table width=\"100%\" border=\"0\" cellspacing=\"1\" cellpadding=\"2\">";
                            Panel +=  "<tr bgcolor=\"#B0B0B0\">";
                            Panel +=  "<td width=\"20%\" align=\"left\">" + SpanClassAdminNormal + "User</td>";
                            Panel +=  "<td width=\"20%\" align=\"left\">" + SpanClassAdminNormal + "IP&nbsp;Address</td>";
                            Panel +=  "<td width=\"20%\" align=\"left\">" + SpanClassAdminNormal + "Last&nbsp;Page&nbsp;Hit</td>";
                            Panel +=  "<td width=\"10%\" align=\"right\">" + SpanClassAdminNormal + "Page&nbsp;Hits</td>";
                            Panel +=  "<td width=\"10%\" align=\"right\">" + SpanClassAdminNormal + "Visit</td>";
                            Panel +=  "<td width=\"30%\" align=\"left\">" + SpanClassAdminNormal + "Referer</td>";
                            Panel +=  "</tr>";
                            RowColor = "ccPanelRowEven";
                            while (csData.ok()) {
                                VisitID = csData.getInteger("VisitID");
                                Panel +=  "<tr class=\"" + RowColor + "\">";
                                Panel +=  "<td align=\"left\">" + SpanClassAdminNormal + "<a target=\"_blank\" href=\"/" + HtmlController.encodeHtml(core.appConfig.adminRoute + "?" + rnAdminForm + "=" + AdminFormReports + "&rid=16&MemberID=" + csData.getInteger("MemberID")) + "\">" + csData.getText("MemberName") + "</A></span></td>";
                                Panel +=  "<td align=\"left\">" + SpanClassAdminNormal + csData.getText("Remote_Addr") + "</span></td>";
                                Panel +=  "<td align=\"left\">" + SpanClassAdminNormal + csData.getDate("LastVisitTime").ToString("") + "</span></td>";
                                Panel +=  "<td align=\"right\">" + SpanClassAdminNormal + "<a target=\"_blank\" href=\"/" + core.appConfig.adminRoute + "?" + rnAdminForm + "=" + AdminFormReports + "&rid=10&VisitID=" + VisitID + "\">" + csData.getText("PageVisits") + "</A></span></td>";
                                Panel +=  "<td align=\"right\">" + SpanClassAdminNormal + "<a target=\"_blank\" href=\"/" + core.appConfig.adminRoute + "?" + rnAdminForm + "=" + AdminFormReports + "&rid=17&VisitID=" + VisitID + "\">" + VisitID + "</A></span></td>";
                                Panel +=  "<td align=\"left\">" + SpanClassAdminNormal + "&nbsp;" + csData.getText("referer") + "</span></td>";
                                Panel +=  "</tr>";
                                if (RowColor == "ccPanelRowEven") {
                                    RowColor = "ccPanelRowOdd";
                                } else {
                                    RowColor = "ccPanelRowEven";
                                }
                                csData.goNext();
                            }
                            Panel +=  "</table>";
                        }
                        csData.close();
                    }
                    Stream.add(core.html.getPanel(Panel, "ccPanel", "ccPanelShadow", "ccPanelHilite", "100%", 0));
                }
                Stream.add("</td></tr></table>");
                //
                result = HtmlController.form(core, Stream.text);
                return result;
                //
                // ----- Error Trap
                //
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
            }
            return result;
        }
        //

    }
}
