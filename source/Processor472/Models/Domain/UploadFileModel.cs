
namespace Contensive.Processor.Models.Domain {
    //
    public class UploadFileModel {
        //
        internal string filename { get; set; }
        //
        public int fileSize { get; set; }
        //
        public byte[] value { get; set; }
        //
        public string contentType { get; set; }
        //
        public bool isFile {
            get {
                if (!string.IsNullOrEmpty(filename)) { return true; }
                return false;
            }
        }
    }
}
