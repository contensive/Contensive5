
namespace Contensive.Models.Db {
    public class CountryModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("Countries", "ccCountries", "default", false);
        //
        //====================================================================================================
        //
        public string abbreviation { get; set; }
        public bool domesticShipping  { get; set; }

    }
}
