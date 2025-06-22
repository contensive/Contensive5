using Contensive.Models.Db;

namespace Contensive.Processor.Addons.PortalFramework.Models.Db {
    public class PortalFeatureModel : DbBaseModel {
        //
        public static readonly DbBaseTableMetadataModel tableMetadata = new DbBaseTableMetadataModel("Portal Features", "ccPortalFeatures", "default", false);
        //
        //====================================================================================================
        //
        // -- instance properties
        public int portalId { get; set; }
        public int addonId { get; set; }
        public string heading { get; set; }
        public int parentFeatureId { get; set; }
        public int dataContentId { get; set; }        
        //public bool addPadding { get; set; }
    }
}
