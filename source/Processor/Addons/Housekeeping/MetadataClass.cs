
using Contensive.BaseClasses;
using Contensive.Processor.Controllers;
using NLog;
using System;
using System.Collections.Generic;

namespace Contensive.Processor.Addons.Housekeeping {
    /// <summary>
    /// Housekeep this content
    /// </summary>
    public static class MetadataClass {
        //
        // static logger
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        //
        //====================================================================================================
        /// <summary>
        /// execute hourly tasks
        /// </summary>
        /// <param name="core"></param>
        public static void executeHourlyTasks(HouseKeepEnvironmentModel env) {
            try {
                //
                env.log("Housekeep, executeHourlyTasks, MetaData");
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
        /// execute Daily Tasks
        /// </summary>
        /// <param name="core"></param>
        /// <param name="env"></param>
        public static void executeDailyTasks(HouseKeepEnvironmentModel env) {
            try {
                //
                env.log("Housekeep, metadata");
                //
                //
                // block duplicate redirect fields (match contentid+fieldtype+caption)
                //
                env.log("Inactivate duplicate redirect fields");
                int FieldContentId = 0;
                string FieldLast = null;
                string FieldNew = null;
                int FieldRecordId = 0;
                using (var csData = new CsModel(env.core)) {
                    csData.openSql("Select ID, ContentID, Type, Caption from ccFields where (active<>0)and(Type=" + (int)CPContentBaseClass.FieldTypeIdEnum.Redirect + ") Order By ContentID, Caption, ID");
                    FieldLast = "";
                    while (csData.ok()) {
                        FieldContentId = csData.getInteger("Contentid");
                        string FieldCaption = csData.getText("Caption");
                        FieldNew = FieldContentId + FieldCaption;
                        if (FieldNew == FieldLast) {
                            FieldRecordId = csData.getInteger("ID");
                            env.core.db.executeNonQuery("Update ccFields set active=0 where ID=" + FieldRecordId + ";");
                        }
                        FieldLast = FieldNew;
                        csData.goNext();
                    }
                }
                //
                // convert FieldTypeLongText + htmlContent to FieldTypeHTML
                env.log("convert FieldTypeLongText + htmlContent to FieldTypeHTML.");
                string sql = "update ccfields set type=" + (int)CPContentBaseClass.FieldTypeIdEnum.HTML + " where type=" + (int)CPContentBaseClass.FieldTypeIdEnum.LongText + " and ( htmlcontent<>0 )";
                env.core.db.executeNonQuery(sql);
                //
                // -- TextFile types with no controlling record
                //
                if (GenericController.getBoolean(env.core.siteProperties.getText("ArchiveAllowFileClean", "false"))) {
                    //
                    int DSType = env.core.db.getDataSourceType();
                    env.log("Content TextFile types with no controlling record.");
                    using var csData = new CsModel(env.core);
                    sql = "SELECT DISTINCT ccTables.Name as TableName, ccFields.Name as FieldName"
                        + " FROM (ccFields LEFT JOIN ccContent ON ccFields.ContentId = ccContent.ID) LEFT JOIN ccTables ON ccContent.ContentTableId = ccTables.ID"
                        + " Where (((ccFields.Type) = 10))"
                        + " ORDER BY ccTables.Name";
                    csData.openSql(sql);
                    while (csData.ok()) {
                        //
                        // Get all the files in this path, and check that the record exists with this in its field
                        //
                        string FieldName = csData.getText("FieldName");
                        string TableName = csData.getText("TableName");
                        string PathName = TableName + "\\" + FieldName;
                        List<CPFileSystemBaseClass.FileDetail> FileList = env.core.cdnFiles.getFileList(PathName);
                        if (FileList.Count > 0) {
                            env.core.db.executeNonQuery("CREATE INDEX temp" + FieldName + " ON " + TableName + " (" + FieldName + ")");
                            foreach (CPFileSystemBaseClass.FileDetail file in FileList) {
                                string Filename = file.Name;
                                string VirtualFileName = PathName + "\\" + Filename;
                                string VirtualLink = GenericController.strReplace(VirtualFileName, "\\", "/");
                                long FileSize = file.Size;
                                if (FileSize == 0) {
                                    sql = "update " + TableName + " set " + FieldName + "=null where (" + FieldName + "=" + DbController.encodeSQLText(VirtualFileName) + ")or(" + FieldName + "=" + DbController.encodeSQLText(VirtualLink) + ")";
                                    env.core.db.executeNonQuery(sql);
                                    env.core.cdnFiles.deleteFile(VirtualFileName);
                                } else {
                                    using var csTest = new CsModel(env.core); 
                                    sql = "SELECT ID FROM " + TableName + " WHERE (" + FieldName + "=" + DbController.encodeSQLText(VirtualFileName) + ")or(" + FieldName + "=" + DbController.encodeSQLText(VirtualLink) + ")";
                                    if (!csTest.openSql(sql)) {
                                        env.core.cdnFiles.deleteFile(VirtualFileName);
                                    }
                                }
                            }
                            if (DSType == 1) {
                                // access
                                sql = "Drop INDEX temp" + FieldName + " ON " + TableName;
                            } else if (DSType == 2) {
                                // sql server
                                sql = "DROP INDEX " + TableName + ".temp" + FieldName;
                            } else {
                                // mysql
                                sql = "ALTER TABLE " + TableName + " DROP INDEX temp" + FieldName;
                            }
                            env.core.db.executeNonQuery(sql);
                        }
                        csData.goNext();
                    }
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{env.core.logCommonMessage}");
                LogController.logAlarm(env.core, "Housekeep, exception, ex [" + ex + "]");
                throw;
            }
        }
    }
}