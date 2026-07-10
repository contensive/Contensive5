
using Contensive.BaseClasses;
using Contensive.BaseModels;
using System;
using System.Data;
using System.Diagnostics;
//
namespace Contensive.Processor.Controllers {
    /// <summary>
    /// Availability/resource-headroom checks for the /status endpoint (Site Monitor roadmap Phase 2).
    /// Mirrors the WordPress Site-Status plugin's performance checks, adapted to Contensive's data model.
    /// Like SecurityDiagnosticsController, these checks are informational only and do not affect the
    /// endpoint's overall status/statusOk -- that stays reserved for uptime. Called directly from
    /// StatusClass.BuildResponse(), the same way SecurityDiagnosticsController and PerformanceMetricsController
    /// are -- this is a direct method call, not a `diagnostic>0` addon, because the addon extensibility
    /// mechanism only feeds a pass/fail string into StatusClass's check loop and has no channel back into
    /// structured JSON fields.
    ///
    /// Deliberately not measured here (left null, see StatusResponseModel.StatusPerformanceModel for detail
    /// on each field): autoloadOptionsSizeKb/autoloadOptionsCount (WordPress-specific "autoload" options
    /// bloat has no Contensive analog), memoryLimitBytes/memoryUsagePercent (no fixed memory limit to compare
    /// against inside a long-lived IIS worker process), pageCacheActive (Contensive has no full-response/HTML
    /// output cache analogous to a WordPress page-cache plugin -- researched via CacheController and found to
    /// only cache arbitrary app-level objects, never whole rendered responses), and cronBacklogged (Contensive's
    /// scheduled work runs through a server-controlled process-addon scheduler, not a visitor-triggered cron
    /// hook, so it has no equivalent silent-backlog failure mode).
    ///
    /// objectCacheActive IS measured: Contensive's Cache class (Contensive.Processor.Controllers.CacheController,
    /// exposed to addons as cp.Cache / CPCacheBaseClass) is a WordPress-object-cache analog -- an arbitrary
    /// key/value store (getObject/storeObject/invalidate) used by app code such as CacheRuntimeController's
    /// layout/link-alias/content dictionaries. It has three swappable backends (Redis via StackExchange.Redis,
    /// local in-process System.Runtime.Caching.MemoryCache, or local file), gated by the server config flags
    /// ServerConfigBaseModel.enableRemoteCache / enableLocalMemoryCache / enableLocalFileCache (mirrors
    /// CacheController's own private "allowCache" gate, which is the OR of those three flags). Those flags are
    /// reachable from a diagnostic check via cp.ServerConfig, so "is the object cache active" is answered the
    /// same way CacheController itself decides whether to read/write cache at all.
    ///
    /// Process uptime and per-request response timing already live in PerformanceMetricsController and are
    /// surfaced separately as the `metrics` key -- this controller does not duplicate that Stopwatch.
    /// </summary>
    public static class PerformanceDiagnosticsController {
        //
        //====================================================================================================
        /// <summary>
        /// Build the `performance` object for the /status JSON response.
        /// </summary>
        public static StatusResponseModel.StatusPerformanceModel GetPerformanceInfo(CPBaseClass cp) {
            try {
                return new StatusResponseModel.StatusPerformanceModel {
                    dbSizeMb = GetDatabaseSizeMb(cp),
                    autoloadOptionsSizeKb = null,
                    autoloadOptionsCount = null,
                    memoryLimitBytes = null,
                    memoryUsageBytes = GetProcessMemoryUsageBytes(cp),
                    memoryUsagePercent = null,
                    objectCacheActive = GetObjectCacheActive(cp),
                    pageCacheActive = null,
                    cronBacklogged = null
                };
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex, "PerformanceDiagnosticsController.GetPerformanceInfo");
                return null;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Size, in megabytes, of the current site's database, based on the sum of the allocated SQL Server
        /// data/log file sizes (sys.database_files.size, in 8KB pages). This matches the "Size" figure SSMS
        /// shows for a database, and is the closest SQL Server analog to the WordPress plugin's
        /// information_schema-based dbSizeMb. Queried through cp.Db.ExecuteQuery(), the same raw-SQL-query
        /// helper used throughout the admin/dashboard controllers (see DashboardViewModel, AdminNav, etc.),
        /// rather than opening a separate connection.
        /// Returns null (rather than throwing) if the query fails, so a DB metadata problem doesn't blank
        /// out the rest of the performance object.
        /// </summary>
        private static double? GetDatabaseSizeMb(CPBaseClass cp) {
            try {
                using (DataTable dt = cp.Db.ExecuteQuery("select cast(sum(size) * 8.0 / 1024.0 as decimal(18,2)) as sizeMb from sys.database_files")) {
                    if ((dt == null) || (dt.Rows.Count == 0) || (dt.Rows[0][0] == DBNull.Value)) { return null; }
                    return Convert.ToDouble(dt.Rows[0][0]);
                }
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex, "PerformanceDiagnosticsController.GetDatabaseSizeMb");
                return null;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Current process working-set memory, in bytes. This is the straightforward, reliable measure of
        /// process memory usage available from inside a long-lived IIS-hosted .NET process; there's no
        /// analog to a configured limit to compute a percentage against (see memoryLimitBytes' summary).
        /// </summary>
        private static long? GetProcessMemoryUsageBytes(CPBaseClass cp) {
            try {
                return Process.GetCurrentProcess().WorkingSet64;
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex, "PerformanceDiagnosticsController.GetProcessMemoryUsageBytes");
                return null;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// True if Contensive's object cache (cp.Cache / CacheController) is enabled for this site. Mirrors
        /// CacheController's own private "allowCache" gate: the cache is live (Redis, local memory, and/or
        /// local file backend) whenever any of ServerConfig's enableRemoteCache / enableLocalMemoryCache /
        /// enableLocalFileCache flags is set. If none are set, CacheController short-circuits every
        /// get/store/invalidate call into a no-op, i.e. the object cache exists in code but is inert.
        /// </summary>
        private static bool? GetObjectCacheActive(CPBaseClass cp) {
            try {
                return cp.ServerConfig.enableRemoteCache
                    || cp.ServerConfig.enableLocalMemoryCache
                    || cp.ServerConfig.enableLocalFileCache;
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex, "PerformanceDiagnosticsController.GetObjectCacheActive");
                return null;
            }
        }
    }
}
