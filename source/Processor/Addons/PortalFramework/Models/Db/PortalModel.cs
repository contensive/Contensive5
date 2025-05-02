using Contensive.Models.Db;

namespace Contensive.Processor.Addons.PortalFramework.Models.Db {
    public class PortalModel : DbBaseModel {
        //
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("Portals", "ccPortals", "default", false);
        //
        //====================================================================================================
        //
        // -- instance properties
        public string defaultConfigJson { get; set; }
        //
        public int defaultFeatureId { get; set; }
    }
}
