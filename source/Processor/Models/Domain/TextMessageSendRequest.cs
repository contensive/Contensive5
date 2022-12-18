
namespace Contensive.Processor.Models.Domain {

    //
    //====================================================================================================        
    /// <summary>
    /// object for sms send
    /// </summary>
    public class TextMessageSendRequest {
        public int toMemberId { get; set; }
        public string toPhone { get; set; }
        public string textBody { get; set; }
        public int attempts { get; set; }
        public int systemTextMessageId { get; set; }
        public int groupTextMessageId { get; set; }
    }
}