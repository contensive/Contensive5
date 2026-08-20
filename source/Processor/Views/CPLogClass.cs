
using System;
using Contensive.BaseClasses;
using NLog;
using static Contensive.BaseClasses.CPLogBaseClass;

namespace Contensive.Processor {
    /// <summary>
    /// Logging interface. All methods guard on NLog's IsEnabled check to avoid
    /// building logCommonMessage and concatenating strings when the level is disabled.
    /// </summary>
    public class CPLogClass : CPLogBaseClass, IDisposable {
        //
        // static logger
        private static readonly NLog.Logger logger = LogManager.GetCurrentClassLogger();
        //
        // ====================================================================================================
        /// <summary>
        /// dependencies
        /// </summary>
        private readonly CPClass cp;
        //
        // ====================================================================================================
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="cp"></param>
        public CPLogClass(CPClass cp)
            => this.cp = cp;
        //
        // ====================================================================================================
        // IsEnabled properties — delegate to NLog so addon callers can guard expensive message construction
        //
        /// <summary>
        /// True if trace-level logging is enabled
        /// </summary>
        public override bool IsTraceEnabled => logger.IsTraceEnabled;
        //
        /// <summary>
        /// True if debug-level logging is enabled
        /// </summary>
        public override bool IsDebugEnabled => logger.IsDebugEnabled;
        //
        /// <summary>
        /// True if info-level logging is enabled
        /// </summary>
        public override bool IsInfoEnabled => logger.IsInfoEnabled;
        //
        /// <summary>
        /// True if warn-level logging is enabled
        /// </summary>
        public override bool IsWarnEnabled => logger.IsWarnEnabled;
        //
        /// <summary>
        /// True if error-level logging is enabled
        /// </summary>
        public override bool IsErrorEnabled => logger.IsErrorEnabled;
        //
        /// <summary>
        /// True if fatal-level logging is enabled
        /// </summary>
        public override bool IsFatalEnabled => logger.IsFatalEnabled;
        //
        // ====================================================================================================
        /// <summary>
        /// add a log message at the trace level (trace, debug, info, warn, error, fatal)
        /// </summary>
        /// <param name="logMessage"></param>
        public override void Trace(string logMessage) {
            if (!logger.IsTraceEnabled) { return; }
            logger.Trace($"{cp.core.logCpommonMessage_forStructuredLogging},{{Message}}", logMessage);
        }
        //
        // ====================================================================================================
        /// <summary>
        /// add a log message at the debug level (trace, debug, info, warn, error, fatal)
        /// </summary>
        /// <param name="logMessage"></param>
        public override void Debug(string logMessage) {
            if (!logger.IsDebugEnabled) { return; }
            logger.Debug($"{cp.core.logCpommonMessage_forStructuredLogging},{{Message}}", logMessage);
        }
        //
        // ====================================================================================================
        /// <summary>
        /// add a log message at the info level (trace, debug, info, warn, error, fatal)
        /// </summary>
        /// <param name="logMessage"></param>
        public override void Info(string logMessage) {
            if (!logger.IsInfoEnabled) { return; }
            logger.Info($"{cp.core.logCpommonMessage_forStructuredLogging},{{Message}}", logMessage);
        }
        //
        // ====================================================================================================
        /// <summary>
        /// add a log message at the warn level (trace, debug, info, warn, error, fatal)
        /// </summary>
        /// <param name="logMessage"></param>
        public override void Warn(string logMessage) {
            if (!logger.IsWarnEnabled) { return; }
            logger.Warn($"{cp.core.logCpommonMessage_forStructuredLogging},{{Message}}", logMessage);
        }
        //
        // ====================================================================================================
        /// <summary>
        /// add a log message at the warn level (trace, debug, info, warn, error, fatal)
        /// </summary>
        /// <param name="logMessage"></param>
        public override void Warn(Exception ex, string logMessage) {
            if (!logger.IsWarnEnabled) { return; }
            logger.Warn(ex, $"{cp.core.logCpommonMessage_forStructuredLogging},{{Message}}", logMessage);
        }
        //
        // ====================================================================================================
        /// <summary>
        /// add a log message at the warn level (trace, debug, info, warn, error, fatal)
        /// </summary>
        /// <param name="logMessage"></param>
        public override void Warn(Exception ex) {
            if (!logger.IsWarnEnabled) { return; }
            logger.Warn(ex, cp.core.logCpommonMessage_forStructuredLogging);
        }
        //
        // ====================================================================================================
        /// <summary>
        /// add a log message at the error level (trace, debug, info, warn, error, fatal)
        /// </summary>
        /// <param name="logMessage"></param>
        public override void Error(string logMessage) {
            if (!logger.IsErrorEnabled) { return; }
            logger.Error($"{cp.core.logCpommonMessage_forStructuredLogging},{{Message}}", logMessage);
        }
        //
        // ====================================================================================================
        /// <summary>
        /// add a log message at the error level (trace, debug, info, warn, error, fatal)
        /// </summary>
        /// <param name="logMessage"></param>
        public override void Error(Exception ex, string logMessage) {
            if (!logger.IsErrorEnabled) { return; }
            logger.Error(ex, $"{cp.core.logCpommonMessage_forStructuredLogging},{{Message}}", logMessage);
        }
        //
        // ====================================================================================================
        /// <summary>
        /// add a log message at the error level (trace, debug, info, warn, error, fatal)
        /// </summary>
        /// <param name="logMessage"></param>
        public override void Error(Exception ex) {
            if (!logger.IsErrorEnabled) { return; }
            logger.Error(ex, cp.core.logCpommonMessage_forStructuredLogging);
        }
        //
        // ====================================================================================================
        /// <summary>
        /// add a log message at the fatal level (trace, debug, info, warn, error, fatal)
        /// </summary>
        /// <param name="logMessage"></param>
        public override void Fatal(string logMessage) {
            if (!logger.IsFatalEnabled) { return; }
            logger.Fatal($"{cp.core.logCpommonMessage_forStructuredLogging},{{Message}}", logMessage);
        }
        //
        // ====================================================================================================
        /// <summary>
        /// add a log message at the fatal level (trace, debug, info, warn, error, fatal)
        /// </summary>
        /// <param name="logMessage"></param>
        public override void Fatal(Exception ex, string logMessage) {
            if (!logger.IsFatalEnabled) { return; }
            logger.Fatal(ex, $"{cp.core.logCpommonMessage_forStructuredLogging},{{Message}}", logMessage);
        }
        //
        // ====================================================================================================
        /// <summary>
        /// add a log message at the fatal level (trace, debug, info, warn, error, fatal)
        /// </summary>
        /// <param name="logMessage"></param>
        public override void Fatal(Exception ex) {
            if (!logger.IsFatalEnabled) { return; }
            logger.Fatal(ex, cp.core.logCpommonMessage_forStructuredLogging);
        }
        //
        // ====================================================================================================
        /// <summary>
        /// add a log message at the debug level
        /// </summary>
        /// <param name="logMessage"></param>
        public override void Add(string logMessage) {
            if (!logger.IsDebugEnabled) { return; }
            logger.Debug($"{cp.core.logCpommonMessage_forStructuredLogging},{{Message}}", logMessage);
        }
        //
        // ====================================================================================================
        /// <summary>
        /// add a log message
        /// </summary>
        /// <param name="level"></param>
        /// <param name="logMessage"></param>
        public override void Add(LogLevel level, string logMessage) {
            switch (level) {
                case LogLevel.Trace: {
                        Trace(logMessage);
                        break;
                    }
                case LogLevel.Debug: {
                        Debug(logMessage);
                        break;
                    }
                case LogLevel.Warn: {
                        Warn(logMessage);
                        break;
                    }
                case LogLevel.Error: {
                        Error(logMessage);
                        break;
                    }
                case LogLevel.Fatal: {
                        Fatal(logMessage);
                        break;
                    }
                default: {
                        Info(logMessage);
                        break;
                    }
            }
        }
        //
        #region  IDisposable Support
        // Do not change or add Overridable to these methods.
        // Put cleanup code in Dispose(ByVal disposing As Boolean).
        //
        // ====================================================================================================
        /// <summary>
        /// must call to dispose
        /// </summary>
        protected virtual void Dispose(bool disposing_log) {
            if (!this.disposed_log) {
                if (disposing_log) {
                    //
                    // call .dispose for managed objects
                    //
                }
                //
                // Add code here to release the unmanaged resource.
                //
            }
            this.disposed_log = true;
        }
        //
        protected bool disposed_log;
        public override void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        //
        ~CPLogClass() {
            Dispose(false);
        }
        #endregion
    }
}
