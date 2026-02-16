
using System;

namespace Contensive.BaseClasses {
    /// <summary>
    /// Properties of optional http request
    /// </summary>
    public abstract class CPRequestBaseClass {
        /// <summary>
        /// The browser string of the request
        /// </summary>
        public abstract string Browser { get; }
        /// <summary>
        /// true if the browser is a mobile device
        /// </summary>
        public abstract bool BrowserIsMobile { get; }
        /// <summary>
        /// Cookie by name
        /// </summary>
        /// <param name="cookieName"></param>
        /// <returns></returns>
        public abstract string Cookie(string cookieName);
        /// <summary>
        /// Full cookie list from the browser. empty line seperates cookies. name=value pairs
        /// </summary>
        public abstract string CookieString { get; }
        /// <summary>
        /// Full key=value list for the form submitted
        /// </summary>
        public abstract string Form { get; }
        /// <summary>
        /// request verb
        /// </summary>
        public abstract string FormAction { get; }
        /// <summary>
        /// return a value for a key=value pair
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public abstract bool GetBoolean(string key);
        /// <summary>
        /// return a value for a key=value pair
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public abstract DateTime GetDate(string key);
        /// <summary>
        /// return a value for a key=value pair
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public abstract int GetInteger(string key);
        /// <summary>
        /// return a value for a key=value pair
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public abstract double GetNumber(string key);
        /// <summary>
        /// return a value for a key=value pair
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public abstract string GetText(string key);
        /// return a value for a key=value pair.
        /// use only for input that expects text but a user might enter html. 
        /// This method will return the text with html removed. If the input is expected to be html, use GetText() instead.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public abstract string GetTextSafe(string key);
        /// <summary>
        /// The requested domain
        /// </summary>
        public abstract string Host { get; }
        /// <summary>
        /// The requested accept type
        /// </summary>
        public abstract string HTTPAccept { get; }
        /// <summary>
        /// The requested accect character set
        /// </summary>
        public abstract string HTTPAcceptCharset { get; }
        /// <summary>
        /// 
        /// </summary>
        public abstract string HTTPProfile { get; }
        /// <summary>
        /// 
        /// </summary>
        public abstract string HTTPXWapProfile { get; }
        /// <summary>
        /// The requested language
        /// </summary>
        public abstract string Language { get; }
        /// <summary>
        /// The requested link
        /// </summary>
        public abstract string Link { get; }
        /// <summary>
        /// 
        /// </summary>
        public abstract string LinkForwardSource { get; }
        /// <summary>
        /// 
        /// </summary>
        public abstract string LinkSource { get; }
        /// <summary>
        /// the right-most segment of the url. The page for website urls
        /// </summary>
        public abstract string Page { get; }
        /// <summary>
        /// the url segment between the page and the domain
        /// </summary>
        public abstract string Path { get; }
        /// <summary>
        /// The path and page segments of the url
        /// </summary>
        public abstract string PathPage { get; }
        /// <summary>
        /// The request protocol (http, https, etc)
        /// </summary>
        public abstract string Protocol { get; }
        /// <summary>
        /// The request querystring (segment following the question mark)
        /// </summary>
        public abstract string QueryString { get; }
        /// <summary>
        /// The refering url from the browser
        /// </summary>
        public abstract string Referer { get; }
        /// <summary>
        /// The IP of the request
        /// </summary>
        public abstract string RemoteIP { get; }
        /// <summary>
        /// true if the request is https
        /// </summary>
        public abstract bool Secure { get; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="RequestName"></param>
        /// <returns></returns>
        public abstract bool OK(string RequestName);
        /// <summary>
        /// The body of the entire request entity. Use when iis does not parse the body into form elements, such as application/json
        /// </summary>
        public abstract string Body { get; }
        /// <summary>
        /// The content type of the request
        /// </summary>
        public abstract string ContentType { get; }
        //
        //====================================================================================================
        // deprecated
        //
        [Obsolete("Deprecated", false)] public abstract bool BrowserIsIE { get; }
        [Obsolete("Deprecated", false)] public abstract bool BrowserIsMac { get; }
        [Obsolete("Deprecated", false)] public abstract bool BrowserIsWindows { get; }
        [Obsolete("Deprecated", false)] public abstract string BrowserVersion { get; }
    }
}

