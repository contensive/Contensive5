
using Contensive.BaseClasses;
using System;

namespace Contensive.Models.Db {
    /// <summary>
    /// List of common passwords
    /// </summary>
    public class UsedPasswordModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("Used Passwords", "ccUsedPasswords", "default", true);
        //
        public int memberId { get; set; }
        //
        /// <summary>
        /// return true if this password was used by this user within the last 30 days
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="password"></param>
        /// <param name="passwordHash"></param>
        /// <returns></returns>
        public static bool isUsedPassword(CPBaseClass cp, string password, string passwordHash) {
            if(string.IsNullOrEmpty(password) || string.IsNullOrEmpty(passwordHash)) {  return true; }
            return getCount<UsedPasswordModel>(cp, $"(dateadded>{cp.Db.EncodeSQLDate(DateTime.Now.AddDays(-30))})and(memberid={cp.User.Id})and((name={cp.Db.EncodeSQLText(password)})or(name={cp.Db.EncodeSQLText(passwordHash)}))") > 0;
        }
        //
        public static void saveUsedPassword(CPBaseClass cp, string valueToSave, int userId) {
            var usedPassword = DbBaseModel.addDefault<UsedPasswordModel>(cp, userId);
            usedPassword.name = valueToSave;
            usedPassword.memberId = userId;
            usedPassword.save(cp);


        }
    }
}
