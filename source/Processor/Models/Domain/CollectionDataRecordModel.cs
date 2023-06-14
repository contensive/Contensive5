
namespace Contensive.Processor.Models.Domain {
    /// <summary>
    /// A row in the Data-Record tab of a collection record. Represents one-or-more records to be exported from a content
    /// </summary>
    public class CollectionDataRecordModel {
        /// <summary>
        /// The content for this export. if recordGuid and recordName are empty, export all records
        /// </summary>
        public string contentName { get; set; }
        /// <summary>
        /// if not empty, the guid of the record to export
        /// </summary>
        public string recordGuid { get; set; }
        /// <summary>
        /// valid only if recordGuid is empty. if not empty, the record name to be exported
        /// </summary>
        public string recordName { get; set; }
    }
}
