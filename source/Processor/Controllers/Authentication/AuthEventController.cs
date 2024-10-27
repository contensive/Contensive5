using System;
using System.Globalization;
using System.Text;
//
namespace Contensive.Processor.Controllers {
    //
    //========================================================================
    /// <summary>
    /// Recover password form and process
    /// </summary>
    public static class AuthEventController {
        //
        /// <summary>
        /// authentication event happens right after session is initialized.
        /// handle the built-in authentication methods (header basic authorization, default login form, link login )
        /// </summary>
        /// <param name="core"></param>
        /// <returns>Return true if successful authentication.</returns>
        public static bool processAuthenticationDefaultEvent(CoreController core, ref string UserErrorMessage) {
            //
            // -- default username/password login (? /mfa-otp/reset-password)
            string authType = core.docProperties.getText("type");
            if(!string.IsNullOrEmpty(authType)) {
                if(authType.Equals("login", StringComparison.InvariantCultureIgnoreCase)) {
                    string requestPassword = core.docProperties.getText("password");
                    if (LoginWorkflowController.processLogin(core, 
                        core.docProperties.getText("username"), 
                        requestPassword,
                        !string.IsNullOrEmpty(requestPassword),
                        ref UserErrorMessage)) {
                        return true;
                    }
                }
            }
            //
            // -- authentication from form/querystring, authAction=login, authUsername+authPassword
            //
            bool authenticationProcessSuccess = false;
            if (core.docProperties.containsKey("authAction") && core.docProperties.getText("authAction").ToLowerInvariant().Equals("login")) {
                string authUsername = core.docProperties.getText("authUsername");
                string authPassword = core.docProperties.getText("authPassword");
                if ((!string.IsNullOrWhiteSpace(authUsername)) && (!string.IsNullOrWhiteSpace(authPassword))) {
                    string userErrorMessage = "";
                    int userId = AuthController.preflightAuthentication_returnUserId(core, core.session, authUsername, authPassword, false, ref userErrorMessage);
                    if (userId > 0) {
                        AuthController.authenticateById(core, core.session, userId);
                        return true;
                    }
                }
            }
            //
            // -- authentication from authorization header
            //
            string basicAuthentication = core.docProperties.getText("authorization");
            if ((!string.IsNullOrWhiteSpace(basicAuthentication)) && (basicAuthentication.Length > 7) && (basicAuthentication.Substring(0, 6).ToLower(CultureInfo.InvariantCulture) == "basic ")) {
                //
                // -- Basic authentication
                string usernamePasswordEncoded = basicAuthentication.Substring(6);
                byte[] usernamePasswordBytes = Convert.FromBase64String(usernamePasswordEncoded);
                string[] usernamePassword = Encoding.ASCII.GetString(usernamePasswordBytes).Split(':');
                if (usernamePassword.Length == 2) {
                    string username = usernamePassword[0];
                    string password = usernamePassword[1];
                    string userErrorMessage = "";
                    int userId = AuthController.preflightAuthentication_returnUserId(core, core.session, username, password, false, ref userErrorMessage);
                    if (userId > 0) {
                        AuthController.authenticateById(core, core.session, userId);
                        return true;
                    }
                }
            }
            //
            // -- link authentication
            //
            string linkEid = core.docProperties.getText("eid");
            if (!string.IsNullOrEmpty(linkEid)) {
                //
                // -- attempt link authentication
                var linkToken = SecurityController.decodeToken(core, linkEid);
                logger.Trace($"{core.logCommonMessage},link authentication, linkEid [" + linkEid + "], linkToken.id [" + linkToken.id + "]");
                if (!linkToken.id.Equals(0) && linkToken.expires.CompareTo(core.dateTimeNowMockable) > 0) {
                    //
                    // -- valid link token, attempt login/recognize
                    if (core.siteProperties.getBoolean("AllowLinkLogin", true)) {
                        //
                        // -- allow Link Login
                        logger.Trace($"{core.logCommonMessage},attempt link Login, userid [" + linkToken.id + "]");
                        if (AuthController.authenticateById(core, core.session, linkToken.id)) {
                            LogController.addActivityCompletedVisit(core, "Login", "Successful link login", core.session.user.id);
                            return true;
                        }
                    } else if (core.siteProperties.getBoolean("AllowLinkRecognize", true)) {
                        //
                        // -- allow Link Recognize
                        logger.Trace($"{core.logCommonMessage},attempt link recognize, userid [" + linkToken.id + "]");
                        if (AuthController.recognizeById(core, core.session, linkToken.id)) {
                            LogController.addActivityCompletedVisit(core, "Login", "Successful link recognize", core.session.user.id);
                            return true;
                        }
                    } else {
                        //
                        logger.Trace($"{core.logCommonMessage},link login unsuccessful, site properties disabled");
                        //
                    }
                } else {
                    //
                    logger.Trace($"{core.logCommonMessage},link login unsuccessful, token expired or invalid [" + linkEid + "]");
                    //
                }
            }
            //
            // -- auto recognize/login authentication
            // -- always overrides trackVisits, only valid on first hit of new visit by returning user with valid visit cookie)
            //
            if (!authenticationProcessSuccess) {
                if (!core.session.visitor.memberId.Equals(0) && core.session.visit.pageVisits.Equals(0)) {
                    if (core.siteProperties.allowAutoLogin) {
                        //
                        // -- login by the visitor.memberid
                        if (AuthController.authenticateById(core, core.session, core.session.visitor.memberId, true)) {
                            LogController.addActivityCompletedVisit(core, "Login", "auto-login", core.session.user.id);
                            return true;
                        }
                    } else if (core.siteProperties.allowAutoRecognize) {
                        //
                        // -- recognize by the visitor.memberid
                        if (AuthController.recognizeById(core, core.session, core.session.visitor.memberId, true)) {
                            LogController.addActivityCompletedVisit(core, "Recognize", "auto-recognize", core.session.user.id);
                            return true;
                        }
                    }
                }
            }
            return false;
        }
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    }
}
