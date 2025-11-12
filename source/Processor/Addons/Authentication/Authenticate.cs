
using Contensive.Models.Db;
using Contensive.Processor.Controllers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
//
namespace Contensive.Processor.Addons.Primitives {
    //
    //====================================================================================================
    /// <summary>
    /// Remote method to authenticate
    /// </summary>
    public class AuthenticateClass : Contensive.BaseClasses.AddonBaseClass {
        //
        //====================================================================================================
        /// <summary>
        /// process a username/password authentication with no success result.
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public override object Execute(Contensive.BaseClasses.CPBaseClass cp) {
            string result = "";
            try {
                CoreController core = ((CPClass)cp).core;
                //
                // -- start with a logout if logged in
                if (core.session.isAuthenticated || core.session.isRecognized()) { AuthController.logout(core, core.session); }
                //
                // -- test for username/password authentication
                {
                    string username = core.docProperties.getText("username");
                    string password = core.docProperties.getText("password");
                    if ((!string.IsNullOrWhiteSpace(username)) && (!string.IsNullOrWhiteSpace(password))) {
                        //
                        // -- username and password provided, attempt username login
                        return authenticateUsernamePassword(core, username, password, "Username/Password Login");
                    }
                }
                //
                // -- test for header basic authentication
                string basicAuthentication = core.docProperties.getText("authorization");
                if ((!string.IsNullOrWhiteSpace(basicAuthentication)) && (basicAuthentication.Length > 7) && (basicAuthentication.Substring(0, 6).ToLower(CultureInfo.InvariantCulture) == "basic ")) {
                    string usernamePasswordEncoded = basicAuthentication.Substring(6);
                    byte[] usernamePasswordBytes = Convert.FromBase64String(usernamePasswordEncoded);
                    string[] usernamePassword = Encoding.ASCII.GetString(usernamePasswordBytes).Split(':');
                    if (usernamePassword.Length != 2) {
                        cp.Response.SetStatus(WebServerController.httpResponseStatus.Unauthorized);
                        return new AuthenticateResponse {
                            errors = ["Basic Authentication Failed."],
                            data = new AuthenticateResponseData()
                        };
                    }
                    string username = usernamePassword[0];
                    string password = usernamePassword[1];
                    return authenticateUsernamePassword(core, username, password, "Basic Authentication");
                }
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
            }
            return result;
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
                core.webServer.setResponseStatus(WebServerController.httpResponseStatus.Unauthorized);
                return new AuthenticateResponse {
                    errors = [$"{errorPrefix} failed. {userErrorMessage}"],
                    data = new AuthenticateResponseData()
                };
            } else {
                if (!AuthController.authenticateById(core, core.session, userId)) {
                    //
                    // -- username/password login failed
                    core.webServer.setResponseStatus(WebServerController.httpResponseStatus.Unauthorized);
                    return new AuthenticateResponse {
                        errors = [$"{errorPrefix} failed. {userErrorMessage}"],
                        data = new AuthenticateResponseData()
                    };
                } else {
                    var user = DbBaseModel.create<PersonModel>(core.cpParent, core.session.user.id);
                    if (user == null) {
                        core.webServer.setResponseStatus(WebServerController.httpResponseStatus.Unauthorized);
                        return new AuthenticateResponse {
                            errors = new List<string> { errorPrefix + " user is not valid." },
                            data = new AuthenticateResponseData()
                        };
                    } else {
                        LogController.addActivityCompletedVisit(core, "Login", errorPrefix + " successful", core.session.user.id);
                        return new AuthenticateResponse {
                            errors = new List<string>(),
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
            public AuthenticateResponseData data = new();
            /// <summary>
            /// non-zero length list indicates an error
            /// </summary>
            public List<string> errors = new();

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
