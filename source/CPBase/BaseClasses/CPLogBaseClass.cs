
using System;
using System.Data;

namespace Contensive.BaseClasses {
    /// <summary>
    /// CP.Log, methods to access and control the logging system
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
        /// Log a message
        /// </summary>
        /// <param name="logMessage"></param>
        public abstract void Trace(string logMessage);
        //
        //====================================================================================================
        /// <summary>
        /// Log a message
        /// </summary>
        /// <param name="logMessage"></param>
        public abstract void Debug(string logMessage);
        //
        //====================================================================================================
        /// <summary>
        /// Log a message
        /// </summary>
        /// <param name="logMessage"></param>
        public abstract void Info(string logMessage);
        //
        //====================================================================================================
        /// <summary>
        /// Log a message
        /// </summary>
        /// <param name="logMessage"></param>
        public abstract void Warn(string logMessage);
        //
        //====================================================================================================
        /// <summary>
        /// Log a message
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="logMessage"></param>
        public abstract void Warn(Exception ex, string logMessage);
        //
        //====================================================================================================
        /// <summary>
        /// Log a message
        /// </summary>
        /// <param name="ex"></param>
        public abstract void Warn(Exception ex);
        //
        //====================================================================================================
        /// <summary>
        /// Log a message
        /// </summary>
        /// <param name="logMessage"></param>
        public abstract void Error(string logMessage);
        //
        //====================================================================================================
        /// <summary>
        /// Log a message
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="logMessage"></param>
        public abstract void Error(Exception ex, string logMessage);
        //
        //====================================================================================================
        /// <summary>
        /// Log a message
        /// </summary>
        /// <param name="ex"></param>
        public abstract void Error(Exception ex);
        //
        //====================================================================================================
        /// <summary>
        /// Log a message
        /// </summary>
        /// <param name="logMessage"></param>
        public abstract void Fatal(string logMessage);
        //
        //====================================================================================================
        /// <summary>
        /// Log a message
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="logMessage"></param>
        public abstract void Fatal(Exception ex, string logMessage);
        //
        //====================================================================================================
        /// <summary>
        /// Log a message
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

