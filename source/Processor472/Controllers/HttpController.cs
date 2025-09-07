
using System;
using System.Reflection;
using static Contensive.Processor.Controllers.GenericController;
//
// todo -- this should use the filesystem objects, not system.io
using System.IO;
using System.Net;
using Contensive.Exceptions;
using System.Text;
using System.Collections.Generic;
//
namespace Contensive.Processor.Controllers {
    //
    public class HttpController {
        private WebHeaderCollection privateRequestHeaders;
        private string _password;
        private string _username;
        private string _setCookie;
        private string privateResponseFilename;
        //
        // had to fake bc webClient removes first line of header
        private readonly string privateResponseProtocol = "HTTP/1.1";
        private string _responseStatusDescription;
        private int _responseStatusCode;
        private WebHeaderCollection privateResponseHeaders = new System.Net.WebHeaderCollection();
        private readonly string _socketResponse = "";
        //
        //======================================================================================
        // constructor
        //======================================================================================
        //
        public HttpController() {
            Type myType = typeof(CoreController);
            Assembly myAssembly = Assembly.GetAssembly(myType);
            AssemblyName myAssemblyname = myAssembly.GetName();
            Version myVersion = myAssemblyname.Version;
            userAgent = "contensive/" + myVersion.Major.ToString("0") + "." + myVersion.Minor.ToString("0000") + "." + myVersion.Build.ToString("00");
            _timeout = 30000;
        }
        //
        //====================================================================================================
        /// <summary>
        /// Return the URL protocol + domain + "/" to be used to prepend rroot relative urls in images, links, etc
        /// </summary>
        /// <param name="core"></param>
        /// <returns></returns>
        public static string getCdnFilePathPrefixAbsolute(CoreController core) {
            //
            // -- if remote file system, return the cdnFileUrl
            if (!core.serverConfig.isLocalFileSystem) { return core.appConfig.cdnFileUrl; }
            //
            // -- local file system
            return getWebAddressProtocolDomain(core) + core.appConfig.cdnFileUrl;
        }
        //
        //======================================================================================
        public static string getCdnFilePathPrefix(CoreController core) {
            //
            // -- This value is determined during installation and saved in the ecdnFileUrl appConfig property
            return core.appConfig.cdnFileUrl;
        }
        //
        //======================================================================================
        public static string getWebAddressProtocolDomain(CoreController core) {
            //
            // -- This value is determined during installation and saved in the ecdnFileUrl appConfig property
            //
            // -- local file system
            string webAddressProtocolDomain = core.siteProperties.getText("webAddressProtocolDomain");
            //
            // -- if site property is empty, use https + first domain in domain list
            return string.IsNullOrWhiteSpace(webAddressProtocolDomain) ? "https://" + core.appConfig.domainList[0] : webAddressProtocolDomain;
        }
        //
        //======================================================================================
        /// <summary>
        /// initialize http object from values in this object. Call right before http method call
        /// </summary>
        /// <param name="http"></param>
        private void initHttp(WebClientExt http) {
            //
            http.password = _password;
            http.username = _username;
            http.userAgent = userAgent;
            if (!string.IsNullOrEmpty(_setCookie)) {
                string[] cookies = _setCookie.Split(';');
                for (int CookiePointer = 0; CookiePointer <= cookies.GetUpperBound(0); CookiePointer++) {
                    string[] CookiePart = cookies[CookiePointer].Split('=');
                    http.addCookie(CookiePart[0], CookiePart[1]);
                }
            }
            http.timeout = _timeout;
        }
        //
        //======================================================================================
        //
        public string postUrl(string url) {
            return postUrl(url, new System.Collections.Specialized.NameValueCollection());
        }
        //
        //======================================================================================
        //
        public string postUrl(string url, System.Collections.Specialized.NameValueCollection requestArguments) {
            using (WebClientExt http = new WebClientExt()) {
                initHttp(http);
                byte[] responsebytes = http.UploadValues(url, "POST", requestArguments);
                UTF8Encoding utf8 = new UTF8Encoding();
                return utf8.GetString(responsebytes);
            }
        }
        //
        //======================================================================================
        //   Requests the doc and saves the body in the file specified
        //
        //   check the HTTPResponse and SocketResponse when done
        //   If the HTTPResponse is "", Check the SocketResponse
        //======================================================================================
        //
        public void getUrlToFile(string URL, string physicalFilename) {
            try {
                using (WebClientExt http = new WebClientExt()) {
                    initHttp(http);
                    //
                    privateResponseFilename = physicalFilename;
                    string path = physicalFilename.Replace("/", "\\");
                    int ptr = path.LastIndexOf("\\", StringComparison.InvariantCulture);
                    if (ptr > 0) {
                        path = physicalFilename.left(ptr);
                        Directory.CreateDirectory(path);
                    }
                    File.Delete(privateResponseFilename);
                    privateRequestHeaders = http.Headers;
                    try {
                        http.DownloadFile(URL, privateResponseFilename);
                        _responseStatusCode = 200;
                        _responseStatusDescription = HttpStatusCode.OK.ToString();
                        privateResponseHeaders = http.ResponseHeaders;
                    } catch {
                        //
                        // -- exception, no http data is valid
                        _responseStatusCode = 0;
                        _responseStatusDescription = "";
                        privateResponseHeaders = new System.Net.WebHeaderCollection();
                        throw;
                    }
                }
            } catch (Exception ex) {
                throw new HttpException("Exception in getUrlToFile(" + URL + "," + physicalFilename + ")", ex);
            }
        }
        //
        //======================================================================================
        //   Returns the body of a URL requested
        //
        //   If there is an error, it returns "", and the HTTPResponse should be checked
        //   If the HTTPResponse is "", Check the SocketResponse
        //======================================================================================
        //
        public string getURL(string url) {
            try {
                using (WebClientExt http = new WebClientExt()) {
                    initHttp(http);
                    privateRequestHeaders = http.Headers;
                    try {
                        string returnString = http.DownloadString(url);
                        _responseStatusCode = 200;
                        _responseStatusDescription = HttpStatusCode.OK.ToString();
                        privateResponseHeaders = http.ResponseHeaders;
                        return returnString;

                    } catch {
                        //
                        // -- exception, no http data is valid
                        _responseStatusCode = 0;
                        _responseStatusDescription = "";
                        privateResponseHeaders = new System.Net.WebHeaderCollection();
                        throw;
                    }
                }
            } catch (Exception ex) {
                throw new HttpException("Exception in getURL(" + url + ")", ex);
            }
        }
        //
        //================================================================
        //
        public string userAgent { get; set; }
        //
        //================================================================
        //
        public int timeout {
            get {
                return encodeInteger(_timeout / 1000);
            }
            set {
                if (value > 65535) {
                    value = 65535;
                }
                _timeout = value * 1000;
            }
        }
        private int _timeout;
        //
        //================================================================
        //
        public string requestHeader {
            get {
                try {
                    string returnString = "";
                    if (privateRequestHeaders.Count > 0) {
                        for (int ptr = 0; ptr < privateRequestHeaders.Count; ptr++) {
                            returnString += privateRequestHeaders[ptr];
                        }
                    }
                    return returnString;
                } catch (Exception ex) {
                    throw new GenericException("exception in requestHeader Property, get Method", ex);
                }
            }
        }
        //
        //================================================================
        //
        public string responseHeader {
            get {
                try {
                    string returnString = "";
                    if (_responseStatusCode != 0) {
                        returnString += privateResponseProtocol + " " + _responseStatusCode + " " + _responseStatusDescription;
                        if (privateResponseHeaders.Count > 0) {
                            for (int ptr = 0; ptr < privateResponseHeaders.Count; ptr++) {
                                returnString += Environment.NewLine + privateResponseHeaders.GetKey(ptr) + ":" + privateResponseHeaders[ptr];
                            }
                        }
                    }
                    return returnString;
                } catch (Exception ex) {
                    throw new GenericException("exception in responseHeader Property, get Method", ex);
                }
            }
        }
        //
        //================================================================
        //
        public string socketResponse {
            get {
                return _socketResponse;
            }
        }
        //
        //================================================================
        //
        public string responseStatusDescription {
            get {
                return _responseStatusDescription;
            }
        }
        //
        //================================================================
        //
        public int responseStatusCode {
            get {
                return _responseStatusCode;
            }
        }
        //
        //================================================================
        //
        public string setCookie {
            set {
                _setCookie = value;
            }
        }
        //
        //================================================================
        //
        public string username {
            set {
                _username = value;
            }
        }
        //
        //================================================================
        //
        public string password {
            set {
                _password = value;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    }
    //
    // exception classes
    //
    [Serializable]
    public class HttpException : Exception {
        //
        public HttpException(string context, Exception innerEx) : base("Unknown error in http4Class, " + context + ", innerException [" + innerEx.ToString() + "]") {
        }

        public HttpException() {
        }

        public HttpException(string message) : base(message) {
        }
    }
}

