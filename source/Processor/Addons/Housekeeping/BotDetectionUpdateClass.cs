
using Contensive.Processor.Controllers;
using NLog;
using System;
//
namespace Contensive.Processor.Addons.Housekeeping {
    /// <summary>
    /// Daily housekeeping task that downloads updated bot detection data:
    /// - regexes.yaml from ua-parser/uap-core (for UAParser)
    /// - crawler-user-agents.json from monperrus/crawler-user-agents (crawler regex patterns)
    /// Files are cached to programDataFiles and hot-swapped into BotDetectionService.
    /// </summary>
    public static class BotDetectionUpdateClass {
        //
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        //
        //====================================================================================================
        /// <summary>
        /// execute hourly tasks
        /// </summary>
        public static void executeHourlyTasks(HouseKeepEnvironmentModel env) {
            try {
                //
                env.log("Housekeep, executeHourlyTasks, BotDetectionUpdateClass");
                //
            } catch (Exception ex) {
                logger.Error(ex, $"{env.core.logCommonMessage}");
                LogController.logAlarm(env.core, "Housekeep, exception, ex [" + ex + "]");
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Download updated bot detection data files and reload the in-memory detection service.
        /// On failure, existing in-memory data is preserved (no disruption to detection).
        /// </summary>
        public static void executeDailyTasks(HouseKeepEnvironmentModel env) {
            try {
                env.log("Housekeep, BotDetectionUpdateClass, updating bot detection data");
                //
                bool regexesUpdated = downloadFile(env, BotDetectionService.RegexesYamlUrl, "BotDetection\\regexes.yaml", "regexes.yaml");
                bool crawlerUpdated = downloadFile(env, BotDetectionService.CrawlerJsonUrl, "BotDetection\\crawler-user-agents.json", "crawler-user-agents.json");
                //
                if (regexesUpdated || crawlerUpdated) {
                    BotDetectionService.reload(env.core);
                    env.log("Housekeep, BotDetectionUpdateClass, reloaded bot detection data");
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{env.core.logCommonMessage}");
                LogController.logAlarm(env.core, $"Housekeep, BotDetectionUpdateClass exception, ex [{ex}]");
                // -- do not rethrow; bot detection update failure should not block other housekeeping
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Download a file from a URL and save to programDataFiles. Returns true on success.
        /// </summary>
        private static bool downloadFile(HouseKeepEnvironmentModel env, string url, string cachePathFilename, string label) {
            try {
                var http = new HttpController { timeout = 60 };
                string content = http.getURL(url);
                if (string.IsNullOrEmpty(content)) {
                    env.log($"Housekeep, BotDetectionUpdateClass, downloaded empty {label}, skipping");
                    return false;
                }
                env.core.programDataFiles.saveFile(cachePathFilename, content);
                env.log($"Housekeep, BotDetectionUpdateClass, downloaded and cached {label} ({content.Length:N0} chars)");
                return true;
            } catch (Exception ex) {
                env.log($"Housekeep, BotDetectionUpdateClass, failed to download {label}: {ex.Message}");
                logger.Warn(ex, $"{env.core.logCommonMessage}, BotDetectionUpdateClass, failed to download {label}");
                return false;
            }
        }
    }
}
