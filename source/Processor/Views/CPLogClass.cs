
using System;
using Contensive.BaseClasses;
using Contensive.Processor.Controllers;
using static Contensive.BaseClasses.CPLogBaseClass;

namespace Contensive.Processor {
    /// <summary>
    /// Logging interface
    /// </summary>
    public class CPLogClass : CPLogBaseClass, IDisposable {
        //
        // ====================================================================================================
        /// <summary>
        /// dependencies
        /// </summary>
        private readonly CPClass cp;
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
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
        /// <summary>
        /// add a log message at the trace level (trace, debug, info, warn, error, fatal)
        /// </summary>
        /// <param name="logMessage"></param>
        public override void Trace(string logMessage) {
            Logger.Log(typeof(CPLogClass), new NLog.LogEventInfo(NLog.LogLevel.Trace, Logger.Name, LogControllerX.processLogMessage(cp.core, logMessage, false)));
        }
        //
        // ====================================================================================================
        /// <summary>
        /// add a log message at the debug level (trace, debug, info, warn, error, fatal)
        /// </summary>
        /// <param name="logMessage"></param>
        public override void Debug(string logMessage) {
            Logger.Log(typeof(CPLogClass), new NLog.LogEventInfo(NLog.LogLevel.Debug, Logger.Name, LogControllerX.processLogMessage(cp.core, logMessage, false)));
        }
        //
        // ====================================================================================================
        /// <summary>
        /// add a log message at the info level (trace, debug, info, warn, error, fatal)
        /// </summary>
        /// <param name="logMessage"></param>
        public override void Info(string logMessage) {
            Logger.Log(typeof(CPLogClass), new NLog.LogEventInfo(NLog.LogLevel.Info, Logger.Name, LogControllerX.processLogMessage(cp.core, logMessage, false)));
        }
        //
        // ====================================================================================================
        /// <summary>
        /// add a log message at the warn level (trace, debug, info, warn, error, fatal)
        /// </summary>
        /// <param name="logMessage"></param>
        public override void Warn(string logMessage) {
            Logger.Log(typeof(CPLogClass), new NLog.LogEventInfo(NLog.LogLevel.Warn, Logger.Name, LogControllerX.processLogMessage(cp.core, logMessage, true)));
        }
        //
        // ====================================================================================================
        /// <summary>
        /// add a log message at the warn level (trace, debug, info, warn, error, fatal)
        /// </summary>
        /// <param name="logMessage"></param>
        public override void Warn(Exception ex, string logMessage) {
            Logger.Log(typeof(CPLogClass), new NLog.LogEventInfo(NLog.LogLevel.Warn, Logger.Name, LogControllerX.processLogMessage(cp.core, logMessage + ", exception [" + ex + "]", true)));
        }
        //
        // ====================================================================================================
        /// <summary>
        /// add a log message at the warn level (trace, debug, info, warn, error, fatal)
        /// </summary>
        /// <param name="logMessage"></param>
        public override void Warn(Exception ex) {
            Logger.Log(typeof(CPLogClass), new NLog.LogEventInfo(NLog.LogLevel.Warn, Logger.Name, LogControllerX.processLogMessage(cp.core, "exception [" + ex + "]", true)));
        }
        //
        // ====================================================================================================
        /// <summary>
        /// add a log message at the error level (trace, debug, info, warn, error, fatal)
        /// </summary>
        /// <param name="logMessage"></param>
        public override void Error(string logMessage) {
            Logger.Log(typeof(CPLogClass), new NLog.LogEventInfo(NLog.LogLevel.Error, Logger.Name, LogControllerX.processLogMessage(cp.core, logMessage, true)));
        }
        //
        // ====================================================================================================
        /// <summary>
        /// add a log message at the error level (trace, debug, info, warn, error, fatal)
        /// </summary>
        /// <param name="logMessage"></param>
        public override void Error(Exception ex, string logMessage) {
            Logger.Log(typeof(CPLogClass), new NLog.LogEventInfo(NLog.LogLevel.Error, Logger.Name, LogControllerX.processLogMessage(cp.core, logMessage + ", exception [" + ex + "]", true)));
        }
        //
        // ====================================================================================================
        /// <summary>
        /// add a log message at the error level (trace, debug, info, warn, error, fatal)
        /// </summary>
        /// <param name="logMessage"></param>
        public override void Error(Exception ex) {
            Logger.Log(typeof(CPLogClass), new NLog.LogEventInfo(NLog.LogLevel.Error, Logger.Name, LogControllerX.processLogMessage(cp.core, "exception [" + ex + "]", true)));
        }
        //
        // ====================================================================================================
        /// <summary>
        /// add a log message at the fatal level (trace, debug, info, warn, error, fatal)
        /// </summary>
        /// <param name="logMessage"></param>
        public override void Fatal(string logMessage) {
            Logger.Log(typeof(CPLogClass), new NLog.LogEventInfo(NLog.LogLevel.Fatal, Logger.Name, LogControllerX.processLogMessage(cp.core, logMessage, true)));
        }
        //
        // ====================================================================================================
        /// <summary>
        /// add a log message at the fatal level (trace, debug, info, warn, error, fatal)
        /// </summary>
        /// <param name="logMessage"></param>
        public override void Fatal(Exception ex, string logMessage) {
            Logger.Log(typeof(CPLogClass), new NLog.LogEventInfo(NLog.LogLevel.Fatal, Logger.Name, LogControllerX.processLogMessage(cp.core, logMessage + ", exception[" + ex + "]", true)));
        }
        //
        // ====================================================================================================
        /// <summary>
        /// add a log message at the fatal level (trace, debug, info, warn, error, fatal)
        /// </summary>
        /// <param name="logMessage"></param>
        public override void Fatal(Exception ex) {
            Logger.Log(typeof(CPLogClass), new NLog.LogEventInfo(NLog.LogLevel.Fatal, Logger.Name, LogControllerX.processLogMessage(cp.core, "exception[" + ex + "]", true)));
        }
        //
        // ====================================================================================================
        /// <summary>
        /// add a log message at the debug level
        /// </summary>
        /// <param name="logMessage"></param>
        public override void Add(string logMessage) {
            Logger.Log(typeof(CPLogClass), new NLog.LogEventInfo(NLog.LogLevel.Debug, Logger.Name, LogControllerX.processLogMessage(cp.core, logMessage, false)));
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
                        Logger.Log(typeof(CPLogClass), new NLog.LogEventInfo(NLog.LogLevel.Trace, Logger.Name, LogControllerX.processLogMessage(cp.core, logMessage, false)));
                        break;
                    }
                case LogLevel.Debug: {
                        Logger.Log(typeof(CPLogClass), new NLog.LogEventInfo(NLog.LogLevel.Debug, Logger.Name, LogControllerX.processLogMessage(cp.core, logMessage, false)));
                        break;
                    }
                case LogLevel.Warn: {
                        Logger.Log(typeof(CPLogClass), new NLog.LogEventInfo(NLog.LogLevel.Warn, Logger.Name, LogControllerX.processLogMessage(cp.core, logMessage, true)));
                        break;
                    }
                case LogLevel.Error: {
                        Logger.Log(typeof(CPLogClass), new NLog.LogEventInfo(NLog.LogLevel.Error, Logger.Name, LogControllerX.processLogMessage(cp.core, logMessage, true)));
                        break;
                    }
                case LogLevel.Fatal: {
                        Logger.Log(typeof(CPLogClass), new NLog.LogEventInfo(NLog.LogLevel.Fatal, Logger.Name, LogControllerX.processLogMessage(cp.core, logMessage, true)));
                        break;
                    }
                default: {
                        Logger.Log(typeof(CPLogClass), new NLog.LogEventInfo(NLog.LogLevel.Info, Logger.Name, LogControllerX.processLogMessage(cp.core, logMessage, false)));
                        break;
                    }
            }
            LogControllerX.log(cp.core, logMessage, level);
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