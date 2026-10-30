
using Contensive.BaseClasses;
using System;

namespace Contensive.Models.Db {
    /// <summary>
    /// Log of text messages sent and received. One row per message.
    /// </summary>
    public class TextMessageLogModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("Text Message log", "ccTextMessagelog", "default", false);
        //
        //====================================================================================================
        /// <summary>
        /// values for statusId. Positional in the Status lookup list, never reorder.
        /// </summary>
        public enum StatusEnum {
            /// <summary>Row created, not yet handed to the provider</summary>
            Queued = 1,
            /// <summary>Accepted by the provider</summary>
            Sent = 2,
            /// <summary>Provider reported delivery to the handset</summary>
            Delivered = 3,
            /// <summary>Send failed, or the provider reported a delivery failure</summary>
            Failed = 4,
            /// <summary>Not sent because the number is on the block list or opted out</summary>
            Blocked = 5,
            /// <summary>Not sent for another reason (not configured, invalid phone, too long)</summary>
            NotSent = 6
        }
        //
        //====================================================================================================
        /// <summary>
        /// values for providerId. Positional in the Provider lookup list, same positions as the SMS Provider Id site setting.
        /// </summary>
        public enum ProviderEnum {
            /// <summary>No provider</summary>
            None = 0,
            /// <summary>Twilio</summary>
            Twilio = 1,
            /// <summary>AWS End User Messaging</summary>
            Aws = 2
        }
        //
        //====================================================================================================
        /// <summary>
        /// If set, this log entry is from a system text.
        /// </summary>
        public int systemTextMessageId { get; set; }
        /// <summary>
        /// If set, this log entry is from a group text.
        /// </summary>
        public int groupTextMessageId { get; set; }
        /// <summary>
        /// The person the message was sent to, or received from.
        /// </summary>
        public int memberId { get; set; }
        /// <summary>
        /// OK if successful, else the reason for the failure. Human-readable summary.
        /// </summary>
        public string sendStatus { get; set; }
        /// <summary>
        /// E.164 phone number. Outbound: the contact. Inbound: our number.
        /// </summary>
        public string toPhone { get; set; }
        /// <summary>
        /// The message body.
        /// </summary>
        public string body { get; set; }
        /// <summary>
        /// The provider used, see ProviderEnum.
        /// </summary>
        public int providerId { get; set; }
        /// <summary>
        /// The message id returned by the provider (AWS MessageId, Twilio SID), or the inbound message id.
        /// </summary>
        public string providerMessageId { get; set; }
        /// <summary>
        /// The delivery status, see StatusEnum.
        /// </summary>
        public int statusId { get; set; }
        /// <summary>
        /// The date and time of the last status change.
        /// </summary>
        public DateTime? statusDate { get; set; }
        /// <summary>
        /// The status or error code from the provider, for example TEXT_DELIVERED.
        /// </summary>
        public string providerStatusCode { get; set; }
        /// <summary>
        /// The provider description of the status, truncated to 255 characters.
        /// </summary>
        public string statusDetail { get; set; }
        /// <summary>
        /// The administrator who sent the message. 0 for system sends.
        /// </summary>
        public int sentByMemberId { get; set; }
        /// <summary>
        /// The number of SMS segments the body uses.
        /// </summary>
        public int segments { get; set; }
        /// <summary>
        /// The price reported by the provider in the delivery event, if available.
        /// </summary>
        public double price { get; set; }
        /// <summary>
        /// What sent the message, for example "CRM Send Text", "System Text Message", "Group Text Message", "Inbound", "cp.SMS.Send".
        /// </summary>
        public string source { get; set; }
        /// <summary>
        /// True for messages received from a contact.
        /// </summary>
        public bool isInbound { get; set; }
        /// <summary>
        /// Outbound: the origination number if known. Inbound: the phone number of the contact.
        /// </summary>
        public string fromPhone { get; set; }
        /// <summary>
        /// True if the message was a test send.
        /// </summary>
        public bool isTest { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// Get the log row for a provider message id. Returns null if not found or the id is blank.
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="providerMessageId"></param>
        /// <returns></returns>
        public static TextMessageLogModel getByProviderMessageId(CPBaseClass cp, string providerMessageId) {
            if (string.IsNullOrWhiteSpace(providerMessageId)) { return null; }
            return createFirstOfList<TextMessageLogModel>(cp, $"(providerMessageId={cp.Db.EncodeSQLText(providerMessageId)})", "id desc");
        }
        //
        //====================================================================================================
        /// <summary>
        /// Get the newest outbound log row to a phone number that is linked to a person. Returns null if none.
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="e164"></param>
        /// <returns></returns>
        public static TextMessageLogModel getLastOutboundToPhone(CPBaseClass cp, string e164) {
            if (string.IsNullOrWhiteSpace(e164)) { return null; }
            return createFirstOfList<TextMessageLogModel>(cp, $"(toPhone={cp.Db.EncodeSQLText(e164)})and((isInbound=0)or(isInbound is null))and(memberId>0)", "id desc");
        }
    }
}
