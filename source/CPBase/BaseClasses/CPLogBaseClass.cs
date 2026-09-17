
using System;
using System.Data;

namespace Contensive.BaseClasses {
    /// <summary>
    /// CP.Log — the primary logging interface for application code.
    /// Use cp.Log methods (Trace, Debug, Info, Warn, Error, Fatal) for all application-level logging.
    /// These write to the application log file at the specified NLog severity level.
    ///
    /// For operational alerts that require admin attention, see also:
    /// - cp.Site.ErrorReport() — logs at ERROR level and sends error notifications. Use for unexpected exceptions.
    /// - cp.Site.LogAlarm() — writes to the server Alarms folder and triggers the server alarm monitor. Use only for fatal/critical issues requiring immediate admin intervention.
    /// - cp.Site.SetSiteWarning() — displays a warning indicator in the admin UI. Use for content or configuration issues an admin can fix.
    /// </summary>
    /// <remarks></remarks>
    public abstract class CPLogBaseClass : IDisposable {
        /// <summary>
        /// Log levels
        /// </summary>
        public enum LogLevel {
            /// <summary>
            /// Begin method X, end method X etc
            /// </summary>
            Trace = 0,
            /// <summary>
            /// Executed queries, user authenticated, session expired
            /// </summary>
            Debug = 1,
            /// <summary>
            /// Normal behavior like mail sent, user updated profile etc.
            /// </summary>
            Info = 2,
            /// <summary>
            /// Incorrect behavior but the application can continue
            /// </summary>
            Warn = 3,
            /// <summary>
            /// For example application crashes / exceptions.
            /// </summary>
            Error = 4,
            /// <summary>
            /// Highest level: important stuff down
            /// </summary>
            Fatal = 5
        }
        //
        //====================================================================================================
        /// <summary>
        /// True if trace-level logging is enabled. Use to guard expensive message construction.
        /// </summary>
        public virtual bool IsTraceEnabled => true;
        //
        //====================================================================================================
        /// <summary>
        /// True if debug-level logging is enabled. Use to guard expensive message construction.
        /// </summary>
        public virtual bool IsDebugEnabled => true;
        //
        //====================================================================================================
        /// <summary>
        /// True if info-level logging is enabled. Use to guard expensive message construction.
        /// </summary>
        public virtual bool IsInfoEnabled => true;
        //
        //====================================================================================================
        /// <summary>
        /// True if warn-level logging is enabled. Use to guard expensive message construction.
        /// </summary>
        public virtual bool IsWarnEnabled => true;
        //
        //====================================================================================================
        /// <summary>
        /// True if error-level logging is enabled. Use to guard expensive message construction.
        /// </summary>
        public virtual bool IsErrorEnabled => true;
        //
        //====================================================================================================
        /// <summary>
        /// True if fatal-level logging is enabled. Use to guard expensive message construction.
        /// </summary>
        public virtual bool IsFatalEnabled => true;
        //
        //====================================================================================================
        /// <summary>
        /// Log a message at the info level. (same a Info(logMessage))
        /// </summary>
        /// <param name="logMessage"></param>
        public abstract void Add(string logMessage);
        //
        //====================================================================================================
        /// <summary>
        /// Log a message at the info level.
        /// </summary>
        /// <param name="logMessage"></param>
        /// <param name="level"></param>
        public abstract void Add(LogLevel level, string logMessage);
        //
        //====================================================================================================
        /// <summary>
        /// Log a trace-level message. Use for fine-grained diagnostics like method entry/exit.
        /// </summary>
        /// <param name="logMessage"></param>
        public abstract void Trace(string logMessage);
        //
        //====================================================================================================
        /// <summary>
        /// Log a debug-level message. Use for diagnostic details like executed queries, authentication events, or session state.
        /// </summary>
        /// <param name="logMessage"></param>
        public abstract void Debug(string logMessage);
        //
        //====================================================================================================
        /// <summary>
        /// Log an info-level message. Use for normal operational events like mail sent, profile updated, or process completed.
        /// </summary>
        /// <param name="logMessage"></param>
        public abstract void Info(string logMessage);
        //
        //====================================================================================================
        /// <summary>
        /// Log a warn-level message. Use for incorrect behavior the application can recover from, such as rate limits, missing optional data, or retryable failures.
        /// </summary>
        /// <param name="logMessage"></param>
        public abstract void Warn(string logMessage);
        //
        //====================================================================================================
        /// <summary>
        /// Log a warn-level message with an exception. Use for incorrect behavior the application can recover from.
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="logMessage"></param>
        public abstract void Warn(Exception ex, string logMessage);
        //
        //====================================================================================================
        /// <summary>
        /// Log a warn-level exception. Use for incorrect behavior the application can recover from.
        /// </summary>
        /// <param name="ex"></param>
        public abstract void Warn(Exception ex);
        //
        //====================================================================================================
        /// <summary>
        /// Log an error-level message. Use for application errors and exceptions that indicate a bug or unexpected failure.
        /// For errors that also need admin notification, consider cp.Site.ErrorReport() instead.
        /// </summary>
        /// <param name="logMessage"></param>
        public abstract void Error(string logMessage);
        //
        //====================================================================================================
        /// <summary>
        /// Log an error-level message with an exception. Use for application errors and exceptions that indicate a bug or unexpected failure.
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="logMessage"></param>
        public abstract void Error(Exception ex, string logMessage);
        //
        //====================================================================================================
        /// <summary>
        /// Log an error-level exception. Use for application errors and exceptions that indicate a bug or unexpected failure.
        /// </summary>
        /// <param name="ex"></param>
        public abstract void Error(Exception ex);
        //
        //====================================================================================================
        /// <summary>
        /// Log a fatal-level message. Use for critical failures that prevent the application from continuing.
        /// To also trigger the server alarm monitor, use cp.Site.LogAlarm() instead.
        /// </summary>
        /// <param name="logMessage"></param>
        public abstract void Fatal(string logMessage);
        //
        //====================================================================================================
        /// <summary>
        /// Log a fatal-level message with an exception. Use for critical failures that prevent the application from continuing.
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="logMessage"></param>
        public abstract void Fatal(Exception ex, string logMessage);
        //
        //====================================================================================================
        /// <summary>
        /// Log a fatal-level exception. Use for critical failures that prevent the application from continuing.
        /// </summary>
        /// <param name="ex"></param>
        public abstract void Fatal(Exception ex);
        //
        //====================================================================================================
        /// <summary>
        /// Support disposable for non-default datasources
        /// </summary>
        public abstract void Dispose();
    }

}

