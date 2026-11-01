
namespace Contensive.Processor.Models.Domain {

    //
    //====================================================================================================        
    /// <summary>
    /// object for sms send. Serialized to json in the text message queue, so new properties must default to values that are safe for older queued rows.
    /// </summary>
    public class TextMessageSendRequest {
        public int toMemberId { get; set; }
        public string toPhone { get; set; }
        public string textBody { get; set; }
        public int attempts { get; set; }
        public int systemTextMessageId { get; set; }
        public int groupTextMessageId { get; set; }
        /// <summary>
        /// the administrator who sent the message. 0 for the system
        /// </summary>
        public int sentByMemberId { get; set; }
        /// <summary>
        /// short label for the log, for example "CRM Send Text". max 50 characters. blank is replaced with a default
        /// </summary>
        public string source { get; set; }
        /// <summary>
        /// if true, links to the site's own domains in the body are replaced with tracked links
        /// </summary>
        public bool trackLinks { get; set; }
        /// <summary>
        /// if true, the message is a test
        /// </summary>
        public bool isTest { get; set; }
    }
}
