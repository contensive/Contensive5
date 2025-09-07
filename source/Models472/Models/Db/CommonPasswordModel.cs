
namespace Contensive.Models.Db {
    /// <summary>
    /// List of common passwords
    /// </summary>
    public class CommonPasswordModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("Common Passwords", "cccommonpasswords", "default", true);
    }
}
