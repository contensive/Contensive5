
using Contensive.Processor.Controllers;
using Contensive.Processor.Models.Domain;
using System;

namespace Contensive.Processor.Addons.Housekeeping {
    /// <summary>
    /// Housekeep user properties
    /// </summary>
    public static class UserProperyClass {
        //
        //====================================================================================================
        /// <summary>
        /// execute hourly tasks
        /// </summary>
        /// <param name="core"></param>
        public static void executeHourlyTasks(HouseKeepEnvironmentModel env) {
            try {
                //
                env.log("Housekeep, executeHourlyTasks, UserProperties");
                //
            } catch (Exception ex) {
                LogControllerX.logError(env.core, ex);
                LogControllerX.logAlarm(env.core, "Housekeep, exception, ex [" + ex + "]");
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// daily housekeep. delete orphan user properties
        /// </summary>
        /// <param name="core"></param>
        public static void executeDailyTasks(HouseKeepEnvironmentModel env) {
            try {
                //
                env.log("Housekeep, userproperites");
                //
                string sql = "delete from ccProperties from ccProperties p left join ccmembers m on m.id=p.KeyID where (p.TypeID=" + (int)PropertyModelClass.PropertyTypeEnum.user + ") and (m.ID is null)";
                env.core.db.sqlCommandTimeout = 180;
                env.core.db.executeNonQuery(sql);
                //
                // Member Properties with no member
                //
                env.log("Deleting member properties with no member record.");
                sql = "delete ccproperties from ccproperties left join ccmembers on ccmembers.id=ccproperties.keyid where (ccproperties.typeid=0) and (ccmembers.id is null)";
                env.core.db.sqlCommandTimeout = 180;
                env.core.db.executeNonQuery(sql);

            } catch (Exception ex) {
                LogControllerX.logError(env.core, ex);
                LogControllerX.logAlarm(env.core, "Housekeep, exception, ex [" + ex + "]");
                throw;
            }
        }
        //
    }
}