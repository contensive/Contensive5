namespace Contensive.Processor.Models.Domain {
    //
    //====================================================================================================
    /// <summary>
    /// AWS End User Messaging SMS two-way (inbound) message, the Message of an SNS Notification.
    /// Field names from the AWS two-way format (verify against a captured message). Unknown fields are ignored, missing fields are null.
    /// </summary>
    public class SmsInboundMessageModel {
        /// <summary>the contact who sent the text</summary>
        public string originationNumber { get; set; }
        /// <summary>our number</summary>
        public string destinationNumber { get; set; }
        /// <summary>the text</summary>
        public string messageBody { get; set; }
        /// <summary>unique id of the inbound message. Used for idempotency</summary>
        public string inboundMessageId { get; set; }
        /// <summary>keyword reported by AWS, if any (semantics: verify)</summary>
        public string messageKeyword { get; set; }
        /// <summary>id of the last message published to the contact, if any (verify)</summary>
        public string previousPublishedMessageId { get; set; }
    }
}
