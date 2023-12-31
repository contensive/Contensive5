
using Contensive.BaseClasses;
using System;
using System.Data;

namespace Contensive.Models.Db {
    /// <summary>
    /// Logs events related to user actvity
    /// </summary>
    public class AuthenticationLogModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("Authentication Log", "ccAuthenticationLog", "default", false);
        // 
        // ====================================================================================================
        // -- instance properties
        // 
        public bool success { get; set; }
        public int memberId { get; set; }
        //
        public static void log(CPBaseClass cp, int userid, bool success) {
            var log = DbBaseModel.addDefault<AuthenticationLogModel>(cp);
            log.memberId = userid;
            log.success = success;
            log.save(cp);
        }
        /// <summary>
        /// return true if login allowed. return false if policy fails and block login
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public static bool allowLoginForLockoutPolicy(CPBaseClass cp, int userId) {
            DataTable dt = cp.Db.ExecuteQuery($"select top 3 success from ccAuthenticationLog where memberid={userId} and dateadded<{cp.Db.EncodeSQLDate(DateTime.Now)} order by id desc");
            if (dt == null || dt.Rows.Count == 0) return true;
            // return false if 3 false in a row
            int failCnt = 0;
            foreach( DataRow dr in dt.Rows) {
                if (cp.Utils.EncodeBoolean(dr[0])) { return true; }
                failCnt++;
            }
            return failCnt < 3;
        }
    }
}
