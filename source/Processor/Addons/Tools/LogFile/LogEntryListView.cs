
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Contensive.BaseClasses.LayoutBuilder;
using Contensive.Processor.Controllers;
using Contensive.Processor.Models.Domain;
using NLog;
using static Contensive.BaseClasses.LayoutBuilder.LayoutBuilderBaseClass;
//
namespace Contensive.Processor.Addons.Tools {
    //
    //====================================================================================================
    /// <summary>
    /// Log file tool screen 2: the parsed, searchable, sortable, paginated entries of one log file. Rendered with a LayoutBuilderList.
    /// Every value from the log is untrusted and is html-encoded before it is written to the page.
    /// </summary>
    public static class LogEntryListView {
        //
        // static logger
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        //
        // -- Csv download is OFF. 
        // -- LayoutBuilderList writes the download file to the public CDN (cdnFiles) under a sequential downloads record, so anyone who can guess the url could fetch log content 
        // -- (user names, visit ids, urls, server paths, exception text). Set this true only when the builder can write downloads privately.
        // -- The download values and the csvSafe guard are already supplied on every row so the switch is the only change needed.
        private const bool allowLogDownload = false;
        //
        // -- the largest page honoured when a download is not allowed but the request asks for one (the builder then reports 9999999)
        private const int maxPageSizeNoDownload = 500;
        private const int maxSearchTermLength = 200;
        //
        // -- the most characters of a message, exception or logger stack written to the page
        private const int maxBlockChars = 20000;
        //
        private static readonly string[] levelOrder = { "FATAL", "ERROR", "WARN", "INFO", "DEBUG", "TRACE" };
        //
        private const string styles = @"<style type=""text/css"">
.logEntry summary{cursor:pointer;word-break:break-word}
.logEntryText{white-space:pre-wrap;word-break:break-word;max-height:24em;overflow:auto;font-size:.85em;margin:.25em 0}
.logEntryMeta,.logEntryLabel{font-size:.8em;color:#666;margin-top:.25em}
</style>";
        //
        //====================================================================================================
        /// <summary>
        /// Return the html of the log view. The order of the builder member calls matters: the builder caches search, sort and paging on first read.
        /// </summary>
        public static string getHtml(CPClass cp) {
            // -- no try/catch here. A failure is logged once, by the Execute method of the addon that called this view
            string rel = cp.Doc.GetText("logFile");
            //
            // -- validate the requested file before anything else. Nothing here reads builder state.
            string logRoot = LogFilePathController.getLogRoot(cp.core);
            bool isPathValid = LogFilePathController.tryResolve(logRoot, rel, true, out string absPathFilename, out string relChecked, out string rejectReason);
            if (!isPathValid) { logger.Warn($"{cp.core.logCommonMessage}, log file tool file rejected, reason [{rejectReason}], requested [{LogFilePathController.cleanForLog(rel)}]"); }
            string relFile = isPathValid ? relChecked : "";
            //
            var lb = cp.AdminUI.CreateLayoutBuilderList(Constants.addonGuidLogFileToolEntries);
            //
            // -- fresh open of a different file (a link, not a form or ajax post) starts with no search term and no level filter
            if (!cp.Doc.IsProperty("callbackAddonGuid") && cp.Visit.GetText("LogFileTool_lastFile") != relFile) {
                cp.Visit.SetProperty($"AdminUIListSearch_{Constants.addonGuidLogFileToolEntries}", "");
                // -- the property name the builder uses in getFilterBoolean is "{viewName}-{filterHtmlName}"
                foreach (string level in levelOrder) { cp.Visit.SetProperty($"LogFileTool-logLevel{level}", false); }
                cp.Visit.SetProperty("LogFileTool_lastFile", relFile);
            }
            //
            // -- changing a filter returns to page 1. "Apply Filter" is the literal submit value of the builder filter form
            if (cp.Doc.GetText("button") == "Apply Filter" || !string.IsNullOrEmpty(cp.Doc.GetText("removeFilter"))) { lb.paginationReset(); }
            //
            // -- static setup
            lb.title = $"Log File: {cp.Utils.EncodeHTML(relFile)}";
            lb.description = $"Newest entries first. Click a message to expand it. Search matches message, URL, user and exception. Reading {cp.Utils.EncodeHTML(logRoot)}.";
            lb.includeBodyPadding = true;
            lb.includeBodyColor = true;
            lb.paginationPageSizeDefault = 50;
            lb.paginationRecordAlias = "entries";
            lb.allowDownloadButton = allowLogDownload;
            lb.addLinkButton("Back to files", cp.Utils.EncodeHTML(LogFileToolLinks.folderLink(cp, LogFileToolLinks.parentOf(relFile))));
            if (isPathValid) { lb.addFormHidden("logFile", cp.Utils.EncodeHTML(relFile)); }
            //
            // -- read builder state, in this order
            var selectedLevels = new List<string>();
            foreach (string level in levelOrder) {
                if (lb.getFilterBoolean($"logLevel{level}", "LogFileTool")) { selectedLevels.Add(level); }
            }
            string term = (lb.sqlSearchTerm ?? "").Trim();
            if (term.Length > maxSearchTermLength) { term = term.Substring(0, maxSearchTermLength); }
            string sortField = lb.sortField;
            string sortDirection = lb.sortDirection;
            int pageNumber = lb.paginationPageNumber;
            int pageSize = lb.paginationPageSize;
            if (!allowLogDownload && pageSize > maxPageSizeNoDownload) { pageSize = lb.paginationPageSizeDefault; }
            //
            // -- columns, always added, also for empty or failed results
            addColumns(lb);
            //
            if (!isPathValid) {
                lb.failMessage = "That file cannot be opened. Only .log and .txt files inside the Logs folder can be viewed.";
                lb.recordCount = 0;
                lb.htmlAfterBody = styles;
                return lb.getHtml();
            }
            if (!File.Exists(absPathFilename)) {
                lb.failMessage = $"The log file {cp.Utils.EncodeHTML(relFile)} was not found. It may have been archived or removed.";
                lb.recordCount = 0;
                lb.htmlAfterBody = styles;
                return lb.getHtml();
            }
            //
            // -- parse
            LogFileParseResultModel result;
            try {
                result = LogFileParserController.parseFile(absPathFilename, LogFileParserController.maxParseBytesDefault);
            } catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException) {
                logger.Error(ex, $"{cp.core.logCommonMessage}");
                lb.failMessage = $"The log file could not be read: {cp.Utils.EncodeHTML(ex.Message)}";
                lb.recordCount = 0;
                lb.htmlAfterBody = styles;
                return lb.getHtml();
            }
            //
            // -- filters
            lb.addFilterGroup("Level");
            var levelCounts = LogEntryQueryController.countLevels(result.entries, levelOrder);
            foreach (string level in levelOrder) {
                int levelCount = levelCounts[level];
                lb.addFilterCheckbox($"{level} ({levelCount})", $"logLevel{level}", "1", selectedLevels.Contains(level));
            }
            //
            // -- filter, search and sort in memory
            List<LogFileEntryModel> rows = LogEntryQueryController.apply(result.entries, selectedLevels, term, sortField, sortDirection);
            lb.recordCount = rows.Count;
            //
            // -- rows. A page, or all rows (up to the builder limit) if a download was requested and is allowed, since the builder then reports a huge page size
            int skip = LogEntryQueryController.getSkip(pageNumber, pageSize, rows.Count);
            int take = Math.Min(pageSize, lb.reportRowLimit);
            foreach (LogFileEntryModel entry in rows.Skip(skip).Take(take)) {
                addRow(cp, lb, entry);
            }
            //
            // -- messages
            var infoMessages = new List<string>();
            if (result.entries.Count == 0) {
                infoMessages.Add(result.fileLength == 0 ? "This log file is empty." : "No log entries were found in the expected format.");
            }
            if (result.malformedCount > 0) {
                infoMessages.Add($"{result.malformedCount} entries did not match the log format and are shown as raw text.");
            }
            lb.infoMessage = string.Join(" ", infoMessages);
            if (result.isTruncated) {
                string firstTime = result.entries.Count > 0 ? result.entries[0].timeText : "";
                lb.warningMessage = $"This file is {(result.fileLength / 1048576.0).ToString("0.0", CultureInfo.InvariantCulture)} MB. Only the newest {LogFileParserController.maxParseBytesDefault / 1048576} MB were read, starting at {cp.Utils.EncodeHTML(firstTime)}. Older entries are not shown.";
            }
            lb.htmlAfterBody = styles;
            return lb.getHtml();
        }
        //
        //====================================================================================================
        // -- 14 columns. Five are visible. The rest exist only so a download has the complete entry.
        private static void addColumns(LayoutBuilderListBaseClass lb) {
            lb.addColumn(new ReportListColumnBaseClass { name = "time", caption = "Time", captionClass = AfwStyles.afwTextAlignLeft, cellClass = AfwStyles.afwTextAlignLeft, sortable = true, visible = true, downloadable = true, columnWidthPercent = 12 });
            lb.addColumn(new ReportListColumnBaseClass { name = "level", caption = "Level", captionClass = AfwStyles.afwTextAlignLeft, cellClass = AfwStyles.afwTextAlignLeft, sortable = true, visible = true, downloadable = true, columnWidthPercent = 6 });
            lb.addColumn(new ReportListColumnBaseClass { name = "url", caption = "URL", captionClass = AfwStyles.afwTextAlignLeft, cellClass = AfwStyles.afwTextAlignLeft, sortable = true, visible = true, downloadable = true, columnWidthPercent = 16 });
            lb.addColumn(new ReportListColumnBaseClass { name = "user", caption = "User", captionClass = AfwStyles.afwTextAlignLeft, cellClass = AfwStyles.afwTextAlignLeft, sortable = false, visible = true, downloadable = false, columnWidthPercent = 14 });
            lb.addColumn(new ReportListColumnBaseClass { name = "message", caption = "Message", captionClass = AfwStyles.afwTextAlignLeft, cellClass = AfwStyles.afwTextAlignLeft, sortable = false, visible = true, downloadable = true, columnWidthPercent = 52 });
            foreach (var hidden in new[] { ("app", "App"), ("userId", "User Id"), ("userName", "User Name"), ("visit", "Visit"), ("view", "View"), ("thread", "Thread"), ("exception", "Exception"), ("loggerStack", "Logger Stack"), ("lineNumber", "Line") }) {
                lb.addColumn(new ReportListColumnBaseClass { name = hidden.Item1, caption = hidden.Item2, captionClass = "", cellClass = "", sortable = false, visible = false, downloadable = true, columnWidthPercent = 0 });
            }
        }
        //
        //====================================================================================================
        // -- one row, exactly 14 cells in column order
        private static void addRow(CPClass cp, LayoutBuilderListBaseClass lb, LogFileEntryModel entry) {
            lb.addRow();
            // 0 time
            lb.setCell(entry.timeText == "" ? "&mdash;" : cp.Utils.EncodeHTML(entry.timeText), LogEntryQueryController.csvSafe(entry.timeText));
            // 1 level
            lb.setCell(entry.level == "" ? "&mdash;" : $"<span class=\"badge {getBadgeClass(entry.level)}\">{cp.Utils.EncodeHTML(entry.level)}</span>", LogEntryQueryController.csvSafe(entry.level));
            // 2 url
            lb.setCell($"<span title=\"{cp.Utils.EncodeHTML(entry.url)}\">{cp.Utils.EncodeHTML(LogEntryQueryController.truncate(entry.url, 60))}</span>", LogEntryQueryController.csvSafe(entry.url));
            // 3 user
            string userText = cp.Utils.EncodeHTML(entry.userName);
            if (entry.userId > 0) { userText = $"{userText} ({entry.userId})".Trim(); }
            lb.setCell(userText == "" ? "&mdash;" : userText, "");
            // 4 message
            lb.setCell(getMessageHtml(cp, entry), LogEntryQueryController.csvSafe(entry.message));
            // 5-13 download only
            lb.setCell("", LogEntryQueryController.csvSafe(entry.app));
            lb.setCell("", entry.userId.ToString(CultureInfo.InvariantCulture));
            lb.setCell("", LogEntryQueryController.csvSafe(entry.userName));
            lb.setCell("", LogEntryQueryController.csvSafe(entry.visit));
            lb.setCell("", LogEntryQueryController.csvSafe(entry.view));
            lb.setCell("", LogEntryQueryController.csvSafe(entry.thread));
            lb.setCell("", LogEntryQueryController.csvSafe(entry.exception));
            lb.setCell("", LogEntryQueryController.csvSafe(entry.loggerStack));
            lb.setCell("", entry.lineNumber.ToString(CultureInfo.InvariantCulture));
        }
        //
        //====================================================================================================
        /// <summary>
        /// The message cell. A native details element expands to the full message, exception, logger stack and visit, view and thread.
        /// Truncate before encoding, never truncate encoded html.
        /// </summary>
        private static string getMessageHtml(CPClass cp, LogFileEntryModel entry) {
            string summary;
            if (entry.message == "") {
                summary = entry.exception == "" ? "(no message)" : "(no message, see exception)";
            } else {
                summary = $"{cp.Utils.EncodeHTML(LogEntryQueryController.truncate(entry.message, 160))}{(entry.message.Length > 160 ? "&hellip;" : "")}";
            }
            var sb = new StringBuilder();
            sb.Append($"<details class=\"logEntry\"><summary>{summary}</summary>");
            sb.Append($"<div class=\"logEntryMeta\">Visit {cp.Utils.EncodeHTML(entry.visit)} &middot; View {cp.Utils.EncodeHTML(entry.view)} &middot; Thread {cp.Utils.EncodeHTML(entry.thread)} &middot; App {cp.Utils.EncodeHTML(entry.app)} &middot; Line {entry.lineNumber}</div>");
            if (entry.message != "") { sb.Append($"<pre class=\"logEntryText\">{LogEntryQueryController.capForDisplay(entry.message, maxBlockChars, cp.Utils.EncodeHTML)}</pre>"); }
            if (entry.exception != "") { sb.Append($"<div class=\"logEntryLabel\">Exception</div><pre class=\"logEntryText\">{LogEntryQueryController.capForDisplay(entry.exception, maxBlockChars, cp.Utils.EncodeHTML)}</pre>"); }
            if (entry.loggerStack != "") { sb.Append($"<div class=\"logEntryLabel\">Logger stack</div><pre class=\"logEntryText\">{LogEntryQueryController.capForDisplay(entry.loggerStack, maxBlockChars, cp.Utils.EncodeHTML)}</pre>"); }
            sb.Append("</details>");
            return sb.ToString();
        }
        //
        // -- fixed map from the whitelisted level to a bootstrap badge class. The level is never used as a class name.
        private static string getBadgeClass(string level) {
            switch (level) {
                case "FATAL":
                case "ERROR":
                    return "bg-danger";
                case "WARN":
                    return "bg-warning text-dark";
                case "INFO":
                    return "bg-info text-dark";
                default:
                    return "bg-secondary";
            }
        }
    }
}
