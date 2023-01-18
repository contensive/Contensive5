
using Contensive.Processor.Exceptions;
using Contensive.Processor.Models.Domain;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Web;
using static Contensive.Processor.Constants;

namespace Contensive.Processor.Controllers {
    //
    //====================================================================================================
    /// <summary>
    /// controller for shared non-specific tasks
    /// </summary>
    public static class GenericController {
        //
        //====================================================================================================
        /// <summary>
        /// returns true if first version is older than the second version
        /// </summary>
        /// <param name="versionFirst"></param>
        /// <param name="versionSecond"></param>
        /// <returns></returns>
        public static bool versionIsOlder(string versionFirst, string versionSecond) {
            try {
                //
                // -- formats:
                // c4 = 4.1.636
                //          major = 4
                //          minor = 1
                //          revision = 636
                //          build = 0
                // c5, 5.1.2.3
                //          major = 5
                //          minor = 1
                //          revision = 2
                //          build = 3
                // c5, 5.18.1027.1
                //          major = 5
                //          minor = 18
                //          revision = 1027
                //          build = 1
                // c5, 5.2001.1.1
                //          major = 5
                //          minor = 2001
                //          revision = 1
                //          build = 1
                // 20.6.14.2
                //          major = 20 (2020)
                //          minor = 6 (june)
                //          revision = 14 (day)
                //          build = 2
                string[] vfsplit = versionFirst.Split('.');
                string[] vssplit = versionSecond.Split('.');
                //
                {
                    int vf = (vfsplit.Length >= 1) ? encodeInteger(vfsplit[0]) : 0;
                    int vs = (vssplit.Length >= 1) ? encodeInteger(vssplit[0]) : 0;
                    if (vf < vs) { return true; }
                    if (vf > vs) { return false; }
                }
                {
                    int vf = (vfsplit.Length >= 2) ? encodeInteger(vfsplit[1]) : 0;
                    int vs = (vssplit.Length >= 2) ? encodeInteger(vssplit[1]) : 0;
                    if (vf < vs) { return true; }
                    if (vf > vs) { return false; }
                }
                {
                    int vf = (vfsplit.Length >= 3) ? encodeInteger(vfsplit[2]) : 0;
                    int vs = (vssplit.Length >= 3) ? encodeInteger(vssplit[2]) : 0;
                    if (vf < vs) { return true; }
                    if (vf > vs) { return false; }
                }
                {
                    int vf = (vfsplit.Length >= 4) ? encodeInteger(vfsplit[3]) : 0;
                    int vs = (vssplit.Length >= 4) ? encodeInteger(vssplit[3]) : 0;
                    if (vf < vs) { return true; }
                    if (vf > vs) { return false; }
                }
                return false;
            } catch (Exception) {
                return true;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// return a normalized guid in registry format (which include the {})
        /// </summary>
        /// <param name="CP"></param>
        /// <param name="registryFormat"></param>
        /// <returns></returns>
        public static string getGUID() => getGUID(true);
        //
        //====================================================================================================
        /// <summary>
        /// return a normalized guid.
        /// </summary>
        /// <param name="includeBraces">If true, it includes the {}, else it does not</param>
        /// <returns></returns>
        public static string getGUID(bool includeBraces) {
            string result = "";
            Guid g = Guid.NewGuid();
            if (g != Guid.Empty) {
                result = g.ToString();
                //
                if (!string.IsNullOrEmpty(result)) {
                    result = includeBraces ? "{" + result + "}" : result;
                }
            }
            return result;
        }
        /// <summary>
        /// Get a GUID with no braces or dashes, just a simple string of characters
        /// </summary>
        /// <returns></returns>
        public static string getGUIDNaked() => getGUID(false).Replace("-", "");
        //
        //====================================================================================================
        /// <summary>
        /// convert a guid in any guid format to the registery format used here
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        public static string normalizeGuid(string guid) {
            if (guid.Length == 38) { return guid.ToLowerInvariant(); }
            if (guid.Length == 36) { return "{" + guid.ToLowerInvariant() + "}"; }
            if (guid.Length == 32) {
                guid = guid.ToLowerInvariant();
                return guid.left(8) + "-" + guid.Substring(8, 4) + "-" + guid.Substring(12, 4) + "-" + guid.Substring(16, 4) + "-" + guid.Substring(20);
            }
            return string.Empty;
        }
        //
        //====================================================================================================
        /// <summary>
        /// If string is empty, return default value
        /// </summary>
        /// <param name="sourceText"></param>
        /// <param name="defaultText"></param>
        /// <returns></returns>
        public static string encodeEmpty(string sourceText, string defaultText) {
            return string.IsNullOrWhiteSpace(sourceText) ? defaultText : sourceText;
        }
        //
        //====================================================================================================
        /// <summary>
        /// convert a string to an integer. If the string is not empty, but a non-valid integer, return 0. If the string is empty, return a default value
        /// </summary>
        /// <param name="sourceText"></param>
        /// <param name="DefaultInteger"></param>
        /// <returns></returns>
        public static int encodeEmptyInteger(string sourceText, int DefaultInteger) => encodeInteger(encodeEmpty(sourceText, DefaultInteger.ToString(CultureInfo.InvariantCulture)));
        //
        //====================================================================================================
        /// <summary>
        /// convert a string to a date. If the string is empty, return the defaultDate. If the string is not a valid date, return date.minvalue
        /// </summary>
        /// <param name="sourceText"></param>
        /// <param name="DefaultDate"></param>
        /// <returns></returns>
        public static DateTime encodeEmptyDate(string sourceText, DateTime DefaultDate) => encodeDate(encodeEmpty(sourceText, DefaultDate.ToString(CultureInfo.InvariantCulture)));
        //
        //====================================================================================================
        /// <summary>
        /// convert a string to a double. If the string is empty, return the default number. If the string is not a valid number, return 0.0
        /// </summary>
        /// <param name="sourceText"></param>
        /// <param name="DefaultNumber"></param>
        /// <returns></returns>
        public static double encodeEmptyNumber(string sourceText, double DefaultNumber) => encodeNumber(encodeEmpty(sourceText, DefaultNumber.ToString(CultureInfo.InvariantCulture)));
        //
        //====================================================================================================
        /// <summary>
        /// convert a string to a boolean. If the string is empty, return the default state. if the string is empty, return false
        /// </summary>
        /// <param name="sourceText"></param>
        /// <param name="DefaultState"></param>
        /// <returns></returns>
        public static bool encodeEmptyBoolean(string sourceText, bool DefaultState) => encodeBoolean(encodeEmpty(sourceText, DefaultState.ToString(CultureInfo.InvariantCulture)));
        //
        //=============================================================================
        /// <summary>
        /// Modify the querystring at the end of a link. If there is no, question mark, the link argument is assumed to be a link, not the querysting
        /// </summary>
        /// <param name="link"></param>
        /// <param name="queryName"></param>
        /// <param name="queryValue"></param>
        /// <param name="addIfMissing"></param>
        /// <returns></returns>
        public static string modifyLinkQuery(string link, string queryName, string queryValue, bool addIfMissing) {
            string tempmodifyLinkQuery = null;
            try {
                string[] Element = { };
                int ElementCount = 0;
                int ElementPointer = 0;
                string[] NameValue = null;
                string UcaseQueryName = null;
                bool ElementFound = false;
                bool iAddIfMissing = false;
                string QueryString = null;
                //
                iAddIfMissing = addIfMissing;
                if (strInstr(1, link, "?") != 0) {
                    tempmodifyLinkQuery = link.left(strInstr(1, link, "?") - 1);
                    QueryString = link.Substring(tempmodifyLinkQuery.Length + 1);
                } else {
                    tempmodifyLinkQuery = link;
                    QueryString = "";
                }
                UcaseQueryName = toUCase(encodeRequestVariable(queryName));
                if (!string.IsNullOrEmpty(QueryString)) {
                    Element = QueryString.Split('&');
                    ElementCount = Element.GetUpperBound(0) + 1;
                    for (ElementPointer = 0; ElementPointer < ElementCount; ElementPointer++) {
                        NameValue = Element[ElementPointer].Split('=');
                        if (NameValue.GetUpperBound(0) == 1) {
                            if (toUCase(NameValue[0]) == UcaseQueryName) {
                                if (string.IsNullOrEmpty(queryValue)) {
                                    Element[ElementPointer] = "";
                                } else {
                                    Element[ElementPointer] = queryName + "=" + queryValue;
                                }
                                ElementFound = true;
                                break;
                            }
                        }
                    }
                }
                if (!ElementFound && (!string.IsNullOrEmpty(queryValue))) {
                    //
                    // element not found, it needs to be added
                    //
                    if (iAddIfMissing) {
                        if (string.IsNullOrEmpty(QueryString)) {
                            QueryString = encodeRequestVariable(queryName) + "=" + encodeRequestVariable(queryValue);
                        } else {
                            QueryString = QueryString + "&" + encodeRequestVariable(queryName) + "=" + encodeRequestVariable(queryValue);
                        }
                    }
                } else {
                    //
                    // element found
                    //
                    QueryString = string.Join("&", Element);
                    if ((!string.IsNullOrEmpty(QueryString)) && (string.IsNullOrEmpty(queryValue))) {
                        //
                        // element found and needs to be removed
                        //
                        QueryString = strReplace(QueryString, "&&", "&");
                        if (QueryString.left(1) == "&") {
                            QueryString = QueryString.Substring(1);
                        }
                        if (QueryString.Substring(QueryString.Length - 1) == "&") {
                            QueryString = QueryString.left(QueryString.Length - 1);
                        }
                    }
                }
                if (!string.IsNullOrEmpty(QueryString)) {
                    tempmodifyLinkQuery = tempmodifyLinkQuery + "?" + QueryString;
                }
            } catch (Exception ex) {
                throw new GenericException("Exception in modifyLinkQuery", ex);
            }
            //
            return tempmodifyLinkQuery;
        }
        //
        public static string modifyLinkQuery(string link, string queryName, string queryValue) => modifyLinkQuery(link, queryName, queryValue, true);
        //
        //=============================================================================
        /// <summary>
        /// add the values from the addQuery to the primaryQuery
        /// </summary>
        /// <param name="primaryQuery"></param>
        /// <param name="addQuery"></param>
        /// <returns></returns>
        public static string joinQueryString(string primaryQuery, string addQuery) {
            if (string.IsNullOrWhiteSpace(addQuery)) { return primaryQuery; }
            foreach (string queryPair in addQuery.Split('&')) {
                string[] queryVar = queryPair.Split('=');
                if (queryVar.Length < 2) { continue; }
                primaryQuery = modifyQueryString(primaryQuery, queryVar[0], queryVar[1]);
            }
            return primaryQuery;
        }
        //
        //=============================================================================
        /// <summary>
        /// Create the part of the sql where clause that is modified by the user, WorkingQuery is the original querystring to change, QueryName is the name part of the name pair to change, If the QueryName is not found in the string
        /// </summary>
        /// <param name="workingQuery"></param>
        /// <param name="queryName"></param>
        /// <param name="queryValue"></param>
        /// <param name="addIfMissing"></param>
        /// <returns></returns>
        public static string modifyQueryString(string workingQuery, string queryName, string queryValue, bool addIfMissing) {
            string result = "";
            //
            if (workingQuery.IndexOf("?", StringComparison.InvariantCultureIgnoreCase) >= 0) {
                result = modifyLinkQuery(workingQuery, queryName, queryValue, addIfMissing);
            } else {
                result = modifyLinkQuery("?" + workingQuery, queryName, queryValue, addIfMissing);
                if (result.Length > 0) {
                    result = result.Substring(1);
                }
            }
            return result;
        }
        //
        //=============================================================================
        //
        public static string modifyQueryString(string workingQuery, string queryName, string queryValue) => modifyQueryString(workingQuery, queryName, queryValue, true);
        //
        //=============================================================================
        //
        public static string modifyQueryString(string WorkingQuery, string QueryName, int QueryValue, bool AddIfMissing) => modifyQueryString(WorkingQuery, QueryName, QueryValue.ToString(CultureInfo.InvariantCulture), AddIfMissing);
        //
        public static string modifyQueryString(string WorkingQuery, string QueryName, int QueryValue) => modifyQueryString(WorkingQuery, QueryName, QueryValue.ToString(CultureInfo.InvariantCulture), true);
        //
        //=============================================================================
        //
        public static string modifyQueryString(string WorkingQuery, string QueryName, bool QueryValue, bool AddIfMissing) => modifyQueryString(WorkingQuery, QueryName, QueryValue.ToString(CultureInfo.InvariantCulture), AddIfMissing);
        //
        public static string modifyQueryString(string WorkingQuery, string QueryName, bool QueryValue) => modifyQueryString(WorkingQuery, QueryName, QueryValue.ToString(CultureInfo.InvariantCulture), true);
        //
        //========================================================================
        /// <summary>
        /// legacy indent - now returns source
        /// </summary>
        /// <param name="Source"></param>
        /// <param name="depth"></param>
        /// <returns></returns>
        public static string nop(string Source, int depth = 1) {
            return Source;
        }
        //
        //========================================================================================================
        /// <summary>
        /// convert byte array to string
        /// </summary>
        /// <param name="Bytes"></param>
        /// <returns></returns>
        public static string byteArrayToString(byte[] Bytes) {
            return System.Text.UTF8Encoding.ASCII.GetString(Bytes);
        }
        //
        //========================================================================================================
        /// <summary>
        /// RFC1123 is "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'"
        /// convert a date to a string with RFC1123 format as is used in http last-modified header, "day, DD-Mon-YYYY HH:MM:SS GMT" 
        /// Note: it does NOT perform a time change, just converts to the formatted string
        /// </summary>
        /// <param name="DateValue"></param>
        /// <returns></returns>
        //
        public static string getRFC1123PatternDateFormat(DateTime DateValue) {
            //
            return DateValue.ToString("R");
        }
        //
        //========================================================================================================
        /// <summary>
        /// convert the enum status into a displayable caption
        /// </summary>
        /// <param name="ApplicationStatus"></param>
        /// <returns></returns>
        public static string getApplicationStatusMessage(AppConfigModel.AppStatusEnum ApplicationStatus) {
            string tempGetApplicationStatusMessage = null;

            switch (ApplicationStatus) {
                case AppConfigModel.AppStatusEnum.ok:
                    tempGetApplicationStatusMessage = "Application OK";
                    break;
                case AppConfigModel.AppStatusEnum.maintenance:
                    tempGetApplicationStatusMessage = "Application building";
                    break;
                default:
                    tempGetApplicationStatusMessage = "Unknown status code [" + ApplicationStatus + "], see trace log for details";
                    break;
            }
            return tempGetApplicationStatusMessage;
        }
        //
        //========================================================================================================
        /// <summary>
        /// Legacy spacer
        /// </summary>
        /// <param name="Width"></param>
        /// <param name="Height"></param>
        /// <returns></returns>
        public static string nop2(int Width, int Height) => string.Empty;
        //
        //========================================================================================================
        /// <summary>
        /// return an array of strings split on new line (crlf)
        /// </summary>
        /// <param name="textToSplit"></param>
        /// <returns></returns>
        public static string[] splitNewLine(string textToSplit) {
            return textToSplit.Split(new[] { windowsNewLine, macNewLine, unixNewLine }, StringSplitOptions.None);
        }
        //
        //========================================================================================================
        /// <summary>
        /// legacy encode - not referenced but the decode is still used, so this will be needed
        /// </summary>
        /// <param name="Arg"></param>
        /// <returns></returns>
        public static string encodeAddonConstructorArgument(string Arg) {
            string a = Arg;
            a = strReplace(a, "\\", "\\\\");
            a = strReplace(a, Environment.NewLine, "\\n");
            a = strReplace(a, "\t", "\\t");
            a = strReplace(a, "&", "\\&");
            a = strReplace(a, "=", "\\=");
            a = strReplace(a, ",", "\\,");
            a = strReplace(a, "\"", "\\\"");
            a = strReplace(a, "'", "\\'");
            a = strReplace(a, "|", "\\|");
            a = strReplace(a, "[", "\\[");
            a = strReplace(a, "]", "\\]");
            a = strReplace(a, ":", "\\:");
            return a;
        }
        //
        //========================================================================================================
        /// <summary>
        /// Decodes an argument parsed from an AddonConstructorString for all non-allowed characters.
        ///       AddonConstructorString is a &amp; delimited string of name=value[selector]descriptor
        ///       to get a value from an AddonConstructorString, first use getargument() to get the correct value[selector]descriptor
        ///       then remove everything to the right of any '['
        ///       call encodeAddonConstructorargument before parsing them together
        ///       call decodeAddonConstructorArgument after parsing them apart
        ///       Arg0,Arg1,Arg2,Arg3,Name=Value Name=VAlue[Option1|Option2]
        ///       This routine is needed for all Arg, Name, Value, Option values
        /// </summary>
        /// <param name="EncodedArg"></param>
        /// <returns></returns>
        public static string decodeAddonConstructorArgument(string EncodedArg) {
            string a;
            //
            a = EncodedArg;
            a = strReplace(a, "\\:", ":");
            a = strReplace(a, "\\]", "]");
            a = strReplace(a, "\\[", "[");
            a = strReplace(a, "\\|", "|");
            a = strReplace(a, "\\'", "'");
            a = strReplace(a, "\\\"", "\"");
            a = strReplace(a, "\\,", ",");
            a = strReplace(a, "\\=", "=");
            a = strReplace(a, "\\&", "&");
            a = strReplace(a, "\\t", "\t");
            a = strReplace(a, "\\n", Environment.NewLine);
            a = strReplace(a, "\\\\", "\\");
            return a;
        }
        //
        // ====================================================================================================
        /// <summary>
        /// return argument for separateUrl
        /// </summary>
        public class UrlDetailsClass {
            /// <summary>
            /// constructor
            /// </summary>
            public UrlDetailsClass() {
                pathSegments = new List<string>();
            }
            /// <summary>
            /// 'http://' or 'https://'
            /// </summary>
            public string protocol { get; set; }
            /// <summary>
            /// The domain name
            /// </summary>
            public string host { get; set; }
            /// <summary>
            /// the port
            /// </summary>
            public string port { get; set; }
            /// <summary>
            /// The each of the path segments. An empty pathSegments is the path "/". Path segments are constructed with slashes before and after.
            /// </summary>
            public List<String> pathSegments { get; set; }
            /// <summary>
            /// The rightmost segment of the path if not followed by a slash
            /// </summary>
            public string filename { get; set; }
            /// <summary>
            /// query string
            /// </summary>
            public string queryString { get; set; }
            /// <summary>
            /// The path constructed from pathsegments with '/'
            /// </summary>
            /// <returns></returns>
            public string unixPath {
                get {
                    string path = string.Join("/", pathSegments);
                    if (string.IsNullOrEmpty(path)) { return "/"; }
                    return "/" + path + "/";
                }
            }
            /// <summary>
            /// The path constructed from pathsegments with '\'
            /// </summary>
            /// <returns></returns>
            public string dosPath {
                get {
                    string path = string.Join("\\", pathSegments);
                    if (string.IsNullOrEmpty(path)) { return "\\"; }
                    return "\\" + path + "\\";
                }
            }
        }
        //
        // ====================================================================================================
        /// <summary>
        /// split a source Url into its components. Url and Uri are always UNIX slashed.
        /// </summary>
        /// <param name="sourceUrl"></param>
        public static UrlDetailsClass splitUrl(string sourceUrl) {
            string url = sourceUrl;
            //
            //   Divide the URL into URLHost, URLPath, and URLPage
            string protocol = "";
            int pos = strInstr(1, url, "://");
            if (pos != 0) {
                protocol = url.left(pos + 2);
                url = url.Substring(pos + 2);
            }
            //
            // separate Host:Port from pathpage
            string host = url;
            pos = strInstr(host, "/");
            string path = "";
            string filename = "";
            if (pos == 0) {
                //
                // just host, no path or page
                path = "/";
            } else {
                path = host.Substring(pos - 1);
                host = host.left(pos - 1);
                //
                // separate page from path
                pos = path.LastIndexOf("/", StringComparison.InvariantCulture) + 1;
                if (pos == 0) {
                    //
                    // no path, just a page
                    filename = path;
                    path = "/";
                } else {
                    filename = path.Substring(pos);
                    path = path.left(pos);
                }
            }
            //
            // Divide Host from Port
            pos = strInstr(host, ":");
            string port = "";
            if (pos == 0) {
                //
                // host not given, take a guess
                switch (protocol.ToLowerInvariant()) {
                    case "ftp://": {
                            port = "21";
                            break;
                        }
                    case "http://":
                    case "https://": {
                            port = "80";
                            break;
                        }
                    default: {
                            port = "80";
                            break;
                        }
                }
            } else {
                port = host.Substring(pos);
                host = host.left(pos - 1);
            }
            pos = strInstr(1, filename, "?");
            string queryString = "";
            if (pos > 0) {
                queryString = filename.Substring(pos - 1);
                filename = filename.left(pos - 1);
            }
            List<string> pathSegments = path.Split('/').Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            return new UrlDetailsClass {
                protocol = protocol,
                host = host,
                port = port,
                pathSegments = pathSegments,
                filename = filename,
                queryString = queryString
            };
        }
        //
        //================================================================================================================
        /// <summary>
        /// deprecated, use native date conversions - convert a GMT date string to a datetime object. I honestly cannot find any kind of backup for this format
        /// </summary>
        /// <param name="GMTDate"></param>
        /// <returns></returns>
        public static DateTime deprecatedDecodeGMTDate(string GMTDate) {
            if (string.IsNullOrEmpty(GMTDate)) { return default; }
            double HourPart = encodeNumber(GMTDate.Substring(5, 11));
            if (!isDate(HourPart)) { return default; }
            double YearPart = encodeNumber(GMTDate.Substring(17, 8));
            if (!isDate(YearPart)) { return DateTime.FromOADate(YearPart + (HourPart + 4) / 24); }
            return default;
        }
        // 
        //=================================================================================
        /// <summary>
        /// Get the value of a name in a string of name value pairs parsed with vrlf and =
        ///   ex delimiter amp -> name1=value1 amp name2=value2"
        ///   There can be no extra spaces between the delimiter, the name and the "="
        /// </summary>
        /// <param name="key"></param>
        /// <param name="keyValueString"></param>
        /// <param name="defaultValue"></param>
        /// <param name="delimiter"></param>
        /// <returns></returns>
        public static string getValueFromKeyValueString(string key, string keyValueString, string defaultValue, string delimiter) {
            string result = defaultValue;
            try {
                //
                // determine delimiter
                string workingDelimiter = delimiter;
                if (string.IsNullOrEmpty(workingDelimiter)) {
                    //
                    // If not explicit
                    if (strInstr(1, keyValueString, Environment.NewLine) != 0) {
                        //
                        // crlf can only be here if it is the delimiter
                        workingDelimiter = Environment.NewLine;
                    } else {
                        //
                        // either only one option, or it is the legacy '&' delimit
                        workingDelimiter = "&";
                    }
                }
                string WorkingString = keyValueString;
                if (!string.IsNullOrEmpty(WorkingString)) {
                    WorkingString = workingDelimiter + WorkingString + workingDelimiter;
                    int ValueStart = strInstr(1, WorkingString, workingDelimiter + key + "=", 1);
                    if (ValueStart != 0) {
                        int NameLength = key.Length;
                        bool IsQuoted = false;
                        ValueStart = ValueStart + workingDelimiter.Length + NameLength + 1;
                        if (WorkingString.Substring(ValueStart - 1, 1) == "\"") {
                            IsQuoted = true;
                            ValueStart = ValueStart + 1;
                        }
                        int ValueEnd = 0;
                        if (IsQuoted) {
                            ValueEnd = strInstr(ValueStart, WorkingString, "\"" + workingDelimiter);
                        } else {
                            ValueEnd = strInstr(ValueStart, WorkingString, workingDelimiter);
                        }
                        if (ValueEnd == 0) {
                            result = WorkingString.Substring(ValueStart - 1);
                        } else {
                            result = WorkingString.Substring(ValueStart - 1, ValueEnd - ValueStart);
                        }
                    }
                }
            } catch (Exception) {
                throw;
            }
            return result;
        }
        //
        //=================================================================================
        /// <summary>
        /// Get a Random integer Value
        /// </summary>
        /// <param name="core"></param>
        /// <returns></returns>
        public static int getRandomInteger(CoreController core) {
            return core.random.Next(Int32.MaxValue);
        }
        //
        //=================================================================================
        /// <summary>
        /// return a string from an integer, padded left with 0. If the number is longer then the width, the full number is returned.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="digitCount"></param>
        /// <returns></returns>
        public static string getIntegerString(int value, int digitCount) {
            if (sizeof(int) <= digitCount) {
                return value.ToString(CultureInfo.InvariantCulture).PadLeft(digitCount, '0');
            } else {
                return value.ToString(CultureInfo.InvariantCulture);
            }
        }
        //
        //==========================================================================================
        /// <summary>
        /// Test if a test string is in a delimited string
        /// </summary>
        /// <param name="stringToSearch"></param>
        /// <param name="find"></param>
        /// <param name="Delimiter"></param>
        /// <returns></returns>
        public static bool isInDelimitedString(string stringToSearch, string find, string Delimiter) {

            return ((Delimiter + stringToSearch + Delimiter).IndexOf(Delimiter + find + Delimiter, StringComparison.InvariantCultureIgnoreCase) >= 0);
        }
        //========================================================================
        /// <summary>
        /// Encode a string to be used as a path or filename in a url. only what is to the left of the question mark.
        /// </summary>
        /// <param name="Source"></param>
        /// <returns></returns>
        public static string encodeURL(string Source) {
            return WebUtility.UrlEncode(Source);
        }
        //
        //========================================================================
        /// <summary>
        /// It is prefered to encode the request variables then assemble then into a query string. This routine parses them out and encodes them.
        /// </summary>
        /// <param name="Source"></param>
        /// <returns></returns>
        public static string encodeQueryString(string Source) {
            string result = "";
            try {
                if (!string.IsNullOrWhiteSpace(Source)) {
                    string[] QSSplit = Source.Split('&');
                    for (int QSPointer = 0; QSPointer <= QSSplit.GetUpperBound(0); QSPointer++) {
                        string NV = QSSplit[QSPointer];
                        if (!string.IsNullOrEmpty(NV)) {
                            string[] NVSplit = NV.Split('=');
                            if (NVSplit.GetUpperBound(0) == 0) {
                                NVSplit[0] = encodeRequestVariable(NVSplit[0]);
                                result += "&" + NVSplit[0];
                            } else {
                                NVSplit[0] = encodeRequestVariable(NVSplit[0]);
                                NVSplit[1] = encodeRequestVariable(NVSplit[1]);
                                result += "&" + NVSplit[0] + "=" + NVSplit[1];
                            }
                        }
                    }
                    if (!string.IsNullOrEmpty(result)) {
                        result = result.Substring(1);
                    }
                }
            } catch (Exception) {
                throw;
            }
            return result;
        }
        //
        //========================================================================
        /// <summary>
        /// encode the name or value part of a querystring, to be parsed with &amp; and = 
        /// </summary>
        /// <param name="Source"></param>
        /// <returns></returns>
        public static string encodeRequestVariable(string Source) {
            if (Source == null) {
                return "";
            }
            return System.Uri.EscapeDataString(Source);
        }
        //
        //========================================================================
        /// <summary>
        /// return the string that would result from encoding the string
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string decodeResponseVariable(string source) {
            string result = source;
            //
            int Position = strInstr(1, result, "%");
            while (Position != 0) {
                string ESCString = result.Substring(Position - 1, 3);
                string Digit0 = toUCase(ESCString.Substring(1, 1));
                string Digit1 = toUCase(ESCString.Substring(2, 1));
                if (((string.CompareOrdinal(Digit0, "0") >= 0) && (string.CompareOrdinal(Digit0, "9") <= 0)) || ((string.CompareOrdinal(Digit0, "A") >= 0) && (string.CompareOrdinal(Digit0, "F") <= 0))) {
                    if (((string.CompareOrdinal(Digit1, "0") >= 0) && (string.CompareOrdinal(Digit1, "9") <= 0)) || ((string.CompareOrdinal(Digit1, "A") >= 0) && (string.CompareOrdinal(Digit1, "F") <= 0))) {
                        int ESCValue = 0;
                        try {
                            ESCValue = Convert.ToInt32(ESCString.Substring(1), 16);
                        } catch {
                            // do nothing -- just put a 0 in as the escape code was not valid, a data problem not a code problem
                        }
                        result = result.left(Position - 1) + Convert.ToChar(ESCValue) + result.Substring(Position + 2);
                    }
                }
                Position = strInstr(Position + 1, result, "%");
            }
            //
            return result;
        }
        //   
        //========================================================================
        /// <summary>
        /// Converts a querystring from an Encoded URL (with %20 and +), to non incoded (with spaced)
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string decodeURL(string source) {
            return WebUtility.UrlDecode(source);
        }
        //
        //========================================================================
        /// <summary>
        /// return the first of two dates, excluding minValue
        /// </summary>
        /// <param name="Date0"></param>
        /// <param name="Date1"></param>
        /// <returns></returns>
        public static DateTime getFirstNonZeroDate(DateTime Date0, DateTime Date1) {
            if (Date0 == DateTime.MinValue) {
                if (Date1 == DateTime.MinValue) {
                    //
                    // Both 0, return 0
                    //
                    return DateTime.MinValue;
                } else {
                    //
                    // Date0 is NullDate, return Date1
                    //
                    return Date1;
                }
            } else {
                if (Date1 == DateTime.MinValue) {
                    //
                    // Date1 is nulldate, return Date0
                    //
                    return Date0;
                } else if (Date0 < Date1) {
                    //
                    // Date0 is first
                    //
                    return Date0;
                } else {
                    //
                    // Date1 is first
                    //
                    return Date1;
                }
            }
        }
        //
        //========================================================================
        //
        public static int getFirstNonZeroInteger(int Integer1, int Integer2) {
            if (Integer1 == 0) {
                if (Integer2 == 0) {
                    //
                    // Both 0, return 0
                    return 0;
                } else {
                    //
                    // Integer1 is 0, return Integer2
                    return Integer2;
                }
            } else {
                if (Integer2 == 0) {
                    //
                    // Integer2 is 0, return Integer1
                    return Integer1;
                } else if (Integer1 < Integer2) {
                    //
                    // Integer1 is first
                    return Integer1;
                } else {
                    //
                    // Integer2 is first
                    return Integer2;
                }
            }
        }
        //
        //========================================================================
        /// <summary>
        /// splitDelimited, returns the result of a Split, except it honors quoted text, if a quote is found, it is assumed to also be a delimiter ( 'this"that"theother' = 'this "that" theother' )
        /// </summary>
        /// <param name="WordList"></param>
        /// <param name="Delimiter"></param>
        /// <returns></returns>
        public static string[] splitDelimited(string WordList, string Delimiter) {
            string[] Out = new string[1];
            int OutPointer = 0;
            if (!string.IsNullOrEmpty(WordList)) {
                string[] QuoteSplit = stringSplit(WordList, "\"");
                int QuoteSplitCount = QuoteSplit.GetUpperBound(0) + 1;
                bool InQuote = (string.IsNullOrEmpty(WordList.left(1)));
                int OutSize = 1;
                for (int QuoteSplitPointer = 0; QuoteSplitPointer < QuoteSplitCount; QuoteSplitPointer++) {
                    string Fragment = QuoteSplit[QuoteSplitPointer];
                    if (string.IsNullOrEmpty(Fragment)) {
                        //
                        // empty fragment
                        // this is a quote at the end, or two quotes together
                        // do not skip to the next out pointer
                        //
                        if (OutPointer >= OutSize) {
                            OutSize = OutSize + 10;
                            Array.Resize(ref Out, OutSize + 1);
                        }
                    } else {
                        if (!InQuote) {
                            string[] SpaceSplit = Fragment.Split(Delimiter.ToCharArray());
                            int SpaceSplitCount = SpaceSplit.GetUpperBound(0) + 1;
                            for (int SpaceSplitPointer = 0; SpaceSplitPointer < SpaceSplitCount; SpaceSplitPointer++) {
                                if (OutPointer >= OutSize) {
                                    OutSize = OutSize + 10;
                                    Array.Resize(ref Out, OutSize + 1);
                                }
                                Out[OutPointer] = Out[OutPointer] + SpaceSplit[SpaceSplitPointer];
                                if (SpaceSplitPointer != (SpaceSplitCount - 1)) {
                                    //
                                    // divide output between splits
                                    //
                                    OutPointer = OutPointer + 1;
                                    if (OutPointer >= OutSize) {
                                        OutSize = OutSize + 10;
                                        Array.Resize(ref Out, OutSize + 1);
                                    }
                                }
                            }
                        } else {
                            Out[OutPointer] = Out[OutPointer] + "\"" + Fragment + "\"";
                        }
                    }
                    InQuote = !InQuote;
                }
            }
            Array.Resize(ref Out, OutPointer + 1);
            return Out;
        }
        //
        //========================================================================
        /// <summary>
        /// return Yes if true, No if false
        /// </summary>
        /// <param name="Key"></param>
        /// <returns></returns>
        public static string getYesNo(bool Key) {
            if (Key) {
                return "Yes";
            }
            return "No";
        }
        //
        // ====================================================================================================
        /// <summary>
        /// remove the host and approotpath, leaving the "active" path and all else
        /// </summary>
        /// <param name="url"></param>
        /// <param name="urlPrefix"></param>
        /// <returns></returns>
        public static string removeUrlPrefix(string url, string urlPrefix) {
            string result = url;
            if (!string.IsNullOrEmpty(url) && !string.IsNullOrEmpty(urlPrefix)) {
                if (strInstr(1, result, urlPrefix, 1) == 1) {
                    result = result.Substring(urlPrefix.Length);
                }
            }
            return result;
        }
        //
        // ====================================================================================================
        /// <summary>convert links as follows
        ///   Preserve URLs that do not start HTTP or HTTPS
        ///   Preserve URLs from other sites (offsite)
        ///   Preserve HTTP://ServerHost/ServerVirtualPath/Files/ in all cases
        ///   Convert HTTP://ServerHost/ServerVirtualPath/folder/page -> /folder/page
        ///   Convert HTTP://ServerHost/folder/page -> /folder/page
        /// </summary>
        /// <param name="url"></param>
        /// <param name="domain"></param>
        /// <param name="appVirtualPath">App virtualPath is typically /appName,  </param>
        /// <returns></returns>
        public static string convertLinkToShortLink(string url, string domain, string appVirtualPath) {
            //
            string BadString = "";
            string GoodString = "";
            string Protocol = "";
            string WorkingLink = url;
            //
            // ----- Determine Protocol
            if (strInstr(1, WorkingLink, "HTTP://", 1) == 1) {
                //
                // HTTP
                //
                Protocol = WorkingLink.left(7);
            } else if (strInstr(1, WorkingLink, "HTTPS://", 1) == 1) {
                //
                // HTTPS
                //
                // an ssl link can not be shortened
                return WorkingLink;
            }
            if (!string.IsNullOrEmpty(Protocol)) {
                //
                // ----- Protcol found, determine if is local
                //
                GoodString = Protocol + domain;
                if (WorkingLink.IndexOf(GoodString, System.StringComparison.InvariantCultureIgnoreCase) != -1) {
                    //
                    // URL starts with Protocol ServerHost
                    //
                    GoodString = Protocol + domain + appVirtualPath + "/files/";
                    if (WorkingLink.IndexOf(GoodString, System.StringComparison.InvariantCultureIgnoreCase) != -1) {
                        //
                        // URL is in the virtual files directory
                        //
                        BadString = GoodString;
                        GoodString = appVirtualPath + "/files/";
                        WorkingLink = strReplace(WorkingLink, BadString, GoodString, 1, 99, 1);
                    } else {
                        //
                        // URL is not in files virtual directory
                        //
                        BadString = Protocol + domain + appVirtualPath + "/";
                        GoodString = "/";
                        WorkingLink = strReplace(WorkingLink, BadString, GoodString, 1, 99, 1);
                        //
                        BadString = Protocol + domain + "/";
                        GoodString = "/";
                        WorkingLink = strReplace(WorkingLink, BadString, GoodString, 1, 99, 1);
                    }
                }
            }
            return WorkingLink;
        }
        //
        // ====================================================================================================
        /// <summary>
        /// convert a file link (like /ccLibraryFiles/imageFilename/000001/this.png) to a full URL
        /// </summary>
        /// <param name="url"></param>
        /// <param name="virtualPath"></param>
        /// <param name="appRootPath"></param>
        /// <param name="serverHost"></param>
        /// <returns></returns>
        public static string encodeVirtualPath(string url, string virtualPath, string appRootPath, string serverHost) {
            string result = url;
            bool VirtualHosted = false;
            if ((result.IndexOf(serverHost, System.StringComparison.InvariantCultureIgnoreCase) != -1) || (url.IndexOf("/") + 1 == 1)) {
                //
                // This link is onsite and has a path
                //
                VirtualHosted = (appRootPath.IndexOf(virtualPath, System.StringComparison.InvariantCultureIgnoreCase) != -1);
                if (VirtualHosted && (url.IndexOf(appRootPath, System.StringComparison.InvariantCultureIgnoreCase) + 1 == 1)) {
                    //
                    // quick - virtual hosted and link starts at AppRootPath
                    //
                } else if ((!VirtualHosted) && (url.left(1) == "/") && (url.IndexOf(appRootPath, System.StringComparison.InvariantCultureIgnoreCase) + 1 == 1)) {
                    //
                    // quick - not virtual hosted and link starts at Root
                    //
                } else {
                    UrlDetailsClass urlDetails = splitUrl(url);
                    string path = urlDetails.unixPath;
                    if (VirtualHosted) {
                        //
                        // Virtual hosted site, add VirualPath if it is not there
                        //
                        if (strInstr(1, urlDetails.unixPath, appRootPath, 1) == 0) {
                            if (path == "/") {
                                path = appRootPath;
                            } else {
                                path = appRootPath + path.Substring(1);
                            }
                        }
                    } else {
                        //
                        // Root hosted site, remove virtual path if it is there
                        //
                        if (strInstr(1, path, appRootPath, 1) != 0) {
                            path = strReplace(path, appRootPath, "/");
                        }
                    }
                    result = urlDetails.protocol + urlDetails.host + path + urlDetails.filename + urlDetails.queryString;
                }
            }
            return result;
        }
        //
        // ====================================================================================================
        /// <summary>
        /// encode initial caps
        /// </summary>
        /// <param name="Source"></param>
        /// <returns></returns>
        public static string encodeInitialCaps(string Source) {
            string result = "";
            if (!string.IsNullOrEmpty(Source)) {
                string[] SegSplit = Source.Split(' ');
                int SegMax = SegSplit.GetUpperBound(0);
                if (SegMax >= 0) {
                    for (int SegPtr = 0; SegPtr <= SegMax; SegPtr++) {
                        SegSplit[SegPtr] = toUCase(SegSplit[SegPtr].left(1)) + toLCase(SegSplit[SegPtr].Substring(1));
                    }
                }
                result = string.Join(" ", SegSplit);
            }
            return result;
        }
        //
        // ====================================================================================================
        /// <summary>
        /// attempt to make the word from plural to singular
        /// </summary>
        /// <param name="PluralSource"></param>
        /// <returns></returns>
        public static string getSingular_Sortof(string PluralSource) {
            string result = PluralSource;
            bool UpperCase = false;
            if (result.Length > 1) {
                string LastCharacter = result.Substring(result.Length - 1);
                UpperCase = (LastCharacter == LastCharacter.ToUpper());
                if (toUCase(result.Substring(result.Length - 3)) == "IES") {
                    result = result.left(result.Length - 3) + (UpperCase ? "Y" : "y");
                } else if (toUCase(result.Substring(result.Length - 2)) == "SS") {
                    // nothing
                } else if (toUCase(result.Substring(result.Length - 1)) == "S") {
                    result = result.left(result.Length - 1);
                }
            }
            return result;
        }
        //
        // ====================================================================================================
        /// <summary>
        /// encode a string to be used as a javascript single quoted string. 
        /// For example, if creating a string "alert('" + ex.Message + "');"; 
        /// </summary>
        /// <param name="Source"></param>
        /// <returns></returns>
        public static string encodeJavascriptStringSingleQuote(string Source) {
            return HttpUtility.JavaScriptStringEncode(Source);
        }
        //
        // ====================================================================================================
        /// <summary>
        /// returns a 1-based index into the comma seperated ListOfItems where Item is found
        /// </summary>
        /// <param name="Item"></param>
        /// <param name="ListOfItems"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public static int getListIndex(string Item, string ListOfItems) {
            int tempGetListIndex = 0;
            //
            string[] Items = null;
            string LcaseItem = null;
            string LcaseList = null;
            int Ptr = 0;
            //
            tempGetListIndex = 0;
            if (!string.IsNullOrEmpty(ListOfItems)) {
                LcaseItem = toLCase(Item);
                LcaseList = toLCase(ListOfItems);
                Items = splitDelimited(LcaseList, ",");
                for (Ptr = 0; Ptr <= Items.GetUpperBound(0); Ptr++) {
                    if (Items[Ptr] == LcaseItem) {
                        tempGetListIndex = Ptr + 1;
                        break;
                    }
                }
            }
            //
            return tempGetListIndex;
        }
        //
        // ====================================================================================================
        //
        public static int encodeInteger(object expression) {
            if (expression == null) { return 0; }
            string trialString = expression.ToString();
            if (int.TryParse(trialString, out int trialInt)) { return trialInt; }
            if (double.TryParse(trialString, out double trialDbl)) { return (int)trialDbl; }
            if (bool.TryParse(trialString, out bool trialBool)) { return (trialBool) ? 1 : 0; }
            return 0;
        }
        //
        // ====================================================================================================
        /// <summary>
        /// Converts the expression to a nullable type
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public static int? encodeIntegerNullable(object expression) {
            if (expression == null) { return null; }
            if ((expression is string) && (string.IsNullOrWhiteSpace((string)expression))) { return null; }
            return encodeInteger(expression);
        }
        //
        // ====================================================================================================
        /// <summary>
        /// convert an expression from boolean, double, text or date to a double number.
        /// encodeNumber, encodeBoolean, encodeInteger, encodeDate work together with encodeText to be reversable. For example, the outcome matches the input of encodeBolean( encodeText( booleanValue ))
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public static double encodeNumber(object expression) {
            if (expression == null) { return 0; }
            string trialString = expression.ToString();
            if (double.TryParse(trialString, out double trialDbl)) { return trialDbl; }
            if (bool.TryParse(trialString, out bool trialBool)) { return (trialBool) ? 1 : 0; }
            return 0;
        }
        //
        // ====================================================================================================
        /// <summary>
        /// Converts the expression to a nullable type
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public static double? encodeNumberNullable(object expression) {
            if (expression == null) { return null; }
            if ((expression is string) && (string.IsNullOrWhiteSpace((string)expression))) { return null; }
            return encodeNumber(expression);
        }
        //
        //====================================================================================================
        //
        public static string encodeText(object Expression) {
            if ((Expression is DBNull) || (Expression == null)) { return string.Empty; }
            return Expression.ToString();
        }
        //
        //====================================================================================================
        //
        public static bool encodeBoolean(object Expression) {
            if (Expression == null) { return false; }
            if (Expression is bool) { return (bool)Expression; }
            if (Expression.isNumeric()) { return (encodeText(Expression) != "0"); }
            if (Expression is string) { return (new string[] { "on", "yes", "true" }).Any(((string)Expression).ToLowerInvariant().Equals); }
            return false;
        }
        //
        //====================================================================================================
        //
        public static DateTime encodeDate(object Expression) {
            if (Expression is string) {
                // visual basic - when converting a date to a string, it converts minDate to "12:00:00 AM". 
                // however, Convert.ToDateTime() converts "12:00:00 AM" to the current date.
                // this is a terrible hack, but to be compatible with current software, "#12:00:00 AM#" must return mindate 
                if ((String)Expression == "12:00:00 AM") { return DateTime.MinValue; }
            }
            if (isDate(Expression)) { return Convert.ToDateTime(Expression); }
            return DateTime.MinValue;
        }
        //
        //========================================================================
        //   Gets the next line from a string, and removes the line
        //
        public static string getLine(ref string Body) {
            string returnFirstLine = Body;
            try {
                int nextCR = strInstr(1, Body, "\r");
                int nextLF = strInstr(1, Body, "\n");
                if ((nextCR != 0) || (nextLF != 0)) {
                    int eol = 0;
                    int bol = 0;
                    if (nextCR != 0) {
                        if (nextLF != 0) {
                            if (nextCR < nextLF) {
                                eol = nextCR - 1;
                                if (nextLF == nextCR + 1) {
                                    bol = nextLF + 1;
                                } else {
                                    bol = nextCR + 1;
                                }

                            } else {
                                eol = nextLF - 1;
                                bol = nextLF + 1;
                            }
                        } else {
                            eol = nextCR - 1;
                            bol = nextCR + 1;
                        }
                    } else {
                        eol = nextLF - 1;
                        bol = nextLF + 1;
                    }
                    returnFirstLine = Body.left(eol);
                    Body = Body.Substring(bol - 1);
                } else {
                    returnFirstLine = Body;
                    Body = "";
                }
            } catch (Exception) { }
            return returnFirstLine;
        }
        //
        // ====================================================================================================
        //
        public static string runProcess(CoreController core, string Cmd, string Arguments, bool WaitForReturn) {
            //
            LogController.logInfo(core, "runProcess, cmd=[" + Cmd + "], Arguments=[" + Arguments + "], WaitForReturn=[" + WaitForReturn + "]");
            //
            using (Process p = new Process()) {
                p.StartInfo.FileName = Cmd;
                p.StartInfo.Arguments = Arguments;
                p.StartInfo.UseShellExecute = true;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.ErrorDialog = false;
                p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                p.Start();
                if (WaitForReturn) {
                    p.WaitForExit(1000 * 60 * 5);
                }
            }
            return "";
        }
        //
        public static string runProcess(CoreController core, string Cmd, string Arguments) => runProcess(core, Cmd, Arguments, false);
        //
        public static string runProcess(CoreController core, string Cmd) => runProcess(core, Cmd, "", false);
        //
        // ====================================================================================================
        //
        public static string encodeNvaArgument(string arg) {
            if (string.IsNullOrEmpty(arg)) { return string.Empty; }
            arg = strReplace(arg, Environment.NewLine, "#0013#");
            arg = strReplace(arg, "\n", "#0013#");
            arg = strReplace(arg, "\r", "#0013#");
            arg = strReplace(arg, "&", "#0038#");
            arg = strReplace(arg, "=", "#0061#");
            arg = strReplace(arg, ",", "#0044#");
            arg = strReplace(arg, "\"", "#0034#");
            arg = strReplace(arg, "'", "#0039#");
            arg = strReplace(arg, "|", "#0124#");
            arg = strReplace(arg, "[", "#0091#");
            arg = strReplace(arg, "]", "#0093#");
            arg = strReplace(arg, ":", "#0058#");
            return arg;
        }
        //
        // ====================================================================================================
        //   use only internally
        //       decode an argument removed from a name=value& string
        //       see encodeNvaArgument for details on how to use this
        //
        public static string decodeNvaArgument(string arg) {
            if (string.IsNullOrEmpty(arg)) { return string.Empty; }
            arg = strReplace(arg, "#0058#", ":");
            arg = strReplace(arg, "#0093#", "]");
            arg = strReplace(arg, "#0091#", "[");
            arg = strReplace(arg, "#0124#", "|");
            arg = strReplace(arg, "#0039#", "'");
            arg = strReplace(arg, "#0034#", "\"");
            arg = strReplace(arg, "#0044#", ",");
            arg = strReplace(arg, "#0061#", "=");
            arg = strReplace(arg, "#0038#", "&");
            arg = strReplace(arg, "#0013#", Environment.NewLine);
            return arg;
        }
        //
        //=================================================================================
        //   Renamed to catch all the cases that used it in addons
        //
        //   Do not use this routine in Addons to get the addon option string value
        //   to get the value in an option string, use cmc.csv_getAddonOption("name")
        //
        // Get the value of a name in a string of name value pairs parsed with vrlf and =
        //   the legacy line delimiter was a '&' -> name1=value1&name2=value2"
        //   new format is "name1=value1 crlf name2=value2 crlf ..."
        //   There can be no extra spaces between the delimiter, the name and the "="
        //=================================================================================
        //
        public static string getSimpleNameValue(string Name, string ArgumentString, string DefaultValue, string Delimiter) {
            if (string.IsNullOrEmpty(Delimiter)) {
                //
                // determine delimiter
                if (strInstr(1, ArgumentString, Environment.NewLine) != 0) {
                    //
                    // crlf can only be here if it is the delimiter
                    Delimiter = Environment.NewLine;
                } else {
                    //
                    // either only one option, or it is the legacy '&' delimit
                    Delimiter = "&";
                }
            }
            string WorkingString = ArgumentString;
            string result = DefaultValue;
            if (!string.IsNullOrEmpty(WorkingString)) {
                WorkingString = Delimiter + WorkingString + Delimiter;
                int ValueStart = strInstr(1, WorkingString, Delimiter + Name + "=", 1);
                if (ValueStart != 0) {
                    int NameLength = Name.Length;
                    ValueStart = ValueStart + Delimiter.Length + NameLength + 1;
                    bool IsQuoted = false;
                    if (WorkingString.Substring(ValueStart - 1, 1) == "\"") {
                        IsQuoted = true;
                        ValueStart = ValueStart + 1;
                    }
                    int ValueEnd = 0;
                    if (IsQuoted) {
                        ValueEnd = strInstr(ValueStart, WorkingString, "\"" + Delimiter);
                    } else {
                        ValueEnd = strInstr(ValueStart, WorkingString, Delimiter);
                    }
                    if (ValueEnd == 0) {
                        result = WorkingString.Substring(ValueStart - 1);
                    } else {
                        result = WorkingString.Substring(ValueStart - 1, ValueEnd - ValueStart);
                    }
                }
            }
            return result;
        }
        //
        // ====================================================================================================
        //
        public static string getIpAddressList() {
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
            string ipAddressList = "";
            foreach (IPAddress ip in host.AddressList) {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) {
                    ipAddressList += "," + ip.ToString();
                }
            }
            if (!string.IsNullOrEmpty(ipAddressList)) {
                ipAddressList = ipAddressList.Substring(1);
            }
            return ipAddressList;
        }
        //
        // ====================================================================================================
        //
        public static bool isNull(object source) {
            return (source == null);
        }
        //
        // ====================================================================================================
        //
        public static bool isMissing(object source) {
            return false;
        }

