
using Contensive.Processor.Controllers;
using Contensive.Processor.Models.Domain;
using System;

namespace Contensive.Processor.Addons.Housekeeping {
    /// <summary>
    /// Housekeep this content
    /// </summary>
    public static class VisitorPropertyClass {
        //
        //====================================================================================================
        /// <summary>
        /// execute hourly tasks
        /// </summary>
        /// <param name="core"></param>
        public static void executeHourlyTasks(HouseKeepEnvironmentModel env) {
            try {
                //
                env.log("Housekeep, executeHourlyTasks, VisitorProperties");
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
        /// delete orphan visitor properties
        /// </summary>
        /// <param name="core"></param>
        public static void executeDailyTasks(HouseKeepEnvironmentModel env) {
            try {
                //
                env.log("Housekeep, visitorproperties");
                //
                string sql = "delete from ccProperties from ccProperties p left join ccvisitors m on m.id=p.KeyID where (p.TypeID=" + (int)PropertyModelClass.PropertyTypeEnum.visitor + ") and (m.ID is null)";
                env.core.db.sqlCommandTimeout = 180;
                env.core.db.executeNonQuery(sql);
                //
                // Visitor Properties with no visitor
                //
                env.log("Deleting visitor properties with no visitor record.");
                sql = "delete ccProperties from ccProperties LEFT JOIN ccvisitors on ccvisitors.ID=ccProperties.KeyID where ccproperties.typeid=2 and ccvisitors.id is null";
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