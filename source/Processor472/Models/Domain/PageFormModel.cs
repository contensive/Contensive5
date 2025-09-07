
using System.Collections.Generic;
//
namespace Contensive.Processor.Models.Domain {
    //
    internal class PageFormModel {
        internal string preRepeat { get; set; }
        internal string postRepeat { get; set; }
        internal string repeatCell { get; set; }
        internal string addGroupNameList { get; set; }
        internal bool authenticateOnFormProcess { get; set; }
        internal List<PageFormFieldModel> formFieldList = new List<PageFormFieldModel>();
    }
}