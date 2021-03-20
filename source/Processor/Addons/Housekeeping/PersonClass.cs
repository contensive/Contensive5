
using Contensive.Processor.Controllers;
using System;

namespace Contensive.Processor.Addons.Housekeeping {
    /// <summary>
    /// Housekeep this content
    /// </summary>
    public static class PersonClass {
        //
        //====================================================================================================
        /// <summary>
        /// execute hourly tasks
        /// </summary>
        /// <param name="core"></param>
        public static void executeHourlyTasks(CoreController core) {
            try {
                //
                LogController.logInfo(core, "Housekeep, executeHourlyTasks, Person");
                //
            } catch (Exception ex) {
                LogController.logError(core, ex);
                LogController.logAlarm(core, "Housekeep, exception, ex [" + ex + "]");
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
        public static void executeDailyTasks(CoreController core, HouseKeepEnvironmentModel env) {
            try {
                //
                // -- if eccommerce is installed, create sqlCriteria suffix for accountid
                string accountIdSuffix = "";
                if (core.db.isSQLTableField("ccmembers", "accountId")) {
                    accountIdSuffix += "and((m.accountid is null)or(m.accountId=0))";
                }
                //
                // -- calc archiveDate
                int localGuestArchiveDays = env.guestArchiveAgeDays;
                if (localGuestArchiveDays < 2) { localGuestArchiveDays = 2; }
                if (localGuestArchiveDays > 30) { localGuestArchiveDays = 30; }
                DateTime ArchiveDate = core.dateTimeNowMockable.AddDays(-localGuestArchiveDays).Date;
                string SQLTablePeople = MetadataController.getContentTablename(core, "People");
                string DeleteBeforeDateSQL = DbController.encodeSQLDate(ArchiveDate);
                {
                    //
                    // name repair
                    //
                    core.db.executeNonQuery("update ccmembers set name=firstname+' '+lastname  where ((name is null)or(name='guest'))and(firstname<>'Guest')and((firstname is not null)or(lastname is not null))");
                    core.db.executeNonQuery("update ccmembers set name=email  where ((name is null)or(name='guest'))and(email is not null)");
                    core.db.executeNonQuery("update ccmembers set name=username  where ((name is null)or(name='guest'))and(username is not null)");
                }
                //
                {
                    //
                    LogController.logInfo(core, "Housekeep, People-Daily, update createdByVisit, set null to 0, (pre v4.1.152)");
                    //
                    core.db.executeNonQuery("update ccmembers set CreatedByVisit=0 where createdbyvisit is null");
                }
                {
                    //
                    LogController.logInfo(core, "Housekeep, People-Daily, delete people from bot visits");
                    //
                    string sql = "delete from ccmembers from ccmembers m left join ccvisits v on v.memberid=m.id where (m.createdbyvisit=1)and(m.username is null)and(m.email is null)and(v.lastvisittime<DATEADD(hour, -" + localGuestArchiveDays + ", GETDATE()))and(v.bot>0)" + accountIdSuffix;
                    core.db.sqlCommandTimeout = 1800;
                    core.db.executeNonQuery(sql);
                }
                //
                {
                    //
                    LogController.logInfo(core, "Housekeep, People-Daily, delete guests -- people with createdByVisit=1, null username and a null email address.");
                    //
                    string sql = "delete from ccmembers from ccmembers m where (m.createdbyvisit=1) and(m.username is null) and(m.email is null)and(m.lastvisit<DATEADD(day, -" + localGuestArchiveDays + ", GETDATE()))" + accountIdSuffix;
                    core.db.sqlCommandTimeout = 1800;
                    core.db.executeNonQuery(sql);

                }
                {
                    //
                    LogController.logInfo(core, "Housekeep, People-Daily, mark all people allowbulkemail if their email address is in the emailbouncelist");
                    // 
                    string sql = "update ccmembers set allowbulkemail=0 from ccmembers m left join emailbouncelist b on b.name LIKE CONCAT('%', m.[email], '%') where b.id is not null and m.email is not null";
                    core.db.sqlCommandTimeout = 1800;
                    core.cpParent.Db.ExecuteNonQuery(sql);
                }
                {
                    //
                    LogController.logInfo(core, "Housekeep, delete people created by bots (visitor)");
                    // 
                    string sql = "delete from ccmembers from ccmembers u left join ccvisitors v on v.MemberID=u.id where v.bot=1";
                    core.db.sqlCommandTimeout = 1800;
                    core.cpParent.Db.ExecuteNonQuery(sql);
                    //
                }
                {
                    //
                    LogController.logInfo(core, "Housekeep, delete people created by bots (visits)");
                    // 
                    string sql = "delete from ccmembers from ccmembers u left join ccvisits v on v.MemberID=u.id where v.bot=1";
                    core.db.sqlCommandTimeout = 1800;
                    core.cpParent.Db.ExecuteNonQuery(sql);
                    //
                }
                //
            } catch (Exception ex) {
                LogController.logError(core, ex);
                LogController.logAlarm(core, "Housekeep, exception, ex [" + ex + "]");
                throw;
            }
        }
    }
}