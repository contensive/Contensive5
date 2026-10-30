
using System;
using System.IO;
using System.Security;
//
namespace Contensive.Processor.Controllers {
    //
    //====================================================================================================
    /// <summary>
    /// Confines every file access of the log file tool to the log root folder. 
    /// All values arrive from a request and are untrusted. No dependency on CPClass or CoreController.
    /// </summary>
    public static partial class LogFilePathController {
        //
        /// <summary>
        /// the only file extensions the tool lists or opens
        /// </summary>
        public static readonly string[] allowedExtensions = { ".log", ".txt" };
        //
        /// <summary>
        /// characters rejected anywhere in a requested path. The colon rejects drive letters, alternate data streams and \\?\ prefixes.
        /// </summary>
        private const string rejectedChars = ":*?\"<>|";
        //
        //====================================================================================================
        /// <summary>
        /// Normalize a folder to a full path that ends with a directory separator.
        /// </summary>
        public static string normalizeRoot(string folder) {
            string full = Path.GetFullPath(folder);
            char sep = Path.DirectorySeparatorChar;
            return full.EndsWith(sep.ToString(), StringComparison.Ordinal) ? full : $"{full}{sep}";
        }
        //
        // -- Windows reserved device names. The superscript digits are also reserved by Windows (COM¹ COM² COM³ LPT¹ LPT² LPT³)
        private static readonly string[] deviceNames = {
            "CON", "PRN", "AUX", "NUL",
            "COM0", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
            "LPT0", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
            "COM\u00B9", "COM\u00B2", "COM\u00B3", "LPT\u00B9", "LPT\u00B2", "LPT\u00B3"
        };
        //
        //====================================================================================================
        /// <summary>
        /// true if the segment is a Windows reserved device name. The base name is the text before the first dot, trimmed, compared case-insensitively. 
        /// Windows treats "CON.log" as the console device, on every extension. Checked on every platform so behavior does not depend on the host.
        /// </summary>
        public static bool isDeviceName(string segment) {
            if (string.IsNullOrEmpty(segment)) { return false; }
            int dot = segment.IndexOf('.');
            string baseName = (dot < 0 ? segment : segment.Substring(0, dot)).Trim();
            return Array.FindIndex(deviceNames, x => string.Equals(x, baseName, StringComparison.OrdinalIgnoreCase)) >= 0;
        }
        //
        //====================================================================================================
        /// <summary>
        /// a request value, with control characters removed and shortened to 200 characters, for the log
        /// </summary>
        public static string cleanForLog(string value) {
            if (string.IsNullOrEmpty(value)) { return ""; }
            var sb = new System.Text.StringBuilder();
            foreach (char c in value) {
                if (c < 0x20) { continue; }
                sb.Append(c);
                if (sb.Length >= 200) { break; }
            }
            return sb.ToString();
        }
        //
        //====================================================================================================
        /// <summary>
        /// Resolve a requested path (relative to the log root) to a full path that is verified to be inside the root. 
        /// The first failure returns false with a short internal rejectReason. The rejectReason is for the log, do not show it to the user.
        /// If the file does not exist it still returns true so the caller can show "not found". A folder must exist.
        /// </summary>
        /// <param name="logRootFull">The full path of the log root, ending with a separator. See normalizeRoot()</param>
        /// <param name="requested">The path from the request, relative to the root. "\" or "/" separators</param>
        /// <param name="isFile">true for a file, false for a folder</param>
        /// <param name="absPath">The full path</param>
        /// <param name="relPath">The canonical path relative to the root with "\" separators. Use this in links and hiddens.</param>
        /// <param name="rejectReason"></param>
        /// <returns></returns>
        public static bool tryResolve(string logRootFull, string requested, bool isFile, out string absPath, out string relPath, out string rejectReason) {
            absPath = "";
            relPath = "";
            rejectReason = "";
            //
            // -- 1. empty
            if (requested == null) { requested = ""; }
            if (requested.Length == 0) {
                if (isFile) { rejectReason = "empty file name"; return false; }
                if (!Directory.Exists(logRootFull)) { rejectReason = "log root does not exist"; return false; }
                absPath = logRootFull;
                return true;
            }
            //
            // -- 2. separators
            string normalized = requested.Replace('/', '\\');
            //
            // -- 3. invalid characters
            char[] invalidPathChars = Path.GetInvalidPathChars();
            foreach (char c in normalized) {
                if (c < 0x20 || Array.IndexOf(invalidPathChars, c) >= 0 || rejectedChars.IndexOf(c) >= 0) {
                    rejectReason = "invalid character";
                    return false;
                }
            }
            //
            // -- 4. rooted, unc and device paths
            if (normalized[0] == '\\') { rejectReason = "rooted path"; return false; }
            if (Path.IsPathRooted(normalized)) { rejectReason = "rooted path"; return false; }
            //
            // -- 5. segments. Windows silently trims trailing dots and spaces, so they bypass the extension check
            string[] segments = normalized.Split('\\');
            foreach (string segment in segments) {
                if (segment.Length == 0) { rejectReason = "empty segment"; return false; }
                char lastChar = segment[segment.Length - 1];
                if (lastChar == '.' || lastChar == ' ') { rejectReason = "dot segment or trailing dot or space"; return false; }
                if (isDeviceName(segment)) { rejectReason = "device name"; return false; }
            }
            //
            // -- 6. resolve
            string full;
            try {
                full = Path.GetFullPath(Path.Combine(logRootFull, string.Join(Path.DirectorySeparatorChar.ToString(), segments)));
            } catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException || ex is SecurityException) {
                rejectReason = $"path could not be resolved: {ex.GetType().Name}";
                return false;
            }
            //
            // -- 7. containment. Root ends with a separator, so a sibling folder with the same prefix does not match
            if (!full.StartsWith(logRootFull, StringComparison.OrdinalIgnoreCase)) { rejectReason = "outside the log root"; return false; }
            if (full.Length <= logRootFull.Length) { rejectReason = "not below the log root"; return false; }
            //
            // -- 8. extension
            if (isFile) {
                string extension = Path.GetExtension(full);
                if (Array.FindIndex(allowedExtensions, x => string.Equals(x, extension, StringComparison.OrdinalIgnoreCase)) < 0) {
                    rejectReason = "extension not allowed";
                    return false;
                }
            }
            //
            // -- 9. existence and type. A missing file is the caller's decision, a missing folder is rejected
            if (!isFile && !Directory.Exists(full)) { rejectReason = "folder does not exist"; return false; }
            //
            // -- 10. reparse points. A junction or symlink inside the root could lead outside it
            string current = logRootFull.TrimEnd(Path.DirectorySeparatorChar);
            foreach (string segment in segments) {
                current = Path.Combine(current, segment);
                if (!File.Exists(current) && !Directory.Exists(current)) { break; }
                FileAttributes attributes;
                try {
                    attributes = File.GetAttributes(current);
                } catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException || ex is NotSupportedException) {
                    rejectReason = $"attributes could not be read: {ex.GetType().Name}";
                    return false;
                }
                if ((attributes & FileAttributes.ReparsePoint) != 0) { rejectReason = "reparse point"; return false; }
            }
            //
            // -- 11. canonical values
            absPath = full;
            relPath = full.Substring(logRootFull.Length).TrimEnd(Path.DirectorySeparatorChar).Replace(Path.DirectorySeparatorChar, '\\');
            return true;
        }
    }
}
