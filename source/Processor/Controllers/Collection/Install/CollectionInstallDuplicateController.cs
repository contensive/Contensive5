
using Contensive.BaseClasses;
using Contensive.Models.Db;
using System;
using System.Collections.Generic;

namespace Contensive.Processor.Controllers {
    /// <summary>
    /// Shared utilities for cleaning up duplicate layout, template, and email template records during collection install.
    /// </summary>
    public static class CollectionInstallDuplicateController {
        //
        //====================================================================================================
        /// <summary>
        /// Find a record by name, preferring the newest (highest ID).
        /// This replaces DbBaseModel.createByUniqueName which returns the oldest (lowest ID).
        /// Returns null if no record matches.
        /// </summary>
        public static T findNewestByName<T>(CPBaseClass cp, string recordName) where T : DbBaseModel {
            if (string.IsNullOrEmpty(recordName)) { return null; }
            return DbBaseModel.createFirstOfList<T>(cp, $"(name={cp.Db.EncodeSQLText(recordName)})", "id desc");
        }
        //
        //====================================================================================================
        /// <summary>
        /// Given a target record (already resolved by GUID or name), query for all records of the same
        /// type with the same name and contentcontrolid, keep the target, and delete the duplicates.
        /// The contentcontrolid filter prevents cross-type deletion on shared tables (e.g. cctemplates
        /// holds both PageTemplateModel and EmailTemplateModel).
        /// </summary>
        public static T deleteDuplicatesByName<T>(CPBaseClass cp, T targetRecord) where T : DbBaseModel {
            if (targetRecord == null) { return null; }
            if (string.IsNullOrEmpty(targetRecord.name)) { return targetRecord; }
            if (string.Equals(targetRecord.name, "No Name", StringComparison.OrdinalIgnoreCase)) { return targetRecord; }
            try {
                //
                // -- look up the contentcontrolid for this model's content definition
                string contentName = DbBaseModel.derivedContentName(typeof(T));
                string tableName = DbBaseModel.derivedTableName(typeof(T));
                int contentId = 0;
                using (var dtContent = cp.Db.ExecuteQuery($"select id from cccontent where name={cp.Db.EncodeSQLText(contentName)}")) {
                    if (dtContent?.Rows != null && dtContent.Rows.Count > 0) {
                        contentId = cp.Utils.EncodeInteger(dtContent.Rows[0]["id"]);
                    }
                }
                //
                // -- build criteria: same name, and same contentcontrolid if available
                string nameCriteria = $"(name={cp.Db.EncodeSQLText(targetRecord.name)})";
                string criteria = (contentId > 0)
                    ? $"{nameCriteria}and(contentcontrolid={cp.Db.EncodeSQLNumber(contentId)})"
                    : nameCriteria;
                //
                // -- query all matching records ordered by id
                var allWithName = DbBaseModel.createList<T>(cp, criteria, "id");
                //
                // -- delete all except the target record
                foreach (var record in allWithName) {
                    if (record.id == targetRecord.id) { continue; }
                    logger.Info($"Deleting duplicate {typeof(T).Name} record id [{record.id}] name [{record.name}], keeping id [{targetRecord.id}]");
                    DbBaseModel.delete<T>(cp, record.id);
                }
            } catch (Exception ex) {
                logger.Error(ex, $"Error cleaning up duplicate {typeof(T).Name} records with name [{targetRecord.name}]");
            }
            return targetRecord;
        }
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    }
}
