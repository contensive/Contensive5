
using System;
using Amazon.Runtime;
using Amazon.Runtime.Internal.Util;
using Contensive.BaseClasses;
using Contensive.Processor.Controllers;
using NLog;
using static Contensive.BaseClasses.CPLogBaseClass;

namespace Contensive.Processor {
    /// <summary>
    /// Logging interface
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
            Logger.Trace( cp.core.logCommonMessage + "," + logMessage);
        }
        //
        // ====================================================================================================
        /// <summary>
        /// add a log message at the debug level (trace, debug, info, warn, error, fatal)
        /// </summary>
        /// <param name="logMessage"></param>
        public override void Debug(string logMessage) {
            Logger.Debug(cp.core.logCommonMessage + "," + logMessage);
        }
        //
        // ====================================================================================================
        /// <summary>
        /// add a log message at the info level (trace, debug, info, warn, error, fatal)
        /// </summary>
        /// <param name="logMessage"></param>
        public override void Info(string logMessage) {
            Logger.Info(cp.core.logCommonMessage + "," + logMessage);
        }
        //
        // ====================================================================================================
        /// <summary>
        /// add a log message at the warn level (trace, debug, info, warn, error, fatal)
        /// </summary>
        /// <param name="logMessage"></param>
        public override void Warn(string logMessage) {
            Logger.Warn(cp.core.logCommonMessage + "," + logMessage);
        }
        //
        // ====================================================================================================
        /// <summary>
        /// add a log message at the warn level (trace, debug, info, warn, error, fatal)
        /// </summary>
        /// <param name="logMessage"></param>
        public override void Warn(Exception ex, string logMessage) {
            Logger.Warn(ex, cp.core.logCommonMessage + "," + logMessage);
        }
        //
        // ====================================================================================================
        /// <summary>
        /// add a log message at the warn level (trace, debug, info, warn, error, fatal)
        /// </summary>
        /// <param name="logMessage"></param>
        public override void Warn(Exception ex) {
            Logger.Warn(ex, cp.core.logCommonMessage);
        }
        //
        // ====================================================================================================
        /// <summary>
        /// add a log message at the error level (trace, debug, info, warn, error, fatal)
        /// </summary>
        /// <param name="logMessage"></param>
        public override void Error(string logMessage) {
            Logger.Error( cp.core.logCommonMessage + "," + logMessage);
        }
        //
        // ====================================================================================================
        /// <summary>
        /// add a log message at the error level (trace, debug, info, warn, error, fatal)
        /// </summary>
        /// <param name="logMessage"></param>
        public override void Error(Exception ex, string logMessage) {
            Logger.Error(ex, cp.core.logCommonMessage + "," + logMessage);
        }
        //
        // ====================================================================================================
        /// <summary>
        /// add a log message at the error level (trace, debug, info, warn, error, fatal)
        /// </summary>
        /// <param name="logMessage"></param>
        public override void Error(Exception ex) {
            Logger.Warn(ex, cp.core.logCommonMessage );
        }
        //
        // ====================================================================================================
        /// <summary>
        /// add a log message at the fatal level (trace, debug, info, warn, error, fatal)
        /// </summary>
        /// <param name="logMessage"></param>
        public override void Fatal(string logMessage) {
            Logger.Warn(cp.core.logCommonMessage + "," + logMessage);
        }
        //
        // ====================================================================================================
        /// <summary>
        /// add a log message at the fatal level (trace, debug, info, warn, error, fatal)
        /// </summary>
        /// <param name="logMessage"></param>
        public override void Fatal(Exception ex, string logMessage) {
            Logger.Fatal(ex, cp.core.logCommonMessage + "," + logMessage);
        }
        //
        // ====================================================================================================
        /// <summary>
        /// add a log message at the fatal level (trace, debug, info, warn, error, fatal)
        /// </summary>
        /// <param name="logMessage"></param>
        public override void Fatal(Exception ex) {
            Logger.Fatal(ex, cp.core.logCommonMessage);
        }
        //
        // ====================================================================================================
        /// <summary>
        /// add a log message at the debug level
        /// </summary>
        /// <param name="logMessage"></param>
        public override void Add(string logMessage) {
            Logger.Debug(cp.core.logCommonMessage + "," + logMessage);
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
                        logger.Trace($"{cp.core.logCommonMessage},{logMessage}");
                        break;
                    }
                case LogLevel.Debug: {
                        logger.Debug($"{cp.core.logCommonMessage},{logMessage}");
                        break;
                    }
                case LogLevel.Warn: {
                        logger.Warn($"{cp.core.logCommonMessage},{logMessage}");
                        break;
                    }
                case LogLevel.Error: {
                        logger.Error($"{cp.core.logCommonMessage},{logMessage}");
                        break;
                    }
                case LogLevel.Fatal: {
                        logger.Fatal($"{cp.core.logCommonMessage},{logMessage}");
                        break;
                    }
                default: {
                        logger.Info($"{cp.core.logCommonMessage},{logMessage}");
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