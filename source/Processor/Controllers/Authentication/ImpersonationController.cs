
using Contensive.Models.Db;
using System;
using System.Collections.Generic;
using System.Linq;
//
namespace Contensive.Processor.Controllers {
    //
    //========================================================================
    /// <summary>
    /// methods to manager impersonation -- the process an administrator can use to take on another user's identity with just thier id or username
    /// </summary>
    public static class ImpersonationController {
        /// <summary>
        /// attempt impersonation. If true, it was successful. If false, the user-displayable reason in the the userError argument.
        /// </summary>
        /// <param name="core"></param>
        /// <param name="usernameOrId"></param>
        /// <param name="userError"></param>
        /// <returns></returns>
        public static bool tryImpersonate(CoreController core, string usernameOrId, ref string userError) {
            try {
                userError = "Impersonation failed because you do not have permission.";
                if (!core.session.isAuthenticatedAdmin()) { return false; }
                if (string.IsNullOrWhiteSpace(usernameOrId)) { return false; }
                //
                userError = "Impersonation failed because the requested credential did not select a valid, unique, active, non-admin user.";
                string sqlImpersonate;
                if (usernameOrId.Equals(GenericController.encodeInteger(usernameOrId).ToString()))
                    // 
                    // -- username is numeric, consider it an ID
                    sqlImpersonate = "(id=" + DbControllerX.encodeSQLNumber(GenericController.encodeInteger(usernameOrId)) + ")";
                else {
                    // 
                    // -- username is not numeric, consider it a username
                    string sqlUsername = DbControllerX.encodeSQLText(usernameOrId);
                    sqlImpersonate = "(username=" + sqlUsername + ")";
                }
                List<PersonModel> people = DbBaseModel.createList<PersonModel>(core.cpParent, sqlImpersonate);
                if (people.Count == 0) { return false; }
                if (people.Count > 1) { return false; }
                PersonModel person = people.First();
                //
                int userIdToRestore = core.session.user.id;
                if (!core.session.authenticateById(person.id, core.session)) {
                    // 
                    // -- login failed
                    userError = "Impersonation failed because the user with the requested credential could not be logged-in.";
                    core.session.authenticateById(userIdToRestore, core.session);
                    return false;
                }
                // 
                // -- impersonate user
                core.visitProperty.setProperty("adminImpersonation", userIdToRestore);
                return true;
            } catch (Exception ex) {
                LogControllerX.logError(core, ex);
                throw;
            }
        }
        //
        /// <summary>
        /// If an admin has impersonated a user, this method restores the admin's account 
        /// </summary>
        /// <param name="core"></param>
        /// <param name="usernameOrId"></param>
        /// <param name="userError"></param>
        /// <returns></returns>
        public static bool tryRestore(CoreController core, ref string userError) {
            try {
                userError = "Restore impersonation failed because you not currently authenticated. Instead login as your admin user.";
                if (!core.session.isAuthenticated) { return false; }
                //
                userError = "Restore impersonation failed. Instead login as your admin user.";
                int userId = core.visitProperty.getInteger("adminImpersonation");
                if (userId == 0) { return false; }
                PersonModel person = DbBaseModel.create<PersonModel>(core.cpParent, userId);
                if ((person == null)) {  return false; }
                //
                if (!core.session.authenticateById(userId, core.session)) { return false;  }
                core.visitProperty.clearProperty("adminImpersonation");
                return true;
            } catch (Exception ex) {
                LogControllerX.logError(core, ex);
                throw;
            }
        }
    }
}