
namespace Contensive.Processor.Models.Domain {

    //
    //====================================================================================================        
    /// <summary>
    /// object for email send and queue serialization
    /// </summary>
    public class EmailSendRequest {
        public int toMemberId { get; set; }
        public string toAddress { get; set; }
        public string fromAddress { get; set; }
        public string bounceAddress { get; set; }
        public string replyToAddress { get; set; }
        public string subject { get; set; }
        public string textBody { get; set; }
        public string htmlBody { get; set; }
        public int attempts { get; set; }
        public int emailId { get; set; }
    }
}