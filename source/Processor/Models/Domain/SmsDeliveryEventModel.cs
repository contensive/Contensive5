using System.Collections.Generic;

namespace Contensive.Processor.Models.Domain {
    //
    //====================================================================================================
    /// <summary>
    /// AWS End User Messaging SMS event (configuration set event destination), the Message of an SNS Notification.
    /// Field names from the AWS event format (verify against a captured event). Unknown fields are ignored, missing fields are null.
    /// </summary>
    public class SmsDeliveryEventModel {
        /// <summary>for example TEXT_DELIVERED, TEXT_CARRIER_UNREACHABLE</summary>
        public string eventType { get; set; }
        /// <summary>the MessageId returned by SendTextMessage</summary>
        public string messageId { get; set; }
        /// <summary>the contact, E.164</summary>
        public string destinationPhoneNumber { get; set; }
        /// <summary>our number, E.164</summary>
        public string originationPhoneNumber { get; set; }
        /// <summary>for example DELIVERED</summary>
        public string messageStatus { get; set; }
        /// <summary>provider description of the status</summary>
        public string messageStatusDescription { get; set; }
        /// <summary>true if no further events are expected for this message</summary>
        public bool? isFinal { get; set; }
        /// <summary>total price of all message parts (USD), if reported</summary>
        public double? totalMessagePrice { get; set; }
        /// <summary>epoch milliseconds (kept as text so any format parses)</summary>
        public string eventTimestamp { get; set; }
        /// <summary>the Context sent with SendTextMessage. Core sends app (the app name) and log (the log row ccguid)</summary>
        public Dictionary<string, string> context { get; set; }
    }
}
