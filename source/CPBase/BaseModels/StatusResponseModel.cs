
using System;
using System.Collections.Generic;

namespace Contensive.BaseModels {
    /// <summary>
    /// Model for the /status remote method JSON response.
    /// Use this to deserialize the JSON returned by the /status endpoint with format=json.
    /// </summary>
    public class StatusResponseModel {
        /// <summary>
        /// The Contensive version running on the server
        /// </summary>
        public string version { get; set; }
        /// <summary>
        /// Status result: "ok" or "error"
        /// </summary>
        public string status { get; set; }
        /// <summary>
        /// Human-readable status message
        /// </summary>
        public string message { get; set; }
        /// <summary>
        /// Performance metrics for the application
        /// </summary>
        public StatusMetricsModel metrics { get; set; }
        /// <summary>
        /// Windows update status, or null if unavailable
        /// </summary>
        public StatusWindowsUpdatesModel windowsUpdates { get; set; }
        /// <summary>
        /// Security and update-hygiene status (Site Monitor roadmap Phase 1), or null if unavailable.
        /// Not every field applies to every platform -- e.g. phpVersion is WordPress-only,
        /// outdatedAddonCollections is Contensive-only. Unused fields are simply omitted/null.
        /// </summary>
        public StatusSecurityModel security { get; set; }
        /// <summary>
        /// Availability/resource-headroom status (Site Monitor roadmap Phase 2), or null if unavailable.
        /// Distinct from <see cref="metrics"/>, which is self-reported request timing -- this object covers
        /// database size, memory headroom, caching, and cron/scheduler backlog. Not every field applies to
        /// every platform -- e.g. autoloadOptionsSizeKb is WordPress-only, dbSizeMb is set by both. Unused
        /// fields are simply left null, matching how Phase 1's security object handles platform-specific fields.
        /// </summary>
        public StatusPerformanceModel performance { get; set; }
        /// <summary>
        /// Backup/reliability status (Site Monitor roadmap Phase 3), or null if unavailable. Contensive only
        /// for now -- WordPress's equivalent backup-monitoring fields live in its own `reliability` object,
        /// built by the WordPress Site-Status plugin rather than this model.
        /// </summary>
        public StatusReliabilityModel reliability { get; set; }
        //
        /// <summary>
        /// Performance metrics included in the status response
        /// </summary>
        public class StatusMetricsModel {
            /// <summary>
            /// Average response time in milliseconds
            /// </summary>
            public long avgResponseTimeMs { get; set; }
            /// <summary>
            /// Average response time over the last 5 minutes in milliseconds
            /// </summary>
            public long avgResponseTime5MinMs { get; set; }
            /// <summary>
            /// Total hit count since startup
            /// </summary>
            public long hitCount { get; set; }
            /// <summary>
            /// Hit count over the last 5 minutes
            /// </summary>
            public long hitCount5Min { get; set; }
            /// <summary>
            /// Minutes since the application started
            /// </summary>
            public long uptimeMinutes { get; set; }
        }
        //
        /// <summary>
        /// Windows update status included in the status response
        /// </summary>
        public class StatusWindowsUpdatesModel {
            /// <summary>
            /// True if there are pending Windows updates
            /// </summary>
            public bool updatesAvailable { get; set; }
            /// <summary>
            /// Number of pending Windows updates
            /// </summary>
            public int updateCount { get; set; }
            /// <summary>
            /// Titles of pending Windows updates
            /// </summary>
            public List<string> updateTitles { get; set; }
            /// <summary>
            /// When the last Windows update check was performed
            /// </summary>
            public DateTime lastChecked { get; set; }
            /// <summary>
            /// Whether the last Windows update check completed successfully
            /// </summary>
            public bool checkSuccessful { get; set; }
            /// <summary>
            /// Error message if the Windows update check failed
            /// </summary>
            public string errorMessage { get; set; }
        }
        //
        /// <summary>
        /// Security and update-hygiene status included in the status response (Site Monitor roadmap Phase 1).
        /// Populated by the WordPress Site-Status plugin and/or the Contensive Status endpoint -- see each
        /// field's summary for which platform(s) set it.
        /// </summary>
        public class StatusSecurityModel {
            /// <summary>
            /// Installed PHP version. WordPress only; null for Contensive sites.
            /// </summary>
            public string phpVersion { get; set; }
            /// <summary>
            /// Minimum recommended PHP version. WordPress only; null for Contensive sites.
            /// </summary>
            public string phpMinRecommended { get; set; }
            /// <summary>
            /// Names of installed plugins with no update in longer than the configured threshold. WordPress only.
            /// </summary>
            public List<string> abandonedPlugins { get; set; }
            /// <summary>
            /// Names of installed themes with no update in longer than the configured threshold. WordPress only.
            /// </summary>
            public List<string> abandonedThemes { get; set; }
            /// <summary>
            /// Admin-level users detected since the last acknowledged snapshot. Set by both platforms.
            /// </summary>
            public List<string> newAdminUsers { get; set; }
            /// <summary>
            /// Count of failed login attempts within the rolling window described by failedLoginWindowHours.
            /// Set by both platforms.
            /// </summary>
            public int failedLoginCount { get; set; }
            /// <summary>
            /// Length, in hours, of the rolling window used for failedLoginCount. Set by both platforms.
            /// </summary>
            public int failedLoginWindowHours { get; set; }
            /// <summary>
            /// True if wp-config.php/.htaccess (WordPress) has changed since the last known-good hash. WordPress only; null for Contensive.
            /// </summary>
            public bool? configFilesModified { get; set; }
            /// <summary>
            /// True if core file checksums match the official release. WordPress only; null for Contensive
            /// (Contensive has no equivalent check yet -- see the Phase 1 implementation plan).
            /// </summary>
            public bool? coreIntegrityOk { get; set; }
            /// <summary>
            /// Count of core files that failed the checksum comparison. WordPress only; null for Contensive.
            /// </summary>
            public int? coreIntegrityMismatchCount { get; set; }
            /// <summary>
            /// Names of installed addon collections with a newer version available in the collection library. Contensive only.
            /// </summary>
            public List<string> outdatedAddonCollections { get; set; }
            /// <summary>
            /// True once a WPScan API token is configured (Site Monitor roadmap Phase 3, item 9: known CVE
            /// match on plugin/theme versions). WordPress only. False (with vulnerablePlugins/vulnerableThemes
            /// both empty) means "not checked yet" -- distinct from "checked, nothing found" -- since an empty
            /// token skips the check entirely rather than reporting a false-clean result.
            /// </summary>
            public bool? cveCheckEnabled { get; set; }
            /// <summary>
            /// Installed plugins with at least one known, not-yet-fixed-in-this-version vulnerability, per
            /// WPScan. WordPress only; empty for Contensive (no plugin/theme concept to match against a
            /// WordPress-specific vulnerability DB -- see the Phase 4 implementation plan).
            /// </summary>
            public List<StatusVulnerableItemModel> vulnerablePlugins { get; set; }
            /// <summary>
            /// Same as vulnerablePlugins, for installed themes. WordPress only.
            /// </summary>
            public List<StatusVulnerableItemModel> vulnerableThemes { get; set; }
        }
        //
        /// <summary>
        /// A single plugin or theme with at least one known vulnerability still applicable to its installed
        /// version (Site Monitor roadmap Phase 3, item 9). WordPress only.
        /// </summary>
        public class StatusVulnerableItemModel {
            public string name { get; set; }
            public string slug { get; set; }
            public string version { get; set; }
            public List<StatusVulnerabilityModel> vulnerabilities { get; set; }
        }
        //
        public class StatusVulnerabilityModel {
            public string title { get; set; }
            public string fixedIn { get; set; }
        }
        //
        /// <summary>
        /// Availability/resource-headroom status included in the status response (Site Monitor roadmap Phase 2).
        /// Mirrors the WordPress Site-Status plugin's performance checks, adapted to Contensive's data model.
        /// See each field's summary for which platform(s) set it.
        /// </summary>
        public class StatusPerformanceModel {
            /// <summary>
            /// Size, in megabytes, of the site's database. Set by both platforms -- for Contensive this is
            /// the sum of the allocated SQL Server data/log file sizes (sys.database_files), which matches
            /// what SSMS reports as the database size.
            /// </summary>
            public double? dbSizeMb { get; set; }
            /// <summary>
            /// Size, in kilobytes, of the WordPress "autoload" options blob (wp_options where autoload='yes').
            /// WordPress only; null for Contensive, which has no equivalent bulk-autoloaded options table.
            /// </summary>
            public double? autoloadOptionsSizeKb { get; set; }
            /// <summary>
            /// Count of rows in the WordPress autoloaded-options set. WordPress only; null for Contensive.
            /// </summary>
            public int? autoloadOptionsCount { get; set; }
            /// <summary>
            /// Configured memory limit, in bytes, for the running process (e.g. WordPress's WP_MEMORY_LIMIT).
            /// Null for Contensive: a long-lived IIS-hosted .NET process has no fixed script memory limit
            /// analogous to PHP's -- the closest concept, the app pool's private-memory recycling threshold,
            /// isn't reliably readable from inside the worker process, so it's left null rather than guessed.
            /// </summary>
            public long? memoryLimitBytes { get; set; }
            /// <summary>
            /// Current process memory usage, in bytes. Set by both platforms -- for Contensive this is the
            /// current process's working-set size (Process.GetCurrentProcess().WorkingSet64).
            /// </summary>
            public long? memoryUsageBytes { get; set; }
            /// <summary>
            /// memoryUsageBytes as a percentage of memoryLimitBytes. Null for Contensive since
            /// memoryLimitBytes is null there -- see its summary for why.
            /// </summary>
            public double? memoryUsagePercent { get; set; }
            /// <summary>
            /// True if an object cache (e.g. Redis/Memcached) is active. For Contensive, true if any of
            /// ServerConfig's enableRemoteCache / enableLocalMemoryCache / enableLocalFileCache flags is set --
            /// the same OR-gate Contensive's own CacheController (cp.Cache) uses internally to decide whether
            /// its get/store/invalidate calls are live or no-ops. See PerformanceDiagnosticsController.
            /// </summary>
            public bool? objectCacheActive { get; set; }
            /// <summary>
            /// True if a page cache (full-response/HTML output cache) is active. WordPress only; null for
            /// Contensive, which has no such mechanism -- its only cache (CacheController / cp.Cache) stores
            /// arbitrary app-level objects (records, dictionaries, addon lists, etc.), never whole rendered
            /// responses, so there is nothing analogous to a WordPress page-cache plugin to report here.
            /// </summary>
            public bool? pageCacheActive { get; set; }
            /// <summary>
            /// True if scheduled work (WordPress wp-cron) is backlogged. WordPress only; null for Contensive,
            /// whose scheduled work runs through a server-controlled process-addon scheduler (see
            /// taskSchedulerController) rather than a visitor-triggered cron hook, so it doesn't have the
            /// same silent-backlog failure mode that wp-cron does.
            /// </summary>
            public bool? cronBacklogged { get; set; }
        }
        //
        /// <summary>
        /// Backup/reliability status included in the status response (Site Monitor roadmap Phase 3, items 2-4:
        /// last successful backup date, backup size, backup-destination awareness). Contensive's database is
        /// SQL Server, hosted either as SQL Express directly on the app server (backed up to a local drive by
        /// an external scheduled job/script) or as AWS RDS (backed up via RDS's own automated snapshots) --
        /// backupSource reports which one this site is using, and the other fields are populated accordingly.
        /// See ReliabilityDiagnosticsController for how each source is actually checked.
        /// </summary>
        public class StatusReliabilityModel {
            /// <summary>
            /// Which backup mechanism this site's database uses: "local" (SQL Express, backed up to a local
            /// drive; checked via msdb.dbo.backupset) or "rds" (AWS RDS automated snapshots; checked via the
            /// RDS API). Contensive only; null for WordPress, which reports backupPluginDetected instead.
            /// </summary>
            public string backupSource { get; set; }
            /// <summary>
            /// When the most recent successful backup completed. Set by both platforms: for Contensive, the
            /// latest full backup in msdb.dbo.backupset ("local") or the latest automated snapshot's creation
            /// time ("rds"); for WordPress, the most recent backup set in the detected backup plugin's history
            /// (UpdraftPlus first -- see backupPluginDetected). Null if no backup has been found yet, or if
            /// the check itself failed/wasn't run.
            /// </summary>
            public DateTime? lastBackupDate { get; set; }
            /// <summary>
            /// Size, in bytes, of the most recent backup. Set by both platforms when determinable: for
            /// Contensive, only for backupSource "local" (msdb.dbo.backupset reports an exact size; "rds" is
            /// left null since the API doesn't expose an actual snapshot byte size). For WordPress, only when
            /// the backup plugin's files still exist in its local folder (null if shipped off-site only).
            /// </summary>
            public long? lastBackupSizeBytes { get; set; }
            /// <summary>
            /// Non-empty if the backup check itself failed (e.g. couldn't query msdb, or couldn't reach the RDS
            /// API/couldn't match this site's connection endpoint to an RDS instance). Contensive only; distinct
            /// from a null lastBackupDate that means "checked successfully, but no backup found yet".
            /// </summary>
            public string backupCheckError { get; set; }
            /// <summary>
            /// Name of the detected backup plugin (e.g. "UpdraftPlus"), or null if none is active. WordPress only.
            /// </summary>
            public string backupPluginDetected { get; set; }
            /// <summary>
            /// Count of PHP fatal errors found in debug.log within scanWindowHours. WordPress only.
            /// </summary>
            public int? fatalErrorCount { get; set; }
            /// <summary>
            /// Count of PHP warnings found in debug.log within scanWindowHours. WordPress only.
            /// </summary>
            public int? warningCount { get; set; }
            /// <summary>
            /// Count of PHP notices found in debug.log within scanWindowHours. WordPress only.
            /// </summary>
            public int? noticeCount { get; set; }
            /// <summary>
            /// Count of PHP deprecation notices found in debug.log within scanWindowHours. WordPress only.
            /// </summary>
            public int? deprecatedCount { get; set; }
            /// <summary>
            /// The most recent fatal error's message, or null if none found in the scan window. WordPress only.
            /// </summary>
            public string lastFatalErrorMessage { get; set; }
            /// <summary>
            /// When the most recent fatal error occurred, or null if none found in the scan window. WordPress only.
            /// </summary>
            public DateTime? lastFatalErrorTime { get; set; }
            /// <summary>
            /// How many hours back the debug.log scan covers. WordPress only.
            /// </summary>
            public int? scanWindowHours { get; set; }
        }
    }
}
