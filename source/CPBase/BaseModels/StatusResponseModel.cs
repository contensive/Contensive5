
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
        }
    }
}
