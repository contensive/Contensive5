
using Contensive.Processor.Controllers;
using System;

namespace Contensive.Processor {
    /// <summary>
    /// expose httpContext response
    /// </summary>
    public class CPResponseClass : BaseClasses.CPResponseBaseClass, IDisposable {
        //
        //====================================================================================================
        /// <summary>
        /// dependencies
        /// </summary>
        private readonly CPClass cp;
        //
        //====================================================================================================
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="cp"></param>
        public CPResponseClass(CPClass cp) {
            this.cp = cp;
        }
        //
        //====================================================================================================
        /// <summary>
        /// response contenttype
        /// </summary>
        public override string ContentType {
            get {
                return cp.core.webServer.responseContentType;
            }
            set {
                cp.core.webServer.responseContentType = value;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// return response cookies in a simple name=value  format
        /// </summary>
        public override string Cookies {
            get {
                return cp.core.webServer.responseCookies;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// return response headers in a name=value format
        /// </summary>
        public override string Header {
            get {
                return cp.core.webServer.responseHeaders;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// deprecated
        /// </summary>
        public override void Clear()  {
            cp.core.webServer.clearResponseBuffer();
        }
        //
        //====================================================================================================
        /// <summary>
        /// close the response. speeds the exit from processing.
        /// </summary>
        public override void Close()  {
            cp.core.doc.continueProcessing = false;
        }
        //
        //====================================================================================================
        /// <summary>
        /// add a response header
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public override void AddHeader(string name, string value) {
            cp.core.webServer.addResponseHeader(name, value);
        }
        //
        //====================================================================================================
        /// <summary>
        /// clear the response
        /// </summary>
        public override void Flush()  {
            cp.core.webServer.flushStream();
        }
        //
        //====================================================================================================
        /// <summary>
        /// redirect to a link
        /// </summary>
        /// <param name="link"></param>
        public override void Redirect(string link) {
            string callStack = "CP.Redirect call from " + GenericController.getCallStack();
            cp.core.webServer.redirect(link, callStack, false, false);
        }
        //
        //====================================================================================================
        /// <summary>
        /// set response status
        /// </summary>
        /// <param name="status"></param>
        public override void SetStatus(string status) {
            cp.core.webServer.setResponseStatus(status);
        }
        //
        //====================================================================================================
        /// <summary>
        /// set response contentType
        /// </summary>
        /// <param name="contentType"></param>
        public override void SetType(string contentType) {
            cp.core.webServer.responseContentType = contentType;
        }
        //
        //====================================================================================================
        /// <summary>
        /// Add a reponse cookie
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public override void SetCookie(string name, string value) {
            cp.core.webServer.addResponseCookie(name, value);
        }
        //
        //====================================================================================================
        /// <summary>
        /// Add a reponse cookie
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="dateExpires"></param>
        public override void SetCookie(string name, string value, DateTime dateExpires) {
            cp.core.webServer.addResponseCookie(name, value, dateExpires);
        }
        //
        //====================================================================================================
        /// <summary>
        /// Add a reponse cookie
        /// </summary>
        /// <param name="CookieName"></param>
        /// <param name="CookieValue"></param>
        /// <param name="DateExpires"></param>
        /// <param name="Domain"></param>
        public override void SetCookie(string CookieName, string CookieValue, DateTime DateExpires, string Domain) {
            cp.core.webServer.addResponseCookie(CookieName, CookieValue, DateExpires, Domain, "/", false);
        }
        //
        //====================================================================================================
        /// <summary>
        /// Add a reponse cookie
        /// </summary>
        /// <param name="CookieName"></param>
        /// <param name="CookieValue"></param>
        /// <param name="DateExpires"></param>
        /// <param name="Domain"></param>
        /// <param name="Path"></param>
        public override void SetCookie(string CookieName, string CookieValue, DateTime DateExpires, string Domain, string Path) {
            cp.core.webServer.addResponseCookie(CookieName, CookieValue, DateExpires, Domain, Path, false);
        }
        //
        //====================================================================================================
        /// <summary>
        /// true if the response is still active. False after redirect or other response interruption
        /// </summary>
        public override bool isOpen {
            get {
                return cp.core.doc.continueProcessing;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Add a reponse cookie
        /// </summary>
        /// <param name="CookieName"></param>
        /// <param name="CookieValue"></param>
        /// <param name="DateExpires"></param>
        /// <param name="Domain"></param>
        /// <param name="Path"></param>
        /// <param name="Secure"></param>
        public override void SetCookie(string CookieName, string CookieValue, DateTime DateExpires, string Domain, string Path, bool Secure) {
            cp.core.webServer.addResponseCookie(CookieName, CookieValue, DateExpires, Domain, Path, Secure);
        }
        //
        //====================================================================================================
        /// <summary>
        /// deprecated. response no longer supports buffer
        /// </summary>
        /// <param name="message"></param>
        [Obsolete("The write buffer is deprecated")]
        public override void Write(string message) {}
        //
        /// <summary>
        /// deprecated. Set in client directly.
        /// </summary>
        /// <param name="timeoutSeconds"></param>
        [Obsolete("Deprecated. Set http response timeout directly in response client.", true)]
        public override void SetTimeout(string timeoutSeconds) {
            // 
        }
        //
        //====================================================================================================
        /// <summary>
        /// deprecated
        /// </summary>
        /// <param name="bufferOn"></param>
        [Obsolete("Deprecated. Output buffer is deprecated", false)]
        public override void SetBuffer(bool bufferOn) { 
            //
        }
        //
        #region  IDisposable Support 
        //
        // dispose
        //
        protected virtual void Dispose(bool disposing_res) {
            if (!this.disposed_res) {
                if (disposing_res) {
                    //
                    // call .dispose for managed objects
                    //
                }
                //
                // Add code here to release the unmanaged resource.
                //
            }
            this.disposed_res = true;
        }
        protected bool disposed_res;
        // Do not change or add Overridable to these methods.
        // Put cleanup code in Dispose(ByVal disposing As Boolean).
        public void Dispose()  {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        ~CPResponseClass()  {
            Dispose(false);
            
            
        }
        #endregion
    }
}