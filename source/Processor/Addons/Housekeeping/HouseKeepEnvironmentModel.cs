
using Contensive.Processor.Controllers;
using NLog;
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
        // static logger
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
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
            logger.Info($"{core.logCommonMessage},{message}");
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
                DateTime result = core.dateTimeNowMockable.Date.AddDays(-30);
                if (result < visitArchiveDate) {
                    result = visitArchiveDate;
                }
                return result;
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
                int result = core.siteProperties.getInteger("ArchiveRecordAgeDays", 2);
                if (result < 2) {
                    result = 2;
                    core.siteProperties.setProperty("ArchiveRecordAgeDays", 2);
                }
                return result;
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
                int result = core.siteProperties.getInteger("ArchivePeopleAgeDays", 2);
                if (result < 2) {
                    result = 2;
                    core.siteProperties.setProperty("ArchivePeopleAgeDays", result);
                }
                return result;
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
                int result = core.siteProperties.getInteger("ArchiveEmailDropAgeDays", 90);
                if (result < 2) {
                    result = 2;
                    core.siteProperties.setProperty("ArchiveEmailDropAgeDays", result);
                }
                if (result > 365) {
                    result = 365;
                    core.siteProperties.setProperty("ArchiveEmailDropAgeDays", result);
                }
                return result;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// How many days the email log stores the email body (large data)
        /// </summary>
        public int emailLogBodyRetainDays { get { return GenericController.getInteger(core.siteProperties.getText("EmailLogBodyRetainDays", "7")); } }
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
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
            }
        }
    }
}