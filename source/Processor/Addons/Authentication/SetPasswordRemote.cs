
using Contensive.Models.Db;
using Contensive.Processor.Controllers;
using Microsoft.ClearScript.Windows;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
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
            string result = "";
            try {
                CoreController core = ((CPClass)cp).core;
                //
                // -- determine user. If authenticated, use current user.
                // -- if authToken string is included, used as one-time-login tokenk saved in user
                string userErrorMessage = "";
                PersonModel user = null;
                if (cp.User.IsAuthenticated) {
                    //
                    // -- user changing password
                    user = DbBaseModel.create<PersonModel>(cp, cp.User.Id);
                } else if (cp.Doc.IsProperty("authToken")) {
                    //
                    // -- forgot-password process, aut
                    List<PersonModel> users = DbBaseModel.createList<PersonModel>(cp, $"authToken={DbController.encodeSQLText(cp.Doc.GetText("authToken"))}");
                    if (users.Count == 1) {
                        user = users.First();
                    }
                }
                if (user == null) {
                    return HtmlController.form(core, cp.Mustache.Render(Properties.Resources.Layout_SetPassword, new setPasswordDataModel {
                        userErrorHtml = "<p>Set password feature is disabled because the user can not be determined.</p>",
                        autoToken = ""
                    }));
                }
                //
                if (cp.Doc.GetText("button").ToLowerInvariant() == "cancel") {
                    //
                    // -- handle cancel button
                    if (cp.Request.PathPage== cp.GetAppConfig().adminRoute) {
                        cp.Response.Redirect(cp.GetAppConfig().adminRoute + "?refresh");
                    } else {
                        cp.Response.Redirect("/");
                    }
                }
                if (cp.Doc.GetText("button").ToLowerInvariant() == "setpassword") {
                    //
                    // -- handle set password
                    string password = cp.Doc.GetText("password");
                    string confirm = cp.Doc.GetText("confirm");
                    if (password != confirm) {
                        //
                        // -- delay to discourage brute-force
                        cp.UserError.Add("Password and password confirm do not match");
                        System.Threading.Thread.Sleep(3000);
                    }
                    if (AuthenticationController.tryIsValidPassword(core, user, password, ref userErrorMessage)) {
                        AuthenticationController.trySetPassword(core.cpParent, password, user);
                    }
                }
                return HtmlController.form(core, cp.Mustache.Render(Properties.Resources.Layout_SetPassword, new setPasswordDataModel {
                    userErrorHtml = userErrorMessage,
                    autoToken = cp.Doc.GetText("authToken")
                }));
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                return "<p>There was an error attempting impersonation.</p>";
            }
        }
        //
        public class setPasswordDataModel {
            public string autoToken { get; set; }
            public string userErrorHtml { get; set; }
        }
    }
}
