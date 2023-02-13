
using Contensive.BaseClasses;
using Contensive.Models.Db;
using Contensive.Exceptions;
using Contensive.Processor.Models.Domain;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using UAParser;
using static Contensive.Processor.Constants;
using static Contensive.Processor.Controllers.GenericController;
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
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        //
        //====================================================================================================
        /// <summary>
        /// If the session was initialize without visit tracking, use verifyUser to initialize a user.
        /// This is called automatically when an addon references cp.user.id
        /// </summary>
        public void verifyUser() {
            if (user.id == 0) {
                var user = createGuest(core, false);
                recognizeById(core, user.id, this);
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
            Logger.Trace("SessionController.create, enter-1");
            Logger.Trace(LogController.processLogMessage(core, "SessionController.create, enter-2", false));
            LogController.logTrace(core, "SessionController.create, enter-3");
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
                var visitToken = (string.IsNullOrEmpty(visitCookie)) ? new SecurityController.TokenData() : SecurityController.decodeToken(core, visitCookie);
                LogController.logTrace(core, "visitCookie [" + visitCookie + "], visitCookie.id [" + visitToken.id + "]");
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
                var visitorToken = (string.IsNullOrEmpty(visitorCookie)) ? new SecurityController.TokenData() : SecurityController.decodeToken(core, visitorCookie);
                LogController.logTrace(core, "visitorCookie [" + visitorCookie + "], visitorCookie.id [" + visitorToken.id + "]");
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
                    LogController.logTrace(core, "link authentication, linkEid [" + linkEid + "], linkToken.id [" + linkToken.id + "]");
                    if (!linkToken.id.Equals(0) && linkToken.expires.CompareTo(core.dateTimeNowMockable) < 0) {
                        //
                        // -- valid link token, attempt login/recognize
                        if (core.siteProperties.getBoolean("AllowLinkLogin", true)) {
                            //
                            // -- allow Link Login
                            LogController.logTrace(core, "attempt link Login, userid [" + linkToken.id + "]");
                            if (authenticateById(core, linkToken.id, resultSessionContext)) {
                                trackVisits = true;
                                LogController.addActivityCompletedVisit(core, "Login", "Successful link login", resultSessionContext.user.id);
                            }
                        } else if (core.siteProperties.getBoolean("AllowLinkRecognize", true)) {
                            //
                            // -- allow Link Recognize
                            LogController.logTrace(core, "attempt link recognize, userid [" + linkToken.id + "]");
                            if (recognizeById(core, linkToken.id, resultSessionContext)) {
                                trackVisits = true;
                                LogController.addActivityCompletedVisit(core, "Login", "Successful link recognize", resultSessionContext.user.id);
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
                            LogController.addActivityCompletedVisit(core, "Login", "auto-login", resultSessionContext.user.id);
                            visitor_changes = true;
                            user_changes = true;
                        }
                    } else if (core.siteProperties.allowAutoRecognize) {
                        //
                        // -- recognize by the visitor.memberid
                        if (recognizeById(core, resultSessionContext.visitor.memberId, resultSessionContext, true)) {
                            LogController.addActivityCompletedVisit(core, "Recognize", "auto-recognize", resultSessionContext.user.id);
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
                        // -- setup browser
                        if (string.IsNullOrEmpty(core.webServer.requestBrowser)) {
                            //
                            // blank browser, Blank-Browser-Bot
                            //
                            resultSessionContext.visit.name = "Blank-Browser-Bot";
                            resultSessionContext.visit.bot = true;
                            resultSessionContext.visit.mobile = false;
                            resultSessionContext.visit.browser = string.Empty;
                        } else {
                            //
                            // -- valid user-agent
                            var uaParser = Parser.GetDefault();
                            ClientInfo userAgent = uaParser.Parse(core.webServer.requestBrowser);
                            //
                            resultSessionContext.visit.browser = core.webServer.requestBrowser.substringSafe(0, 254);
                            resultSessionContext.visit.name = userAgent.Device.IsSpider ? userAgent.Device.Family + " " + userAgent.UA.Family : "user";
                            resultSessionContext.visit.bot = userAgent.Device.IsSpider;
                            resultSessionContext.visit.mobile = isMobile(core, core.webServer.requestBrowser);
                            //
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
                    if (resultSessionContext.visit.timeToLastHit < 0) { resultSessionContext.visit.timeToLastHit = 0; }
                    resultSessionContext.visit.excludeFromAnalytics |= resultSessionContext.visit.bot || resultSessionContext.user.excludeFromAnalytics || resultSessionContext.user.admin || resultSessionContext.user.developer;
                    resultSessionContext.visit.pageVisits += 1;
                    resultSessionContext.visit.cookieSupport |= !string.IsNullOrEmpty(visitCookie) || !string.IsNullOrEmpty(visitorCookie);
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
                    // -- set visitor fields needed for tracking
                    if (resultSessionContext.visitor.bot != resultSessionContext.visit.bot) {
                        resultSessionContext.visitor.bot = resultSessionContext.visit.bot;
                        visitor_changes = true;
                    }
                    if (resultSessionContext.visitor.cookieSupport != resultSessionContext.visit.cookieSupport) {
                        resultSessionContext.visitor.cookieSupport = resultSessionContext.visit.cookieSupport;
                        visitor_changes = true;
                    }

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
                        CPUtilsBaseClass.addonExecuteContext executeContext = new() {
                            addonType = CPUtilsBaseClass.addonContext.ContextOnNewVisit,
                            errorContextMessage = "new visit event running addon  [" + addon.name + "]"
                        };
                        core.addon.execute(addon, executeContext);
                    }
                }
                //
                // -- Write Visit Cookie and exit
                setVisitCookie(core, resultSessionContext);
                return resultSessionContext;
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
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
            //
            LogController.logTrace(core, "SessionController.createGuest, enter");
            //
            PersonModel user = DbBaseModel.addEmpty<PersonModel>(core.cpParent);
            user.createdByVisit = true;
            user.name = "Guest";
            user.firstName = "Guest";
            user.createdBy = user.id;
            user.dateAdded = core.doc.profileStartTime;
            user.modifiedBy = user.id;
            user.modifiedDate = core.doc.profileStartTime;
            // 23.2.12 changed from 1 to 0 because next step is always recognize, which increments
            user.visits = 0;
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
            //
            LogController.logTrace(core, "SessionController.getVisitCookie, enter");
            //
            return core.webServer.requestCookie(appNameCookiePrefix + cookieNameVisit);
        }
        //
        //========================================================================
        /// <summary>
        /// get the request visit cookie
        /// </summary>
        /// <returns></returns>
        public string getVisitorCookie() {
            //
            LogController.logTrace(core, "SessionController.getVisitorCookie, enter");
            //
            return core.webServer.requestCookie(appNameCookiePrefix + cookieNameVisitor);
        }
        //
        //========================================================================
        /// <summary>
        /// sets the visit cookie based on the sessionContext (visit.id)
        /// </summary>
        /// <param name="core"></param>
        /// <param name="sessionContext"></param>
        public static void setVisitCookie(CoreController core, SessionController sessionContext) {
            //
            LogController.logTrace(core, "SessionController.setVisitCookie, enter");
            //
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
            //
            LogController.logTrace(core, "SessionController.setVisitorCookie, enter");
            //
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
                //
                LogController.logTrace(core, "SessionController.isAuthenticatedAdmin, enter");
                //
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
                //
                LogController.logTrace(core, "SessionController.isAuthenticatedDeveloper, enter");
                //
                return visit.visitAuthenticated && user.developer;
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
                //
                LogController.logTrace(core, "SessionController.isAuthenticatedContentManager, enter");
                //
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
                //
                LogController.logTrace(core, "SessionController.isAuthenticatedContentManager, enter");
                //
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
                //
                LogController.logTrace(core, "SessionController.isAuthenticatedContentManager, enter");
                //
                if (core.session.isAuthenticatedAdmin()) { return true; }
                if (_isAuthenticatedContentManagerAnything_loaded && _isAuthenticatedContentManagerAnything_userId.Equals(user.id)) { return _isAuthenticatedContentManagerAnything; }
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
        /// Test the username and password against users and return the userId of the match, or 0 if not valid match
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="requestIncludesPassword">If true, the noPassword option is disabled</param>
        /// <returns></returns>
        public int getUserIdForUsernameCredentials(string username, string password, bool requestIncludesPassword) {
            try {
                //
                LogController.logTrace(core, "SessionController.getUserIdForUsernameCredentials enter");
                //
                if (string.IsNullOrEmpty(username)) {
                    //
                    // -- username blank, stop here
                    LogController.logTrace(core, "SessionController.getUserIdForUsernameCredentials fail, username blank");
                    return 0;
                }
                bool allowNoPassword = !requestIncludesPassword && core.siteProperties.getBoolean(sitePropertyName_AllowNoPasswordLogin);
                if (string.IsNullOrEmpty(password) && !allowNoPassword) {
                    //
                    // -- password blank, stop here
                    LogController.logTrace(core, "SessionController.getUserIdForUsernameCredentials fail, password blank");
                    return 0;
                }
                if (visit.loginAttempts >= core.siteProperties.maxVisitLoginAttempts) {
                    //
                    // ----- already tried 5 times
                    LogController.logTrace(core, "SessionController.getUserIdForUsernameCredentials fail, maxVisitLoginAttempts reached");
                    return 0;
                }
                string Criteria;
                bool allowEmailLogin = core.siteProperties.getBoolean(sitePropertyName_AllowEmailLogin);
                if (allowEmailLogin) {
                    //
                    // -- login by username or email
                    LogController.logTrace(core, "SessionController.getUserIdForUsernameCredentials, attempt email login");
                    Criteria = "((username=" + DbController.encodeSQLText(username) + ")or(email=" + DbController.encodeSQLText(username) + "))";
                } else {
                    //
                    // -- login by username only
                    LogController.logTrace(core, "SessionController.getUserIdForUsernameCredentials, attempt username login");
                    Criteria = "(username=" + DbController.encodeSQLText(username) + ")";
                }
                Criteria += "and((dateExpires is null)or(dateExpires>" + DbController.encodeSQLDate(core.dateTimeNowMockable) + "))";
                string peopleFieldList = "ID,password,passwordHash,admin,developer,ccguid";
                bool allowPlainTextPassword = core.siteProperties.getBoolean(sitePropertyName_AllowPlainTextPassword, true);
                using (var cs = new CsModel(core)) {
                    if (!cs.open("People", Criteria, "id", true, user.id, peopleFieldList, PageSize: 2)) {
                        //
                        // -- fail, username not found, stop here
                        LogController.logTrace(core, "SessionController.getUserIdForUsernameCredentials fail, user record not found");
                        return 0;
                    }
                    if (cs.getRowCount() > 1) {
                        //
                        // -- fail, multiple matches
                        LogController.logTrace(core, "SessionController.getUserIdForUsernameCredentials fail, multiple users found");
                        return 0;
                    }
                    if (!allowNoPassword) {
                        //
                        // -- password mode
                        if (string.IsNullOrEmpty(password)) {
                            //
                            // -- fail, no password
                            LogController.logTrace(core, "SessionController.getUserIdForUsernameCredentials fail, blank password");
                            return 0;
                        }
                        if (allowPlainTextPassword) {
                            //
                            // -- legacy plain text password mode
                            if (password.Equals(cs.getText("password"), StringComparison.InvariantCultureIgnoreCase)) {
                                //
                                // -- success, password match
                                LogController.logTrace(core, "SessionController.getUserIdForUsernameCredentials success, pw match");
                                return cs.getInteger("ID");
                            }
                            //
                            // -- fail, plain text
                            LogController.logTrace(core, "SessionController.getUserIdForUsernameCredentials fail, blank or incorrect pw");
                            return 0;
                        } else {
                            //
                            // -- test encrypted password entered with passwordHash saved
                            string passwordHash = SecurityController.encryptOneWay(core, password, cs.getText("ccguid"));
                            if (passwordHash.Equals(cs.getText("passwordHash"), StringComparison.InvariantCultureIgnoreCase)) {
                                //
                                // -- success, encrypted password match
                                LogController.logTrace(core, "SessionController.getUserIdForUsernameCredentials success, pw match");
                                return cs.getInteger("ID");
                            }
                            if (!core.siteProperties.getBoolean(sitePropertyName_AllowPlainTextPasswordHash, true)) {
                                //
                                // -- migration mode disabled, encrypted password fail
                                LogController.logTrace(core, "SessionController.getUserIdForUsernameCredentials fail, blank or incorrect pw");
                                return 0;
                            }
                            if (!string.IsNullOrEmpty(cs.getText("password")) && string.IsNullOrEmpty(cs.getText("passwordhash")) && password.Equals(cs.getText("password"), StringComparison.InvariantCultureIgnoreCase)) {
                                //
                                // -- migration model -- password matches plain text password, no hash, allow 1-time and migrate
                                cs.set("passwordHash", passwordHash);
                                cs.set("password", "");
                                cs.save();
                                LogController.logTrace(core, "SessionController.getUserIdForUsernameCredentials success, pw migration");
                                return cs.getInteger("ID");
                            }
                            //
                            // -- fail, hash password
                            LogController.logTrace(core, "SessionController.getUserIdForUsernameCredentials fail, blank or incorrect pw");
                            return 0;
                        }
                    }
                    //
                    // -- no-password mode
                    if (cs.getBoolean("admin") || cs.getBoolean("developer")) {
                        //
                        // -- fail, no-password-mode and match is admin/dev
                        LogController.logTrace(core, "SessionController.getUserIdForUsernameCredentials fail, no-pw mode matched admin/dev");
                        return 0;
                    }
                    //
                    // -- no-password auth cannot be content manager
                    using var csRules = new CsModel(core);
                    string SQL = ""
                        + " select ccGroupRules.ContentID"
                        + " from ccGroupRules right join ccMemberRules ON ccGroupRules.GroupId = ccMemberRules.GroupID"
                        + " where (1=1)"
                        + " and(ccMemberRules.memberId=" + cs.getInteger("ID") + ")"
                        + " and(ccMemberRules.active>0)"
                        + " and(ccGroupRules.active>0)"
                        + " and(ccGroupRules.ContentID Is not Null)"
                        + " and((ccMemberRules.DateExpires is null)OR(ccMemberRules.DateExpires>" + DbController.encodeSQLDate(core.doc.profileStartTime) + "))"
                        + ");";
                    if (!csRules.openSql(SQL)) {
                        //
                        // -- success, match is not content manager
                        LogController.logTrace(core, "SessionController.getUserIdForUsernameCredentials fail, no-pw mode did not match content manager");
                        return cs.getInteger("ID");
                    }
                }
                //
                LogController.logTrace(core, "SessionController.getUserIdForUsernameCredentials fail, exit with no  match");
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
                //
                LogController.logTrace(core, "SessionController.isNewCredentialOK enter");
                //
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
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="setUserAutoLogin"></param>
        /// <returns></returns>
        public bool authenticate(string username, string password, bool setUserAutoLogin) {
            try {
                //
                LogController.logTrace(core, "SessionController.authenticate enter");
                //
                int userId = getUserIdForUsernameCredentials(username, password, false);
                if (!userId.Equals(0) && authenticateById(userId, this)) {
                    //
                    // -- successful
                    LogController.addActivityCompletedVisit(core, "Login", "successful login, credential [" + username + "]", user.id);
                    //
                    core.db.executeNonQuery("update ccmembers set autoLogin=" + (setUserAutoLogin ? "1" : "0") + " where id=" + userId);
                    return true;
                }
                //
                // -- failed to authenticate
                ErrorController.addUserError(core, loginFailedError);
                //
                // -- pause to make brute force attempt for expensive
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
        /// <param name="authContext"></param>
        /// <param name="requestUserAutoLogin">if true, the user must have autoLogin true to be authenticate (use for auto-login process)</param>
        /// <returns></returns>
        public static bool authenticateById(CoreController core, int userId, SessionController authContext, bool requestUserAutoLogin) {
            try {
                //
                LogController.logTrace(core, "SessionController.authenticateById, enter, userid [" + userId + "]");
                //
                if (userId == 0) { return false; }
                if (!recognizeById(core, userId, authContext, requestUserAutoLogin)) {
                    //
                    // -- pause to make brute force attempt for expensive
                    Thread.Sleep(3000);
                    //
                    return false;
                }
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
        ///
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
                LogController.logTrace(core, "SessionController.recognizeById, enter");
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
            //
            LogController.logTrace(core, "SessionController.isAuthenticatedMember, enter");
            //
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
            //
            LogController.logTrace(core, "SessionController.isGuest, enter");
            //
            return !isRecognized();
        }
        //
        //========================================================================
        /// <summary>
        /// Is Recognized (not new and not authenticted)
        /// </summary>
        /// <returns></returns>
        public bool isRecognized() {
            //
            LogController.logTrace(core, "SessionController.isRecognized, enter");
            //
            return !visit.memberNew;
        }
        //
        //========================================================================
        /// <summary>
        /// true if editing enabled and user is admin
        /// </summary>
        /// <returns></returns>
        public bool isEditing() {
            try {
                if (isEditingLocal != null) { return (bool)isEditingLocal; }
                //
                // -- return true if admin and editing is turned on
                if (!isAuthenticated) {
                    isEditingLocal = false;
                    return false;
                }
                bool editingSiteProperty = core.visitProperty.getBoolean("AllowEditing") || core.visitProperty.getBoolean("AllowAdvancedEditor");
                isEditingLocal = editingSiteProperty && (core.session.user.admin || core.session.user.developer);
                return (bool)isEditingLocal;
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        private bool? isEditingLocal = null;
        //
        //========================================================================
        /// <summary>
        /// true if editing the content from content id. If contentId is 0, or not a valid content, return false
        /// </summary>
        /// <param name="contentId"></param>
        /// <returns></returns>
        public bool isEditing_ContentId(int contentId) {
            try {
                //
                LogController.logTrace(core, "SessionController.isEditing, contentid [" + contentId + "]");
                //
                if (contentId <= 0) { return false; }
                if (!isAuthenticated) { return false; }
                string contentName = MetadataController.getContentNameByID(core, contentId);
                if (string.IsNullOrEmpty(contentName)) { return false; }
                return isEditing_ContentName(contentName);
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //========================================================================
        /// <summary>
        /// true if editing the content from content name. If contentName is blank, returns true of admin.
        /// </summary>
        /// <param name="contentName"></param>
        /// <returns></returns>
        public bool isEditing_ContentName(string contentName) {
            try {
                if (string.IsNullOrEmpty(contentName)) { return isEditing(); }
                //
                LogController.logTrace(core, "SessionController.isEditing [" + contentName + "]");
                //
                // -- if empty contentid or contentName, return true if admin and editing is turned on
                if (!isAuthenticated) { return false; }
                bool editingSiteProperty = core.visitProperty.getBoolean("AllowEditing") || core.visitProperty.getBoolean("AllowAdvancedEditor");
                if (!editingSiteProperty) { return false; }
                if (editingSiteProperty && (core.session.user.admin || core.session.user.developer)) { return true; }
                //
                // -- is editing content (id or name)
                string contentNameLc = contentName.ToLowerInvariant();
                if (core.doc.contentIsEditingList.Contains(contentNameLc)) { return true; }
                if (core.doc.contentNotEditingList.Contains(contentNameLc)) { return false; }
                if (isAuthenticatedContentManager(contentNameLc)) {
                    core.doc.contentIsEditingList.Add(contentNameLc);
                    return true;
                }
                core.doc.contentNotEditingList.Add(contentNameLc);
                return false;
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //========================================================================
        /// <summary>
        /// True if editing a specific content
        /// </summary>
        /// <param name="contentNameOrId"></param>
        /// <returns></returns>
        public bool isEditing(string contentNameOrId) {
            try {
                if (string.IsNullOrEmpty(contentNameOrId)) { return isEditing(); }
                if (contentNameOrId.isNumeric()) { return isEditing_ContentId(encodeInteger(contentNameOrId)); }
                return isEditing_ContentName(contentNameOrId);
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //========================================================================
        /// <summary>
        /// true if editing templates or advanced editing
        /// </summary>
        /// <returns></returns>
        public bool isTemplateEditing() {
            //
            LogController.logTrace(core, "SessionController.isTemplateEditing, enter");
            //
            if (!isAuthenticatedAdmin()) { return false; }
            return core.visitProperty.getBoolean("AllowTemplateEditing", false) || core.visitProperty.getBoolean("AllowAdvancedEditor", false);
        }
        //
        //========================================================================
        /// <summary>
        /// true if editing page addon lists
        /// </summary>
        /// <returns></returns>
        public bool isPageBuilderEditing() {
            //
            LogController.logTrace(core, "SessionController.isPageBuilderEditing, enter");
            //
            if (!isAuthenticatedAdmin()) { return false; }
            return core.visitProperty.getBoolean("AllowQuickEditor", false);
        }
        //
        //========================================================================
        /// <summary>
        /// true if developer and debugging
        /// </summary>
        /// <returns></returns>
        public bool isDebugging() {
            //
            LogController.logTrace(core, "SessionController.IsDebugging, enter");
            //
            if (!isAuthenticatedDeveloper()) { return false; }
            return core.visitProperty.getBoolean("AllowDebugging", false);
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
                //
                LogController.logTrace(core, "SessionController.isQuickEditing, enter");
                //
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
            //
            LogController.logTrace(core, "SessionController.isAdvancedEditing, enter");
            //
            // -- todo consider advancedEditing only for developers
            if ((!user.admin) && (!user.developer)) { return false; }
            return core.visitProperty.getBoolean("AllowAdvancedEditor");
        }
        //
        // ================================================================================================
        //
        public static bool isMobile(CoreController core, string browserUserAgent) {
            //
            LogController.logTrace(core, "SessionController.isMobile, enter");
            //
            Regex b = new(@"(android|bb\d+|meego).+mobile|avantgo|bada\/|blackberry|blazer|compal|elaine|fennec|hiptop|iemobile|ip(hone|od)|iris|kindle|lge |maemo|midp|mmp|mobile.+firefox|netfront|opera m(ob|in)i|palm( os)?|phone|p(ixi|re)\/|plucker|pocket|psp|series(4|6)0|symbian|treo|up\.(browser|link)|vodafone|wap|windows ce|xda|xiino", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            Regex v = new(@"1207|6310|6590|3gso|4thp|50[1-6]i|770s|802s|a wa|abac|ac(er|oo|s\-)|ai(ko|rn)|al(av|ca|co)|amoi|an(ex|ny|yw)|aptu|ar(ch|go)|as(te|us)|attw|au(di|\-m|r |s )|avan|be(ck|ll|nq)|bi(lb|rd)|bl(ac|az)|br(e|v)w|bumb|bw\-(n|u)|c55\/|capi|ccwa|cdm\-|cell|chtm|cldc|cmd\-|co(mp|nd)|craw|da(it|ll|ng)|dbte|dc\-s|devi|dica|dmob|do(c|p)o|ds(12|\-d)|el(49|ai)|em(l2|ul)|er(ic|k0)|esl8|ez([4-7]0|os|wa|ze)|fetc|fly(\-|_)|g1 u|g560|gene|gf\-5|g\-mo|go(\.w|od)|gr(ad|un)|haie|hcit|hd\-(m|p|t)|hei\-|hi(pt|ta)|hp( i|ip)|hs\-c|ht(c(\-| |_|a|g|p|s|t)|tp)|hu(aw|tc)|i\-(20|go|ma)|i230|iac( |\-|\/)|ibro|idea|ig01|ikom|im1k|inno|ipaq|iris|ja(t|v)a|jbro|jemu|jigs|kddi|keji|kgt( |\/)|klon|kpt |kwc\-|kyo(c|k)|le(no|xi)|lg( g|\/(k|l|u)|50|54|\-[a-w])|libw|lynx|m1\-w|m3ga|m50\/|ma(te|ui|xo)|mc(01|21|ca)|m\-cr|me(rc|ri)|mi(o8|oa|ts)|mmef|mo(01|02|bi|de|do|t(\-| |o|v)|zz)|mt(50|p1|v )|mwbp|mywa|n10[0-2]|n20[2-3]|n30(0|2)|n50(0|2|5)|n7(0(0|1)|10)|ne((c|m)\-|on|tf|wf|wg|wt)|nok(6|i)|nzph|o2im|op(ti|wv)|oran|owg1|p800|pan(a|d|t)|pdxg|pg(13|\-([1-8]|c))|phil|pire|pl(ay|uc)|pn\-2|po(ck|rt|se)|prox|psio|pt\-g|qa\-a|qc(07|12|21|32|60|\-[2-7]|i\-)|qtek|r380|r600|raks|rim9|ro(ve|zo)|s55\/|sa(ge|ma|mm|ms|ny|va)|sc(01|h\-|oo|p\-)|sdk\/|se(c(\-|0|1)|47|mc|nd|ri)|sgh\-|shar|sie(\-|m)|sk\-0|sl(45|id)|sm(al|ar|b3|it|t5)|so(ft|ny)|sp(01|h\-|v\-|v )|sy(01|mb)|t2(18|50)|t6(00|10|18)|ta(gt|lk)|tcl\-|tdg\-|tel(i|m)|tim\-|t\-mo|to(pl|sh)|ts(70|m\-|m3|m5)|tx\-9|up(\.b|g1|si)|utst|v400|v750|veri|vi(rg|te)|vk(40|5[0-3]|\-v)|vm40|voda|vulc|vx(52|53|60|61|70|80|81|83|85|98)|w3c(\-| )|webc|whit|wi(g |nc|nw)|wmlb|wonu|x700|yas\-|your|zeto|zte\-", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            if (string.IsNullOrEmpty(browserUserAgent) || browserUserAgent.Length < 4) { return false; }
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
            //
            LogController.logTrace(core, "SessionController.isLoginOK, enter");
            //
            return !getUserIdForUsernameCredentials(Username, Password, false).Equals(0);
        }
        //
        // ================================================================================================
        //
        public string getAuthoringStatusMessage(bool RecordEditLocked, string main_EditLockName, DateTime main_EditLockExpires) {
            //
            LogController.logTrace(core, "SessionController.getAuthoringStatusMessage, enter");
            //
            if (!RecordEditLocked) { return Msg_WorkflowDisabled; }
            //
            // ----- site does not support workflow authoring
            string result = strReplace(Msg_EditLock, "<EDITNAME>", main_EditLockName);
            result = strReplace(result, "<EDITEXPIRES>", main_EditLockExpires.ToString());
            result = strReplace(result, "<EDITEXPIRESMINUTES>", encodeText(encodeInteger((main_EditLockExpires - core.doc.profileStartTime).TotalMinutes)));
            result += "<br>" + Msg_WorkflowDisabled;
            return result;
        }
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    }
}