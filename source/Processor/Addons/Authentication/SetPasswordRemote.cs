
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
                        authToken = cp.Doc.GetText("authToken")
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
                        authToken = cp.Doc.GetText("authToken")
                    }));
                }
                //
                // -- matches, attempt change
                // -- determine user.
                // -- If authenticated, use current user.
                // -- if authToken string is included, used as one-time-login tokenk saved in user
                PersonModel user = null;
                AuthTokenInfoModel visitAuthTokeninfo = AuthTokenInfoModel.getVisitAuthTokenInfo(cp);
                if (cp.User.IsAuthenticated) {
                    //
                    // -- user changing password
                    user = DbBaseModel.create<PersonModel>(cp, cp.User.Id);
                } else if (visitAuthTokeninfo != null && !string.IsNullOrEmpty(visitAuthTokeninfo.text) && visitAuthTokeninfo.text == cp.Doc.GetText(Constants.rn_authToken)) {
                    //
                    // -- authToken in link to site matches authToken saved in visit when invitation was sent (email/sms) so this is the same visitor
                    // -- forgot-password process, this user-visit requested forgot-password link
                    user = DbBaseModel.create<PersonModel>(cp, visitAuthTokeninfo.userId);
                }
                if (user == null) {
                    return HtmlController.form(core, cp.Mustache.Render(Properties.Resources.Layout_SetPassword, new setPasswordDataModel {
                        userErrorHtml = "<p>Set password feature is disabled because the user can not be determined.</p>",
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
                    authToken = cp.Doc.GetText("authToken")
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
