
using Contensive.Processor.Controllers;
using NLog;
using System;
using System.Xml;
using static Contensive.Processor.Controllers.GenericController;

namespace Contensive.Processor.Addons.Housekeeping {
    /// <summary>
    /// Housekeep this content
    /// </summary>
    public static class ViewingSummaryClass {
        //
        // static logger
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        //
        //====================================================================================================
        /// <summary>
        /// execute hourly tasks
        /// </summary>
        /// <param name="core"></param>
        public static void executeHourlyTasks(HouseKeepEnvironmentModel env) {
            try {
                //
                env.log("Housekeep, executeHourlyTasks, ViewingSummary");
                //
            } catch (Exception ex) {
                logger.Error(ex, $"{env.core.logCommonMessage}");
                LogController.logAlarm(env.core, "Housekeep, exception, ex [" + ex + "]");
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// execute Daily Tasks
        /// </summary>
        /// <param name="core"></param>
        /// <param name="env"></param>
        public static void updateViewingSummary(HouseKeepEnvironmentModel env) {
            try {
                //
                env.log("Housekeep, viewingsummary");
                //
                //
                // -- there is a bug and I need to move on.
                return;
                //
                // Page View Summary
                //
                //{
                //    DateTime datePtr = default;
                //    using (var csData = new CsModel(env.core)) {
                //        if (!csData.openSql(env.core.db.getSQLSelect("ccviewingsummary", "DateNumber", "TimeDuration=24 and DateNumber>=" + env.oldestVisitSummaryWeCareAbout.Date.ToOADate(), "DateNumber Desc", "", 1))) {
                //            datePtr = env.oldestVisitSummaryWeCareAbout;
                //        } else {
                //            datePtr = DateTime.MinValue.AddDays(csData.getInteger("DateNumber"));
                //        }
                //    }
                //    if (datePtr < env.oldestVisitSummaryWeCareAbout) { datePtr = env.oldestVisitSummaryWeCareAbout; }
                //    pageViewSummary(core, datePtr, env.yesterday, 24, env.core.siteProperties.dataBuildVersion, env.oldestVisitSummaryWeCareAbout);
                //}
            } catch (Exception ex) {
                logger.Error(ex, $"{env.core.logCommonMessage}");
                LogController.logAlarm(env.core, "Housekeep, exception, ex [" + ex + "]");
                throw;
            }
        }
        //====================================================================================================
        /// <summary>
        /// Summarize the page views, excludes non-cookie visits, excludes administrator and developer visits, excludes authenticated users with ExcludeFromReporting
        /// </summary>
        /// <param name="core"></param>
        /// <param name="StartTimeDate"></param>
        /// <param name="EndTimeDate"></param>
        /// <param name="HourDuration"></param>
        /// <param name="BuildVersion"></param>
        /// <param name="OldestVisitSummaryWeCareAbout"></param>
        //
        public static void pageViewSummary(HouseKeepEnvironmentModel env, DateTime StartTimeDate, DateTime EndTimeDate, int HourDuration, string BuildVersion, DateTime OldestVisitSummaryWeCareAbout) {
            int hint = 0;
            string hinttxt = "";
            try {
                XmlDocument LibraryCollections = new();
                XmlDocument LocalCollections = new();
                XmlDocument Doc = new XmlDocument();
                {
                    hint = 1;
                    DateTime PeriodStart = default;
                    PeriodStart = StartTimeDate;
                    if (PeriodStart < OldestVisitSummaryWeCareAbout) {
                        PeriodStart = OldestVisitSummaryWeCareAbout;
                    }
                    DateTime PeriodDatePtr = default;
                    PeriodDatePtr = PeriodStart.Date;
                    while (PeriodDatePtr < EndTimeDate) {
                        hint = 2;
                        //
                        hinttxt = ", HourDuration [" + HourDuration + "], PeriodDatePtr [" + PeriodDatePtr + "], PeriodDatePtr.AddHours(HourDuration / 2.0) [" + PeriodDatePtr.AddHours(HourDuration / 2.0) + "]";
                        int DateNumber = encodeInteger( (PeriodDatePtr - default(DateTime)).TotalDays);
                        // encodeInteger(PeriodDatePtr.AddHours(HourDuration / 2.0).ToOADate());
                        int TimeNumber = encodeInteger(PeriodDatePtr.TimeOfDay.TotalHours);
                        DateTime DateStart = default;
                        DateStart = PeriodDatePtr.Date;
                        DateTime DateEnd = default;
                        DateEnd = PeriodDatePtr.AddHours(HourDuration).Date;
                        string PageTitle = "";
                        int PageId = 0;
                        int PageViews = 0;
                        int AuthenticatedPageViews = 0;
                        int MobilePageViews = 0;
                        int BotPageViews = 0;
                        int NoCookiePageViews = 0;
                        //
                        // Loop through all the pages hit during this period
                        //
                        //
                        // for now ignore the problem caused by addons like Blogs creating multiple 'pages' within on pageid
                        // One way to handle this is to expect the addon to set something unquie in he page title
                        // then use the title to distinguish a page. The problem with this is the current system puts the
                        // visit number and page number in the name. if we select on district name, they will all be.
                        //
                        using (var csPages = new CsModel(env.core)) {
                            string sql = "select distinct recordid,pagetitle from ccviewings h"
                                + " where (h.recordid<>0)"
                                + " and(h.dateadded>=" + DbController.encodeSQLDate(DateStart) + ")"
                                + " and (h.dateadded<" + DbController.encodeSQLDate(DateEnd) + ")"
                                + " and((h.ExcludeFromAnalytics is null)or(h.ExcludeFromAnalytics=0))"
                                + "order by recordid";
                            hint = 3;
                            if (!csPages.openSql(sql)) {
                                //
                                // no hits found - add or update a single record for this day so we know it has been calculated
                                csPages.open("Page View Summary", "(timeduration=" + HourDuration + ")and(DateNumber=" + DateNumber + ")and(TimeNumber=" + TimeNumber + ")and(pageid=" + PageId + ")and(pagetitle=" + DbController.encodeSQLText(PageTitle) + ")");
                                if (!csPages.ok()) {
                                    csPages.close();
                                    csPages.insert("Page View Summary");
                                }
                                //
                                if (csPages.ok()) {
                                    csPages.set("name", HourDuration + " hr summary for " + DateTime.MinValue.AddDays(DateNumber) + " " + TimeNumber + ":00, " + PageTitle);
                                    csPages.set("DateNumber", DateNumber);
                                    csPages.set("TimeNumber", TimeNumber);
                                    csPages.set("TimeDuration", HourDuration);
                                    csPages.set("PageViews", PageViews);
                                    csPages.set("PageID", PageId);
                                    csPages.set("PageTitle", PageTitle);
                                    csPages.set("AuthenticatedPageViews", AuthenticatedPageViews);
                                    csPages.set("NoCookiePageViews", NoCookiePageViews);
                                    {
                                        csPages.set("MobilePageViews", MobilePageViews);
                                        csPages.set("BotPageViews", BotPageViews);
                                    }
                                }
                                csPages.close();
                                hint = 4;
                            } else {
                                hint = 5;
                                //
                                // add an entry for each page hit on this day
                                //
                                while (csPages.ok()) {
                                    PageId = csPages.getInteger("recordid");
                                    PageTitle = csPages.getText("pagetitle");
                                    string baseCriteria = ""
                                        + " (h.recordid=" + PageId + ")"
                                        + " "
                                        + " and(h.dateadded>=" + DbController.encodeSQLDate(DateStart) + ")"
                                        + " and(h.dateadded<" + DbController.encodeSQLDate(DateEnd) + ")"
                                        + " and((v.ExcludeFromAnalytics is null)or(v.ExcludeFromAnalytics=0))"
                                        + " and((h.ExcludeFromAnalytics is null)or(h.ExcludeFromAnalytics=0))"
                                        + "";
                                    if (!string.IsNullOrEmpty(PageTitle)) {
                                        baseCriteria = baseCriteria + "and(h.pagetitle=" + DbController.encodeSQLText(PageTitle) + ")";
                                    }
                                    hint = 6;
                                    //
                                    // Total Page Views
                                    using (var csPageViews = new CsModel(env.core)) {
                                        sql = "select count(h.id) as cnt"
                                            + " from ccviewings h left join ccvisits v on h.visitid=v.id"
                                            + " where " + baseCriteria + " and (v.CookieSupport<>0)"
                                            + "";
                                        csPageViews.openSql(sql);
                                        if (csPageViews.ok()) {
                                            PageViews = csPageViews.getInteger("cnt");
                                        }
                                    }
                                    //
                                    // Authenticated Visits
                                    //
                                    using (var csAuthPages = new CsModel(env.core)) {
                                        sql = "select count(h.id) as cnt"
                                            + " from ccviewings h left join ccvisits v on h.visitid=v.id"
                                            + " where " + baseCriteria + " and(v.CookieSupport<>0)"
                                            + " and(v.visitAuthenticated<>0)"
                                            + "";
                                        csAuthPages.openSql(sql);
                                        if (csAuthPages.ok()) {
                                            AuthenticatedPageViews = csAuthPages.getInteger("cnt");
                                        }
                                    }
                                    //
                                    // No Cookie Page Views
                                    //
                                    using (var csNoCookie = new CsModel(env.core)) {
                                        sql = "select count(h.id) as NoCookiePageViews"
                                            + " from ccviewings h left join ccvisits v on h.visitid=v.id"
                                            + " where " + baseCriteria + " and((v.CookieSupport=0)or(v.CookieSupport is null))"
                                            + "";
                                        csNoCookie.openSql(sql);
                                        if (csNoCookie.ok()) {
                                            NoCookiePageViews = csNoCookie.getInteger("NoCookiePageViews");
                                        }
                                    }
                                    //
                                    //
                                    // Mobile Visits
                                    using (var csMobileVisits = new CsModel(env.core)) {
                                        sql = "select count(h.id) as cnt"
                                            + " from ccviewings h left join ccvisits v on h.visitid=v.id"
                                            + " where " + baseCriteria + " and(v.CookieSupport<>0)"
                                            + " and(v.mobile<>0)"
                                            + "";
                                        csMobileVisits.openSql(sql);
                                        if (csMobileVisits.ok()) {
                                            MobilePageViews = csMobileVisits.getInteger("cnt");
                                        }
                                    }
                                    //
                                    // Bot Visits
                                    using (var csBotVisits = new CsModel(env.core)) {
                                        sql = "select count(h.id) as cnt"
                                            + " from ccviewings h left join ccvisits v on h.visitid=v.id"
                                            + " where " + baseCriteria + " and(v.CookieSupport<>0)"
                                            + " and(v.bot<>0)"
                                            + "";
                                        csBotVisits.openSql(sql);
                                        if (csBotVisits.ok()) {
                                            BotPageViews = csBotVisits.getInteger("cnt");
                                        }
                                    }
                                    //
                                    // Add or update the Visit Summary Record
                                    //
                                    using (var csPVS = new CsModel(env.core)) {
                                        if (!csPVS.open("Page View Summary", "(timeduration=" + HourDuration + ")and(DateNumber=" + DateNumber + ")and(TimeNumber=" + TimeNumber + ")and(pageid=" + PageId + ")and(pagetitle=" + DbController.encodeSQLText(PageTitle) + ")")) {
                                            csPVS.insert("Page View Summary");
                                        }
                                        //
                                        if (csPVS.ok()) {
                                            hint = 11;
                                            string PageName = "";
                                            if (string.IsNullOrEmpty(PageTitle)) {
                                                PageName = MetadataController.getRecordName(env.core, "page content", PageId);
                                                csPVS.set("name", HourDuration + " hr summary for " + DateTime.MinValue.AddDays(DateNumber) + " " + TimeNumber + ":00, " + PageName);
                                                csPVS.set("PageTitle", PageName);
                                            } else {
                                                csPVS.set("name", HourDuration + " hr summary for " + DateTime.MinValue.AddDays(DateNumber) + " " + TimeNumber + ":00, " + PageTitle);
                                                csPVS.set("PageTitle", PageTitle);
                                            }
                                            csPVS.set("DateNumber", DateNumber);
                                            csPVS.set("TimeNumber", TimeNumber);
                                            csPVS.set("TimeDuration", HourDuration);
                                            csPVS.set("PageViews", PageViews);
                                            csPVS.set("PageID", PageId);
                                            csPVS.set("AuthenticatedPageViews", AuthenticatedPageViews);
                                            csPVS.set("NoCookiePageViews", NoCookiePageViews);
                                            hint = 12;
                                            {
                                                csPVS.set("MobilePageViews", MobilePageViews);
                                                csPVS.set("BotPageViews", BotPageViews);
                                            }
                                        }
                                    }
                                    csPages.goNext();
                                }
                            }
                        }
                        PeriodDatePtr = PeriodDatePtr.AddHours(HourDuration);
                    }
                }
                //
                return;
            } catch (Exception ex) {
                logger.Error($"{env.core.logCommonMessage}, hint [" + hint + "]", ex);
            }
        }
        //
    }
}