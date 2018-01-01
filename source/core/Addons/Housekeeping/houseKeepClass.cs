﻿
using System;
using System.Reflection;
using System.Xml;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Contensive.BaseClasses;
using Contensive.Core;
using Contensive.Core.Models.Entity;
using Contensive.Core.Controllers;
using static Contensive.Core.Controllers.genericController;
using static Contensive.Core.constants;
//
using System.IO;
//
namespace Contensive.Core.Addons.Housekeeping {
    //
    //====================================================================================================
    /// <summary>
    /// support for housekeeping functions
    /// </summary>
    public class houseKeepClass : Contensive.BaseClasses.AddonBaseClass {
        //
        private DateTime LogCheckDateLast;
        //
        //====================================================================================================
        /// <summary>
        /// addon interface
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public override object Execute(CPBaseClass cp) {
            string result = "";
            try {
                //
                // -- ok to cast cpbase to cp because they build from the same solution
                //this.cp = (CPClass)cp;
                coreClass cpCore = ((CPClass)cp).core;
                HouseKeep(cpCore, false);
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
            }
            return result;
        }        //
        //
        //
        public void HouseKeep(coreClass cpCore, bool DebugMode) {
            int EmailDropArchiveAgeDays = 0;
            int Pos = 0;
            string DomainNamePrimary = null;
            DateTime workingDate = default(DateTime);
            DateTime OldestVisitSummaryWeCareAbout = default(DateTime);
            string DefaultMemberName = null;
            int PeopleCID = 0;
            string RegisterList = "";
            string Content = null;
            string ErrorMessage = null;
            DateTime Yesterday = default(DateTime);
            DateTime LastTimeSummaryWasRun = default(DateTime);
            DateTime NextSummaryStartDate = default(DateTime);
            DateTime ALittleWhileAgo = default(DateTime);
            DateTime PeriodStartDate = default(DateTime);
            double PeriodDatePtr = 0;
            double PeriodStep = 0;
            DateTime StartOfHour = default(DateTime);
            int HoursPerDay = 0;
            DateTime OldestDateAdded = default(DateTime);
            object emptyData = null;
            bool NeedToClearCache = false;
            int ArchiveParentID = 0;
            int RecordID = 0;
            int CS = 0;
            XmlDocument LibraryCollections = new XmlDocument();
            XmlDocument LocalCollections = new XmlDocument();
            XmlDocument Doc = new XmlDocument();
            string AlarmTimeString = null;
            double AlarmTimeMinutesSinceMidnight = 0;
            string FolderName = null;
            int VisitArchiveAgeDays = 0;
            int GuestArchiveAgeDays = 0;
            DateTime VisitArchiveDate = default(DateTime);
            bool RunServerHousekeep = false;
            bool NewHour = false;
            DateTime rightNow = default(DateTime);
            double LastCheckMinutesFromMidnight = 0;
            double minutesSinceMidnight = 0;
            string ConfigFilename = null;
            string Config = null;
            string[] ConfigLines = null;
            string Line = null;
            int LineCnt = 0;
            int LinePtr = 0;
            string[] NameValue = null;
            string SQLNow = null;
            string SQL = null;
            int DataSourceType = 0;
            CPClass cp = null;
            List<string> nonCriticalErrorList = new List<string>();
            //
            // put token in a config file
            //
            cp = new CPClass();
            //
            rightNow = DateTime.Now;
            Yesterday = rightNow.AddDays(-1).Date;
            ALittleWhileAgo = rightNow.AddDays(-90).Date;
            SQLNow = cpCore.db.encodeSQLDate(rightNow);
            DateTime LastCheckDateTime = cpCore.siteProperties.getDate("housekeep, last check", default(DateTime));
            int ServerHousekeepHour = cpCore.siteProperties.getInteger("housekeep, run time hour", 2);
            //
            // ----- Run Server Housekeep
            //
            if (rightNow.Date > LastCheckDateTime.Date) {
                //
                // new day since lastcheck, is alarm less then now
                //
                RunServerHousekeep = (ServerHousekeepHour < rightNow.Hour);
                //} else {
                //    //
                //    // same day as lastcheck, is alarm between now and last time check
                //    //
                //    RunServerHousekeep = (rightNow > ServerHousekeepHour) && (LastCheckDateTime < ServerHousekeepHour);
            }
            NewHour = rightNow.Hour != LastCheckDateTime.Hour;
            if (DebugMode || RunServerHousekeep) {
                cpCore.siteProperties.setProperty("housekeep, last check", rightNow);
                //
                // it is the next day, remove old log files
                //
                logController.housekeepLogFolder(cpCore);
                //
                // Download Updates
                //
                DownloadUpdates(cpCore);
                //
                // Set LogCheckDate
                //
                LogCheckDateLast = DateTime.Now.Date;
            }
            //
            // Register and unregister files in the Addon folder
            //
            housekeepAddonFolder(cpCore);
            //
            // Upgrade Local Collections, and all applications that use them
            //
            ErrorMessage = "";
            AppendClassLog(cpCore, "", "HouseKeep", "Updating local collections from library, see Upgrade log for details during this period.");
            string ignoreRefactorText = "";
            bool ignoreRefactorBoolean = false;
            if (!collectionController.UpgradeLocalCollectionRepoFromRemoteCollectionRepo(cpCore, ref ErrorMessage, ref ignoreRefactorText, ref ignoreRefactorBoolean, false, ref nonCriticalErrorList)) {
                if (string.IsNullOrEmpty(ErrorMessage)) {
                    ErrorMessage = "No detailed error message was returned from UpgradeAllLocalCollectionsFromLib2 although it returned 'not ok' status.";
                }
                AppendClassLog(cpCore, "", "HouseKeep", "Updating local collections from Library returned an error, " + ErrorMessage);
            }
            //
            // Verify core installation
            //
            collectionController.installCollectionFromRemoteRepo(cpCore, CoreCollectionGuid, ref ErrorMessage, "", false, ref nonCriticalErrorList);
            //
            DomainNamePrimary = cpCore.serverConfig.appConfig.domainList[0];
            Pos = genericController.vbInstr(1, DomainNamePrimary, ",");
            if (Pos > 1) {
                DomainNamePrimary = DomainNamePrimary.Left(Pos - 1);
            }
            //dataBuildVersion = cpCore.app.getSiteProperty("BuildVersion", "0")
            DataSourceType = cpCore.db.getDataSourceType("default");
            //
            DefaultMemberName = "";
            PeopleCID = Models.Complex.cdefModel.getContentId(cpCore, "people");
            SQL = "select defaultvalue from ccfields where name='name' and contentid=(" + PeopleCID + ")";
            CS = cpCore.db.csOpenSql_rev("default", SQL);
            if (cpCore.db.csOk(CS)) {
                DefaultMemberName = cpCore.db.csGetText(CS, "defaultvalue");
            }
            cpCore.db.csClose(ref CS);
            //
            // Get ArchiveAgeDays - use this as the oldest data they care about
            //
            VisitArchiveAgeDays = genericController.encodeInteger(cpCore.siteProperties.getText("ArchiveRecordAgeDays", "365"));
            if (VisitArchiveAgeDays < 2) {
                VisitArchiveAgeDays = 2;
                cpCore.siteProperties.setProperty("ArchiveRecordAgeDays", "2");
            }
            VisitArchiveDate = rightNow.AddDays(-VisitArchiveAgeDays).Date;
            OldestVisitSummaryWeCareAbout = DateTime.Now.Date.AddDays(-120);
            if (OldestVisitSummaryWeCareAbout < VisitArchiveDate) {
                OldestVisitSummaryWeCareAbout = VisitArchiveDate;
            }
            //OldestVisitSummaryWeCareAbout = now.date - VisitArchiveAgeDays
            //
            // Get GuestArchiveAgeDays
            //
            GuestArchiveAgeDays = genericController.encodeInteger(cpCore.siteProperties.getText("ArchivePeopleAgeDays", "2"));
            if (GuestArchiveAgeDays < 2) {
                GuestArchiveAgeDays = 2;
                cpCore.siteProperties.setProperty("ArchivePeopleAgeDays", GuestArchiveAgeDays.ToString());
            }
            //
            // Get EmailDropArchiveAgeDays
            //
            EmailDropArchiveAgeDays = genericController.encodeInteger(cpCore.siteProperties.getText("ArchiveEmailDropAgeDays", "90"));
            if (EmailDropArchiveAgeDays < 2) {
                EmailDropArchiveAgeDays = 2;
                cpCore.siteProperties.setProperty("ArchiveEmailDropAgeDays", EmailDropArchiveAgeDays.ToString());
            }
            //
            // Do non-optional housekeeping
            //
            if (RunServerHousekeep || DebugMode) {
                if (true) // 3.3.971" Then
                {
                    //
                    // Move Archived pages from their current parent to their archive parent
                    //
                    AppendClassLog(cpCore, cpCore.serverConfig.appConfig.name, "HouseKeep", "Archive update for pages on [" + cpCore.serverConfig.appConfig.name + "]");
                    SQL = "select * from ccpagecontent where (( DateArchive is not null )and(DateArchive<" + SQLNow + "))and(active<>0)";
                    CS = cpCore.db.csOpenSql_rev("default", SQL);
                    while (cpCore.db.csOk(CS)) {
                        ArchiveParentID = cpCore.db.csGetInteger(CS, "ArchiveParentID");
                        if (ArchiveParentID == 0) {
                            SQL = "update ccpagecontent set DateArchive=null where (id=" + RecordID + ")";
                            cpCore.db.executeQuery(SQL);
                        } else {
                            RecordID = cpCore.db.csGetInteger(CS, "ID");
                            SQL = "update ccpagecontent set ArchiveParentID=null,DateArchive=null,parentid=" + ArchiveParentID + " where (id=" + RecordID + ")";
                            cpCore.db.executeQuery(SQL);
                            NeedToClearCache = true;
                        }
                        cpCore.db.csGoNext(CS);
                    }
                    cpCore.db.csClose(ref CS);
                    //
                    // Clear caches
                    //
                    if (NeedToClearCache) {
                        emptyData = null;
                        cpCore.cache.invalidate("Page Content");
                        cpCore.cache.setObject("PCC", emptyData);
                    }
                }
                if (true) {
                    //
                    // Delete any daily visit summary duplicates during this period(keep the first)
                    //
                    SQL = "delete from ccvisitsummary"
                        + " where id in ("
                        + " select d.id from ccvisitsummary d,ccvisitsummary f"
                        + " where f.datenumber=d.datenumber"
                        + " and f.datenumber>" + cpCore.db.encodeSQLDate(OldestVisitSummaryWeCareAbout) + " and f.datenumber<" + cpCore.db.encodeSQLDate(Yesterday) + " and f.TimeDuration=24"
                        + " and d.TimeDuration=24"
                        + " and f.id<d.id"
                        + ")";
                    cpCore.db.executeQuery(SQL);
                    //
                    // Find missing daily summaries, summarize that date
                    //
                    SQL = cpCore.db.GetSQLSelect("default", "ccVisitSummary", "DateNumber", "TimeDuration=24 and DateNumber>=" + OldestVisitSummaryWeCareAbout.Date.ToOADate(), "DateNumber,TimeNumber");
                    CS = cpCore.db.csOpenSql_rev("default", SQL);
                    DateTime datePtr = OldestVisitSummaryWeCareAbout;
                    while (datePtr <= Yesterday) {
                        if (!cpCore.db.csOk(CS)) {
                            //
                            // Out of data, start with this DatePtr
                            //
                            HouseKeep_VisitSummary(cpCore, datePtr, datePtr, 24, cpCore.siteProperties.dataBuildVersion, OldestVisitSummaryWeCareAbout);
                            //Exit For
                        } else {
                            workingDate = DateTime.MinValue.AddDays(cpCore.db.csGetInteger(CS, "DateNumber"));
                            if (datePtr < workingDate) {
                                //
                                // There are missing dates, update them
                                //
                                HouseKeep_VisitSummary(cpCore, datePtr, workingDate.AddDays(-1), 24, cpCore.siteProperties.dataBuildVersion, OldestVisitSummaryWeCareAbout);
                            }
                        }
                        if (cpCore.db.csOk(CS)) {
                            //
                            // if there is more data, go to the next record
                            //
                            cpCore.db.csGoNext(CS);
                        }
                        datePtr = datePtr.AddDays(1).Date;
                    }
                    cpCore.db.csClose(ref CS);
                }
                //
                // Remote Query Expiration
                //
                SQL = "delete from ccRemoteQueries where (DateExpires is not null)and(DateExpires<" + cpCore.db.encodeSQLDate(DateTime.Now) + ")";
                cpCore.db.executeQuery(SQL);
                if (true) {
                    //
                    // Clean Navigation
                    //
                    if (DataSourceType == DataSourceTypeODBCMySQL) {
                        SQL = "delete m from ccmenuEntries m left join ccAggregateFunctions a on a.id=m.AddonID where m.addonid<>0 and a.id is null";
                    } else {
                        SQL = "delete from ccmenuEntries where id in (select m.ID from ccMenuEntries m left join ccAggregateFunctions a on a.id=m.AddonID where m.addonid<>0 and a.id is null)";
                    }
                    cpCore.db.executeQuery(SQL);
                    //
                    if (DataSourceType == DataSourceTypeODBCMySQL) {
                        SQL = "delete m from ccmenuEntries m left join ccAggregateFunctions a on a.id=m.helpaddonid where m.helpaddonid<>0 and a.id is null";
                    } else {
                        SQL = "delete from ccmenuEntries where id in (select m.ID from ccMenuEntries m left join ccAggregateFunctions a on a.id=m.helpaddonid where m.helpaddonid<>0 and a.id is null)";
                    }
                    cpCore.db.executeQuery(SQL);
                    //
                    if (DataSourceType == DataSourceTypeODBCMySQL) {
                        SQL = "delete m from ccmenuEntries m left join ccAggregateFunctions a on a.id=m.helpcollectionid where m.helpcollectionid<>0 and a.id is null";
                    } else {
                        SQL = "delete from ccmenuEntries where id in (select m.ID from ccMenuEntries m left join ccAddonCollections c on c.id=m.helpcollectionid Where m.helpcollectionid <> 0 And c.Id Is Null)";
                    }
                    cpCore.db.executeQuery(SQL);
                }
                //
                // Page View Summary
                //
                if (true) // 4.1.187" Then
                {
                    //
                    // Delete duplicates
                    //
                    //SQL = "delete from ccviewingsummary" _
                    //    & " where id in (" _
                    //    & " select d.id from ccviewingsummary d,ccviewingsummary f" _
                    //    & " where f.datenumber=d.datenumber" _
                    //    & " and f.datenumber>" & encodeSQLDate(OldestVisitSummaryWeCareAbout) _
                    //    & " and f.datenumber<" & encodeSQLDate(Yesterday) _
                    //    & " and f.TimeDuration=24" _
                    //    & " and d.TimeDuration=24" _
                    //    & " and f.id<d.id" _
                    //    & ")"
                    //Call cpCore.app.ExecuteSQL( SQL)
                    //
                    // Find the day of the last entry in the viewing summary table as start there
                    // PageViewSummary should always add at least one entry for each day, even if 0
                    //
                    if (true) {
                        DateTime datePtr = default(DateTime);
                        SQL = cpCore.db.GetSQLSelect("default", "ccviewingsummary", "DateNumber", "TimeDuration=24 and DateNumber>=" + OldestVisitSummaryWeCareAbout.Date.ToOADate(), "DateNumber Desc", "", 1);
                        CS = cpCore.db.csOpenSql_rev("default", SQL);
                        if (!cpCore.db.csOk(CS)) {
                            datePtr = OldestVisitSummaryWeCareAbout;
                        } else {
                            datePtr = DateTime.MinValue.AddDays(cpCore.db.csGetInteger(CS, "DateNumber"));
                        }
                        cpCore.db.csClose(ref CS);
                        if (datePtr < OldestVisitSummaryWeCareAbout) {
                            datePtr = OldestVisitSummaryWeCareAbout;
                        }
                        HouseKeep_PageViewSummary(cpCore, datePtr, Yesterday, 24, cpCore.siteProperties.dataBuildVersion, OldestVisitSummaryWeCareAbout);
                    }
                }
            }
            //
            // Each hour, summarize the visits and viewings into the Visit Summary table
            //
            if (DebugMode || NewHour) {
                //
                // Set NextSummaryStartDate based on the last time we ran hourly summarization
                //
                LastTimeSummaryWasRun = VisitArchiveDate;
                //LastTimeSummaryWasRun = ALittleWhileAgo
                //sql="select top 1 dateadded from ccvisitsummary where (timeduration=1)and(Dateadded>" & encodeSQLDate(ALittleWhileAgo) & ") order by id desc"
                SQL = cpCore.db.GetSQLSelect("default", "ccVisitSummary", "DateAdded", "(timeduration=1)and(Dateadded>" + cpCore.db.encodeSQLDate(VisitArchiveDate) + ")", "id Desc", "", 1);
                //SQL = cpCore.app.csv_GetSQLSelect("default", "ccVisitSummary", "DateAdded", "(timeduration=1)and(Dateadded>" & encodeSQLDate(ALittleWhileAgo) & ")", "id Desc", , 1)
                CS = cpCore.db.csOpenSql_rev("default", SQL);
                if (cpCore.db.csOk(CS)) {
                    LastTimeSummaryWasRun = cpCore.db.csGetDate(CS, "DateAdded");
                    AppendClassLog(cpCore, cpCore.serverConfig.appConfig.name, "HouseKeep", "Update hourly visit summary, last time summary was run was [" + LastTimeSummaryWasRun + "]");
                } else {
                    AppendClassLog(cpCore, cpCore.serverConfig.appConfig.name, "HouseKeep", "Update hourly visit summary, no hourly summaries were found, set start to [" + LastTimeSummaryWasRun + "]");
                }
                cpCore.db.csClose(ref CS);
                NextSummaryStartDate = LastTimeSummaryWasRun;
                //
                // Each hourly entry includes visits that started during that hour, but we do not know when they finished (maybe during last hour)
                //   Find the oldest starttime of all the visits with endtimes after the LastTimeSummaryWasRun. Resummarize all periods
                //   from then to now
                //
                //   For the past 24 hours, find the oldest visit with the last viewing during the last hour
                //
                //OldestDateAdded = LastTimeSummaryWasRun
                //PeriodStep = CDbl(1) / CDbl(24)
                StartOfHour = (new DateTime(LastTimeSummaryWasRun.Year, LastTimeSummaryWasRun.Month, LastTimeSummaryWasRun.Day, LastTimeSummaryWasRun.Hour, 1, 1)).AddHours(-1); // (Int(24 * LastTimeSummaryWasRun) / 24) - PeriodStep
                OldestDateAdded = StartOfHour;
                SQL = cpCore.db.GetSQLSelect("default", "ccVisits", "DateAdded", "LastVisitTime>" + cpCore.db.encodeSQLDate(StartOfHour), "dateadded", "", 1);
                //SQL = "select top 1 Dateadded from ccvisits where LastVisitTime>" & encodeSQLDate(StartOfHour) & " order by DateAdded"
                CS = cpCore.db.csOpenSql_rev("default", SQL);
                if (cpCore.db.csOk(CS)) {
                    OldestDateAdded = cpCore.db.csGetDate(CS, "DateAdded");
                    if (OldestDateAdded < NextSummaryStartDate) {
                        NextSummaryStartDate = OldestDateAdded;
                        AppendClassLog(cpCore, cpCore.serverConfig.appConfig.name, "HouseKeep", "Update hourly visit summary, found a visit with the last viewing during the past hour. It started [" + OldestDateAdded + "], before the last summary was run.");
                    }
                }
                cpCore.db.csClose(ref CS);
                //
                // Verify there are 24 hour records for every day back the past 90 days
                //
                DateTime DateofMissingSummary = DateTime.MinValue;
                //Call AppendClassLog(cpCore, cpCore.appEnvironment.name, "HouseKeep", "Verify there are 24 hour records for the past 90 days")
                PeriodStartDate = rightNow.Date.AddDays(-90);
                PeriodStep = 1;
                //INSTANT C# TODO TASK: The step increment was not confirmed to be positive - confirm that the stopping condition is appropriate:
                //ORIGINAL LINE: For PeriodDatePtr = PeriodStartDate.ToOADate To OldestDateAdded.ToOADate Step PeriodStep
                for (PeriodDatePtr = PeriodStartDate.ToOADate(); PeriodDatePtr <= OldestDateAdded.ToOADate(); PeriodDatePtr += PeriodStep) {
                    SQL = "select count(id) as HoursPerDay from ccVisitSummary where TimeDuration=1 and DateNumber=" + encodeInteger(PeriodDatePtr) + " group by DateNumber";
                    //SQL = "select count(id) as HoursPerDay from ccVisitSummary group by DateNumber having DateNumber=" & CLng(PeriodDatePtr)
                    CS = cpCore.db.csOpenSql_rev("default", SQL);
                    HoursPerDay = 0;
                    if (cpCore.db.csOk(CS)) {
                        HoursPerDay = cpCore.db.csGetInteger(CS, "HoursPerDay");
                    }
                    cpCore.db.csClose(ref CS);
                    if (HoursPerDay < 24) {
                        DateofMissingSummary = DateTime.FromOADate(PeriodDatePtr);
                        break;
                    }
                }
                if ((DateofMissingSummary != DateTime.MinValue) && (DateofMissingSummary < NextSummaryStartDate)) {
                    AppendClassLog(cpCore, cpCore.serverConfig.appConfig.name, "HouseKeep", "Found a missing hourly period in the visit summary table [" + DateofMissingSummary + "], it only has [" + HoursPerDay + "] hourly summaries.");
                    NextSummaryStartDate = DateofMissingSummary;
                }
                //
                // Now summarize all visits during all hourly periods between OldestDateAdded and the previous Hour
                //
                AppendClassLog(cpCore, cpCore.serverConfig.appConfig.name, "HouseKeep", "Summaryize visits hourly, starting [" + NextSummaryStartDate + "]");
                PeriodStep = (double)1 / (double)24;
                //PeriodStart = (Int(OldestDateAdded * 24) / 24)
                HouseKeep_VisitSummary(cpCore, NextSummaryStartDate, rightNow, 1, cpCore.siteProperties.dataBuildVersion, OldestVisitSummaryWeCareAbout);
            }
            //
            // OK to run archive
            // During archive, non-cookie records are removed, so this has to run after summarizing
            // and we can only delete non-cookie records older than 2 days (so we can be sure they have been summarized)
            //
            if (DebugMode) {
                //
                // debug mode - run achive if no times are given
                //
                HouseKeep_App_Daily(cpCore, VisitArchiveAgeDays, GuestArchiveAgeDays, EmailDropArchiveAgeDays, DefaultMemberName, cpCore.siteProperties.dataBuildVersion);
            } else {
                //
                // Check for site's archive time of day
                //
                AlarmTimeString = cpCore.siteProperties.getText("ArchiveTimeOfDay", "12:00:00 AM");
                if (string.IsNullOrEmpty(AlarmTimeString)) {
                    AlarmTimeString = "12:00:00 AM";
                    cpCore.siteProperties.setProperty("ArchiveTimeOfDate", AlarmTimeString);
                }
                if (!dateController.IsDate(AlarmTimeString)) {
                    AlarmTimeString = "12:00:00 AM";
                    cpCore.siteProperties.setProperty("ArchiveTimeOfDate", AlarmTimeString);
                }
                AlarmTimeMinutesSinceMidnight = genericController.encodeDate(AlarmTimeString).TimeOfDay.TotalMinutes;
                minutesSinceMidnight = rightNow.TimeOfDay.TotalMinutes;
                LastCheckMinutesFromMidnight = LastCheckDateTime.TimeOfDay.TotalMinutes;
                if ((minutesSinceMidnight > LastCheckMinutesFromMidnight) && (LastCheckMinutesFromMidnight < minutesSinceMidnight)) {
                    //
                    // Same Day - Midnight is before last and after current
                    //
                    HouseKeep_App_Daily(cpCore, VisitArchiveAgeDays, GuestArchiveAgeDays, EmailDropArchiveAgeDays, DefaultMemberName, cpCore.siteProperties.dataBuildVersion);
                } else if ((LastCheckMinutesFromMidnight > minutesSinceMidnight) && ((LastCheckMinutesFromMidnight < minutesSinceMidnight))) {
                    //
                    // New Day - Midnight is between Last and Set
                    //
                    HouseKeep_App_Daily(cpCore, VisitArchiveAgeDays, GuestArchiveAgeDays, EmailDropArchiveAgeDays, DefaultMemberName, cpCore.siteProperties.dataBuildVersion);
                }
            }
        }
        //
        //
        //
        private void HouseKeep_App_Daily(coreClass cpCore, int VisitArchiveAgeDays, int GuestArchiveAgeDays, int EmailDropArchiveAgeDays, string DefaultMemberName, string BuildVersion) {
            try {
                //
                DateTime ArchiveEmailDropDate = default(DateTime);
                string VirtualFileName = null;
                string VirtualLink = null;
                string FilenameOriginal = null;
                int Pos = 0;
                string FilenameExt = null;
                string FilenameNoExt = null;
                string FilenameAltSize = null;
                FileInfo[] FileList = null;
                //
                long FileSize = 0;
                string PathNameRev = null;
                string[] FilenameDim = null;
                //
                int DaystoRemove = 0;
                int fieldType = 0;
                int FieldContentID = 0;
                string FieldCaption = null;
                string FieldLast = null;
                string FieldNew = null;
                int FieldRecordID = 0;
                int RecordID = 0;
                int ArchiveParentID = 0;
                DateTime OldestVisitDate = default(DateTime);
                DateTime ArchiveDate = default(DateTime);
                DateTime thirtyDaysAgo = default(DateTime);
                DateTime SingleDate = default(DateTime);
                int DataSourceType = 0;
                //
                //Dim Controller As controlClass
                int VisitArchiveDeleteSize = 0;
                //Dim AppService As appServicesClass
                //Dim KernelService As KernelServicesClass
                //    Dim CSConnection As appEnvironmentStruc
                string SQL = null;
                string SQLCriteria = null;
                string PathName = null;
                string TableName = null;
                string FieldName = null;
                int CS = 0;
                int CSTest = 0;
                string Filename = null;
                string[] FileSplit = null;
                string FolderName = null;
                string FolderList = null;
                string[] FolderArray = null;
                int FolderArrayCount = 0;
                int FolderArrayPointer = 0;
                string[] FolderSplit = null;
                int AdminLicenseCount = 0;
                string ArchiveDateSQL = null;
                string appName = null;
                string SQLTablePeople = null;
                string SQLTableMemberRules = null;
                string SQLTableGroups = null;
                int PeopleCID = 0;
                //Dim DefaultName As String
                string Hint = null;
                bool ArchiveDeleteNoCookie = false;
                DateTime MidnightTwoDaysAgo = default(DateTime);
                string SQLDateMidnightTwoDaysAgo = null;
                int TimeoutSave = 0;
                DateTime Yesterday = default(DateTime);
                DateTime rightNow = DateTime.Now;
                //
                Yesterday = rightNow.AddDays(-1).Date;
                MidnightTwoDaysAgo = rightNow.AddDays(-2).Date;
                thirtyDaysAgo = rightNow.AddDays(-30).Date;
                appName = cpCore.serverConfig.appConfig.name;
                ArchiveDeleteNoCookie = genericController.encodeBoolean(cpCore.siteProperties.getText("ArchiveDeleteNoCookie", "1"));
                DataSourceType = cpCore.db.getDataSourceType("default");
                TimeoutSave = cpCore.db.sqlCommandTimeout;
                cpCore.db.sqlCommandTimeout = 1800;
                //
                SQLTablePeople = Models.Complex.cdefModel.getContentTablename(cpCore, "People");
                SQLTableMemberRules = Models.Complex.cdefModel.getContentTablename(cpCore, "Member Rules");
                SQLTableGroups = Models.Complex.cdefModel.getContentTablename(cpCore, "Groups");
                SQLDateMidnightTwoDaysAgo = cpCore.db.encodeSQLDate(MidnightTwoDaysAgo);
                //
                // Any member records that were created outside contensive need to have CreatedByVisit=0 (past v4.1.152)
                cpCore.db.executeQuery("update ccmembers set CreatedByVisit=0 where createdbyvisit is null");
                //
                // delete nocookie visits
                // This must happen after the housekeep summarizing, and no sooner then 48 hours ago so all hits have been summarized before deleting
                //
                if (ArchiveDeleteNoCookie) {
                    //
                    // delete members from the non-cookie visits
                    // legacy records without createdbyvisit will have to be corrected by hand (or upgrade)
                    //
                    AppendClassLog(cpCore, appName, "HouseKeep_App_Daily(" + appName + ")", "Deleting members from visits with no cookie support older than Midnight, Two Days Ago");
                    switch (DataSourceType) {
                        case DataSourceTypeODBCAccess:
                            SQL = "delete m.*"
                                + " from " + SQLTablePeople + " m,ccvisits v"
                                + " where v.memberid=m.id"
                                + " and(m.Visits=1)"
                                + " and(m.createdbyvisit=1)"
                                + " and(m.Username is null)"
                                + " and(m.email is null)"
                                + " and(v.CookieSupport=0)and(v.LastVisitTime<" + SQLDateMidnightTwoDaysAgo + ")";
                            break;
                        case DataSourceTypeODBCMySQL:
                            SQL = "delete m"
                                + " from " + SQLTablePeople + " m,ccvisits v"
                                + " where v.memberid=m.id"
                                + " and(m.Visits=1)"
                                + " and(m.createdbyvisit=1)"
                                + " and(m.Username is null)"
                                + " and(m.email is null)"
                                + " and(v.CookieSupport=0)and(v.LastVisitTime<" + SQLDateMidnightTwoDaysAgo + ")";
                            break;
                        default:
                            SQL = "delete from " + SQLTablePeople + " from " + SQLTablePeople + " m,ccvisits v"
                                + " where v.memberid=m.id"
                                + " and(m.Visits=1)"
                                + " and(m.createdbyvisit=1)"
                                + " and(m.Username is null)"
                                + " and(m.email is null)"
                                + " and(v.CookieSupport=0)and(v.LastVisitTime<" + SQLDateMidnightTwoDaysAgo + ")";
                            break;
                    }
                    // removed name requirement bc spiders not have bot names
                    //        Select Case DataSourceType
                    //            Case DataSourceTypeODBCAccess
                    //                SQL = "delete m.*" _
                    //                    & " from " & SQLTablePeople & " m,ccvisits v" _
                    //                    & " where v.memberid=m.id" _
                    //                    & " and(m.Name=" & encodeSQLText(DefaultMemberName) & ")" _
                    //                    & " and(m.Visits=1)" _
                    //                    & " and(m.Username is null)" _
                    //                    & " and(m.email is null)" _
                    //                    & " and(v.CookieSupport=0)and(v.LastVisitTime<" & SQLDateMidnightTwoDaysAgo & ")"
                    //            Case DataSourceTypeODBCMySQL
                    //                SQL = "delete m" _
                    //                    & " from " & SQLTablePeople & " m,ccvisits v" _
                    //                    & " where v.memberid=m.id" _
                    //                    & " and(m.Name=" & encodeSQLText(DefaultMemberName) & ")" _
                    //                    & " and(m.Visits=1)" _
                    //                    & " and(m.Username is null)" _
                    //                    & " and(m.email is null)" _
                    //                    & " and(v.CookieSupport=0)and(v.LastVisitTime<" & SQLDateMidnightTwoDaysAgo & ")"
                    //            Case Else
                    //                SQL = "delete from " & SQLTablePeople _
                    //                    & " from " & SQLTablePeople & " m,ccvisits v" _
                    //                    & " where v.memberid=m.id" _
                    //                    & " and(m.Name=" & encodeSQLText(DefaultMemberName) & ")" _
                    //                    & " and(m.Visits=1)" _
                    //                    & " and(m.Username is null)" _
                    //                    & " and(m.email is null)" _
                    //                    & " and(v.CookieSupport=0)and(v.LastVisitTime<" & SQLDateMidnightTwoDaysAgo & ")"
                    //        End Select
                    try {
                        cpCore.db.executeQuery(SQL);
                    } catch (Exception ex) {
                    }

                    //
                    // delete viewings from the non-cookie visits
                    //
                    AppendClassLog(cpCore, appName, "HouseKeep_App_Daily(" + appName + ")", "Deleting viewings from visits with no cookie support older than Midnight, Two Days Ago");
                    switch (DataSourceType) {
                        case DataSourceTypeODBCAccess:
                            SQL = "delete h.*"
                                + " from ccviewings h,ccvisits v"
                                + " where h.visitid=v.id"
                                + " and(v.CookieSupport=0)and(v.LastVisitTime<" + SQLDateMidnightTwoDaysAgo + ")";
                            break;
                        case DataSourceTypeODBCMySQL:
                            SQL = "delete h"
                                + " from ccviewings h,ccvisits v"
                                + " where h.visitid=v.id"
                                + " and(v.CookieSupport=0)and(v.LastVisitTime<" + SQLDateMidnightTwoDaysAgo + ")";
                            break;
                        default:
                            SQL = "delete from ccviewings"
                                + " from ccviewings h,ccvisits v"
                                + " where h.visitid=v.id"
                                + " and(v.CookieSupport=0)and(v.LastVisitTime<" + SQLDateMidnightTwoDaysAgo + ")";
                            break;
                    }
                    // if this fails, continue with the rest of the work
                    try {
                        cpCore.db.executeQuery(SQL);
                    } catch (Exception) {
                    }
                    //
                    // delete visitors from the non-cookie visits
                    //
                    AppendClassLog(cpCore, appName, "HouseKeep_App_Daily(" + appName + ")", "Deleting visitors from visits with no cookie support older than Midnight, Two Days Ago");
                    switch (DataSourceType) {
                        case DataSourceTypeODBCAccess:
                            SQL = "delete r.*"
                                + " from ccvisitors r,ccvisits v"
                                + " where r.id=v.visitorid"
                                + " and(v.CookieSupport=0)and(v.LastVisitTime<" + SQLDateMidnightTwoDaysAgo + ")";
                            break;
                        case DataSourceTypeODBCMySQL:
                            SQL = "delete r"
                                + " from ccvisitors r,ccvisits v"
                                + " where r.id=v.visitorid"
                                + " and(v.CookieSupport=0)and(v.LastVisitTime<" + SQLDateMidnightTwoDaysAgo + ")";
                            break;
                        default:
                            SQL = "delete from ccvisitors"
                                + " from ccvisitors r,ccvisits v"
                                + " where r.id=v.visitorid"
                                + " and(v.CookieSupport=0)and(v.LastVisitTime<" + SQLDateMidnightTwoDaysAgo + ")";
                            break;
                    }
                    try {
                        cpCore.db.executeQuery(SQL);
                    } catch (Exception) {
                    }
                    //
                    // delete visits from the non-cookie visits
                    //
                    AppendClassLog(cpCore, appName, "HouseKeep_App_Daily(" + appName + ")", "Deleting visits with no cookie support older than Midnight, Two Days Ago");
                    cpCore.db.DeleteTableRecordChunks("default", "ccvisits", "(CookieSupport=0)and(LastVisitTime<" + SQLDateMidnightTwoDaysAgo + ")", 1000, 10000);
                }
                //
                // Visits with no DateAdded
                //
                AppendClassLog(cpCore, appName, "HouseKeep_App_Daily(" + appName + ")", "Deleting visits with no DateAdded");
                cpCore.db.DeleteTableRecordChunks("default", "ccvisits", "(DateAdded is null)or(DateAdded<=" + cpCore.db.encodeSQLDate(new DateTime(1995, 1, 1)) + ")", 1000, 10000);
                //
                // Visits with no visitor
                //
                AppendClassLog(cpCore, appName, "HouseKeep_App_Daily(" + appName + ")", "Deleting visits with no DateAdded");
                cpCore.db.DeleteTableRecordChunks("default", "ccvisits", "(VisitorID is null)or(VisitorID=0)", 1000, 10000);
                //
                // viewings with no visit
                //
                AppendClassLog(cpCore, appName, "HouseKeep_App_Daily(" + appName + ")", "Deleting viewings with null or invalid VisitID");
                cpCore.db.DeleteTableRecordChunks("default", "ccviewings", "(visitid=0 or visitid is null)", 1000, 10000);
                //
                // Get Oldest Visit
                //
                //SQL = "select top 1 DateAdded from ccVisits where dateadded>0 order by DateAdded"
                SQL = cpCore.db.GetSQLSelect("default", "ccVisits", "DateAdded", "", "dateadded", "", 1);
                CS = cpCore.db.csOpenSql_rev("default", SQL);
                if (cpCore.db.csOk(CS)) {
                    OldestVisitDate = cpCore.db.csGetDate(CS, "DateAdded").Date;
                }
                cpCore.db.csClose(ref CS);
                //
                // Remove old visit records
                //   if > 30 days in visit table, limit one pass to just 30 days
                //   this is to prevent the entire server from being bogged down for one site change
                //
                if (OldestVisitDate == DateTime.MinValue) {
                    AppendClassLog(cpCore, appName, "HouseKeep_App_Daily(" + appName + ")", "No records were removed because no visit records were found while requesting the oldest visit.");
                } else if (VisitArchiveAgeDays <= 0) {
                    AppendClassLog(cpCore, appName, "HouseKeep_App_Daily(" + appName + ")", "No records were removed because Housekeep ArchiveRecordAgeDays is 0.");
                } else {
                    ArchiveDate = rightNow.AddDays(-VisitArchiveAgeDays).Date;
                    DaystoRemove = encodeInteger(ArchiveDate.Subtract(OldestVisitDate).TotalDays);
                    if (DaystoRemove > 30) {
                        ArchiveDate = OldestVisitDate.AddDays(30);
                    }
                    if (OldestVisitDate >= ArchiveDate) {
                        AppendClassLog(cpCore, appName, "HouseKeep_App_Daily(" + appName + ")", "No records were removed because Oldest Visit Date [" + OldestVisitDate + "] >= ArchiveDate [" + ArchiveDate + "].");
                    } else {
                        AppendClassLog(cpCore, appName, "HouseKeep_App_Daily(" + appName + ")", "Removing records from [" + OldestVisitDate + "] to [" + ArchiveDate + "].");
                        SingleDate = OldestVisitDate;
                        do {
                            HouseKeep_App_Daily_RemoveVisitRecords(cpCore, SingleDate, DataSourceType);
                            SingleDate = SingleDate.AddDays(1);
                        } while (SingleDate < ArchiveDate);
                    }
                }
                //
                // Remove old guest records
                //
                ArchiveDate = rightNow.AddDays(-GuestArchiveAgeDays).Date;
                HouseKeep_App_Daily_RemoveGuestRecords(cpCore, ArchiveDate, DataSourceType);
                //
                // delete 'guests' Members with one visits but no valid visit record
                //
                AppendClassLog(cpCore, appName, "HouseKeep_App_Daily(" + appName + ")", "Deleting 'guest' members with no visits (name is default name, visits=1, username null, email null,dateadded=lastvisit)");
                switch (DataSourceType) {
                    case DataSourceTypeODBCAccess:
                        SQL = "delete m.*"
                            + " from " + SQLTablePeople + " m,ccvisits v"
                            + " where v.memberid=m.id"
                            + " and(m.createdbyvisit=1)"
                            + " and(m.Visits=1)"
                            + " and(m.Username is null)"
                            + " and(m.email is null)"
                            + " and(m.dateadded=m.lastvisit)"
                            + " and(v.id is null)";
                        break;
                    case DataSourceTypeODBCMySQL:
                        SQL = "delete m"
                            + " from " + SQLTablePeople + " m,ccvisits v"
                            + " where v.memberid=m.id"
                            + " and(m.createdbyvisit=1)"
                            + " and(m.Visits=1)"
                            + " and(m.Username is null)"
                            + " and(m.email is null)"
                            + " and(m.dateadded=m.lastvisit)"
                            + " and(v.id is null)";
                        break;
                    default:
                        SQL = "delete from " + SQLTablePeople + " from " + SQLTablePeople + " m,ccvisits v"
                            + " where v.memberid=m.id"
                            + " and(m.createdbyvisit=1)"
                            + " and(m.Visits=1)"
                            + " and(m.Username is null)"
                            + " and(m.email is null)"
                            + " and(m.dateadded=m.lastvisit)"
                            + " and(v.id is null)";
                        break;
                }
                //INSTANT C# TODO TASK: The '////On Error //Resume Next' statement is not converted by Instant C#:
                ////On Error //Resume Next

                cpCore.db.executeQuery(SQL);
                //INSTANT C# TODO TASK: Calls to the VB 'Err' function are not converted by Instant C#:
                if (0 != 0) {
                    //throw new ApplicationException("Unexpected exception");
                }
                //INSTANT C# TODO TASK: Calls to the VB 'Err' function are not converted by Instant C#:
                //Microsoft.VisualBasic.Information.Err().Clear();
                // moved to upgrade code
                //    '
                //    ' Update CreatedByVisit for older records where this field is null
                //    '
                //    ////On Error //Resume Next
                //    SQL = "update ccmembers set createdbyvisit=1 where (createdbyvisit Is Null) And (dateadded<" & encodeSQLDate("1/1/2010") & ") and (username Is Null) And (email Is Null) And ((visits <> 0) And (visits Is Not Null))"
                //    Call cpCore.app.ExecuteSQL( SQL)
                //    Err.Clear
                //    On Error GoTo ErrorTrap
                //
                // delete 'guests' Members created before ArchivePeopleAgeDays
                //
                AppendClassLog(cpCore, appName, "HouseKeep_App_Daily(" + appName + ")", "Deleting 'guest' members with no visits (name is default name, visits=1, username null, email null,dateadded=lastvisit)");
                switch (DataSourceType) {
                    case DataSourceTypeODBCAccess:
                        SQL = "delete m.*"
                            + " from " + SQLTablePeople + " m left join ccvisits v on v.memberid=m.id"
                            + " where(m.createdbyvisit=1)"
                            + " and(m.Visits=1)"
                            + " and(m.Username is null)"
                            + " and(m.email is null)"
                            + " and(m.dateadded=m.lastvisit)"
                            + " and(v.id is null)";
                        break;
                    case DataSourceTypeODBCMySQL:
                        SQL = "delete m"
                            + " from " + SQLTablePeople + " m left join ccvisits v on v.memberid=m.id"
                            + " where(m.createdbyvisit=1)"
                            + " and(m.Visits=1)"
                            + " and(m.Username is null)"
                            + " and(m.email is null)"
                            + " and(m.dateadded=m.lastvisit)"
                            + " and(v.id is null)";
                        break;
                    default:
                        SQL = "delete from " + SQLTablePeople + " from " + SQLTablePeople + " m left join ccvisits v on v.memberid=m.id"
                            + " where(m.createdbyvisit=1)"
                            + " and(m.Visits=1)"
                            + " and(m.Username is null)"
                            + " and(m.email is null)"
                            + " and(m.dateadded=m.lastvisit)"
                            + " and(v.id is null)";
                        break;
                }
                //INSTANT C# TODO TASK: The '////On Error //Resume Next' statement is not converted by Instant C#:
                ////On Error //Resume Next

                cpCore.db.executeQuery(SQL);
                //INSTANT C# TODO TASK: Calls to the VB 'Err' function are not converted by Instant C#:
                if (0 != 0) {
                    //throw new ApplicationException("Unexpected exception");
                }
                //INSTANT C# TODO TASK: Calls to the VB 'Err' function are not converted by Instant C#:
                //Microsoft.VisualBasic.Information.Err().Clear();
                //
                // delete email drops older than archive.
                //
                AppendClassLog(cpCore, appName, "HouseKeep_App_Daily(" + appName + ")", "Deleting email drops older then " + EmailDropArchiveAgeDays + " days");
                ArchiveEmailDropDate = rightNow.AddDays(-EmailDropArchiveAgeDays).Date;
                //INSTANT C# TODO TASK: The '////On Error //Resume Next' statement is not converted by Instant C#:
                ////On Error //Resume Next

                cpCore.db.deleteContentRecords("Email drops", "(DateAdded is null)or(DateAdded<=" + cpCore.db.encodeSQLDate(ArchiveEmailDropDate) + ")");
                //INSTANT C# TODO TASK: Calls to the VB 'Err' function are not converted by Instant C#:
                if (0 != 0) {
                    //throw new ApplicationException("Unexpected exception");
                }
                //INSTANT C# TODO TASK: Calls to the VB 'Err' function are not converted by Instant C#:
                //Microsoft.VisualBasic.Information.Err().Clear();
                //
                // delete email log entries not realted to a drop, older than archive.
                //
                AppendClassLog(cpCore, appName, "HouseKeep_App_Daily(" + appName + ")", "Deleting non-drop email logs older then " + EmailDropArchiveAgeDays + " days");
                ArchiveEmailDropDate = rightNow.AddDays(-EmailDropArchiveAgeDays).Date;
                //INSTANT C# TODO TASK: The '////On Error //Resume Next' statement is not converted by Instant C#:
                ////On Error //Resume Next

                cpCore.db.deleteContentRecords("Email Log", "(emailDropId is null)and((DateAdded is null)or(DateAdded<=" + cpCore.db.encodeSQLDate(ArchiveEmailDropDate) + "))");
                //INSTANT C# TODO TASK: Calls to the VB 'Err' function are not converted by Instant C#:
                if (0 != 0) {
                    //throw new ApplicationException("Unexpected exception");
                }
                //INSTANT C# TODO TASK: Calls to the VB 'Err' function are not converted by Instant C#:
                //Microsoft.VisualBasic.Information.Err().Clear();
                //
                // delete email log entries without email drops
                //
                AppendClassLog(cpCore, appName, "HouseKeep_App_Daily(" + appName + ")", "Deleting drop email log entries for drops without a valid drop record.");
                switch (DataSourceType) {
                    case DataSourceTypeODBCAccess:
                        SQL = "delete l.*"
                            + " from ccemaillog l"
                            + " left join ccemaildrops d on d.id=l.emaildropid"
                            + " where l.emaildropid Is Not Null"
                            + " and d.id is null"
                            + "";
                        break;
                    case DataSourceTypeODBCMySQL:
                        SQL = "delete l"
                            + " from ccemaillog l"
                            + " left join ccemaildrops d on d.id=l.emaildropid"
                            + " where l.emaildropid Is Not Null"
                            + " and d.id is null"
                            + "";
                        break;
                    default:
                        SQL = "delete from ccemaillog"
                            + " from ccemaillog l"
                            + " left join ccemaildrops d on d.id=l.emaildropid"
                            + " where l.emaildropid Is Not Null"
                            + " and d.id is null"
                            + "";
                        break;
                }
                //INSTANT C# TODO TASK: The '////On Error //Resume Next' statement is not converted by Instant C#:
                ////On Error //Resume Next

                cpCore.db.executeQuery(SQL);
                //INSTANT C# TODO TASK: Calls to the VB 'Err' function are not converted by Instant C#:
                if (0 != 0) {
                    //throw new ApplicationException("Unexpected exception");
                }
                //INSTANT C# TODO TASK: Calls to the VB 'Err' function are not converted by Instant C#:
                //Microsoft.VisualBasic.Information.Err().Clear();

                //
                // block duplicate redirect fields (match contentid+fieldtype+caption)
                //
                AppendClassLog(cpCore, appName, "HouseKeep_App_Daily(" + appName + ")", "Inactivate duplicate redirect fields");
                CS = cpCore.db.csOpenSql_rev("Default", "Select ID, ContentID, Type, Caption from ccFields where (active<>0)and(Type=" + FieldTypeIdRedirect + ") Order By ContentID, Caption, ID");
                FieldLast = "";
                while (cpCore.db.csOk(CS)) {
                    //FieldType = cpCore.app.csv_cs_getInteger(CS, "Type")
                    FieldContentID = cpCore.db.csGetInteger(CS, "Contentid");
                    FieldCaption = cpCore.db.csGetText(CS, "Caption");
                    FieldNew = FieldContentID + FieldCaption;
                    if (FieldNew == FieldLast) {
                        FieldRecordID = cpCore.db.csGetInteger(CS, "ID");
                        cpCore.db.executeQuery("Update ccFields set active=0 where ID=" + FieldRecordID + ";");
                    }
                    FieldLast = FieldNew;
                    cpCore.db.csGoNext(CS);
                }
                cpCore.db.csClose(ref CS);
                //
                // block duplicate non-redirect fields (match contentid+fieldtype+name)
                //
                AppendClassLog(cpCore, appName, "HouseKeep_App_Daily(" + appName + ")", "Inactivate duplicate non-redirect fields");
                CS = cpCore.db.csOpenSql_rev("Default", "Select ID, Name, ContentID, Type from ccFields where (active<>0)and(Type<>" + FieldTypeIdRedirect + ") Order By ContentID, Name, Type, ID");
                FieldLast = "";
                while (cpCore.db.csOk(CS)) {
                    fieldType = cpCore.db.csGetInteger(CS, "Type");
                    FieldContentID = cpCore.db.csGetInteger(CS, "Contentid");
                    FieldName = cpCore.db.csGetText(CS, "Name");
                    FieldRecordID = cpCore.db.csGetInteger(CS, "ID");
                    FieldNew = FieldContentID + FieldName + fieldType;
                    if (FieldNew == FieldLast) {
                        cpCore.db.executeQuery("Update ccFields set active=0 where ID=" + FieldRecordID + ";");
                    }
                    FieldLast = FieldNew;
                    cpCore.db.csGoNext(CS);
                }
                cpCore.db.csClose(ref CS);
                //
                // Activities with no Member
                //
                AppendClassLog(cpCore, appName, "HouseKeep_App_Daily(" + appName + ")", "Deleting activities with no member record.");
                switch (DataSourceType) {
                    case DataSourceTypeODBCAccess:
                        SQL = "delete ccactivitylog.*"
                            + " From ccactivitylog LEFT JOIN " + SQLTablePeople + " on " + SQLTablePeople + ".ID=ccactivitylog.memberid"
                            + " WHERE (" + SQLTablePeople + ".ID is null)";
                        cpCore.db.executeQuery(SQL);
                        break;
                    case DataSourceTypeODBCSQLServer:
                        SQL = "delete from ccactivitylog"
                            + " From ccactivitylog LEFT JOIN " + SQLTablePeople + " on " + SQLTablePeople + ".ID=ccactivitylog.memberid"
                            + " WHERE (" + SQLTablePeople + ".ID is null)";
                        cpCore.db.executeQuery(SQL);
                        break;
                    default:
                        SQL = "delete ccactivitylog"
                            + " From ccactivitylog LEFT JOIN " + SQLTablePeople + " on " + SQLTablePeople + ".ID=ccactivitylog.memberid"
                            + " WHERE (" + SQLTablePeople + ".ID is null)";
                        cpCore.db.executeQuery(SQL);
                        break;
                }
                //
                // Member Properties with no member
                //
                AppendClassLog(cpCore, appName, "HouseKeep_App_Daily(" + appName + ")", "Deleting member properties with no member record.");
                switch (DataSourceType) {
                    case DataSourceTypeODBCAccess:
                        SQL = "delete ccProperties.*"
                            + " From ccProperties LEFT JOIN " + SQLTablePeople + " on " + SQLTablePeople + ".ID=ccProperties.KeyID"
                            + " WHERE (ccProperties.TypeID=0)"
                            + " AND (" + SQLTablePeople + ".ID is null)";
                        cpCore.db.executeQuery(SQL);
                        break;
                    case DataSourceTypeODBCSQLServer:
                        SQL = "delete From ccProperties"
                            + " From ccProperties LEFT JOIN " + SQLTablePeople + " on " + SQLTablePeople + ".ID=ccProperties.KeyID"
                            + " WHERE (ccProperties.TypeID=0)"
                            + " AND (" + SQLTablePeople + ".ID is null)";
                        cpCore.db.executeQuery(SQL);
                        break;
                    default:
                        SQL = "delete ccProperties"
                            + " From ccProperties LEFT JOIN " + SQLTablePeople + " on " + SQLTablePeople + ".ID=ccProperties.KeyID"
                            + " WHERE (ccProperties.TypeID=0)"
                            + " AND (" + SQLTablePeople + ".ID is null)";
                        cpCore.db.executeQuery(SQL);
                        break;
                }
                //
                // Visit Properties with no visits
                //
                AppendClassLog(cpCore, appName, "HouseKeep_App_Daily(" + appName + ")", "Deleting visit properties with no visit record.");
                switch (DataSourceType) {
                    case DataSourceTypeODBCAccess:
                        SQL = "delete ccProperties.*"
                            + " from ccProperties LEFT JOIN ccVisits on ccVisits.ID=ccProperties.KeyID"
                            + " WHERE (ccProperties.TypeID=1)"
                            + " AND (ccVisits.ID is null)";
                        cpCore.db.executeQuery(SQL);
                        break;
                    case DataSourceTypeODBCSQLServer:
                        SQL = "delete From ccProperties"
                            + " from ccProperties LEFT JOIN ccVisits on ccVisits.ID=ccProperties.KeyID"
                            + " WHERE (ccProperties.TypeID=1)"
                            + " AND (ccVisits.ID is null)";
                        cpCore.db.executeQuery(SQL);
                        break;
                    default:
                        SQL = "delete ccProperties"
                            + " from ccProperties LEFT JOIN ccVisits on ccVisits.ID=ccProperties.KeyID"
                            + " WHERE (ccProperties.TypeID=1)"
                            + " AND (ccVisits.ID is null)";
                        cpCore.db.executeQuery(SQL);
                        break;
                }
                //
                // Visitor Properties with no visitor
                //
                AppendClassLog(cpCore, appName, "HouseKeep_App_Daily(" + appName + ")", "Deleting visitor properties with no visitor record.");
                switch (DataSourceType) {
                    case DataSourceTypeODBCAccess:
                        SQL = "delete ccProperties.*"
                            + " from ccProperties LEFT JOIN ccvisitors on ccvisitors.ID=ccProperties.KeyID"
                            + " where ccproperties.typeid=2"
                            + " and ccvisitors.id is null";
                        cpCore.db.executeQuery(SQL);
                        break;
                    case DataSourceTypeODBCSQLServer:
                        SQL = "delete From ccProperties"
                            + " from ccProperties LEFT JOIN ccvisitors on ccvisitors.ID=ccProperties.KeyID"
                            + " where ccproperties.typeid=2"
                            + " and ccvisitors.id is null";
                        cpCore.db.executeQuery(SQL);
                        break;
                    default:
                        SQL = "delete ccProperties"
                            + " from ccProperties LEFT JOIN ccvisitors on ccvisitors.ID=ccProperties.KeyID"
                            + " where ccproperties.typeid=2"
                            + " and ccvisitors.id is null";
                        cpCore.db.executeQuery(SQL);
                        break;
                }
                //
                // MemberRules with bad MemberID
                //
                AppendClassLog(cpCore, appName, "HouseKeep_App_Daily(" + appName + ")", "Deleting Member Rules with bad MemberID.");
                switch (DataSourceType) {
                    case DataSourceTypeODBCAccess:
                        SQL = "delete " + SQLTableMemberRules + ".*"
                            + " From " + SQLTableMemberRules + ""
                            + " LEFT JOIN " + SQLTablePeople + " on " + SQLTablePeople + ".ID=" + SQLTableMemberRules + ".MemberID"
                            + " WHERE (" + SQLTablePeople + ".ID is null)";
                        cpCore.db.executeQuery(SQL);
                        break;
                    case DataSourceTypeODBCSQLServer:
                        SQL = "delete From " + SQLTableMemberRules + ""
                            + " From " + SQLTableMemberRules + ""
                            + " LEFT JOIN " + SQLTablePeople + " on " + SQLTablePeople + ".ID=" + SQLTableMemberRules + ".MemberID"
                            + " WHERE (" + SQLTablePeople + ".ID is null)";
                        cpCore.db.executeQuery(SQL);
                        break;
                    default:
                        SQL = "delete " + SQLTableMemberRules + ""
                            + " From " + SQLTableMemberRules + ""
                            + " LEFT JOIN " + SQLTablePeople + " on " + SQLTablePeople + ".ID=" + SQLTableMemberRules + ".MemberID"
                            + " WHERE (" + SQLTablePeople + ".ID is null)";
                        cpCore.db.executeQuery(SQL);
                        break;
                }
                //
                // MemberRules with bad GroupID
                //
                AppendClassLog(cpCore, appName, "HouseKeep_App_Daily(" + appName + ")", "Deleting Member Rules with bad GroupID.");
                switch (DataSourceType) {
                    case DataSourceTypeODBCAccess:
                        SQL = "delete " + SQLTableMemberRules + ".*"
                            + " From " + SQLTableMemberRules + ""
                            + " LEFT JOIN " + SQLTableGroups + " on " + SQLTableGroups + ".ID=" + SQLTableMemberRules + ".GroupID"
                            + " WHERE (" + SQLTableGroups + ".ID is null)";
                        cpCore.db.executeQuery(SQL);
                        break;
                    case DataSourceTypeODBCSQLServer:
                        SQL = "delete From " + SQLTableMemberRules + ""
                            + " From " + SQLTableMemberRules + ""
                            + " LEFT JOIN " + SQLTableGroups + " on " + SQLTableGroups + ".ID=" + SQLTableMemberRules + ".GroupID"
                            + " WHERE (" + SQLTableGroups + ".ID is null)";
                        cpCore.db.executeQuery(SQL);
                        break;
                    default:
                        SQL = "delete " + SQLTableMemberRules + ""
                            + " From " + SQLTableMemberRules + ""
                            + " LEFT JOIN " + SQLTableGroups + " on " + SQLTableGroups + ".ID=" + SQLTableMemberRules + ".GroupID"
                            + " WHERE (" + SQLTableGroups + ".ID is null)";
                        cpCore.db.executeQuery(SQL);
                        break;
                }
                //
                // GroupRules with bad ContentID
                //   Handled record by record removed to prevent CDEF reload
                //
                AppendClassLog(cpCore, appName, "HouseKeep_App_Daily(" + appName + ")", "Deleting Group Rules with bad ContentID.");
                SQL = "Select ccGroupRules.ID"
                    + " From ccGroupRules LEFT JOIN ccContent on ccContent.ID=ccGroupRules.ContentID"
                    + " WHERE (ccContent.ID is null)";
                CS = cpCore.db.csOpenSql_rev("default", SQL);
                while (cpCore.db.csOk(CS)) {
                    cpCore.db.deleteContentRecord("Group Rules", cpCore.db.csGetInteger(CS, "ID"));
                    cpCore.db.csGoNext(CS);
                }
                cpCore.db.csClose(ref CS);
                //
                // GroupRules with bad GroupID
                //
                AppendClassLog(cpCore, appName, "HouseKeep_App_Daily(" + appName + ")", "Deleting Group Rules with bad GroupID.");
                switch (DataSourceType) {
                    case DataSourceTypeODBCAccess:
                        SQL = "delete ccGroupRules.*"
                            + " From ccGroupRules"
                            + " LEFT JOIN " + SQLTableGroups + " on " + SQLTableGroups + ".ID=ccGroupRules.GroupID"
                            + " WHERE (" + SQLTableGroups + ".ID is null)";
                        cpCore.db.executeQuery(SQL);
                        break;
                    case DataSourceTypeODBCSQLServer:
                        SQL = "delete from ccGroupRules"
                            + " From ccGroupRules"
                            + " LEFT JOIN " + SQLTableGroups + " on " + SQLTableGroups + ".ID=ccGroupRules.GroupID"
                            + " WHERE (" + SQLTableGroups + ".ID is null)";
                        cpCore.db.executeQuery(SQL);
                        break;
                    default:
                        SQL = "delete ccGroupRules"
                            + " From ccGroupRules"
                            + " LEFT JOIN " + SQLTableGroups + " on " + SQLTableGroups + ".ID=ccGroupRules.GroupID"
                            + " WHERE (" + SQLTableGroups + ".ID is null)";
                        cpCore.db.executeQuery(SQL);
                        break;
                }
                //
                // TopicRules with bad ContentID
                // delete manually to prevent cdef reload
                //
                //Call AppendClassLog(AppName, "HouseKeep_App_Daily(" & AppName & ")", "Deleting Topic Rules with bad ContentID.")
                //SQL = "Select ccTopicRules.ID" _
                //    & " From ccTopicRules LEFT JOIN ccContent on ccContent.ID=ccTopicRules.ContentID" _
                //    & " WHERE (ccContent.ID is null)"
                //CS = cpCore.app.csv_OpenCSSQL("default", SQL)
                //Do While cpCore.app.csv_IsCSOK(CS)
                //    Call cpCore.csv_DeleteContentRecord("Topic Rules", cpCore.app.csv_cs_getInteger(CS, "ID"))
                //    Call cpCore.app.csv_NextCSRecord(CS)
                //    Loop
                //Call cpCore.app.csv_CloseCS(CS)
                //
                // TopicRules with bad TopicID
                //
                //Call AppendClassLog(AppName, "HouseKeep_App_Daily(" & AppName & ")", "Deleting Topic Rules with bad TopicID.")
                //Select Case DataSourceType
                //    Case DataSourceTypeODBCAccess
                //        SQL = "delete ccTopicRules.*" _
                //            & " From ccTopicRules" _
                //            & " LEFT JOIN ccTopics on ccTopics.ID=ccTopicRules.topicID" _
                //            & " WHERE (ccTopics.ID is null)"
                //        Call cpCore.app.executeSql(sql)
                //    Case DataSourceTypeODBCSQLServer
                //        SQL = "delete from ccTopicRules" _
                //            & " From ccTopicRules" _
                //            & " LEFT JOIN ccTopics on ccTopics.ID=ccTopicRules.topicID" _
                //            & " WHERE (ccTopics.ID is null)"
                //        Call cpCore.app.executeSql(sql)
                //    Case Else
                //        SQL = "delete ccTopicRules" _
                //            & " From ccTopicRules" _
                //            & " LEFT JOIN ccTopics on ccTopics.ID=ccTopicRules.topicID" _
                //            & " WHERE (ccTopics.ID is null)"
                //        Call cpCore.app.executeSql(sql)
                //End Select
                //
                // ContentWatch with bad CContentID
                //     must be deleted manually
                //
                AppendClassLog(cpCore, appName, "HouseKeep_App_Daily(" + appName + ")", "Deleting Content Watch with bad ContentID.");
                SQL = "Select ccContentWatch.ID"
                    + " From ccContentWatch LEFT JOIN ccContent on ccContent.ID=ccContentWatch.ContentID"
                    + " WHERE (ccContent.ID is null)or(ccContent.Active=0)or(ccContent.Active is null)";
                CS = cpCore.db.csOpenSql_rev("default", SQL);
                while (cpCore.db.csOk(CS)) {
                    cpCore.db.deleteContentRecord("Content Watch", cpCore.db.csGetInteger(CS, "ID"));
                    cpCore.db.csGoNext(CS);
                }
                cpCore.db.csClose(ref CS);
                //
                // ContentWatchListRules with bad ContentWatchID
                //
                AppendClassLog(cpCore, appName, "HouseKeep_App_Daily(" + appName + ")", "Deleting ContentWatchList Rules with bad ContentWatchID.");
                switch (DataSourceType) {
                    case DataSourceTypeODBCAccess:
                        SQL = "delete ccContentWatchListRules.*"
                            + " From ccContentWatchListRules"
                            + " LEFT JOIN ccContentWatch on ccContentWatch.ID=ccContentWatchListRules.ContentWatchID"
                            + " WHERE (ccContentWatch.ID is null)";
                        cpCore.db.executeQuery(SQL);
                        break;
                    case DataSourceTypeODBCSQLServer:
                        SQL = "delete from ccContentWatchListRules"
                            + " From ccContentWatchListRules"
                            + " LEFT JOIN ccContentWatch on ccContentWatch.ID=ccContentWatchListRules.ContentWatchID"
                            + " WHERE (ccContentWatch.ID is null)";
                        cpCore.db.executeQuery(SQL);
                        break;
                    default:
                        SQL = "delete ccContentWatchListRules"
                            + " From ccContentWatchListRules"
                            + " LEFT JOIN ccContentWatch on ccContentWatch.ID=ccContentWatchListRules.ContentWatchID"
                            + " WHERE (ccContentWatch.ID is null)";
                        cpCore.db.executeQuery(SQL);
                        break;
                }
                //
                // ContentWatchListRules with bad ContentWatchListID
                //
                AppendClassLog(cpCore, appName, "HouseKeep_App_Daily(" + appName + ")", "Deleting ContentWatchList Rules with bad ContentWatchListID.");
                switch (DataSourceType) {
                    case DataSourceTypeODBCAccess:
                        SQL = "delete ccContentWatchListRules.*"
                            + " From ccContentWatchListRules"
                            + " LEFT JOIN ccContentWatchLists on ccContentWatchLists.ID=ccContentWatchListRules.ContentWatchListID"
                            + " WHERE (ccContentWatchLists.ID is null)";
                        cpCore.db.executeQuery(SQL);
                        break;
                    case DataSourceTypeODBCSQLServer:
                        SQL = "delete from ccContentWatchListRules"
                            + " From ccContentWatchListRules"
                            + " LEFT JOIN ccContentWatchLists on ccContentWatchLists.ID=ccContentWatchListRules.ContentWatchListID"
                            + " WHERE (ccContentWatchLists.ID is null)";
                        cpCore.db.executeQuery(SQL);
                        break;
                    default:
                        SQL = "delete ccContentWatchListRules"
                            + " From ccContentWatchListRules"
                            + " LEFT JOIN ccContentWatchLists on ccContentWatchLists.ID=ccContentWatchListRules.ContentWatchListID"
                            + " WHERE (ccContentWatchLists.ID is null)";
                        cpCore.db.executeQuery(SQL);
                        break;
                }
                //
                // Field help with no field
                //
                AppendClassLog(cpCore, appName, "HouseKeep_App_Daily(" + appName + ")", "Deleting field help with no field.");
                SQL = ""
                    + "delete from ccfieldhelp where id in ("
                    + " select h.id"
                    + " from ccfieldhelp h"
                    + " left join ccfields f on f.id=h.fieldid where f.id is null"
                    + ")";
                cpCore.db.executeQuery(SQL);
                //
                // Field help duplicates - messy, but I am not sure where they are coming from, and this patchs the edit page performance problem
                //
                AppendClassLog(cpCore, appName, "HouseKeep_App_Daily(" + appName + ")", "Deleting duplicate field help records.");
                SQL = ""
                    + "delete from ccfieldhelp where id in ("
                    + " select b.id"
                    + " from ccfieldhelp a"
                    + " left join ccfieldhelp b on a.fieldid=b.fieldid where a.id< b.id"
                    + ")";
                cpCore.db.executeQuery(SQL);
                //
                //addon editor rules with no addon
                //
                SQL = "delete from ccAddonContentFieldTypeRules where id in ("
                    + "select r.id from ccAddonContentFieldTypeRules r left join ccaggregatefunctions a on a.id=r.addonid where a.Id Is Null"
                    + ")";
                cpCore.db.executeQuery(SQL);
                //
                // convert FieldTypeLongText + htmlContent to FieldTypeHTML
                //
                AppendClassLog(cpCore, appName, "HouseKeep_App_Daily(" + appName + ")", "convert FieldTypeLongText + htmlContent to FieldTypeHTML.");
                SQL = "update ccfields set type=" + FieldTypeIdHTML + " where type=" + FieldTypeIdLongText + " and ( htmlcontent<>0 )";
                cpCore.db.executeQuery(SQL);
                //
                // convert FieldTypeTextFile + htmlContent to FieldTypeHTMLFile
                //
                //Call AppendClassLog(appName, "HouseKeep_App_Daily(" & appName & ")", "convert FieldTypeTextFile + htmlContent to FieldTypeHTMLFile.")
                //SQL = "update ccfields set type=" & FieldTypeIdFileHTMLPrivate & " where type=" & FieldTypeIdFileTextPrivate & " and ( htmlcontent<>0 )"
                //Call cpCore.app.executeSql(SQL)
                //
                // Log files Older then 30 days
                //
                HouseKeep_App_Daily_LogFolder(cpCore, "temp", thirtyDaysAgo);
                HouseKeep_App_Daily_LogFolder(cpCore, "TrapLogs", thirtyDaysAgo);
                HouseKeep_App_Daily_LogFolder(cpCore, "BounceLog", thirtyDaysAgo);
                HouseKeep_App_Daily_LogFolder(cpCore, "BounceProcessing", thirtyDaysAgo);
                HouseKeep_App_Daily_LogFolder(cpCore, "SMTPLog", thirtyDaysAgo);
                HouseKeep_App_Daily_LogFolder(cpCore, "DebugLog", thirtyDaysAgo);
                //
                // Content TextFile types with no controlling record
                //
                if (genericController.encodeBoolean(cpCore.siteProperties.getText("ArchiveAllowFileClean", "false"))) {
                    //
                    int DSType = cpCore.db.getDataSourceType("");
                    AppendClassLog(cpCore, appName, "HouseKeep_App_Daily(" + appName + ")", "Content TextFile types with no controlling record.");
                    SQL = "SELECT DISTINCT ccTables.Name as TableName, ccFields.Name as FieldName"
                        + " FROM (ccFields LEFT JOIN ccContent ON ccFields.ContentID = ccContent.ID) LEFT JOIN ccTables ON ccContent.ContentTableID = ccTables.ID"
                        + " Where (((ccFields.Type) = 10))"
                        + " ORDER BY ccTables.Name";
                    CS = cpCore.db.csOpenSql_rev("Default", SQL);
                    while (cpCore.db.csOk(CS)) {
                        //
                        // Get all the files in this path, and check that the record exists with this in its field
                        //
                        FieldName = cpCore.db.csGetText(CS, "FieldName");
                        TableName = cpCore.db.csGetText(CS, "TableName");
                        PathName = TableName + "\\" + FieldName;
                        FileList = cpCore.cdnFiles.getFileList(PathName);
                        if (FileList.Length > 0) {
                            cpCore.db.executeQuery("CREATE INDEX temp" + FieldName + " ON " + TableName + " (" + FieldName + ")");
                            foreach (FileInfo file in FileList) {
                                Filename = file.Name;
                                VirtualFileName = PathName + "\\" + Filename;
                                VirtualLink = genericController.vbReplace(VirtualFileName, "\\", "/");
                                FileSize = file.Length;
                                if (FileSize == 0) {
                                    SQL = "update " + TableName + " set " + FieldName + "=null where (" + FieldName + "=" + cpCore.db.encodeSQLText(VirtualFileName) + ")or(" + FieldName + "=" + cpCore.db.encodeSQLText(VirtualLink) + ")";
                                    cpCore.db.executeQuery(SQL);
                                    cpCore.cdnFiles.deleteFile(VirtualFileName);
                                } else {
                                    SQL = "SELECT ID FROM " + TableName + " WHERE (" + FieldName + "=" + cpCore.db.encodeSQLText(VirtualFileName) + ")or(" + FieldName + "=" + cpCore.db.encodeSQLText(VirtualLink) + ")";
                                    CSTest = cpCore.db.csOpenSql_rev("default", SQL);
                                    if (!cpCore.db.csOk(CSTest)) {
                                        cpCore.cdnFiles.deleteFile(VirtualFileName);
                                    }
                                    cpCore.db.csClose(ref CSTest);
                                }
                            }
                            if (DSType == 1) {
                                // access
                                SQL = "Drop INDEX temp" + FieldName + " ON " + TableName;
                            } else if (DSType == 2) {
                                // sql server
                                SQL = "DROP INDEX " + TableName + ".temp" + FieldName;
                            } else {
                                // mysql
                                SQL = "ALTER TABLE " + TableName + " DROP INDEX temp" + FieldName;
                            }
                            cpCore.db.executeQuery(SQL);
                        }
                        cpCore.db.csGoNext(CS);
                    }
                    cpCore.db.csClose(ref CS);
                    //
                    // problem here is 1) images may have resized images in the folder
                    // 2) files may be in the wrong recordID if workflow.
                    //
                    //        '
                    //        ' Content File types with no controlling record
                    //        '
                    //        SQL = "SELECT DISTINCT ccTables.Name as TableName, ccFields.Name as FieldName" _
                    //            & " FROM (ccFields LEFT JOIN ccContent ON ccFields.ContentID = ccContent.ID) LEFT JOIN ccTables ON ccContent.ContentTableID = ccTables.ID" _
                    //            & " Where ((ccFields.Type=6)OR(ccFields.Type=11)OR(ccFields.Type=16)OR(ccFields.Type=17)OR(ccFields.Type=18))" _
                    //            & " ORDER BY ccTables.Name"
                    //        CS = cpCore.app.csv_OpenCSSQL("Default", SQL)
                    //        Do While cpCore.app.csv_IsCSOK(CS)
                    //            '
                    //            ' Get all the files in this path, and check that the record exists with this in its field
                    //            '
                    //            FieldName = cpCore.app.csv_cs_getText(CS, "FieldName")
                    //            TableName = cpCore.app.csv_cs_getText(CS, "TableName")
                    //            If cpCore.csv_IsSQLTableField("Default", TableName, FieldName) Then
                    //                PathName = TableName & "\" & FieldName
                    //                PathNameRev = TableName & "/" & FieldName
                    //                FolderList = cpCore.contentFiles.getFolderList(PathName)
                    //                If FolderList <> "" Then
                    //                    FolderArray = Split(FolderList, vbCrLf)
                    //                    FolderArrayCount = UBound(FolderArray) + 1
                    //                    For FolderArrayPointer = 0 To FolderArrayCount - 1
                    //                        If FolderArray(FolderArrayPointer) <> "" Then
                    //                            FolderSplit = Split(FolderArray(FolderArrayPointer), ",")
                    //                            FolderName = FolderSplit(0)
                    //                            '
                    //                            ' just verify the record exists -- all files in the folder are valid
                    //                            '
                    //                            SQL = "select id from " & TableName & " where id=" & encodeSQLNumber(FolderName)
                    //                            CSTest = cpCore.app.csv_OpenCSSQL("default", SQL)
                    //                            If Not cpCore.app.csv_IsCSOK(CSTest) Then
                    //                            '    Call cpCore.csv_DeleteVirtualFolder(PathNameRev & "\" & FolderName)
                    //                            End If
                    //                            Call cpCore.app.csv_CloseCS(CSTest)
                    //
                    //                            FileList = cpCore.csv_GetVirtualFileList(PathName & "\" & FolderName)
                    //                            If FileList <> "" Then
                    //                                FileArray = Split(FileList, vbCrLf)
                    //                                FileArrayCount = UBound(FileArray) + 1
                    //                                For FileArrayPointer = 0 To FileArrayCount - 1
                    //                                    If FileArray(FileArrayPointer) <> "" Then
                    //                                        FileSplit = Split(FileArray(FileArrayPointer), ",")
                    //                                        FilenameOriginal = FileSplit(0)
                    //                                        Filename = FilenameOriginal
                    //                                        FilenameAltSize = ""
                    //                                        Pos = InStrRev(Filename, ".")
                    //
                    //                                        If Pos > 0 Then
                    //                                            FilenameExt = Mid(Filename, Pos + 1)
                    //                                            FilenameNoExt = Mid(Filename, 1, Pos - 1)
                    //                                            Pos = InStrRev(FilenameNoExt, "-")
                    //                                            If Pos > 0 Then
                    //                                                FilenameAltSize = Mid(FilenameNoExt, Pos + 1)
                    //                                                If FilenameAltSize <> "" Then
                    //                                                FilenameDim = Split(FilenameAltSize, "x")
                    //                                                If UBound(FilenameDim) = 1 Then
                    //                                                    If genericController.vbIsNumeric(FilenameDim(0)) And genericController.vbIsNumeric(FilenameDim(1)) Then
                    //                                                        FilenameNoExt = Mid(FilenameNoExt, 1, Pos - 1)
                    //                                                    End If
                    //                                                End If
                    //                                                End If
                    //                                            End If
                    //                                            Filename = FilenameNoExt & "." & FilenameExt
                    //                                        End If
                    //                                        If FilenameAltSize <> "" Then
                    //                                            SQL = "SELECT ID FROM " & TableName & " WHERE (" & FieldName & "=" & encodeSQLText(PathNameRev & "/" & FolderName & "/" & FilenameOriginal) & ")or(" & FieldName & "=" & encodeSQLText(PathNameRev & "/" & FolderName & "/" & Filename) & ")"
                    //                                        Else
                    //                                            SQL = "SELECT ID FROM " & TableName & " WHERE " & FieldName & "=" & encodeSQLText(PathNameRev & "/" & FolderName & "/" & FilenameOriginal)
                    //                                        End If
                    //                                        CSTest = cpCore.app.csv_OpenCSSQL("default", SQL)
                    //                                        If Not cpCore.app.csv_IsCSOK(CSTest) Then
                    //                                            Call cpCore.virtualFiles.DeleteFile(PathNameRev & "/" & FolderName & "/" & FilenameOriginal)
                    //                                        End If
                    //                                        Call cpCore.app.csv_CloseCS(CSTest)
                    //                                    End If
                    //                                Next
                    //                            End If
                    //                        End If
                    //                    Next
                    //                End If
                    //            End If
                    //            Call cpCore.app.csv_NextCSRecord(CS)
                    //        Loop
                    //        Call cpCore.app.csv_CloseCS(CS)
                }
                cpCore.db.sqlCommandTimeout = TimeoutSave;
                return;
                //
            } catch (Exception ex) {
                cpCore.handleException(ex);
            }
        }
        //
        //
        //
        private void HouseKeep_App_Daily_RemoveVisitRecords(coreClass cpCore, DateTime DeleteBeforeDate, int DataSourceType) {
            try {
                //
                int TimeoutSave = 0;
                //Dim Controller As controlClass
                int VisitArchiveAgeDays = 0;
                int VisitArchiveDeleteSize = 0;
                //Dim AppService As appServicesClass
                //Dim KernelService As KernelServicesClass
                //      Dim CSConnection As appEnvironmentStruc
                string SQL = null;
                string SQLCriteria = null;
                string PathName = null;
                string TableName = null;
                string FieldName = null;
                string FileList = null;
                string[] FileArray = null;
                int FileArrayCount = 0;
                int FileArrayPointer = 0;
                int CS = 0;
                int CSTest = 0;
                string Filename = null;
                string[] FileSplit = null;
                string FolderName = null;
                string FolderList = null;
                string[] FolderArray = null;
                int FolderArrayCount = 0;
                int FolderArrayPointer = 0;
                string[] FolderSplit = null;
                int AdminLicenseCount = 0;
                string DeleteBeforeDateSQL = null;
                string appName = null;
                string SQLTablePeople = null;
                //Dim SQLTableMemberRules As String
                //Dim SQLTableGroups As String
                //Dim PeopleCID as integer
                string DefaultName = null;
                //
                // Set long timeout (30 min) needed for heavy work on big tables
                //
                TimeoutSave = cpCore.db.sqlCommandTimeout;
                cpCore.db.sqlCommandTimeout = 1800;
                //
                SQLTablePeople = Models.Complex.cdefModel.getContentTablename(cpCore, "People");
                //SQLTableMemberRules = cpCore.csv_GetContentTablename("Member Rules")
                //SQLTableGroups = cpCore.csv_GetContentTablename("Groups")
                //
                //VisitArchiveAgeDays = genericController.EncodeInteger(cpCore.csv_GetSiteProperty("ArchiveRecordAgeDays", "0"))
                if (true) {
                    appName = cpCore.serverConfig.appConfig.name;
                    DeleteBeforeDateSQL = cpCore.db.encodeSQLDate(DeleteBeforeDate);
                    //
                    // Visits older then archive age
                    //
                    AppendClassLog(cpCore, cpCore.serverConfig.appConfig.name, "HouseKeep_App_Daily_RemoveVisitRecords(" + appName + ")", "Deleting visits before [" + DeleteBeforeDateSQL + "]");
                    cpCore.db.DeleteTableRecordChunks("default", "ccVisits", "(DateAdded<" + DeleteBeforeDateSQL + ")", 1000, 10000);
                    //
                    // Viewings with visits before the first
                    //
                    AppendClassLog(cpCore, appName, "HouseKeep_App_Daily_RemoveVisitRecords(" + appName + ")", "Deleting viewings with visitIDs lower then the lowest ccVisits.ID");
                    cpCore.db.DeleteTableRecordChunks("default", "ccviewings", "(visitid<(select min(ID) from ccvisits))", 1000, 10000);
                    //
                    // Visitors with no visits
                    //
                    AppendClassLog(cpCore, appName, "HouseKeep_App_Daily_RemoveVisitRecords(" + appName + ")", "Deleting visitors with no visits");
                    switch (DataSourceType) {
                        case DataSourceTypeODBCAccess:
                            SQL = "delete ccVisitors.*"
                                + " from ccVisitors Left Join ccVisits on ccVisits.VisitorID=ccVisitors.ID"
                                + " where ccVisits.ID is null";
                            cpCore.db.executeQuery(SQL);

                            break;
                        case DataSourceTypeODBCSQLServer:
                            SQL = "delete From ccVisitors"
                                + " from ccVisitors Left Join ccVisits on ccVisits.VisitorID=ccVisitors.ID"
                                + " where ccVisits.ID is null";
                            cpCore.db.executeQuery(SQL);
                            break;
                        default:
                            SQL = "delete ccVisitors"
                                + " from ccVisitors Left Join ccVisits on ccVisits.VisitorID=ccVisitors.ID"
                                + " where ccVisits.ID is null";
                            cpCore.db.executeQuery(SQL);
                            break;
                    }
                    //        '
                    //        ' Delete People
                    //        '   created before DeleteBeforeDate,
                    //        '   created during a visit (not created by another process),
                    //        '   with default name (created during a hit)
                    //        '   with no username (they are not planning on returning)
                    //        '   with 1 visit (not created with 0 visits, has not returned)
                    //        '
                    //        Call AppendClassLog(AppName, "HouseKeep_App_Daily_RemoveVisitRecords(" & AppName & ")", "Deleting members with default name [" & DefaultName & "], LastVisit before DeleteBeforeDate [" & DeleteBeforeDate & "], exactly one total visit, a null username and a null email address.")
                    //        SQLCriteria = "" _
                    //            & "(" & SQLTablePeople & ".Name=" & encodeSQLText(DefaultName) & ")" _
                    //            & " and(LastVisit<" & DeleteBeforeDateSQL & ")" _
                    //            & " and(createdbyvisit=1)" _
                    //            & " and(Visits=1)" _
                    //            & " and(Username is null)" _
                    //            & " and(email is null)"
                    //        Call cpCore.csv_DeleteTableRecordChunks("default", "" & SQLTablePeople & "", SQLCriteria, 1000, 10000)
                }
                //
                // restore sved timeout
                //
                cpCore.db.sqlCommandTimeout = TimeoutSave;
                return;
                //
            } catch (Exception ex) {
                cpCore.handleException(ex);
            }
        }
        //
        //
        //
        private void HouseKeep_App_Daily_RemoveGuestRecords(coreClass cpCore, DateTime DeleteBeforeDate, int DataSourceType) {
            try {
                //
                int TimeoutSave = 0;
                //Dim Controller As controlClass
                int VisitArchiveAgeDays = 0;
                int VisitArchiveDeleteSize = 0;
                //Dim AppService As appServicesClass
                //Dim KernelService As KernelServicesClass
                //   Dim CSConnection As appEnvironmentStruc
                string SQL = null;
                string SQLCriteria = null;
                string PathName = null;
                string TableName = null;
                string FieldName = null;
                string FileList = null;
                string[] FileArray = null;
                int FileArrayCount = 0;
                int FileArrayPointer = 0;
                int CS = 0;
                int CSTest = 0;
                string Filename = null;
                string[] FileSplit = null;
                string FolderName = null;
                string FolderList = null;
                string[] FolderArray = null;
                int FolderArrayCount = 0;
                int FolderArrayPointer = 0;
                string[] FolderSplit = null;
                int AdminLicenseCount = 0;
                string DeleteBeforeDateSQL = null;
                string appName = null;
                string SQLTablePeople = null;
                //Dim SQLTableMemberRules As String
                //Dim SQLTableGroups As String
                //Dim PeopleCID as integer
                string DefaultName = null;
                //
                // Set long timeout (30 min) needed for heavy work on big tables
                //
                TimeoutSave = cpCore.db.sqlCommandTimeout;
                cpCore.db.sqlCommandTimeout = 1800;
                //
                SQLTablePeople = Models.Complex.cdefModel.getContentTablename(cpCore, "People");
                //SQLTableMemberRules = cpCore.csv_GetContentTablename("Member Rules")
                //SQLTableGroups = cpCore.csv_GetContentTablename("Groups")
                //
                //VisitArchiveAgeDays = genericController.EncodeInteger(cpCore.csv_GetSiteProperty("ArchiveRecordAgeDays", "0"))
                if (true) {
                    appName = cpCore.serverConfig.appConfig.name;
                    DeleteBeforeDateSQL = cpCore.db.encodeSQLDate(DeleteBeforeDate);
                    //        '
                    //        ' Visits older then archive age
                    //        '
                    //        Call AppendClassLog(cpCore, cpCore.appEnvironment.name, "HouseKeep_App_Daily_RemoveGuestRecords(" & AppName & ")", "Deleting visits before [" & DeleteBeforeDateSQL & "]")
                    //        Call cpCore.csv_DeleteTableRecordChunks("default", "ccVisits", "(DateAdded<" & DeleteBeforeDateSQL & ")", 1000, 10000)
                    //        '
                    //        ' Viewings with visits before the first
                    //        '
                    //        Call AppendClassLog(AppName, "HouseKeep_App_Daily_RemoveGuestRecords(" & AppName & ")", "Deleting viewings with visitIDs lower then the lowest ccVisits.ID")
                    //        Call cpCore.csv_DeleteTableRecordChunks("default", "ccviewings", "(visitid<(select min(ID) from ccvisits))", 1000, 10000)
                    //        '
                    //        ' Visitors with no visits
                    //        '
                    //        Call AppendClassLog(AppName, "HouseKeep_App_Daily_RemoveGuestRecords(" & AppName & ")", "Deleting visitors with no visits")
                    //        Select Case DataSourceType
                    //            Case DataSourceTypeODBCAccess
                    //                SQL = "delete ccVisitors.*" _
                    //                    & " from ccVisitors Left Join ccVisits on ccVisits.VisitorID=ccVisitors.ID" _
                    //                    & " where ccVisits.ID is null"
                    //                Call cpCore.app.executeSql(sql)
                    //
                    //            Case DataSourceTypeODBCSQLServer
                    //                SQL = "delete From ccVisitors" _
                    //                    & " from ccVisitors Left Join ccVisits on ccVisits.VisitorID=ccVisitors.ID" _
                    //                    & " where ccVisits.ID is null"
                    //                Call cpCore.app.executeSql(sql)
                    //            Case Else
                    //                SQL = "delete ccVisitors" _
                    //                    & " from ccVisitors Left Join ccVisits on ccVisits.VisitorID=ccVisitors.ID" _
                    //                    & " where ccVisits.ID is null"
                    //                Call cpCore.app.executeSql(sql)
                    //        End Select
                    //
                    // Delete People
                    //   created before DeleteBeforeDate,
                    //   created during a visit (not created by another process),
                    //   x with default name (created during a hit) - no, spider detect changes name
                    //   with no username (they are not planning on returning)
                    //   with 1 visit (not created with 0 visits, has not returned)
                    //
                    AppendClassLog(cpCore, appName, "HouseKeep_App_Daily_RemoveGuestRecords(" + appName + ")", "Deleting members with  LastVisit before DeleteBeforeDate [" + DeleteBeforeDate + "], exactly one total visit, a null username and a null email address.");
                    SQLCriteria = ""
                        + " (LastVisit<" + DeleteBeforeDateSQL + ")"
                        + " and(createdbyvisit=1)"
                        + " and(Visits=1)"
                        + " and(Username is null)"
                        + " and(email is null)";
                    cpCore.db.DeleteTableRecordChunks("default", "" + SQLTablePeople + "", SQLCriteria, 1000, 10000);
                    //& "(" & SQLTablePeople & ".Name=" & encodeSQLText(DefaultName) & ")"
                }
                //
                // restore sved timeout
                //
                cpCore.db.sqlCommandTimeout = TimeoutSave;
                return;
                //
            } catch (Exception ex) {
                cpCore.handleException(ex);
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
        public void HouseKeep_VisitSummary(coreClass cpCore, DateTime StartTimeDate, DateTime EndTimeDate, int HourDuration, string BuildVersion, DateTime OldestVisitSummaryWeCareAbout) {
            try {
                //
                //
                //Dim StartDate As Date
                double StartTimeHoursSinceMidnight = 0;
                DateTime PeriodStart = default(DateTime);
                double TotalTimeOnSite = 0;
                int MultiPageVisitCnt = 0;
                int MultiPageHitCnt = 0;
                double MultiPageTimetoLastHitSum = 0;
                double TimeOnSite = 0;
                //Dim PeriodStepInHours As Double
                DateTime PeriodDatePtr = default(DateTime);
                DateTime StartOfHour = default(DateTime);
                int DateNumber = 0;
                int TimeNumber = 0;
                double SumStartTime = 0;
                double SumStopTime = 0;
                int HoursPerDay = 0;
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
                DateTime OldestDateAdded = default(DateTime);
                object EmptyVariant = null;
                bool NeedToClearCache = false;
                int ArchiveParentID = 0;
                int RecordID = 0;
                int CS = 0;
                int LoopPtr = 0;
                int Ptr = 0;
                string LocalFile = null;
                string LocalFilename = null;
                string[] Folders = null;
                int FolderCnt = 0;
                string CollectionGUID = null;
                string CollectionName = null;
                int Pos = 0;
                string LastChangeDate = null;
                string SubFolderList = null;
                string[] SubFolders = null;
                string SubFolder = null;
                int Cnt = 0;
                string LocalGUID = null;
                string LocalLastChangeDateStr = null;
                DateTime LocalLastChangeDate = default(DateTime);
                string LibGUID = null;
                string LibLastChangeDateStr = null;
                DateTime LibLastChangeDate = default(DateTime);
                XmlNode LibListNode = null;
                XmlNode LocalListNode = null;
                XmlNode CollectionNode = null;
                XmlDocument LibraryCollections = new XmlDocument();
                XmlDocument LocalCollections = new XmlDocument();
                XmlDocument Doc = new XmlDocument();
                //Dim AppService As appServicesClass
                //Dim KernelService As KernelServicesClass
                string SetTimeCheckString = null;
                double SetTimeCheck = 0;
                DateTime LogDate = default(DateTime);
                string FolderName = null;
                string FileList = null;
                string[] FileArray = null;
                int FileArrayCount = 0;
                int FileArrayPointer = 0;
                string[] FileSplit = null;
                string FolderList = null;
                string[] FolderArray = null;
                int FolderArrayCount = 0;
                int FolderArrayPointer = 0;
                string[] FolderSplit = null;
                //Dim fs As New fileSystemClass
                int VisitArchiveAgeDays = 0;
                bool NewDay = false;
                bool NewHour = false;
                //
                DateTime LastTimeCheck = default(DateTime);
                //
                string ConfigFilename = null;
                string Config = null;
                string[] ConfigLines = null;
                //
                string Line = null;
                int LineCnt = 0;
                int LinePtr = 0;
                string[] NameValue = null;
                string SQLNow = null;
                string SQL = null;
                int AveReadTime = 0;
                //Dim AddonInstall As New addonInstallClass
                //
                if (string.CompareOrdinal(BuildVersion, cpCore.codeVersion()) < 0) {
                    //throw new ApplicationException("Unexpected exception");
                } else {
                    PeriodStart = StartTimeDate;
                    if (PeriodStart < OldestVisitSummaryWeCareAbout) {
                        PeriodStart = OldestVisitSummaryWeCareAbout;
                    }
                    StartTimeHoursSinceMidnight = PeriodStart.TimeOfDay.TotalHours;
                    PeriodStart = PeriodStart.Date.AddHours(StartTimeHoursSinceMidnight);
                    //PeriodStepInHours = CDbl(HourDuration) / 24.0!
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
                        SumStartTime = 0;
                        SumStopTime = 0;
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
                        SQL = "select count(v.id) as NoCookieVisits"
                            + " from ccvisits v"
                            + " where (v.CookieSupport<>1)"
                            + " and(v.dateadded>=" + cpCore.db.encodeSQLDate(DateStart) + ")"
                            + " and (v.dateadded<" + cpCore.db.encodeSQLDate(DateEnd) + ")"
                            + " and((v.ExcludeFromAnalytics is null)or(v.ExcludeFromAnalytics=0))"
                            + "";
                        //SQL = "select count(id) as NoCookieVisits from ccvisits where (CookieSupport<>1)and(dateadded>=" & encodeSQLDate(DateStart) & ")and(dateadded<" & encodeSQLDate(DateEnd) & ")"
                        CS = cpCore.db.csOpenSql_rev("default", SQL);
                        if (cpCore.db.csOk(CS)) {
                            NoCookieVisits = cpCore.db.csGetInteger(CS, "NoCookieVisits");
                        }
                        cpCore.db.csClose(ref CS);
                        //
                        // Total Visits
                        //
                        SQL = "select count(v.id) as VisitCnt ,Sum(v.PageVisits) as HitCnt ,sum(v.TimetoLastHit) as TimeOnSite"
                            + " from ccvisits v"
                            + " where (v.CookieSupport<>0)"
                            + " and(v.dateadded>=" + cpCore.db.encodeSQLDate(DateStart) + ")"
                            + " and (v.dateadded<" + cpCore.db.encodeSQLDate(DateEnd) + ")"
                            + " and((v.ExcludeFromAnalytics is null)or(v.ExcludeFromAnalytics=0))"
                            + "";
                        //SQL = "select count(id) as VisitCnt ,Sum(PageVisits) as HitCnt ,sum(TimetoLastHit) as TimeOnSite from ccvisits where (CookieSupport<>0)and(dateadded>=" & encodeSQLDate(DateStart) & ")and (dateadded<" & encodeSQLDate(DateEnd) & ")"
                        CS = cpCore.db.csOpenSql_rev("default", SQL);
                        if (cpCore.db.csOk(CS)) {
                            VisitCnt = cpCore.db.csGetInteger(CS, "VisitCnt");
                            HitCnt = cpCore.db.csGetInteger(CS, "HitCnt");
                            TimeOnSite = cpCore.db.csGetNumber(CS, "TimeOnSite");
                        }
                        cpCore.db.csClose(ref CS);
                        //
                        // Visits by new visitors
                        //
                        if (VisitCnt > 0) {
                            SQL = "select count(v.id) as NewVisitorVisits"
                                + " from ccvisits v"
                                + " where (v.CookieSupport<>0)"
                                + " and(v.dateadded>=" + cpCore.db.encodeSQLDate(DateStart) + ")"
                                + " and (v.dateadded<" + cpCore.db.encodeSQLDate(DateEnd) + ")"
                                + " and((v.ExcludeFromAnalytics is null)or(v.ExcludeFromAnalytics=0))"
                                + " and(v.VisitorNew<>0)"
                                + "";
                            //SQL = "select count(id) as NewVisitorVisits from ccvisits where (CookieSupport<>0)and(VisitorNew<>0)and(dateadded>=" & encodeSQLDate(DateStart) & ")and(dateadded<" & encodeSQLDate(DateEnd) & ")"
                            CS = cpCore.db.csOpenSql_rev("default", SQL);
                            if (cpCore.db.csOk(CS)) {
                                NewVisitorVisits = cpCore.db.csGetInteger(CS, "NewVisitorVisits");
                            }
                            cpCore.db.csClose(ref CS);
                            //
                            // Single Page Visits
                            //
                            SQL = "select count(v.id) as SinglePageVisits"
                                + " from ccvisits v"
                                + " where (v.CookieSupport<>0)"
                                + " and(v.dateadded>=" + cpCore.db.encodeSQLDate(DateStart) + ")"
                                + " and (v.dateadded<" + cpCore.db.encodeSQLDate(DateEnd) + ")"
                                + " and((v.ExcludeFromAnalytics is null)or(v.ExcludeFromAnalytics=0))"
                                + " and(v.PageVisits=1)"
                                + "";
                            //SQL = "select count(id) as SinglePageVisits from ccvisits where (CookieSupport<>0)and(PageVisits=1)and(dateadded>=" & encodeSQLDate(DateStart) & ")and(dateadded<" & encodeSQLDate(DateEnd) & ")"
                            CS = cpCore.db.csOpenSql_rev("default", SQL);
                            if (cpCore.db.csOk(CS)) {
                                SinglePageVisits = cpCore.db.csGetInteger(CS, "SinglePageVisits");
                            }
                            cpCore.db.csClose(ref CS);
                            //
                            // Multipage Visits
                            //
                            SQL = "select count(v.id) as VisitCnt ,sum(v.PageVisits) as HitCnt ,sum(v.TimetoLastHit) as TimetoLastHitSum "
                                + " from ccvisits v"
                                + " where (v.CookieSupport<>0)"
                                + " and(v.dateadded>=" + cpCore.db.encodeSQLDate(DateStart) + ")"
                                + " and (v.dateadded<" + cpCore.db.encodeSQLDate(DateEnd) + ")"
                                + " and((v.ExcludeFromAnalytics is null)or(v.ExcludeFromAnalytics=0))"
                                + " and(PageVisits>1)"
                                + "";
                            //SQL = "select count(id) as VisitCnt ,sum(PageVisits) as HitCnt ,sum(TimetoLastHit) as TimetoLastHitSum from ccvisits where (CookieSupport<>0)and(PageVisits>1)and(dateadded>=" & encodeSQLDate(DateStart) & ")and (dateadded<" & encodeSQLDate(DateEnd) & ")"
                            CS = cpCore.db.csOpenSql_rev("default", SQL);
                            if (cpCore.db.csOk(CS)) {
                                MultiPageVisitCnt = cpCore.db.csGetInteger(CS, "VisitCnt");
                                MultiPageHitCnt = cpCore.db.csGetInteger(CS, "HitCnt");
                                MultiPageTimetoLastHitSum = cpCore.db.csGetNumber(CS, "TimetoLastHitSum");
                            }
                            cpCore.db.csClose(ref CS);
                            //
                            // Authenticated Visits
                            //
                            SQL = "select count(v.id) as AuthenticatedVisits "
                                + " from ccvisits v"
                                + " where (v.CookieSupport<>0)"
                                + " and(v.dateadded>=" + cpCore.db.encodeSQLDate(DateStart) + ")"
                                + " and (v.dateadded<" + cpCore.db.encodeSQLDate(DateEnd) + ")"
                                + " and((v.ExcludeFromAnalytics is null)or(v.ExcludeFromAnalytics=0))"
                                + " and(VisitAuthenticated<>0)"
                                + "";
                            //SQL = "select count(id) as AuthenticatedVisits from ccvisits where (CookieSupport<>0)and(VisitAuthenticated<>0)and(dateadded>=" & encodeSQLDate(DateStart) & ")and(dateadded<" & encodeSQLDate(DateEnd) & ")"
                            CS = cpCore.db.csOpenSql_rev("default", SQL);
                            if (cpCore.db.csOk(CS)) {
                                AuthenticatedVisits = cpCore.db.csGetInteger(CS, "AuthenticatedVisits");
                            }
                            cpCore.db.csClose(ref CS);
                            //
                            if (true) {
                                //
                                // Mobile Visits
                                //
                                SQL = "select count(v.id) as cnt "
                                    + " from ccvisits v"
                                    + " where (v.CookieSupport<>0)"
                                    + " and(v.dateadded>=" + cpCore.db.encodeSQLDate(DateStart) + ")"
                                    + " and (v.dateadded<" + cpCore.db.encodeSQLDate(DateEnd) + ")"
                                    + " and((v.ExcludeFromAnalytics is null)or(v.ExcludeFromAnalytics=0))"
                                    + " and(Mobile<>0)"
                                    + "";
                                //SQL = "select count(id) as AuthenticatedVisits from ccvisits where (CookieSupport<>0)and(VisitAuthenticated<>0)and(dateadded>=" & encodeSQLDate(DateStart) & ")and(dateadded<" & encodeSQLDate(DateEnd) & ")"
                                CS = cpCore.db.csOpenSql_rev("default", SQL);
                                if (cpCore.db.csOk(CS)) {
                                    MobileVisits = cpCore.db.csGetInteger(CS, "cnt");
                                }
                                cpCore.db.csClose(ref CS);
                                //
                                // Bot Visits
                                //
                                SQL = "select count(v.id) as cnt "
                                    + " from ccvisits v"
                                    + " where (v.CookieSupport<>0)"
                                    + " and(v.dateadded>=" + cpCore.db.encodeSQLDate(DateStart) + ")"
                                    + " and (v.dateadded<" + cpCore.db.encodeSQLDate(DateEnd) + ")"
                                    + " and((v.ExcludeFromAnalytics is null)or(v.ExcludeFromAnalytics=0))"
                                    + " and(Bot<>0)"
                                    + "";
                                CS = cpCore.db.csOpenSql_rev("default", SQL);
                                if (cpCore.db.csOk(CS)) {
                                    BotVisits = cpCore.db.csGetInteger(CS, "cnt");
                                }
                                cpCore.db.csClose(ref CS);
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
                        CS = cpCore.db.csOpen("Visit Summary", "(timeduration=" + HourDuration + ")and(DateNumber=" + DateNumber + ")and(TimeNumber=" + TimeNumber + ")");
                        if (!cpCore.db.csOk(CS)) {
                            cpCore.db.csClose(ref CS);
                            CS = cpCore.db.csInsertRecord("Visit Summary", 0);
                        }
                        //
                        if (cpCore.db.csOk(CS)) {
                            cpCore.db.csSet(CS, "name", HourDuration + " hr summary for " + DateTime.FromOADate(DateNumber).ToShortDateString() + " " + TimeNumber + ":00");
                            cpCore.db.csSet(CS, "DateNumber", DateNumber);
                            cpCore.db.csSet(CS, "TimeNumber", TimeNumber);
                            cpCore.db.csSet(CS, "Visits", VisitCnt);
                            cpCore.db.csSet(CS, "PagesViewed", HitCnt);
                            cpCore.db.csSet(CS, "TimeDuration", HourDuration);
                            cpCore.db.csSet(CS, "NewVisitorVisits", NewVisitorVisits);
                            cpCore.db.csSet(CS, "SinglePageVisits", SinglePageVisits);
                            cpCore.db.csSet(CS, "AuthenticatedVisits", AuthenticatedVisits);
                            cpCore.db.csSet(CS, "NoCookieVisits", NoCookieVisits);
                            cpCore.db.csSet(CS, "AveTimeOnSite", AveTimeOnSite);
                            if (true) {
                                cpCore.db.csSet(CS, "MobileVisits", MobileVisits);
                                cpCore.db.csSet(CS, "BotVisits", BotVisits);
                            }
                        }
                        cpCore.db.csClose(ref CS);
                        PeriodDatePtr = PeriodDatePtr.AddHours(HourDuration);
                    }
                }
                //
                return;
            } catch (Exception ex) {
                cpCore.handleException(ex);
            }
            //ErrorTrap:
            //throw new ApplicationException("Unexpected exception");
        }
        //
        //======================================================================================
        //   Log a reported error
        //======================================================================================
        //
        public void AppendClassLog(coreClass cpcore, string ApplicationName, string MethodName, string LogCopy) {
            logController.appendLogWithLegacyRow(cpcore, ApplicationName, LogCopy, "ccHouseKeep", "HouseKeepClass", MethodName, 0, "", "", false, true, "", "HouseKeep", "");
        }
        //
        //Private Sub HandleClassTrapError(ByVal ApplicationName As String, ByVal MethodName As String, ByVal Cause As String, ByVal ResumeNext As Boolean)
        //    '
        //    //throw new ApplicationException("Unexpected exception")
        //    '
        //End Sub
        //
        // ----- temp solution to convert error reporting without spending the time right now
        //
        //Private Sub HandleClassInternalError(ByVal ApplicationName As String, ByVal MethodName As String, ByVal ErrNumber As Integer, ByVal Cause As String)
        //    '
        //    //throw new ApplicationException("Unexpected exception")
        //    '
        //End Sub
        //
        //
        //
        private void HouseKeep_App_Daily_LogFolder(coreClass cpCore, string FolderName, DateTime LastMonth) {
            try {
                //
                FileInfo[] FileList = null;
                string[] FileArray = null;
                int FileArrayCount = 0;
                int FileArrayPointer = 0;
                string[] FileSplit = null;
                //
                AppendClassLog(cpCore, cpCore.serverConfig.appConfig.name, "HouseKeep_App_Daily_LogFolder(" + cpCore.serverConfig.appConfig.name + ")", "Deleting files from folder [" + FolderName + "] older than " + LastMonth);
                FileList = cpCore.privateFiles.getFileList(FolderName);
                foreach (FileInfo file in FileList) {
                    if (file.CreationTime < LastMonth) {
                        cpCore.privateFiles.deleteFile(FolderName + "/" + file.Name);
                    }
                }
                return;
                //
            } catch (Exception ex) {
                cpCore.handleException(ex);
            }
            //ErrorTrap:
            //throw new ApplicationException("Unexpected exception");
        }
        //
        //
        //
        private bool DownloadUpdates(coreClass cpCore) {
            bool tempDownloadUpdates = false;
            bool loadOK = true;
            try {
                XmlDocument Doc = null;
                //INSTANT C# NOTE: Commented this declaration since looping variables in 'foreach' loops are declared in the 'foreach' header in C#:
                //				XmlNode CDefSection = null;
                string URL = null;
                string Copy = null;
                //
                Doc = new XmlDocument();
                URL = "http://support.contensive.com/GetUpdates?iv=" + cpCore.codeVersion();
                loadOK = true;
                Doc.Load(URL);
                if (Doc.DocumentElement.Name.ToLower() != genericController.vbLCase("ContensiveUpdate")) {
                    tempDownloadUpdates = false;
                } else {
                    if (Doc.DocumentElement.ChildNodes.Count == 0) {
                        tempDownloadUpdates = false;
                    } else {
                        foreach (XmlNode CDefSection in Doc.DocumentElement.ChildNodes) {
                            Copy = CDefSection.InnerText;
                            switch (genericController.vbLCase(CDefSection.Name)) {
                                case "mastervisitnamelist":
                                    //
                                    // Read in the interfaces and save to Add-ons
                                    //
                                    cpCore.privateFiles.saveFile("config\\VisitNameList.txt", Copy);
                                    //Call cpCore.app.privateFiles.SaveFile(getAppPath & "\config\DefaultBotNameList.txt", copy)
                                    break;
                                case "masteremailbouncefilters":
                                    //
                                    // save the updated filters file
                                    //
                                    cpCore.privateFiles.saveFile("config\\EmailBounceFilters.txt", Copy);
                                    //Call cpCore.app.privateFiles.SaveFile(getAppPath & "\cclib\config\Filters.txt", copy)
                                    break;
                                case "mastermobilebrowserlist":
                                    //
                                    // save the updated filters file
                                    //
                                    cpCore.privateFiles.saveFile("config\\MobileBrowserList.txt", Copy);
                                    break;
                            }
                        }
                    }
                }
            } catch (Exception ex) {
                //
                // error - Need a way to reach the user that submitted the file
                //
                tempDownloadUpdates = false;
                //throw new ApplicationException("Unexpected exception");
            }
            return loadOK;
        }
        //
        //=========================================================================================
        // Summarize the page views
        //   excludes non-cookie visits
        //   excludes administrator and developer visits
        //   excludes authenticated users with ExcludeFromReporting
        //
        //=========================================================================================
        //
        public void HouseKeep_PageViewSummary(coreClass cpCore, DateTime StartTimeDate, DateTime EndTimeDate, int HourDuration, string BuildVersion, DateTime OldestVisitSummaryWeCareAbout) {
            try {
                //
                //
                string baseCriteria = null;
                DateTime StartDate = default(DateTime);
                //Dim StartTime As Date
                DateTime PeriodStart = default(DateTime);
                //Dim TotalTimeOnSite
                int MultiPageVisitCnt = 0;
                int MultiPageHitCnt = 0;
                double MultiPageTimetoLastHitSum = 0;
                double TimeOnSite = 0;
                double PeriodStep = 0;
                DateTime PeriodDatePtr = default(DateTime);
                DateTime StartOfHour = default(DateTime);
                int DateNumber = 0;
                int TimeNumber = 0;
                double SumStartTime = 0;
                double SumStopTime = 0;
                int HoursPerDay = 0;
                DateTime DateStart = default(DateTime);
                DateTime DateEnd = default(DateTime);
                int NewVisitorVisits = 0;
                int SinglePageVisits = 0;
                int AuthenticatedVisits = 0;
                int NoCookieVisits = 0;
                double AveTimeOnSite = 0;
                int HitCnt = 0;
                int VisitCnt = 0;
                DateTime OldestDateAdded = default(DateTime);
                object EmptyVariant = null;
                bool NeedToClearCache = false;
                int ArchiveParentID = 0;
                int RecordID = 0;
                int CS = 0;
                //Dim AddonInstall As New addonInstallClass
                int LoopPtr = 0;
                int Ptr = 0;
                string LocalFile = null;
                string LocalFilename = null;
                string[] Folders = null;
                int FolderCnt = 0;
                string CollectionGUID = null;
                string CollectionName = null;
                int Pos = 0;
                string LastChangeDate = null;
                string SubFolderList = null;
                string[] SubFolders = null;
                string SubFolder = null;
                int Cnt = 0;
                string LocalGUID = null;
                string LocalLastChangeDateStr = null;
                DateTime LocalLastChangeDate = default(DateTime);
                string LibGUID = null;
                string LibLastChangeDateStr = null;
                DateTime LibLastChangeDate = default(DateTime);
                XmlNode LibListNode = null;
                XmlNode LocalListNode = null;
                XmlNode CollectionNode = null;
                XmlDocument LibraryCollections = new XmlDocument();
                XmlDocument LocalCollections = new XmlDocument();
                XmlDocument Doc = new XmlDocument();
                //Dim AppService As appServicesClass
                //Dim KernelService As KernelServicesClass
                string SetTimeCheckString = null;
                double SetTimeCheck = 0;
                DateTime LogDate = default(DateTime);
                string FolderName = null;
                string FileList = null;
                string[] FileArray = null;
                int FileArrayCount = 0;
                int FileArrayPointer = 0;
                string[] FileSplit = null;
                string FolderList = null;
                string[] FolderArray = null;
                int FolderArrayCount = 0;
                int FolderArrayPointer = 0;
                string[] FolderSplit = null;
                //Dim fs As New fileSystemClass
                int VisitArchiveAgeDays = 0;
                bool NewDay = false;
                bool NewHour = false;
                int CSPages = 0;
                int PageID = 0;
                string PageTitle = null;
                int NoCookiePageViews = 0;
                int PageViews = 0;
                int AuthenticatedPageViews = 0;
                int MobilePageViews = 0;
                int BotPageViews = 0;
                //
                DateTime LastTimeCheck = default(DateTime);
                //
                string ConfigFilename = null;
                string Config = null;
                string[] ConfigLines = null;
                //
                string Line = null;
                int LineCnt = 0;
                int LinePtr = 0;
                string[] NameValue = null;
                string SQLNow = null;
                string SQL = null;
                int AveReadTime = 0;

                //
                if (string.CompareOrdinal(BuildVersion, cpCore.codeVersion()) < 0) {
                    cpCore.handleException(new ApplicationException("Can not summarize analytics until this site's data needs been upgraded."));
                } else {
                    PeriodStart = StartTimeDate;
                    if (PeriodStart < OldestVisitSummaryWeCareAbout) {
                        PeriodStart = OldestVisitSummaryWeCareAbout;
                    }
                    StartDate = PeriodStart.Date;
                    PeriodStep = (double)HourDuration / 24.0F;
                    while (PeriodDatePtr < EndTimeDate) {
                        //
                        DateNumber = encodeInteger(PeriodDatePtr.AddHours(HourDuration / 2.0).ToOADate());
                        TimeNumber = encodeInteger(PeriodDatePtr.TimeOfDay.TotalHours);
                        DateStart = PeriodDatePtr.Date;
                        DateEnd = PeriodDatePtr.AddHours(HourDuration).Date;
                        //
                        VisitCnt = 0;
                        HitCnt = 0;
                        SumStartTime = 0;
                        SumStopTime = 0;
                        NewVisitorVisits = 0;
                        SinglePageVisits = 0;
                        MultiPageVisitCnt = 0;
                        MultiPageTimetoLastHitSum = 0;
                        AuthenticatedVisits = 0;
                        NoCookieVisits = 0;
                        AveTimeOnSite = 0;
                        PageTitle = "";
                        PageID = 0;
                        PageViews = 0;
                        AuthenticatedPageViews = 0;
                        MobilePageViews = 0;
                        BotPageViews = 0;
                        NoCookiePageViews = 0;
                        //
                        // Loop through all the pages hit during this period
                        //
                        //
                        // for now ignore the problem caused by addons like Blogs creating multiple 'pages' within on pageid
                        // One way to handle this is to expect the addon to set something unquie in he page title
                        // then use the title to distinguish a page. The problem with this is the current system puts the
                        // visit number and page number in the name. if we select on district name, they will all be.
                        //
                        SQL = "select distinct recordid,pagetitle from ccviewings h"
                            + " where (h.recordid<>0)"
                            + " and(h.dateadded>=" + cpCore.db.encodeSQLDate(DateStart) + ")"
                            + " and (h.dateadded<" + cpCore.db.encodeSQLDate(DateEnd) + ")"
                            + " and((h.ExcludeFromAnalytics is null)or(h.ExcludeFromAnalytics=0))"
                            + "order by recordid";
                        CSPages = cpCore.db.csOpenSql_rev("default", SQL);
                        if (!cpCore.db.csOk(CSPages)) {
                            //
                            // no hits found - add or update a single record for this day so we know it has been calculated
                            //
                            CS = cpCore.db.csOpen("Page View Summary", "(timeduration=" + HourDuration + ")and(DateNumber=" + DateNumber + ")and(TimeNumber=" + TimeNumber + ")and(pageid=" + PageID + ")and(pagetitle=" + cpCore.db.encodeSQLText(PageTitle) + ")");
                            if (!cpCore.db.csOk(CS)) {
                                cpCore.db.csClose(ref CS);
                                CS = cpCore.db.csInsertRecord("Page View Summary");
                            }
                            //
                            if (cpCore.db.csOk(CS)) {
                                cpCore.db.csSet(CS, "name", HourDuration + " hr summary for " + DateTime.FromOADate((double)DateNumber) + " " + TimeNumber + ":00, " + PageTitle);
                                cpCore.db.csSet(CS, "DateNumber", DateNumber);
                                cpCore.db.csSet(CS, "TimeNumber", TimeNumber);
                                cpCore.db.csSet(CS, "TimeDuration", HourDuration);
                                cpCore.db.csSet(CS, "PageViews", PageViews);
                                cpCore.db.csSet(CS, "PageID", PageID);
                                cpCore.db.csSet(CS, "PageTitle", PageTitle);
                                cpCore.db.csSet(CS, "AuthenticatedPageViews", AuthenticatedPageViews);
                                cpCore.db.csSet(CS, "NoCookiePageViews", NoCookiePageViews);
                                if (true) {
                                    cpCore.db.csSet(CS, "MobilePageViews", MobilePageViews);
                                    cpCore.db.csSet(CS, "BotPageViews", BotPageViews);
                                }
                            }
                            cpCore.db.csClose(ref CS);
                        } else {
                            //
                            // add an entry for each page hit on this day
                            //
                            while (cpCore.db.csOk(CSPages)) {
                                PageID = cpCore.db.csGetInteger(CSPages, "recordid");
                                PageTitle = cpCore.db.csGetText(CSPages, "pagetitle");
                                baseCriteria = ""
                                    + " (h.recordid=" + PageID + ")"
                                    + " "
                                    + " and(h.dateadded>=" + cpCore.db.encodeSQLDate(DateStart) + ")"
                                    + " and(h.dateadded<" + cpCore.db.encodeSQLDate(DateEnd) + ")"
                                    + " and((v.ExcludeFromAnalytics is null)or(v.ExcludeFromAnalytics=0))"
                                    + " and((h.ExcludeFromAnalytics is null)or(h.ExcludeFromAnalytics=0))"
                                    + "";
                                if (!string.IsNullOrEmpty(PageTitle)) {
                                    baseCriteria = baseCriteria + "and(h.pagetitle=" + cpCore.db.encodeSQLText(PageTitle) + ")";
                                }
                                //
                                // Total Page Views
                                //
                                SQL = "select count(h.id) as cnt"
                                    + " from ccviewings h left join ccvisits v on h.visitid=v.id"
                                    + " where " + baseCriteria + " and (v.CookieSupport<>0)"
                                    + "";
                                CS = cpCore.db.csOpenSql_rev("default", SQL);
                                if (cpCore.db.csOk(CS)) {
                                    PageViews = cpCore.db.csGetInteger(CS, "cnt");
                                }
                                cpCore.db.csClose(ref CS);
                                //
                                // Authenticated Visits
                                //
                                SQL = "select count(h.id) as cnt"
                                    + " from ccviewings h left join ccvisits v on h.visitid=v.id"
                                    + " where " + baseCriteria + " and(v.CookieSupport<>0)"
                                    + " and(v.visitAuthenticated<>0)"
                                    + "";
                                CS = cpCore.db.csOpenSql_rev("default", SQL);
                                if (cpCore.db.csOk(CS)) {
                                    AuthenticatedPageViews = cpCore.db.csGetInteger(CS, "cnt");
                                }
                                cpCore.db.csClose(ref CS);
                                //
                                // No Cookie Page Views
                                //
                                SQL = "select count(h.id) as NoCookiePageViews"
                                    + " from ccviewings h left join ccvisits v on h.visitid=v.id"
                                    + " where " + baseCriteria + " and((v.CookieSupport=0)or(v.CookieSupport is null))"
                                    + "";
                                CS = cpCore.db.csOpenSql_rev("default", SQL);
                                if (cpCore.db.csOk(CS)) {
                                    NoCookiePageViews = cpCore.db.csGetInteger(CS, "NoCookiePageViews");
                                }
                                cpCore.db.csClose(ref CS);
                                //
                                if (true) {
                                    //
                                    // Mobile Visits
                                    //
                                    SQL = "select count(h.id) as cnt"
                                        + " from ccviewings h left join ccvisits v on h.visitid=v.id"
                                        + " where " + baseCriteria + " and(v.CookieSupport<>0)"
                                        + " and(v.mobile<>0)"
                                        + "";
                                    CS = cpCore.db.csOpenSql_rev("default", SQL);
                                    if (cpCore.db.csOk(CS)) {
                                        MobilePageViews = cpCore.db.csGetInteger(CS, "cnt");
                                    }
                                    cpCore.db.csClose(ref CS);
                                    //
                                    // Bot Visits
                                    //
                                    SQL = "select count(h.id) as cnt"
                                        + " from ccviewings h left join ccvisits v on h.visitid=v.id"
                                        + " where " + baseCriteria + " and(v.CookieSupport<>0)"
                                        + " and(v.bot<>0)"
                                        + "";
                                    CS = cpCore.db.csOpenSql_rev("default", SQL);
                                    if (cpCore.db.csOk(CS)) {
                                        BotPageViews = cpCore.db.csGetInteger(CS, "cnt");
                                    }
                                    cpCore.db.csClose(ref CS);
                                }
                                //
                                // Add or update the Visit Summary Record
                                //
                                CS = cpCore.db.csOpen("Page View Summary", "(timeduration=" + HourDuration + ")and(DateNumber=" + DateNumber + ")and(TimeNumber=" + TimeNumber + ")and(pageid=" + PageID + ")and(pagetitle=" + cpCore.db.encodeSQLText(PageTitle) + ")");
                                if (!cpCore.db.csOk(CS)) {
                                    cpCore.db.csClose(ref CS);
                                    CS = cpCore.db.csInsertRecord("Page View Summary");
                                }
                                //
                                if (cpCore.db.csOk(CS)) {
                                    string PageName = null;

                                    if (string.IsNullOrEmpty(PageTitle)) {
                                        PageName = cpCore.db.getRecordName("page content", PageID);
                                        cpCore.db.csSet(CS, "name", HourDuration + " hr summary for " + DateTime.FromOADate((double)DateNumber) + " " + TimeNumber + ":00, " + PageName);
                                        cpCore.db.csSet(CS, "PageTitle", PageName);
                                    } else {
                                        cpCore.db.csSet(CS, "name", HourDuration + " hr summary for " + DateTime.FromOADate((double)DateNumber) + " " + TimeNumber + ":00, " + PageTitle);
                                        cpCore.db.csSet(CS, "PageTitle", PageTitle);
                                    }
                                    cpCore.db.csSet(CS, "DateNumber", DateNumber);
                                    cpCore.db.csSet(CS, "TimeNumber", TimeNumber);
                                    cpCore.db.csSet(CS, "TimeDuration", HourDuration);
                                    cpCore.db.csSet(CS, "PageViews", PageViews);
                                    cpCore.db.csSet(CS, "PageID", PageID);
                                    cpCore.db.csSet(CS, "AuthenticatedPageViews", AuthenticatedPageViews);
                                    cpCore.db.csSet(CS, "NoCookiePageViews", NoCookiePageViews);
                                    if (true) {
                                        cpCore.db.csSet(CS, "MobilePageViews", MobilePageViews);
                                        cpCore.db.csSet(CS, "BotPageViews", BotPageViews);
                                    }
                                }
                                cpCore.db.csClose(ref CS);
                                cpCore.db.csGoNext(CSPages);
                            }
                        }
                        cpCore.db.csClose(ref CSPages);
                        PeriodDatePtr = PeriodDatePtr.AddHours(HourDuration);
                    }
                }
                //
                return;
            } catch (Exception ex) {
                cpCore.handleException(ex);
            }
            //ErrorTrap:
            //throw new ApplicationException("Unexpected exception");
        }
        //
        //====================================================================================================
        public void housekeepAddonFolder(coreClass cpCore) {
            try {
                string RegisterPathList = null;
                string RegisterPath = null;
                string[] RegisterPaths = null;
                string Path = null;
                int NodeCnt = 0;
                bool IsActiveFolder = false;
                string Cmd = null;
                string CollectionRootPath = null;
                int Pos = 0;
                DirectoryInfo[] FolderList = null;
                string LocalPath = null;
                string LocalGuid = null;
                XmlDocument Doc = new XmlDocument();
                //INSTANT C# NOTE: Commented this declaration since looping variables in 'foreach' loops are declared in the 'foreach' header in C#:
                //				XmlNode CollectionNode = null;
                //INSTANT C# NOTE: Commented this declaration since looping variables in 'foreach' loops are declared in the 'foreach' header in C#:
                //				XmlNode LocalListNode = null;
                int FolderPtr = 0;
                string CollectionPath = null;
                DateTime LastChangeDate = default(DateTime);
                string hint = "";
                string LocalName = null;
                int Ptr = 0;
                string collectionFileFilename = null;
                //
                AppendClassLog(cpCore, "Server", "RegisterAddonFolder", "Entering RegisterAddonFolder");
                //
                bool loadOK = true;
                try {
                    collectionFileFilename = cpCore.privateFiles.rootLocalPath + cpCore.addon.getPrivateFilesAddonPath() + "Collections.xml";
                    Doc.Load(collectionFileFilename);
                } catch (Exception) {
                    AppendClassLog(cpCore, "Server", "", "RegisterAddonFolder, Hint=[" + hint + "], Error loading Collections.xml file.");
                    loadOK = false;
                }
                if (loadOK) {
                    //
                    AppendClassLog(cpCore, "Server", "RegisterAddonFolder", "Collection.xml loaded ok");
                    //
                    if (genericController.vbLCase(Doc.DocumentElement.Name) != genericController.vbLCase(CollectionListRootNode)) {
                        AppendClassLog(cpCore, "Server", "", "RegisterAddonFolder, Hint=[" + hint + "], The Collections.xml file has an invalid root node, [" + Doc.DocumentElement.Name + "] was received and [" + CollectionListRootNode + "] was expected.");
                    } else {
                        //
                        AppendClassLog(cpCore, "Server", "RegisterAddonFolder", "Collection.xml root name ok");
                        //
                        if (true) {
                            //If genericController.vbLCase(.name) <> "collectionlist" Then
                            //    Call AppendClassLog(cpcore,"Server", "", "RegisterAddonFolder, basename was not collectionlist, [" & .name & "].")
                            //Else
                            NodeCnt = 0;
                            RegisterPathList = "";
                            foreach (XmlNode LocalListNode in Doc.DocumentElement.ChildNodes) {
                                //
                                // Get the collection path
                                //
                                CollectionPath = "";
                                LocalGuid = "";
                                LocalName = "no name found";
                                LocalPath = "";
                                switch (genericController.vbLCase(LocalListNode.Name)) {
                                    case "collection":
                                        LocalGuid = "";
                                        foreach (XmlNode CollectionNode in LocalListNode.ChildNodes) {
                                            switch (genericController.vbLCase(CollectionNode.Name)) {
                                                case "name":
                                                    //
                                                    LocalName = genericController.vbLCase(CollectionNode.InnerText);
                                                    break;
                                                case "guid":
                                                    //
                                                    LocalGuid = genericController.vbLCase(CollectionNode.InnerText);
                                                    break;
                                                case "path":
                                                    //
                                                    CollectionPath = genericController.vbLCase(CollectionNode.InnerText);
                                                    break;
                                                case "lastchangedate":
                                                    LastChangeDate = genericController.encodeDate(CollectionNode.InnerText);
                                                    break;
                                            }
                                        }
                                        break;
                                }
                                //
                                AppendClassLog(cpCore, "Server", "RegisterAddonFolder", "Node[" + NodeCnt + "], LocalName=[" + LocalName + "], LastChangeDate=[" + LastChangeDate + "], CollectionPath=[" + CollectionPath + "], LocalGuid=[" + LocalGuid + "]");
                                //
                                // Go through all subpaths of the collection path, register the version match, unregister all others
                                //
                                //fs = New fileSystemClass
                                if (string.IsNullOrEmpty(CollectionPath)) {
                                    //
                                    AppendClassLog(cpCore, "Server", "RegisterAddonFolder", "no collection path, skipping");
                                    //
                                } else {
                                    CollectionPath = genericController.vbLCase(CollectionPath);
                                    CollectionRootPath = CollectionPath;
                                    Pos = CollectionRootPath.LastIndexOf("\\") + 1;
                                    if (Pos <= 0) {
                                        //
                                        AppendClassLog(cpCore, "Server", "RegisterAddonFolder", "CollectionPath has no '\\', skipping");
                                        //
                                    } else {
                                        CollectionRootPath = CollectionRootPath.Left(Pos - 1);
                                        Path = cpCore.addon.getPrivateFilesAddonPath() + "\\" + CollectionRootPath + "\\";
                                        FolderList = new DirectoryInfo[0];
                                        if (cpCore.privateFiles.pathExists(Path)) {
                                            FolderList = cpCore.privateFiles.getFolderList(Path);
                                            //INSTANT C# TODO TASK: Calls to the VB 'Err' function are not converted by Instant C#:
                                            if (0 != 0) {
                                                //INSTANT C# TODO TASK: Calls to the VB 'Err' function are not converted by Instant C#:
                                                //Microsoft.VisualBasic.Information.Err().Clear();
                                            }
                                        }
                                        if (FolderList.Length == 0) {
                                            //
                                            AppendClassLog(cpCore, "Server", "RegisterAddonFolder", "no subfolders found in physical path [" + Path + "], skipping");
                                            //
                                        } else {
                                            foreach (DirectoryInfo dir in FolderList) {
                                                IsActiveFolder = false;
                                                //
                                                // register or unregister all files in this folder
                                                //
                                                if (string.IsNullOrEmpty(dir.Name)) {
                                                    //
                                                    AppendClassLog(cpCore, "Server", "RegisterAddonFolder", "....empty folder [" + dir.Name + "], skipping");
                                                    //
                                                } else {
                                                    //
                                                    AppendClassLog(cpCore, "Server", "RegisterAddonFolder", "....Folder [" + dir.Name + "]");
                                                    IsActiveFolder = (CollectionRootPath + "\\" + dir.Name == CollectionPath);
                                                    if (IsActiveFolder && (FolderPtr != (FolderList.Length - 1))) {
                                                        //
                                                        // This one is active, but not the last
                                                        //
                                                        AppendClassLog(cpCore, "Server", "RegisterAddonFolder", "....Active addon is not the most current, this folder is the active folder, but there are more recent folders. This folder will be preserved.");
                                                    }
                                                    // 20161005 - no longer need to register activeX
                                                    //FileList = cpCore.app.privateFiles.GetFolderFiles(Path & "\" & dir.Name)
                                                    //For Each file As FileInfo In FileList
                                                    //    If Right(file.Name, 4) = ".dll" Then
                                                    //        If IsActiveFolder Then
                                                    //            '
                                                    //            ' register this file
                                                    //            '
                                                    //            RegisterPathList = RegisterPathList & vbCrLf & Path & dir.Name & "\" & file.Name
                                                    //            '                                                                Cmd = "%comspec% /c regsvr32 """ & RegisterPathList & """ /s"
                                                    //            '                                                                Call AppendClassLog(cpcore,"Server", "RegisterAddonFolder", "....Regsiter DLL [" & Cmd & "]")
                                                    //            '                                                                Call runProcess(cpCore,Cmd, , True)
                                                    //        Else
                                                    //            '
                                                    //            ' unregister this file
                                                    //            '
                                                    //            Cmd = "%comspec% /c regsvr32 /u """ & Path & dir.Name & "\" & file.Name & """ /s"
                                                    //            Call AppendClassLog(cpcore,"Server", "RegisterAddonFolder", "....Unregsiter DLL [" & Cmd & "]")
                                                    //            Call runProcess(cpCore, Cmd, , True)
                                                    //        End If
                                                    //    End If
                                                    //Next
                                                    //
                                                    // only keep last two non-matching folders and the active folder
                                                    //
                                                    if (IsActiveFolder) {
                                                        //IsActiveFolder = IsActiveFolder;
                                                    } else {
                                                        if (FolderPtr < (FolderList.Length - 3)) {
                                                            AppendClassLog(cpCore, "Server", "RegisterAddonFolder", "....Deleting path because non-active and not one of the newest 2 [" + Path + dir.Name + "]");
                                                            cpCore.privateFiles.deleteFolder(Path + dir.Name);
                                                        }
                                                    }
                                                }
                                            }
                                            //
                                            // register files found in the active folder last
                                            //
                                            if (!string.IsNullOrEmpty(RegisterPathList)) {
                                                RegisterPaths = genericController.stringSplit(RegisterPathList, "\r\n");
                                                for (Ptr = 0; Ptr <= RegisterPaths.GetUpperBound(0); Ptr++) {
                                                    RegisterPath = RegisterPaths[Ptr].Trim(' ');
                                                    if (!string.IsNullOrEmpty(RegisterPath)) {
                                                        Cmd = "%comspec% /c regsvr32 \"" + RegisterPath + "\" /s";
                                                        AppendClassLog(cpCore, "Server", "RegisterAddonFolder", "....Register DLL [" + Cmd + "]");
                                                        runProcess(cpCore, Cmd, "", true);
                                                    }
                                                }
                                                RegisterPathList = "";
                                            }
                                        }
                                    }
                                }
                                NodeCnt = NodeCnt + 1;
                            }
                            // 20161005 - no longer need to register activeX
                            //
                            // register files found in the active folder last
                            //
                            //If RegisterPathList <> "" Then
                            //    RegisterPaths = Split(RegisterPathList, vbCrLf)
                            //    For Ptr = 0 To UBound(RegisterPaths)
                            //        RegisterPath = Trim(RegisterPaths[Ptr])
                            //        If RegisterPath <> "" Then
                            //            Cmd = "%comspec% /c regsvr32 """ & RegisterPath & """ /s"
                            //            Call AppendClassLog(cpcore,"Server", "RegisterAddonFolder", "....Register DLL [" & Cmd & "]")
                            //            Call runProcess(cpCore, Cmd, , True)
                            //        End If
                            //    Next
                            //End If
                        }
                    }
                }
                //
                AppendClassLog(cpCore, "Server", "RegisterAddonFolder", "Exiting RegisterAddonFolder");
            } catch (Exception ex) {
                throw new ApplicationException("Unexpected Exception", ex);
            }
        }
    }
}
