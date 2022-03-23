
using Contensive.Processor.Controllers;
using System;
//
namespace Contensive.Processor.Addons.Housekeeping {
    //
    //====================================================================================================
    /// <summary>
    /// housekeep environment, to facilitate argument passing
    /// </summary>
    public class HouseKeepEnvironmentModel {
        //
        public readonly CoreController core;
        //
        //====================================================================================================
        /// <summary>
        /// append the temp housekeep log
        /// </summary>
        /// <param name="message"></param>
        public void log( string message ) {
            //
            // -- add to the info log
            LogController.logInfo(core, message);
            //
            // -- append to the temp/housekeep log
            DateTime rightNow = DateTime.Now;
            string pathFilename = "housekeepLog\\" + (rightNow.Year - 2000).ToString() + rightNow.Month.ToString().PadLeft(2, '0') + rightNow.Day.ToString().PadLeft(2, '0') + ".log";
            core.tempFiles.appendFile(pathFilename , rightNow.ToString() + "\t" + message + "\r\n");
        }
        //
        //====================================================================================================
        /// <summary>
        /// calls to housekeeping will force both the hourly and daily to run
        /// </summary>
        public bool forceHousekeep {
            get {
                return core.docProperties.getBoolean("force");
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// the last time hourly housekeep was run
        /// </summary>
        public DateTime baseHousekeepHourlyLastRunTime { 
            get { 
                return core.siteProperties.getDate("base housekeep, last hourly run", default); 
            } 
            set {
                core.siteProperties.setProperty("base housekeep, last hourly run", value);
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// the last time daily housekeep was run
        /// </summary>
        public DateTime baseHousekeepDailyLastRunTime { 
            get { 
                return core.siteProperties.getDate("base housekeep, last daily run", default); 
            } 
            set {
                core.siteProperties.setProperty("base housekeep, last daily run", value);
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// The hour of the day when daily housekeep should run
        /// </summary>
        public int baseHousekeepRunHour { get { return core.siteProperties.getInteger("base housekeep, run time hour", 2); } }
        //
        //====================================================================================================
        /// <summary>
        /// day before current mockable date
        /// </summary>
        public DateTime yesterday { get { return core.dateTimeNowMockable.AddDays(-1).Date; } }
        //
        //====================================================================================================
        /// <summary>
        /// 90 days ago
        /// </summary>
        public DateTime aLittleWhileAgo { get { return core.dateTimeNowMockable.AddDays(-90).Date; } }
        //
        //====================================================================================================
        /// <summary>
        /// oldest visit we care about (30 days)
        /// </summary>
        public DateTime oldestVisitSummaryWeCareAbout {
            get {
                DateTime oldestVisitSummaryWeCareAbout = core.dateTimeNowMockable.Date.AddDays(-30);
                if (oldestVisitSummaryWeCareAbout < visitArchiveDate) {
                    oldestVisitSummaryWeCareAbout = visitArchiveDate;
                }
                return oldestVisitSummaryWeCareAbout;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// days that we keep simple archive records like visits, viewings, activitylogs. Not for summary files like visitsummary
        /// minimum 2 days for sites with no archive features
        /// 
        /// </summary>
        public int archiveAgeDays {
            get {
                //
                // -- Get ArchiveAgeDays - use this as the oldest data they care about
                int visitArchiveAgeDays = core.siteProperties.getInteger("ArchiveRecordAgeDays", 2);
                if (visitArchiveAgeDays < 2) {
                    visitArchiveAgeDays = 2;
                    core.siteProperties.setProperty("ArchiveRecordAgeDays", 2);
                }
                return visitArchiveAgeDays;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// The date before which we delete archives
        /// </summary>
        public DateTime visitArchiveDate {
            get {
                return core.dateTimeNowMockable.AddDays(-archiveAgeDays).Date;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// how long we keep guest records
        /// </summary>
        public int guestArchiveAgeDays {
            get {
                //
                // -- Get GuestArchiveAgeDays
                int guestArchiveAgeDays = core.siteProperties.getInteger("ArchivePeopleAgeDays", 2);
                if (guestArchiveAgeDays < 2) {
                    guestArchiveAgeDays = 2;
                    core.siteProperties.setProperty("ArchivePeopleAgeDays", guestArchiveAgeDays);
                }
                return guestArchiveAgeDays;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// how many days the email drop and email log data are kept
        /// </summary>
        public int emailDropArchiveAgeDays {
            get {
                //
                // -- Get EmailDropArchiveAgeDays
                int emailDropArchiveAgeDays = core.siteProperties.getInteger("ArchiveEmailDropAgeDays", 90);
                if (emailDropArchiveAgeDays < 2) {
                    emailDropArchiveAgeDays = 2;
                    core.siteProperties.setProperty("ArchiveEmailDropAgeDays", emailDropArchiveAgeDays);
                }
                if (emailDropArchiveAgeDays > 365) {
                    emailDropArchiveAgeDays = 365;
                    core.siteProperties.setProperty("ArchiveEmailDropAgeDays", emailDropArchiveAgeDays);
                }
                return emailDropArchiveAgeDays;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// How many days the email log stores the email body (large data)
        /// </summary>
        public int emailLogBodyRetainDays { get { return GenericController.encodeInteger(core.siteProperties.getText("EmailLogBodyRetainDays", "7")); } }
        //
        //====================================================================================================
        /// <summary>
        /// how long to keep no-cookie visits
        /// </summary>
        public bool archiveDeleteNoCookie { get { return core.siteProperties.getBoolean("ArchiveDeleteNoCookie", true); } }
        //
        //====================================================================================================
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="core"></param>
        public HouseKeepEnvironmentModel(CoreController core) {
            try {
                this.core = core;
                core.siteProperties.setProperty("housekeep, last check", core.dateTimeNowMockable);
            } catch (Exception ex) {
                LogController.logError(core, ex);
            }
        }
    }
}