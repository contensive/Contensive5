
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using UAParser;
//
namespace Contensive.Processor.Controllers {
    //
    /// <summary>
    /// Manages bot/crawler detection using multiple sources:
    /// 1) Contensive custom bots (contensive-bots.json) - internal monitoring tools and known partners
    /// 2) Browser format validation (RFC 9110) - detects non-conforming user-agents
    /// 3) UAParser (regexes.yaml) - provides Device.IsSpider plus browser/device family names
    /// 4) crawler-user-agents.json (from github.com/monperrus/crawler-user-agents) - community regex patterns
    ///
    /// Sources 3-4 can be updated at runtime via housekeeping, with bundled fallbacks for first-run.
    /// </summary>
    public static class BotDetectionService {
        //
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        //
        // -- file names for cached data in programDataFiles
        private const string CachedRegexesYamlFilename = "BotDetection\\regexes.yaml";
        private const string CachedCrawlerJsonFilename = "BotDetection\\crawler-user-agents.json";
        //
        // -- bundled fallback files deployed alongside assembly (CopyToOutputDirectory)
        private const string BundledRegexesYamlFilename = "regexes.yaml";
        private const string BundledCrawlerJsonFilename = "crawler-user-agents.json";
        private const string BundledContensiveBotsFilename = "contensive-bots.json";
        //
        // -- download URLs
        public const string RegexesYamlUrl = "https://raw.githubusercontent.com/ua-parser/uap-core/master/regexes.yaml";
        public const string CrawlerJsonUrl = "https://raw.githubusercontent.com/monperrus/crawler-user-agents/master/crawler-user-agents.json";
        //
        // -- thread safety for hot-swap during reload
        private static readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        //
        // -- active detection data
        private static Parser _uaParser;
        private static List<Regex> _crawlerPatterns = new List<Regex>();
        private static List<CustomBotEntry> _customBots = new List<CustomBotEntry>();
        private static bool _initialized;
        //
        //====================================================================================================
        /// <summary>
        /// Initialize the service by loading cached or bundled detection data.
        /// Safe to call multiple times; only the first call loads data.
        /// </summary>
        public static void init(CoreController core) {
            if (_initialized) { return; }
            _lock.EnterWriteLock();
            try {
                if (_initialized) { return; }
                loadCustomBots(core);
                loadUaParser(core);
                loadCrawlerPatterns(core);
                _initialized = true;
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}, BotDetectionService.init");
            } finally {
                _lock.ExitWriteLock();
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Reload detection sources from disk cache. Called by housekeeping after downloading new files.
        /// Custom bots are not reloaded as they are bundled with the assembly.
        /// </summary>
        public static void reload(CoreController core) {
            _lock.EnterWriteLock();
            try {
                loadUaParser(core);
                loadCrawlerPatterns(core);
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}, BotDetectionService.reload");
            } finally {
                _lock.ExitWriteLock();
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Parse a user-agent string using UAParser. Returns ClientInfo with Device, OS, UA details.
        /// </summary>
        public static ClientInfo parse(string userAgentString) {
            _lock.EnterReadLock();
            try {
                var parser = _uaParser ?? Parser.GetDefault();
                return parser.Parse(userAgentString);
            } finally {
                _lock.ExitReadLock();
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Check if the user-agent matches any known crawler pattern from crawler-user-agents.json.
        /// </summary>
        public static bool isCrawler(string userAgentString) {
            if (string.IsNullOrEmpty(userAgentString)) { return false; }
            _lock.EnterReadLock();
            try {
                foreach (var pattern in _crawlerPatterns) {
                    try {
                        if (pattern.IsMatch(userAgentString)) { return true; }
                    } catch (RegexMatchTimeoutException) {
                        // pattern took too long, skip it
                    }
                }
                return false;
            } finally {
                _lock.ExitReadLock();
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Check if the user-agent matches any custom bot from contensive-bots.json and return the bot name if found.
        /// Returns null if not a custom bot.
        /// </summary>
        public static string getCustomBotName(string userAgentString) {
            if (string.IsNullOrEmpty(userAgentString)) { return null; }
            _lock.EnterReadLock();
            try {
                foreach (var bot in _customBots) {
                    if (string.IsNullOrEmpty(bot.pattern)) { continue; }
                    bool isMatch = bot.isRegex
                        ? Regex.IsMatch(userAgentString, bot.pattern, RegexOptions.IgnoreCase)
                        : userAgentString.IndexOf(bot.pattern, StringComparison.OrdinalIgnoreCase) >= 0;
                    if (isMatch) {
                        return bot.name;
                    }
                }
                return null;
            } finally {
                _lock.ExitReadLock();
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Check if the user-agent conforms to standard browser format (RFC 9110).
        /// Legitimate browsers typically start with Mozilla/ and contain platform info and rendering engine.
        /// </summary>
        public static bool conformsToBrowserFormat(string userAgentString) {
            if (string.IsNullOrEmpty(userAgentString)) { return false; }

            // Check for Mozilla prefix (standard for all modern browsers)
            if (!userAgentString.StartsWith("Mozilla/", StringComparison.OrdinalIgnoreCase)) {
                return false;
            }

            // Check for platform information in parentheses
            if (!userAgentString.Contains("(")) {
                return false;
            }

            // Check for rendering engine (AppleWebKit, Gecko, or Trident)
            if (!userAgentString.Contains("AppleWebKit/") &&
                !userAgentString.Contains("Gecko/") &&
                !userAgentString.Contains("Trident/")) {
                return false;
            }

            return true;
        }
        //
        //====================================================================================================
        /// <summary>
        /// Load UAParser from cached regexes.yaml (programDataFiles), falling back to bundled copy, then NuGet default.
        /// </summary>
        private static void loadUaParser(CoreController core) {
            try {
                //
                // -- try cached copy in programDataFiles (updated by housekeeping)
                if (core.programDataFiles.fileExists(CachedRegexesYamlFilename)) {
                    string yaml = core.programDataFiles.readFileText(CachedRegexesYamlFilename);
                    if (!string.IsNullOrEmpty(yaml)) {
                        _uaParser = Parser.FromYaml(yaml);
                        logger.Trace($"{core.logCommonMessage}, BotDetectionService loaded UAParser from cached regexes.yaml");
                        return;
                    }
                }
                //
                // -- try bundled copy deployed with assembly
                string bundledYaml = readBundledFile(core, BundledRegexesYamlFilename);
                if (!string.IsNullOrEmpty(bundledYaml)) {
                    _uaParser = Parser.FromYaml(bundledYaml);
                    logger.Trace($"{core.logCommonMessage}, BotDetectionService loaded UAParser from bundled regexes.yaml");
                    return;
                }
            } catch (Exception ex) {
                logger.Warn(ex, $"{core.logCommonMessage}, BotDetectionService.loadUaParser failed to load custom yaml, using NuGet default");
            }
            //
            // -- fall back to NuGet-bundled default
            _uaParser = Parser.GetDefault();
            logger.Trace($"{core.logCommonMessage}, BotDetectionService loaded UAParser from NuGet default");
        }
        //
        //====================================================================================================
        /// <summary>
        /// Load crawler patterns from cached JSON (programDataFiles), falling back to bundled copy.
        /// </summary>
        private static void loadCrawlerPatterns(CoreController core) {
            string json = null;
            try {
                //
                // -- try cached copy in programDataFiles (updated by housekeeping)
                if (core.programDataFiles.fileExists(CachedCrawlerJsonFilename)) {
                    json = core.programDataFiles.readFileText(CachedCrawlerJsonFilename);
                }
                //
                // -- try bundled copy deployed with assembly
                if (string.IsNullOrEmpty(json)) {
                    json = readBundledFile(core, BundledCrawlerJsonFilename);
                }
                //
                if (string.IsNullOrEmpty(json)) {
                    logger.Warn($"{core.logCommonMessage}, BotDetectionService, no crawler-user-agents.json found, crawler pattern detection disabled");
                    _crawlerPatterns = new List<Regex>();
                    return;
                }
                //
                var entries = JsonConvert.DeserializeObject<List<CrawlerEntry>>(json);
                var patterns = new List<Regex>();
                if (entries != null) {
                    foreach (var entry in entries) {
                        if (string.IsNullOrEmpty(entry.pattern)) { continue; }
                        try {
                            patterns.Add(new Regex(entry.pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromMilliseconds(100)));
                        } catch (Exception ex) {
                            logger.Warn($"{core.logCommonMessage}, BotDetectionService, skipping invalid crawler pattern [{entry.pattern}], {ex.Message}");
                        }
                    }
                }
                _crawlerPatterns = patterns;
                logger.Trace($"{core.logCommonMessage}, BotDetectionService loaded {patterns.Count} crawler patterns");
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}, BotDetectionService.loadCrawlerPatterns");
                _crawlerPatterns = new List<Regex>();
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Read a file bundled with the assembly (CopyToOutputDirectory in csproj).
        /// Uses core.programFiles which points to the assembly's execution directory.
        /// </summary>
        private static string readBundledFile(CoreController core, string filename) {
            try {
                if (core.programFiles.fileExists(filename)) {
                    return core.programFiles.readFileText(filename);
                }
            } catch (Exception ex) {
                logger.Warn(ex, $"{core.logCommonMessage}, BotDetectionService.readBundledFile [{filename}]");
            }
            return null;
        }
        //
        //====================================================================================================
        /// <summary>
        /// Load custom bots from bundled contensive-bots.json.
        /// This file is never updated by housekeeping, only by build.
        /// </summary>
        private static void loadCustomBots(CoreController core) {
            string json = null;
            try {
                // Load bundled copy deployed with assembly
                json = readBundledFile(core, BundledContensiveBotsFilename);

                if (string.IsNullOrEmpty(json)) {
                    logger.Debug($"{core.logCommonMessage}, BotDetectionService, no contensive-bots.json found, custom bot detection disabled");
                    _customBots = new List<CustomBotEntry>();
                    return;
                }

                var entries = JsonConvert.DeserializeObject<List<CustomBotEntry>>(json);
                _customBots = entries ?? new List<CustomBotEntry>();
                logger.Trace($"{core.logCommonMessage}, BotDetectionService loaded {_customBots.Count} custom bot patterns");
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}, BotDetectionService.loadCustomBots");
                _customBots = new List<CustomBotEntry>();
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// JSON model for entries in crawler-user-agents.json
        /// </summary>
        private class CrawlerEntry {
            public string pattern { get; set; }
        }
        //
        //====================================================================================================
        /// <summary>
        /// JSON model for entries in contensive-bots.json
        /// </summary>
        private class CustomBotEntry {
            public string pattern { get; set; }
            public string name { get; set; }
            public bool isRegex { get; set; }
        }
    }
}
