﻿
using System;
using Amazon.S3.Model.Internal.MarshallTransformations;
using Contensive.Processor.Controllers;
using Contensive.Processor.Models.Domain;
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
        /// returns true if daily has not run today, and it is after the house-keep-hour
        /// </summary>
        public bool runDailyTasks {
            get {
                return ((core.dateTimeNowMockable.Date > lastCheckDateTime.Date) && (serverHousekeepHour < core.dateTimeNowMockable.Hour));
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// the last time housekeep was run
        /// </summary>
        public DateTime lastCheckDateTime { get { return core.siteProperties.getDate("housekeep, last check", default); } }
        //
        //====================================================================================================
        /// <summary>
        /// The hour of the day when daily housekeep should run
        /// </summary>
        public int serverHousekeepHour { get { return core.siteProperties.getInteger("housekeep, run time hour", 2); } }
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
        /// days that we keep simple archive records like visits. Not for summary files like visitsummary
        /// </summary>
        public int visitArchiveAgeDays {
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
                return core.dateTimeNowMockable.AddDays(-visitArchiveAgeDays).Date;
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
        //
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