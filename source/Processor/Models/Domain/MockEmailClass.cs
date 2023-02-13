
using Contensive.Processor.Controllers;
//
namespace Contensive.Processor.Models.Domain {
    public class MockEmailClass {
        public EmailSendRequest email { get; set; }
        public string AttachmentFilename { get; set; } = string.Empty;
    }

}