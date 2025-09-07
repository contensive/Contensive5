
namespace Contensive.Models.Db {
    //
    public class StateModel : DbBaseModel {
        ////
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("states", "ccstates", "default", true);
        //
        //====================================================================================================
        public string abbreviation { get; set; }
        public int countryId { get; set; }
        public double salesTax { get; set; }
    }
}
