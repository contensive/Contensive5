
using System;
using System.Data;
using NLog;
using NLog.Config;
using System.Collections.Generic;
using NLog.AWS.Logger;
using System.Globalization;
using Contensive.BaseClasses;
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
            if (core.serverConfig == null) return;
            if (string.IsNullOrWhiteSpace(core.serverConfig.awsCloudWatchLogGroup)) return;
            var awsTarget = new AWSTarget {
                Layout = "${longdate}|${level:uppercase=true}|${callsite}|${message}",
                LogGroup = core.serverConfig.awsCloudWatchLogGroup,
                Region = core.awsCredentials.awsRegion.DisplayName,
                Credentials = new Amazon.Runtime.BasicAWSCredentials(core.awsCredentials.awsAccessKeyId, core.awsCredentials.awsSecretAccessKey),
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
            switch (level) {
                case BaseClasses.CPLogBaseClass.LogLevel.Trace:
                    return NLog.LogLevel.Trace;
                case BaseClasses.CPLogBaseClass.LogLevel.Debug:
                    return NLog.LogLevel.Debug;
                case BaseClasses.CPLogBaseClass.LogLevel.Warn:
                    return NLog.LogLevel.Warn;
                case BaseClasses.CPLogBaseClass.LogLevel.Error:
                    return NLog.LogLevel.Error;
                case BaseClasses.CPLogBaseClass.LogLevel.Fatal:
                    return NLog.LogLevel.Fatal;
                default:
                    return NLog.LogLevel.Info;
            }
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
            string messageLine = processLogMessage(core, message, level >= BaseClasses.CPLogBaseClass.LogLevel.Warn);
            logger.Log(typeof(LogController), new LogEventInfo(getNLogLogLevel(level), logger.Name, messageLine));
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
            core.programDataFiles.appendFile("Alarms/" + now.Year.ToString("0000", CultureInfo.InvariantCulture) + now.Month.ToString("00", CultureInfo.InvariantCulture) + now.Day.ToString("00", CultureInfo.InvariantCulture) + "-alarms.log", @"\r\n" + processLogMessage(core, now.ToString(CultureInfo.InvariantCulture) + ", [" + cause + "]", true));
        }
        //
        //=====================================================================================================
        /// <summary>
        /// add activity about a user to the site's activity log for content managers to review
        /// </summary>
        /// <param name="core"></param>
        /// <param name="Message"></param>
        /// <param name="ByMemberID"></param>
        /// <param name="SubjectMemberID"></param>
        /// <param name="SubjectOrganizationID"></param>
        /// <param name="Link"></param>
        /// <param name="VisitorId"></param>
        /// <param name="VisitId"></param>
        public static void addSiteActivity(CoreController core, string Message, int ByMemberID, int SubjectMemberID, int SubjectOrganizationID, string Link = "", int VisitorId = 0, int VisitId = 0) {
            try {
                //
                if (Message.Length > 255) Message = Message.Substring(0, 255);
                if (Link.Length > 255) Message = Link.Substring(0, 255);
                using (var csData = new CsModel(core)) {
                    if (csData.insert("Activity Log")) {
                        csData.set("MemberID", SubjectMemberID);
                        csData.set("OrganizationID", SubjectOrganizationID);
                        csData.set("Message", Message);
                        csData.set("Link", Link);
                        csData.set("VisitorID", VisitorId);
                        csData.set("VisitID", VisitId);
                    }
                }
                //
                return;
                //
            } catch (Exception ex) {
                log(core, "exception [" + ex + "]", BaseClasses.CPLogBaseClass.LogLevel.Error);
                throw;
            }
        }
        //
        //=====================================================================================================
        /// <summary>
        /// add activity about a user to the site's activity log for content managers to review
        /// </summary>
        /// <param name="core"></param>
        /// <param name="message"></param>
        /// <param name="subjectMemberID"></param>
        /// <param name="subjectOrganizationID"></param>
        public static void addSiteActivity(CoreController core, string message, int subjectMemberID, int subjectOrganizationID) {
            if ((core.session != null) && (core.session.user != null) && (core.session.visitor != null) && (core.session.visit != null) && (core.webServer != null)) {
                addSiteActivity(core, message, core.session.user.id, subjectMemberID, subjectOrganizationID, core.webServer.requestUrl, core.session.visitor.id, core.session.visit.id);
            }
        }
        //
        //================================================================================================
        /// <summary>
        /// Add a site Warning: for content managers to make content changes with the site
        ///   Report Warning
        ///       A warning is logged in the site warnings log
        ///           name - a generic description of the warning
        ///               "bad link found on page"
        ///           issueCategory - a generic string that describes the warning. the warning report
        ///               will display one line for each generalKey (name matches guid)
        ///               like "bad link"
        ///           location - the URL, service or process that caused the problem
        ///               "http://goodpageThankHasBadLink.com"
        ///           pageid - the record id of the bad page.
        ///               "http://goodpageThankHasBadLink.com"
        ///           description - a specific description
        ///               "link to http://www.this.com/pagename was found on http://www.this.com/About-us"
        ///           count - the number of times the name and issueCategory matched. "This error was reported 100 times"
        /// </summary>
        /// <param name="core"></param>
        /// <param name="name">A generic description of the warning that describes the problem, but if the issue occurs again the name will match, like Page Not Found on /Home</param>
        /// <param name="ignore">To be deprecated - same as name</param>
        /// <param name="location">Where the issue occurred, like on a page, or in a background process.</param>
        /// <param name="pageID"></param>
        /// <param name="description">Any detail the use will need to debug the problem.</param>
        /// <param name="issueCategory">A general description of the issue that can be grouped in a report, like Page Not Found</param>
        /// <param name="ignore2">to be deprecated, same a name.</param>
        //
        public static void addSiteWarning(CoreController core, string name, string ignore, string location, int pageID, string description, string issueCategory, string ignore2) {
            string SQL = "select top 1 ID from ccSiteWarnings"
                + " where (name=" + DbController.encodeSQLText(name) + ")"
                + " and(generalKey=" + DbController.encodeSQLText(issueCategory) + ")"
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
                    //
                    // -- ASYNC test
                    LogController.log(core, "addSiteWarning, async test 1 of 3", CPLogBaseClass.LogLevel.Debug);
                    //
                    core.db.executeNonQueryAsync(SQL);
                    return;
                }
            }
            //
            // -- insert new record
            using var csData = new CsModel(core);
            if (csData.insert("Site Warnings")) {
                csData.set("name", name);
                csData.set("description", description);
                csData.set("generalKey", issueCategory);
                csData.set("count", 1);
                csData.set("DateLastReported", core.dateTimeNowMockable);
                csData.set("location", location);
                csData.set("pageId", pageID);
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