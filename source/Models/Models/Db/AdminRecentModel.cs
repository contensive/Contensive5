
using Contensive.BaseClasses;

namespace Contensive.Models.Db {
    /// <summary>
    /// Addon Categories
    /// </summary>
    public class AdminRecentModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("admin recents", "ccadminrecents", "default", true);
        public int userId { get; set; }
        public string href { get; set; }
        public int contentId { get; set; }
        public int addonId { get; set; }
        //
        public static void insertAdminRecentContent(CPBaseClass cp, int userId, string name, string href, int contentId) {
            insertAdminRecent(cp, userId, name, href, AdminRecentTypeEnum.content, contentId,  0);
        }
        //
        public static void insertAdminRecentAddon(CPBaseClass cp, int userId, string name, string href, int addonId) {
            insertAdminRecent(cp, userId, name, href, AdminRecentTypeEnum.addon,0 , addonId);
        }
        //
        private static void insertAdminRecent(CPBaseClass cp, int userId, string name, string href, AdminRecentTypeEnum typeId, int contentId, int addonId) {
            string sqlUpdate = "update ccadminrecents set modifiedDate=" + cp.Db.EncodeSQLDate(System.DateTime.Now) + ",href=" + cp.Db.EncodeSQLText(href) + " where (userid=" + userId + ")and(name=" + cp.Db.EncodeSQLText(name) + ")";
            int recordsAffected = 0;
            cp.Db.ExecuteNonQuery(sqlUpdate, ref recordsAffected);
            if (recordsAffected > 0) { return; }
            //
            string sql = "insert into ccadminrecents "
                + " (userid,href,contentId,addonId,name,ccguid,active,contentControlId,createdBy,dateAdded,modifiedBy,modifiedDate,sortOrder)"
                + " values ("
                + userId + ","
                + cp.Db.EncodeSQLText(href) + ","
                + contentId + ","
                + addonId + ","
                + cp.Db.EncodeSQLText(name) + ","
                + cp.Db.EncodeSQLText(cp.Utils.CreateGuid()) + ","
                + "1,"
                + "0,"
                + userId + ","
                + cp.Db.EncodeSQLDate(System.DateTime.Now) + ","
                + userId + ","
                + cp.Db.EncodeSQLDate(System.DateTime.Now) + ","
                + "''"
                + ")";
            cp.Db.ExecuteNonQuery(sql);
            cp.Cache.InvalidateTable("ccadminrecents");
        }
        private enum AdminRecentTypeEnum {
            content = 1,
            addon = 2
        }
    }
}
