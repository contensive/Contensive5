
using System;

namespace Contensive.Models.Db {
    //
    public class MemberRuleModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("member rules", "ccmemberrules", "default", false);
        //
        //====================================================================================================
        public DateTime? dateExpires { get; set; }
        public int groupId { get; set; }
        public int memberId { get; set; }
        public int groupRoleId { get; set; }
    }
}
