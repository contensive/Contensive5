using Contensive.Models.Db;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using static Contensive.Processor.Constants;

namespace Contensive.Processor.Controllers {
    //
    //====================================================================================================
    /// <summary>
    /// Tracked links in outbound text messages, and the click handling for /tl/{token}.
    /// Only urls on the site's own domains (appConfig.domainList) are tracked, and the destination is checked again on every click, so /tl is never an open redirect.
    /// </summary>
    public static class SmsLinkController {
        //
        public const int tokenLength = 14;
        public const int maxDestinationLength = 255;
        private const string tokenAlphabet = "abcdefghijklmnopqrstuvwxyz0123456789";
        private const int maxTokenAttempts = 5;
        //
        private static readonly Regex urlPattern = new Regex(@"https?://[^\s<>""]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        private static readonly Regex tokenPattern = new Regex(@"^[a-z0-9]{14}$", RegexOptions.CultureInvariant);
        private static readonly char[] trailingPunctuation = { '.', ',', ';', ':', '!', '?', ')' };
        //
        //====================================================================================================
        /// <summary>
        /// a url found in a text body
        /// </summary>
        public class UrlMatch {
            public int index { get; set; }
            public int length { get; set; }
            public string url { get; set; }
        }
        //
        //====================================================================================================
        /// <summary>
        /// The result of a click on /tl/{token}
        /// </summary>
        public class ClickResult {
            /// <summary>where to redirect. Always a site url, "/" when the token is unknown or the destination is not allowed</summary>
            public string redirectUrl { get; set; } = "/";
            /// <summary>true if this was the first click</summary>
            public bool isFirstClick { get; set; }
            /// <summary>the event arguments if Text Message Link Clicked was thrown, else null</summary>
            public Dictionary<string, string> eventThrown { get; set; }
        }
        //
        //====================================================================================================
        /// <summary>
        /// The urls in the body that may be tracked: http or https, host equal (case-insensitive) to one of the domains, at most 255 characters.
        /// Trailing . , ; : ! ? ) are left outside the url. Pure.
        /// </summary>
        public static List<UrlMatch> findTrackableUrls(string body, IEnumerable<string> domainList) {
            var result = new List<UrlMatch>();
            if (string.IsNullOrEmpty(body)) { return result; }
            foreach (Match match in urlPattern.Matches(body)) {
                string url = match.Value.TrimEnd(trailingPunctuation);
                if (url.Length == 0) { continue; }
                if (!isAllowedDestination(url, domainList)) { continue; }
                result.Add(new UrlMatch { index = match.Index, length = url.Length, url = url });
            }
            return result;
        }
        //
        //====================================================================================================
        /// <summary>
        /// true if the url is absolute http or https, at most 255 characters, with no user info, and the host equals (case-insensitive) one of the domains. Pure.
        /// Used when a link is created and again on every click.
        /// </summary>
        public static bool isAllowedDestination(string url, IEnumerable<string> domainList) {
            if (string.IsNullOrEmpty(url) || url.Length > maxDestinationLength || domainList == null) { return false; }
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri)) { return false; }
            if (!uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.Ordinal) && !uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.Ordinal)) { return false; }
            if (!string.IsNullOrEmpty(uri.UserInfo)) { return false; }
            foreach (string domain in domainList) {
                string host = normalizeDomain(domain);
                if (host.Length > 0 && host.Equals(uri.Host, StringComparison.OrdinalIgnoreCase)) { return true; }
            }
            return false;
        }
        //
        /// <summary>
        /// a domain list entry as a host: trimmed, no scheme, no port, no path
        /// </summary>
        private static string normalizeDomain(string domain) {
            if (string.IsNullOrWhiteSpace(domain)) { return ""; }
            string host = domain.Trim();
            int schemeEnd = host.IndexOf("://", StringComparison.Ordinal);
            if (schemeEnd >= 0) { host = host.Substring(schemeEnd + 3); }
            int pathStart = host.IndexOf('/');
            if (pathStart >= 0) { host = host.Substring(0, pathStart); }
            int portStart = host.IndexOf(':');
            if (portStart >= 0) { host = host.Substring(0, portStart); }
            return host;
        }
        //
        //====================================================================================================
        /// <summary>
        /// 14 characters from [a-z0-9], cryptographically random (about 72 bits). Lowercase because the default SQL Server collation is case-insensitive.
        /// </summary>
        public static string generateToken() {
            var sb = new StringBuilder(tokenLength);
            byte[] buffer = new byte[32];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create()) {
                while (sb.Length < tokenLength) {
                    rng.GetBytes(buffer);
                    foreach (byte b in buffer) {
                        // -- reject values that would bias the modulo (252 = 7 * 36)
                        if (b >= 252) { continue; }
                        sb.Append(tokenAlphabet[b % tokenAlphabet.Length]);
                        if (sb.Length == tokenLength) { break; }
                    }
                }
            }
            return sb.ToString();
        }
        //
        //====================================================================================================
        /// <summary>
        /// true if the token is exactly 14 characters [a-z0-9]
        /// </summary>
        public static bool isValidToken(string token) => !string.IsNullOrEmpty(token) && tokenPattern.IsMatch(token);
        //
        //====================================================================================================
        /// <summary>
        /// The token from a /tl/{token} request path (last segment, lower case), else empty.
        /// </summary>
        public static string getTokenFromPath(string pathPage) {
            if (string.IsNullOrEmpty(pathPage)) { return ""; }
            string path = pathPage;
            int queryStart = path.IndexOfAny(new[] { '?', '#' });
            if (queryStart >= 0) { path = path.Substring(0, queryStart); }
            path = path.TrimEnd('/');
            int lastSlash = path.LastIndexOf('/');
            string segment = (lastSlash >= 0) ? path.Substring(lastSlash + 1) : path;
            if (segment.Equals("tl", StringComparison.OrdinalIgnoreCase)) { return ""; }
            return segment.ToLowerInvariant();
        }
        //
        //====================================================================================================
        /// <summary>
        /// Replace each site-domain url in the body with a tracked link https://{primary domain}/tl/{token}, one Text Message Links row per url.
        /// Other urls are left as they are. Returns the body unchanged on any error, or if the site has no primary domain.
        /// </summary>
        public static string replaceLinks(CoreController core, string body, int textMessageLogId, int memberId) {
            try {
                if (string.IsNullOrEmpty(body)) { return body; }
                List<string> domainList = core.appConfig.domainList;
                string primaryDomain = normalizeDomain(core.cpParent.Site.DomainPrimary);
                if (domainList == null || domainList.Count == 0 || string.IsNullOrEmpty(primaryDomain)) { return body; }
                List<UrlMatch> matches = findTrackableUrls(body, domainList);
                if (matches.Count == 0) { return body; }
                var sb = new StringBuilder();
                int position = 0;
                foreach (UrlMatch match in matches) {
                    string token = createLink(core, match.url, textMessageLogId, memberId);
                    if (string.IsNullOrEmpty(token)) { continue; }
                    sb.Append(body, position, match.index - position);
                    sb.Append($"https://{primaryDomain}/tl/{token}");
                    position = match.index + match.length;
                }
                sb.Append(body, position, body.Length - position);
                return sb.ToString();
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage},replaceLinks");
                return body;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// add a Text Message Links row with a new unique token. Returns the token, or empty if no unique token was found.
        /// </summary>
        private static string createLink(CoreController core, string destinationUrl, int textMessageLogId, int memberId) {
            for (int attempt = 0; attempt < maxTokenAttempts; attempt++) {
                string token = generateToken();
                if (TextMessageLinkModel.getByToken(core.cpParent, token) != null) { continue; }
                var link = DbBaseModel.addDefault<TextMessageLinkModel>(core.cpParent);
                link.name = truncate($"{token} {destinationUrl}", 255);
                link.token = token;
                link.textMessageLogId = textMessageLogId;
                link.memberId = memberId;
                link.destinationUrl = destinationUrl;
                link.clickCount = 0;
                link.firstClickDate = null;
                link.lastClickDate = null;
                link.save(core.cpParent);
                return token;
            }
            logger.Warn($"{core.logCommonMessage},replaceLinks, no unique token after {maxTokenAttempts} attempts, link not tracked");
            return "";
        }
        //
        //====================================================================================================
        /// <summary>
        /// A click on /tl/{token}. The destination comes only from the stored row, never from the request, and is checked against the domain list again.
        /// Unknown or invalid tokens and disallowed destinations return "/" (the home page). Every click is counted.
        /// The first click (decided by an atomic update) throws Text Message Link Clicked, unless the text was a test.
        /// </summary>
        public static ClickResult processClick(CoreController core, string token) {
            var result = new ClickResult();
            if (!isValidToken(token)) { return result; }
            TextMessageLinkModel link = TextMessageLinkModel.getByToken(core.cpParent, token);
            if (link == null) { return result; }
            if (!isAllowedDestination(link.destinationUrl, core.appConfig.domainList)) {
                logger.Warn($"{core.logCommonMessage},tracked link [{link.id}] destination is not on a site domain, redirected to the home page");
                return result;
            }
            // -- redirect to the url as parsed and validated, not the raw stored text
            result.redirectUrl = new Uri(link.destinationUrl, UriKind.Absolute).AbsoluteUri;
            //
            // -- count, and claim the first click atomically so two simultaneous first clicks throw one event
            DateTime now = core.dateTimeNowMockable;
            var parameters = new Dictionary<string, object> { { "@id", link.id }, { "@now", now } };
            core.db.executeNonQuery("update ccTextMessageLinks set clickCount=isnull(clickCount,0)+1, lastClickDate=@now where id=@id", parameters);
            int recordsAffected = 0;
            core.db.executeNonQuery("update ccTextMessageLinks set firstClickDate=@now where id=@id and firstClickDate is null", parameters, ref recordsAffected);
            core.cache.invalidateRecordKey(link.id, TextMessageLinkModel.tableMetadata.tableNameLower);
            if (recordsAffected <= 0) { return result; }
            result.isFirstClick = true;
            //
            // -- event, not for test sends
            TextMessageLogModel log = (link.textMessageLogId > 0) ? DbBaseModel.create<TextMessageLogModel>(core.cpParent, link.textMessageLogId) : null;
            if (log != null && log.isTest) { return result; }
            var args = SmsNotificationController.getEventArguments(eventNameTextMessageLinkClicked, log);
            if (log == null) {
                args["smsTextMessageLogId"] = link.textMessageLogId.ToString();
                args["smsMemberId"] = link.memberId.ToString();
                args["smsIsTest"] = "0";
            }
            args["smsLinkUrl"] = link.destinationUrl ?? "";
            SmsNotificationController.throwEvent(core, args, null);
            result.eventThrown = args;
            return result;
        }
        //
        private static string truncate(string text, int maxLength) {
            if (string.IsNullOrEmpty(text)) { return ""; }
            return (text.Length <= maxLength) ? text : text.Substring(0, maxLength);
        }
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    }
}
