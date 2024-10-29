
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
                string authToken = cp.Doc.GetText("authToken");
                if (cp.Doc.GetText("button").ToLowerInvariant() == "cancel") {
                    //
                    // -- clear the setPassword token
                    AuthTokenInfoModel.clearVisitAuthTokenInfo(cp);
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
                        authToken = authToken
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
                        authToken = authToken
                    }));
                }
                //
                // -- if autoToken present, log out
                if (!string.IsNullOrEmpty(authToken)) { cp.User.Logout(); }
                //
                PersonModel user = null;
                string authTokenJson = cp.Visit.GetText("authTokenJson");
                AuthTokenInfoModel visitAuthTokeninfo = AuthTokenInfoModel.getVisitAuthTokenInfo(cp, authTokenJson);
                if (cp.User.IsAuthenticated) {
                    //
                    // -- user changing password
                    user = DbBaseModel.create<PersonModel>(cp, cp.User.Id);
                } else if (visitAuthTokeninfo != null && !string.IsNullOrEmpty(visitAuthTokeninfo.text)) {
                    //
                    // -- authToken in link to site matches authToken saved in visit when invitation was sent (email/sms) so this is the same visitor
                    // -- forgot-password process, this user-visit requested forgot-password link
                    user = DbBaseModel.create<PersonModel>(cp, visitAuthTokeninfo.userId);
                } else if (visitAuthTokeninfo.expires.CompareTo(DateTime.Now)<0 ) {
                    //
                    // -- authToken expired
                    userErrorMessage = "The time period for resetting your password has expired.";
                }
                if (user == null) {
                    return HtmlController.form(core, cp.Mustache.Render(Properties.Resources.Layout_SetPassword, new setPasswordDataModel {
                        userErrorHtml = $"<p>Set Password failed because the user who requested the password change could not be determined. This link is only valid for {AuthTokenInfoModel.tokenTTLsec} minutes, and must be used by the same browser that requested the password change.</p>",
                        authToken = ""
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
                    authToken = authToken
                }));
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                return "<p>There was an error on the set-password form.</p>";
            }
        }
        //
        public class setPasswordDataModel {
            public string authToken { get; set; }
            public string userErrorHtml { get; set; }
        }
    }
}
