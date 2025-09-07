
namespace Contensive.Models.Db {
    //
    public class AddonEventCatcherModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("Add-on Event Catchers", "ccAddonEventCatchers", "default", false);
        //
        //====================================================================================================
        //
        public int addonId { get; set; }
        public int eventId { get; set; }
    }
}
