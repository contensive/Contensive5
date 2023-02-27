using Contensive.BaseModels;
using System;
using System.Collections.Generic;

namespace Contensive.Processor.Models.Domain {
    //
    //====================================================================================================
    /// <summary>
    /// Parameter object for CPClass to provide website request context
    /// </summary>
    public class HttpContextModel : HttpContextBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// construct
        /// </summary>
        public HttpContextModel() {
            Request = new HttpContextRequest();
            Response = new HttpContextResponse();
            ApplicationInstance = new HttpContextApplicationInstance();
        }
        //
        //====================================================================================================
        /// <summary>
        /// Request
        /// </summary>
        public HttpContextRequest Request;
        //
        //====================================================================================================
        /// <summary>
        /// Response
        /// </summary>
        public HttpContextResponse Response;
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public HttpContextApplicationInstance ApplicationInstance;
    }
    //
    //====================================================================================================
    /// <summary>
    /// 
    /// </summary>
    public class HttpContextApplicationInstance {
        /// <summary>
        /// 
        /// </summary>
        public void CompleteRequest() {

        }
    }
    //
    //====================================================================================================
    /// <summary>
    /// Request Object
    /// </summary>
    public class HttpContextRequest {
        //
        //====================================================================================================
        /// <summary>
        /// construct
        /// </summary>
        public HttpContextRequest() {
            Url = new HttpContentRequestUrl();
            Cookies = new Dictionary<string, HttpContextRequestCookie>(StringComparer.InvariantCultureIgnoreCase);
            Form = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            QueryString = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            Headers = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            ServerVariables = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            Files = new List<DocPropertyModel>();
        }
        //
        //====================================================================================================
        /// <summary>
        /// Request Url
        /// </summary>
        public HttpContentRequestUrl Url { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public Uri UrlReferrer { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public Dictionary<string, HttpContextRequestCookie> Cookies { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public Dictionary<string, string> Form { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public Dictionary<string, string> QueryString { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public Dictionary<string, string> Headers { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public Dictionary<string, string> ServerVariables { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// List of uploaded file details, physical files saved to windowsTempFiles
        /// </summary>
        public List<DocPropertyModel> Files { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public string ContentType { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// The content body created before the call
        /// </summary>
        public string requestBody { get; set; }
    }
    //
    //====================================================================================================
    /// <summary>
    /// Request Url
    /// </summary>
    public class HttpContentRequestUrl {
        //
        //====================================================================================================
        /// <summary>
        /// port
        /// </summary>
        public int Port;
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public string AbsoluteUri;
    }
    //
    //====================================================================================================
    /// <summary>
    /// 
    /// </summary>
    public class HttpContextRequestCookie {
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public string Name;
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public string Value;
    }
    //
    //====================================================================================================
    /// <summary>
    /// Response object. System adds to it, parent client
    /// </summary>
    public class HttpContextResponse {
        //
        //====================================================================================================
        /// <summary>
        /// construct
        /// </summary>
        public HttpContextResponse() {
            cookies = new Dictionary<string, HttpContextResponseCookie>(StringComparer.InvariantCultureIgnoreCase);
            headers = new List<HttpContextResponseHeader>();
            cacheControl = "no-cache";
            expires = -1;
            buffer = true;
            contentType = "";
        }
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public void Flush() {
            // clear the response
        }
        //
        //====================================================================================================
        /// <summary>
        /// Redirect Url. if not blank, redirect on output
        /// </summary>
        public string redirectUrl { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// List of headers to pass back to the http client
        /// </summary>
        public List<HttpContextResponseHeader> headers { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public string contentType { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public string status { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public Dictionary<string, HttpContextResponseCookie> cookies;
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public string cacheControl { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public int expires { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public bool buffer { get; set; }
    }
    //
    //====================================================================================================
    /// <summary>
    /// Response Headers
    /// </summary>
    public class HttpContextResponseHeader {
        //
        //====================================================================================================
        /// <summary>
        /// Header name
        /// </summary>
        public string name;
        //
        //====================================================================================================
        /// <summary>
        /// Header Value
        /// </summary>
        public string value;
    }
    //
    //====================================================================================================
    /// <summary>
    /// 
    /// </summary>
    public class HttpContextResponseCookie {
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public bool httpOnly;
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public HttpContextResponseCookieSameSiteMode sameSite;
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public string value;
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public DateTime expires;
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public string domain;
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public string path;
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public bool secure;
    }
    //
    //====================================================================================================
    /// <summary>
    /// 
    /// </summary>
    public enum HttpContextResponseCookieSameSiteMode {
        /// <summary>
        /// same site mode is the only mode supported
        /// </summary>
        Lax = 1
    }
}

