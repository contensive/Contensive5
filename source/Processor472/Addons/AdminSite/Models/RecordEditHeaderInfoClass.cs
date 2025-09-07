

using System;

namespace Contensive.Processor.Addons.AdminSite.Models {
    //
    //====================================================================================================
    /// <summary>
    /// structure used in admin edit forms at the top
    /// </summary>
    public class RecordEditHeaderInfoClass {
        public string recordName { get; set; }
        public int recordId { get; set; }
        public DateTime recordLockExpiresDate { get; set; }
        public int recordLockById { get; set; }
    }
}
