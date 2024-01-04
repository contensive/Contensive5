using Contensive.BaseClasses;
using Contensive.Models.Db;
using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.ServiceModel.Security;
using System.Threading;
using static Contensive.Processor.Constants;
//
namespace Contensive.Processor.Controllers {
    //
    //========================================================================
    /// <summary>
    /// Handle authentication
    /// Authentication state is stored in the sessionController instance
    /// 
    /// authenticated -- the user is part of the session, and is logged in. This use is known to be the user in the session
    /// 
    /// recognize -- means this user is part of the session, but is not authenticated. So the session user might be the correct person, but we are not sure.
    /// 
    /// guest -- means the user has not been recognized. Values are accumulating, but the person is anonymous
    /// 
    /// </summary>
    public static class AuthenticationController {
        //
        //========================================================================
        /// <summary>
        /// Recognize the current member to be non-authenticated, but recognized.  Static method because it runs in constructor.
        /// 
        /// </summary>
        /// <param name="core">DI object with DI containers</param>
        /// <param name="session">session for this operations. Might be different than default session cp.session</param>
        /// <param name="userId">user to recognize</param>
        /// <param name="requireUserAutoLogin">if true, the user must have autoLogin true to be recognized.</param>
        /// <returns></returns>
        //
        public static bool recognizeById(CoreController core, SessionController session, int userId, bool requireUserAutoLogin) {
            try {
                //
                LogController.logTrace(core, "AuthenticationController.recognizeById, enter");
                //
                // -- argument validation
                if (userId.Equals(0)) { return false; }
                //
                // -- find user and validate
                PersonModel contextUser = DbBaseModel.create<PersonModel>(core.cpParent, userId);
                if (contextUser == null) { return false; }
                if (requireUserAutoLogin && !contextUser.autoLogin) { return false; }
                //
                // -- recognize ok, verify visit and visitor incase visitTracking is off
                if ((session.visitor == null) || (session.visitor.id == 0)) {
                    session.visitor = DbBaseModel.addEmpty<VisitorModel>(core.cpParent);
                }
                if ((session.visit == null) || (session.visit.id == 0)) {
                    session.visit = DbBaseModel.addEmpty<VisitModel>(core.cpParent);
                }
                //
                // -- update session for recognized user
                session.user = contextUser;
                session.visitor.memberId = session.user.id;
                session.visit.memberId = session.user.id;
                session.visit.visitAuthenticated = false;
                session.visit.visitorId = session.visitor.id;
                session.user.visits = session.user.visits + 1;
                if (session.user.visits == 1) {
                    session.visit.memberNew = true;
                } else {
                    session.visit.memberNew = false;
                }
                session.user.lastVisit = core.doc.profileStartTime;
                session.visit.excludeFromAnalytics = session.visit.excludeFromAnalytics || session.visit.bot || session.user.excludeFromAnalytics || session.user.admin || session.user.developer;
                session.visit.save(core.cpParent);
                session.visitor.save(core.cpParent);
                session.user.save(core.cpParent);
                return true;
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Recognize the current member to be non-authenticated, but recognized. 
        /// </summary>
        /// <param name="core"></param>
        /// <param name="userId"></param>
        /// <param name="session"></param>
        /// <returns></returns>
        public static bool recognizeById(CoreController core, SessionController session, int userId)
            => recognizeById(core, session, userId, false);
        //
        //========================================================================
        /// <summary>
        /// logout user
        /// </summary>
        public static void logout(CoreController core, SessionController session) {
            try {
                //
                // -- if user has autoLogin, turn off
                if (session.user.autoLogin) {
                    session.user.autoLogin = false;
                    session.user.save(core.cpParent);
                }
                if (!core.siteProperties.allowVisitTracking) {
                    session.visit = new VisitModel();
                    session.visitor = new VisitorModel();
                    session.user = new PersonModel();
                    return;
                }
                session.user = SessionController.createGuest(core, true);
                //
                // -- guest was created from a logout, disable autoLogin
                session.user.autoLogin = false;
                session.user.save(core.cpParent);
                //
                // -- update visit record for new user, not authenticated
                session.visit.memberId = session.user.id;
                session.visit.visitAuthenticated = false;
                session.visit.save(core.cpParent);
                //
                // -- update visitor record
                session.visitor.memberId = session.user.id;
                session.visitor.save(core.cpParent);
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }

        //
        //========================================================================
        /// <summary>
        /// Login (by username and password)
        /// </summary>
        /// <param name="requestUsername"></param>
        /// <param name="requestPassword"></param>
        /// <param name="setUserAutoLogin"></param>
        /// <returns></returns>
        public static bool tryAuthenticate(CoreController core, SessionController session, string requestUsername, string requestPassword, bool setUserAutoLogin, ref string userErrorMessage) {
            try {
                userErrorMessage = "";
                //
                LogController.logTrace(core, "AuthenticationController.authenticate enter");
                //
                int userId = preflightAuthentication_returnUserId(core, session, requestUsername, requestPassword, false, ref userErrorMessage);
                if (!userId.Equals(0) && authenticateById(core, session, userId)) {
                    //
                    // -- successful
                    LogController.addActivityCompletedVisit(core, "Login", "successful login, credential [" + requestUsername + "]", session.user.id);
                    //
                    core.db.executeNonQuery("update ccmembers set autoLogin=" + (setUserAutoLogin ? "1" : "0") + " where id=" + userId);
                    return true;
                }
                //
                // -- failed to authenticate
                ErrorController.addUserError(core, loginFailedError);
                //
                // -- pause to make brute force attempt more expensive
                Thread.Sleep(3000);
                //
                return false;
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //========================================================================
        /// <summary>
        /// Member Login By ID. Static method because it runs in constructor
        /// </summary>
        /// <param name="core"></param>
        /// <param name="userId"></param>
        /// <param name="session"></param>
        /// <param name="requestUserAutoLogin">if true, the user must have autoLogin true to be authenticate (use for auto-login process)</param>
        /// <returns></returns>
        public static bool authenticateById(CoreController core, SessionController session, int userId, bool requestUserAutoLogin) {
            try {
                //
                LogController.logTrace(core, "AuthenticationController.authenticateById, enter, userid [" + userId + "]");
                //
                if (userId == 0) { return false; }
                if (!recognizeById(core, session, userId, requestUserAutoLogin)) {
                    //
                    // -- pause to make brute force attempt for expensive
                    Thread.Sleep(3000);
                    //
                    return false;
                }
                //
                // -- recognize success, log them in to that user
                session.visit.visitAuthenticated = true;
                //
                // -- verify start time for visit
                if (session.visit.startTime != DateTime.MinValue) session.visit.startTime = core.doc.profileStartTime;
                //
                session.visit.save(core.cpParent);
                return true;
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //========================================================================
        ///
        public static bool authenticateById(CoreController core, SessionController authContext, int userId)
            => authenticateById(core, authContext, userId, false);
        //
        //========================================================================
        //
        public static void processLoginFail(CoreController core, int userId) {
            try {
                if (userId == 0) { return; }
                AuthenticationLogModel.log(core.cpParent, userId, false);
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //========================================================================
        //
        public static void processLoginSuccess(CoreController core, int userId) {
            try {
                //
                // -- successful login, clear login attempts
                core.session.visit.loginAttempts = 0;
                core.db.executeNonQuery($"update ccvisits set loginattempts={core.session.visit.loginAttempts} where id={core.session.visit.id}");
                //
                AuthenticationLogModel.log(core.cpParent, userId, true);
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //===================================================================================================
        /// <summary>
        /// If credentials are valid, return the userId, but the user is NOT authenticated. Else return 0 and a userErrorMessage that can be displayed.
        /// Test for valid credentials.
        /// Log success and fail here to centralize.
        /// </summary>
        /// <param name="requestUsername"></param>
        /// <param name="requestPassword"></param>
        /// <param name="requestIncludesPassword">If true, the noPassword option is disabled</param>
        /// <returns></returns>
        public static int preflightAuthentication_returnUserId(CoreController core, SessionController session, string requestUsername, string requestPassword, bool requestIncludesPassword, ref string userErrorMessage) {
            try {
                //
                LogController.logTrace(core, "preflightAuthentication_returnUserId enter");
                //
                // -- track the visit attempt
                core.session.visit.loginAttempts = core.session.visit.loginAttempts + 1;
                core.db.executeNonQuery($"update ccvisits set loginattempts={core.session.visit.loginAttempts} where id={core.session.visit.id}");
                //
                userErrorMessage = "";
                if (string.IsNullOrEmpty(requestUsername)) {
                    //
                    // -- username blank, stop here
                    userErrorMessage = "Username is required.";
                    LogController.logTrace(core, "preflightAuthentication_returnUserId fail, username blank");
                    return 0;
                }
                bool allowNoPassword = !requestIncludesPassword && core.siteProperties.getBoolean(sitePropertyName_AllowNoPasswordLogin);
                if (string.IsNullOrEmpty(requestPassword) && !allowNoPassword) {
                    //
                    // -- password blank, stop here
                    userErrorMessage = "Password is required.";
                    LogController.logTrace(core, "preflightAuthentication_returnUserId fail, password blank");
                    return 0;
                }
                string Criteria;
                bool allowEmailLogin = core.siteProperties.getBoolean(sitePropertyName_AllowEmailLogin);
                if (allowEmailLogin) {
                    //
                    // -- login by username or email
                    LogController.logTrace(core, "preflightAuthentication_returnUserId, attempt email login");
                    Criteria = "((username=" + DbController.encodeSQLText(requestUsername) + ")or(email=" + DbController.encodeSQLText(requestUsername) + "))";
                } else {
                    //
                    // -- login by username only
                    LogController.logTrace(core, "preflightAuthentication_returnUserId, attempt username login");
                    Criteria = "(username=" + DbController.encodeSQLText(requestUsername) + ")";
                }
                Criteria += "and((dateExpires is null)or(dateExpires>" + DbController.encodeSQLDate(core.dateTimeNowMockable) + "))";
                bool allowPlainTextPassword = core.siteProperties.getBoolean(sitePropertyName_AllowPlainTextPassword, true);
                List<PersonModel> records = DbBaseModel.createList<PersonModel>(core.cpParent, Criteria, "id", 2);
                if (records.Count == 0) {
                    //
                    // -- fail, username not found, stop here
                    userErrorMessage = "Username or password incorrect.";
                    LogController.logTrace(core, "preflightAuthentication_returnUserId fail, user record not found");
                    return 0;
                }
                if (records.Count > 1) {
                    //
                    // -- fail, multiple matches
                    userErrorMessage = "Username is not valid.";
                    LogController.logTrace(core, "preflightAuthentication_returnUserId fail, multiple users found");
                    return 0;
                }
                var record = records[0];
                if (allowNoPassword) {
                    //
                    // -- no-password mode
                    if (record.admin || record.developer) {
                        //
                        // -- fail, admin/dev cannot be no-password-mode
                        userErrorMessage = "Admin users require password login.";
                        LogController.logTrace(core, "preflightAuthentication_returnUserId fail, no-pw mode matched admin/dev");
                        return 0;
                    }
                    //
                    // todo -- consider removing content manager role
                    // -- no-password auth cannot be content manager
                    using var csRules = new CsModel(core);
                    string SQL = ""
                        + " select ccGroupRules.ContentID"
                        + " from ccGroupRules right join ccMemberRules ON ccGroupRules.GroupId = ccMemberRules.GroupID"
                        + " where (1=1)"
                        + " and(ccMemberRules.memberId=" + record.id + ")"
                        + " and(ccMemberRules.active>0)"
                        + " and(ccGroupRules.active>0)"
                        + " and(ccGroupRules.ContentID Is not Null)"
                        + " and((ccMemberRules.DateExpires is null)OR(ccMemberRules.DateExpires>" + DbController.encodeSQLDate(core.doc.profileStartTime) + "))"
                        + ");";
                    if (!csRules.openSql(SQL)) {
                        //
                        // -- success, match is not content manager
                        LogController.logTrace(core, "preflightAuthentication_returnUserId fail, no-pw mode did not match content manager");
                        return record.id;
                    }

                }
                //
                // -- password mode
                //
                if (string.IsNullOrEmpty(requestPassword)) {
                    //
                    // -- fail, no password
                    userErrorMessage = "Password is required.";
                    LogController.logTrace(core, "preflightAuthentication_returnUserId fail, blank requestPassword");
                    processLoginFail(core, record.id);
                    return 0;
                }
                string recordPassword = record.password;
                if (allowPlainTextPassword) {
                    //
                    // -- legacy plain text password mode
                    //
                    if (requestPassword.Equals(recordPassword, StringComparison.InvariantCultureIgnoreCase)) {
                        //
                        // -- check account lockout
                        if (!AuthenticationLogModel.allowLoginForLockoutPolicy(core.cpParent, record.id)) {
                            //
                            // -- fail, lockout
                            userErrorMessage = "Too many login attempts. Please wait and try again.";
                            LogController.logTrace(core, $"preflightAuthentication_returnUserId, allowPlainTextPassword, fail account lockout, memberId [{record.id}]");
                            processLoginFail(core, record.id);
                            return 0;
                        }
                        //
                        // -- success, password match
                        LogController.logTrace(core, "preflightAuthentication_returnUserId success, pw match");
                        processLoginSuccess(core, record.id);
                        return record.id;
                    }
                    // 
                    // -- fail, plain text
                    userErrorMessage = "Username or password are incorrect.";
                    LogController.logTrace(core, $"preflightAuthentication_returnUserId, allowPlainTextPassword, fail password mismatch");
                    processLoginFail(core, record.id);
                    return 0;
                } else {
                    //
                    // -- hash password mode
                    PasswordHashInfoModel recordPasswordHashInfo = new(record.passwordHash);
                    //
                    // -- test for plain-text to hash password migration (must be plain-text password match)
                    if (string.IsNullOrEmpty(record.passwordHash) && !string.IsNullOrEmpty(recordPassword)) {
                        //
                        // -- record with password and no hash
                        if (requestPassword.Equals(recordPassword, StringComparison.InvariantCultureIgnoreCase)) {
                            //
                            // -- plain-text password match with request, convert record to hash
                            PasswordHashInfoModel requestPasswordHashInfo = createPasswordHash_current(core, requestPassword, record.ccguid);
                            if (core.siteProperties.clearAdminPasswordOnHash) {
                                record.password = "";
                            }
                            record.passwordHash = requestPasswordHashInfo.text;
                            record.save(core.cpParent);
                            recordPasswordHashInfo = requestPasswordHashInfo;
                        }
                    }
                    //
                    switch (recordPasswordHashInfo.hasherVersion) {
                        case "231226": {
                                //
                                // 231226
                                // 
                                PasswordHashInfoModel requestPasswordHashInfo = createPasswordHash_231226(core, requestPassword, record.ccguid);
                                if (!requestPasswordHashInfo.payload.Equals(recordPasswordHashInfo.payload)) {
                                    //
                                    // -- fail, unsuccessful hash-login, attempt migration and return status with the userErrorMessage from first fail
                                    userErrorMessage = "Username or password are incorrect.";
                                    processLoginFail(core, record.id);
                                    return 0;
                                }
                                //
                                if (!AuthenticationLogModel.allowLoginForLockoutPolicy(core.cpParent, record.id)) {
                                    //
                                    // -- fail, account lockout
                                    LogController.logTrace(core, $"preflightAuthentication_returnUserId, hash-231226, fail account lockout, memberId [{record.id}]");
                                    userErrorMessage = "Too many login attempts. Please wait and try again.";
                                    processLoginFail(core, record.id);
                                    return 0;
                                }
                                //
                                // -- success, passwordHash match, try upgrde to current password hasher and return success
                                LogController.logTrace(core, "preflightAuthentication_returnUserId, success, passwordHash match, !allowPlainTextPassword");
                                processLoginSuccess(core, record.id);
                                return record.id;
                            }
                        default: {
                                //
                                // -- legacy hash. no version, etc
                                PasswordHashInfoModel requestPasswordHashInfo = createPasswordHash_Legacy(core, requestPassword, record.ccguid);
                                if (!requestPasswordHashInfo.text.Equals(recordPasswordHashInfo.text)) {
                                    //
                                    // -- unsuccessful hash-login, attempt migration and return status with the userErrorMessage from first fail
                                    userErrorMessage = "Username or password are incorrect.";
                                    processLoginFail(core, record.id);
                                    return 0;
                                }
                                //
                                // -- check account lockout
                                if (!AuthenticationLogModel.allowLoginForLockoutPolicy(core.cpParent, record.id)) {
                                    //
                                    // -- fail, lockout
                                    LogController.logTrace(core, $"preflightAuthentication_returnUserId, hash-no-version, fail account lockout, memberId [{record.id}]");
                                    userErrorMessage = "Too many login attempts. Please wait and try again.";
                                    processLoginFail(core, record.id);
                                    return 0;
                                }
                                //
                                // -- success, passwordHash match, try upgrde to current password hasher and return success
                                LogController.logTrace(core, "preflightAuthentication_returnUserId, success, passwordHash match, !allowPlainTextPassword");
                                trySetPassword(core.cpParent, requestPassword, record, ref userErrorMessage);
                                processLoginSuccess(core, record.id);
                                return record.id;
                            }
                    }
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// If the hash-password login fails, call this method to attempt a conversion from plain-text password to the current hasher.
        /// on success, returns the userId
        /// otherwize return 0
        /// </summary>
        /// <param name="core"></param>
        /// <param name="requestPassword"></param>
        /// <param name="recorrdPassword"></param>
        /// <returns></returns>
        public static int migrateToPasswordHash(CoreController core, string requestPassword, string recordPassword, int userId, PasswordHashInfoModel requestPasswordHash) {
            //
            // -- migration mode
            if (string.IsNullOrEmpty(recordPassword)) {
                //
                // -- fail, blank record password
                LogController.logTrace(core, "getUserIdForUsernameCredential, !allowPlainTextPassword, migration-mode, plain-text password blank");
                return 0;
            }
            if (requestPassword.Equals(recordPassword, StringComparison.InvariantCultureIgnoreCase)) {
                //
                // -- migration mode -- plain-text password matche, allow 1-time and passwordhas update
                if (core.siteProperties.clearAdminPasswordOnHash) {
                    core.db.executeNonQuery($"update ccmembers set password=null,passwordhash={requestPasswordHash.text} where id={userId}");
                } else {
                    core.db.executeNonQuery($"update ccmembers set passwordhash={requestPasswordHash.text} where id={userId}");
                }
                LogController.logTrace(core, "migrateToPasswordHash success, !allowPlainTextPassword, migration-mode, matched plain text pw, setup passwordhash, cleared password.");
                return userId;
            }
            //
            // -- fail, hash password
            LogController.logTrace(core, "migrateToPasswordHash fail, !allowPlainTextPassword, migration-mode, migration failed because plain-text password mismatch");
            return 0;

        }
        //
        //====================================================================================================
        //
        public static bool login(CoreController core, SessionController session, string usernameOrEmail, string password, bool setAutoLogin, ref string userErrorMessage) {
            if (session == null) { return false; }
            return tryAuthenticate(core, session, usernameOrEmail, password, setAutoLogin, ref userErrorMessage);
        }
        //
        //====================================================================================================
        //
        public static bool loginById(CoreController core, SessionController session, int userId, bool setAutoLogin) {
            if (core?.session == null) { return false; }
            bool result = authenticateById(core, session, userId);
            if (result) {
                core.session.user.autoLogin = setAutoLogin;
                core.session.user.save(core.cpParent);
            }
            return result;
        }
        //
        //====================================================================================================
        /// <summary>
        /// set the current user's password. returns false if password does not meet password rule criteria
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="plainTextPassword"></param>
        public static bool trySetPassword(CPClass cp, string plainTextPassword, PersonModel user) {
            string userErrorMessage = "";
            return trySetPassword(cp, plainTextPassword, user, ref userErrorMessage);
        }
        //
        //====================================================================================================
        /// <summary>
        /// set the current user's password. returns false if password does not meet password rule criteria
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="plainTextPassword"></param>
        public static bool trySetPassword(CPClass cp, string plainTextPassword, ref string userErrorMessage) {
            var user = DbBaseModel.create<PersonModel>(cp, cp.User.Id);
            return trySetPassword(cp, plainTextPassword, user, ref userErrorMessage);
        }
        //
        //====================================================================================================
        //
        public static bool trySetPassword(CPClass cp, string plainTextPassword, PersonModel user, ref string userErrorMessage) {
            try {
                userErrorMessage = "";
                string userGuid = "";
                //
                {
                    if (user is null) {
                        userErrorMessage = $"The user could not be found for id [{user.id}]";
                        return false;
                    }
                    if (!tryIsValidPassword(cp.core, user, plainTextPassword, ref userErrorMessage)) { return false; }
                    userGuid = user.ccguid;
                }
                //
                if (cp.Site.GetBoolean("allow plain text password", true)) {
                    //
                    // -- set plain-text password
                    user.passwordHash = null;
                    user.password = plainTextPassword;
                    user.modifiedDate = DateTime.Now;
                    user.modifiedBy = user.id;
                    user.passwordModifiedDate = DateTime.Now;
                    user.save(cp);
                    UsedPasswordModel.saveUsedPassword(cp, plainTextPassword, user.id);
                    DbBaseModel.invalidateCacheOfRecord<PersonModel>(cp, user.id);
                    return true;
                }
                //
                // -- set hash password
                PasswordHashInfoModel passwordHashInfo = createPasswordHash_current(cp.core, plainTextPassword, userGuid);
                user.passwordHash = passwordHashInfo.text;
                user.password = null;
                user.modifiedDate = DateTime.Now;
                user.modifiedBy = user.id;
                user.passwordModifiedDate = DateTime.Now;
                user.save(cp);
                UsedPasswordModel.saveUsedPassword(cp, passwordHashInfo.text, user.id);
                DbBaseModel.invalidateCacheOfRecord<PersonModel>(cp, user.id);
                return true;
            } catch (Exception ex) {
                LogController.logError(cp.core, ex);
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// create password hash with current hasher
        /// </summary>
        /// <param name="core"></param>
        /// <param name="plainTextPassword"></param>
        /// <param name="salt"></param>
        /// <returns></returns>
        public static PasswordHashInfoModel createPasswordHash_current(CoreController core, string plainTextPassword, string salt) {
            return createPasswordHash_231226(core, plainTextPassword, salt);
        }
        //
        //====================================================================================================
        /// <summary>
        /// legacy password-hasher.
        /// Create the password hash encrypt-one-way and save the hash without versions
        /// legacy hasher = guid-salted, no pepper, sha-512, base64-encoded (encrypt-one-way)
        /// </summary>
        /// <param name="core"></param>
        /// <param name="plainTextPassword"></param>
        /// <param name="salt"></param>
        /// <returns></returns>
        private static PasswordHashInfoModel createPasswordHash_Legacy(CoreController core, string plainTextPassword, string salt) {
            return new PasswordHashInfoModel(SecurityController.encryptOneWay(core, plainTextPassword, salt));
        }
        //
        //====================================================================================================
        /// <summary>
        /// Password Hasher 231226.
        /// Create the password hash object, with the hasherversion, encryptVersion, payload, and text (the composite string to be saved in the Db)
        /// hasher 231226 = guid-salted, no pepper, sha-512, base64-encoded (encrypt-one-way)
        /// </summary>
        /// <param name="core"></param>
        /// <param name="plainTextPassword"></param>
        /// <param name="salt"></param>
        /// <returns></returns>
        private static PasswordHashInfoModel createPasswordHash_231226(CoreController core, string plainTextPassword, string salt) {
            return new PasswordHashInfoModel("231226", "231226", SecurityController.encryptOneWay(core, plainTextPassword, salt));
        }
        //
        //====================================================================================================
        //
        public const string usernameAllowedCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        public const string passwordAllowedSpecialCharacters = "~!@#$%^&*()_+`-={}|[]\\:;'<>,.";
        public const string passwordAllowedNonSpecialCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        //public const int passwordMinLength = 10;
        public const int userNameMinLength = 4;
        //
        //====================================================================================================
        /// <summary>
        /// return true if this username is acceptable, else false with a user acceptable error message
        /// </summary>
        /// <param name="core"></param>
        /// <param name="user"></param>
        /// <param name="newUsername"></param>
        /// <param name="userErrorMessage"></param>
        /// <returns></returns>
        public static bool tryIsValidUsername(CoreController core, PersonModel user, string newUsername, ref string userErrorMessage) {
            try {
                userErrorMessage = "";
                // 
                // -- min length
                if (string.IsNullOrEmpty(newUsername)) {
                    userErrorMessage = $"Username must be a minimum of {userNameMinLength} characters.";
                    return false;
                }
                if (newUsername.Length < userNameMinLength) {
                    userErrorMessage = $"Username must be a minimum of {userNameMinLength} characters.";
                    return false;
                }
                // 
                // -- must contain only characters
                if (!newUsername.Any(c => usernameAllowedCharacters.Contains(c))) {
                    userErrorMessage = $"The username must only contain the following characters ({usernameAllowedCharacters}).";
                    return false;
                }
                // 
                // -- username must be unique accross all local usernames
                int dupCount = DbBaseModel.getCount<PersonModel>(core.cpParent, $"(id<>{user.id})and(username={DbController.encodeSQLText(newUsername)})");
                if (dupCount > 0) {
                    userErrorMessage = $"This username is not available.";
                    return false;
                }
                // 
                // -- success
                return true;
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// return true if this password is acceptable, else false with a user acceptable error message
        /// </summary>
        /// <param name="core"></param>
        /// <param name="user"></param>
        /// <param name="newPassword"></param>
        /// <param name="userErrorMessage"></param>
        /// <returns></returns>
        public static bool tryIsValidPassword(CoreController core, PersonModel user, string newPassword, ref string userErrorMessage) {
            try {
                userErrorMessage = "";
                // 
                // -- min length
                if (string.IsNullOrEmpty(newPassword)) {
                    userErrorMessage = $"Password must be a minimum of {core.siteProperties.passwordMinLength} characters.";
                    return false;
                }
                if (newPassword.Length < core.siteProperties.passwordMinLength) {
                    userErrorMessage = $"Password must be a minimum of {core.siteProperties.passwordMinLength} characters.";
                    return false;
                }
                // 
                // -- must contain only characters
                if (!newPassword.Any(c => (passwordAllowedNonSpecialCharacters + passwordAllowedSpecialCharacters).Contains(c))) {
                    userErrorMessage = $"The password must only contain the following characters ({passwordAllowedNonSpecialCharacters + passwordAllowedSpecialCharacters}).";
                    return false;
                }
                // 
                // -- must at least 1 a-z
                if (core.siteProperties.passwordRequiresLowercase) {
                    if (!newPassword.Any(char.IsLower)) {
                        userErrorMessage = $"This password must include at least 1 lower case character.";
                        return false;
                    }
                }
                // 
                // -- must at least 1 A-Z
                if (core.siteProperties.passwordRequiresUppercase) {
                    if (!newPassword.Any(char.IsUpper)) {
                        userErrorMessage = $"This password must include at least 1 upper case character.";
                        return false;
                    }
                }
                // 
                // -- must at least 1 number
                if (core.siteProperties.passwordRequiresNumber) {
                    if (!newPassword.Any(char.IsNumber)) {
                        userErrorMessage = $"This password must include at least 1 number.";
                        return false;
                    }
                }
                // 
                // -- must at least 1 special characters
                if (core.siteProperties.passwordRequiresSpecialCharacter) {
                    if (!newPassword.Any(c => passwordAllowedSpecialCharacters.Contains(c))) {
                        userErrorMessage = $"This password must include at least 1 special character ({passwordAllowedSpecialCharacters}).";
                        return false;
                    }
                }
                // 
                // -- password must not be on bad-password list
                if (DbBaseModel.getCount<CommonPasswordModel>(core.cpParent, $"(name={DbController.encodeSQLText(newPassword)})") > 0) {
                    userErrorMessage = $"This password is not available.";
                    return false;
                }
                // 
                PasswordHashInfoModel newPasswordHashInfo = createPasswordHash_current(core, newPassword, user.ccguid);
                // 
                // -- is on used password list
                if (UsedPasswordModel.isUsedPassword(core.cpParent, newPassword, newPasswordHashInfo.text)) {
                    userErrorMessage = $"This password is not available.";
                    return false;
                }
                //
                // -- success
                return true;
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //====================================================================================================
        //
        /// <summary>
        /// structor of the password hash info object, response from AuthenticationController.create password hash
        /// </summary>
        public class PasswordHashInfoModel {
            //
            /// <summary>
            /// create from full text
            /// </summary>
            /// <param name="text"></param>
            public PasswordHashInfoModel(string text) {
                if (text.Contains("$")) {
                    //
                    // -- delimited 
                    string[] textparts = text.Split('$');
                    if (textparts.Length != 3) {
                        this.text = text;
                        return;
                    }
                    this.text = text;
                    hasherVersion = textparts[0];
                    encryptVersion = textparts[1];
                    payload = textparts[2];
                    return;
                }
                //
                // -- legacy (payload = text, no versions)
                this.text = text;
                payload = text;
                encryptVersion = "";
                hasherVersion = "";
                return;
            }
            //
            /// <summary>
            /// create from individual elements
            /// </summary>
            /// <param name="hasherVersion"></param>
            /// <param name="encryptVersion"></param>
            /// <param name="payload"></param>
            public PasswordHashInfoModel(string hasherVersion, string encryptVersion, string payload) {
                this.hasherVersion = hasherVersion;
                this.encryptVersion = encryptVersion;
                this.payload = payload;
                text = $"{encryptVersion}${hasherVersion}${payload}";
            }
            public string text { get; }
            public string payload { get; }
            public string hasherVersion { get; }
            public string encryptVersion { get; }
        }
    }
}