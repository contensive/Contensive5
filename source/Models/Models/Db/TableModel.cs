
using Contensive.BaseClasses;

namespace Contensive.Models.Db {
    //
    /// <summary>
    /// Database table registry. Each record represents a physical database table.
    /// Table: cctables
    /// The name field (inherited from DbBaseModel) holds the actual SQL table name (e.g., "ccMembers", "crmLeadSources").
    ///
    /// Relationships:
    /// - dataSourceId -> DataSourceModel (ccdatasources.id) - which database this table lives in
    ///
    /// Referenced by:
    /// - ContentModel.contentTableId - content definitions that use this table
    ///
    /// To resolve a content definition's table name:
    ///   join cccontent c inner join cctables t on t.id = c.contenttableid, then use t.name
    /// </summary>
    public class TableModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("tables", "cctables", "default", true);
        //
        //====================================================================================================
        /// <summary>
        /// FK to DataSourceModel (ccdatasources.id) - the database this table belongs to
        /// </summary>
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
