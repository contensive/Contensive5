﻿
using Contensive.Processor.Controllers;
using NLog;
using System;

namespace Contensive.Processor.Addons.Housekeeping {
    /// <summary>
    /// Housekeep this content
    /// </summary>
    public static class LinkAliasClass {
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
                env.log("Housekeep, executeHourlyTasks, LinkAlias");
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
                int recordsAffected = 0;
                int recordsAffectedTotal = 0;
                //
                env.log("Housekeep, linkalias");
                //
                // -- delete dups
                string sql = "delete from ccLinkAliases where id in ( select b.id from cclinkaliases a,cclinkaliases b where a.id<b.id and a.name=b.name )";
                env.core.db.executeNonQuery(sql, ref recordsAffected);
                recordsAffectedTotal += recordsAffected;
                //
                // -- delete blank name
                sql = "delete from ccLinkAliases where (name is null)or(name='')";
                env.core.db.executeNonQuery(sql, ref recordsAffected);
                recordsAffectedTotal += recordsAffected;
                //
                // -- delete missing or invalid page
                sql = "delete from ccLinkAliases from ccLinkAliases l left join ccpagecontent p on p.id=l.pageId where p.id is null";
                env.core.db.executeNonQuery(sql, ref recordsAffected);
                recordsAffectedTotal += recordsAffected;
                //
                // todo - this cache invalidation occurs in the task process. If the app is local cache, need to signal other processes (website) to invalidate also
                //
                if (recordsAffectedTotal > 0) { Contensive.Models.Db.DbBaseModel.invalidateCacheOfTable<Contensive.Models.Db.LinkAliasModel>(env.core.cpParent); }

            } catch (Exception ex) {
                logger.Error(ex, $"{env.core.logCommonMessage}");
            }

        }
    }
}