
using Contensive.Processor.Controllers;
using System;

namespace Contensive.Processor.Addons.Housekeeping {
    /// <summary>
    /// Housekeep this content
    /// </summary>
    public static class VisitPropertyClass {
        //
        //====================================================================================================
        /// <summary>
        /// hourly housekeep tasks.
        /// </summary>
        /// <param name="core"></param>
        public static void executeHourlyTasks(HouseKeepEnvironmentModel env) {
            {
                try {
                    {
                        //
                        env.log("executeHourlyTasks, delete visit properties from  visits over 1 hour old");
                        //
                        string sql = "delete from ccproperties from ccproperties p left join ccvisits v on (v.id=p.keyid and p.typeid=1) where v.lastvisittime<dateadd(hour, -1, GETDATE())";
                        env.core.db.sqlCommandTimeout = 180;
                        env.core.db.executeNonQuery(sql);
                    }
                    {
                        //
                        env.log("executeHourlyTasks, delete visit properties without a visit");
                        //
                        string sql = "delete ccproperties from ccproperties left join ccvisits on ccvisits.id=ccproperties.keyid where (ccproperties.typeid=1) and (ccvisits.id is null)";
                        env.core.db.sqlCommandTimeout = 180;
                        env.core.db.executeNonQuery(sql);
                    }
                    {
                        //
                        env.log("delete all visit properties over 1 day without visit");
                        //
                        string sql = "delete from ccProperties where (TypeID=1)and(dateAdded<dateadd(hour, -24, getdate()))";
                        env.core.db.sqlCommandTimeout = 180;
                        env.core.db.executeNonQuery(sql);
                    }
                } catch (Exception ex) {
                    LogController.logError(env.core, ex);
                    LogController.logAlarm(env.core, "Housekeep, exception, ex [" + ex + "]");
                    throw;
                }
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// daily housekeep tasks
        /// </summary>
        /// <param name="core"></param>
        public static void executeDailyTasks(HouseKeepEnvironmentModel env) {
            try {
                //
                env.log("executeDailyTasks, visitproperites");
                //
                {
                    //
                    // Visit Properties with no visits
                    string sql = "delete ccproperties from ccproperties left join ccvisits on ccvisits.id=ccproperties.keyid where (ccproperties.typeid=1) and (ccvisits.id is null)";
                    env.core.db.sqlCommandTimeout = 180;
                    env.core.db.executeNonQuery(sql);
                }
                {
                    //
                    // delete all visit properties over 24 hours old
                    string sql = "delete from ccProperties where (TypeID=1)and(dateAdded<dateadd(hour, -24, getdate()))";
                    env.core.db.sqlCommandTimeout = 180;
                    env.core.db.executeNonQuery(sql);
                }
            } catch (Exception ex) {
                LogController.logError(env.core, ex);
                LogController.logAlarm(env.core, "Housekeep, exception, ex [" + ex + "]");
                throw;
            }
        }
        //
    }
}