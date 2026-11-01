
namespace Contensive.BaseModels {
    /// <summary>
    /// The outcome of a text message send.
    /// </summary>
    public enum SmsSendStatusEnum {
        /// <summary>The provider accepted the message</summary>
        Sent = 1,
        /// <summary>The message was queued for later send</summary>
        Queued = 2,
        /// <summary>Not sent because the number is blocked (block list, person opt-out, or provider opt-out)</summary>
        Blocked = 3,
        /// <summary>Not sent because the phone number is not valid</summary>
        InvalidPhone = 4,
        /// <summary>Not sent because text messaging is not configured or is turned off</summary>
        Disabled = 5,
        /// <summary>Not sent because the message is longer than the maximum segments allowed</summary>
        TooLong = 6,
        /// <summary>The send failed</summary>
        Failed = 7
    }
    /// <summary>
    /// The result returned by cp.SMS.SendText().
    /// </summary>
    public class SmsSendResultModel {
        /// <summary>
        /// True only if the status is Sent or Queued.
        /// </summary>
        public bool success { get; set; }
        /// <summary>
        /// The outcome of the send.
        /// </summary>
        public SmsSendStatusEnum status { get; set; }
        /// <summary>
        /// An administrator-readable message. Never contains secrets.
        /// </summary>
        public string userError { get; set; } = "";
        /// <summary>
        /// The id of the Text Message Log record for this send.
        /// </summary>
        public int textMessageLogId { get; set; }
        /// <summary>
        /// The id returned by the provider (AWS MessageId, Twilio SID). Blank if not sent.
        /// </summary>
        public string providerMessageId { get; set; } = "";
        /// <summary>
        /// The destination phone number in E.164 format. Blank if the phone was not valid.
        /// </summary>
        public string normalizedPhone { get; set; } = "";
        /// <summary>
        /// The number of SMS segments the message uses.
        /// </summary>
        public int segments { get; set; }
    }
}
