﻿
using System;
using Contensive.Processor.Controllers;
using static Contensive.Processor.Controllers.GenericController;
using static Contensive.Processor.Constants;
using System.Xml;
using Contensive.Processor;

namespace Contensive.Addons.Housekeeping {
    //
    public static class VisitSummaryClass {
        //
        //=========================================================================================
        /// <summary>
        /// summarized visits hourly
        /// </summary>
        /// <param name="core"></param>
        /// <param name="env"></param>
        public static void summarizeDaily(CoreController core, HouseKeepEnvironmentModel env) {
            try {
                bool NewHour = (env.rightNow.Hour != env.LastCheckDateTime.Hour);
                if (env.force || NewHour) {
                    //
                    // Set NextSummaryStartDate based on the last time we ran hourly summarization
                    //
                    DateTime LastTimeSummaryWasRun = env.VisitArchiveDate;
                    using (var csData = new CsModel(core)) {
                        if (csData.openSql(core.db.getSQLSelect("ccVisitSummary", "DateAdded", "(timeduration=1)and(Dateadded>" + DbController.encodeSQLDate(env.VisitArchiveDate) + ")", "id Desc", "", 1))) {
                            LastTimeSummaryWasRun = csData.getDate("DateAdded");
                            LogController.logInfo(core, "Update hourly visit summary, last time summary was run was [" + LastTimeSummaryWasRun + "]");
                        } else {
                            LogController.logInfo(core, "Update hourly visit summary, no hourly summaries were found, set start to [" + LastTimeSummaryWasRun + "]");
                        }
                    }
                    DateTime NextSummaryStartDate = LastTimeSummaryWasRun;
                    //
                    // Each hourly entry includes visits that started during that hour, but we do not know when they finished (maybe during last hour)
                    //   Find the oldest starttime of all the visits with endtimes after the LastTimeSummaryWasRun. Resummarize all periods
                    //   from then to now
                    //
                    //   For the past 24 hours, find the oldest visit with the last viewing during the last hour
                    //
                    DateTime StartOfHour = (new DateTime(LastTimeSummaryWasRun.Year, LastTimeSummaryWasRun.Month, LastTimeSummaryWasRun.Day, LastTimeSummaryWasRun.Hour, 1, 1)).AddHours(-1); // (Int(24 * LastTimeSummaryWasRun) / 24) - PeriodStep
                    DateTime OldestDateAdded = StartOfHour;
                    using (var csData = new CsModel(core)) {
                        if (csData.openSql(core.db.getSQLSelect("ccVisits", "DateAdded", "LastVisitTime>" + DbController.encodeSQLDate(StartOfHour), "dateadded", "", 1))) {
                            OldestDateAdded = csData.getDate("DateAdded");
                            if (OldestDateAdded < NextSummaryStartDate) {
                                NextSummaryStartDate = OldestDateAdded;
                                LogController.logInfo(core, "Update hourly visit summary, found a visit with the last viewing during the past hour. It started [" + OldestDateAdded + "], before the last summary was run.");
                            }
                        }
                    }
                    //
                    // Verify there are 24 hour records for every day back the past 90 days
                    //
                    DateTime DateofMissingSummary = DateTime.MinValue;
                    //Call AppendClassLog(core, core.appEnvironment.name, "HouseKeep", "Verify there are 24 hour records for the past 90 days")
                    DateTime PeriodStartDate = env.rightNow.Date.AddDays(-90);
                    double PeriodStep = 1;
                    int HoursPerDay = 0;
                    for (double PeriodDatePtr = PeriodStartDate.ToOADate(); PeriodDatePtr <= OldestDateAdded.ToOADate(); PeriodDatePtr += PeriodStep) {
                        using (var csData = new CsModel(core)) {
                            if (csData.openSql("select count(id) as HoursPerDay from ccVisitSummary where TimeDuration=1 and DateNumber=" + encodeInteger(PeriodDatePtr) + " group by DateNumber")) {
                                HoursPerDay = csData.getInteger("HoursPerDay");
                            }
                            csData.close();
                            if (HoursPerDay < 24) {
                                DateofMissingSummary = DateTime.FromOADate(PeriodDatePtr);
                                break;
                            }
                        }
                        if ((DateofMissingSummary != DateTime.MinValue) && (DateofMissingSummary < NextSummaryStartDate)) {
                            LogController.logInfo(core, "Found a missing hourly period in the visit summary table [" + DateofMissingSummary + "], it only has [" + HoursPerDay + "] hourly summaries.");
                            NextSummaryStartDate = DateofMissingSummary;
                        }
                        //
                        // Now summarize all visits during all hourly periods between OldestDateAdded and the previous Hour
                        //
                        LogController.logInfo(core, "Summaryize visits hourly, starting [" + NextSummaryStartDate + "]");
                        PeriodStep = (double)1 / (double)24;
                        VisitSummaryClass.summarizePeriod(core, env, NextSummaryStartDate, env.rightNow, 1, core.siteProperties.dataBuildVersion, env.OldestVisitSummaryWeCareAbout);
                    }
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
            }
        }
        //
        //=========================================================================================
        // Summarize the visits
        //   excludes non-cookie visits
        //   excludes administrator and developer visits
        //   excludes authenticated users with ExcludeFromReporting
        //
        // Average time on site
        //
        //   Example data
        //   Pages       TimeToLastHit
        //   1           0           - hit 1 page, start time = last time
        //   10          3510        - hit 10 pages, first hit time - last hit time = 3510
        //   2           30          - hit 2 pages, first hit time - last hit time = 30
        //
        // AveReadTime is the average time spent reading pages
        //   this is calculated from the multi-page visits only
        //   = MultiPageTimeToLastHitSum / ( MultiPageHitCnt - MultiPageVisitCnt )
        //   = ( 3510 + 30 ) / ((10+2) - 2 )
        //   = 354
        //
        // TotalTimeOnSite is the total time people spent reading pages
        //   There are two parts:
        //     1) the TimeToLastHit, which covers all but the last hit of each visit
        //     2) assume the last hit of each visit is the AveReadTime
        //   = MultiPageTimeToLastHitSum + ( AveReadTime * VisitCnt )
        //   = ( 3510 + 30 ) + ( 354 * 3 )
        //   = 4602
        //
        // AveTimeOnSite
        //   = TotalTimeOnSite / TotalHits
        //   = 4602 / 3
        //   = 1534
        //
        //=========================================================================================
        //
        private static void summarizePeriod(CoreController core, HouseKeepEnvironmentModel env, DateTime StartTimeDate, DateTime EndTimeDate, int HourDuration, string BuildVersion, DateTime OldestVisitSummaryWeCareAbout) {
            try {
                double StartTimeHoursSinceMidnight = 0;
                DateTime PeriodStart = default(DateTime);
                double TotalTimeOnSite = 0;
                int MultiPageVisitCnt = 0;
                int MultiPageHitCnt = 0;
                double MultiPageTimetoLastHitSum = 0;
                double TimeOnSite = 0;
                DateTime PeriodDatePtr = default(DateTime);
                int DateNumber = 0;
                int TimeNumber = 0;
                DateTime DateStart = default(DateTime);
                DateTime DateEnd = default(DateTime);
                int NewVisitorVisits = 0;
                int SinglePageVisits = 0;
                int AuthenticatedVisits = 0;
                int MobileVisits = 0;
                int BotVisits = 0;
                int NoCookieVisits = 0;
                double AveTimeOnSite = 0;
                int HitCnt = 0;
                int VisitCnt = 0;
                XmlDocument LibraryCollections = new XmlDocument();
                XmlDocument LocalCollections = new XmlDocument();
                XmlDocument Doc = new XmlDocument();
                int AveReadTime = 0;
                //
                if (string.CompareOrdinal(BuildVersion, core.codeVersion()) < 0) {
                } else {
                    PeriodStart = StartTimeDate;
                    if (PeriodStart < OldestVisitSummaryWeCareAbout) {
                        PeriodStart = OldestVisitSummaryWeCareAbout;
                    }
                    StartTimeHoursSinceMidnight = PeriodStart.TimeOfDay.TotalHours;
                    PeriodStart = PeriodStart.Date.AddHours(StartTimeHoursSinceMidnight);
                    PeriodDatePtr = PeriodStart;
                    while (PeriodDatePtr < EndTimeDate) {
                        //
                        DateNumber = encodeInteger(PeriodDatePtr.AddHours(HourDuration / 2.0).ToOADate());
                        TimeNumber = encodeInteger(PeriodDatePtr.TimeOfDay.TotalHours);
                        DateStart = PeriodDatePtr.Date;
                        DateEnd = PeriodDatePtr.AddHours(HourDuration).Date;
                        //
                        VisitCnt = 0;
                        HitCnt = 0;
                        NewVisitorVisits = 0;
                        SinglePageVisits = 0;
                        MultiPageVisitCnt = 0;
                        MultiPageTimetoLastHitSum = 0;
                        AuthenticatedVisits = 0;
                        MobileVisits = 0;
                        BotVisits = 0;
                        NoCookieVisits = 0;
                        AveTimeOnSite = 0;
                        //
                        // No Cookie Visits
                        //
                        string SQL = "select count(v.id) as NoCookieVisits"
                            + " from ccvisits v"
                            + " where (v.CookieSupport<>1)"
                            + " and(v.dateadded>=" + DbController.encodeSQLDate(DateStart) + ")"
                            + " and (v.dateadded<" + DbController.encodeSQLDate(DateEnd) + ")"
                            + " and((v.ExcludeFromAnalytics is null)or(v.ExcludeFromAnalytics=0))"
                            + "";
                        using (var csData = new CsModel(core)) {
                            csData.openSql(SQL, "Default");
                            if (csData.ok()) {
                                NoCookieVisits = csData.getInteger("NoCookieVisits");
                            }
                        }
                        //
                        // Total Visits
                        //
                        SQL = "select count(v.id) as VisitCnt ,Sum(v.PageVisits) as HitCnt ,sum(v.TimetoLastHit) as TimeOnSite"
                            + " from ccvisits v"
                            + " where (v.CookieSupport<>0)"
                            + " and(v.dateadded>=" + DbController.encodeSQLDate(DateStart) + ")"
                            + " and (v.dateadded<" + DbController.encodeSQLDate(DateEnd) + ")"
                            + " and((v.ExcludeFromAnalytics is null)or(v.ExcludeFromAnalytics=0))"
                            + "";
                        using (var csData = new CsModel(core)) {
                            csData.openSql(SQL, "Default");
                            if (csData.ok()) {
                                VisitCnt = csData.getInteger("VisitCnt");
                                HitCnt = csData.getInteger("HitCnt");
                                TimeOnSite = csData.getNumber("TimeOnSite");
                            }
                        }
                        //
                        // Visits by new visitors
                        //
                        if (VisitCnt > 0) {
                            SQL = "select count(v.id) as NewVisitorVisits"
                                + " from ccvisits v"
                                + " where (v.CookieSupport<>0)"
                                + " and(v.dateadded>=" + DbController.encodeSQLDate(DateStart) + ")"
                                + " and (v.dateadded<" + DbController.encodeSQLDate(DateEnd) + ")"
                                + " and((v.ExcludeFromAnalytics is null)or(v.ExcludeFromAnalytics=0))"
                                + " and(v.VisitorNew<>0)"
                                + "";
                            using (var csData = new CsModel(core)) {
                                csData.openSql(SQL, "Default");
                                if (csData.ok()) {
                                    NewVisitorVisits = csData.getInteger("NewVisitorVisits");
                                }
                            }
                            //
                            // Single Page Visits
                            //
                            SQL = "select count(v.id) as SinglePageVisits"
                                + " from ccvisits v"
                                + " where (v.CookieSupport<>0)"
                                + " and(v.dateadded>=" + DbController.encodeSQLDate(DateStart) + ")"
                                + " and (v.dateadded<" + DbController.encodeSQLDate(DateEnd) + ")"
                                + " and((v.ExcludeFromAnalytics is null)or(v.ExcludeFromAnalytics=0))"
                                + " and(v.PageVisits=1)"
                                + "";
                            using (var csData = new CsModel(core)) {
                                csData.openSql(SQL, "Default");
                                if (csData.ok()) {
                                    SinglePageVisits = csData.getInteger("SinglePageVisits");
                                }
                            }
                            //
                            // Multipage Visits
                            //
                            SQL = "select count(v.id) as VisitCnt ,sum(v.PageVisits) as HitCnt ,sum(v.TimetoLastHit) as TimetoLastHitSum "
                                + " from ccvisits v"
                                + " where (v.CookieSupport<>0)"
                                + " and(v.dateadded>=" + DbController.encodeSQLDate(DateStart) + ")"
                                + " and (v.dateadded<" + DbController.encodeSQLDate(DateEnd) + ")"
                                + " and((v.ExcludeFromAnalytics is null)or(v.ExcludeFromAnalytics=0))"
                                + " and(PageVisits>1)"
                                + "";
                            using (var csData = new CsModel(core)) {
                                csData.openSql(SQL, "Default");
                                if (csData.ok()) {
                                    MultiPageVisitCnt = csData.getInteger("VisitCnt");
                                    MultiPageHitCnt = csData.getInteger("HitCnt");
                                    MultiPageTimetoLastHitSum = csData.getNumber("TimetoLastHitSum");
                                }
                            }
                            //
                            // Authenticated Visits
                            //
                            SQL = "select count(v.id) as AuthenticatedVisits "
                                + " from ccvisits v"
                                + " where (v.CookieSupport<>0)"
                                + " and(v.dateadded>=" + DbController.encodeSQLDate(DateStart) + ")"
                                + " and (v.dateadded<" + DbController.encodeSQLDate(DateEnd) + ")"
                                + " and((v.ExcludeFromAnalytics is null)or(v.ExcludeFromAnalytics=0))"
                                + " and(VisitAuthenticated<>0)"
                                + "";
                            using (var csData = new CsModel(core)) {
                                csData.openSql(SQL, "Default");
                                if (csData.ok()) {
                                    AuthenticatedVisits = csData.getInteger("AuthenticatedVisits");
                                }
                            }
                            // 
                            //
                            // Mobile Visits
                            //
                            SQL = "select count(v.id) as cnt "
                                + " from ccvisits v"
                                + " where (v.CookieSupport<>0)"
                                + " and(v.dateadded>=" + DbController.encodeSQLDate(DateStart) + ")"
                                + " and (v.dateadded<" + DbController.encodeSQLDate(DateEnd) + ")"
                                + " and((v.ExcludeFromAnalytics is null)or(v.ExcludeFromAnalytics=0))"
                                + " and(Mobile<>0)"
                                + "";
                            using (var csData = new CsModel(core)) {
                                csData.openSql(SQL, "Default");
                                if (csData.ok()) {
                                    MobileVisits = csData.getInteger("cnt");
                                }
                            }
                            //
                            // Bot Visits
                            //
                            SQL = "select count(v.id) as cnt "
                                + " from ccvisits v"
                                + " where (v.CookieSupport<>0)"
                                + " and(v.dateadded>=" + DbController.encodeSQLDate(DateStart) + ")"
                                + " and (v.dateadded<" + DbController.encodeSQLDate(DateEnd) + ")"
                                + " and((v.ExcludeFromAnalytics is null)or(v.ExcludeFromAnalytics=0))"
                                + " and(Bot<>0)"
                                + "";
                            using (var csData = new CsModel(core)) {
                                csData.openSql(SQL, "Default");
                                if (csData.ok()) {
                                    BotVisits = csData.getInteger("cnt");
                                }
                            }
                            //
                            if ((MultiPageHitCnt > MultiPageVisitCnt) && (HitCnt > 0)) {
                                AveReadTime = encodeInteger(MultiPageTimetoLastHitSum / (MultiPageHitCnt - MultiPageVisitCnt));
                                TotalTimeOnSite = MultiPageTimetoLastHitSum + (AveReadTime * VisitCnt);
                                AveTimeOnSite = TotalTimeOnSite / VisitCnt;
                            }
                        }
                        //
                        // Add or update the Visit Summary Record
                        //
                        using (var csData = new CsModel(core)) {
                            csData.open("Visit Summary", "(timeduration=" + HourDuration + ")and(DateNumber=" + DateNumber + ")and(TimeNumber=" + TimeNumber + ")");
                            if (!csData.ok()) {
                                csData.close();
                                csData.insert("Visit Summary");
                            }
                            //
                            if (csData.ok()) {
                                csData.set("name", HourDuration + " hr summary for " + DateTime.FromOADate(DateNumber).ToShortDateString() + " " + TimeNumber + ":00");
                                csData.set("DateNumber", DateNumber);
                                csData.set("TimeNumber", TimeNumber);
                                csData.set("Visits", VisitCnt);
                                csData.set("PagesViewed", HitCnt);
                                csData.set("TimeDuration", HourDuration);
                                csData.set("NewVisitorVisits", NewVisitorVisits);
                                csData.set("SinglePageVisits", SinglePageVisits);
                                csData.set("AuthenticatedVisits", AuthenticatedVisits);
                                csData.set("NoCookieVisits", NoCookieVisits);
                                csData.set("AveTimeOnSite", AveTimeOnSite);
                                if (true) {
                                    csData.set("MobileVisits", MobileVisits);
                                    csData.set("BotVisits", BotVisits);
                                }
                            }
                        }
                        PeriodDatePtr = PeriodDatePtr.AddHours(HourDuration);
                    }
                    {
                        //
                        // Delete any daily visit summary duplicates during this period(keep the first)
                        //
                        string SQL = "delete from ccvisitsummary"
                            + " where id in ("
                            + " select d.id from ccvisitsummary d,ccvisitsummary f"
                            + " where f.datenumber=d.datenumber"
                            + " and f.datenumber>" + env.OldestVisitSummaryWeCareAbout.ToOADate() + " and f.datenumber<" + env.Yesterday.ToOADate() + " and f.TimeDuration=24"
                            + " and d.TimeDuration=24"
                            + " and f.id<d.id"
                            + ")";
                        core.db.executeQuery(SQL);
                        //
                        // Find missing daily summaries, summarize that date
                        //
                        SQL = core.db.getSQLSelect("ccVisitSummary", "DateNumber", "TimeDuration=24 and DateNumber>=" + env.OldestVisitSummaryWeCareAbout.Date.ToOADate(), "DateNumber,TimeNumber");
                        using (var csData = new CsModel(core)) {
                            csData.openSql(SQL, "Default");
                            DateTime datePtr = env.OldestVisitSummaryWeCareAbout;
                            while (datePtr <= env.Yesterday) {
                                if (!csData.ok()) {
                                    //
                                    // Out of data, start with this DatePtr
                                    //
                                    VisitSummaryClass.summarizePeriod(core, env, datePtr, datePtr, 24, core.siteProperties.dataBuildVersion, env.OldestVisitSummaryWeCareAbout);
                                    //Exit For
                                } else {
                                    DateTime workingDate = DateTime.MinValue.AddDays(csData.getInteger("DateNumber"));
                                    if (datePtr < workingDate) {
                                        //
                                        // There are missing dates, update them
                                        //
                                        VisitSummaryClass.summarizePeriod(core, env, datePtr, workingDate.AddDays(-1), 24, core.siteProperties.dataBuildVersion, env.OldestVisitSummaryWeCareAbout);
                                    }
                                }
                                if (csData.ok()) {
                                    //
                                    // if there is more data, go to the next record
                                    //
                                    csData.goNext();
                                }
                                datePtr = datePtr.AddDays(1).Date;
                            }
                            csData.close();
                        }
                    }

                }
                //
                return;
            } catch (Exception ex) {
                LogController.logError(core, ex);
            }
        }

    }
}