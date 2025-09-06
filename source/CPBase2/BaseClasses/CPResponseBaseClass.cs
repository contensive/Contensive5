
using System;

namespace Contensive.BaseClasses {
    /// <summary>
    /// Control over the optional http response
    /// </summary>
    public abstract class CPResponseBaseClass {
        //
        //====================================================================================================
        /// <summary>
        /// set of geth the content type of the response.
        /// </summary>
        public abstract string ContentType { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// A key=value pair string of all cookies being sent in the response
        /// </summary>
        public abstract string Cookies { get; }
        //
        //====================================================================================================
        /// <summary>
        /// response headers
        /// </summary>
        public abstract string Header { get; }
        //
        //====================================================================================================
        /// <summary>
        /// Clear all response output
        /// </summary>
        public abstract void Clear();
        //
        //====================================================================================================
        /// <summary>
        /// Optional finalize the response. 
        /// </summary>
        public abstract void Close();
        //
        //====================================================================================================
        /// <summary>
        /// Add a key=value pair to the http header
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public abstract void AddHeader(string key, string value);
        //
        //====================================================================================================
        /// <summary>
        /// flear the response
        /// </summary>
        public abstract void Flush();
        //
        //====================================================================================================
        /// <summary>
        /// Set the response redirect
        /// </summary>
        /// <param name="link"></param>
        public abstract void Redirect(string link);
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        /// <param name="bufferOn"></param>
        [Obsolete("Deprecated. Output buffer is deprecated",false)]
        public abstract void SetBuffer(bool bufferOn);
        //
        //====================================================================================================
        /// <summary>
        /// Set the http response. ex 200 Success
        /// </summary>
        /// <param name="status"></param>
        public abstract void SetStatus(string status);
        //
        //====================================================================================================
        /// <summary>
        /// set response content type
        /// </summary>
        /// <param name="contentType"></param>
        public abstract void SetType(string contentType);
        //
        //====================================================================================================
        /// <summary>
        /// set a simple response cookie value
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public abstract void SetCookie(string key, string value);
        //
        //====================================================================================================
        /// <summary>
        /// set a response cookie
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="dateExpires"></param>
        public abstract void SetCookie(string key, string value, DateTime dateExpires);
        //
        //====================================================================================================
        /// <summary>
        /// set a response cookie
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="dateExpires"></param>
        /// <param name="domain"></param>
        public abstract void SetCookie(string key, string value, DateTime dateExpires, string domain);
        //
        //====================================================================================================
        /// <summary>
        /// set a response cookie
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="dateExpires"></param>
        /// <param name="domain"></param>
        /// <param name="path"></param>
        public abstract void SetCookie(string key, string value, DateTime dateExpires, string domain, string path);
        //
        //====================================================================================================
        /// <summary>
        /// set a response cookie
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="dateExpires"></param>
        /// <param name="domain"></param>
        /// <param name="path"></param>
        /// <param name="secure"></param>
        public abstract void SetCookie(string key, string value, DateTime dateExpires, string domain, string path, bool secure);
        //
        //====================================================================================================
        /// <summary>
        /// Is the response object available to write. False during background processes and after the html response has ended. For instance, when a remote method is returned the response is closed meaning no other data should be added to the output.
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public abstract bool isOpen { get; }
        //
        //====================================================================================================
        /// <summary>
        /// deprecated. content generation no longer includes a buffer
        /// </summary>
        /// <param name="content"></param>
        [Obsolete("The write buffer is deprecated")]
        public abstract void Write(string content);
        //
        /// <summary>
        /// Deprecated. Set http response timeout directly in response client.
        /// </summary>
        /// <param name="timeoutSeconds"></param>
        [Obsolete("Deprecated. Set http response timeout directly in response client.", true)]
        public abstract void SetTimeout(string timeoutSeconds);
    }
}

