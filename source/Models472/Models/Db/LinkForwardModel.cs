
namespace Contensive.Models.Db {
    //
    public class LinkForwardModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("link forwards", "cclinkforwards", "default", true);
        //
        //====================================================================================================
        public string destinationLink { get; set; }
        public int groupId { get; set; }
        public string sourceLink { get; set; }
        public int viewings { get; set; }
    }
}
