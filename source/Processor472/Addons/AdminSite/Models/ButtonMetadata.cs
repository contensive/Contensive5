
namespace Contensive.Processor.Addons.AdminSite.Models {
    public class ButtonMetadata {
        public string name { get; set; } = "button";
        public string value { get; set; } = "";
        public string classList { get; set; } = "";
        public bool isDelete { get; set; }
        public bool isClose { get; set; }
        public bool isAdd { get; set; }
    }
}
