
using System;
//
namespace Contensive.Processor.Models.Domain {
    //
    //====================================================================================================
    /// <summary>
    /// One parsed entry of an NLog file written with the layout
    /// ${longdate}|${level:uppercase=true}|${message}|${exception}|${stacktrace}
    /// An entry can span many physical lines (exception stack traces). 
    /// All values come from a log file and are untrusted. Html-encode before display.
    /// </summary>
    public sealed class LogFileEntryModel {
        /// <summary>
        /// 0-based order of the entry in the loaded text (stable sort tiebreak)
        /// </summary>
        public int sequence { get; set; }
        /// <summary>
        /// 1-based physical line of the start line, within the loaded text
        /// </summary>
        public int lineNumber { get; set; }
        /// <summary>
        /// raw "yyyy-MM-dd HH:mm:ss.ffff"; "" for a raw (unparsed) entry
        /// </summary>
        public string timeText { get; set; } = "";
        /// <summary>
        /// timeText parsed, local server time. null for a raw entry
        /// </summary>
        public DateTime? time { get; set; }
        /// <summary>
        /// TRACE|DEBUG|INFO|WARN|ERROR|FATAL, "" for a raw entry
        /// </summary>
        public string level { get; set; } = "";
        /// <summary>
        /// TRACE 0 ... FATAL 5, raw -1
        /// </summary>
        public int levelRank { get; set; }
        public string app { get; set; } = "";
        /// <summary>
        /// raw "2001:User 1"
        /// </summary>
        public string user { get; set; } = "";
        /// <summary>
        /// 0 if absent or unparseable
        /// </summary>
        public int userId { get; set; }
        public string userName { get; set; } = "";
        public string view { get; set; } = "";
        public string visit { get; set; } = "";
        public string thread { get; set; } = "";
        public string url { get; set; } = "";
        /// <summary>
        /// full message after the prefix, leading whitespace trimmed, may contain line feeds
        /// </summary>
        public string message { get; set; } = "";
        /// <summary>
        /// ${exception}, NLog 5 default is ToString(): type, message, "   at" lines
        /// </summary>
        public string exception { get; set; } = "";
        /// <summary>
        /// ${stacktrace} "A => B => C", trimmed
        /// </summary>
        public string loggerStack { get; set; } = "";
        public int continuationLineCount { get; set; }
        /// <summary>
        /// fewer than 2 pipes after the level, or a raw entry
        /// </summary>
        public bool isMalformed { get; set; }
    }
}
