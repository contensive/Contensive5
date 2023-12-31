
using Contensive.Processor.Controllers;
using System;
//
namespace Contensive.Processor.Addons.Login {
    //
    //====================================================================================================
    /// <summary>
    /// Execute the current login form. This is the default form, or another addon if configured.
    /// </summary>
    public class GetLoginFormClass : BaseClasses.AddonBaseClass {
        //
        //====================================================================================================
        /// <summary>
        /// Execute the current login form. This is the default form, or another addon if configured.
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public override object Execute(BaseClasses.CPBaseClass cp) {
            try {
                bool requirePassword = cp.Doc.GetBoolean("requirePassword", true);
                bool forceDefaultLogin = cp.Doc.GetBoolean("Force Default Login", false);
                return LoginController.getLoginPage(((CPClass)cp).core, forceDefaultLogin, requirePassword);
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                throw;
            }
        }
    }
}
