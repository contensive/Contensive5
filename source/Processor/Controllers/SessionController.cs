
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
using System.Text;
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
                AuthenticationController.recognizeById(core, this, user.id);
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
        public SessionController(CoreController core, bool trackVisits) {
            this.core = core;
            visit = new VisitModel();
            visitor = new VisitorModel();
            user = new PersonModel();
            visitStateOk = true;
            //
            Logger.Trace("SessionController.create, enter-1");
            Logger.Trace(LogController.processLogMessage(core, "SessionController.create, enter-2", false));
            LogController.logTrace(core, "SessionController.create, enter-3");
            //
            try {
                //
                // --argument testing
                if (core.serverConfig == null) {
                    //
                    // -- application error if no server config
                    LogController.logError(core, new GenericException("authorization context cannot be created without a server configuration."));
                    return;
                }
                if (core.appConfig == null) {
                    //
                    // -- no application, this is a server-only call not related to a 
                    LogController.logTrace(core, "app.config null, create server session");
                    return;
                }
                //
                // -- load visit state from cookie
                //
                string visitCookie = getVisitCookie();
                SecurityController.TokenData visitToken = (string.IsNullOrEmpty(visitCookie)) ? new SecurityController.TokenData() : SecurityController.decodeToken(core, visitCookie);
                LogController.logTrace(core, "visitCookie [" + visitCookie + "], visitCookie.id [" + visitToken.id + "]");
                if (!visitToken.id.Equals(0)) {
                    VisitModel visitTest = DbBaseModel.create<VisitModel>(core.cpParent, visitToken.id);
                    if (visitTest is not null) {
                        visit = visitTest;
                        trackVisits = true;
                        // todo - incorrect. This should be visittoken.created, token must be enhanced to support visitStateOK. consider cookie length and other effects
                        visitStateOk = (visitToken.expires - encodeDate(visit.lastVisitTime)).TotalSeconds < 2;
                    }
                }
                //
                // -- load visitor state from cookie
                //
                string visitorCookie = getVisitorCookie();
                var visitorToken = (string.IsNullOrEmpty(visitorCookie)) ? new SecurityController.TokenData() : SecurityController.decodeToken(core, visitorCookie);
                LogController.logTrace(core, "visitorCookie [" + visitorCookie + "], visitorCookie.id [" + visitorToken.id + "]");
                if (!visitorToken.id.Equals(0)) {
                    VisitorModel visitorTest = DbBaseModel.create<VisitorModel>(core.cpParent, visitorToken.id);
                    if (!(visitorTest is null)) {
                        visitor = visitorTest;
                    }
                }
                bool resultSessionContect_visitor_changes = false;
                bool resultSessionContext_user_changes = false;
                //
                // -- setup session from user,visit,visitor
                //
                bool AllowOnNewVisitEvent = false;
                if (trackVisits) {
                    //
                    // -- verify session visitor
                    //
                    bool visitorNew = false;
                    if (visit != null) {
                        if ((visit.visitorId > 0) && (!visit.visitorId.Equals(visitor.id))) {
                            //
                            // -- visit.visitor overrides cookie visitor
                            VisitorModel testVisitor = DbBaseModel.create<VisitorModel>(core.cpParent, visit.visitorId);
                            if (!(testVisitor is null)) {
                                visitor = testVisitor;
                                visit.visitorId = visitor.id;
                                resultSessionContect_visitor_changes = true;
                                setVisitorCookie(core, this);
                            }
                        }
                    }
                    if ((visitor == null) || visitor.id.Equals(0)) {
                        //
                        // -- create new visitor
                        visitor = DbBaseModel.addEmpty<VisitorModel>(core.cpParent);
                        visitorNew = true;
                        resultSessionContect_visitor_changes = true;
                    }
                    //
                    // -- verify session visit
                    //
                    bool createNewVisit = false;
                    if ((visit == null) || (visit.id.Equals(0))) {
                        //
                        // -- visit record is missing, create a new visit
                        LogController.logTrace(core, "visit invalid, create a new visit");
                        createNewVisit = true;
                    } else if ((visit.lastVisitTime != null) && (encodeDate(visit.lastVisitTime).AddHours(1) < core.doc.profileStartTime)) {
                        //
                        // -- visit has expired, create new visit
                        LogController.logTrace(core, "visit expired, create new visit");
                        createNewVisit = true;
                    }
                    if (createNewVisit) {
                        visit = DbBaseModel.addEmpty<VisitModel>(core.cpParent);
                        visit.startTime = core.doc.profileStartTime;
                        if (string.IsNullOrEmpty(visit.name)) {
                            visit.name = "User";
                        }
                        visit.startTime = core.doc.profileStartTime;
                        visit.remote_addr = core.webServer.requestRemoteIP;
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
                                visit.refererPathPage = "";
                                visit.http_referer = WorkingReferer.substringSafe(0, 254);
                            } else {
                                visit.refererPathPage = WorkingReferer.Substring(SlashPosition - 1).substringSafe(0, 254);
                                visit.http_referer = WorkingReferer.left(SlashPosition - 1).substringSafe(0, 254);
                            }
                        }
                        //
                        // -- setup browser
                        if (string.IsNullOrEmpty(core.webServer.requestBrowser)) {
                            //
                            // blank browser, Blank-Browser-Bot
                            //
                            visit.name = "Blank-Browser-Bot";
                            visit.bot = true;
                            visit.mobile = false;
                            visit.browser = string.Empty;
                        } else {
                            //
                            // -- valid user-agent
                            var uaParser = Parser.GetDefault();
                            ClientInfo userAgent = uaParser.Parse(core.webServer.requestBrowser);
                            //
                            visit.browser = core.webServer.requestBrowser.substringSafe(0, 254);
                            visit.name = userAgent.Device.IsSpider ? userAgent.Device.Family + " " + userAgent.UA.Family : "user";
                            visit.bot = userAgent.Device.IsSpider;
                            visit.mobile = isMobile(core, core.webServer.requestBrowser);
                            //
                        }
                        //
                        // -- new visit, update the persistant visitor cookie
                        setVisitorCookie(core, this);
                        //
                        // -- new visit, OnNewVisit Add-on call
                        AllowOnNewVisitEvent = true;
                    }
                    //
                    // -- visit object is valid, update details
                    visit.timeToLastHit = encodeInteger((core.doc.profileStartTime - encodeDate(visit.startTime)).TotalSeconds);
                    if (visit.timeToLastHit < 0) { visit.timeToLastHit = 0; }
                    visit.excludeFromAnalytics |= visit.bot || user.excludeFromAnalytics || user.admin || user.developer;
                    visit.pageVisits += 1;
                    visit.cookieSupport |= !string.IsNullOrEmpty(visitCookie) || !string.IsNullOrEmpty(visitorCookie);
                    visit.lastVisitTime = core.doc.profileStartTime;
                    visit.visitorNew = visitorNew;
                    visit.visitorId = visitor.id;
                    //
                    // -- verify user identity
                    //
                    if ((user is null) || user.id.Equals(0)) {
                        //
                        // -- setup visit user if not authenticated
                        if (!visit.memberId.Equals(0)) {
                            user = DbBaseModel.create<PersonModel>(core.cpParent, visit.memberId);
                        }
                        //
                        // -- setup new user if nothing else
                        if ((user is null) || user.id.Equals(0)) {
                            user = createGuest(core, true);
                            resultSessionContext_user_changes = true;
                            //
                            visit.visitAuthenticated = false;
                            visit.memberNew = true;
                            visit.memberId = user.id;
                            //
                            visitor.memberId = user.id;
                            resultSessionContect_visitor_changes = true;
                        }
                    }
                    //
                    // -- check for changes in session data consistency
                    //
                    if (visit.createdBy.Equals(0)) {
                        visit.createdBy = user.id;
                        visit.modifiedBy = user.id;
                    }
                    visit.memberId = user.id;
                    visit.visitorId = visitor.id;
                    //
                    // -- set visitor fields needed for tracking
                    if (visitor.bot != visit.bot) {
                        visitor.bot = visit.bot;
                        resultSessionContect_visitor_changes = true;
                    }
                    if (visitor.cookieSupport != visit.cookieSupport) {
                        visitor.cookieSupport = visit.cookieSupport;
                        resultSessionContect_visitor_changes = true;
                    }

                    //
                    // -- Save anything that changed
                    //
                    visit.save(core.cpParent, 0, true);
                    if (resultSessionContect_visitor_changes) {
                        visitor.save(core.cpParent, 0, true);
                    }
                    if (resultSessionContext_user_changes) {
                        user.save(core.cpParent, 0, true);
                    }
                }
                //
                // -- execute onNewVisit addons
                //
                if (AllowOnNewVisitEvent) {
                    LogController.logTrace(core, "execute NewVisit Event");
                    foreach (var addon in core.cacheRuntime.addonCache.getOnNewVisitAddonList()) {
                        CPUtilsBaseClass.addonExecuteContext executeContext = new() {
                            addonType = CPUtilsBaseClass.addonContext.ContextOnNewVisit,
                            errorContextMessage = "new visit event running addon  [" + addon.name + "]"
                        };
                        core.addon.execute(addon, executeContext);
                    }
                }
                //
                // -- Write Visit Cookie and exit
                setVisitCookie(core, this);
                return;
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