        //
        // ====================================================================================================
        /// <summary>
        /// Encode a date to minvalue, if date is less-than minVAlue,m set it to minvalue, if date less-than 1/1/1000 (the beginning of time), it returns date.minvalue
        /// </summary>
        /// <param name="sourceDate"></param>
        /// <returns></returns>
        public static DateTime encodeDateMinValue(DateTime sourceDate) {
            if (sourceDate <= new DateTime(1000, 1, 1)) {
                return DateTime.MinValue;
            }
            return sourceDate;
        }
        //
        //====================================================================================================
        /// <summary>
        /// Return true if a date is the mindate, else return false 
        /// </summary>
        /// <param name="sourceDate"></param>
        /// <returns></returns>
        public static bool isMinDate(DateTime sourceDate) {
            return encodeDateMinValue(sourceDate) == DateTime.MinValue;
        }
        //
        // ====================================================================================================
        // the the name of the current executable
        //
        public static string getAppExeName() {
            return Path.GetFileName(Assembly.GetEntryAssembly()?.Location);
        }
        //
        //====================================================================================================
        /// <summary>
        /// convert a dtaTable to list of string 
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static List<string> convertDataTableColumntoItemList(DataTable dt) {
            List<string> returnString = new List<string>();
            foreach (DataRow dr in dt.Rows) {
                returnString.Add(dr[0].ToString().ToLowerInvariant());
            }
            return returnString;
        }
        //
        //====================================================================================================
        /// <summary>
        /// convert a dtaTable to a comma delimited list of column 0
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static string convertDataTableColumntoItemCommaList(DataTable dt) {
            string returnString = "";
            foreach (DataRow dr in dt.Rows) {
                returnString += "," + dr[0].ToString();
            }
            if (returnString.Length > 0) {
                returnString = returnString.Substring(1);
            }
            return returnString;
        }
        //
        //====================================================================================================
        /// <summary>
        /// returns true or false if a string is located within another string. Similar to indexof but returns true/false 
        /// </summary>
        /// <param name="start"></param>
        /// <param name="haystack"></param>
        /// <param name="needle"></param>
        /// <returns></returns>
        public static bool isInStr(int start, string haystack, string needle) {
            return (haystack.IndexOf(needle, start - 1, System.StringComparison.InvariantCultureIgnoreCase) + 1 >= 0);
        }
        //
        //====================================================================================================
        /// <summary>
        /// Convert a route to the anticipated format (lowercase,no leading /, no trailing /)
        /// </summary>
        /// <param name="route"></param>
        /// <returns></returns>
        public static string normalizeRoute(string route) {
            try {
                if (string.IsNullOrWhiteSpace(route)) {
                    return string.Empty;
                }
                string normalizedRoute = route.ToLowerInvariant().Trim();
                if (string.IsNullOrEmpty(normalizedRoute)) {
                    return string.Empty;
                }
                normalizedRoute = FileController.convertToUnixSlash(normalizedRoute);
                while (normalizedRoute.IndexOf("//", StringComparison.InvariantCultureIgnoreCase) >= 0) {
                    normalizedRoute = normalizedRoute.Replace("//", "/");
                }
                if (route.Equals("/")) {
                    return string.Empty;
                }
                if (normalizedRoute.left(1).Equals("/")) {
                    normalizedRoute = normalizedRoute.Substring(1);
                }
                if (normalizedRoute.Substring(normalizedRoute.Length - 1, 1).Equals("/")) {
                    normalizedRoute = normalizedRoute.left(normalizedRoute.Length - 1);
                }
                return normalizedRoute;
            } catch (Exception ex) {
                throw new GenericException("Unexpected exception in normalizeRoute(route=[" + route + "])", ex);
            }
        }
        //
        //========================================================================
        //   converts a virtual file into a filename
        //       - in local mode, the cdnFiles can be mapped to a virtual folder in appRoot
        //           -- see appConfig.cdnFilesVirtualFolder
        //       convert all / to \
        //       if it includes "://", it is a root file
        //       if it starts with "/", it is already root relative
        //       else (if it start with a file or a path), add the publicFileContentPathPrefix
        //
        public static string convertCdnUrlToCdnPathFilename(string cdnUrl) {
            //
            // this routine was originally written to handle modes that were not adopted (content file absolute and relative URLs)
            // leave it here as a simple slash converter in case other conversions are needed later
            //
            return strReplace(cdnUrl, "/", "\\");
        }
        //
        //==============================================================================
        public static bool isGuid(string Source) {
            try {
                if ((Source.Length == 38) && (Source.left(1) == "{") && (Source.Substring(Source.Length - 1) == "}")) { return true; }                
                if ((Source.Length == 36) && (Source.IndexOf(" ", StringComparison.InvariantCultureIgnoreCase) == -1)) { return true; }                
                if (Source.Length == 32) { return true; }
                //
                // not valid
                //
                return false;
            } catch (Exception ex) {
                throw new GenericException("Exception in isGuid", ex);
            }
        }
        // todo refactor out vb fpo
        //====================================================================================================
        /// <summary>
        /// temp methods to convert from vb, refactor out
        /// </summary>
        /// <param name="startBase1"></param>
        /// <param name="string1"></param>
        /// <param name="string2"></param>
        /// <param name="text1Binary2"></param>
        /// <returns></returns>
        public static int strInstr(int startBase1, string string1, string string2, int text1Binary2) {
            if (string.IsNullOrEmpty(string1)) {
                return 0;
            } else {
                if (startBase1 < 1) {
                    throw new ArgumentException("Instr() start must be > 0.");
                } else {
                    if (text1Binary2 == 1) {
                        return string1.IndexOf(string2, startBase1 - 1, StringComparison.InvariantCultureIgnoreCase) + 1;
                    } else {
                        return string1.IndexOf(string2, startBase1 - 1, StringComparison.InvariantCulture) + 1;
                    }
                }
            }
        }
        //
        // ====================================================================================================
        /// <summary>
        /// vb replacement
        /// </summary>
        /// <param name="string1"></param>
        /// <param name="string2"></param>
        /// <returns></returns>
        public static int strInstr(string string1, string string2) {
            return strInstr(1, string1, string2, 2);
        }
        //
        // ====================================================================================================
        /// <summary>
        /// vb replacement
        /// </summary>
        /// <param name="startBase1"></param>
        /// <param name="string1"></param>
        /// <param name="string2"></param>
        /// <returns></returns>
        public static int strInstr(int startBase1, string string1, string string2) {
            return strInstr(startBase1, string1, string2, 2);
        }
        //
        //====================================================================================================
        /// <summary>
        /// vb replacement
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="oldValue"></param>
        /// <param name="replacement"></param>
        /// <param name="startIgnore"></param>
        /// <param name="countIgnore"></param>
        /// <param name="compare"></param>
        /// <returns></returns>
        public static string strReplace(string expression, string oldValue, string replacement, int startIgnore, int countIgnore, int compare) {
            if (string.IsNullOrEmpty(expression)) {
                return expression;
            } else if (string.IsNullOrEmpty(oldValue)) {
                return expression;
            } else {
                if (compare == 2) {
                    return expression.Replace(oldValue, replacement);
                } else {

                    StringBuilder sb = new StringBuilder();
                    int previousIndex = 0;
                    int Index = expression.IndexOf(oldValue, StringComparison.InvariantCultureIgnoreCase);
                    while (Index != -1) {
                        sb.Append(expression.Substring(previousIndex, Index - previousIndex));
                        sb.Append(replacement);
                        Index += oldValue.Length;
                        previousIndex = Index;
                        Index = expression.IndexOf(oldValue, Index, StringComparison.InvariantCultureIgnoreCase);
                    }
                    sb.Append(expression.Substring(previousIndex));
                    return sb.ToString();
                }
            }
        }
        //
        // ====================================================================================================
        //
        public static string strReplace(string expression, string find, int replacement) { return strReplace(expression, find, replacement.ToString(CultureInfo.InvariantCulture)); }
        //
        // ====================================================================================================
        //
        public static string strReplace(string expression, string find, string replacement) { return strReplace(expression, find, replacement, 1, 9999, 1); }
        //
        //====================================================================================================
        /// <summary>
        /// Visual Basic UCase
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string toUCase(string source) {
            if (!string.IsNullOrEmpty(source)) { return source.ToUpperInvariant(); }
            return string.Empty;
        }
        //
        //====================================================================================================
        /// <summary>
        /// Visual Basic LCase
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string toLCase(string source) {
            if (!string.IsNullOrEmpty(source)) { return source.ToLowerInvariant(); }
            return string.Empty;
        }
        //
        // ====================================================================================================
        /// <summary>
        /// Visual Basic Len()
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static int strLen(string source) {
            if (string.IsNullOrEmpty(source)) {
                return 0;
            } else {
                return source.Length;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Visual Basic Mid()
        /// </summary>
        /// <param name="source"></param>
        /// <param name="startIndex"></param>
        /// <returns></returns>
        public static string strMid(string source, int startIndex) {
            if (string.IsNullOrEmpty(source)) {
                return "";
            } else {
                return source.Substring(startIndex);
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Visual Basic Mid()
        /// </summary>
        /// <param name="source"></param>
        /// <param name="startIndex"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static string strMid(string source, int startIndex, int length) {
            if (string.IsNullOrEmpty(source)) {
                return "";
            } else {
                return source.Substring(startIndex, length);
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Encodes an argument in an Addon OptionString (QueryString) for all non-allowed characters
        /// Arg0,Arg1,Arg2,Arg3,Name=Value Name=VAlue[Option1|Option2]
        /// call this before parsing them together
        /// call decodeAddonOptionArgument after parsing them apart
        /// </summary>
        /// <param name="Arg"></param>
        /// <returns></returns>
        //
        public static string encodeLegacyOptionStringArgument(string Arg) {
            string a = "";
            if (!string.IsNullOrEmpty(Arg)) {
                a = Arg;
                a = strReplace(a, Environment.NewLine, "#0013#");
                a = strReplace(a, "\n", "#0013#");
                a = strReplace(a, "\r", "#0013#");
                a = strReplace(a, "&", "#0038#");
                a = strReplace(a, "=", "#0061#");
                a = strReplace(a, ",", "#0044#");
                a = strReplace(a, "\"", "#0034#");
                a = strReplace(a, "'", "#0039#");
                a = strReplace(a, "|", "#0124#");
                a = strReplace(a, "[", "#0091#");
                a = strReplace(a, "]", "#0093#");
                a = strReplace(a, ":", "#0058#");
            }
            return a;
        }
        //
        //========================================================================
        // main_encodeCookieName, replace invalid cookie characters with %hex
        //
        public static string encodeCookieName(string Source) {
            return encodeURL(Source).ToLowerInvariant();
        }
        //
        //=============================================================================
        // ----- Return the value associated with the name given
        //   NameValueString is a string of Name=Value pairs, separated by spaces or "&"
        //   If Name is not given, returns ""
        //   If Name present but no value, returns true (as if Name=true)
        //   If Name = Value, it returns value
        //
        public static string main_GetNameValue_Internal(CoreController core, string NameValueString, string Name) {
            string result = "";
            //
            string NameValueStringWorking = NameValueString;
            string UcaseNameValueStringWorking = NameValueString.ToUpper();
            string[] pairs = null;
            int PairCount = 0;
            int PairPointer = 0;
            string[] PairSplit = null;
            //
            if ((!string.IsNullOrEmpty(NameValueString)) && (!string.IsNullOrEmpty(Name))) {
                while (strInstr(1, NameValueStringWorking, " =") != 0) {
                    NameValueStringWorking = strReplace(NameValueStringWorking, " =", "=");
                }
                while (strInstr(1, NameValueStringWorking, "= ") != 0) {
                    NameValueStringWorking = strReplace(NameValueStringWorking, "= ", "=");
                }
                while (strInstr(1, NameValueStringWorking, "& ") != 0) {
                    NameValueStringWorking = strReplace(NameValueStringWorking, "& ", "&");
                }
                while (strInstr(1, NameValueStringWorking, " &") != 0) {
                    NameValueStringWorking = strReplace(NameValueStringWorking, " &", "&");
                }
                NameValueStringWorking = NameValueString + "&";
                UcaseNameValueStringWorking = toUCase(NameValueStringWorking);
                //
                result = "";
                if (!string.IsNullOrEmpty(NameValueStringWorking)) {
                    pairs = NameValueStringWorking.Split('&');
                    PairCount = pairs.GetUpperBound(0) + 1;
                    for (PairPointer = 0; PairPointer < PairCount; PairPointer++) {
                        PairSplit = pairs[PairPointer].Split('=');
                        if (toUCase(PairSplit[0]) == toUCase(Name)) {
                            if (PairSplit.GetUpperBound(0) > 0) {
                                result = PairSplit[1];
                            }
                            break;
                        }
                    }
                }
            }
            return result;
        }
        //
        //========================================================================
        //   convert a virtual file into a Link usable on the website:
        //       convert all \ to /
        //       if it includes "://", leave it along
        //       if it starts with "/", it is already root relative, leave it alone
        //       else (if it start with a file or a path), add the serverFilePath
        //
        public static string getCdnFileLink(CoreController core, string virtualFile) {
            string returnLink = virtualFile;
            returnLink = strReplace(returnLink, "\\", "/");
            if (strInstr(1, returnLink, "://") != 0) {
                //
                // icon is an Absolute URL - leave it
                //
                return returnLink;
            } else if (returnLink.left(1) == "/") {
                //
                // icon is Root Relative, leave it
                //
                return returnLink;
            } else {
                //
                // icon is a virtual file, add the serverfilepath
                //
                return core.appConfig.cdnFileUrl + returnLink;
            }
        }
        //
        // ====================================================================================================
        //
        public static string getLinkedText(string AnchorTag, string AnchorText) {
            string iAnchorTag = encodeText(AnchorTag);
            string iAnchorText = encodeText(AnchorText);
            string UcaseAnchorText = toUCase(iAnchorText);
            string result = "";
            if ((!string.IsNullOrEmpty(iAnchorTag)) && (!string.IsNullOrEmpty(iAnchorText))) {
                int LinkPosition = UcaseAnchorText.LastIndexOf("<LINK>", StringComparison.InvariantCulture) + 1;
                if (LinkPosition == 0) {
                    result = iAnchorTag + iAnchorText + "</a>";
                } else {
                    result = iAnchorText;
                    LinkPosition = UcaseAnchorText.LastIndexOf("</LINK>", StringComparison.InvariantCulture) + 1;
                    while (LinkPosition > 1) {
                        result = result.left(LinkPosition - 1) + "</a>" + result.Substring(LinkPosition + 6);
                        LinkPosition = UcaseAnchorText.LastIndexOf("<LINK>", LinkPosition - 2, StringComparison.InvariantCulture) + 1;
                        if (LinkPosition != 0) {
                            result = result.left(LinkPosition - 1) + iAnchorTag + result.Substring(LinkPosition + 5);
                        }
                        LinkPosition = UcaseAnchorText.LastIndexOf("</LINK>", LinkPosition - 1, StringComparison.InvariantCulture) + 1;
                    }
                }
            }
            return result;
        }
        //
        // ====================================================================================================
        //
        public static string convertNameValueDictToREquestString(Dictionary<string, string> nameValueDict) {
            string requestFormSerialized = "";
            if (nameValueDict.Count > 0) {
                foreach (KeyValuePair<string, string> kvp in nameValueDict) {
                    requestFormSerialized += "&" + encodeURL(kvp.Key.left(255)) + "=" + encodeURL(kvp.Value.left(255));
                    if (requestFormSerialized.Length > 255) {
                        break;
                    }
                }
            }
            return requestFormSerialized;
        }
        //
        //====================================================================================================
        /// <summary>
        /// if valid date, return the short date, else return blank string 
        /// </summary>
        /// <param name="srcDate"></param>
        /// <returns></returns>
        public static string getShortDateString(DateTime srcDate) {
            string returnString = "";
            DateTime workingDate = srcDate.minValueIfOld();
            if (!srcDate.isOld()) {
                returnString = workingDate.ToShortDateString();
            }
            return returnString;
        }
        //
        //======================================================================================
        /// <summary>
        /// Convert QS tag argument list (a=1 b=2 with NVA encoding) to a doc property compatible dictionary of strings
        /// </summary>
        /// <param name="core"></param>
        /// <param name="ACArgumentString"></param>
        /// <returns></returns>
        public static Dictionary<string, string> convertQSNVAArgumentstoDocPropertiesList(CoreController core, string ACArgumentString) {
            Dictionary<string, string> returnList = new Dictionary<string, string>();
            try {
                if (!string.IsNullOrEmpty(ACArgumentString)) {

                    List<string> optionList = ACArgumentString.Split('&').ToList<string>();
                    foreach (string option in optionList) {
                        if (!string.IsNullOrWhiteSpace(option)) {
                            string key = option;
                            int firstPos = key.IndexOf('=');
                            string value = string.Empty;
                            if (firstPos >= 0) {
                                key = key.left(firstPos).Trim(' ');
                                value = decodeNvaArgument(option.Substring(firstPos + 1).Trim(' '));
                            }
                            key = decodeNvaArgument(key);
                            if (returnList.ContainsKey(key)) { returnList.Remove(key); }
                            returnList.Add(key, value);
                        }
                    }
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
            return returnList;
        }
        //
        //======================================================================================
        /// <summary>
        /// Convert addon argument list to a doc property compatible dictionary of strings
        /// </summary>
        /// <param name="core"></param>
        /// <param name="SrcOptionList"></param>
        /// <returns></returns>
        public static Dictionary<string, string> convertAddonArgumentstoDocPropertiesList(CoreController core, string SrcOptionList) {
            Dictionary<string, string> returnList = new Dictionary<string, string>();
            try {
                string[] SrcOptions = null;
                string key = null;
                string value = null;
                int Pos = 0;
                //
                if (!string.IsNullOrEmpty(SrcOptionList)) {
                    SrcOptions = stringSplit(SrcOptionList.Replace(Environment.NewLine, "\r").Replace("\n", "\r"), "\r");
                    for (var Ptr = 0; Ptr <= SrcOptions.GetUpperBound(0); Ptr++) {
                        key = SrcOptions[Ptr].Replace("\t", "");
                        if (!string.IsNullOrEmpty(key)) {
                            value = "";
                            Pos = strInstr(1, key, "=");
                            if (Pos > 0) {
                                value = key.Substring(Pos);
                                key = key.left(Pos - 1);
                            }
                            returnList.Add(key, value);
                        }
                    }
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
            return returnList;
        }
        //
        // ====================================================================================================
        /// <summary>
        /// descramble a phrase using decryptTwoWay. If decryption fails, attempt legacy scramble. If no decryption works, return original scrambled source
        /// </summary>
        /// <param name="core"></param>
        /// <param name="Copy"></param>
        /// <returns></returns>
        public static string textDeScramble(CoreController core, string Copy) {
            string result = "";
            try {
                result = SecurityController.decryptTwoWay(core, Copy);
            } catch (Exception) {
                //
                if (!core.siteProperties.allowLegacyDescrambleFallback) {
                    //
                    throw;
                } else {
                    //
                    // -- decryption failed, true legacy descramble
                    try {
                        int CPtr = 0;
                        string C = null;
                        int CValue = 0;
                        int crc = 0;
                        string Source = null;
                        int Base = 0;
                        const int CMin = 32;
                        const int CMax = 126;
                        //
                        // assume this one is not converted
                        //
                        Source = Copy;
                        Base = 50;
                        //
                        // First characger must be _
                        // Second character is the scramble version 'a' is the starting system
                        //
                        if (Source.left(2) != "_a") {
                            result = Copy;
                        } else {
                            Source = Source.Substring(2);
                            //
                            // cycle through all characters
                            //
                            for (CPtr = Source.Length - 1; CPtr >= 1; CPtr--) {
                                C = Source.Substring(CPtr - 1, 1);
                                CValue = Microsoft.VisualBasic.Strings.Asc(C);
                                crc = crc + CValue;
                                if ((CValue < CMin) || (CValue > CMax)) {
                                    //
                                    // if out of ascii bounds, just leave it in place
                                    //
                                } else {
                                    CValue = CValue - Base;
                                    if (CValue < CMin) {
                                        CValue = CValue + CMax - CMin + 1;
                                    }
                                }
                                result += Microsoft.VisualBasic.Strings.Chr(CValue);
                            }
                            //
                            // Test mod
                            //
                            if ((crc % 9).ToString(CultureInfo.InvariantCulture) != Source.Substring(Source.Length - 1, 1)) {
                                //
                                // do nothinge - set it back to the input
                                //
                                result = Copy;
                            }
                        }
                    } catch (Exception ex) {
                        LogController.logError(core, ex);
                        throw;
                    }
                }
            }
            return result;
        }
        //
        //=============================================================================
        // 
        public static string textScramble(CoreController core, string Copy) {
            return SecurityController.encryptTwoWay(core, Copy);
        }
        //
        //====================================================================================================
        /// <summary>
        /// remove html script start and end tags from a string - presumably a javascript string
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string removeScriptTag(string source) {
            string result = source;
            int StartPos = strInstr(1, result, "<script", 1);
            if (StartPos != 0) {
                int EndPos = strInstr(StartPos, result, "</script", 1);
                if (EndPos != 0) {
                    EndPos = strInstr(EndPos, result, ">", 1);
                    if (EndPos != 0) {
                        result = result.left(StartPos - 1) + result.Substring(EndPos);
                    }
                }
            }
            return result;
        }
        //
        // ====================================================================================================
        /// <summary>
        /// split a string
        /// </summary>
        /// <param name="src"></param>
        /// <param name="delimiter"></param>
        /// <returns></returns>
        public static string[] stringSplit(string src, string delimiter) {
            return src.Split(new[] { delimiter }, StringSplitOptions.None);
        }
        //
        // ====================================================================================================
        /// <summary>
        /// Execute an external program synchonously
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public static string executeCommandSync(string command) {
            string result = "";
            try {
                Process proc = new System.Diagnostics.Process {
                    StartInfo = new ProcessStartInfo("%comspec%", "/c " + command) {
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                proc.Start();
                result = proc.StandardOutput.ReadToEnd();
            } catch (Exception) { }
            return result;
        }
        //
        //====================================================================================================
        /// <summary>
        /// return true if strings match and neither is null
        /// </summary>
        /// <param name="source1"></param>
        /// <param name="source2"></param>
        /// <returns></returns>
        public static bool textMatch(string source1, string source2) {
            if ((source1 == null) || (source2 == null)) {
                return false;
            } else {
                return (source1.ToLowerInvariant() == source2.ToLowerInvariant());
            }
        }
        public static bool isDate(object expression) {
            if (expression == null) { return false; }
            return DateTime.TryParse(expression.ToString(), out DateTime testDate);
        }
        //
        //====================================================================================================
        /// <summary>
        /// Return string yyyymmdd string
        /// </summary>
        /// <param name="rightNow"></param>
        /// <returns></returns>
        public static string getDateNumberString(DateTime rightNow) {
            return rightNow.Year + rightNow.Month.ToString(CultureInfo.InvariantCulture).PadLeft(2, '0') + rightNow.Day.ToString(CultureInfo.InvariantCulture).PadLeft(2, '0');

        }
        //
        //====================================================================================================
        /// <summary>
        /// Return string yyyymmdd string
        /// </summary>
        /// <param name="rightNow"></param>
        /// <returns></returns>
        public static string getDateTimeNumberString(DateTime rightNow) {
            return getDateNumberString(rightNow) + rightNow.Hour.ToString(CultureInfo.InvariantCulture).PadLeft(2, '0') + rightNow.Minute.ToString(CultureInfo.InvariantCulture).PadLeft(2, '0') + rightNow.Second.ToString(CultureInfo.InvariantCulture).PadLeft(2, '0');
        }
        //
        //====================================================================================================
        /// <summary>
        /// Return a string with class.method() > class.method() > etc.
        /// </summary>
        /// <returns></returns>
        public static string getCallStack() {
            string callStack = "";
            StackTrace stackTrace = new StackTrace();
            foreach (var stackFrame in stackTrace.GetFrames()) {
                callStack += " > " + stackFrame.GetMethod().GetType().Name + "." + stackFrame.GetMethod().Name + "()";
            }
            return callStack;
        }
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    }
}
