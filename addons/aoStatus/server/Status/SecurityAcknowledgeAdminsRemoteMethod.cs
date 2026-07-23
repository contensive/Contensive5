
using Contensive.BaseClasses;
using System;
//
namespace Contensive.Addons.Status {
    /// <summary>
    /// Re-snapshots the current admin-user list, clearing any admins currently flagged as "new" in the
    /// /status endpoint's `security.newAdminUsers` field. Site Monitor roadmap Phase 1 -- mirrors the
    /// WordPress Site-Status plugin's "Acknowledge current admins" settings-page button.
    ///
    /// Requires an authenticated site administrator (same guard as AuthStatusClass).
    /// </summary>
    public class SecurityAcknowledgeAdminsRemoteMethod : AddonBaseClass {
        //
        //====================================================================================================
        public override object Execute(CPBaseClass cp) {
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
