
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
            int hint = 0;
            try {
                CoreController core = ((CPClass)cp).core;
                //
                string passwordTokenKey = cp.Doc.GetText("passwordTokenKey");
                if (cp.Doc.GetText("button").ToLowerInvariant() == "cancel") {
                    hint = 10;
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
                    hint = 20;
                    //
                    // -- no button pressed
                    return HtmlController.form(core, cp.Mustache.Render(Properties.Resources.Layout_SetPassword, new setPasswordDataModel {
                        userErrorHtml = "",
                        passwordTokenKey = passwordTokenKey
                    }));
                }
                hint = 30;
                //
                // -- handle set password
                string password = cp.Doc.GetText("password");
                string confirm = cp.Doc.GetText("confirm");
                if (password != confirm) {
                    //
                    // -- confirm fail, delay to discourage brute-force
                    System.Threading.Thread.Sleep(3000);
                    return HtmlController.form(core, cp.Mustache.Render(Properties.Resources.Layout_SetPassword, new setPasswordDataModel {
                        userErrorHtml = "Password and password confirm do not match",
                        passwordTokenKey = passwordTokenKey
                    }));
                }
                hint = 40;
                //
                // -- if autoToken present, log out
                if (!string.IsNullOrEmpty(passwordTokenKey)) { cp.User.Logout(); }
                //
                PersonModel user = null;
                PasswordTokenModel passwordToken = PasswordTokenModel.getVisitPasswordTokenInfo(cp, passwordTokenKey);
                if (passwordToken == null) {
                    hint = 41;
                    //
                    // -- passwordToken not found
                    return HtmlController.form(core, cp.Mustache.Render(Properties.Resources.Layout_SetPassword, new setPasswordDataModel {
                        userErrorHtml = "The set password failed because the request for this password reset could not be found associated to this browser. Please request the password reset again from this browser.",
                        passwordTokenKey = passwordTokenKey
                    }));
                } else if (passwordToken.expires.CompareTo(DateTime.Now) < 0) {
                    hint = 43;
                    //
                    // -- passwordToken expired
                    return HtmlController.form(core, cp.Mustache.Render(Properties.Resources.Layout_SetPassword, new setPasswordDataModel {
                        userErrorHtml = $"The set password failed because time period for resetting your password has expired. Please request the password reset again from this browser and respond within {PasswordTokenModel.tokenTTLsec} minutes.",
                        passwordTokenKey = passwordTokenKey
                    }));
                } else if (cp.User.IsAuthenticated) {
                    hint = 41;
                    //
                    // -- user changing password
                    user = DbBaseModel.create<PersonModel>(cp, cp.User.Id);
                } else if (!string.IsNullOrEmpty(passwordToken.key)) {
                    hint = 42;
                    //
                    // -- passwordTokenKey in link to site matches passwordTokenKey saved in visit when invitation was sent (email/sms) so this is the same visitor
                    // -- forgot-password process, this user-visit requested forgot-password link
                    user = DbBaseModel.create<PersonModel>(cp, passwordToken.userId);
                }
                if (user == null) {
                    hint = 44;
                    return HtmlController.form(core, cp.Mustache.Render(Properties.Resources.Layout_SetPassword, new setPasswordDataModel {
                        userErrorHtml = $"<p>Set Password failed because the user who requested the password change could not be determined. This link is only valid for c, and must be used by the same browser that requested the password change.</p>",
                        passwordTokenKey = passwordTokenKey
                    }));
                }
                hint = 60;
                string userErrorMessage = "";
                if (AuthController.tryIsValidPassword(core, user, password, ref userErrorMessage)) {
                    if (AuthController.trySetPassword(core.cpParent, password, user)) {
                        //
                        // -- password changed, forward to home
                        cp.Response.Redirect("/");
                        return "";
                    }
                }
                hint = 70;
                return HtmlController.form(core, cp.Mustache.Render(Properties.Resources.Layout_SetPassword, new setPasswordDataModel {
                    userErrorHtml = userErrorMessage,
                    passwordTokenKey = passwordTokenKey
                }));
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex, $"hint {hint}");
                return $"<p>There was an error on the set-password form. Please use your back-button to return to the set-password form and try again. {hint}</p>";
            }
        }
        //
        public class setPasswordDataModel {
            public string passwordTokenKey { get; set; }
            public string userErrorHtml { get; set; }
        }
    }
}
