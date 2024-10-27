
using Contensive.Processor.Controllers;
using System;
//
namespace Contensive.Processor.Addons.Primitives {
    /// <summary>
    /// Process the form request from the default login form in Addons\Authentication
    /// </summary>
    public class ProcessLoginDefaultClass : Contensive.BaseClasses.AddonBaseClass {
        //
        //====================================================================================================
        /// <summary>
        /// process a username/password authentication with no success result.
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public override object Execute(Contensive.BaseClasses.CPBaseClass cp) {
            try {
                string requestUsername = cp.Doc.GetText("username");
                string requestPassword = cp.Doc.GetText("password");
                bool passwordRequestValid = cp.Doc.IsProperty("password");
                string UserErrorMessage = "";
                LoginWorkflowController.processLogin(((CPClass)cp).core, requestUsername, requestPassword, passwordRequestValid, ref UserErrorMessage);
                if(!string.IsNullOrEmpty(UserErrorMessage)) {
                    cp.UserError.Add(UserErrorMessage);
                }
                return "";
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                throw;
            }
        }
    }
}
