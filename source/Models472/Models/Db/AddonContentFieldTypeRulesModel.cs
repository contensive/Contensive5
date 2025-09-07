
using System;
//
namespace Contensive.Models.Db {
    public class AddonContentFieldTypeRulesModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("add-on Content Field Type Rules", "ccaddoncontentfieldtyperules", "default", false);
        //
        //====================================================================================================
        /// <summary>
        /// field properties
        /// </summary>
        public int addonId { get; set; }
        public int contentFieldTypeId { get; set; }
    }
}
