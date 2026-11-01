
namespace Contensive.BaseModels {
    /// <summary>
    /// A request to send one text message immediately. Used with cp.SMS.SendText().
    /// </summary>
    public class SmsSendRequestModel {
        /// <summary>
        /// The destination phone number in any format. Core normalizes it to E.164.
        /// </summary>
        public string toPhone { get; set; }
        /// <summary>
        /// The person the message is sent to. 0 if the recipient is not a person record.
        /// </summary>
        public int toMemberId { get; set; }
        /// <summary>
        /// The plain text message body, already merged by the caller.
        /// </summary>
        public string body { get; set; }
        /// <summary>
        /// The administrator who sent the message. 0 for the system.
        /// </summary>
        public int sentByMemberId { get; set; }
        /// <summary>
        /// A short label for the log, for example "CRM Send Text". Maximum 50 characters.
        /// </summary>
        public string source { get; set; }
        /// <summary>
        /// Optional. The id of the stock system text message used to create the body.
        /// </summary>
        public int systemTextMessageId { get; set; }
        /// <summary>
        /// If true, links to the site's own domains in the body are replaced with tracked links.
        /// </summary>
        public bool trackLinks { get; set; }
        /// <summary>
        /// If true, the message is a test. Tests are logged as tests and links are not tracked.
        /// </summary>
        public bool isTest { get; set; }
    }
}
