
using Contensive.Processor.Controllers;
using System;

namespace Contensive.Processor.Addons.Housekeeping {
    /// <summary>
    /// Housekeep this content
    /// </summary>
    public static class RemoteQueryClass {
        //
        //====================================================================================================
        /// <summary>
        /// execute hourly tasks
        /// </summary>
        /// <param name="core"></param>
        public static void executeHourlyTasks(HouseKeepEnvironmentModel env) {
            try {
                //
                env.log("Housekeep, executeHourlyTasks, RemoteQuery");
                //
            } catch (Exception ex) {
                LogControllerX.logError(env.core, ex);
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
                string SQL = "";
                //
                // Remote Query Expiration
                //
                SQL = "delete from ccRemoteQueries where (DateExpires is not null)and(DateExpires<" + DbControllerX.encodeSQLDate(env.core.dateTimeNowMockable) + ")";
                env.core.db.executeNonQuery(SQL);
            } catch (Exception ex) {
                LogControllerX.logError(env.core, ex);
            }
        }
        //
    }
}