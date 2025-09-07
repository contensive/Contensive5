
using Contensive.BaseClasses;
using Contensive.Models.Db;
using Contensive.Processor.Controllers;
using NLog;
using System;

namespace Contensive.Processor.Addons.Housekeeping {
    /// <summary>
    /// Housekeep this content
    /// </summary>
    public class EmailBounceListClass {
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
                env.log("Housekeep, executeHourlyTasks, EmailBounceList");
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
                // -- populate email from name field
                foreach (var bounce in DbBaseModel.createList<EmailBounceListModel>(env.core.cpParent, "(name is not null) and (email is null)")) {
                    bounce.email = EmailController.getSimpleEmailFromFriendlyEmail(env.core.cpParent, bounce.name);
                    if (!string.IsNullOrEmpty(bounce.email)) {
                        bounce.save(env.core.cpParent);
                    }
                }
                //
                // -- restore allow-group-email for expiring transients, and remove the transients
                env.core.db.executeNonQuery("update ccmembers set AllowBulkEmail=1 from ccmembers m left join EmailBounceList b on b.email=m.email where b.transient>0 and b.transientfixdeadline<GETDATE()");
                env.core.db.executeNonQuery("delete from EmailBounceList  where transient>0 and transientfixdeadline<GETDATE()");
                //
                // -- clear people allow-buld-email for non-transient bounces
                env.core.cpParent.Db.ExecuteNonQuery("update ccmembers set email=null where email is not null and not ((email like '%@%')and(email like '%.%'))");
                env.core.cpParent.Db.ExecuteNonQuery("update ccmembers set allowbulkemail=0 from ccmembers m left join emailbouncelist b on b.name=m.[email] where b.id is not null and m.email is not null and b.transient=0");
                //
            } catch (Exception ex) {
                logger.Error(ex, $"{env.core.logCommonMessage}");
                LogController.logAlarm(env.core, "Housekeep, exception, ex [" + ex + "]");
                throw;
            }
        }
    }
}