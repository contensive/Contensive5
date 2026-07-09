
using Contensive.BaseClasses;
using Contensive.BaseModels;
using Contensive.Models.Db;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
//
namespace Contensive.Processor.Controllers {
    /// <summary>
    /// Security and update-hygiene checks for the /status endpoint (Site Monitor roadmap Phase 1).
    /// Mirrors the WordPress Site-Status plugin's class-diagnostics.php security checks, adapted to
    /// Contensive's data model. Unlike the pass/fail diagnostic addons run by StatusClass (`diagnostic>0`
    /// AddonModel rows), these checks are informational only and do not affect the endpoint's overall
    /// status/statusOk -- that stays reserved for uptime, per the Phase 1 implementation plan. Called
    /// directly from StatusClass.BuildResponse(), the same way PerformanceMetricsController is.
    ///
    /// Not implemented here: addon-collection version comparison ("core update available" equivalent).
    /// AddonCollectionModel (the installed-collection record, Contensive.Models.Db) has no installed-version
    /// field to compare against AddonCollectionLibraryModel's published version -- see the Phase 1 roadmap
    /// notes. Needs a follow-up decision on where installed version should be sourced from before this can
    /// be built; outdatedAddonCollections is left null until then.
    /// </summary>
    public static class SecurityDiagnosticsController {
        //
        /// <summary>
        /// Site property storing the last-acknowledged admin username/email list, as a JSON string array.
        /// </summary>
        private const string adminSnapshotPropertyName = "Security Admin Snapshot";
        //
        /// <summary>
        /// Site property storing the configured failed-login rolling-window length, in hours.
        /// </summary>
        private const string failedLoginWindowHoursPropertyName = "Failed Login Window Hours";
        //
        private const int defaultFailedLoginWindowHours = 1;
        //
        //====================================================================================================
        /// <summary>
        /// Build the `security` object for the /status JSON response.
        /// </summary>
        public static StatusResponseModel.StatusSecurityModel GetSecurityInfo(CPBaseClass cp) {
            try {
                int failedLoginWindowHours = cp.Site.GetInteger(failedLoginWindowHoursPropertyName, defaultFailedLoginWindowHours);
                if (failedLoginWindowHours < 1) { failedLoginWindowHours = defaultFailedLoginWindowHours; }
                return new StatusResponseModel.StatusSecurityModel {
                    newAdminUsers = GetNewAdminUsers(cp),
                    failedLoginCount = GetFailedLoginCount(cp, failedLoginWindowHours),
                    failedLoginWindowHours = failedLoginWindowHours,
                    outdatedAddonCollections = null
                };
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex, "SecurityDiagnosticsController.GetSecurityInfo");
                return null;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Admin-flagged people present now but not in the last acknowledged snapshot.
        /// First run establishes the baseline silently (nothing to report yet), matching the
        /// WordPress plugin's equivalent behavior in class-diagnostics.php.
        /// </summary>
        private static List<string> GetNewAdminUsers(CPBaseClass cp) {
            var currentAdmins = GetCurrentAdminLogins(cp);
            string snapshotJson = cp.Site.GetText(adminSnapshotPropertyName);
            if (string.IsNullOrEmpty(snapshotJson)) {
                cp.Site.SetProperty(adminSnapshotPropertyName, JsonConvert.SerializeObject(currentAdmins));
                return new List<string>();
            }
            List<string> snapshot;
            try {
                snapshot = JsonConvert.DeserializeObject<List<string>>(snapshotJson) ?? new List<string>();
            } catch (Exception) {
                snapshot = new List<string>();
            }
            return currentAdmins.Except(snapshot, StringComparer.OrdinalIgnoreCase).ToList();
        }
        //
        //====================================================================================================
        /// <summary>
        /// Re-snapshot the current admin list, clearing any currently-flagged new admins.
        /// Intended to be called from a per-site "acknowledge admins" action -- see the Phase 1
        /// implementation plan's note that the natural home for this is the Site Monitor dashboard's
        /// existing per-site controls (alongside "refresh now" and "pause monitoring").
        /// </summary>
        public static void AcknowledgeAdmins(CPBaseClass cp) {
            cp.Site.SetProperty(adminSnapshotPropertyName, JsonConvert.SerializeObject(GetCurrentAdminLogins(cp)));
        }
        //
        //====================================================================================================
        private static List<string> GetCurrentAdminLogins(CPBaseClass cp) {
            return DbBaseModel.createList<PersonModel>(cp, "(admin=1)")
                .Select(p => string.IsNullOrEmpty(p.email) ? p.username : p.email)
                .Where(login => !string.IsNullOrEmpty(login))
                .ToList();
        }
        //
        //====================================================================================================
        /// <summary>
        /// Count of failed login attempts (ccAuthenticationLog, success=0) within the rolling window.
        /// Captures all logins, not just admin-panel ones -- AuthenticationLogModel.log() is called from
        /// AuthController.cs for every login attempt, success or failure.
        /// </summary>
        private static int GetFailedLoginCount(CPBaseClass cp, int windowHours) {
            DateTime cutoff = DateTime.Now.AddHours(-windowHours);
            return DbBaseModel.getCount<AuthenticationLogModel>(cp, $"(success=0)and(dateadded>{cp.Db.EncodeSQLDate(cutoff)})");
        }
    }
}
