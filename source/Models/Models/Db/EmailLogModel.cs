
using System;

namespace Contensive.Models.Db {
    /// <summary>
    /// Email log table (ccemaillog). Records each email event: sends, opens, clicks, bounces, and block requests.
    /// Body field is cleared by housekeeping after 7 days to manage storage.
    /// </summary>
    public class EmailLogModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("email log", "ccemaillog", "default", false);
        //
        //====================================================================================================
        /// <summary>
        /// Expiration date for a temporary email block. Currently unused/reserved.
        /// </summary>
        public DateTime? dateBlockExpires { get; set; }
        /// <summary>
        /// Foreign key to ccemaildrops. Identifies the group email drop campaign this log entry belongs to. 0 for immediate/ad-hoc sends.
        /// </summary>
        public int emailDropId { get; set; }
        /// <summary>
        /// Foreign key to ccemail (EmailModel). Identifies the email template used for this send.
        /// </summary>
        public int emailId { get; set; }
        /// <summary>
        /// The sender email address for this message.
        /// </summary>
        public string fromAddress { get; set; }
        /// <summary>
        /// The type of email event logged. Values defined in Constants:
        /// 1=Drop (sent via drop), 2=Open, 3=Click, 4=Bounce, 5=BlockRequest (unsubscribe), 6=ImmediateSend.
        /// </summary>
        public int logType { get; set; }
        /// <summary>
        /// Foreign key to ccmembers (PersonModel). The person who received or interacted with the email.
        /// </summary>
        public int memberId { get; set; }
        /// <summary>
        /// Outcome status of the send attempt. Values include "ok", "fail", retry messages, or error descriptions.
        /// </summary>
        public string sendStatus { get; set; }
        /// <summary>
        /// The email subject line.
        /// </summary>
        public string subject { get; set; }
        /// <summary>
        /// The recipient email address.
        /// </summary>
        public string toAddress { get; set; }
        /// <summary>
        /// Foreign key to ccvisits. The visit/session during which the email event occurred.
        /// </summary>
        public int visitId { get; set; }
        /// <summary>
        /// The HTML body of the email. Cleared to null by housekeeping after 7 days.
        /// </summary>
        public string body { get; set; }
    }
}
