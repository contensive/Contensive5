
using Contensive.Models.Db;
using Contensive.Processor.Controllers;
using Contensive.Processor.Models.Domain;
using System;
//
namespace Contensive.Processor.Addons {
    //
    //====================================================================================================
    /// <summary>
    /// Remote method to set password. /set-password
    /// marked as html document
    /// </summary>
    public class SetPasswordRemote : Contensive.BaseClasses.AddonBaseClass {
        //
        //====================================================================================================
        /// <summary>
        /// process a username/id. If successful, redirect to source.
        /// Source is in querystring 'source', or if blank, go to origin
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public override object Execute(Contensive.BaseClasses.CPBaseClass cp) {
            try {
                CoreController core = ((CPClass)cp).core;
                //
                string passwordTokenKey = cp.Doc.GetText("passwordTokenKey");
                if (cp.Doc.GetText("button").ToLowerInvariant() == "cancel") {
                    //
                    // -- clear the setPassword token
                    PasswordTokenModel.clearVisitPasswordToken(cp, passwordTokenKey);
                    //
                    // -- handle cancel button
                    if (cp.Request.PathPage == cp.GetAppConfig().adminRoute) {
                        cp.Response.Redirect(cp.GetAppConfig().adminRoute + "?refresh");
                    } else {
                        cp.Response.Redirect("/");
                    }
                }
                if (cp.Doc.GetText("button").ToLowerInvariant() != "setpassword") {
                    //
                    // -- no button pressed
                    return HtmlController.form(core, cp.Mustache.Render(Properties.Resources.Layout_SetPassword, new setPasswordDataModel {
                        userErrorHtml = "",
                        passwordTokenKey = passwordTokenKey
                    }));
                }
                //
                // -- handle set password
                string userErrorMessage = "";
                string password = cp.Doc.GetText("password");
                string confirm = cp.Doc.GetText("confirm");
                if (password != confirm) {
                    //
                    // -- confirm fail, delay to discourage brute-force
                    userErrorMessage = "Password and password confirm do not match";
                    System.Threading.Thread.Sleep(3000);
                    return HtmlController.form(core, cp.Mustache.Render(Properties.Resources.Layout_SetPassword, new setPasswordDataModel {
                        userErrorHtml = userErrorMessage,
                        passwordTokenKey = passwordTokenKey
                    }));
                }
                //
                // -- if autoToken present, log out
                if (!string.IsNullOrEmpty(passwordTokenKey)) { cp.User.Logout(); }
                //
                PersonModel user = null;
                PasswordTokenModel passwordToken = PasswordTokenModel.getVisitPasswordTokenInfo(cp, passwordTokenKey);
                if (cp.User.IsAuthenticated) {
                    //
                    // -- user changing password
                    user = DbBaseModel.create<PersonModel>(cp, cp.User.Id);
                } else if (passwordToken != null && !string.IsNullOrEmpty(passwordToken.key)) {
                    //
                    // -- passwordTokenKey in link to site matches passwordTokenKey saved in visit when invitation was sent (email/sms) so this is the same visitor
                    // -- forgot-password process, this user-visit requested forgot-password link
                    user = DbBaseModel.create<PersonModel>(cp, passwordToken.userId);
                } else if (passwordToken.expires.CompareTo(DateTime.Now)<0 ) {
                    //
                    // -- passwordToken expired
                    userErrorMessage = "The time period for resetting your password has expired.";
                }
                if (user == null) {
                    return HtmlController.form(core, cp.Mustache.Render(Properties.Resources.Layout_SetPassword, new setPasswordDataModel {
                        userErrorHtml = $"<p>Set Password failed because the user who requested the password change could not be determined. This link is only valid for {PasswordTokenModel.tokenTTLsec} minutes, and must be used by the same browser that requested the password change.</p>",
                        passwordTokenKey = ""
                    }));
                }
                if (AuthController.tryIsValidPassword(core, user, password, ref userErrorMessage)) {
                    if (AuthController.trySetPassword(core.cpParent, password, user)) {
                        //
                        // -- password changed, forward to home
                        cp.Response.Redirect("/");
                        return "";
                    }
                    userErrorMessage = "There was an error and the password could not be set.";
                }
                return HtmlController.form(core, cp.Mustache.Render(Properties.Resources.Layout_SetPassword, new setPasswordDataModel {
                    userErrorHtml = userErrorMessage,
                    passwordTokenKey = passwordTokenKey
                }));
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                return "<p>There was an error on the set-password form.</p>";
            }
        }
        //
        public class setPasswordDataModel {
            public string passwordTokenKey { get; set; }
            public string userErrorHtml { get; set; }
        }
    }
}
