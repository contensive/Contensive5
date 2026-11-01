
using System;
using System.Reflection;
using static Contensive.Processor.Controllers.GenericController;
//
// todo -- this should use the filesystem objects, not system.io
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Contensive.Exceptions;
using System.Text;
using System.Collections.Generic;
using System.Collections.Specialized;
//
namespace Contensive.Processor.Controllers {
    //
    public class HttpController : IDisposable {
        private string _password;
        private string _username;
        private string _setCookie;
        private string privateResponseFilename;
        //
        // had to fake bc webClient removes first line of header
        private readonly string privateResponseProtocol = "HTTP/1.1";
        private string _responseStatusDescription;
        private int _responseStatusCode;
        private HttpResponseHeaders _responseHeaders;
        private HttpRequestHeaders _requestHeaders;
        private readonly string _socketResponse = "";
        //
        private HttpClientHandler _handler;
        private HttpClient _client;
        private bool _disposed;
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
            userAgent = $"contensive/{myVersion.Major:0}.{myVersion.Minor:0000}.{myVersion.Build:00}";
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
            return string.IsNullOrWhiteSpace(webAddressProtocolDomain) ? $"https://{core.appConfig.domainList[0]}" : webAddressProtocolDomain;
        }
        //
        //======================================================================================
        /// <summary>
        /// Lazily create the HttpClient and handler when first needed, applying current instance settings.
        /// </summary>
        private HttpClient ensureClient() {
            if (_client != null) { return _client; }
            _handler = new HttpClientHandler {
                CookieContainer = new CookieContainer(),
                UseCookies = true
            };
            if (!string.IsNullOrEmpty(_username)) {
                _handler.Credentials = new NetworkCredential(_username, _password);
            }
            if (!string.IsNullOrEmpty(_setCookie)) {
                string[] cookies = _setCookie.Split(';');
                for (int i = 0; i <= cookies.GetUpperBound(0); i++) {
                    string[] cookiePart = cookies[i].Split('=');
                    if (cookiePart.Length >= 2) {
                        _handler.CookieContainer.Add(new Cookie(cookiePart[0].Trim(), cookiePart[1].Trim(), "/"));
                    }
                }
            }
            _client = new HttpClient(_handler);
            _client.Timeout = TimeSpan.FromMilliseconds(_timeout);
            if (!string.IsNullOrEmpty(userAgent)) {
                _client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);
            }
            return _client;
        }
        //
        //======================================================================================
        //
        public string postUrl(string url) {
            return postUrl(url, new NameValueCollection());
        }
        //
        //======================================================================================
        //
        public string postUrl(string url, NameValueCollection requestArguments) {
            try {
                var client = ensureClient();
                var formData = new List<KeyValuePair<string, string>>();
                foreach (string key in requestArguments.AllKeys) {
                    formData.Add(new KeyValuePair<string, string>(key, requestArguments[key]));
                }
                using (var content = new FormUrlEncodedContent(formData)) {
                    _requestHeaders = client.DefaultRequestHeaders;
                    var response = client.PostAsync(url, content).Result;
                    _responseStatusCode = (int)response.StatusCode;
                    _responseStatusDescription = response.ReasonPhrase;
                    _responseHeaders = response.Headers;
                    response.EnsureSuccessStatusCode();
                    return response.Content.ReadAsStringAsync().Result;
                }
            } catch (AggregateException aex) when (aex.InnerException != null) {
                throw new HttpException($"Exception in postUrl({url})", aex.InnerException);
            } catch (Exception ex) {
                throw new HttpException($"Exception in postUrl({url})", ex);
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
                var client = ensureClient();
                //
                privateResponseFilename = physicalFilename;
                string path = physicalFilename.Replace("/", "\\");
                int ptr = path.LastIndexOf("\\", StringComparison.InvariantCulture);
                if (ptr > 0) {
                    path = physicalFilename.left(ptr);
                    Directory.CreateDirectory(path);
                }
                File.Delete(privateResponseFilename);
                _requestHeaders = client.DefaultRequestHeaders;
                try {
                    var response = client.GetAsync(URL).Result;
                    _responseStatusCode = (int)response.StatusCode;
                    _responseStatusDescription = response.ReasonPhrase;
                    _responseHeaders = response.Headers;
                    response.EnsureSuccessStatusCode();
                    using (var stream = response.Content.ReadAsStreamAsync().Result)
                    using (var fileStream = new FileStream(privateResponseFilename, FileMode.Create, FileAccess.Write)) {
                        stream.CopyTo(fileStream);
                    }
                } catch {
                    //
                    // -- exception, no http data is valid
                    _responseStatusCode = 0;
                    _responseStatusDescription = "";
                    _responseHeaders = null;
                    throw;
                }
            } catch (AggregateException aex) when (aex.InnerException != null) {
                throw new HttpException($"Exception in getUrlToFile({URL},{physicalFilename})", aex.InnerException);
            } catch (Exception ex) {
                throw new HttpException($"Exception in getUrlToFile({URL},{physicalFilename})", ex);
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
                var client = ensureClient();
                _requestHeaders = client.DefaultRequestHeaders;
                try {
                    var response = client.GetAsync(url).Result;
                    _responseStatusCode = (int)response.StatusCode;
                    _responseStatusDescription = response.ReasonPhrase;
                    _responseHeaders = response.Headers;
                    response.EnsureSuccessStatusCode();
                    return response.Content.ReadAsStringAsync().Result;
                } catch {
                    //
                    // -- exception, no http data is valid
                    _responseStatusCode = 0;
                    _responseStatusDescription = "";
                    _responseHeaders = null;
                    throw;
                }
            } catch (AggregateException aex) when (aex.InnerException != null) {
                throw new HttpException($"Exception in getURL({url})", aex.InnerException);
            } catch (Exception ex) {
                throw new HttpException($"Exception in getURL({url})", ex);
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
                return getInteger(_timeout / 1000);
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
                    if (_requestHeaders == null) { return ""; }
                    var sb = new StringBuilder();
                    foreach (var header in _requestHeaders) {
                        sb.Append($"{header.Key}: {string.Join(", ", header.Value)}");
                    }
                    return sb.ToString();
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
                        returnString += $"{privateResponseProtocol} {_responseStatusCode} {_responseStatusDescription}";
                        if (_responseHeaders != null) {
                            foreach (var header in _responseHeaders) {
                                returnString += $"{Environment.NewLine}{header.Key}:{string.Join(", ", header.Value)}";
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
        //================================================================
        //
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        //
        protected virtual void Dispose(bool disposing) {
            if (_disposed) { return; }
            if (disposing) {
                _client?.Dispose();
                _handler?.Dispose();
            }
            _disposed = true;
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
        public HttpException(string context, Exception innerEx) : base($"Unknown error in http4Class, {context}, innerException [{innerEx}]") {
        }

        public HttpException() {
        }

        public HttpException(string message) : base(message) {
        }
    }
}
