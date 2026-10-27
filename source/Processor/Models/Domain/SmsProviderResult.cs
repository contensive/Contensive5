
namespace Contensive.Processor.Models.Domain {
    //
    //====================================================================================================
    /// <summary>
    /// The result of handing one text message to a provider (AWS End User Messaging, Twilio). Provider controllers always return one of these and never throw.
    /// </summary>
    public class SmsProviderResult {
        /// <summary>
        /// true if the provider accepted the message
        /// </summary>
        public bool success { get; set; }
        /// <summary>
        /// the id returned by the provider (AWS MessageId, Twilio SID). blank if not accepted.
        /// </summary>
        public string providerMessageId { get; set; } = "";
        /// <summary>
        /// the provider error code or reason, blank on success
        /// </summary>
        public string errorCode { get; set; } = "";
        /// <summary>
        /// the provider error message, admin-readable, never contains secrets. blank on success
        /// </summary>
        public string errorMessage { get; set; } = "";
        /// <summary>
        /// true if the provider rejected the send because the recipient opted out
        /// </summary>
        public bool isOptedOut { get; set; }
        /// <summary>
        /// true if a later retry of the same send may succeed (throttling, provider outage)
        /// </summary>
        public bool isRetryable { get; set; }
        /// <summary>
        /// the provider used. 1=Twilio, 2=AWS. see TextMessageLogModel.ProviderEnum
        /// </summary>
        public int providerId { get; set; }
    }
}
