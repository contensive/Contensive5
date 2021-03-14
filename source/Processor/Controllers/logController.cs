
using System;
using System.Data;
using NLog;
using NLog.Config;
using System.Collections.Generic;
using NLog.AWS.Logger;
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
        /// <returns></returns>
        public static string getMessageLine(CoreController core, string message) {
            string result = "app [" + ((core.appConfig != null) ? core.appConfig.name : "no-app") + "]";
            result += ", thread [" + System.Threading.Thread.CurrentThread.ManagedThreadId.ToString("000") + "]";
            result += ", url [" + ((core.webServer == null) ? "non-web" : string.IsNullOrEmpty(core.webServer.requestPathPage) ? "empty" : core.webServer.requestPathPage) + "]";
            result += ", " + message.Replace(Environment.NewLine, " ").Replace("\n", " ").Replace("\r", " ");
            return result;
        }
        //
        //=============================================================================
        /// <summary>
        /// log Executed queries, user authenticated, session expired
        /// </summary>
        /// <param name="core"></param>
        /// <param name="message"></param>
        /// <remarks></remarks>
        public static void logDebug(CoreController core, string message) => log(core, message, BaseClasses.CPLogBaseClass.LogLevel.Debug);
        //
        //=============================================================================
        /// <summary>
        /// log application crashes / exceptions
        /// </summary>
        /// <param name="core"></param>
        /// <param name="message"></param>
        /// <remarks></remarks>
        public static void logError(CoreController core, string message) => log(core, message, BaseClasses.CPLogBaseClass.LogLevel.Error);
        //
        //=============================================================================
        /// <summary>
        /// log highest level, most important messages
        /// </summary>
        /// <param name="core"></param>
        /// <param name="message"></param>
        /// <remarks></remarks>
        public static void logFatal(CoreController core, string message) => log(core, message, BaseClasses.CPLogBaseClass.LogLevel.Fatal);
        //
        //=============================================================================
        /// <summary>
        /// Normal behavior like mail sent, user updated profile etc
        /// </summary>
        /// <param name="core"></param>
        /// <param name="message"></param>
        /// <remarks></remarks>
        public static void logInfo(CoreController core, string message) => log(core, message, BaseClasses.CPLogBaseClass.LogLevel.Info);
        //
        //=============================================================================
        /// <summary>
        /// log begin method, end method, etc
        /// </summary>
        /// <param name="core"></param>
        /// <param name="message"></param>
        /// <remarks></remarks>
        public static void logTrace(CoreController core, string message) => log(core, message, BaseClasses.CPLogBaseClass.LogLevel.Trace);
        //
        //=============================================================================
        /// <summary>
        /// log incorrect behavior but the application can continue
        /// </summary>
        /// <param name="core"></param>
        /// <param name="message"></param>
        /// <remarks></remarks>
        public static void logWarn(CoreController core, string message) => log(core, message, BaseClasses.CPLogBaseClass.LogLevel.Warn);
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
        public static void logLocalOnly(string messageLine, BaseClasses.CPLogBaseClass.LogLevel level) {
            try {
                var nlogLogger = LogManager.GetCurrentClassLogger();
                nlogLogger.Log(typeof(LogController), new LogEventInfo(getNLogLogLevel(level), nlogLogger.Name, messageLine));
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
                string messageLine = getMessageLine(core, message);
                Logger loggerInstance = core.nlogLogger;
                loggerInstance.Log(typeof(LogController), new LogEventInfo(getNLogLogLevel(level), loggerInstance.Name, messageLine));
                //
                // -- add to doc exception list to display at top of webpage
                if (level < BaseClasses.CPLogBaseClass.LogLevel.Warn) { return; }
                if (core.doc.errorList == null) { core.doc.errorList = new List<string>(); }
                if (core.doc.errorList.Count < 10) {
                    core.doc.errorList.Add(messageLine);
                    return;
                }
                if (core.doc.errorList.Count == 10) { core.doc.errorList.Add("Exception limit exceeded"); }
            } catch (Exception) {
                // -- throw away errors in error-handling
            }
        }
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
                LogController.logError(core, ex);
            }
        }
        //
        //=====================================================================================================
        /// <summary>
        /// add activity about a user to the site's activity log for content managers to review
        /// </summary>
        /// <param name="core"></param>
        /// <param name="Message"></param>
        /// <param name="SubjectMemberID"></param>
        /// <param name="SubjectOrganizationID"></param>
        public static void addSiteActivity(CoreController core, string Message, int SubjectMemberID, int SubjectOrganizationID) {
            if ((core.session != null) && (core.session.user != null) && (core.session.visitor != null) && (core.session.visit != null) && (core.webServer != null)) {
                addSiteActivity(core, Message, core.session.user.id, SubjectMemberID, SubjectOrganizationID, core.webServer.requestUrl, core.session.visitor.id, core.session.visit.id);
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
        /// <param name="Name">A generic description of the warning that describes the problem, but if the issue occurs again the name will match, like Page Not Found on /Home</param>
        /// <param name="ignore">To be deprecated - same as name</param>
        /// <param name="location">Where the issue occurred, like on a page, or in a background process.</param>
        /// <param name="PageID"></param>
        /// <param name="Description">Any detail the use will need to debug the problem.</param>
        /// <param name="issueCategory">A general description of the issue that can be grouped in a report, like Page Not Found</param>
        /// <param name="ignore2">to be deprecated, same a name.</param>
        //
        public static void addSiteWarning(CoreController core, string Name, string ignore, string location, int PageID, string Description, string issueCategory, string ignore2) {
            int warningId = 0;
            string SQL = "select top 1 ID from ccSiteWarnings"
                + " where (name=" + DbController.encodeSQLText(Name) + ")"
                + " and(generalKey=" + DbController.encodeSQLText(issueCategory) + ")"
                + "";
            DataTable dt = core.db.executeQuery(SQL);
            if (dt.Rows.Count > 0) {
                warningId = GenericController.encodeInteger(dt.Rows[0]["id"]);
            }
            //
            if (warningId != 0) {
                //
                // increment count for matching warning
                //
                SQL = "update ccsitewarnings set count=count+1,DateLastReported=" + DbController.encodeSQLDate(core.dateTimeNowMockable) + " where id=" + warningId;
                core.db.executeNonQuery(SQL);
            } else {
                //
                // insert new record
                //
                using (var csData = new CsModel(core)) {
                    if (csData.insert("Site Warnings")) {
                        csData.set("name", Name);
                        csData.set("description", Description);
                        csData.set("generalKey", issueCategory);
                        csData.set("count", 1);
                        csData.set("DateLastReported", core.dateTimeNowMockable);
                        csData.set("location", location);
                        csData.set("pageId", PageID);
                    }
                }
            }
            //
        }
        //
        //================================================================================================
        /// <summary>
        /// Appends a log file in the /alarms folder in program files. The diagnostic monitor should signal a fail.
        /// </summary>
        /// <param name="core"></param>
        /// <param name="cause"></param>
        public static void logAlarm(CoreController core, string cause) {
            LogController.logFatal(core, "logAlarm: " + cause);
            DateTime now = DateTime.Now;
            core.programDataFiles.appendFile("Alarms/" + now.Year.ToString("0000") + now.Month.ToString("00") + now.Day.ToString("00") + "-alarms.log", getMessageLine(core, now.ToString() + ", [" + cause + "]"));

        }
    }
}