
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
        /// Diagnostic detail text
        /// </summary>
        public string diagnostics { get; set; }
        /// <summary>
        /// Windows update status, or null if unavailable
        /// </summary>
        public StatusWindowsUpdatesModel windowsUpdates { get; set; }
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
    }
}
