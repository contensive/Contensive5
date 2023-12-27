
using Contensive.BaseClasses;

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
        public static bool isUsedPasswordHash(CPBaseClass cp, string passwordHash) {
            return getCount<UsedPasswordModel>(cp, $"(memberid={cp.User.Id}) and (name={cp.Db.EncodeSQLText(passwordHash)})") > 0;
        }
    }
}
