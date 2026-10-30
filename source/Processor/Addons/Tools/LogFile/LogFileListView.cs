
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Contensive.BaseClasses.LayoutBuilder;
using Contensive.Processor.Controllers;
using NLog;
using static Contensive.BaseClasses.LayoutBuilder.LayoutBuilderBaseClass;
//
namespace Contensive.Processor.Addons.Tools {
    //
    //====================================================================================================
    /// <summary>
    /// Log file tool screen 1: the folders and log files under the log root. Rendered with a LayoutBuilderList.
    /// </summary>
    public static class LogFileListView {
        //
        // static logger
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        //
        // -- downloads are off for the log file tool, see LogEntryListView.allowLogDownload. The file list never offers a download.
        private const bool allowFileListDownload = false;
        //
        private sealed class ListItem {
            public bool isFolder;
            public string name = "";
            public long size;
            public DateTime modified;
        }
        //
        //====================================================================================================
        /// <summary>
        /// Return the html of the file list. Order of the builder member calls matters: the builder caches search, sort and paging on first read.
        /// </summary>
        public static string getHtml(CPClass cp) {
            try {
                string relFolder = cp.Doc.GetText("logFolder");
                var lb = cp.AdminUI.CreateLayoutBuilderList(Constants.addonGuidLogFileTool);
                //
                // -- fresh open of a different folder (a link, not a form or ajax post) starts with no search term
                if (!cp.Doc.IsProperty("callbackAddonGuid") && cp.Visit.GetText("LogFileTool_lastFolder") != relFolder) {
                    cp.Visit.SetProperty($"AdminUIListSearch_{Constants.addonGuidLogFileTool}", "");
                    cp.Visit.SetProperty("LogFileTool_lastFolder", relFolder);
                }
                //
                // -- validate the folder
                string logRoot = LogFilePathController.getLogRoot(cp.core);
                lb.title = "Log Files";
                lb.includeBodyPadding = true;
                lb.includeBodyColor = true;
                lb.paginationPageSizeDefault = 50;
                lb.paginationRecordAlias = "items";
                lb.allowDownloadButton = allowFileListDownload;
                //
                string failMessage = "";
                if (!Directory.Exists(logRoot)) {
                    //
                    // -- no log folder at all
                    addColumns(lb);
                    lb.description = $"Reading {cp.Utils.EncodeHTML(logRoot)}";
                    lb.infoMessage = $"No Logs folder was found at {cp.Utils.EncodeHTML(logRoot)}.";
                    lb.recordCount = 0;
                    return lb.getHtml();
                }
                if (!LogFilePathController.tryResolve(logRoot, relFolder, false, out string absFolder, out string relFolderChecked, out string rejectReason)) {
                    logger.Warn($"{cp.core.logCommonMessage}, log file tool folder rejected, reason [{rejectReason}], requested [{cleanForLog(relFolder)}]");
                    failMessage = "That folder is not inside the Logs folder. Showing the Logs folder instead.";
                    LogFilePathController.tryResolve(logRoot, "", false, out absFolder, out relFolderChecked, out _);
                }
                relFolder = relFolderChecked;
                lb.description = $"Folder: {cp.Utils.EncodeHTML(relFolder == "" ? "Logs" : $"Logs\\{relFolder}")}. Reading {cp.Utils.EncodeHTML(logRoot)}. Only .log and .txt files are listed.";
                if (failMessage != "") { lb.failMessage = failMessage; }
                if (relFolder != "") { lb.addLinkButton("Up one folder", cp.Utils.EncodeHTML(LogFileToolLinks.folderLink(cp, LogFileToolLinks.parentOf(relFolder)))); }
                lb.addFormHidden("logFolder", cp.Utils.EncodeHTML(relFolder));
                //
                // -- read builder state, in this order
                string term = (lb.sqlSearchTerm ?? "").Trim();
                if (term.Length > 200) { term = term.Substring(0, 200); }
                string sortField = lb.sortField;
                string sortDirection = lb.sortDirection;
                int pageSize = lb.paginationPageSize;
                if (pageSize > 500) { pageSize = lb.paginationPageSizeDefault; }
                int pageNumber = lb.paginationPageNumber;
                //
                // -- items
                var items = getItems(absFolder, term, sortField, sortDirection, out string readError);
                addColumns(lb);
                if (readError != "") { lb.failMessage = $"{lb.failMessage} {cp.Utils.EncodeHTML(readError)}".Trim(); }
                lb.recordCount = items.Count;
                //
                // -- rows
                int skip = (pageNumber - 1) * pageSize;
                if (skip >= items.Count && items.Count > 0) { skip = ((items.Count - 1) / pageSize) * pageSize; }
                foreach (ListItem item in items.Skip(skip).Take(pageSize)) {
                    lb.addRow();
                    string modifiedText = cp.Utils.EncodeHTML(item.modified.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
                    if (item.isFolder) {
                        string folderRel = relFolder == "" ? item.name : $"{relFolder}\\{item.name}";
                        lb.setCell($"<a href=\"{cp.Utils.EncodeHTML(LogFileToolLinks.folderLink(cp, folderRel))}\"><i class=\"fas fa-folder\"></i> {cp.Utils.EncodeHTML(item.name)}</a>");
                        lb.setCell("");
                        lb.setCell(modifiedText);
                    } else {
                        string fileRel = relFolder == "" ? item.name : $"{relFolder}\\{item.name}";
                        lb.setCell($"<a href=\"{cp.Utils.EncodeHTML(LogFileToolLinks.fileLink(cp, fileRel))}\">{cp.Utils.EncodeHTML(item.name)}</a>");
                        lb.setCell(cp.Utils.EncodeHTML(formatBytes(item.size)));
                        lb.setCell(modifiedText);
                    }
                }
                if (items.Count == 0 && string.IsNullOrEmpty(lb.failMessage)) { lb.infoMessage = "No log files were found in this folder."; }
                return lb.getHtml();
            } catch (Exception ex) {
                logger.Error(ex, $"{cp.core.logCommonMessage}");
                throw;
            }
        }
        //
        //====================================================================================================
        // -- Name, Size, Modified
        private static void addColumns(LayoutBuilderListBaseClass lb) {
            lb.addColumn(new ReportListColumnBaseClass { name = "name", caption = "Name", captionClass = AfwStyles.afwTextAlignLeft, cellClass = AfwStyles.afwTextAlignLeft, sortable = true, visible = true, downloadable = false, columnWidthPercent = 60 });
            lb.addColumn(new ReportListColumnBaseClass { name = "size", caption = "Size", captionClass = AfwStyles.afwTextAlignRight, cellClass = AfwStyles.afwTextAlignRight, sortable = true, visible = true, downloadable = false, columnWidthPercent = 15 });
            lb.addColumn(new ReportListColumnBaseClass { name = "modified", caption = "Modified", captionClass = AfwStyles.afwTextAlignLeft, cellClass = AfwStyles.afwTextAlignLeft, sortable = true, visible = true, downloadable = false, columnWidthPercent = 25 });
        }
        //
        //====================================================================================================
        /// <summary>
        /// Folders (always first, by name) then the .log and .txt files. Hidden, system and reparse point entries are skipped.
        /// </summary>
        private static List<ListItem> getItems(string absFolder, string term, string sortField, string sortDirection, out string readError) {
            readError = "";
            var folders = new List<ListItem>();
            var files = new List<ListItem>();
            try {
                var directory = new DirectoryInfo(absFolder);
                foreach (DirectoryInfo subfolder in directory.GetDirectories()) {
                    if (isSkipped(subfolder.Attributes)) { continue; }
                    if (term.Length > 0 && subfolder.Name.IndexOf(term, StringComparison.OrdinalIgnoreCase) < 0) { continue; }
                    folders.Add(new ListItem { isFolder = true, name = subfolder.Name, modified = subfolder.LastWriteTime });
                }
                foreach (FileInfo file in directory.GetFiles()) {
                    if (isSkipped(file.Attributes)) { continue; }
                    if (Array.FindIndex(LogFilePathController.allowedExtensions, x => string.Equals(x, file.Extension, StringComparison.OrdinalIgnoreCase)) < 0) { continue; }
                    if (term.Length > 0 && file.Name.IndexOf(term, StringComparison.OrdinalIgnoreCase) < 0) { continue; }
                    files.Add(new ListItem { isFolder = false, name = file.Name, size = file.Length, modified = file.LastWriteTime });
                }
            } catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException) {
                logger.Error(ex, $"log file tool could not list folder");
                readError = "The folder could not be read.";
            }
            //
            // -- files: whitelisted sort fields, default is modified descending
            bool ascending = string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);
            IEnumerable<ListItem> sortedFiles;
            switch ((sortField ?? "").ToLowerInvariant()) {
                case "name":
                    sortedFiles = ascending ? files.OrderBy(x => x.name, StringComparer.OrdinalIgnoreCase) : files.OrderByDescending(x => x.name, StringComparer.OrdinalIgnoreCase);
                    break;
                case "size":
                    sortedFiles = ascending ? files.OrderBy(x => x.size).ThenBy(x => x.name, StringComparer.OrdinalIgnoreCase) : files.OrderByDescending(x => x.size).ThenBy(x => x.name, StringComparer.OrdinalIgnoreCase);
                    break;
                case "modified":
                    sortedFiles = ascending ? files.OrderBy(x => x.modified).ThenBy(x => x.name, StringComparer.OrdinalIgnoreCase) : files.OrderByDescending(x => x.modified).ThenBy(x => x.name, StringComparer.OrdinalIgnoreCase);
                    break;
                default:
                    sortedFiles = files.OrderByDescending(x => x.modified).ThenBy(x => x.name, StringComparer.OrdinalIgnoreCase);
                    break;
            }
            var result = folders.OrderBy(x => x.name, StringComparer.OrdinalIgnoreCase).ToList();
            result.AddRange(sortedFiles);
            return result;
        }
        //
        private static bool isSkipped(FileAttributes attributes) {
            return (attributes & (FileAttributes.ReparsePoint | FileAttributes.Hidden | FileAttributes.System)) != 0;
        }
        //
        //====================================================================================================
        /// <summary>
        /// 1,234 bytes as "1.2 KB"
        /// </summary>
        public static string formatBytes(long bytes) {
            if (bytes < 1024) { return $"{bytes} B"; }
            if (bytes < 1024L * 1024) { return string.Format(CultureInfo.InvariantCulture, "{0:0.0} KB", bytes / 1024.0); }
            if (bytes < 1024L * 1024 * 1024) { return string.Format(CultureInfo.InvariantCulture, "{0:0.0} MB", bytes / (1024.0 * 1024)); }
            return string.Format(CultureInfo.InvariantCulture, "{0:0.0} GB", bytes / (1024.0 * 1024 * 1024));
        }
        //
        /// <summary>
        /// a request value, with control characters removed and shortened, for the log
        /// </summary>
        internal static string cleanForLog(string value) {
            if (string.IsNullOrEmpty(value)) { return ""; }
            var chars = value.Where(c => c >= 0x20).Take(200).ToArray();
            return new string(chars);
        }
    }
    //
    //====================================================================================================
    /// <summary>
    /// Links between the two screens. Every value is url-encoded here. The caller html-encodes the result when it is placed in an attribute.
    /// There is no target=_blank, everything renders inside the admin layout.
    /// </summary>
    public static class LogFileToolLinks {
        //
        private static string baseUrl(CPClass cp) {
            return $"{cp.Site.GetText("adminurl")}?addonguid={cp.Utils.EncodeRequestVariable(Constants.addonGuidLogFileTool)}";
        }
        //
        /// <summary>
        /// link to the file list. relFolder "" is the log root
        /// </summary>
        public static string folderLink(CPClass cp, string relFolder) {
            return string.IsNullOrEmpty(relFolder) ? baseUrl(cp) : $"{baseUrl(cp)}&logFolder={cp.Utils.EncodeRequestVariable(relFolder)}";
        }
        //
        /// <summary>
        /// link to the log view of one file
        /// </summary>
        public static string fileLink(CPClass cp, string relFile) {
            return $"{baseUrl(cp)}&logFile={cp.Utils.EncodeRequestVariable(relFile)}";
        }
        //
        /// <summary>
        /// the folder part of a relative path, "" if there is none
        /// </summary>
        public static string parentOf(string rel) {
            if (string.IsNullOrEmpty(rel)) { return ""; }
            int pos = rel.LastIndexOf('\\');
            return pos < 0 ? "" : rel.Substring(0, pos);
        }
    }
}
