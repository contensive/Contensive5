
using Contensive.Processor.Controllers;
using Contensive.Processor.Models.Db;
using NLog;
using System;
using System.Data;

namespace Contensive.Processor.Addons.Housekeeping {
    /// <summary>
    /// Activity Log
    /// </summary>
    public static class FilenameTestClass {
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
                env.log("Housekeep, executeHourlyTasks, FilenameTestClass");
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
                env.log("Housekeep, executeDailyTasks");
                //
                // -- test all files for valid characters
                if (env.core.siteProperties.getBoolean("allow housekeep filename test", false)) {
                    foreach (TableFieldFilesModel file in ContentFieldModel.getTextFilenames(env.core)) {
                        string sql = $"select {file.field} from {file.table} where {file.field} is not null";
                        using DataTable dtFileNames = env.core.db.executeQuery(sql);
                        if (dtFileNames?.Rows == null || dtFileNames.Rows.Count <= 0) { continue; }
                        foreach (DataRow drFilename in dtFileNames.Rows) {
                            string pathFilename = GenericController.encodeText(drFilename[0]);
                            if (string.IsNullOrEmpty(pathFilename)) { continue; }
                            // -- unix test
                            pathFilename = FileController.convertToUnixSlash(pathFilename);
                            if (pathFilename != FileController.encodeUnixPathFilename(pathFilename)) {
                                LogController.logAlarm(env.core, $"Housekeep File Unix compatible Fieldname test failed. Invalid filename [{pathFilename}] in table [{file.table}], field [{file.field}]");
                            }
                            // -- dos test
                            pathFilename = FileController.convertToDosSlash(pathFilename);
                            if (pathFilename != FileController.encodeDosPathFilename(pathFilename)) {
                                LogController.logAlarm(env.core, $"Housekeep File DOS compatible Fieldname test failed. Invalid filename [{pathFilename}] in table [{file.table}], field [{file.field}]");
                            }
                        }
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