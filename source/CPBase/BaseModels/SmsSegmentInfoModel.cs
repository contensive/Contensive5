
namespace Contensive.BaseModels {
    /// <summary>
    /// The size of a text message body, as returned by cp.SMS.GetSegmentInfo().
    /// </summary>
    public class SmsSegmentInfoModel {
        /// <summary>
        /// GSM-7 units (extension characters count 2) or UTF-16 code units.
        /// </summary>
        public int characters { get; set; }
        /// <summary>
        /// The number of SMS segments.
        /// </summary>
        public int segments { get; set; }
        /// <summary>
        /// "GSM-7" or "UCS-2".
        /// </summary>
        public string encoding { get; set; } = "GSM-7";
    }
}
