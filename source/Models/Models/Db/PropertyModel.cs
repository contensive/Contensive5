
namespace Contensive.Models.Db {
    /// <summary>
    /// The underlying table that saves user, visit, and visitor properties.
    /// </summary>
    public class PropertyModel : DbBaseModel {
        ////
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("properties", "ccProperties", "default", true);
        //
        //====================================================================================================
        //
        /// <summary>
        /// The type of property. See PropertyTypeEnum. 0=user, 1=visit, 2=visitor
        /// </summary>
        public int TypeId { get; set; }
        //
        /// <summary>
        /// The id of the ccmembers, ccvisits, or ccvisitor table that represents this user, visit or visitor
        /// </summary>
        public int KeyID { get; set; }
        //
        /// <summary>
        /// A string representation of the actual data stored in this property for this user/visit/visitor
        /// </summary>
        public string fieldValue { get; set; } 
    }
}
