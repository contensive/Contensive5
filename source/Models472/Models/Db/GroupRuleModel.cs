
namespace Contensive.Models.Db {
    //
    public class GroupRuleModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("group rules", "ccgrouprules", "default", false);
        //
        //====================================================================================================
        public bool allowAdd { get; set; }
        public bool allowDelete { get; set; }        
        public int contentId { get; set; }
        public int groupId { get; set; }
    }
}
