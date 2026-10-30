
using System;
using System.Collections.Generic;
using System.IO;
using NLog;
using NLog.Config;
using NLog.Targets;
using NLog.Targets.Wrappers;
//
namespace Contensive.Processor.Controllers {
    //
    //====================================================================================================
    /// <summary>
    /// Resolve the folder the log file tool reads.
    /// </summary>
    public static partial class LogFilePathController {
        //
        // static logger
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        //
        //====================================================================================================
        /// <summary>
        /// The log root is the folder NLog is actually writing to: the directory of the first FileTarget in the NLog configuration (wrapper targets are unwrapped) that exists.
        /// NLog.config can write to a different folder than programData (for example a hard-coded D:\Contensive\Logs), so the programData Logs folder is only the fallback.
        /// Always returns a full path that ends with a directory separator. Never throws.
        /// </summary>
        public static string getLogRoot(CoreController core) {
            string fallback = $"{core.programDataFiles.localAbsRootPath}Logs\\";
            try {
                string nlogFolder = getNLogFileTargetFolder(LogManager.Configuration, DateTime.Now);
                if (!string.IsNullOrEmpty(nlogFolder)) { return normalizeRoot(nlogFolder); }
            } catch (Exception ex) {
                logger.Warn(ex, $"{core.logCommonMessage}, log file tool could not resolve the NLog folder, using the programData Logs folder");
            }
            try {
                return normalizeRoot(fallback);
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                return fallback.EndsWith("\\", StringComparison.Ordinal) ? fallback : $"{fallback}\\";
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// The directory of the first FileTarget in the configuration whose rendered file name has a directory that exists. "" if none.
        /// </summary>
        public static string getNLogFileTargetFolder(LoggingConfiguration configuration, DateTime renderTime) {
            if (configuration == null) { return ""; }
            var targets = new List<Target>();
            foreach (Target target in configuration.AllTargets) { collectFileTargets(target, targets, 0); }
            var logEvent = new LogEventInfo { TimeStamp = renderTime, Level = LogLevel.Info };
            foreach (Target target in targets) {
                try {
                    string fileName = ((FileTarget)target).FileName?.Render(logEvent);
                    if (string.IsNullOrWhiteSpace(fileName)) { continue; }
                    string folder = Path.GetDirectoryName(fileName);
                    if (string.IsNullOrWhiteSpace(folder)) { continue; }
                    if (fileName.Contains("${")) { continue; }
                    folder = Path.GetFullPath(folder);
                    //
                    // -- a drive or filesystem root is never the log folder. It results from a name that did not resolve, like "${LogDirectory}/x.log" rendered with no variable
                    if (string.Equals(folder, Path.GetPathRoot(folder), StringComparison.OrdinalIgnoreCase)) { continue; }
                    if (Directory.Exists(folder)) { return folder; }
                } catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException || ex is IOException) {
                    //
                    // -- this target's name could not be rendered or resolved, try the next
                    continue;
                }
            }
            return "";
        }
        //
        // -- add the FileTargets in a target and in any targets it wraps (AsyncTargetWrapper, BufferingWrapper, groups)
        private static void collectFileTargets(Target target, List<Target> result, int depth) {
            if (target == null || depth > 8) { return; }
            if (target is FileTarget) {
                if (!result.Contains(target)) { result.Add(target); }
                return;
            }
            if (target is WrapperTargetBase wrapper) {
                collectFileTargets(wrapper.WrappedTarget, result, depth + 1);
                return;
            }
            if (target is CompoundTargetBase compound) {
                foreach (Target child in compound.Targets) { collectFileTargets(child, result, depth + 1); }
            }
        }
    }
}
