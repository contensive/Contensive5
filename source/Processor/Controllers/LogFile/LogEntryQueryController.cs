
using System;
using System.Collections.Generic;
using System.Linq;
using Contensive.Processor.Models.Domain;
//
namespace Contensive.Processor.Controllers {
    //
    //====================================================================================================
    /// <summary>
    /// Filter, search and sort a list of parsed log entries in memory. No dependency on CPClass or CoreController.
    /// </summary>
    public static class LogEntryQueryController {
        //
        //====================================================================================================
        /// <summary>
        /// Apply level filter, search and sort. The entries list is not changed.
        /// </summary>
        /// <param name="entries"></param>
        /// <param name="selectedLevels">Levels to keep (TRACE...FATAL). Empty or null keeps all entries, including raw entries.</param>
        /// <param name="searchTerm">Case-insensitive substring of message, url, user or exception. Empty keeps all.</param>
        /// <param name="sortField">time, level or url. Anything else uses the default, time descending</param>
        /// <param name="sortDirection">asc or desc</param>
        /// <returns></returns>
        public static List<LogFileEntryModel> apply(IList<LogFileEntryModel> entries, ICollection<string> selectedLevels, string searchTerm, string sortField, string sortDirection) {
            IEnumerable<LogFileEntryModel> query = entries;
            //
            // -- levels
            if (selectedLevels != null && selectedLevels.Count > 0) {
                var levelSet = new HashSet<string>(selectedLevels, StringComparer.Ordinal);
                query = query.Where(x => levelSet.Contains(x.level));
            }
            //
            // -- search. string.Contains(string, StringComparison) does not exist on net48
            string term = searchTerm ?? "";
            if (term.Length > 0) {
                query = query.Where(x =>
                    x.message.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    x.url.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    x.user.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    x.exception.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0);
            }
            //
            // -- sort. OrderBy/ThenBy are stable
            bool ascending = string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);
            switch ((sortField ?? "").ToLowerInvariant()) {
                case "time":
                    return (ascending
                        ? query.OrderBy(x => x.time ?? DateTime.MinValue).ThenBy(x => x.sequence)
                        : query.OrderByDescending(x => x.time ?? DateTime.MinValue).ThenByDescending(x => x.sequence)).ToList();
                case "level":
                    return (ascending
                        ? query.OrderBy(x => x.levelRank).ThenBy(x => x.time ?? DateTime.MinValue).ThenBy(x => x.sequence)
                        : query.OrderByDescending(x => x.levelRank).ThenByDescending(x => x.time ?? DateTime.MinValue).ThenByDescending(x => x.sequence)).ToList();
                case "url":
                    return (ascending
                        ? query.OrderBy(x => x.url, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.time ?? DateTime.MinValue).ThenBy(x => x.sequence)
                        : query.OrderByDescending(x => x.url, StringComparer.OrdinalIgnoreCase).ThenByDescending(x => x.time ?? DateTime.MinValue).ThenByDescending(x => x.sequence)).ToList();
                default:
                    //
                    // -- default, newest first. Entries with the same time are in reverse file order
                    return query.OrderByDescending(x => x.time ?? DateTime.MinValue).ThenByDescending(x => x.sequence).ToList();
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Guard a value placed in a csv download against spreadsheet formula injection. A value that starts with = + - @ tab or CR is prefixed with an apostrophe.
        /// </summary>
        public static string csvSafe(string value) {
            if (string.IsNullOrEmpty(value)) { return ""; }
            switch (value[0]) {
                case '=':
                case '+':
                case '-':
                case '@':
                case '\t':
                case '\r':
                    return $"'{value}";
                default:
                    return value;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Shorten to at most length characters without splitting a surrogate pair.
        /// </summary>
        public static string truncate(string value, int length) {
            if (string.IsNullOrEmpty(value) || value.Length <= length) { return value ?? ""; }
            int n = length;
            if (n > 0 && char.IsHighSurrogate(value[n - 1])) { n--; }
            return value.Substring(0, n);
        }
        //
        //====================================================================================================
        /// <summary>
        /// Cap a long value for display. The value is truncated first, then encoded, then an encoded note "(truncated, N more characters)" is appended. 
        /// Encoded html is never truncated.
        /// </summary>
        /// <param name="value">the raw text</param>
        /// <param name="maxChars">the most raw characters to show</param>
        /// <param name="encode">the html encoder, applied to the text and to the note</param>
        public static string capForDisplay(string value, int maxChars, Func<string, string> encode) {
            if (string.IsNullOrEmpty(value)) { return ""; }
            if (value.Length <= maxChars) { return encode(value); }
            string shown = truncate(value, maxChars);
            return $"{encode(shown)}{encode($"\n(truncated, {value.Length - shown.Length} more characters)")}";
        }
        //
        //====================================================================================================
        /// <summary>
        /// Count the entries of each level in one pass. Entries with an unknown level (raw entries) are not counted. Every level in levels is present in the result.
        /// </summary>
        public static Dictionary<string, int> countLevels(IEnumerable<LogFileEntryModel> entries, IEnumerable<string> levels) {
            var counts = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (string level in levels) { counts[level] = 0; }
            foreach (LogFileEntryModel entry in entries) {
                if (counts.TryGetValue(entry.level ?? "", out int count)) { counts[entry.level] = count + 1; }
            }
            return counts;
        }
        //
        //====================================================================================================
        /// <summary>
        /// The number of rows to skip for a 1-based page. Computed as long so a huge page number cannot overflow. 
        /// A page beyond the last page is clamped to the last page, so the pager and the rows agree. The result is within [0, rowCount].
        /// </summary>
        public static int getSkip(long pageNumber, long pageSize, int rowCount) {
            if (rowCount <= 0) { return 0; }
            if (pageSize < 1) { pageSize = 1; }
            long lastPageSkip = ((rowCount - 1L) / pageSize) * pageSize;
            long skip = (pageNumber - 1L) * pageSize;
            if (pageNumber < 1 || skip < 0) { skip = 0; }
            if (skip > lastPageSkip) { skip = lastPageSkip; }
            return (int)skip;
        }
    }
}
