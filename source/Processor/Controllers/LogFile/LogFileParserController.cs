
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Contensive.Processor.Models.Domain;
//
namespace Contensive.Processor.Controllers {
    //
    //====================================================================================================
    /// <summary>
    /// Parse an NLog file written with the layout ${longdate}|${level:uppercase=true}|${message}|${exception}|${stacktrace}
    /// into entries. No dependency on CPClass or CoreController. The text in a log file is untrusted.
    /// </summary>
    public static class LogFileParserController {
        //
        /// <summary>
        /// size guard. Only the newest maxParseBytesDefault of a larger file are parsed.
        /// </summary>
        public const long maxParseBytesDefault = 50L * 1024 * 1024;
        //
        private const string timeFormat = "yyyy-MM-dd HH:mm:ss.ffff";
        //
        // -- a start line. Also requires the time to parse (see isStartLine)
        private static readonly Regex startLineRegex = new Regex(
            @"^(?<time>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{4})\|(?<level>TRACE|DEBUG|INFO|WARN|ERROR|FATAL)\|(?<rest>.*)$",
            RegexOptions.CultureInvariant | RegexOptions.Compiled);
        //
        // -- the prefix written by CoreController.logCommonMessage. No Singleline, so . does not cross lines
        private static readonly Regex prefixRegex = new Regex(
            @"^app\[(?<app>[^\]]*)\],user\[(?<user>.*?)\],view\[(?<view>[^\]]*)\],visit\[(?<visit>[^\]]*)\],thread\[(?<thread>[^\]]*)\],url\[(?<url>.*?)\](?:,|(?=\n)|$)",
            RegexOptions.CultureInvariant | RegexOptions.Compiled);
        //
        // -- text that starts an exception
        private static readonly Regex exceptionStartRegex = new Regex(
            @"^[A-Za-z_][A-Za-z0-9_.`+]*(Exception|Error)\b",
            RegexOptions.CultureInvariant | RegexOptions.Compiled);
        //
        //====================================================================================================
        /// <summary>
        /// Parse a log file. Reads with FileShare.ReadWrite so an open NLog writer does not block it. If the file is larger than maxParseBytes, only the newest maxParseBytes are read, beginning at the next line boundary.
        /// </summary>
        /// <param name="absPathFilename">The full path, already validated by LogFilePathController</param>
        /// <param name="maxParseBytes"></param>
        /// <returns></returns>
        public static LogFileParseResultModel parseFile(string absPathFilename, long maxParseBytes) {
            const int retryMax = 3;
            for (int attempt = 1; ; attempt++) {
                try {
                    using (var fs = new FileStream(absPathFilename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, 65536, FileOptions.SequentialScan)) {
                        long fileLength = fs.Length;
                        bool isTruncated = false;
                        long startOffset = 0;
                        if (fileLength > maxParseBytes) {
                            //
                            // -- size guard. Seek one byte before the cut and discard through the next line feed. If the cut is exactly at a line start the preceding byte is the line feed and nothing else is lost.
                            // -- a line feed byte never occurs inside a multi-byte utf-8 sequence, so the decode stays aligned
                            isTruncated = true;
                            startOffset = fileLength - maxParseBytes;
                            fs.Seek(startOffset - 1, SeekOrigin.Begin);
                            int b;
                            startOffset -= 1;
                            while ((b = fs.ReadByte()) >= 0) {
                                startOffset += 1;
                                if (b == 0x0A) { break; }
                            }
                        }
                        using (var reader = new StreamReader(fs, new UTF8Encoding(false, false), true, 65536)) {
                            var result = parse(reader, isTruncated);
                            result.fileLength = fileLength;
                            result.startOffset = startOffset;
                            return result;
                        }
                    }
                } catch (IOException) {
                    if (attempt >= retryMax) { throw; }
                    Thread.Sleep(50 * attempt);
                }
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Parse log text into entries. Pure, used by parseFile and by tests.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="isTruncated">True if the text began at an arbitrary position (size guard). Lines before the first start line are then dropped.</param>
        /// <returns></returns>
        public static LogFileParseResultModel parse(TextReader reader, bool isTruncated) {
            var result = new LogFileParseResultModel { isTruncated = isTruncated };
            //
            // -- lines before the first start line
            var orphanLines = new List<string>();
            //
            // -- the entry being built
            LogFileEntryModel entry = null;
            string time = "";
            string level = "";
            StringBuilder rest = null;
            int lineNumber = 0;
            string line;
            while ((line = reader.ReadLine()) != null) {
                lineNumber++;
                if (isStartLine(line, out string lineTime, out string lineLevel, out string lineRest)) {
                    if (entry != null) { finishEntry(result, entry, time, level, rest.ToString()); }
                    entry = new LogFileEntryModel { sequence = result.entries.Count, lineNumber = lineNumber };
                    time = lineTime;
                    level = lineLevel;
                    rest = new StringBuilder(lineRest);
                    continue;
                }
                if (entry == null) {
                    orphanLines.Add(line);
                    continue;
                }
                //
                // -- continuation line, never dropped once an entry exists
                rest.Append('\n').Append(line);
                entry.continuationLineCount++;
            }
            if (entry != null) { finishEntry(result, entry, time, level, rest.ToString()); }
            //
            // -- lines before the first entry
            if (orphanLines.Count > 0) {
                if (isTruncated) {
                    result.skippedLeadingLines = orphanLines.Count;
                } else {
                    //
                    // -- one raw entry at the top of the list. Sequence numbers are shifted so the raw entry is first.
                    foreach (var e in result.entries) { e.sequence++; }
                    var raw = new LogFileEntryModel {
                        sequence = 0,
                        lineNumber = 1,
                        level = "",
                        levelRank = -1,
                        message = string.Join("\n", orphanLines),
                        isMalformed = true,
                        continuationLineCount = orphanLines.Count - 1
                    };
                    result.entries.Insert(0, raw);
                    result.malformedCount++;
                }
            }
            return result;
        }
        //
        //====================================================================================================
        /// <summary>
        /// true if the line starts an entry: matches the start pattern and the time is a valid date
        /// </summary>
        private static bool isStartLine(string line, out string time, out string level, out string rest) {
            time = ""; level = ""; rest = "";
            // -- quick reject, most continuation lines
            if (line.Length < 29 || !char.IsDigit(line[0])) { return false; }
            var match = startLineRegex.Match(line);
            if (!match.Success) { return false; }
            if (!DateTime.TryParseExact(match.Groups["time"].Value, timeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out _)) { return false; }
            time = match.Groups["time"].Value;
            level = match.Groups["level"].Value;
            rest = match.Groups["rest"].Value;
            return true;
        }
        //
        //====================================================================================================
        /// <summary>
        /// populate the entry from the joined text after "time|LEVEL|" and add it to the result
        /// </summary>
        private static void finishEntry(LogFileParseResultModel result, LogFileEntryModel entry, string time, string level, string rest) {
            entry.timeText = time;
            entry.time = DateTime.TryParseExact(time, timeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime t) ? t : (DateTime?)null;
            entry.level = level;
            entry.levelRank = getLevelRank(level);
            splitFields(rest, out string message, out string exception, out string loggerStack, out bool isMalformed);
            entry.exception = exception;
            entry.loggerStack = loggerStack;
            entry.isMalformed = isMalformed;
            //
            // -- prefix
            var prefix = prefixRegex.Match(message);
            if (prefix.Success) {
                entry.app = prefix.Groups["app"].Value;
                entry.user = prefix.Groups["user"].Value;
                entry.view = prefix.Groups["view"].Value;
                entry.visit = prefix.Groups["visit"].Value;
                entry.thread = prefix.Groups["thread"].Value;
                entry.url = prefix.Groups["url"].Value;
                int colon = entry.user.IndexOf(':');
                string idText = (colon < 0) ? entry.user : entry.user.Substring(0, colon);
                entry.userName = (colon < 0) ? "" : entry.user.Substring(colon + 1);
                entry.userId = int.TryParse(idText, NumberStyles.None, CultureInfo.InvariantCulture, out int id) ? id : 0;
                message = message.Substring(prefix.Length);
            }
            entry.message = message.TrimStart();
            result.entries.Add(entry);
            if (entry.isMalformed) { result.malformedCount++; }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Split the text after "time|LEVEL|" into message, exception and logger stack.
        /// </summary>
        public static void splitFields(string rest, out string message, out string exception, out string loggerStack, out bool isMalformed) {
            message = rest;
            exception = "";
            loggerStack = "";
            isMalformed = false;
            //
            // -- count pipes. need at least 2
            int pipeCount = 0;
            foreach (char c in rest) { if (c == '|') { pipeCount++; } }
            if (pipeCount < 2) {
                isMalformed = true;
                return;
            }
            int last = rest.LastIndexOf('|');
            loggerStack = rest.Substring(last + 1).Trim();
            string r = rest.Substring(0, last);
            if (r.EndsWith("|", StringComparison.Ordinal)) {
                //
                // -- common case: message||stack
                message = r.Substring(0, r.Length - 1);
                return;
            }
            //
            // -- split at the first pipe followed by text that looks like an exception, else at the last pipe
            int cut = -1;
            int lastPipe = -1;
            for (int pos = r.IndexOf('|'); pos >= 0; pos = r.IndexOf('|', pos + 1)) {
                lastPipe = pos;
                if (exceptionStartRegex.IsMatch(r.Substring(pos + 1))) { cut = pos; break; }
            }
            if (cut < 0) { cut = lastPipe; }
            message = r.Substring(0, cut);
            exception = r.Substring(cut + 1);
        }
        //
        //====================================================================================================
        /// <summary>
        /// TRACE 0, DEBUG 1, INFO 2, WARN 3, ERROR 4, FATAL 5, unknown -1
        /// </summary>
        public static int getLevelRank(string level) {
            switch (level) {
                case "TRACE": return 0;
                case "DEBUG": return 1;
                case "INFO": return 2;
                case "WARN": return 3;
                case "ERROR": return 4;
                case "FATAL": return 5;
                default: return -1;
            }
        }
    }
}
