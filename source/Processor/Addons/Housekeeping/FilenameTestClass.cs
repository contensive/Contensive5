
using Contensive.Processor.Controllers;
using Contensive.Processor.Models.Db;
using System;
using System.Data;

namespace Contensive.Processor.Addons.Housekeeping {
    /// <summary>
    /// Activity Log
    /// </summary>
    public static class FilenameTestClass {
        //
        //====================================================================================================
        /// <summary>
        /// execute hourly tasks
        /// </summary>
        /// <param name="core"></param>
        public static void executeHourlyTasks(HouseKeepEnvironmentModel env) {
            try {
                //
                env.log("Housekeep, executeHourlyTasks, FilenameTestClass");
                //
            } catch (Exception ex) {
                LogController.logError(env.core, ex);
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
                env.log("Housekeep, FilenameTestClass");
                //
                foreach (TableFieldFilesModel file in ContentFieldModel.getTextFilenames(env.core)) {
                    string sql = $"select {file.field} from {file.table} where {file.field} is not null";
                    using DataTable dtFileNames = env.core.db.executeQuery(sql);
                    if (dtFileNames?.Rows == null || dtFileNames.Rows.Count <= 0) { continue; }
                    foreach (DataRow drFilename in dtFileNames.Rows) {
                        string pathFilename = GenericController.encodeText(drFilename[0]);
                        if (string.IsNullOrEmpty(pathFilename)) { continue; }
                        // -- unix test
                        pathFilename = pathFilename.Replace("/", "\\");
                        if (pathFilename != FileController.encodeUnixPathFilename(pathFilename)) {
                            LogController.logAlarm(env.core, $"Housekeep File Unix compatible Fieldname test failed. Invalid filename [{pathFilename}] in table [{file.table}], field [{file.field}]");
                        }
                        // -- dos test
                        pathFilename = pathFilename.Replace("\\", "/");
                        if (pathFilename != FileController.encodeDosPathFilename(pathFilename)) {
                            LogController.logAlarm(env.core, $"Housekeep File DOS compatible Fieldname test failed. Invalid filename [{pathFilename}] in table [{file.table}], field [{file.field}]");
                        }
                    }
                }
            } catch (Exception ex) {
                LogController.logError(env.core, ex);
                LogController.logAlarm(env.core, "Housekeep, exception, ex [" + ex + "]");
                throw;

            }
        }
    }
}