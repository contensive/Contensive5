
using System;
using System.Collections.Generic;
using Contensive.Models.Db;
using Contensive.Processor.Controllers;
using Contensive.Processor.Models.Domain;

namespace Contensive.Processor {
    /// <summary>
    /// request
    /// </summary>
    public class CPRequestClass : BaseClasses.CPRequestBaseClass, IDisposable {
        /// <summary>
        /// dependencies
        /// </summary>
        private readonly CPClass cp;
        //
        //====================================================================================================
        /// <summary>
        /// construct
        /// </summary>
        /// <param name="cp"></param>
        public CPRequestClass(CPClass cp) {
            this.cp = cp;
        }
        //
        //====================================================================================================
        /// <summary>
        /// request browser string
        /// </summary>
        public override string Browser {
            get {
                return cp.core.webServer.requestBrowser;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// true if the browser is recognized mobile
        /// </summary>
        public override bool BrowserIsMobile {
            get {
                return cp.core.session.visit.mobile;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// get the request cookie value
        /// </summary>
        /// <param name="CookieName"></param>
        /// <returns></returns>
        public override string Cookie(string CookieName) {
            if (!cp.core.webServer.requestCookies.ContainsKey(CookieName)) { return ""; }
            return cp.core.webServer.requestCookies[CookieName].value;
        }
        //
        //====================================================================================================
        /// <summary>
        /// return a url encoded string that includes the simple name value pairs for all request cookies
        /// </summary>
        /// <returns></returns>
        public override string CookieString {
            get {
                string returnCookies = "";
                foreach (KeyValuePair<string,HttpContextRequestCookie> kvp in cp.core.webServer.httpContext.Request.Cookies) {
                    returnCookies += "&" + GenericController.encodeRequestVariable(kvp.Key) + "=" + GenericController.encodeRequestVariable(kvp.Value.Value);
                }
                if (returnCookies.Length > 0) {
                    returnCookies = returnCookies.Substring(1);
                }
                return returnCookies;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// request form in a querystring form
        /// </summary>
        public override string Form {
            get {
                return Controllers.GenericController.convertNameValueDictToREquestString(cp.core.webServer.requestForm);
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// The request querystring, to be used for actions
        /// </summary>
        public override string FormAction {
            get {
                return cp.core.webServer.requestFormActionURL;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// get property
        /// </summary>
        /// <param name="RequestName"></param>
        /// <returns></returns>
        public override bool GetBoolean(string RequestName) {
            return cp.core.docProperties.getBoolean(RequestName);
        }
        //
        //====================================================================================================
        /// <summary>
        /// get property
        /// </summary>
        /// <param name="RequestName"></param>
        /// <returns></returns>
        public override DateTime GetDate(string RequestName) {
            return cp.core.docProperties.getDate(RequestName);
        }
        //
        //====================================================================================================
        /// <summary>
        /// get property
        /// </summary>
        /// <param name="RequestName"></param>
        /// <returns></returns>
        public override int GetInteger(string RequestName) {
            return cp.core.docProperties.getInteger(RequestName);
        }
        //
        //====================================================================================================
        /// <summary>
        /// get property
        /// </summary>
        /// <param name="RequestName"></param>
        /// <returns></returns>
        public override double GetNumber(string RequestName) {
            return cp.core.docProperties.getNumber(RequestName);
        }
        //
        //====================================================================================================
        /// <summary>
        /// get property
        /// </summary>
        /// <param name="RequestName"></param>
        /// <returns></returns>
        public override string GetText(string RequestName) {
            return cp.core.docProperties.getText(RequestName);
        }
        //
        //====================================================================================================
        /// <summary>
        /// The domain used on the request
        /// </summary>
        public override string Host {
            get {
                return cp.core.webServer.requestDomain;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// legacy server variable with no internal use
        /// </summary>
        public override string HTTPAccept {
            get {
                if ((cp.core.webServer.httpContext == null) || (cp.core.webServer.httpContext.Request == null) || (cp.core.webServer.httpContext.Request.ServerVariables == null)) { return ""; }
                if (!cp.core.webServer.httpContext.Request.ServerVariables.ContainsKey("HTTP_ACCEPT")) { return ""; }
                return cp.core.webServer.httpContext.Request.ServerVariables["HTTP_ACCEPT"];
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// legacy server variable with no internal use
        /// </summary>
        public override string HTTPAcceptCharset {
            get {
                if ((cp.core.webServer.httpContext == null) || (cp.core.webServer.httpContext.Request == null) || (cp.core.webServer.httpContext.Request.ServerVariables == null)) { return ""; }
                if (!cp.core.webServer.httpContext.Request.ServerVariables.ContainsKey("HTTP_ACCEPT_CHARSET")) { return ""; }
                return cp.core.webServer.httpContext.Request.ServerVariables["HTTP_ACCEPT_CHARSET"];
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// legacy server variable with no internal use
        /// </summary>
        public override string HTTPProfile {
            get {
                if ((cp.core.webServer.httpContext == null) || (cp.core.webServer.httpContext.Request == null) || (cp.core.webServer.httpContext.Request.ServerVariables == null)) { return ""; }
                if (!cp.core.webServer.httpContext.Request.ServerVariables.ContainsKey("HTTP_PROFILE")) { return ""; }
                return cp.core.webServer.httpContext.Request.ServerVariables["HTTP_PROFILE"];
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// legacy server variable with no internal use
        /// </summary>
        public override string HTTPXWapProfile {
            get {
                if ((cp.core.webServer.httpContext == null) || (cp.core.webServer.httpContext.Request == null) || (cp.core.webServer.httpContext.Request.ServerVariables == null)) { return ""; }
                if (!cp.core.webServer.httpContext.Request.ServerVariables.ContainsKey("HTTP_X_WAP_PROFILE")) { return ""; }
                return cp.core.webServer.httpContext.Request.ServerVariables["HTTP_X_WAP_PROFILE"];
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// legacy server variable with no internal use
        /// </summary>
        public override string Language {
            get {
                if (cp.core.session.userLanguage == null) { return ""; }
                LanguageModel userLanguage = DbBaseModel.create<LanguageModel>(cp, cp.core.session.user.languageId);
                if (userLanguage != null) { return userLanguage.name; }
                return "English";
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// wrapper around protected webServer property
        /// </summary>
        public override string Link {
            get {
                return cp.core.webServer.requestUrl;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// wrapper around protected webServer property
        /// </summary>
        public override string LinkForwardSource {
            get {
                return cp.core.webServer.requestLinkForwardSource;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// wrapper around protected webServer property
        /// </summary>
        public override string LinkSource {
            get {
                return cp.core.webServer.requestUrlSource;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// wrapper around protected webServer property
        /// </summary>
        public override string Page {
            get {
                return cp.core.webServer.requestPage;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// wrapper around protected webServer property
        /// </summary>
        public override string Path {
            get {
                return cp.core.webServer.requestPath;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// wrapper around protected webServer property
        /// </summary>
        public override string PathPage {
            get {
                return cp.core.webServer.requestPathPage;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// wrapper around protected webServer property
        /// </summary>
        public override string Protocol {
            get {
                return cp.core.webServer.requestProtocol;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// wrapper around protected webServer property
        /// </summary>
        public override string QueryString {
            get {
                return cp.core.webServer.requestQueryString;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// wrapper around protected webServer property
        /// </summary>
        public override string Referer {
            get {
                return cp.core.webServer.requestReferer;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// wrapper around protected webServer property
        /// </summary>
        public override string RemoteIP {
            get {
                return cp.core.webServer.requestRemoteIP;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// wrapper around protected webServer property
        /// </summary>
        public override bool Secure {
            get {
                return cp.core.webServer.requestSecure;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// legacy property not supported in the webserver
        /// </summary>
        public override string Body {
            get {
                if ((cp.core.webServer.httpContext == null) || (cp.core.webServer.httpContext.Request == null)) { return ""; }
                return cp.core.webServer.httpContext.Request.requestBody;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// wrapper around protected webServer property
        /// </summary>
        public override string ContentType {
            get {
                return cp.core.webServer.requestContentType;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// legacy property. true if form or querystring includes this property
        /// </summary>
        /// <param name="RequestName"></param>
        /// <returns></returns>
        public override bool OK(string RequestName) {
            if (cp.core.webServer.httpContext.Request.QueryString.ContainsKey(RequestName)) { return true; }
            if (cp.core.webServer.httpContext.Request.Form.ContainsKey(RequestName)) { return true; }
            return false;
        }
        //
        // deprecated
        //====================================================================================================
        /// <summary>
        /// deprecated, return false
        /// </summary>
        [Obsolete]
        public override bool BrowserIsIE { get { return false; } }
        /// <summary>
        /// deprecated, return false
        /// </summary>
        [Obsolete]
        public override bool BrowserIsMac { get { return false; } }
        /// <summary>
        /// deprecated, return false
        /// </summary>
        [Obsolete]
        public override bool BrowserIsWindows { get { return false; } }
        /// <summary>
        /// deprecated, return false
        /// </summary>
        [Obsolete]
        public override string BrowserVersion { get { return ""; } }
        //
        #region  IDisposable Support 
        //
        //====================================================================================================
        // dispose
        protected virtual void Dispose(bool disposing_req) {
            if (!this.disposed_req) {
                if (disposing_req) {
                    //
                    // call .dispose for managed objects
                    //
                }
                //
                // Add code here to release the unmanaged resource.
                //
            }
            this.disposed_req = true;
        }
        protected bool disposed_req;
        // Do not change or add Overridable to these methods.
        // Put cleanup code in Dispose(ByVal disposing As Boolean).
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        ~CPRequestClass() {
            Dispose(false);


        }
        #endregion
    }
}