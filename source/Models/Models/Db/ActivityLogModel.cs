
namespace Contensive.Models.Db {
    /// <summary>
    /// Logs events related to user actvity
    /// </summary>
    public class ActivityLogModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("Activity Logs", "ccActivityLogs", "default", false);
        //
        //====================================================================================================
        /// <summary>
        /// The url of the event
        /// </summary>
        public string link { get; set; }
        /// <summary>
        /// the user who the event effects
        /// </summary>
        public int memberId { get; set; }
        /// <summary>
        /// a description of the event
        /// </summary>
        public string message { get; set; }
        /// <summary>
        /// the organization effected by the event
        /// </summary>
        public int organizationId { get; set; }
        /// <summary>
        /// The visit session in which the event occured
        /// </summary>
        public int visitId { get; set; }
        /// <summary>
        /// the visitor in effect when the event occured
        /// </summary>
        public int visitorId { get; set; }
    }
}
