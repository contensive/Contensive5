﻿
using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using static Contensive.Processor.Controllers.GenericController;
using static Contensive.Processor.Constants;
using Contensive.BaseClasses;
using Contensive.Processor.Models.Domain;
using Contensive.Processor.Exceptions;
using Contensive.Models.Db;
using System.Globalization;
//
namespace Contensive.Processor.Controllers {
    //
    //====================================================================================================
    /// <summary>
    /// session context -- the identity, visit, visitor, view
    /// </summary>
    public class SessionController {
        //
        //====================================================================================================
        /// <summary>
        /// this class stores state, so it can hold a pointer to the core instance
        /// </summary>
        private CoreController core { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// the visit is the collection of pages, constructor creates default non-authenticated instance
        /// </summary>
        public VisitModel visit { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// visitor represents the browser, constructor creates default non-authenticated instance
        /// </summary>
        public VisitorModel visitor { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// user is the person at the keyboad, constructor creates default non-authenticated instance
        /// </summary>
        public PersonModel user { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// If the session was initialize without visit tracking, use verifyUser to initialize a user.
        /// This is called automatically when an addon references cp.user.id
        /// </summary>
        public void verifyUser() {
            if (user.id == 0) {
                var user = createGuest(core, false);
                //DbBaseModel.addDefault<PersonModel>(core.cpParent, ContentMetadataModel.getDefaultValueDict(core, PersonModel.tableMetadata.contentName));
                //user.createdByVisit = true;
                //user.save(core.cpParent);
                SessionController session = this;
                recognizeById(core, user.id, session);
            }
        }

        //
        //====================================================================================================
        /// <summary>
        /// userLanguage will return a valid populated language object
        /// </summary>
        /// <returns></returns>
        public LanguageModel userLanguage {
            get {
                if ((_language == null) && (user != null)) {
                    if (user.languageId > 0) {
                        //
                        // -- get user language
                        _language = DbBaseModel.create<LanguageModel>(core.cpParent, user.languageId);
                    }
                    if (_language == null) {
                        //
                        // -- try browser language if available
                        string HTTP_Accept_Language = core.webServer.getBrowserAcceptLanguage();
                        if (!string.IsNullOrEmpty(HTTP_Accept_Language)) {
                            List<LanguageModel> languageList = DbBaseModel.createList<LanguageModel>(core.cpParent, "(HTTP_Accept_Language='" + HTTP_Accept_Language + "')");
                            if (languageList.Count > 0) {
                                _language = languageList[0];
                            }
                        }
                    }
                    if (_language == null) {
                        //
                        // -- try default language
                        string defaultLanguageName = core.siteProperties.getText("Language", "English");
                        _language = DbBaseModel.createByUniqueName<LanguageModel>(core.cpParent, defaultLanguageName);
                    }
                    if (_language == null) {
                        //
                        // -- try english
                        _language = DbBaseModel.createByUniqueName<LanguageModel>(core.cpParent, "English");
                    }
                    if (_language == null) {
                        //
                        // -- add english to the table
                        Dictionary<string, String> defaultValues = ContentMetadataModel.getDefaultValueDict(core, LanguageModel.tableMetadata.contentName);
                        _language = LanguageModel.addDefault<LanguageModel>(core.cpParent, defaultValues);
                        _language.name = "English";
                        _language.http_Accept_Language = "en";
                        _language.save(core.cpParent);
                        user.languageId = _language.id;
                        user.save(core.cpParent);
                    }
                }
                return _language;
            }
        }
        private LanguageModel _language = null;
        //
        //====================================================================================================
        /// <summary>
        /// is this user authenticated in this visit
        /// </summary>
        public bool isAuthenticated {
            get {
                return visit.visitAuthenticated;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// browser is identified as a bot, and is not on the friendly-bot list
        /// </summary>
        public bool visitBadBot { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// The current request carries a cookie from the last request (use to detect back-button). if false, page is out of state (sequence)
        /// </summary>
        public bool visitStateOk { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// constructor, no arguments, created default authentication model for use without user, and before user is available
        /// </summary>
        public SessionController(CoreController core) {
            this.core = core;
            visit = new VisitModel();
            visitor = new VisitorModel();
            user = new PersonModel();
            visitStateOk = true;
        }
        //
        //========================================================================
        /// <summary>
        /// create a new session
        /// - set session-visit from visit-cookie
        /// - if session-visit not valid (new visit) set session-visit to new visit
        /// - set session-visit-user from link-eid
        /// - ses session-visit-user
        /// </summary>
        /// <param name="core"></param>
        /// <param name="trackVisits">When true, the session is initialized with a visit, visitor, user. Set false for background processing. 
        /// Set true for website processing when allowVisit true.
        /// When false, a visit can be configured on the fly by any application that attempts to access the cp.user.id
        /// </param>
        /// <returns></returns>
        public static SessionController create(CoreController core, bool trackVisits) {
            //
            LogController.logTrace(core, "SessionController, create");
            //
            SessionController resultSessionContext = null;
            try {
                //
                // --argument testing
                if (core.serverConfig == null) {
                    //
                    // -- application error if no server config
                    LogController.logError(core, new GenericException("authorization context cannot be created without a server configuration."));
                    return default;
                }
                resultSessionContext = new SessionController(core);
                if (core.appConfig == null) {
                    //
                    // -- no application, this is a server-only call not related to a 
                    LogController.logTrace(core, "app.config null, create server session");
                    return resultSessionContext;
                }
                //
                // -- load visit cookie, token
                //
                string visitCookie = resultSessionContext.getVisitCookie();
                bool cookieDetected = !string.IsNullOrEmpty(visitCookie);
                var visitToken = (string.IsNullOrEmpty(visitCookie)) ? new SecurityController.TokenData() : SecurityController.decodeToken(core, visitCookie);
                if (!visitToken.id.Equals(0)) {
                    VisitModel visitTest = DbBaseModel.create<VisitModel>(core.cpParent, visitToken.id);
                    if (!(visitTest is null)) {
                        resultSessionContext.visit = visitTest;
                        trackVisits = true;
                        resultSessionContext.visitStateOk = (visitToken.expires - encodeDate(resultSessionContext.visit.lastVisitTime)).TotalSeconds < 2;
                    }
                }
                //
                // -- load visitor cookie cookie/tokens
                //
                string visitorCookie = resultSessionContext.getVisitorCookie();
                cookieDetected |= !string.IsNullOrEmpty(visitorCookie);
                var visitorToken = (string.IsNullOrEmpty(visitorCookie)) ? new SecurityController.TokenData() : SecurityController.decodeToken(core, visitorCookie);
                if (!visitorToken.id.Equals(0)) {
                    VisitorModel visitorTest = DbBaseModel.create<VisitorModel>(core.cpParent, visitorToken.id);
                    if (!(visitorTest is null)) {
                        resultSessionContext.visitor = visitorTest;
                    }
                }
                //
                // -- sessioncontext loaded. save at end if these flags set
                //
                bool visitor_changes = false;
                bool user_changes = false;
                //
                // -- handle link login
                //
                string linkEid = core.docProperties.getText("eid");
                if (!string.IsNullOrEmpty(linkEid)) {
                    //
                    // -- attempt link authentication
                    var linkToken = SecurityController.decodeToken(core, linkEid);
                    if (!linkToken.id.Equals(0) && linkToken.expires.CompareTo(core.dateTimeNowMockable) < 0) {
                        //
                        // -- valid link token, attempt login/recognize
                        if (core.siteProperties.getBoolean("AllowLinkLogin", true)) {
                            //
                            // -- allow Link Login
                            LogController.logTrace(core, "attempt link Login, userid [" + linkToken.id + "]");
                            if (authenticateById(core, linkToken.id, resultSessionContext)) {
                                trackVisits = true;
                                LogController.addSiteActivity(core, "Successful link login", resultSessionContext.user.id, resultSessionContext.user.organizationId);
                            } else {
                                LogController.addSiteActivity(core, "UnSuccessful link login", resultSessionContext.user.id, resultSessionContext.user.organizationId);
                            }
                        } else if (core.siteProperties.getBoolean("AllowLinkRecognize", true)) {
                            //
                            // -- allow Link Recognize
                            LogController.logTrace(core, "attempt link recognize, userid [" + linkToken.id + "]");
                            if (recognizeById(core, linkToken.id, resultSessionContext)) {
                                trackVisits = true;
                                LogController.addSiteActivity(core, "Successful link recognize", resultSessionContext.user.id, resultSessionContext.user.organizationId);
                            }
                        } else {
                            //
                            LogController.logTrace(core, "link login unsuccessful, site properties disabled");
                            //
                        }
                    } else {
                        //
                        LogController.logTrace(core, "link login unsuccessful, token expired or invalid [" + linkEid + "]");
                        //
                    }
                }
                //
                // -- Handle auto login/recognize (always overrides trackVisits, only valid on first hit of new visit by returning user with valid visit cookie)
                //
                if (!resultSessionContext.visitor.memberId.Equals(0) && resultSessionContext.visit.pageVisits.Equals(0)) {
                    if (core.siteProperties.allowAutoLogin) {
                        //
                        // -- login by the visitor.memberid
                        if (authenticateById(core, resultSessionContext.visitor.memberId, resultSessionContext, true)) {
                            LogController.addSiteActivity(core, "auto-login", resultSessionContext.user.id, resultSessionContext.user.organizationId);
                            visitor_changes = true;
                            user_changes = true;
                        }
                    } else if (core.siteProperties.allowAutoRecognize) {
                        //
                        // -- recognize by the visitor.memberid
                        if (recognizeById(core, resultSessionContext.visitor.memberId, resultSessionContext, true)) {
                            LogController.addSiteActivity(core, "auto-recognize", resultSessionContext.user.id, resultSessionContext.user.organizationId);
                            visitor_changes = true;
                            user_changes = true;
                        }
                    }
                }
                //
                // -- setup session
                //
                bool AllowOnNewVisitEvent = false;
                if (trackVisits) {
                    //
                    // -- verify session visitor
                    //
                    bool visitorNew = false;
                    if (resultSessionContext.visit != null) {
                        if ((resultSessionContext.visit.visitorId > 0) && (!resultSessionContext.visit.visitorId.Equals(resultSessionContext.visitor.id))) {
                            //
                            // -- visit.visitor overrides cookie visitor
                            VisitorModel testVisitor = DbBaseModel.create<VisitorModel>(core.cpParent, resultSessionContext.visit.visitorId);
                            if (!(testVisitor is null)) {
                                resultSessionContext.visitor = testVisitor;
                                resultSessionContext.visit.visitorId = resultSessionContext.visitor.id;
                                visitor_changes = true;
                                setVisitorCookie(core, resultSessionContext);
                            }
                        }
                    }
                    if ((resultSessionContext.visitor == null) || resultSessionContext.visitor.id.Equals(0)) {
                        //
                        // -- create new visitor
                        resultSessionContext.visitor = DbBaseModel.addEmpty<VisitorModel>(core.cpParent);
                        visitorNew = true;
                        visitor_changes = true;
                    }
                    //
                    // -- verify session visit
                    //
                    bool createNewVisit = false;
                    if ((resultSessionContext.visit == null) || (resultSessionContext.visit.id.Equals(0))) {
                        //
                        // -- visit record is missing, create a new visit
                        LogController.logTrace(core, "visit invalid, create a new visit");
                        createNewVisit = true;
                    } else if ((resultSessionContext.visit.lastVisitTime != null) && (encodeDate(resultSessionContext.visit.lastVisitTime).AddHours(1) < core.doc.profileStartTime)) {
                        //
                        // -- visit has expired, create new visit
                        LogController.logTrace(core, "visit expired, create new visit");
                        createNewVisit = true;
                    }
                    if (createNewVisit) {
                        resultSessionContext.visit = DbBaseModel.addEmpty<VisitModel>(core.cpParent);
                        resultSessionContext.visit.startTime = core.doc.profileStartTime;
                        if (string.IsNullOrEmpty(resultSessionContext.visit.name)) {
                            resultSessionContext.visit.name = "User";
                        }
                        resultSessionContext.visit.startTime = core.doc.profileStartTime;
                        resultSessionContext.visit.browser = core.webServer.requestBrowser.substringSafe(0, 254);
                        resultSessionContext.visit.remote_addr = core.webServer.requestRemoteIP;
                        //
                        // -- setup referrer
                        if (!string.IsNullOrEmpty(core.webServer.requestReferrer)) {
                            string WorkingReferer = core.webServer.requestReferrer;
                            int SlashPosition = GenericController.strInstr(1, WorkingReferer, "//");
                            if ((SlashPosition != 0) && (WorkingReferer.Length > (SlashPosition + 2))) {
                                WorkingReferer = WorkingReferer.Substring(SlashPosition + 1);
                            }
                            SlashPosition = strInstr(1, WorkingReferer, "/");
                            if (SlashPosition == 0) {
                                resultSessionContext.visit.refererPathPage = "";
                                resultSessionContext.visit.http_referer = WorkingReferer;
                            } else {
                                resultSessionContext.visit.refererPathPage = WorkingReferer.Substring(SlashPosition - 1);
                                resultSessionContext.visit.http_referer = WorkingReferer.left(SlashPosition - 1);
                            }
                            resultSessionContext.visit.refererPathPage = resultSessionContext.visit.refererPathPage.substringSafe(0, 255);
                        }
                        //
                        if (string.IsNullOrEmpty(core.webServer.requestBrowser)) {
                            //
                            // blank browser, Blank-Browser-Bot
                            //
                            resultSessionContext.visit.name = "Blank-Browser-Bot";
                            resultSessionContext.visit.bot = true;
                            resultSessionContext.visitBadBot = false;
                            resultSessionContext.visit.mobile = false;
                            resultSessionContext.visit.browser = string.Empty;
                        } else {
                            //
                            // -- mobile detect
                            switch (resultSessionContext.visitor.forceBrowserMobile) {
                                case 1: {
                                        resultSessionContext.visit.mobile = true;
                                        break;
                                    }
                                case 2: {
                                        resultSessionContext.visit.mobile = false;
                                        break;
                                    }
                                default: {
                                        resultSessionContext.visit.mobile = isMobile(core.webServer.requestBrowser);
                                        break;
                                    }
                            }
                        }
                        //
                        // -- new visit, update the persistant visitor cookie
                        setVisitorCookie(core, resultSessionContext);
                        //
                        // -- new visit, OnNewVisit Add-on call
                        AllowOnNewVisitEvent = true;
                    }
                    //
                    // -- visit object is valid, update details
                    resultSessionContext.visit.timeToLastHit = encodeInteger((core.doc.profileStartTime - encodeDate(resultSessionContext.visit.startTime)).TotalSeconds);
                    resultSessionContext.visit.excludeFromAnalytics |= resultSessionContext.visit.bot || resultSessionContext.user.excludeFromAnalytics || resultSessionContext.user.admin || resultSessionContext.user.developer;
                    resultSessionContext.visit.pageVisits += 1;
                    resultSessionContext.visit.cookieSupport |= cookieDetected;
                    resultSessionContext.visit.lastVisitTime = core.doc.profileStartTime;
                    resultSessionContext.visit.visitorNew = visitorNew;
                    resultSessionContext.visit.visitorId = resultSessionContext.visitor.id;
                    //
                    // -- verify user identity
                    //
                    if ((resultSessionContext.user is null) || resultSessionContext.user.id.Equals(0)) {
                        //
                        // -- setup visit user if not authenticated
                        if (!resultSessionContext.visit.memberId.Equals(0)) {
                            resultSessionContext.user = DbBaseModel.create<PersonModel>(core.cpParent, resultSessionContext.visit.memberId);
                        }
                        //
                        // -- setup new user if nothing else
                        if ((resultSessionContext.user is null) || resultSessionContext.user.id.Equals(0)) {
                            resultSessionContext.user = createGuest(core, true);
                            //resultSessionContext.user = DbBaseModel.addEmpty<PersonModel>(core.cpParent);
                            //resultSessionContext.user.createdByVisit = true;
                            //resultSessionContext.user.name = "Guest";
                            //resultSessionContext.user.firstName = "Guest";
                            //resultSessionContext.user.createdBy = resultSessionContext.user.id;
                            //resultSessionContext.user.dateAdded = core.doc.profileStartTime;
                            //resultSessionContext.user.modifiedBy = resultSessionContext.user.id;
                            //resultSessionContext.user.modifiedDate = core.doc.profileStartTime;
                            //resultSessionContext.user.visits = 1;
                            user_changes = true;
                            //
                            resultSessionContext.visit.visitAuthenticated = false;
                            resultSessionContext.visit.memberNew = true;
                            resultSessionContext.visit.memberId = resultSessionContext.user.id;
                            //
                            resultSessionContext.visitor.memberId = resultSessionContext.user.id;
                            visitor_changes = true;
                        }
                    }
                    //
                    // -- check for changes in session data consistency
                    //
                    if (resultSessionContext.visit.createdBy.Equals(0)) {
                        resultSessionContext.visit.createdBy = resultSessionContext.user.id;
                        resultSessionContext.visit.modifiedBy = resultSessionContext.user.id;
                    }
                    resultSessionContext.visit.memberId = resultSessionContext.user.id;
                    resultSessionContext.visit.visitorId = resultSessionContext.visitor.id;
                    //
                    // -- Save anything that changed
                    //
                    resultSessionContext.visit.save(core.cpParent, 0, true);
                    if (visitor_changes) {
                        resultSessionContext.visitor.save(core.cpParent, 0, true);
                    }
                    if (user_changes) {
                        resultSessionContext.user.save(core.cpParent, 0, true);
                    }
                }
                //
                // -- execute onNewVisit addons
                //
                if (AllowOnNewVisitEvent) {
                    LogController.logTrace(core, "execute NewVisit Event");
                    foreach (var addon in core.addonCache.getOnNewVisitAddonList()) {
                        CPUtilsBaseClass.addonExecuteContext executeContext = new CPUtilsBaseClass.addonExecuteContext {
                            addonType = CPUtilsBaseClass.addonContext.ContextOnNewVisit,
                            errorContextMessage = "new visit event running addon  [" + addon.name + "]"
                        };
                        core.addon.execute(addon, executeContext);
                    }
                }
                //
                // -- Write Visit Cookie
                //
                setVisitCookie(core, resultSessionContext);
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            } finally {
                //
                LogController.logTrace(core, "finally");
                //
            }
            return resultSessionContext;
        }
        /// <summary>
        /// The prefix for visit and visitor cookes
        /// </summary>
        public string appNameCookiePrefix {
            get {
                return encodeCookieName(core.appConfig.name);
            }
        }
        //
        //========================================================================
        /// <summary>
        /// Create a new user record as Guest
        /// </summary>
        /// <param name="core"></param>
        /// <param name="exitWithoutSave"></param>
        /// <returns></returns>
        public static PersonModel createGuest(CoreController core, bool exitWithoutSave) {
            PersonModel user = DbBaseModel.addEmpty<PersonModel>(core.cpParent);
            user.createdByVisit = true;
            user.name = "Guest";
            user.firstName = "Guest";
            user.createdBy = user.id;
            user.dateAdded = core.doc.profileStartTime;
            user.modifiedBy = user.id;
            user.modifiedDate = core.doc.profileStartTime;
            user.visits = 1;
            user.autoLogin = false;
            user.admin = false;
            user.developer = false;
            user.allowBulkEmail = false;
            if (!exitWithoutSave) { user.save(core.cpParent); }
            return user;
        }
        //
        //========================================================================
        /// <summary>
        /// get the request visit cookie
        /// </summary>
        /// <returns></returns>
        public string getVisitCookie() {
            return core.webServer.getRequestCookie(appNameCookiePrefix + cookieNameVisit);
        }
        //
        //========================================================================
        /// <summary>
        /// get the request visit cookie
        /// </summary>
        /// <returns></returns>
        public string getVisitorCookie() {
            return core.webServer.getRequestCookie(appNameCookiePrefix + cookieNameVisitor);
        }
        //
        //========================================================================
        /// <summary>
        /// sets the visit cookie based on the sessionContext (visit.id)
        /// </summary>
        /// <param name="core"></param>
        /// <param name="sessionContext"></param>
        public static void setVisitCookie(CoreController core, SessionController sessionContext) {
            if (sessionContext.visit.id.Equals(0)) { return; }
            DateTime expirationDate = encodeDate(sessionContext.visit.startTime).AddMinutes(60);
            string cookieValue = SecurityController.encodeToken(core, sessionContext.visit.id, expirationDate);
            LogController.logTrace(core, "set visit cookie, visitId [" + sessionContext.visit.id + "], expirationDate [" + expirationDate.ToString() + "], cookieValue [" + cookieValue + "]");
            core.webServer.addResponseCookie(sessionContext.appNameCookiePrefix + Constants.cookieNameVisit, cookieValue);
        }
        //
        //========================================================================
        /// <summary>
        /// sets the visitor cookie based on the sessionContext
        /// </summary>
        /// <param name="core"></param>
        /// <param name="sessionContext"></param>
        public static void setVisitorCookie(CoreController core, SessionController sessionContext) {
            if (sessionContext.visitor.id.Equals(0)) { return; }
            DateTime expirationDate = encodeDate(sessionContext.visit.startTime).AddYears(1);
            string cookieValue = SecurityController.encodeToken(core, sessionContext.visitor.id, expirationDate);
            LogController.logTrace(core, "set visitor cookie, visitorId [" + sessionContext.visitor.id + "], expirationDate [" + expirationDate.ToString() + "], cookieValue [" + cookieValue + "]");
            core.webServer.addResponseCookie(sessionContext.appNameCookiePrefix + cookieNameVisitor, cookieValue, expirationDate);
        }
        //
        //========================================================================
        /// <summary>
        /// true if the user is authenticate and either an admin or a developer
        /// </summary>
        /// <param name="core"></param>
        /// <returns></returns>
        public bool isAuthenticatedAdmin() {
            try {
                return visit.visitAuthenticated && (user.admin || user.developer);
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //========================================================================
        /// <summary>
        /// true if the user is authenticated and a developoer
        /// </summary>
        /// <param name="core"></param>
        /// <returns></returns>
        public bool isAuthenticatedDeveloper() {
            try {
                return visit.visitAuthenticated && (user.admin || user.developer);
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //========================================================================
        /// <summary>
        /// true if the user is authenticated and has content editing rights to the content provided.
        /// </summary>
        /// <param name="core"></param>
        /// <param name="ContentName"></param>
        /// <returns></returns>
        public bool isAuthenticatedContentManager(ContentMetadataModel contentMetadata) {
            try {
                if (core.session.isAuthenticatedAdmin()) { return true; }
                if (!isAuthenticated) { return false; }
                //
                // -- for specific Content
                return PermissionController.getUserContentPermissions(core, contentMetadata).allowEdit;
            } catch (Exception ex) {
                LogController.logError(core, ex);
                return false;
            }
        }
        //
        //========================================================================
        /// <summary>
        /// true if the user is authenticated and has content editing rights to the content provided. If the content is blank, user must be admin or developer.
        /// </summary>
        /// <param name="core"></param>
        /// <param name="ContentName"></param>
        /// <returns></returns>
        public bool isAuthenticatedContentManager(string ContentName) {
            try {
                if (core.session.isAuthenticatedAdmin()) { return true; }
                //
                if (string.IsNullOrEmpty(ContentName)) {
                    //
                    // -- for anything
                    return isAuthenticatedContentManager();
                } else {
                    //
                    // -- for specific Content
                    ContentMetadataModel cdef = ContentMetadataModel.createByUniqueName(core, ContentName);
                    return PermissionController.getUserContentPermissions(core, cdef).allowEdit;
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //========================================================================
        /// <summary>
        /// true if the user is authenticated and has content editing rights to the content provided. If the content is blank, user must be admin or developer.
        /// </summary>
        /// <param name="core"></param>
        /// <param name="ContentName"></param>
        /// <returns></returns>
        public bool isAuthenticatedContentManager() {
            try {
                if (core.session.isAuthenticatedAdmin()) { return true; }
                if (_isAuthenticatedContentManagerAnything_loaded && _isAuthenticatedContentManagerAnything_userId.Equals(user.id)) return _isAuthenticatedContentManagerAnything;
                //
                // -- Is a CM for any content def
                using (var csData = new CsModel(core)) {
                    string sql = ""
                        + " SELECT ccGroupRules.ContentID"
                        + " FROM ccGroupRules RIGHT JOIN ccMemberRules ON ccGroupRules.GroupId = ccMemberRules.GroupID"
                        + " WHERE ("
                            + "(ccMemberRules.memberId=" + DbController.encodeSQLNumber(user.id) + ")"
                            + " AND(ccMemberRules.active<>0)"
                            + " AND(ccGroupRules.active<>0)"
                            + " AND(ccGroupRules.ContentID Is not Null)"
                            + " AND((ccMemberRules.DateExpires is null)OR(ccMemberRules.DateExpires>" + DbController.encodeSQLDate(core.doc.profileStartTime) + "))"
                            + ")";
                    _isAuthenticatedContentManagerAnything = csData.openSql(sql);
                }
                //
                _isAuthenticatedContentManagerAnything_userId = user.id;
                _isAuthenticatedContentManagerAnything_loaded = true;
                return _isAuthenticatedContentManagerAnything;
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        private bool _isAuthenticatedContentManagerAnything_loaded = false;
        private int _isAuthenticatedContentManagerAnything_userId;
        private bool _isAuthenticatedContentManagerAnything;
        //
        //========================================================================
        /// <summary>
        /// logout user
        /// </summary>
        public void logout() {
            try {
                //
                LogController.addSiteActivity(core, "logout", user.id, user.organizationId);
                //
                // -- if user has autoLogin, turn off
                if (user.autoLogin) {
                    user.autoLogin = false;
                    user.save(core.cpParent);
                }
                if (!core.siteProperties.allowVisitTracking) {
                    visit = new VisitModel();
                    visitor = new VisitorModel();
                    user = new PersonModel();
                    return;
                }
                user = SessionController.createGuest(core, true);
                //var defaultValues = ContentMetadataModel.getDefaultValueDict(core, PersonModel.tableMetadata.contentName);
                //user = DbBaseModel.addDefault<PersonModel>(core.cpParent, defaultValues);
                //if (user == null) {
                //    LogController.logError(core, "logout failed because new user could not be created");
                //    return;
                //}
                //
                // -- guest was created from a logout, disable autoLogin
                user.autoLogin = false;
                user.save(core.cpParent);
                //
                // -- update visit record for new user, not authenticated
                visit.memberId = user.id;
                visit.visitAuthenticated = false;
                visit.save(core.cpParent);
                //
                // -- update visitor record
                visitor.memberId = user.id;
                visitor.save(core.cpParent);
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //===================================================================================================
        /// <summary>
        /// Returns the ID of a member given their Username and Password.
        /// If the Id can not be found, user errors are added with main_AddUserError and 0 is returned (false)
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0063:Use simple 'using' statement", Justification = "<Pending>")]
        public int getUserIdForUsernameCredentials(string username, string password) {
            try {
                //
                bool allowEmailLogin = core.siteProperties.getBoolean(sitePropertyName_AllowEmailLogin);
                if (string.IsNullOrEmpty(username)) {
                    //
                    // -- username blank, stop here
                    return 0;
                }
                bool allowNoPasswordLogin = core.siteProperties.getBoolean(sitePropertyName_AllowNoPasswordLogin);
                if (string.IsNullOrEmpty(password) && !allowNoPasswordLogin) {
                    //
                    // -- password blank, stop here
                    return 0;
                }
                if (visit.loginAttempts >= core.siteProperties.maxVisitLoginAttempts) {
                    //
                    // ----- already tried 5 times
                    return 0;
                }
                string Criteria;
                if (allowEmailLogin) {
                    //
                    // login by username or email
                    //
                    Criteria = "((username=" + DbController.encodeSQLText(username) + ")or(email=" + DbController.encodeSQLText(username) + "))";
                } else {
                    //
                    // login by username only
                    //
                    Criteria = "(username=" + DbController.encodeSQLText(username) + ")";
                }
                Criteria = Criteria + "and((dateExpires is null)or(dateExpires>" + DbController.encodeSQLDate(core.dateTimeNowMockable) + "))";
                bool allowDuplicateUserNames = core.siteProperties.getBoolean("AllowDuplicateUsernames", false);
                using (var cs = new CsModel(core)) {
                    if (!cs.open("People", Criteria, "id", true, user.id, "ID,password,admin,developer", PageSize: 2)) {
                        //
                        // -- username not found, stop here
                        return 0;
                    }
                    if (allowNoPasswordLogin) {
                        //
                        // -- no password. must be one and only one result
                        if (cs.getRowCount().Equals(1)) {
                            return cs.getInteger("id");
                        }
                        return 0;
                    }
                    //
                    // -- password required
                    if ((!allowDuplicateUserNames) && (cs.getRowCount() > 1)) {
                        //
                        // -- AllowDuplicates is false, and there are more then one record
                        return 0;
                    }
                    //
                    // -- search all found records for the correct password
                    while (cs.ok()) {
                        //
                        // -- search for matching password
                        if (!string.IsNullOrEmpty(password) && password.Equals(cs.getText("password"), StringComparison.InvariantCultureIgnoreCase)) {
                            //
                            // -- password match
                            return cs.getInteger("ID");
                        }
                        if (allowNoPasswordLogin && string.IsNullOrEmpty(cs.getText("password")) && (!cs.getBoolean("admin")) && !cs.getBoolean("developer")) {
                            //
                            // -- no-password-login + no password given + account has no password + account not admin/dev/cm
                            // -- verify they are in no content manager groups
                            using (var csRules = new CsModel(core)) {
                                string SQL = ""
                                    + " SELECT ccGroupRules.ContentID"
                                    + " FROM ccGroupRules RIGHT JOIN ccMemberRules ON ccGroupRules.GroupId = ccMemberRules.GroupID"
                                    + " WHERE ("
                                    + "(ccMemberRules.memberId=" + DbController.encodeSQLNumber(cs.getInteger("ID")) + ")"
                                    + " AND(ccMemberRules.active<>0)"
                                    + " AND(ccGroupRules.active<>0)"
                                    + " AND(ccGroupRules.ContentID Is not Null)"
                                    + " AND((ccMemberRules.DateExpires is null)OR(ccMemberRules.DateExpires>" + DbController.encodeSQLDate(core.doc.profileStartTime) + "))"
                                    + ");";
                                if (csRules.openSql(SQL)) {
                                    //
                                    // -- user is a content manager, do not allow no-password logins
                                    return 0;
                                }
                            }
                        }
                        cs.goNext();
                    }
                }
                return 0;
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Checks the username and password for a new login, returns true if this can be used, returns false, and a User Error response if it can not be used
        /// </summary>
        /// <param name="core"></param>
        /// <param name="Username"></param>
        /// <param name="Password"></param>
        /// <param name="returnErrorMessage"></param>
        /// <param name="returnErrorCode"></param>
        /// <returns></returns>
        public bool isNewCredentialOK(string Username, string Password, ref string returnErrorMessage, ref int returnErrorCode) {
            try {
                bool returnOk = false;
                if (string.IsNullOrEmpty(Username)) {
                    //
                    // ----- username blank, stop here
                    returnErrorCode = 1;
                    returnErrorMessage = "A valid login requires a non-blank username.";
                } else if (string.IsNullOrEmpty(Password)) {
                    //
                    // ----- password blank, stop here
                    returnErrorCode = 4;
                    returnErrorMessage = "A valid login requires a non-blank password.";
                } else {
                    using (var csData = new CsModel(core)) {
                        if (csData.open("People", "username=" + DbController.encodeSQLText(Username), "id", false, 2, "ID")) {
                            //
                            // ----- username was found, stop here
                            returnErrorCode = 3;
                            returnErrorMessage = "The username you supplied is currently in use.";
                        } else {
                            returnOk = true;
                        }
                    }
                }
                return returnOk;
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
        /// <param name="core"></param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="AllowAutoLogin"></param>
        /// <returns></returns>
        public bool authenticate(string username, string password, bool AllowAutoLogin = false) {
            try {
                int userId = getUserIdForUsernameCredentials(username, password);
                if (!userId.Equals(0) && authenticateById(userId, this)) {
                    //
                    // -- successful
                    LogController.addSiteActivity(core, "successful login, credential [" + username + "]", user.id, user.organizationId);
                    return true;
                }
                //
                // -- failed to authenticate
                ErrorController.addUserError(core, loginFailedError);
                LogController.addSiteActivity(core, "unsuccessful login, credential [" + username + "]", user.id, user.organizationId);
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
        /// <param name="authContext"></param>
        /// <param name="requestUserAutoLogin">if true, the user must have autoLogin true to be authenticate (use for auto-login process)</param>
        /// <returns></returns>
        public static bool authenticateById(CoreController core, int userId, SessionController authContext, bool requestUserAutoLogin) {
            try {
                if (!recognizeById(core, userId, authContext, requestUserAutoLogin)) return false;
                //
                // -- recognize success, log them in to that user
                authContext.visit.visitAuthenticated = true;
                //
                // -- verify start time for visit
                if (authContext.visit.startTime != DateTime.MinValue) authContext.visit.startTime = core.doc.profileStartTime;
                //
                authContext.visit.save(core.cpParent);
                return true;
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //========================================================================
        //
        public static bool authenticateById(CoreController core, int userId, SessionController authContext)
            => authenticateById(core, userId, authContext, false);
        //
        //========================================================================
        /// <summary>
        /// Member Login By ID.
        /// </summary>
        /// <param name="core"></param>
        /// <param name="userId"></param>
        /// <param name="authContext"></param>
        /// <returns></returns>
        public bool authenticateById(int userId, SessionController authContext) => SessionController.authenticateById(core, userId, authContext, false);
        //
        //========================================================================
        /// <summary>
        /// Recognize the current member to be non-authenticated, but recognized.  Static method because it runs in constructor.
        /// </summary>
        /// <param name="core"></param>
        /// <param name="userId"></param>
        /// <param name="sessionContext"></param>
        /// <param name="requireUserAutoLogin">if true, the user must have autoLogin true to be recognized.</param>
        /// <returns></returns>
        //
        public static bool recognizeById(CoreController core, int userId, SessionController sessionContext, bool requireUserAutoLogin) {
            try {
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
                if ((sessionContext.visitor == null) || (sessionContext.visitor.id == 0)) {
                    sessionContext.visitor = DbBaseModel.addEmpty<VisitorModel>(core.cpParent);
                }
                if ((sessionContext.visit == null) || (sessionContext.visit.id == 0)) {
                    sessionContext.visit = DbBaseModel.addEmpty<VisitModel>(core.cpParent);
                }
                //
                // -- update session for recognized user
                sessionContext.user = contextUser;
                sessionContext.visitor.memberId = sessionContext.user.id;
                sessionContext.visit.memberId = sessionContext.user.id;
                sessionContext.visit.visitAuthenticated = false;
                sessionContext.visit.visitorId = sessionContext.visitor.id;
                sessionContext.visit.loginAttempts = 0;
                sessionContext.user.visits = sessionContext.user.visits + 1;
                if (sessionContext.user.visits == 1) {
                    sessionContext.visit.memberNew = true;
                } else {
                    sessionContext.visit.memberNew = false;
                }
                sessionContext.user.lastVisit = core.doc.profileStartTime;
                sessionContext.visit.excludeFromAnalytics = sessionContext.visit.excludeFromAnalytics || sessionContext.visit.bot || sessionContext.user.excludeFromAnalytics || sessionContext.user.admin || sessionContext.user.developer;
                sessionContext.visit.save(core.cpParent);
                sessionContext.visitor.save(core.cpParent);
                sessionContext.user.save(core.cpParent);
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
        /// <param name="sessionContext"></param>
        /// <returns></returns>
        public static bool recognizeById(CoreController core, int userId, SessionController sessionContext)
            => recognizeById(core, userId, sessionContext, false);
        //
        //========================================================================
        /// <summary>
        /// Recognize the current member to be non-authenticated, but recognized.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="sessionContext"></param>
        /// <returns></returns>
        //
        public bool recognizeById(int userId, ref SessionController sessionContext)
            => recognizeById(core, userId, sessionContext, false);
        //
        //====================================================================================================
        /// <summary>
        /// return true if the user is authenticated, and their people record is in member subcontext
        /// </summary>
        /// <returns></returns>
        public bool isAuthenticatedMember() {
            var userPeopleMetadata = ContentMetadataModel.create(core, user.contentControlId);
            if (userPeopleMetadata == null) { return false; }
            if (userPeopleMetadata.name.ToLower(CultureInfo.InvariantCulture) == "members") { return true; }
            var memberMetadata = ContentMetadataModel.createByUniqueName(core, "members");
            return (memberMetadata.isParentOf(core, userPeopleMetadata.id));
        }
        //
        //========================================================================
        /// <summary>
        /// is Guest
        /// </summary>
        /// <returns></returns>
        public bool isGuest() {
            return !isRecognized();
        }
        //
        //========================================================================
        /// <summary>
        /// Is Recognized (not new and not authenticted)
        /// </summary>
        /// <returns></returns>
        public bool isRecognized() {
            return !visit.memberNew;
        }
        //
        //========================================================================
        /// <summary>
        /// true if editing any content
        /// </summary>
        /// <returns></returns>
        public bool isEditing() {
            return isEditing("");
        }
        //
        //========================================================================
        /// <summary>
        /// true if editing templates or advanced editing
        /// </summary>
        /// <returns></returns>
        public bool isTemplateEditing() {
            if (!isAuthenticatedAdmin()) { return false; }
            return core.visitProperty.getBoolean("AllowTemplateEditing", false) || core.visitProperty.getBoolean("AllowAdvancedEditor", false);
        }
        //
        //========================================================================
        /// <summary>
        /// true if editing page addon lists
        /// </summary>
        /// <returns></returns>
        public bool IsPageBuilderEditing() {
            if (!isAuthenticatedAdmin()) { return false; }
            return core.visitProperty.getBoolean("AllowQuickEditor", false);
        }
        //
        //========================================================================
        /// <summary>
        /// true if developer and debugging
        /// </summary>
        /// <returns></returns>
        public bool IsDebugging() {
            if (!isAuthenticatedDeveloper()) { return false; }
            return core.visitProperty.getBoolean("AllowDebugging", false);
        }
        //
        //
        //========================================================================
        /// <summary>
        /// True if editing a specific content
        /// </summary>
        /// <param name="contentNameOrId"></param>
        /// <returns></returns>
        public bool isEditing(string contentNameOrId) {
            bool result = false;
            try {
                if (!isAuthenticated) { return false; }
                //
                // -- if empty contentid or contentName, return true if admin and editing is turned on
                if (string.IsNullOrWhiteSpace(contentNameOrId)) { return ((core.session.user.admin) || (core.session.user.developer)) && (core.visitProperty.getBoolean("AllowEditing") || core.visitProperty.getBoolean("AllowAdvancedEditor")); }
                string cacheTestName = contentNameOrId.ToLowerInvariant();
                if (core.doc.contentIsEditingList.Contains(cacheTestName)) { return true; }
                if (core.doc.contentNotEditingList.Contains(cacheTestName)) { return false; }
                if (core.visitProperty.getBoolean("AllowEditing") || core.visitProperty.getBoolean("AllowAdvancedEditor")) {
                    if (contentNameOrId.isNumeric()) {
                        contentNameOrId = MetadataController.getContentNameByID(core, encodeInteger(contentNameOrId));
                    }
                    result = isAuthenticatedContentManager(contentNameOrId);
                }
                if (result) {
                    core.doc.contentIsEditingList.Add(cacheTestName);
                } else {
                    core.doc.contentNotEditingList.Add(cacheTestName);
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
            return result;
        }
        //
        //========================================================================
        /// <summary>
        /// true if editing with the quick editor
        /// </summary>
        /// <param name="ContentName"></param>
        /// <returns></returns>
        public bool isQuickEditing(string ContentName) {
            bool returnResult = false;
            try {
                if (isAuthenticatedContentManager(ContentName)) {
                    returnResult = core.visitProperty.getBoolean("AllowQuickEditor");
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
            return returnResult;
        }
        //
        //========================================================================
        // main_IsAdvancedEditing( ContentName )
        /// <summary>
        /// true if advanded editing
        /// </summary>
        /// <param name="ContentName"></param>
        /// <returns></returns>
        public bool isAdvancedEditing() {
            // -- todo consider advancedEditing only for developers
            if ((!user.admin) && (!user.developer)) { return false; }
            return core.visitProperty.getBoolean("AllowAdvancedEditor");
        }
        //
        // ================================================================================================
        //
        public static bool isMobile(string browserUserAgent) {
            Regex b = new Regex(@"(android|bb\d+|meego).+mobile|avantgo|bada\/|blackberry|blazer|compal|elaine|fennec|hiptop|iemobile|ip(hone|od)|iris|kindle|lge |maemo|midp|mmp|mobile.+firefox|netfront|opera m(ob|in)i|palm( os)?|phone|p(ixi|re)\/|plucker|pocket|psp|series(4|6)0|symbian|treo|up\.(browser|link)|vodafone|wap|windows ce|xda|xiino", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            Regex v = new Regex(@"1207|6310|6590|3gso|4thp|50[1-6]i|770s|802s|a wa|abac|ac(er|oo|s\-)|ai(ko|rn)|al(av|ca|co)|amoi|an(ex|ny|yw)|aptu|ar(ch|go)|as(te|us)|attw|au(di|\-m|r |s )|avan|be(ck|ll|nq)|bi(lb|rd)|bl(ac|az)|br(e|v)w|bumb|bw\-(n|u)|c55\/|capi|ccwa|cdm\-|cell|chtm|cldc|cmd\-|co(mp|nd)|craw|da(it|ll|ng)|dbte|dc\-s|devi|dica|dmob|do(c|p)o|ds(12|\-d)|el(49|ai)|em(l2|ul)|er(ic|k0)|esl8|ez([4-7]0|os|wa|ze)|fetc|fly(\-|_)|g1 u|g560|gene|gf\-5|g\-mo|go(\.w|od)|gr(ad|un)|haie|hcit|hd\-(m|p|t)|hei\-|hi(pt|ta)|hp( i|ip)|hs\-c|ht(c(\-| |_|a|g|p|s|t)|tp)|hu(aw|tc)|i\-(20|go|ma)|i230|iac( |\-|\/)|ibro|idea|ig01|ikom|im1k|inno|ipaq|iris|ja(t|v)a|jbro|jemu|jigs|kddi|keji|kgt( |\/)|klon|kpt |kwc\-|kyo(c|k)|le(no|xi)|lg( g|\/(k|l|u)|50|54|\-[a-w])|libw|lynx|m1\-w|m3ga|m50\/|ma(te|ui|xo)|mc(01|21|ca)|m\-cr|me(rc|ri)|mi(o8|oa|ts)|mmef|mo(01|02|bi|de|do|t(\-| |o|v)|zz)|mt(50|p1|v )|mwbp|mywa|n10[0-2]|n20[2-3]|n30(0|2)|n50(0|2|5)|n7(0(0|1)|10)|ne((c|m)\-|on|tf|wf|wg|wt)|nok(6|i)|nzph|o2im|op(ti|wv)|oran|owg1|p800|pan(a|d|t)|pdxg|pg(13|\-([1-8]|c))|phil|pire|pl(ay|uc)|pn\-2|po(ck|rt|se)|prox|psio|pt\-g|qa\-a|qc(07|12|21|32|60|\-[2-7]|i\-)|qtek|r380|r600|raks|rim9|ro(ve|zo)|s55\/|sa(ge|ma|mm|ms|ny|va)|sc(01|h\-|oo|p\-)|sdk\/|se(c(\-|0|1)|47|mc|nd|ri)|sgh\-|shar|sie(\-|m)|sk\-0|sl(45|id)|sm(al|ar|b3|it|t5)|so(ft|ny)|sp(01|h\-|v\-|v )|sy(01|mb)|t2(18|50)|t6(00|10|18)|ta(gt|lk)|tcl\-|tdg\-|tel(i|m)|tim\-|t\-mo|to(pl|sh)|ts(70|m\-|m3|m5)|tx\-9|up(\.b|g1|si)|utst|v400|v750|veri|vi(rg|te)|vk(40|5[0-3]|\-v)|vm40|voda|vulc|vx(52|53|60|61|70|80|81|83|85|98)|w3c(\-| )|webc|whit|wi(g |nc|nw)|wmlb|wonu|x700|yas\-|your|zeto|zte\-", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            return (b.IsMatch(browserUserAgent) || v.IsMatch(browserUserAgent.Substring(0, 4)));
        }
        //
        // ================================================================================================
        /// <summary>
        /// Return true if this username/password are valid without authenticating
        /// </summary>
        /// <param name="Username"></param>
        /// <param name="Password"></param>
        /// <returns></returns>
        public bool isLoginOK(string Username, string Password) {
            return !getUserIdForUsernameCredentials(Username, Password).Equals(0);
        }
        //
        // ================================================================================================
        //
        public string getAuthoringStatusMessage(bool RecordEditLocked, string main_EditLockName, DateTime main_EditLockExpires) {
            if (!RecordEditLocked) return Msg_WorkflowDisabled;
            //
            // ----- site does not support workflow authoring
            string result = strReplace(Msg_EditLock, "<EDITNAME>", main_EditLockName);
            result = strReplace(result, "<EDITEXPIRES>", main_EditLockExpires.ToString());
            result = strReplace(result, "<EDITEXPIRESMINUTES>", encodeText(encodeInteger((main_EditLockExpires - core.doc.profileStartTime).TotalMinutes)));
            result += "<br>" + Msg_WorkflowDisabled;
            return result;
        }
        //
        //========================================================================
        /// <summary>
        /// True if the current visitor is a content manager in workflow rendering mode
        /// </summary>
        /// <returns></returns>
        public static bool isWorkflowRendering() {
            return false;
        }
    }
}