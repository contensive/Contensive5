
using Contensive.Models.Db;
using Contensive.Processor.Exceptions;
using Contensive.Processor.Models.Domain;
using Microsoft.Web.Administration;
using System;
using System.Collections.Generic;
using System.Linq;
using static Contensive.Processor.Constants;
using static Contensive.Processor.Controllers.GenericController;

namespace Contensive.Processor.Controllers {
    /// <summary>
    /// Code dedicated to processing webserver (iis for windows) input and output. lazy Constructed. (see coreHtmlClass for html processing)
    /// What belongs here is everything that would have to change if we converted to apache
    /// </summary>
    public class WebServerController {
        //
        //====================================================================================================
        // enum this, not consts --  https://en.wikipedia.org/wiki/List_of_HTTP_status_codes
        /// <summary>
        /// 200
        /// </summary>
        public static readonly string httpResponseStatus200_Success = "200 OK";
        /// <summary>
        /// 401 unauthorized
        /// </summary>
        public static readonly string httpResponseStatus401_Unauthorized = "401 Unauthorized";
        /// <summary>
        /// 403 forbidden
        /// </summary>
        public static readonly string httpResponseStatus403_Forbidden = "403 Forbidden";
        /// <summary>
        /// 404 not found
        /// </summary>
        public static readonly string httpResponseStatus404_NotFound = "404 Not Found";
        /// <summary>
        /// 500 server error
        /// </summary>
        public static readonly string httpResponseStatus500_ServerError = "500 Internal Server Error";
        //
        //====================================================================================================
        //
        private CoreController core { get; }
        //
        //====================================================================================================
        /// <summary>
        /// The context of the optional http client that may have created this object.
        /// </summary>
        public HttpContextModel httpContext { get; set; } = null;
        //
        //====================================================================================================
        /// <summary>
        /// request port
        /// </summary>
        public int requestPort {
            get {
                if ((httpContext == null) || (httpContext.Request == null) || (httpContext.Request.Url == null)) return 0;
                if (_requestPort == null) {
                    _requestPort = httpContext.Request.Url.Port;
                }
                return (int)_requestPort;
            }
        }
        private int? _requestPort = null;
        //
        // ====================================================================================================
        /// <summary>
        /// The path and page of the current request, without the leading slash which comes from the appRootPath
        /// </summary>
        public string requestPathPage {
            get {
                if (string.IsNullOrEmpty(_requestPathPage)) {
                    if ((httpContext == null) || (httpContext.Request == null) || (httpContext.Request.ServerVariables == null)) return string.Empty;
                    _requestPathPage = httpContext.Request.ServerVariables.ContainsKey("SCRIPT_NAME") ? core.webServer.httpContext.Request.ServerVariables["SCRIPT_NAME"] : "";
                }
                return _requestPathPage;
            }
        }
        private string _requestPathPage;
        //
        // ====================================================================================================
        // todo convert request variables on-demand pattern.
        /// <summary>
        /// The refering URL
        /// </summary>
        public string requestReferrer {
            get {
                if ((httpContext == null) || (httpContext.Request == null) || (httpContext.Request.ServerVariables == null)) return string.Empty;
                return (httpContext.Request.ServerVariables.ContainsKey("HTTP_REFERER")) ? core.webServer.httpContext.Request.ServerVariables["HTTP_REFERER"] : "";
            }
        }
        //
        // ====================================================================================================
        /// <summary>
        /// The domain part of the current request URL
        /// </summary>
        public string requestDomain {
            get {
                if ((httpContext == null) || (httpContext.Request == null) || (httpContext.Request.ServerVariables == null)) return string.Empty;
                return (core.webServer.httpContext.Request.ServerVariables.ContainsKey("SERVER_NAME")) ? core.webServer.httpContext.Request.ServerVariables["SERVER_NAME"] : "";
            }
        }
        //
        // ====================================================================================================
        /// <summary>
        /// true if the current request is secure
        /// </summary>
        public bool requestSecure {
            get {
                if (_requestSecure != null) { return (bool)_requestSecure; }
                if ((httpContext == null) || (httpContext.Request == null) || (httpContext.Request.ServerVariables == null)) return false;
                _requestSecure = core.webServer.httpContext.Request.ServerVariables.ContainsKey("SERVER_PORT_SECURE") && encodeBoolean(core.webServer.httpContext.Request.ServerVariables["SERVER_PORT_SECURE"]);
                return (bool)_requestSecure;
            }
        }
        private bool? _requestSecure = null;
        //
        // ====================================================================================================
        /// <summary>
        /// Legacy property - user's IP
        /// </summary>
        public string requestRemoteIP {
            get {
                if ((httpContext == null) || (httpContext.Request == null) || (httpContext.Request.ServerVariables == null)) return string.Empty;
                return (core.webServer.httpContext.Request.ServerVariables.ContainsKey("REMOTE_ADDR")) ? core.webServer.httpContext.Request.ServerVariables["REMOTE_ADDR"] : "";
            }
        }
        //
        // ====================================================================================================
        /// <summary>
        /// Legacy property - the browser
        /// </summary>
        public string requestBrowser {
            get {
                if ((httpContext == null) || (httpContext.Request == null) || (httpContext.Request.ServerVariables == null)) return string.Empty;
                return (core.webServer.httpContext.Request.ServerVariables.ContainsKey("HTTP_USER_AGENT")) ? core.webServer.httpContext.Request.ServerVariables["HTTP_USER_AGENT"] : "";
            }
        }
        //
        // ====================================================================================================
        /// <summary>
        /// The QueryString of the current URI
        /// </summary>
        public string requestQueryString {
            get {
                if (_requestQueryString != null) { return _requestQueryString; }
                if ((httpContext == null) || (httpContext.Request == null) || (httpContext.Request.QueryString == null)) return string.Empty;
                _requestQueryString = "";
                string delimiter = "";
                foreach (var qsPair in httpContext.Request.QueryString) {
                    _requestQueryString += delimiter + qsPair.Key + "=" + qsPair.Value;
                    delimiter = "&";
                }
                return _requestQueryString;
            }
        }
        private string _requestQueryString { get; set; } = null;
        //
        // ====================================================================================================
        /// <summary>
        /// The url as it was requested. if the url is a route, this is the original route call and requestUrl is the path it is routed to.
        /// </summary>
        public string requestUrlSource {
            get {
                if (_requestUrlSource != null) { return _requestUrlSource; }
                if ((httpContext == null) || (httpContext.Request == null) || (httpContext.Request.Url == null)) return string.Empty;
                _requestUrlSource = httpContext.Request.Url.AbsoluteUri;
                return _requestUrlSource;
            }
        }
        private string _requestUrlSource = null;
        //
        // ====================================================================================================
        /// <summary>
        /// source Url
        /// </summary>
        public string requestLinkForwardSource {
            get {
                if ((httpContext == null) || (httpContext.Request == null) || (httpContext.Request.Url == null)) return string.Empty;
                _requestUrlSource = httpContext.Request.Url.AbsoluteUri;
                return _requestUrlSource;
            }
        }
        //
        // ====================================================================================================
        /// <summary>
        /// if true, viewing saved with this property
        /// </summary>
        public bool pageExcludeFromAnalytics {
            get {
                return _pageExcludeFromAnalytics;
            }
            set {
                _pageExcludeFromAnalytics = value;
            }
        }
        private bool _pageExcludeFromAnalytics = false;
        //
        // ====================================================================================================
        /// <summary>
        /// for more information message
        /// </summary>
        public string adminMessage { get; set; } = "";
        //
        // ====================================================================================================
        /// <summary>
        /// Request Referer
        /// </summary>
        public string requestPageReferer {
            get {
                return requestReferer;
            }
        }
        //
        // ====================================================================================================
        /// <summary>
        /// Request Referer
        /// </summary>
        public string requestReferer {
            get {
                if ((httpContext == null) || (httpContext.Request == null) || (httpContext.Request.UrlReferrer == null)) return string.Empty;
                return httpContext.Request.UrlReferrer.AbsoluteUri;
            }
        }
        //
        // ====================================================================================================
        /// <summary>
        /// The Action for all internal forms, if not set, default
        /// </summary>
        public string requestFormActionURL {
            get {
                if (!string.IsNullOrEmpty(_serverFormActionURL)) { return _serverFormActionURL; }
                requestFormActionURL = requestProtocol + requestDomain + requestPath + requestPage;
                return _serverFormActionURL;
            }
            set {
                _serverFormActionURL = value;
            }
        }
        private string _serverFormActionURL;
        //
        // ====================================================================================================
        /// <summary>
        /// string to prefix a content watch link. protocol + domain + "/"
        /// </summary>
        public string requestContentWatchPrefix {
            get {
                return requestProtocol + requestDomain + "/";
            }
        }
        //
        // ====================================================================================================
        /// <summary>
        /// The protocol used in the current quest
        /// </summary>
        public string requestProtocol {
            get {
                return (requestSecure) ? "https://" : "http://";
            }
        }
        //
        // ====================================================================================================
        /// <summary>
        /// The executed script. If the url is a route, requestUrlSource is the route and requestUrl is the script page that was run
        /// </summary>
        public string requestUrl {
            get {
                if (string.IsNullOrEmpty(_requestUrl)) {
                    _requestUrl = requestProtocol
                        + requestDomain
                        + (requestSecure && requestPort.Equals(443) ? "" : ((!requestSecure) && requestPort.Equals(80) ? "" : (":" + requestPort)))
                        + requestPath
                        + requestPage
                        + ((string.IsNullOrWhiteSpace(requestQueryString)) ? "" : "?" + requestQueryString);
                }
                return _requestUrl;
            }
        }
        private string _requestUrl = null;
        //
        // ====================================================================================================
        /// <summary>
        /// The path between the requestDomain and the requestPage. NOTE - breaking change: this used to follow appRootPath and never started with /
        /// </summary>
        public string requestPath {
            get {
                if (_requestPath != null) { return _requestPath; }
                if ((httpContext == null) || (httpContext.Request == null) || (httpContext.Request.Url == null)) return string.Empty;
                var segments = splitUrl(httpContext.Request.Url.AbsoluteUri);
                _requestPath = segments.unixPath;
                return _requestPath;
            }
        }
        private string _requestPath = null;
        //
        // ====================================================================================================
        /// <summary>
        /// The page or script name, typicall index.html or default.aspx or myPage.aspx
        /// </summary>
        public string requestPage {
            get {
                if (_requestPage != null) { return _requestPage; }
                if ((httpContext == null) || (httpContext.Request == null) || (httpContext.Request.Url == null)) return string.Empty;
                var segments = splitUrl(httpContext.Request.Url.AbsoluteUri);
                _requestPage = segments.filename;
                return _requestPage;
            }
        }
        private string _requestPage = null;
        //
        // ====================================================================================================
        /// <summary>
        /// The URL to the root of the secure area for this site
        /// </summary>
        public string requestSecureURLRoot {
            get {
                return "https://" + requestDomain + "/";
            }
        }
        //
        // ====================================================================================================
        /// <summary>
        /// when set, Meta no follow is added
        /// </summary>
        public bool responseNoFollow {
            get {
                return _responseNoFollow;
            }
            set {
                _responseNoFollow = value;
            }
        }
        private bool _responseNoFollow = false;
        //
        // ====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public Dictionary<string, CookieClass> requestCookies {
            get {
                if (_requestCookies != null) { return _requestCookies; }
                _requestCookies = new Dictionary<string, CookieClass>();
                if (httpContext?.Request?.Cookies == null) { return _requestCookies; }
                //
                // -- add httpContet request cookies to the local simple name/value request cookies dictionary
                foreach (var kvp in httpContext.Request.Cookies) {
                    if (!_requestCookies.ContainsKey(kvp.Key)) {
                        //
                        // -- do not allow duplicate keys
                        _requestCookies.Add(kvp.Key, new CookieClass() { name = kvp.Key, value = kvp.Value.Value });
                    }
                }
                return _requestCookies;
            }
        }
        private Dictionary<string, CookieClass> _requestCookies = null;
        //
        // ====================================================================================================
        /// <summary>
        /// return a cookie value. return empty if cookie is not present.
        /// </summary>
        /// <param name="cookieName"></param>
        /// <returns></returns>
        public string requestCookie(string cookieName) {
            //
            LogController.log(core, "WebServerController.requestCookie, key [" + cookieName + "]", BaseClasses.CPLogBaseClass.LogLevel.Trace);
            //
            if (!requestCookies.ContainsKey(cookieName)) { return ""; }
            //
            LogController.log(core, "WebServerController.requestCookie, ContainsKey true, value [" + requestCookies[cookieName].value + "]", BaseClasses.CPLogBaseClass.LogLevel.Trace);
            //
            return requestCookies[cookieName].value;
        }
        //
        // ====================================================================================================
        /// <summary>
        /// The content type of the request
        /// </summary>
        public string requestContentType {
            get {
                if (_requestContentType != null) { return _requestContentType; };
                if ((httpContext == null) || (httpContext.Request == null)) return "";
                _requestContentType = httpContext.Request.ContentType;
                return _requestContentType;
            }
        }
        private string _requestContentType = null;
        //
        // ====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public Dictionary<string, string> requestHeaders { get; set; } = new Dictionary<string, string>();
        //
        // ====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public Dictionary<string, string> requestForm { get; set; } = new Dictionary<string, string>();
        //
        // ====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        /// <param name="core"></param>
        public WebServerController(CoreController core) {
            this.core = core;
        }
        //
        //=======================================================================================
        /// <summary>
        /// IIS Reset, must be called from an elevated process
        /// </summary>
        public void reset() {
            try {
                string logFilename = core.tempFiles.localAbsRootPath + "iisreset-" + getRandomInteger(core).ToString() + ".Log";
                string cmd = "IISReset.exe";
                string arg = "";
                string stdOut = runProcess(core, cmd, arg, true);
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //=======================================================================================
        /// <summary>
        /// recycle iis process
        /// </summary>
        public void recycle() {
            try {
                ServerManager serverManager = new ServerManager();
                ApplicationPoolCollection appPoolColl = serverManager.ApplicationPools;
                foreach (ApplicationPool appPool in appPoolColl) {
                    if (appPool.Name.ToLowerInvariant() == core.appConfig.name.ToLowerInvariant()) {
                        if (appPool.Start() == ObjectState.Started) {
                            appPool.Recycle();
                            LogController.logInfo(core, "iis recycle, app [" + core.appConfig.name + "]");
                        }
                    }
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //   
        //==================================================================================
        /// <summary>
        /// Initialize httpcontext.
        /// </summary>
        /// <returns></returns>
        public bool initHttpContext() {
            try {
                //
                LogController.logShortLine("initHttpContext, enter", BaseClasses.CPLogBaseClass.LogLevel.Trace);
                LogController.logShortLine("initHttpContext, httpContext.Request.Cookies.Count [" + httpContext.Request.Cookies.Count + "]", BaseClasses.CPLogBaseClass.LogLevel.Trace);
                //
                // -- must have valid context, else non http 
                if (httpContext == null) { return false; }
                if (!core.appConfig.appStatus.Equals(BaseModels.AppConfigBaseModel.AppStatusEnum.ok)) { return false; }
                //
                // -- initialize doc properties from httpContext
                foreach (KeyValuePair<string, string> kvp in httpContext.Request.ServerVariables) {
                    core.docProperties.setProperty(kvp.Key, kvp.Value, DocPropertyModel.DocPropertyTypesEnum.serverVariable);
                }
                foreach (KeyValuePair<string, string> kvp in httpContext.Request.Headers) {
                    core.docProperties.setProperty(kvp.Key, kvp.Value, DocPropertyModel.DocPropertyTypesEnum.header);
                }
                foreach (KeyValuePair<string, string> kvp in httpContext.Request.QueryString) {
                    core.docProperties.setProperty(kvp.Key, kvp.Value, DocPropertyModel.DocPropertyTypesEnum.queryString);
                }
                foreach (KeyValuePair<string, string> kvp in httpContext.Request.Form) {
                    core.docProperties.setProperty(kvp.Key, kvp.Value, DocPropertyModel.DocPropertyTypesEnum.form);
                }
                //
                // -- add uploaded files to docproperties. windowsTempFiles should be disposed by parent object who created them and provided the context
                foreach (DocPropertyModel fileProperty in httpContext.Request.Files) {
                    core.docProperties.setProperty(fileProperty.name, fileProperty);
                }
                //
                // -- setup response 
                core.webServer.responseContentType = "text/html";
                //
                //   javascript cookie detect on page1 of all visits
                string CookieDetectKey = core.docProperties.getText(rnCookieDetect);
                if (!string.IsNullOrEmpty(CookieDetectKey)) {
                    //
                    SecurityController.TokenData visitToken = SecurityController.decodeToken(core, CookieDetectKey);
                    if (visitToken.id != 0) {
                        string sql = "update ccvisits set CookieSupport=1 where id=" + visitToken.id;
                        core.db.executeNonQuery(sql);
                        core.doc.continueProcessing = false;
                        return core.doc.continueProcessing;
                    }
                }
                //
                //   verify Domain table entry
                bool updateDomainCache = false;
                //
                core.domain.name = requestDomain;
                core.domain.rootPageId = 0;
                core.domain.noFollow = false;
                core.domain.typeId = 1;
                core.domain.visited = false;
                core.domain.id = 0;
                core.domain.forwardUrl = "";
                core.domainDictionary = core.cache.getObject<Dictionary<string, DomainModel>>("domainContentList");
                if (core.domainDictionary == null) {
                    //
                    //  no cache found, build domainContentList from database
                    core.domainDictionary = DomainModel.createDictionary(core.cpParent, "(active<>0)and(name is not null)");
                    updateDomainCache = true;
                }
                //
                // verify app config domainlist is in the domainlist cache
                foreach (string domain in core.appConfig.domainList) {
                    if (!core.domainDictionary.ContainsKey(domain.ToLowerInvariant())) {
                        LogController.logTrace(core, "adding domain record because configList domain not found [" + domain.ToLowerInvariant() + "]");
                        var newDomain = DbBaseModel.addEmpty<DomainModel>(core.cpParent, 0);
                        newDomain.name = domain;
                        newDomain.rootPageId = 0;
                        newDomain.noFollow = false;
                        newDomain.typeId = 1;
                        newDomain.visited = false;
                        newDomain.forwardUrl = "";
                        newDomain.defaultTemplateId = 0;
                        newDomain.pageNotFoundPageId = 0;
                        newDomain.forwardDomainId = 0;
                        newDomain.defaultRouteId = core.siteProperties.getInteger("");
                        newDomain.save(core.cpParent, 0);
                        core.domainDictionary.Add(domain.ToLowerInvariant(), newDomain);
                        updateDomainCache = true;
                    }
                }
                //
                // -- verify request domain
                if (!core.domainDictionary.ContainsKey(requestDomain.ToLowerInvariant())) {
                    LogController.logTrace(core, "adding domain record because requestDomain [" + requestDomain.ToLowerInvariant() + "] not found");
                    var newDomain = DomainModel.addEmpty<DomainModel>(core.cpParent, 0);
                    newDomain.name = requestDomain;
                    newDomain.rootPageId = 0;
                    newDomain.noFollow = false;
                    newDomain.typeId = 1;
                    newDomain.visited = false;
                    newDomain.forwardUrl = "";
                    newDomain.defaultTemplateId = 0;
                    newDomain.pageNotFoundPageId = 0;
                    newDomain.forwardDomainId = 0;
                    newDomain.save(core.cpParent, 0);
                    core.domainDictionary.Add(requestDomain.ToLowerInvariant(), newDomain);
                    updateDomainCache = true;
                }
                if (updateDomainCache) {
                    //
                    // if there was a change, update the cache
                    //
                    string dependencyKey = DbBaseModel.createDependencyKeyInvalidateOnChange<DomainModel>(core.cpParent);
                    CacheKeyHashClass dependencyKeyHash = core.cache.createKeyHash(dependencyKey);
                    List<CacheKeyHashClass> keyHashList = new List<CacheKeyHashClass> { dependencyKeyHash };
                    core.cache.storeObject("domainContentList", core.domainDictionary, keyHashList);
                }
                //
                // domain found
                //
                core.domain = core.domainDictionary[requestDomain.ToLowerInvariant()];
                if (core.domain.id == 0) {
                    //
                    // this is a default domain or a new domain -- add to the domain table
                    var domain = new DomainModel {
                        name = requestDomain,
                        typeId = 1,
                        rootPageId = core.domain.rootPageId,
                        forwardUrl = core.domain.forwardUrl,
                        noFollow = core.domain.noFollow,
                        visited = core.domain.visited,
                        defaultTemplateId = core.domain.defaultTemplateId,
                        pageNotFoundPageId = core.domain.pageNotFoundPageId
                    };
                    //
                    // todo - would prefer not save new template
                    // -- fix, must save or template selection fails.
                    domain.save(core.cpParent, 0);
                    core.domain.id = domain.id;
                }
                if (!core.domain.visited) {
                    //
                    // set visited true
                    //
                    core.db.executeNonQuery("update ccdomains set visited=1 where name=" + DbController.encodeSQLText(requestDomain));
                    core.cache.invalidate("domainContentList");
                }
                if (core.domain.typeId == 1) {
                    //
                    // normal domain, leave it
                    //
                } else if (GenericController.strInstr(1, requestPathPage, "/" + core.appConfig.adminRoute, 1) != 0) {
                    //
                    // forwarding does not work in the admin site
                    //
                } else if (core.domain.typeId.Equals(2) && !string.IsNullOrEmpty(core.domain.forwardUrl)) {
                    //
                    // forward to a URL
                    if (GenericController.strInstr(1, core.domain.forwardUrl, "://") == 0) {
                        core.domain.forwardUrl = "http://" + core.domain.forwardUrl;
                    }
                    redirect(core.domain.forwardUrl, "Forwarding to [" + core.domain.forwardUrl + "] because the current domain [" + requestDomain + "] is in the domain content set to forward to this URL", false, false);
                    return core.doc.continueProcessing;
                } else if ((core.domain.typeId == 3) && (core.domain.forwardDomainId != 0) && (core.domain.forwardDomainId != core.domain.id)) {
                    //
                    // forward to a replacement domain
                    //
                    string forwardDomain = MetadataController.getRecordName(core, "domains", core.domain.forwardDomainId);
                    if (!string.IsNullOrEmpty(forwardDomain)) {
                        int pos = requestUrlSource.IndexOf(requestDomain, StringComparison.InvariantCultureIgnoreCase);
                        if (pos > 0) {
                            core.domain.forwardUrl = requestUrlSource.left(pos) + forwardDomain + requestUrlSource.Substring((pos + requestDomain.Length));
                            redirect(core.domain.forwardUrl, "Forwarding to [" + core.domain.forwardUrl + "] because the current domain [" + requestDomain + "] is in the domain content set to forward to this replacement domain", false, false);
                            return core.doc.continueProcessing;
                        }
                    }
                }
                //
                // todo - CORS cannot be generated dynamically because info http method does not execute code
                // -- add default CORS headers to approved domains
                if ((httpContext != null) && (httpContext.Request != null) && (httpContext.Request.UrlReferrer != null)) {
                    Uri originUri = httpContext.Request.UrlReferrer;
                    if (originUri != null) {
                        if (core.domainDictionary.ContainsKey(originUri.Host.ToLowerInvariant())) {
                            if (core.domainDictionary[originUri.Host.ToLowerInvariant()].allowCORS) {
                                addResponseHeader("Access-Control-Allow-Credentials", "true");
                                addResponseHeader("Access-Control-Allow-Methods", "POST,GET,OPTIONS");
                                addResponseHeader("Access-Control-Headers", "Content-Type,soapaction,X-Requested-With");
                                addResponseHeader("Access-Control-Allow-Origin", originUri.GetLeftPart(UriPartial.Authority));
                            }
                        }
                    }
                }
                if (core.domain.noFollow) {
                    responseNoFollow = true;
                }
                //
                // ----- Style tag
                adminMessage = "For more information, please contact the <a href=\"mailto:" + core.siteProperties.emailAdmin + "?subject=Re: " + requestDomain + "\">Site Administrator</A>.";                //
                //
                LogController.logShortLine("initHttpContext, exit", BaseClasses.CPLogBaseClass.LogLevel.Trace);
                //
            } catch (Exception ex) {
                //
                LogController.logShortLine("initHttpContext, exception", BaseClasses.CPLogBaseClass.LogLevel.Trace);
                //
                LogController.logError(core, ex);
                throw;
            }
            return core.doc.continueProcessing;
        }
        //
        //====================================================================================================
        /// <summary>
        /// set cookie in iis response
        /// </summary>
        /// <param name="name">The cookie key</param>
        /// <param name="value">The cookie value</param>
        /// <param name="dateExpires">If MinDate, no expiration and the cookie is not persistent. If not MinDate, the cookied expires on the is date and is persistent.</param>
        /// <param name="domain">The domain for this cookie. Typically the requestDomain.</param>
        /// <param name="path">the path for the cookie. typically "/"</param>
        /// <param name="secure">If true, this cookie will only be served over https.</param>
        public void addResponseCookie(string name, string value, DateTime dateExpires, string domain, string path, bool secure) {
            try {
                if ((httpContext == null) || (httpContext.Response == null) || (httpContext.Response.cookies == null)) { return; }
                //
                // -- add cookie to httpContext response
                if (!httpContext.Response.cookies.ContainsKey(name)) {
                    httpContext.Response.cookies.Add(name, new HttpContextResponseCookie());
                }
                httpContext.Response.cookies[name].httpOnly = true;
                httpContext.Response.cookies[name].sameSite = HttpContextResponseCookieSameSiteMode.Lax;
                httpContext.Response.cookies[name].value = value;
                if (!isMinDate(dateExpires)) {
                    httpContext.Response.cookies[name].expires = dateExpires;
                }
                if (!string.IsNullOrEmpty(domain)) {
                    httpContext.Response.cookies[name].domain = domain;
                }
                if (!string.IsNullOrEmpty(path)) {
                    httpContext.Response.cookies[name].path = path;
                }
                if (secure) {
                    httpContext.Response.cookies[name].secure = secure;
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// set cookie in iis response. creates a cookie, to the current requestDomain, with "/" path, not secure
        /// </summary>
        /// <param name="name">The cookie key</param>
        /// <param name="value">The cookie value</param>
        /// <param name="dateExpires">If MinDate, no expiration and the cookie is not persistent. If not MinDate, the cookied expires on the is date and is persistent.</param>
        public void addResponseCookie(string name, string value, DateTime dateExpires) {
            addResponseCookie(name, value, dateExpires, requestDomain, "/", false);
        }
        //
        //====================================================================================================
        /// <summary>
        /// set cookie in iis response. creates a non-persistent cookie, to the current requestDomain, with "/" path, not secure
        /// </summary>
        /// <param name="name">The cookie key</param>
        /// <param name="value">The cookie value</param>
        public void addResponseCookie(string name, string value) {
            addResponseCookie(name, value, DateTime.MinValue);
        }
        //
        //====================================================================================================
        /// <summary>
        /// Set iis response status
        /// </summary>
        /// <param name="status">A string starting with the response number (like 200 or 404) followed by the response message</param>
        public void setResponseStatus(string status) {
            if (!core.doc.continueProcessing) { return; }
            if ((httpContext == null) || (httpContext.Response == null)) { return; }
            //
            httpContext.Response.status = status;
        }
        //
        //====================================================================================================
        /// <summary>
        /// return response cookie name=value in a querystring format
        /// </summary>
        public string responseCookies {
            get {
                string result = "";
                string delimiter = "";
                foreach (KeyValuePair<string, HttpContextResponseCookie> kvp in httpContext.Response.cookies) {
                    result += delimiter + kvp.Key + "=" + kvp.Value.value;
                    delimiter = "&";
                }
                return result;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// return response headers name=value in a querystring format
        /// </summary>
        public string responseHeaders {
            get {
                string result = "";
                string delimiter = "";
                foreach (HttpContextResponseHeader header in httpContext.Response.headers) {
                    result += delimiter + header.name + "=" + header.value;
                    delimiter = "&";
                }
                return result;
            }
        }
        //
        //===========================================================================================
        /// <summary>
        /// 
        /// </summary>
        public string responseContentType {
            get {
                if ((httpContext == null) || (httpContext.Response == null)) { return ""; }
                return httpContext.Response.contentType;
            }
            set {
                if ((httpContext == null) || (httpContext.Response == null)) { return; }
                httpContext.Response.contentType = value;
            }
        }
        //
        //===========================================================================================
        /// <summary>
        /// 
        /// </summary>
        /// <param name="HeaderName"></param>
        /// <param name="HeaderValue"></param>
        public void addResponseHeader(string HeaderName, string HeaderValue) {
            try {
                if (!core.doc.continueProcessing) { return; }
                if ((httpContext == null) || (httpContext.Response == null)) { return; }
                //
                httpContext.Response.headers.Add(new HttpContextResponseHeader() {
                    name = HeaderName,
                    value = HeaderValue
                });
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //===========================================================================================
        /// <summary>
        /// redirect
        /// </summary>
        /// <param name="NonEncodedLink"></param>
        /// <param name="RedirectReason"></param>
        /// <param name="IsPageNotFound"></param>
        /// <param name="allowDebugMessage">If true, when visit property debugging is enabled, the routine returns </param>
        public string redirect(string NonEncodedLink, string RedirectReason, bool IsPageNotFound, bool allowDebugMessage) {
            string result = HtmlController.div("Redirecting to [" + NonEncodedLink + "], reason [" + RedirectReason + "]", "ccWarningBox");
            try {
                if (!core.doc.continueProcessing) { return result; }
                if ((httpContext == null) || (httpContext.Response == null)) { return result; }
                //
                if (core.doc.continueProcessing) {
                    string FullLink = NonEncodedLink;
                    string ShortLink = "";
                    //
                    // convert link to a long link on this domain
                    if (NonEncodedLink.left(4).ToLowerInvariant() != "http") {
                        if (NonEncodedLink.left(1).ToLowerInvariant() == "/") {
                            //
                            // -- root relative - url starts with path, let it go
                        } else if (NonEncodedLink.left(1).ToLowerInvariant() == "?") {
                            //
                            // -- starts with qs, fix issue where iis consideres this on the physical page, not the link-alias vitrual route
                            NonEncodedLink = requestPathPage + NonEncodedLink;
                        } else {
                            //
                            // -- url starts with the page
                            NonEncodedLink = requestPath + NonEncodedLink;
                        }
                        ShortLink = NonEncodedLink;
                        ShortLink = GenericController.convertLinkToShortLink(ShortLink, requestDomain, core.appConfig.cdnFileUrl);
                        ShortLink = GenericController.encodeVirtualPath(ShortLink, core.appConfig.cdnFileUrl, appRootPath, requestDomain);
                        FullLink = requestProtocol + requestDomain + ShortLink;
                    }
                    //
                    string EncodedLink = null;
                    if (string.IsNullOrEmpty(NonEncodedLink)) {
                        //
                        // Link is not valid
                        //
                        LogController.logError(core, new GenericException("Redirect was called with a blank Link. Redirect Reason [" + RedirectReason + "]"));
                        return result;
                        //
                        // changed to main_ServerLinksource because if a redirect is caused by a link forward, and the host page for the iis 404 is
                        // the same as the destination of the link forward, this throws an error and does not forward. the only case where main_ServerLinksource is different
                        // then main_ServerLink is the linkfforward/linkalias case.
                        //
                    } else if ((requestForm.Count == 0) && (requestUrlSource == FullLink)) {
                        //
                        // Loop redirect error, throw trap and block redirect to prevent loop
                        //
                        LogController.logError(core, new GenericException("Redirect was called to the same URL, main_ServerLink is [" + requestUrl + "], main_ServerLinkSource is [" + requestUrlSource + "]. This redirect is only allowed if either the form or querystring has change to prevent cyclic redirects. Redirect Reason [" + RedirectReason + "]"));
                        return result;
                    } else if (IsPageNotFound) {
                        //
                        // Do a PageNotFound then redirect
                        //
                        LogController.addSiteWarning(core, "Page Not Found Redirect", "Page Not Found Redirect", "", 0, "Page Not Found Redirect [" + requestUrlSource + "]", "Page Not Found Redirect", "Page Not Found Redirect");
                        if (!string.IsNullOrEmpty(ShortLink)) {
                            string sql = "Update ccContentWatch set link=null where link=" + DbController.encodeSQLText(ShortLink);
                            core.db.executeNonQuery(sql);
                        }
                        //
                        if (allowDebugMessage && core.doc.visitPropertyAllowDebugging) {
                            //
                            // -- Verbose - do not redirect, just print the link
                            EncodedLink = NonEncodedLink;
                            result = "<div style=\"padding:20px;border:1px dashed black;background-color:white;color:black;\">" + RedirectReason + "<p>Page Not Found. Click to continue the redirect to <a href=" + EncodedLink + ">" + HtmlController.encodeHtml(NonEncodedLink) + "</a>...</p></div>";
                        } else {
                            setResponseStatus(WebServerController.httpResponseStatus404_NotFound);
                        }
                    } else {

                        //
                        // Go ahead and redirect
                        //
                        LogController.logInfo(core, "Redirect called, from [" + requestUrl + "], to [" + NonEncodedLink + "], reason [" + RedirectReason + "]");
                        if (allowDebugMessage && core.doc.visitPropertyAllowDebugging) {
                            //
                            // -- Verbose - do not redirect, just print the link
                            EncodedLink = NonEncodedLink;
                            result = "<div style=\"padding:20px;border:1px dashed black;background-color:white;color:black;\">" + RedirectReason + "<p>Click to continue the redirect to <a href=" + EncodedLink + ">" + HtmlController.encodeHtml(NonEncodedLink) + "</a>...</p></div>";
                        } else {
                            //
                            // -- Redirect now
                            clearResponseBuffer();
                            if (httpContext != null) {
                                //
                                // -- redirect and release application. HOWEVER -- the thread will continue so use responseOpen=false to abort as much activity as possible
                                httpContext.Response.redirectUrl = NonEncodedLink;
                                httpContext.ApplicationInstance.CompleteRequest();
                            }
                        }
                    }
                    //
                    // -- close the output stream
                    core.doc.continueProcessing = false;
                }
                return result;
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        /// <param name="NonEncodedLink"></param>
        /// <param name="RedirectReason"></param>
        /// <returns></returns>
        public string redirect(string NonEncodedLink, string RedirectReason)
            => redirect(NonEncodedLink, RedirectReason, false);
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        /// <param name="NonEncodedLink"></param>
        /// <param name="RedirectReason"></param>
        /// <param name="IsPageNotFound"></param>
        /// <returns></returns>
        public string redirect(string NonEncodedLink, string RedirectReason, bool IsPageNotFound)
            => redirect(NonEncodedLink, RedirectReason, IsPageNotFound, false);
        //========================================================================
        /// <summary>
        /// 
        /// </summary>
        /// <param name="contentName"></param>
        /// <param name="recordId"></param>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public bool redirectByRecord_ReturnStatus(string contentName, int recordId, string fieldName) {
            try {
                string iContentName = GenericController.encodeText(contentName);
                int iRecordId = GenericController.encodeInteger(recordId);
                string iFieldName = GenericController.encodeEmpty(fieldName, "link");
                bool result = false;
                using (var csData = new CsModel(core)) {
                    if (csData.open(iContentName, "ID=" + iRecordId)) {
                        //
                        // Assume all Link fields are already encoded -- as this is how they would appear if the admin cut and pasted
                        string EncodedLink = encodeText(csData.getText(iFieldName)).Trim(' ');
                        bool BlockRedirect = false;
                        string LinkPrefix = "";
                        string NonEncodedLink = "";
                        if (string.IsNullOrEmpty(EncodedLink)) {
                            BlockRedirect = true;
                        } else {
                            //
                            // ----- handle content special cases (prevent redirect to deleted records)
                            //
                            NonEncodedLink = GenericController.decodeResponseVariable(EncodedLink);
                            if (iContentName.ToLowerInvariant() == "content watch") {
                                //
                                // ----- special case
                                //       if this is a content watch record, check the underlying content for
                                //       inactive or expired before redirecting
                                //
                                LinkPrefix = core.webServer.requestContentWatchPrefix;
                                int contentId = csData.getInteger("ContentID");
                                var contentMeta = ContentMetadataModel.create(core, contentId);
                                contentMeta.name = MetadataController.getContentNameByID(core, contentId);
                                int HostRecordId = 0;
                                if (string.IsNullOrEmpty(contentMeta.name)) {
                                    //
                                    // ----- Content Watch with a bad ContentID, mark inactive
                                    //
                                    BlockRedirect = true;
                                    csData.set("active", 0);
                                } else {
                                    HostRecordId = (csData.getInteger("RecordID"));
                                    if (HostRecordId == 0) {
                                        //
                                        // ----- Content Watch with a bad iRecordID, mark inactive
                                        //
                                        BlockRedirect = true;
                                        csData.set("active", 0);
                                    } else {
                                        using var CSHost = new CsModel(core);
                                        CSHost.open(contentMeta.name, "ID=" + HostRecordId);
                                        if (!CSHost.ok()) {
                                            //
                                            // ----- Content Watch host record not found, mark inactive
                                            //
                                            BlockRedirect = true;
                                            csData.set("active", 0);
                                        }
                                    }
                                }
                                if (BlockRedirect) {
                                    //
                                    // ----- if a content watch record is blocked, delete the content tracking
                                    //
                                    MetadataController.deleteContentRules(core, contentMeta, HostRecordId);
                                }
                            }
                        }
                        if (!BlockRedirect) {
                            //
                            // If link incorrectly includes the LinkPrefix, take it off first, then add it back
                            //
                            NonEncodedLink = GenericController.removeUrlPrefix(NonEncodedLink, LinkPrefix);
                            if (csData.isFieldSupported("Clicks")) {
                                csData.set("Clicks", (csData.getNumber("Clicks")) + 1);
                            }
                            core.webServer.redirect(LinkPrefix + NonEncodedLink, "Redirect by Record, content [" + contentName + "], recordId [" + recordId + "], field [" + fieldName + "], called from " + GenericController.getCallStack(), false, false);
                            result = true;
                        }
                    }
                }
                return result;
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //========================================================================
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string getBrowserAcceptLanguage() {
            try {
                string AcceptLanguageString = (core.webServer.httpContext.Request.ServerVariables.ContainsKey("HTTP_ACCEPT_LANGUAGE")) ? core.webServer.httpContext.Request.ServerVariables["HTTP_ACCEPT_LANGUAGE"] : "";
                int CommaPosition = GenericController.strInstr(1, AcceptLanguageString, ",");
                while (CommaPosition != 0) {
                    string AcceptLanguage = (AcceptLanguageString.left(CommaPosition - 1)).Trim(' ');
                    AcceptLanguageString = AcceptLanguageString.Substring(CommaPosition);
                    if (AcceptLanguage.Length > 0) {
                        int DashPosition = GenericController.strInstr(1, AcceptLanguage, "-");
                        if (DashPosition > 1) {
                            AcceptLanguage = AcceptLanguage.left(DashPosition - 1);
                        }
                        DashPosition = GenericController.strInstr(1, AcceptLanguage, ";");
                        if (DashPosition > 1) {
                            return AcceptLanguage.left(DashPosition - 1);
                        }
                    }
                    CommaPosition = GenericController.strInstr(1, AcceptLanguageString, ",");
                }
                return "";
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public void flushStream() {
            if ((httpContext == null) || (httpContext.Response == null)) { return; }
            httpContext.Response.Flush();
        }
        //
        //====================================================================================================
        /// <summary>
        /// Verify a site exists, it not add it, it is does, verify all its settings
        /// </summary>
        /// <param name="appName"></param>
        /// <param name="DomainName"></param>
        /// <param name="rootPublicFilesPath"></param>
        public void verifySite(string appName, string DomainName, string rootPublicFilesPath) {
            try {
                verifyAppPool(appName);
                verifyWebsite(appName, DomainName, rootPublicFilesPath, appName);
            } catch (Exception ex) {
                LogController.logError(core, ex, "verifySite");
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// verify the application pool. If it exists, update it. If not, create it
        /// </summary>
        /// <param name="poolName"></param>
        public void verifyAppPool(string poolName) {
            try {
                using ServerManager serverManager = new ServerManager();
                bool poolFound = false;
                ApplicationPool appPool = null;
                foreach (ApplicationPool appPoolWithinLoop in serverManager.ApplicationPools) {
                    if (appPoolWithinLoop.Name == poolName) {
                        poolFound = true;
                        break;
                    }
                }
                if (!poolFound) {
                    appPool = serverManager.ApplicationPools.Add(poolName);
                } else {
                    appPool = serverManager.ApplicationPools[poolName];
                }
                appPool.ManagedRuntimeVersion = "v4.0";
                appPool.Enable32BitAppOnWin64 = true;
                appPool.ManagedPipelineMode = ManagedPipelineMode.Integrated;
                serverManager.CommitChanges();
            } catch (Exception ex) {
                LogController.logError(core, ex, "verifyAppPool");
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// verify the website. If it exists, update it. If not, create it
        /// </summary>
        /// <param name="appName"></param>
        /// <param name="domainName"></param>
        /// <param name="phyPath"></param>
        /// <param name="appPool"></param>
        public void verifyWebsite(string appName, string domainName, string phyPath, string appPool) {
            try {
                using ServerManager iisManager = new ServerManager();
                //
                // -- verify the site exists
                bool found = false;
                foreach (Site siteWithinLoop in iisManager.Sites) {
                    if (siteWithinLoop.Name.ToLowerInvariant() == appName.ToLowerInvariant()) {
                        found = true;
                        break;
                    }
                }
                if (!found) {
                    iisManager.Sites.Add(appName, "http", "*:80:" + appName, phyPath);
                }
                Site site = iisManager.Sites[appName];
                //
                // -- verify the domain binding
                verifyWebsiteBinding(site, domainName);
                //
                // -- verify the application pool
                site.ApplicationDefaults.ApplicationPoolName = appPool;
                foreach (Application iisApp in site.Applications) {
                    iisApp.ApplicationPoolName = appPool;
                }
                //
                // -- verify the cdn virtual directory (if configured)
                string cdnFilesPrefix = core.appConfig.cdnFileUrl;
                if (cdnFilesPrefix.IndexOf("://", StringComparison.InvariantCulture) < 0) {
                    verifyWebsiteVirtualDirectory(site, appName, cdnFilesPrefix, core.appConfig.localFilesPath);
                }
                //
                // -- commit any changes
                iisManager.CommitChanges();
            } catch (Exception ex) {
                LogController.logError(core, ex, "verifyWebsite");
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Verify the binding
        /// </summary>
        /// <param name="site"></param>
        /// <param name="domainName"></param>
        private void verifyWebsiteBinding(Site site, string domainName) {
            try {
                string bindingInformation = "*:80:" + domainName;
                string bindingProtocol = "http";
                using ServerManager iisManager = new ServerManager();
                bool found = false;
                foreach (Binding bindingWithinLoop in site.Bindings) {
                    if ((bindingWithinLoop.BindingInformation == bindingInformation) && (bindingWithinLoop.Protocol == bindingProtocol)) {
                        found = true;
                        break;
                    }
                }
                if (!found) {
                    Binding binding = site.Bindings.CreateElement();
                    binding.BindingInformation = bindingInformation;
                    binding.Protocol = bindingProtocol;
                    site.Bindings.Add(binding);
                    iisManager.CommitChanges();
                }
            } catch (Exception ex) {
                LogController.logError(core, ex, "verifyWebsite_Binding");
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// verify a binding exists for the 
        /// </summary>
        /// <param name="appName"></param>
        /// <param name="domainName"></param>
        public void verifyWebsiteBinding(string appName, string domainName) {
            try {
                using ServerManager iisManager = new ServerManager();
                //
                // -- verify the site exists
                bool found = false;
                foreach (Site siteWithinLoop in iisManager.Sites) {
                    if (siteWithinLoop.Name.ToLowerInvariant() == appName.ToLowerInvariant()) {
                        found = true;
                        break;
                    }
                }
                if (found) {
                    Site site = iisManager.Sites[appName];
                    //
                    // -- verify the domain binding
                    verifyWebsiteBinding(site, domainName);
                    //
                    // -- commit any changes
                    iisManager.CommitChanges();
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        /// <param name="site"></param>
        /// <param name="appName"></param>
        /// <param name="virtualFolder"></param>
        /// <param name="physicalPath"></param>
        public void verifyWebsiteVirtualDirectory(Site site, string appName, string virtualFolder, string physicalPath) {
            try {
                bool found = false;
                foreach (Application iisApp in site.Applications) {
                    if (iisApp.ApplicationPoolName.ToLowerInvariant() == appName.ToLowerInvariant()) {
                        foreach (VirtualDirectory virtualDirectory in iisApp.VirtualDirectories) {
                            if (virtualDirectory.Path == virtualFolder) {
                                found = true;
                                break;
                            }
                        }
                        if (!found) {
                            //
                            // -- create each of the folder segments in the virtualFolder
                            List<string> appVirtualFolderSegments = virtualFolder.Split('/').ToList();
                            foreach (string appVirtualFolderSegment in appVirtualFolderSegments) {
                                if (!string.IsNullOrEmpty(appVirtualFolderSegment)) {
                                    string newDirectoryPath = "/" + appVirtualFolderSegment;
                                    bool directoryFound = false;
                                    foreach (VirtualDirectory currentDirectory in iisApp.VirtualDirectories) {
                                        if (currentDirectory.Path.ToLowerInvariant() == newDirectoryPath.ToLowerInvariant()) {
                                            directoryFound = true;
                                            break;
                                        }
                                    }
                                    if (!directoryFound) {
                                        iisApp.VirtualDirectories.Add(newDirectoryPath, physicalPath);
                                    }
                                }
                            }
                        }
                    }
                    if (found) {
                        break;
                    }
                }
            } catch (Exception ex) {
                LogController.logError(core, ex, "verifyWebsite_VirtualDirectory");
                throw;
            }
        }
        //
        //========================================================================
        /// <summary>
        /// 
        /// </summary>
        public void clearResponseBuffer() {
            httpContext.Response.headers.Clear();
        }
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    }
    //
    // ====================================================================================================
    /// <summary>
    /// QueryString, Form and cookie Processing variables
    /// </summary>
    public class CookieClass {
        /// <summary>
        /// 
        /// </summary>
        public string name { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string value { get; set; }
    }
}