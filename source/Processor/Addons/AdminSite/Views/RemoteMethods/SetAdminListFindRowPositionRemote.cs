
using System;
using Contensive.Processor.Controllers;

namespace Contensive.Processor.Addons.AdminSite {
    /// <summary>
    /// Remote method to persist the admin list find-row position preference (top or bottom).
    /// Saves to userProperty so the setting persists across visits and applies to all admin list screens.
    /// </summary>
    public class SetAdminListFindRowPositionRemote : Contensive.BaseClasses.AddonBaseClass {
        //
        public override object Execute(Contensive.BaseClasses.CPBaseClass cp) {
            try {
                if (!cp.User.IsAdmin) { return ""; }
                CoreController core = ((CPClass)cp).core;
                bool top = cp.Doc.GetBoolean("top");
                core.userProperty.setProperty("AdminListFindRowTop", top.ToString());
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                return "There was an error executing this addon.";
            }
            return "";
        }
    }
}
