
using Contensive.BaseClasses;
using Contensive.Models.Db;
using NLog.Targets.Wrappers;
using System;
using System.Data;
using System.Linq;
using System.Runtime.Remoting.Messaging;
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
                session.visit.loginAttempts = 0;
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
        public static bool authenticate(CoreController core, SessionController session, string requestUsername, string requestPassword, bool setUserAutoLogin) {
            try {
                //
                LogController.logTrace(core, "AuthenticationController.authenticate enter");
                //
                int userId = getUserByUsernamePassword(core, session, requestUsername, requestPassword, false);
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
        //===================================================================================================
        /// <summary>
        /// Test the username and password against users and return the userId of the match, or 0 if not valid match
        /// </summary>
        /// <param name="requestUsername"></param>
        /// <param name="requestPassword"></param>
        /// <param name="requestIncludesPassword">If true, the noPassword option is disabled</param>
        /// <returns></returns>
        public static int getUserByUsernamePassword(CoreController core, SessionController session, string requestUsername, string requestPassword, bool requestIncludesPassword) {
            try {
                //
                LogController.logTrace(core, "getUserIdForUsernameCredentials enter");
                //
                if (string.IsNullOrEmpty(requestUsername)) {
                    //
                    // -- username blank, stop here
                    LogController.logTrace(core, "getUserIdForUsernameCredentials fail, username blank");
                    return 0;
                }
                bool allowNoPassword = !requestIncludesPassword && core.siteProperties.getBoolean(sitePropertyName_AllowNoPasswordLogin);
                if (string.IsNullOrEmpty(requestPassword) && !allowNoPassword) {
                    //
                    // -- password blank, stop here
                    LogController.logTrace(core, "getUserIdForUsernameCredentials fail, password blank");
                    return 0;
                }
                if (session.visit.loginAttempts >= core.siteProperties.maxVisitLoginAttempts) {
                    //
                    // ----- already tried 5 times
                    LogController.logTrace(core, "getUserIdForUsernameCredentials fail, maxVisitLoginAttempts reached");
                    return 0;
                }
                string Criteria;
                bool allowEmailLogin = core.siteProperties.getBoolean(sitePropertyName_AllowEmailLogin);
                if (allowEmailLogin) {
                    //
                    // -- login by username or email
                    LogController.logTrace(core, "getUserIdForUsernameCredentials, attempt email login");
                    Criteria = "((username=" + DbController.encodeSQLText(requestUsername) + ")or(email=" + DbController.encodeSQLText(requestUsername) + "))";
                } else {
                    //
                    // -- login by username only
                    LogController.logTrace(core, "getUserIdForUsernameCredentials, attempt username login");
                    Criteria = "(username=" + DbController.encodeSQLText(requestUsername) + ")";
                }
                Criteria += "and((dateExpires is null)or(dateExpires>" + DbController.encodeSQLDate(core.dateTimeNowMockable) + "))";
                string peopleFieldList = "ID,password,passwordHash,admin,developer,ccguid";
                bool allowPlainTextPassword = core.siteProperties.getBoolean(sitePropertyName_AllowPlainTextPassword, true);
                using (var cs = new CsModel(core)) {
                    if (!cs.open("People", Criteria, "id", true, session.user.id, peopleFieldList, PageSize: 2)) {
                        //
                        // -- fail, username not found, stop here
                        LogController.logTrace(core, "getUserIdForUsernameCredentials fail, user record not found");
                        return 0;
                    }
                    if (cs.getRowCount() > 1) {
                        //
                        // -- fail, multiple matches
                        LogController.logTrace(core, "getUserIdForUsernameCredentials fail, multiple users found");
                        return 0;
                    }
                    int recordId = cs.getInteger("id");
                    if (allowNoPassword) {
                        //
                        // -- no-password mode
                        if (cs.getBoolean("admin") || cs.getBoolean("developer")) {
                            //
                            // -- fail, admin/dev cannot be no-password-mode
                            LogController.logTrace(core, "getUserIdForUsernameCredentials fail, no-pw mode matched admin/dev");
                            return 0;
                        }
                        //
                        // -- no-password auth cannot be content manager
                        using var csRules = new CsModel(core);
                        string SQL = ""
                            + " select ccGroupRules.ContentID"
                            + " from ccGroupRules right join ccMemberRules ON ccGroupRules.GroupId = ccMemberRules.GroupID"
                            + " where (1=1)"
                            + " and(ccMemberRules.memberId=" + recordId + ")"
                            + " and(ccMemberRules.active>0)"
                            + " and(ccGroupRules.active>0)"
                            + " and(ccGroupRules.ContentID Is not Null)"
                            + " and((ccMemberRules.DateExpires is null)OR(ccMemberRules.DateExpires>" + DbController.encodeSQLDate(core.doc.profileStartTime) + "))"
                            + ");";
                        if (!csRules.openSql(SQL)) {
                            //
                            // -- success, match is not content manager
                            LogController.logTrace(core, "getUserIdForUsernameCredentials fail, no-pw mode did not match content manager");
                            return recordId;
                        }

                    }
                    //
                    // -- password mode
                    //
                    if (string.IsNullOrEmpty(requestPassword)) {
                        //
                        // -- fail, no password
                        LogController.logTrace(core, "getUserIdForUsernameCredentials fail, blank requestPassword");
                        return 0;
                    }
                    string recordPassword = cs.getText("password");
                    if (allowPlainTextPassword) {
                        //
                        // -- legacy plain text password mode
                        //
                        if (requestPassword.Equals(recordPassword, StringComparison.InvariantCultureIgnoreCase)) {
                            //
                            // -- success, password match
                            LogController.logTrace(core, "getUserIdForUsernameCredentials success, pw match");
                            return recordId;
                        }
                        // todo remove tmp-password logging
                        // -- fail, plain text
                        LogController.logTrace(core, $"getUserIdForUsernameCredentials, allowPlainTextPassword, fail password mismatch");
                        return 0;
                    } else {
                        //
                        // -- hash password mode
                        //
                        PasswordHashInfo recordPasswordHash = new(cs.getText("passwordHash"));
                        switch( recordPasswordHash.hasherVersion) {
                            case "": {
                                    //
                                    // -- legacy hash. no version, etc
                                    //
                                    PasswordHashInfo requestPasswordHash = createPasswordHash_Legacy(core, requestPassword, cs.getText("ccguid"));
                                    if (requestPasswordHash.text.Equals(recordPasswordHash.text)) {
                                        //
                                        // -- success, passwordHash match, try upgrde to current password hasher and return success
                                        string userErrorMessage = "";
                                        if (trySetPassword(core.cpParent, requestPassword, recordId, ref userErrorMessage)) {
                                            // 
                                            // log issue
                                            LogController.logWarn(core, $"Cannot migrade from legacy-hasher to current-current, no user error returned, error [{userErrorMessage}]");
                                            return recordId;
                                        }
                                        LogController.logTrace(core, "getUserIdForUsernameCredentials, success, passwordHash match, !allowPlainTextPassword");
                                        return recordId;
                                    }
                                    //
                                    // -- unsuccessful hash-login, attempt migration and return status with the userErrorMessage from first fail
                                    return migrateToPasswordHash(core, requestPassword, recordPassword, recordId, requestPasswordHash);
                                }
                            case "231226": {
                                    //
                                    // 231226
                                    // 
                                    PasswordHashInfo requestPasswordHash = createPasswordHash_231226(core, requestPassword, cs.getText("ccguid"));
                                    if (requestPasswordHash.payload.Equals(recordPasswordHash.payload)) {
                                        //
                                        // -- success, passwordHash match, try upgrde to current password hasher and return success
                                        LogController.logTrace(core, "getUserIdForUsernameCredentials, success, passwordHash match, !allowPlainTextPassword");
                                        return recordId;
                                    }
                                    //
                                    // -- unsuccessful hash-login, attempt migration and return status with the userErrorMessage from first fail
                                    return migrateToPasswordHash(core, requestPassword, recordPassword, recordId, requestPasswordHash);
                                }
                        }
                    }
                }
                //
                LogController.logTrace(core, "getUserIdForUsernameCredentials fail, exit with no  match");
                //
                return 0;
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
        public static int  migrateToPasswordHash(CoreController core, string requestPassword, string recordPassword, int userId, PasswordHashInfo requestPasswordHash) {
            bool sp_migrateToPasswordHash = core.siteProperties.getBoolean(sitePropertyName_AllowAutoCreatePasswordHash, true);
            if (!sp_migrateToPasswordHash) {
                //
                // todo remove tmp-password logging
                // -- migration mode disabled, encrypted password fail
                LogController.logTrace(core, $"getUserIdForUsernameCredentials fail passwordhash mismatch");
                return 0;
            }
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
                core.db.executeNonQuery($"update ccmembers set password=null,passwordhash={requestPasswordHash.text} where id={userId}");
                LogController.logTrace(core, "getUserIdForUsernameCredentials success, !allowPlainTextPassword, migration-mode, matched plain text pw, setup passwordhash, cleared password.");
                return userId;
            }
            //
            // -- fail, hash password
            LogController.logTrace(core, "getUserIdForUsernameCredentials fail, !allowPlainTextPassword, migration-mode, migration failed because plain-text password mismatch");
            return 0;

        }
        //
        //====================================================================================================
        //
        public static bool login(CoreController core, SessionController session, string usernameOrEmail, string password, bool setAutoLogin) {
            if (session == null) { return false; }
            return authenticate(core, session, usernameOrEmail, password, setAutoLogin);
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
        public static bool trySetPassword(CPClass cp, string plainTextPassword, int userId) {
            string userErrorMessage = "";
            return trySetPassword(cp, plainTextPassword, userId, ref userErrorMessage);
        }
        //
        //====================================================================================================
        /// <summary>
        /// set the current user's password. returns false if password does not meet password rule criteria
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="plainTextPassword"></param>
        public static bool trySetPassword(CPClass cp, string plainTextPassword, ref string userErrorMessage) {
            return trySetPassword(cp, plainTextPassword, cp.User.Id, ref userErrorMessage);
        }
        //
        //====================================================================================================
        //
        public static bool trySetPassword(CPClass cp, string plainTextPassword, int userId, ref string userErrorMessage) {
            try {
                userErrorMessage = "";
                string userGuid = "";
                //
                {
                    PersonModel user = DbBaseModel.create<PersonModel>(cp, userId);
                    if (user is null) {
                        userErrorMessage = $"The user could not be found for id [{userId}]";
                        return false;
                    }
                    if (!tryIsValidPassword(cp.core, user, plainTextPassword, ref userErrorMessage)) { return false; }
                    userGuid = user.ccguid;
                }
                //
                int passwordMinLength = cp.Site.GetInteger("password min length", 5);
                if (plainTextPassword.Length < passwordMinLength) {
                    userErrorMessage = "Password length must be at least " + passwordMinLength.ToString() + " characters.";
                    return false;
                }
                //
                if (cp.Site.GetBoolean("allow plain text password", true)) {
                    //
                    // -- set plain-text password
                    cp.Db.ExecuteNonQuery($"update ccmembers set passwordHash=null,password={cp.Db.EncodeSQLText(plainTextPassword)},modifiedDate={cp.Db.EncodeSQLDate(DateTime.Now)},modifiedBy={cp.User.Id} where id={userId}");
                    DbBaseModel.invalidateCacheOfRecord<PersonModel>(cp, userId);
                    return true;
                }
                //
                // -- set hash password
                PasswordHashInfo passwordHash = createPasswordHash_current(cp.core, plainTextPassword, userGuid);
                cp.Db.ExecuteNonQuery($"update ccmembers set password=null,passwordHash={cp.Db.EncodeSQLText(passwordHash.text)},modifiedDate={cp.Db.EncodeSQLDate(DateTime.Now)},modifiedBy={cp.User.Id} where id={userId}");
                DbBaseModel.invalidateCacheOfRecord<PersonModel>(cp, userId);
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
        public static PasswordHashInfo createPasswordHash_current(CoreController core, string plainTextPassword, string salt) {
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
        private static PasswordHashInfo createPasswordHash_Legacy(CoreController core, string plainTextPassword, string salt) {
            return new PasswordHashInfo(SecurityController.encryptOneWay(core, plainTextPassword, salt));
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
        private static PasswordHashInfo createPasswordHash_231226(CoreController core, string plainTextPassword, string salt) {
            return new PasswordHashInfo("231226", "231226", SecurityController.encryptOneWay(core, plainTextPassword, salt));
        }
        //
        //====================================================================================================
        //
        public const string usernameAllowedCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        public const string passwordAllowedSpecialCharacters = "~!@#$%^&*()_+`-={}|[]\\:;'<>,.";
        public const string passwordAllowedNonSpecialCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        public const int passwordMinLength = 10;
        //
        //====================================================================================================
        /// <summary>
        /// return true if this username is acceptable, else false with a user acceptable error message
        /// </summary>
        /// <param name="core"></param>
        /// <param name="user"></param>
        /// <param name="newUsername"></param>
        /// <param name="userError"></param>
        /// <returns></returns>
        public static bool tryIsValidUsername(CoreController core, PersonModel user, string newUsername, ref string userError) {
            try {
                int userNameMinLength = 4;
                userError = "";
                // 
                // -- min length
                if (string.IsNullOrEmpty(newUsername)) {
                    userError = $"Username must be a minimum of {userNameMinLength} characters.";
                    return false;
                }
                if (newUsername.Length < userNameMinLength) {
                    userError = $"Username must be a minimum of {userNameMinLength} characters.";
                    return false;
                }
                // 
                // -- must contain only characters
                if (!newUsername.Any(c => usernameAllowedCharacters.Contains(c))) {
                    userError = $"The username must only contain the following characters ({usernameAllowedCharacters}).";
                    return false;
                }
                // 
                // -- username must be unique accross all local usernames
                int dupCount = DbBaseModel.getCount<PersonModel>(core.cpParent, $"(id<>{user.id})and(username={DbController.encodeSQLText(newUsername)})");
                if (dupCount > 0) {
                    userError = $"This username is not available.";
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
        /// <param name="userError"></param>
        /// <returns></returns>
        public static bool tryIsValidPassword(CoreController core, PersonModel user, string newPassword, ref string userError) {
            try {
                userError = "";
                // 
                // -- min length
                if (string.IsNullOrEmpty(newPassword)) {
                    userError = $"Password must be a minimum of {passwordMinLength} characters.";
                    return false;
                }
                if (newPassword.Length < passwordMinLength) {
                    userError = $"Password must be a minimum of {passwordMinLength} characters.";
                    return false;
                }
                // 
                // -- must contain only characters
                if (!newPassword.Any(c => (passwordAllowedNonSpecialCharacters + passwordAllowedSpecialCharacters).Contains(c))) {
                    userError = $"The password must only contain the following characters ({passwordAllowedNonSpecialCharacters + passwordAllowedSpecialCharacters}).";
                    return false;
                }
                // 
                // -- must at least 1 a-z
                if (!newPassword.Any(char.IsLower)) {
                    userError = $"This password must include at least 1 lower case character.";
                    return false;
                }
                // 
                // -- must at least 1 A-Z
                if (!newPassword.Any(char.IsUpper)) {
                    userError = $"This password must include at least 1 upper case character.";
                    return false;
                }
                // 
                // -- must at least 1 number
                if (!newPassword.Any(char.IsNumber)) {
                    userError = $"This password must include at least 1 number.";
                    return false;
                }
                // 
                // -- must at least 1 special characters
                if (!newPassword.Any(c => passwordAllowedSpecialCharacters.Contains(c))) {
                    userError = $"This password must include at least 1 special character ({passwordAllowedSpecialCharacters}).";
                    return false;
                }
                // 
                // -- password must not be on bad-password list
                if (DbBaseModel.getCount<CommonPasswordModel>(core.cpParent, $"(name={DbController.encodeSQLText(newPassword)})") > 0) {
                    userError = $"This password is not available.";
                    return false;
                }
                // 
                PasswordHashInfo newPasswordHash = createPasswordHash_current(core, newPassword, user.ccguid);
                // 
                // -- password must be different
                if (DbBaseModel.getCount<PersonModel>(core.cpParent, $"(id={user.id})and((password={DbController.encodeSQLText(newPassword)})or(passwordHash={DbController.encodeSQLText(newPasswordHash.text)}))") > 0) {
                    userError = $"This password is not available.";
                    return false;
                }
                // 
                // -- is on used password list
                if (UsedPasswordModel.isUsedPasswordHash(core.cpParent, newPassword)) {
                    userError = $"This Password is not available.";
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
        public class PasswordHashInfo {
            //
            /// <summary>
            /// create from full text
            /// </summary>
            /// <param name="text"></param>
            public PasswordHashInfo(string text) {
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
            public PasswordHashInfo(string hasherVersion, string encryptVersion, string payload) {
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