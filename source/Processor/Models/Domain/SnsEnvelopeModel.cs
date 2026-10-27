namespace Contensive.Processor.Models.Domain {
    //
    //====================================================================================================
    /// <summary>
    /// The JSON document AWS SNS posts to an HTTPS subscription (Notification, SubscriptionConfirmation, UnsubscribeConfirmation).
    /// Property names match the SNS JSON keys. Missing keys are null.
    /// </summary>
    public class SnsEnvelopeModel {
        /// <summary>Notification, SubscriptionConfirmation or UnsubscribeConfirmation</summary>
        public string Type { get; set; }
        /// <summary>unique id of this SNS message. Used for idempotency</summary>
        public string MessageId { get; set; }
        /// <summary>arn:aws:sns:{region}:{account}:{name}. Must be in the site allow-list</summary>
        public string TopicArn { get; set; }
        /// <summary>optional, Notification only. Part of the string to sign only when present</summary>
        public string Subject { get; set; }
        /// <summary>the payload. For Notification from AWS End User Messaging this is a JSON document (delivery event or inbound message)</summary>
        public string Message { get; set; }
        /// <summary>ISO-8601 time SNS published the message</summary>
        public string Timestamp { get; set; }
        /// <summary>"1" (SHA1withRSA) or "2" (SHA256withRSA)</summary>
        public string SignatureVersion { get; set; }
        /// <summary>base64 RSA signature of the canonical string</summary>
        public string Signature { get; set; }
        /// <summary>https url of the PEM certificate that signed the message. Host must be sns.{region}.amazonaws.com</summary>
        public string SigningCertURL { get; set; }
        /// <summary>SubscriptionConfirmation/UnsubscribeConfirmation: the url to GET to confirm</summary>
        public string SubscribeURL { get; set; }
        /// <summary>SubscriptionConfirmation/UnsubscribeConfirmation: the confirmation token</summary>
        public string Token { get; set; }
        /// <summary>Notification: the url to unsubscribe. Never called by the site</summary>
        public string UnsubscribeURL { get; set; }
    }
}
