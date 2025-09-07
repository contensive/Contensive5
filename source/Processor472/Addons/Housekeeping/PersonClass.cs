
using Contensive.Processor.Controllers;
using NLog;
using System;

namespace Contensive.Processor.Addons.Housekeeping {
    /// <summary>
    /// Housekeep this content
    /// </summary>
    public static class PersonClass {
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
                env.log("Housekeep, executeHourlyTasks, Person");
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
                // -- if eccommerce is installed, create sqlCriteria suffix for accountid
                string accountIdSuffix = "";
                if (env.core.db.isSQLTableField("ccmembers", "accountId")) {
                    accountIdSuffix += "and((m.accountid is null)or(m.accountId=0))";
                }
                //
                // -- calc archiveDate
                int localGuestArchiveDays = env.guestArchiveAgeDays;
                if (localGuestArchiveDays < 2) { localGuestArchiveDays = 2; }
                if (localGuestArchiveDays > 30) { localGuestArchiveDays = 30; }
                DateTime ArchiveDate = env.core.dateTimeNowMockable.AddDays(-localGuestArchiveDays).Date;
                string SQLTablePeople = MetadataController.getContentTablename(env.core, "People");
                string DeleteBeforeDateSQL = DbController.encodeSQLDate(ArchiveDate);
                {
                    //
                    env.log("Housekeep, People-Daily, name repair");
                    //
                    env.core.db.executeNonQuery("update ccmembers set name=SUBSTRING(firstname+' '+lastname, 1, 100)  where ((name is null)or(name='guest'))and(firstname<>'Guest')and((firstname is not null)or(lastname is not null))");
                    env.core.db.executeNonQuery("update ccmembers set name=SUBSTRING(email, 1, 100)  where ((name is null)or(name='guest'))and(email is not null)");
                    env.core.db.executeNonQuery("update ccmembers set name=SUBSTRING(username, 1, 100)  where ((name is null)or(name='guest'))and(username is not null)");
                }
                //
                {
                    //
                    env.log("Housekeep, People-Daily, set createdByVisit 0 where null");
                    //
                    env.core.db.executeNonQuery("update ccmembers set CreatedByVisit=0 where createdbyvisit is null");
                }
                {
                    //
                    env.log("Housekeep, delete people created by bots (visitor)");
                    // 
                    string sql = "delete from ccmembers from ccmembers u left join ccvisitors v on v.MemberID=u.id where (u.createdbyvisit=1)and(v.bot=1)";
                    env.core.db.sqlCommandTimeout = 1800;
                    env.core.cpParent.Db.ExecuteNonQuery(sql);
                    //
                }
                {
                    //
                    env.log("Housekeep, delete people created by bots (visits)");
                    // 
                    string sql = "delete from ccmembers from ccmembers u left join ccvisits v on v.MemberID=u.id where (u.createdbyvisit=1)and(v.bot=1)";
                    env.core.db.sqlCommandTimeout = 1800;
                    env.core.cpParent.Db.ExecuteNonQuery(sql);
                    //
                }
                {
                    //
                    env.log($"Housekeep, People-Daily, delete guests -- createdByVisit=1, username=null, email=null, name='Guest', firstname='Guest', lastvisit< {localGuestArchiveDays} days ago, and ecommerce-criteria [{accountIdSuffix}].");
                    // 
                    int recordsAffected = 0;
                    int cnt = 0;
                    do {
                        env.core.db.sqlCommandTimeout = 180;
                        env.core.db.executeNonQuery($"delete top (10000) from ccmembers from ccmembers m where (m.createdbyvisit=1) and(m.username is null) and(m.email is null) and (m.name='Guest') and (m.firstname='Guest') and(m.lastvisit<DATEADD(day, -{localGuestArchiveDays}, GETDATE())){accountIdSuffix}");
                        cnt++;
                    } while ((recordsAffected != 0) && (cnt < 100));
                }
                //
            } catch (Exception ex) {
                logger.Error(ex, $"{env.core.logCommonMessage}");
                LogController.logAlarm(env.core, "Housekeep, exception, ex [" + ex + "]");
                throw;
            }
        }
    }
}