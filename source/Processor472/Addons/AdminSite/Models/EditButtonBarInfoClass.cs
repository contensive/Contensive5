

using Contensive.Processor.Controllers;
using System;

namespace Contensive.Processor.Addons.AdminSite.Models {
    //
    public class EditButtonBarInfoClass {
        public bool allowDelete { get; set; }
        public bool allowCancel { get; set; }
        public bool allowSave { get; set; }
        public bool allowAdd { get; set; }
        public bool allowActivate { get; set; }
        public bool allowSendTest { get; set; }
        public bool allowSend { get; set; }
        public bool hasChildRecords { get; set; }
        public bool isPageContent { get; set; }
        public bool allowMarkReviewed { get; set; }
        public bool allowRefresh { get; set; }
        public bool allowCreateDuplicate { get; set; }
        public bool allowDeactivate { get; set; }
        public int contentId { get; set; }
        //
        public EditButtonBarInfoClass(CoreController core, AdminDataModel adminData, bool allowDelete, bool allowRefresh, bool allowSave, bool allowAdd) {
            allowActivate = false;
            this.allowAdd = allowAdd && adminData.adminContent.allowAdd && adminData.editRecord.getAllowUserAdd();
            allowCancel = true;
            allowCreateDuplicate = allowAdd && (adminData.editRecord.id != 0);
            allowDeactivate = false;
            this.allowDelete = allowDelete && adminData.adminContent.allowDelete && adminData.editRecord.allowUserDelete;
            allowMarkReviewed = false;
            this.allowRefresh = allowRefresh;
            this.allowSave = allowSave && adminData.editRecord.allowUserSave;
            allowSend = false;
            allowSendTest = false;
            hasChildRecords = false;
            isPageContent = false;
            contentId = adminData.adminContent.id;
        }
    }
}
