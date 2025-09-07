
using Contensive.BaseClasses;

namespace Contensive.Models.Db {
    //
    public class TableModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("tables", "cctables", "default", true);
        //
        //====================================================================================================
        public int dataSourceId { get; set; }
        //
        //====================================================================================================
        //
        public static TableModel createByContentName(CPBaseClass cp, string contentName) {
            var content = createByUniqueName<ContentModel>(cp, contentName);
            if (content != null) { return create<TableModel>(cp, content.contentTableId); }
            return null;
        }
    }
}
