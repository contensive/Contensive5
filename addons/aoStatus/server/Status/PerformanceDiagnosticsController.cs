
using Contensive.BaseClasses;
using Contensive.BaseModels;
using System;
using System.Data;
using System.Diagnostics;
//
namespace Contensive.Addons.Status {
    /// <summary>
    /// Availability/resource-headroom checks for the /status endpoint (Site Monitor roadmap Phase 2).
    /// Mirrors the WordPress Site-Status plugin's performance checks, adapted to Contensive's data model.
    /// Like SecurityDiagnosticsController, these checks are informational only and do not affect the
    /// endpoint's overall status/statusOk -- that stays reserved for uptime.
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
        /// data/log file sizes (sys.database_files.size, in 8KB pages).
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
        /// Current process working-set memory, in bytes.
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
        /// True if Contensive's object cache (cp.Cache / CacheController) is enabled for this site.
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
