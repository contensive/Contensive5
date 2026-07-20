
using Amazon;
using Amazon.RDS;
using Amazon.RDS.Model;
using Contensive.BaseClasses;
using Contensive.BaseModels;
using System;
using System.Data;
using System.Linq;
//
namespace Contensive.Processor.Controllers {
    /// <summary>
    /// Backup/reliability checks for the /status endpoint (Site Monitor roadmap Phase 3, items 2-4: last
    /// successful backup date, backup size, backup-destination awareness). Called directly from
    /// StatusClass.BuildResponse(), the same direct-method-call pattern SecurityDiagnosticsController and
    /// PerformanceDiagnosticsController use (informational only -- does not affect status/statusOk).
    ///
    /// Contensive's database is SQL Server, deployed one of two ways per Jay (2026-07-19):
    /// - SQL Express directly on the app server, backed up to a local drive by an external scheduled
    ///   job/script (not SQL Agent -- Express doesn't include it). Whatever mechanism runs the backup, a
    ///   successful `BACKUP DATABASE` populates msdb.dbo.backupset regardless of what triggered it, so
    ///   that's queried directly rather than guessing a backup-file folder convention.
    /// - AWS RDS, backed up via RDS's own automated snapshots. These are taken at the storage layer, not
    ///   via a T-SQL BACKUP command, so they never appear in msdb.dbo.backupset -- checked via the RDS API
    ///   instead (DescribeDBSnapshots).
    ///
    /// Which one applies is derived from cp.ServerConfig.defaultDataSourceAddress (an RDS endpoint always
    /// ends in ".rds.amazonaws.com") -- no separate config flag needed. The RDS path reuses the same
    /// awsAccessKey/awsSecretAccessKey/awsRegionName already on ServerConfig for other AWS services
    /// (S3, Route53, etc.), and resolves the DB instance identifier by matching this site's connection
    /// endpoint against every RDS instance's own reported Endpoint.Address, rather than parsing the
    /// identifier out of the hostname (a custom/aliased endpoint wouldn't necessarily follow that pattern).
    /// </summary>
    public static class ReliabilityDiagnosticsController {
        //
        //====================================================================================================
        /// <summary>
        /// Build the `reliability` object for the /status JSON response.
        /// </summary>
        public static StatusResponseModel.StatusReliabilityModel GetReliabilityInfo(CPBaseClass cp) {
            try {
                string address = cp.ServerConfig.defaultDataSourceAddress ?? "";
                bool isRds = address.IndexOf(".rds.amazonaws.com", StringComparison.OrdinalIgnoreCase) >= 0;
                string error = "";
                DateTime? lastBackupDate;
                long? lastBackupSizeBytes = null;
                if (isRds) {
                    var snapshot = GetLatestRdsSnapshot(cp, address, ref error);
                    lastBackupDate = snapshot?.SnapshotCreateTime;
                } else {
                    lastBackupDate = GetLastLocalBackupDate(cp, out lastBackupSizeBytes, ref error);
                }
                return new StatusResponseModel.StatusReliabilityModel {
                    backupSource = isRds ? "rds" : "local",
                    lastBackupDate = lastBackupDate,
                    lastBackupSizeBytes = lastBackupSizeBytes,
                    backupCheckError = error
                };
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex, "ReliabilityDiagnosticsController.GetReliabilityInfo");
                return null;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Most recent full backup for this database, from SQL Server's own backup history table -- populated
        /// automatically by any successful BACKUP DATABASE, regardless of what triggered it (scheduled task,
        /// script, manual run). Scoped to DB_NAME() so a SQL Server instance hosting multiple sites' databases
        /// only reports this site's own backup history.
        /// </summary>
        private static DateTime? GetLastLocalBackupDate(CPBaseClass cp, out long? sizeBytes, ref string error) {
            sizeBytes = null;
            try {
                using (DataTable dt = cp.Db.ExecuteQuery(
                    "SELECT TOP 1 backup_finish_date, backup_size FROM msdb.dbo.backupset " +
                    "WHERE database_name = DB_NAME() AND type = 'D' ORDER BY backup_finish_date DESC")) {
                    if (dt == null || dt.Rows.Count == 0) {
                        // -- Genuinely "no backup taken yet" (e.g. a brand-new site) -- not a check failure, so
                        // -- error is left empty. A null lastBackupDate already conveys this to the dashboard.
                        return null;
                    }
                    DataRow row = dt.Rows[0];
                    if (row["backup_size"] != DBNull.Value) {
                        sizeBytes = Convert.ToInt64(Convert.ToDouble(row["backup_size"]));
                    }
                    return row["backup_finish_date"] != DBNull.Value ? Convert.ToDateTime(row["backup_finish_date"]) : (DateTime?)null;
                }
            } catch (Exception ex) {
                // -- Most likely cause: the connection's SQL login doesn't have permission to read msdb.
                error = "Could not query msdb.dbo.backupset -- " + ex.Message;
                return null;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Most recent automated snapshot for the RDS instance backing this site's database. Snapshot size in
        /// bytes isn't exposed by this API (only the source volume's provisioned storage, which isn't the same
        /// thing), so only the timestamp is used -- see StatusReliabilityModel.lastBackupSizeBytes.
        /// </summary>
        private static DBSnapshot GetLatestRdsSnapshot(CPBaseClass cp, string connectionAddress, ref string error) {
            try {
                string regionName = string.IsNullOrEmpty(cp.ServerConfig.awsRegionName) ? "us-east-1" : cp.ServerConfig.awsRegionName;
                RegionEndpoint region = RegionEndpoint.GetBySystemName(regionName);
                using (var rdsClient = new AmazonRDSClient(cp.ServerConfig.awsAccessKey, cp.ServerConfig.awsSecretAccessKey, region)) {
                    var instancesResponse = rdsClient.DescribeDBInstancesAsync(new DescribeDBInstancesRequest()).Result;
                    var matchedInstance = instancesResponse.DBInstances.FirstOrDefault(i =>
                        string.Equals(i.Endpoint?.Address, connectionAddress, StringComparison.OrdinalIgnoreCase));
                    if (matchedInstance == null) {
                        error = "Could not find an RDS instance matching this site's connection endpoint (" + connectionAddress + ").";
                        return null;
                    }
                    var snapshotsResponse = rdsClient.DescribeDBSnapshotsAsync(new DescribeDBSnapshotsRequest {
                        DBInstanceIdentifier = matchedInstance.DBInstanceIdentifier,
                        SnapshotType = "automated"
                    }).Result;
                    var latest = snapshotsResponse.DBSnapshots.OrderByDescending(s => s.SnapshotCreateTime).FirstOrDefault();
                    if (latest == null) {
                        // -- Instance found, it just has no automated snapshots yet (e.g. brand-new) -- not a
                        // -- check failure, unlike the "couldn't find the instance at all" case above.
                    }
                    return latest;
                }
            } catch (Exception ex) {
                error = "Could not reach the AWS RDS API -- " + ex.Message;
                return null;
            }
        }
    }
}
