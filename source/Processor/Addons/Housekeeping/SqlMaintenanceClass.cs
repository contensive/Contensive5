
using Contensive.Processor.Controllers;
using NLog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
//
namespace Contensive.Processor.Addons.Housekeeping {
    /// <summary>
    /// Daily SQL Server maintenance: drop unused indexes, create missing indexes,
    /// rebuild/reorganize fragmented indexes, update stale statistics.
    /// Only operates on tables registered in cctables (Contensive-managed).
    /// </summary>
    public static class SqlMaintenanceClass {
        //
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        //
        //====================================================================================================
        /// <summary>
        /// Run all SQL maintenance tasks. Each category is wrapped in its own try/catch
        /// so one failure does not block the others.
        /// </summary>
        public static void executeDailyTasks(HouseKeepEnvironmentModel env) {
            try {
                env.log("SqlMaintenance, start");
                //
                // -- build set of Contensive-managed table names (lowercase for case-insensitive comparison)
                var managedTables = getManagedTableNames(env);
                if (managedTables.Count == 0) {
                    env.log("SqlMaintenance, no managed tables found, skipping");
                    return;
                }
                env.log($"SqlMaintenance, found {managedTables.Count} managed tables");
                //
                // -- A. Drop unused indexes (only if SQL Server has been up >= 7 days)
                try {
                    dropUnusedIndexes(env, managedTables);
                } catch (Exception ex) {
                    logger.Error(ex, $"{env.core.logCommonMessage},SqlMaintenance, dropUnusedIndexes exception");
                }
                //
                // -- B. Create missing indexes
                try {
                    createMissingIndexes(env, managedTables);
                } catch (Exception ex) {
                    logger.Error(ex, $"{env.core.logCommonMessage},SqlMaintenance, createMissingIndexes exception");
                }
                //
                // -- C. Rebuild/reorganize fragmented indexes
                try {
                    defragmentIndexes(env, managedTables);
                } catch (Exception ex) {
                    logger.Error(ex, $"{env.core.logCommonMessage},SqlMaintenance, defragmentIndexes exception");
                }
                //
                // -- D. Update stale statistics
                try {
                    updateStaleStatistics(env, managedTables);
                } catch (Exception ex) {
                    logger.Error(ex, $"{env.core.logCommonMessage},SqlMaintenance, updateStaleStatistics exception");
                }
                //
                env.log("SqlMaintenance, done");
            } catch (Exception ex) {
                logger.Error(ex, $"{env.core.logCommonMessage},SqlMaintenance, unexpected exception");
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Get the set of table names registered in cctables (Contensive-managed), lowercased.
        /// </summary>
        private static HashSet<string> getManagedTableNames(HouseKeepEnvironmentModel env) {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (DataTable dt = env.core.db.executeQuery("select name from cctables where name is not null and name <> ''")) {
                foreach (DataRow row in dt.Rows) {
                    string name = row["name"]?.ToString()?.Trim() ?? "";
                    if (!string.IsNullOrEmpty(name)) {
                        result.Add(name);
                    }
                }
            }
            return result;
        }
        //
        //====================================================================================================
        /// <summary>
        /// Drop indexes that have zero reads since SQL Server started, but only if the instance
        /// has been running at least 7 days. Skips primary keys, unique constraints, clustered indexes,
        /// and auto-created indexes (IX_auto_ prefix) to avoid drop/create loops.
        /// </summary>
        private static void dropUnusedIndexes(HouseKeepEnvironmentModel env, HashSet<string> managedTables) {
            env.log("SqlMaintenance.dropUnusedIndexes, start");
            //
            // -- check SQL Server uptime, skip if < 7 days
            int uptimeDays = 0;
            using (DataTable dt = env.core.db.executeQuery("select datediff(day, sqlserver_start_time, getdate()) as uptime_days from sys.dm_os_sys_info")) {
                if (dt.Rows.Count > 0) {
                    uptimeDays = GenericController.getInteger(dt.Rows[0]["uptime_days"]);
                }
            }
            if (uptimeDays < 7) {
                env.log($"SqlMaintenance.dropUnusedIndexes, SQL Server uptime is {uptimeDays} days (< 7), skipping unused index cleanup");
                return;
            }
            env.log($"SqlMaintenance.dropUnusedIndexes, SQL Server uptime {uptimeDays} days, proceeding");
            //
            // -- query for unused indexes
            string sql = @"
                select
                    o.name as table_name,
                    i.name as index_name,
                    s.user_updates as write_count
                from sys.dm_db_index_usage_stats s
                inner join sys.indexes i on i.object_id = s.object_id and i.index_id = s.index_id
                inner join sys.objects o on o.object_id = s.object_id
                where s.database_id = db_id()
                    and o.type = 'U'
                    and i.type = 2
                    and i.is_primary_key = 0
                    and i.is_unique_constraint = 0
                    and i.is_unique = 0
                    and s.user_seeks = 0
                    and s.user_scans = 0
                    and s.user_lookups = 0
                    and i.name is not null
                    and i.name not like 'IX_auto_%'
                order by s.user_updates desc";
            int dropCount = 0;
            using (DataTable dt = env.core.db.executeQuery(sql)) {
                foreach (DataRow row in dt.Rows) {
                    string tableName = row["table_name"]?.ToString() ?? "";
                    string indexName = row["index_name"]?.ToString() ?? "";
                    int writeCount = GenericController.getInteger(row["write_count"]);
                    if (string.IsNullOrEmpty(tableName) || string.IsNullOrEmpty(indexName)) { continue; }
                    if (!managedTables.Contains(tableName)) { continue; }
                    //
                    // -- drop the unused index
                    env.log($"SqlMaintenance.dropUnusedIndexes, dropping [{indexName}] on [{tableName}], writes={writeCount}");
                    try {
                        env.core.db.executeNonQuery($"drop index [{indexName}] on [{tableName}]");
                        dropCount++;
                    } catch (Exception ex) {
                        env.log($"SqlMaintenance.dropUnusedIndexes, error dropping [{indexName}] on [{tableName}]: {ex.Message}");
                    }
                }
            }
            env.log($"SqlMaintenance.dropUnusedIndexes, done, dropped {dropCount} indexes");
        }
        //
        //====================================================================================================
        /// <summary>
        /// Create missing indexes identified by SQL Server's missing index DMVs.
        /// Only creates high-impact indexes (improvement measure > 10000), limited to top 10 per run.
        /// </summary>
        private static void createMissingIndexes(HouseKeepEnvironmentModel env, HashSet<string> managedTables) {
            env.log("SqlMaintenance.createMissingIndexes, start");
            //
            string sql = @"
                select top 10
                    d.statement as full_table_name,
                    object_name(d.object_id) as table_name,
                    d.equality_columns,
                    d.inequality_columns,
                    d.included_columns,
                    gs.avg_total_user_cost * gs.avg_user_impact * (gs.user_seeks + gs.user_scans) as improvement_measure,
                    gs.user_seeks,
                    gs.user_scans
                from sys.dm_db_missing_index_details d
                inner join sys.dm_db_missing_index_groups g on g.index_handle = d.index_handle
                inner join sys.dm_db_missing_index_group_stats gs on gs.group_handle = g.index_group_handle
                where d.database_id = db_id()
                    and gs.avg_total_user_cost * gs.avg_user_impact * (gs.user_seeks + gs.user_scans) > 10000
                order by improvement_measure desc";
            int createCount = 0;
            using (DataTable dt = env.core.db.executeQuery(sql)) {
                foreach (DataRow row in dt.Rows) {
                    string tableName = row["table_name"]?.ToString() ?? "";
                    string equalityCols = row["equality_columns"]?.ToString() ?? "";
                    string inequalityCols = row["inequality_columns"]?.ToString() ?? "";
                    string includeCols = row["included_columns"]?.ToString() ?? "";
                    double improvementMeasure = GenericController.getNumber(row["improvement_measure"]);
                    if (string.IsNullOrEmpty(tableName)) { continue; }
                    if (!managedTables.Contains(tableName)) { continue; }
                    //
                    // -- build the index column list: equality columns first, then inequality columns
                    string indexColumns = equalityCols;
                    if (!string.IsNullOrEmpty(inequalityCols)) {
                        indexColumns = string.IsNullOrEmpty(indexColumns) ? inequalityCols : $"{indexColumns}, {inequalityCols}";
                    }
                    if (string.IsNullOrEmpty(indexColumns)) { continue; }
                    //
                    // -- generate a deterministic index name from the column list
                    string colHash = getShortHash(indexColumns);
                    string indexName = $"IX_auto_{tableName}_{colHash}";
                    //
                    // -- build the CREATE INDEX statement
                    string includeClause = string.IsNullOrEmpty(includeCols) ? "" : $" include ({includeCols})";
                    string createSql = $"create nonclustered index [{indexName}] on [{tableName}] ({indexColumns}){includeClause}";
                    //
                    env.log($"SqlMaintenance.createMissingIndexes, creating [{indexName}] on [{tableName}], improvement={improvementMeasure:N0}");
                    try {
                        env.core.db.executeNonQuery(createSql);
                        createCount++;
                    } catch (Exception ex) {
                        env.log($"SqlMaintenance.createMissingIndexes, error creating [{indexName}] on [{tableName}]: {ex.Message}");
                    }
                }
            }
            env.log($"SqlMaintenance.createMissingIndexes, done, created {createCount} indexes");
        }
        //
        //====================================================================================================
        /// <summary>
        /// Rebuild indexes with > 30% fragmentation and > 1000 pages.
        /// Reorganize indexes with 10-30% fragmentation and > 1000 pages.
        /// Uses LIMITED mode for the fragmentation scan (fast, header-only sampling).
        /// </summary>
        private static void defragmentIndexes(HouseKeepEnvironmentModel env, HashSet<string> managedTables) {
            env.log("SqlMaintenance.defragmentIndexes, start");
            //
            // -- detect if Enterprise edition (supports ONLINE rebuild)
            bool isEnterprise = false;
            using (DataTable dt = env.core.db.executeQuery("select serverproperty('EngineEdition') as edition")) {
                if (dt.Rows.Count > 0) {
                    int edition = GenericController.getInteger(dt.Rows[0]["edition"]);
                    // 3 = Enterprise, Enterprise Evaluation, Developer
                    isEnterprise = (edition == 3);
                }
            }
            //
            string sql = @"
                select
                    object_name(ps.object_id) as table_name,
                    i.name as index_name,
                    ps.avg_fragmentation_in_percent,
                    ps.page_count
                from sys.dm_db_index_physical_stats(db_id(), null, null, null, 'LIMITED') ps
                inner join sys.indexes i on i.object_id = ps.object_id and i.index_id = ps.index_id
                where ps.index_id > 0
                    and ps.page_count > 1000
                    and ps.avg_fragmentation_in_percent > 10
                    and i.name is not null
                order by ps.avg_fragmentation_in_percent desc";
            int rebuildCount = 0;
            int reorganizeCount = 0;
            using (DataTable dt = env.core.db.executeQuery(sql)) {
                foreach (DataRow row in dt.Rows) {
                    string tableName = row["table_name"]?.ToString() ?? "";
                    string indexName = row["index_name"]?.ToString() ?? "";
                    double fragPercent = GenericController.getNumber(row["avg_fragmentation_in_percent"]);
                    long pageCount = GenericController.getInteger(row["page_count"]);
                    if (string.IsNullOrEmpty(tableName) || string.IsNullOrEmpty(indexName)) { continue; }
                    if (!managedTables.Contains(tableName)) { continue; }
                    //
                    if (fragPercent > 30) {
                        // -- rebuild
                        string onlineOption = isEnterprise ? " with (online = on)" : "";
                        string rebuildSql = $"alter index [{indexName}] on [{tableName}] rebuild{onlineOption}";
                        env.log($"SqlMaintenance.defragmentIndexes, rebuilding [{indexName}] on [{tableName}], frag={fragPercent:N1}%, pages={pageCount}");
                        try {
                            env.core.db.executeNonQuery(rebuildSql);
                            rebuildCount++;
                        } catch (Exception ex) {
                            env.log($"SqlMaintenance.defragmentIndexes, error rebuilding [{indexName}] on [{tableName}]: {ex.Message}");
                        }
                    } else {
                        // -- reorganize (10-30% fragmentation)
                        string reorgSql = $"alter index [{indexName}] on [{tableName}] reorganize";
                        env.log($"SqlMaintenance.defragmentIndexes, reorganizing [{indexName}] on [{tableName}], frag={fragPercent:N1}%, pages={pageCount}");
                        try {
                            env.core.db.executeNonQuery(reorgSql);
                            reorganizeCount++;
                        } catch (Exception ex) {
                            env.log($"SqlMaintenance.defragmentIndexes, error reorganizing [{indexName}] on [{tableName}]: {ex.Message}");
                        }
                    }
                }
            }
            env.log($"SqlMaintenance.defragmentIndexes, done, rebuilt {rebuildCount}, reorganized {reorganizeCount}");
        }
        //
        //====================================================================================================
        /// <summary>
        /// Update statistics where modification_counter exceeds 20% of total rows.
        /// Uses SAMPLE rate (not FULLSCAN) to be conservative on I/O.
        /// </summary>
        private static void updateStaleStatistics(HouseKeepEnvironmentModel env, HashSet<string> managedTables) {
            env.log("SqlMaintenance.updateStaleStatistics, start");
            //
            string sql = @"
                select
                    object_name(sp.object_id) as table_name,
                    sp.stats_id,
                    s.name as stat_name,
                    sp.rows,
                    sp.modification_counter
                from sys.stats s
                cross apply sys.dm_db_stats_properties(s.object_id, s.stats_id) sp
                inner join sys.objects o on o.object_id = s.object_id
                where o.type = 'U'
                    and sp.rows > 0
                    and sp.modification_counter > sp.rows * 0.20
                order by sp.modification_counter desc";
            int updateCount = 0;
            using (DataTable dt = env.core.db.executeQuery(sql)) {
                foreach (DataRow row in dt.Rows) {
                    string tableName = row["table_name"]?.ToString() ?? "";
                    string statName = row["stat_name"]?.ToString() ?? "";
                    long rows = GenericController.getInteger(row["rows"]);
                    long modCount = GenericController.getInteger(row["modification_counter"]);
                    if (string.IsNullOrEmpty(tableName) || string.IsNullOrEmpty(statName)) { continue; }
                    if (!managedTables.Contains(tableName)) { continue; }
                    //
                    env.log($"SqlMaintenance.updateStaleStatistics, updating [{statName}] on [{tableName}], rows={rows}, modifications={modCount}");
                    try {
                        env.core.db.executeNonQuery($"update statistics [{tableName}] ([{statName}])");
                        updateCount++;
                    } catch (Exception ex) {
                        env.log($"SqlMaintenance.updateStaleStatistics, error updating [{statName}] on [{tableName}]: {ex.Message}");
                    }
                }
            }
            env.log($"SqlMaintenance.updateStaleStatistics, done, updated {updateCount} statistics");
        }
        //
        //====================================================================================================
        /// <summary>
        /// Generate a short deterministic hash from a column list string, used to create
        /// unique but repeatable index names.
        /// </summary>
        private static string getShortHash(string input) {
            using (var sha = SHA256.Create()) {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(input.ToLowerInvariant()));
                return BitConverter.ToString(hash, 0, 4).Replace("-", "").ToLowerInvariant();
            }
        }
    }
}
