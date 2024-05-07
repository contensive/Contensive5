
using Contensive.Processor.Controllers;
using NLog;
using System;

namespace Contensive.Processor.Addons.Housekeeping {
    /// <summary>
    /// Housekeep this content
    /// </summary>
    public static class MemberRuleClass {
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
                env.log("Housekeep, executeHourlyTasks, MemberRules");
                //
            } catch (Exception ex) {
                logger.Error(ex, $"{env.core.logCommonMessage}");
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
                env.log("Housekeep, memberrules");
                //
                //
                // -- delete rows with invalid columns
                env.core.db.executeNonQuery("delete from ccMemberRules where groupid is null or memberid is null");
                //
                // MemberRules with bad MemberID
                //
                env.log("Deleting Member Rules with bad MemberID.");
                string sql = "delete ccmemberrules"
                    + " From ccmemberrules"
                    + " LEFT JOIN ccmembers on ccmembers.ID=ccmemberrules.memberId"
                    + " WHERE (ccmembers.ID is null)";
                env.core.db.executeNonQuery(sql);
                //
                // MemberRules with bad GroupID
                //
                env.log("Deleting Member Rules with bad GroupID.");
                sql = "delete ccmemberrules"
                    + " From ccmemberrules"
                    + " LEFT JOIN ccgroups on ccgroups.ID=ccmemberrules.GroupID"
                    + " WHERE (ccgroups.ID is null)";
                env.core.db.executeNonQuery(sql);
                //
                // -- delete duplicates (very slow query)
                sql = "delete from ccmemberrules where id in ("
                        + " select distinct b.id"
                        + " from ccmemberrules a, ccmemberrules b"
                        + " where ((a.memberid=b.memberid)and(a.groupid=b.groupid)and(a.id<b.id))"
                        + ")";
                env.core.db.executeNonQuery(sql);
            } catch (Exception ex) {
                logger.Error(ex, $"{env.core.logCommonMessage}");
            }
        }
    }
}