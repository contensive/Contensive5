
using System.Collections.Generic;
//
namespace Contensive.Processor.Models.Domain {
    //
    //====================================================================================================
    /// <summary>
    /// The result of parsing a log file.
    /// </summary>
    public sealed class LogFileParseResultModel {
        public List<LogFileEntryModel> entries { get; set; } = new List<LogFileEntryModel>();
        /// <summary>
        /// length snapshot at open
        /// </summary>
        public long fileLength { get; set; }
        /// <summary>
        /// 0, or the byte offset where reading began (size guard)
        /// </summary>
        public long startOffset { get; set; }
        /// <summary>
        /// size guard applied
        /// </summary>
        public bool isTruncated { get; set; }
        /// <summary>
        /// partial lines dropped before the first entry when truncated
        /// </summary>
        public int skippedLeadingLines { get; set; }
        public int malformedCount { get; set; }
    }
}
