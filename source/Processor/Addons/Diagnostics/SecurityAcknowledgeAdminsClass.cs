
using Contensive.Processor.Controllers;
using System;
//
namespace Contensive.Processor.Addons.Diagnostics {
    /// <summary>
    /// Re-snapshots the current admin-user list, clearing any admins currently flagged as "new" in the
    /// /status endpoint's `security.newAdminUsers` field. Site Monitor roadmap Phase 1 -- mirrors the
    /// WordPress Site-Status plugin's "Acknowledge current admins" settings-page button.
    ///
    /// Requires an authenticated site administrator (same guard as AuthStatusClass). This is manual/browser
    /// use only for now -- wiring an "acknowledge admins" button into the Site Monitor dashboard (per the
    /// Phase 1 implementation plan) would need SiteMonitor to call this endpoint server-to-server, which
    /// needs its own auth mechanism (e.g. a shared API key) since there's no logged-in admin session in
    /// that context. That's a follow-up decision, not built here.
    /// </summary>
    public class SecurityAcknowledgeAdminsClass : Contensive.BaseClasses.AddonBaseClass {
        //
        //====================================================================================================
        public override object Execute(Contensive.BaseClasses.CPBaseClass cp) {
            try {
                if (!cp.User.IsAdmin) {
                    cp.Response.SetStatus("401 Unauthorized");
                    return "Authentication required.";
                }
                SecurityDiagnosticsController.AcknowledgeAdmins(cp);
                cp.Response.SetType("text/plain");
                return "ok, admin snapshot updated.";
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                return "ERROR, unexpected exception while acknowledging admins.";
            }
        }
    }
}
