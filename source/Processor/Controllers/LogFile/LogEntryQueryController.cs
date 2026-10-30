
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
    }
}
