
using Contensive.Models.Db;
using Contensive.Processor.Controllers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
//
namespace Contensive.Processor.Addons {
    //
    //====================================================================================================
    /// <summary>
    /// Remote method to authenticate
    /// </summary>
    public class ImpersonateRemote : Contensive.BaseClasses.AddonBaseClass {
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
                // -- handle cancel button
                if (cp.Doc.GetText("button").ToLowerInvariant() == "cancel") {
                    if (cp.User.IsAdmin) {
                        cp.Response.Redirect(cp.GetAppConfig().adminRoute);
                    } else {
                        cp.Response.Redirect("/");
                    }
                }
                //
                // -- attempt impersonation if username provided. ignore button so this can be hit as a remote method
                if (cp.Doc.IsProperty("username")) {
                    string userError = "";
                    if (ImpersonationController.tryImpersonate(core, cp.Doc.GetText("username"), ref userError)) {
                        string dstUrl = "/";
                        cp.Response.Redirect(dstUrl);
                    } else {
                        //
                        // -- delay to discourage brute-force
                        cp.UserError.Add(userError);
                        System.Threading.Thread.Sleep(3000);
                    }
                }
                //
                // -- attempt failed or no attempt made, show form
                string layout = Properties.Resources.Layout_Impersonate;
                //
                // -- add user errors
                string userErrorMessage = core.doc.userErrorList.Count.Equals(0) ? "" : ErrorController.getUserError(core); 
                layout = MustacheController.renderStringToString(layout, new { userError = userErrorMessage });
                //
                // -- wrap in form
                result += Controllers.HtmlController.form(core, layout);
                result = Controllers.HtmlController.div(result, "ccLoginFormCon pt-4");
                return result;
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                return "<p>There was an error on the impersonation form.</p>";
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Authenticate with username and password
        /// </summary>
        /// <param name="core"></param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="errorPrefix"></param>
        /// <returns></returns>
        public static AuthenticateResponse authenticateUsernamePassword(CoreController core, string username, string password, string errorPrefix) {
            string userErrorMessage = "";
            int userId = AuthController.preflightAuthentication_returnUserId(core, core.session, username, password, false, ref userErrorMessage);
            if (userId == 0) {
                //
                // -- user was not found
                core.webServer.setResponseStatus(WebServerController.httpResponseStatus401_Unauthorized);
                return new AuthenticateResponse {
                    errors = [$"{errorPrefix} failed. {userErrorMessage}"],
                    data = new AuthenticateResponseData()
                };
            } else {
                if (!AuthController.authenticateById(core, core.session, userId)) {
                    //
                    // -- username/password login failed
                    core.webServer.setResponseStatus(WebServerController.httpResponseStatus401_Unauthorized);
                    return new AuthenticateResponse {
                        errors = [$"{errorPrefix} failed. {userErrorMessage}"],
                        data = new AuthenticateResponseData()
                    };
                } else {
                    var user = DbBaseModel.create<PersonModel>(core.cpParent, core.session.user.id);
                    if (user == null) {
                        core.webServer.setResponseStatus(WebServerController.httpResponseStatus401_Unauthorized);
                        return new AuthenticateResponse {
                            errors = [$"{errorPrefix} failed. User is not valid"],
                            data = new AuthenticateResponseData()
                        };
                    } else {
                        LogController.addActivityCompletedVisit(core, "Login", errorPrefix + " successful", core.session.user.id);
                        return new AuthenticateResponse {
                            errors = [],
                            data = new AuthenticateResponseData {
                                firstName = user.firstName,
                                lastName = user.lastName,
                                email = user.email,
                                avatar = (!string.IsNullOrWhiteSpace(user.thumbnailFilename)) ? core.appConfig.cdnFileUrl + user.thumbnailFilename : (!string.IsNullOrWhiteSpace(user.imageFilename)) ? core.appConfig.cdnFileUrl + user.imageFilename : ""
                            }
                        };
                    }
                }
            }

        }
        /// <summary>
        /// Authenticate remote method reponse
        /// </summary>
        public class AuthenticateResponse {
            /// <summary>
            /// if no errors, this is basic user data
            /// </summary>
            public AuthenticateResponseData data { get; set; } = new();
            /// <summary>
            /// non-zero length list indicates an error
            /// </summary>
            public List<string> errors { get; set; } = new();

        }
        /// <summary>
        /// user data returned by authenticate remote method
        /// </summary>
        public class AuthenticateResponseData {
            /// <summary>
            /// user data on success
            /// </summary>
            public string firstName { get; set; }
            /// <summary>
            /// user data on success
            /// </summary>
            public string lastName { get; set; }
            /// <summary>
            /// user data on success
            /// </summary>
            public string email { get; set; }
            /// <summary>
            /// user data on success
            /// </summary>
            public string avatar { get; set; }
        }
    }
}
