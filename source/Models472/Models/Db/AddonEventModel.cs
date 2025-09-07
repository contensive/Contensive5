
namespace Contensive.Models.Db {
    //
    public class AddonEventModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("Add-on Events", "ccAddonEvents", "default", false);
        //
        //====================================================================================================
        //
    }
}
