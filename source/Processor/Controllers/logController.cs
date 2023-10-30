
using System;
using System.Data;
using NLog;
using NLog.Config;
using System.Collections.Generic;
using NLog.AWS.Logger;
using System.Globalization;
using Contensive.BaseClasses;
using Contensive.Models.Db;
//
namespace Contensive.Processor.Controllers {
    //
    //====================================================================================================
    /// <summary>
    /// static class controller
    /// nlog: http://nlog-project.org/
    /// base configuration from: https://brutaldev.com/post/logging-setup-in-5-minutes-with-nlog
    /// </summary>
    public static class LogController {
        //
        //=============================================================================
        /// <summary>
        /// configure target "aws" for AWS CloudWatch
        /// </summary>
        /// <param name="core"></param>
        public static void awsConfigure(CoreController core) {
            if (core.serverConfig == null) { return; }
            if (string.IsNullOrWhiteSpace(core.serverConfig.awsCloudWatchLogGroup)) { return; }
            var awsTarget = new AWSTarget {
                Layout = "${longdate}|${level:uppercase=true}|${callsite}|${message}",
                LogGroup = core.serverConfig.awsCloudWatchLogGroup,
                Region = core.serverConfig.awsRegionName,
                Credentials = new Amazon.Runtime.BasicAWSCredentials(core.secrets.awsAccessKey, core.secrets.awsSecretAccessKey),
                LogStreamNamePrefix = core.appConfig.name,
                LogStreamNameSuffix = "NLog",
                MaxQueuedMessages = 5000
            };
            var config = new LoggingConfiguration();
            config.AddTarget("aws", awsTarget);
            config.LoggingRules.Add(new LoggingRule("*", LogLevel.Trace, awsTarget));
            LogManager.Configuration = config;
        }
        //
        //=============================================================================
        /// <summary>
        /// create the error log message with core
        /// </summary>
        /// <param name="core"></param>
        /// <param name="message"></param>
        /// <param name="addtoUI">Iftrue, the message will be appended to the doc object, and if applicable, added to the UI/</param>
        /// <returns></returns>
        public static string processLogMessage(CoreController core, string message, bool addtoUI) {
            string result = "app [" + ((core.appConfig != null) ? core.appConfig.name : "no-app") + "]";
            result += ", thread [" + System.Threading.Thread.CurrentThread.ManagedThreadId.ToString("000") + "]";
            result += ", url [" + ((core.webServer == null) ? "non-web" : string.IsNullOrEmpty(core.webServer.requestPathPage) ? "empty" : core.webServer.requestPathPage) + "]";
            result += ", " + message.Replace(Environment.NewLine, " ").Replace("\n", " ").Replace("\r", " ");
            //
            // -- add to doc exception list to display at top of webpage
            if (!addtoUI) { return result; }
            if (core.doc.errorList == null) { core.doc.errorList = new List<string>(); }
            if (core.doc.errorList.Count == 10) { core.doc.errorList.Add("Exception limit exceeded"); }
            if (core.doc.errorList.Count >= 10) { return result; }
            core.doc.errorList.Add(result);
            return result;
        }
        //
        //=============================================================================
        /// <summary>
        /// Convert internal logLevels to NLog, decoupling log levels so we don't expose NLog classes
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        public static NLog.LogLevel getNLogLogLevel(BaseClasses.CPLogBaseClass.LogLevel level) {
            //
            // -- decouple NLog types from internal enum
            return level switch {
                CPLogBaseClass.LogLevel.Trace => LogLevel.Trace,
                CPLogBaseClass.LogLevel.Debug => LogLevel.Debug,
                CPLogBaseClass.LogLevel.Warn => LogLevel.Warn,
                CPLogBaseClass.LogLevel.Error => LogLevel.Error,
                CPLogBaseClass.LogLevel.Fatal => LogLevel.Fatal,
                _ => LogLevel.Info,
            };
        }
        //
        //=============================================================================
        /// <summary>
        /// log any level with NLOG without messageLine formatting. Only use in extreme cases where the application environment is not stable.
        /// </summary>
        /// <param name="messageLine"></param>
        /// <param name="level"></param>
        public static void logShortLine(string messageLine, BaseClasses.CPLogBaseClass.LogLevel level) {
            try {
                logger.Log(typeof(LogController), new LogEventInfo(getNLogLogLevel(level), logger.Name, messageLine));
            } catch (Exception) {
                // -- throw away errors in error-handling
            }
        }
        //
        //=============================================================================
        /// <summary>
        /// Log all levels if configured, else log errors and above
        /// </summary>
        /// <param name="core"></param>
        /// <param name="message"></param>
        /// <param name="level"></param>
        public static void log(CoreController core, string message, BaseClasses.CPLogBaseClass.LogLevel level) {
            try {
                string messageLine = processLogMessage(core, message, level >= BaseClasses.CPLogBaseClass.LogLevel.Warn);
                logger.Log(typeof(LogController), new LogEventInfo(getNLogLogLevel(level), logger.Name, messageLine));
            } catch (Exception) {
                // -- throw away errors in error-handling
            }
        }
        //
        //=============================================================================
        /// <summary>
        /// log highest level, most important messages
        /// </summary>
        /// <param name="core"></param>
        /// <param name="message"></param>
        /// <remarks></remarks>
        [Obsolete("Use nlog instance in each class", false)]
        public static void logFatal(CoreController core, string message)
            => log(core, message, BaseClasses.CPLogBaseClass.LogLevel.Fatal);
        //
        //=============================================================================
        /// <summary>
        /// Normal behavior like mail sent, user updated profile etc
        /// </summary>
        /// <param name="core"></param>
        /// <param name="message"></param>
        /// <remarks></remarks>        
        public static void logInfo(CoreController core, string message)
            => log(core, message, BaseClasses.CPLogBaseClass.LogLevel.Info);
        //
        //=============================================================================
        /// <summary>
        /// log begin method, end method, etc
        /// </summary>
        /// <param name="core"></param>
        /// <param name="message"></param>
        /// <remarks></remarks>
        public static void logTrace(CoreController core, string message)
            => log(core, message, BaseClasses.CPLogBaseClass.LogLevel.Trace);
        //
        //=============================================================================
        /// <summary>
        /// log for special cases, debugging
        /// </summary>
        /// <param name="core"></param>
        /// <param name="message"></param>
        public static void logDebug(CoreController core, string message)
            => log(core, message, BaseClasses.CPLogBaseClass.LogLevel.Debug);
        //
        //=============================================================================
        /// <summary>
        /// log incorrect behavior but the application can continue
        /// </summary>
        /// <param name="core"></param>
        /// <param name="message"></param>
        /// <remarks></remarks>
        public static void logWarn(CoreController core, string message)
            => log(core, message, BaseClasses.CPLogBaseClass.LogLevel.Warn);
        //
        //====================================================================================================
        /// <summary>
        /// Standard log
        /// </summary>
        /// <param name="core"></param>
        /// <param name="ex"></param>
        /// <param name="cause"></param>
        public static void logError(CoreController core, Exception ex, string cause) {
            log(core, cause + ", exception [" + ex + "]", BaseClasses.CPLogBaseClass.LogLevel.Error);
        }
        //
        //====================================================================================================
        /// <summary>
        /// Standard log
        /// </summary>
        /// <param name="core"></param>
        /// <param name="ex"></param>
        public static void logError(CoreController core, Exception ex) {
            log(core, "exception [" + ex + "]", BaseClasses.CPLogBaseClass.LogLevel.Error);
        }
        //
        //====================================================================================================
        /// <summary>
        /// Standard log
        /// </summary>
        /// <param name="core"></param>
        /// <param name="ex"></param>
        /// <param name="cause"></param>
        public static void logWarn(CoreController core, Exception ex, string cause) {
            log(core, cause + ", exception [" + ex + "]", BaseClasses.CPLogBaseClass.LogLevel.Warn);
        }
        //
        //====================================================================================================
        /// <summary>
        /// Standard log
        /// </summary>
        /// <param name="core"></param>
        /// <param name="ex"></param>
        public static void logWarn(CoreController core, Exception ex) {
            log(core, "exception [" + ex + "]", BaseClasses.CPLogBaseClass.LogLevel.Warn);
        }
        //
        //====================================================================================================
        /// <summary>
        /// Standard log
        /// </summary>
        /// <param name="core"></param>
        /// <param name="ex"></param>
        /// <param name="cause"></param>
        public static void logFatal(CoreController core, Exception ex, string cause) {
            log(core, cause + ", exception [" + ex + "]", BaseClasses.CPLogBaseClass.LogLevel.Fatal);
        }
        //
        //====================================================================================================
        /// <summary>
        /// Standard log
        /// </summary>
        /// <param name="core"></param>
        /// <param name="ex"></param>
        public static void logFatal(CoreController core, Exception ex) {
            log(core, "exception [" + ex + "]", BaseClasses.CPLogBaseClass.LogLevel.Fatal);
        }
        //
        //================================================================================================
        /// <summary>
        /// An error that also appends a log file in the /alarms folder in program files. The diagnostic monitor should signal a fail.
        /// </summary>
        /// <param name="core"></param>
        /// <param name="cause"></param>
        public static void logAlarm(CoreController core, string cause) {
            log(core, "logAlarm: " + cause, BaseClasses.CPLogBaseClass.LogLevel.Fatal);
            //
            // -- set off alarm
            DateTime now = DateTime.Now;
            core.programDataFiles.appendFile("Alarms/" + now.Year.ToString("0000", CultureInfo.InvariantCulture) + now.Month.ToString("00", CultureInfo.InvariantCulture) + now.Day.ToString("00", CultureInfo.InvariantCulture) + "-alarms.log", "\r\n" + processLogMessage(core, now.ToString(CultureInfo.InvariantCulture) + ", [" + cause + "]", true));
        }
        //
        //=====================================================================================================
        /// <summary>
        /// add activity about a user to the site's activity log for content managers to review
        /// </summary>
        /// <param name="core"></param>
        /// <param name="subject"></param>
        /// <param name="activityDetails"></param>
        /// <param name="activityUserId"></param>
        /// <param name="typeId">see ActivityLogTypeEnum, 1=online visit, 2=online purchase, 3=email-to, 4=email-from, 5=call-to, 6=call-from, 7=text-to, 8=text-from, 9=meeting-video, 10=meeting-in-person, </param>
        /// <param name="dateScheduled"></param>
        /// <param name="duration"></param>
        /// <param name="scheduledStaffId"></param>
        public static int addActivityScheduled(CoreController core, string subject, string activityDetails, int activityUserId, int typeId, DateTime dateScheduled, int duration, int scheduledStaffId) {
            try {
                ActivityLogModel log = DbBaseModel.addDefault<ActivityLogModel>(core.cpParent);
                log.name = (subject == null) ? "" : subject;
                log.dateCanceled = null;
                log.dateCompleted = null;
                log.dateScheduled = dateScheduled;
                log.duration = duration;
                log.link = "";
                log.memberId = activityUserId;
                log.message = string.IsNullOrEmpty(activityDetails) ? "" : activityDetails;
                log.typeId = (typeId < 1) ? (int)ActivityLogModel.ActivityLogTypeEnum.OnlineVisit : typeId;
                log.scheduledStaffId = scheduledStaffId;
                log.visitId = (core?.session?.visit == null) ? 0 : core.session.visit.id;
                log.visitorId = (core?.session?.visitor == null) ? 0 : core.session.visitor.id;
                log.save(core.cpParent);
                return log.id;
            } catch (Exception ex) {
                log(core, "exception [" + ex + "]", BaseClasses.CPLogBaseClass.LogLevel.Error);
                // do not abort page over logging issues
                // throw;
                return 0;
            }
        }
        //
        //=====================================================================================================
        /// <summary>
        /// add online-visit activity about a user to the site's activity log for content managers to review.
        /// This overload is for an activity scheduled for the future
        /// </summary>
        /// <param name="core"></param>
        /// <param name="subject"></param>
        /// <param name="activityDetails"></param>
        /// <param name="activityUserId"></param>
        /// <param name="dateScheduled"></param>
        /// <param name="duration"></param>
        /// <param name="scheduledStaffId"></param>
        [Obsolete("Use overload with activity-type",false)]
        public static int addActivityScheduled(CoreController core, string subject, string activityDetails, int activityUserId, DateTime dateScheduled, int duration, int scheduledStaffId) {
            return addActivityScheduled(core, subject, activityDetails, activityUserId,(int)ActivityLogModel.ActivityLogTypeEnum.OnlineVisit, dateScheduled, duration, scheduledStaffId);
        }
        //
        //=====================================================================================================
        /// <summary>
        /// add activity about a completed user record edit to the site.
        /// This overload is for an activity that occured (not one scheduled for the future)
        /// </summary>
        /// <param name="core"></param>
        /// <param name="subject"></param>
        /// <param name="activityDetails"></param>
        /// <param name="activityUserId"></param>
        /// <param name="subjectOrganizationID"></param>
        public static int addActivityCompletedEdit(CoreController core, string subject, string activityDetails, int activityUserId) {
            return addActivityCompleted(core, subject, activityDetails, activityUserId, (int)ActivityLogModel.ActivityLogTypeEnum.ContactUpdate);
        }
        //
        //=====================================================================================================
        /// <summary>
        /// add activity about a completed user visit to the site.
        /// This overload is for an activity that occured (not one scheduled for the future)
        /// </summary>
        /// <param name="core"></param>
        /// <param name="subject"></param>
        /// <param name="activityDetails"></param>
        /// <param name="activityUserId"></param>
        /// <param name="subjectOrganizationID"></param>
        public static int addActivityCompletedVisit(CoreController core, string subject, string activityDetails, int activityUserId) {
            return addActivityCompleted(core, subject, activityDetails, activityUserId, (int)ActivityLogModel.ActivityLogTypeEnum.OnlineVisit);
        }
        //
        //=====================================================================================================
        /// <summary>
        /// add activity about a user to the site's activity log. 
        /// This overload is for an activity that occured (not one scheduled for the future)
        /// </summary>
        /// <param name="core"></param>
        /// <param name="subject">Brief text to expand on the activity type, like "filled out online form". This will be the name field.</param>
        /// <param name="activityDetails">Any details that need to be saved, like if this was an online form filled out, this is what the user submitted.</param>
        /// <param name="activityUserId">The user that this activity was about. Like the user who filled out the online form</param>
        /// <param name="typeId">see ActivityLogTypeEnum, 1=visit online, 2=email-to, 3=email-from, 4=call-to, 5=call-from, 6=text-to, 7=text-from, 8=meeting-video, 9=meeting-in-person, 10=edit, 11=online-purchase</param>
        public static int addActivityCompleted(CoreController core, string subject, string activityDetails, int activityUserId, int typeId) {
            // return addActivity(core, subject, activityDetails, activityUserId, 1, DateTime.MinValue, 0,0);
            try {
                //
                ActivityLogModel log = DbBaseModel.addDefault<ActivityLogModel>(core.cpParent);
                log.name = (subject == null) ? "" : subject;
                log.dateCanceled = null;
                log.dateCompleted = DateTime.Now;
                log.dateScheduled = DateTime.Now;
                log.duration = 0;
                log.link = "";
                log.memberId = activityUserId;
                log.message = string.IsNullOrEmpty(activityDetails) ? "" : activityDetails;
                log.typeId = (typeId < 1) ? 1 : typeId;
                log.scheduledStaffId = 0;
                log.visitId = (core?.session?.visit == null) ? 0 : core.session.visit.id;
                log.visitorId = (core?.session?.visitor == null) ? 0 : core.session.visitor.id;
                log.save(core.cpParent);
                return log.id;
            } catch (Exception ex) {
                log(core, "exception [" + ex + "]", BaseClasses.CPLogBaseClass.LogLevel.Error);
                return 0;
            }

        }
        //
        //=====================================================================================================
        /// <summary>
        /// add activity about a user to the site's activity log for content managers to review
        /// </summary>
        /// <param name="core"></param>
        /// <param name="subject"></param>
        /// <param name="activityDetails"></param>
        public static int addActivityCompletedVisit(CoreController core, string subject, string activityDetails) {
            core.session.verifyUser();
            return addActivityCompletedVisit(core, subject, activityDetails, core.session.user.id);
        }
        //
        //================================================================================================
        /// <summary>
        /// Add a site Warning: for content managers to make content changes with the site
        ///   Report Warning
        ///       A warning is logged in the site warnings log
        ///           name - a generic description of the warning
        ///               "bad link found on page"
        ///           description - a specific description
        ///               "link to http://www.this.com/pagename was found on http://www.this.com/About-us"
        /// </summary>
        /// <param name="core"></param>
        /// <param name="name">A generic description of the warning that describes the problem, but if the issue occurs again the name will match, like Page Not Found on /Home</param>
        /// <param name="description">Any detail the use will need to debug the problem.</param>
        //
        public static void addAdminWarning(CoreController core, string name, string description) {
            string SQL = "select top 1 ID from ccSiteWarnings"
                + " where (name=" + DbController.encodeSQLText(name) + ")"
                + " and(description=" + DbController.encodeSQLText(description) + ")"
                + "";
            using (DataTable dt = core.db.executeQuery(SQL)) {
                if (dt.Rows.Count > 0) {
                    //
                    // -- increment count for matching warning
                    int warningId = GenericController.encodeInteger(dt.Rows[0]["id"]);
                    SQL = "update ccsitewarnings "
                        + " set count=count+1,"
                        + " dateLastReported=" + DbController.encodeSQLDate(core.dateTimeNowMockable) + " "
                        + " where id=" + warningId;
                    core.db.executeNonQuery(SQL);
                    return;
                }
            }
            //
            // -- insert new record
            using var csData = new CsModel(core);
            if (csData.insert("Site Warnings")) {
                csData.set("name", name);
                csData.set("description", description);
                csData.set("count", 1);
                csData.set("DateLastReported", core.dateTimeNowMockable);
            }
            //
        }
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    }
}