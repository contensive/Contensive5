using Contensive.Processor.Extensions;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Contensive.Processor.Controllers {
    //
    //====================================================================================================
    /// <summary>
    /// Fetch and cache the SNS signing certificates (PEM). Process-wide.
    /// Only call with a url that passed SnsMessageVerifier.isValidSnsUrl. Limits, so forged posts cannot turn the site into a fetch amplifier:
    /// - cached by scheme+host+path for 24 hours; a failed fetch is cached for 5 minutes;
    /// - at most 10 fetches per minute for the process; over the limit the certificate is not available (the message is rejected, SNS retries later);
    /// - redirects are not followed, the fetch times out after 5 seconds and the response is capped at 20 KB;
    /// - at most 100 entries; expired entries are evicted first, then the entries that expire soonest.
    /// </summary>
    public static class SnsCertificateCache {
        //
        private const int maxCertificateBytes = 20 * 1024;
        private const int maxCacheEntries = 100;
        private const int maxFetchesPerMinute = 10;
        private static readonly TimeSpan cacheDuration = TimeSpan.FromHours(24);
        private static readonly TimeSpan failureCacheDuration = TimeSpan.FromMinutes(5);
        //
        private sealed class CacheEntry {
            /// <summary>null for a cached failure</summary>
            public X509Certificate2 certificate { get; set; }
            public DateTime expires { get; set; }
        }
        //
        private static readonly ConcurrentDictionary<string, CacheEntry> cache = new ConcurrentDictionary<string, CacheEntry>(StringComparer.Ordinal);
        //
        private static readonly object fetchLimitLock = new object();
        private static DateTime fetchWindowStart = DateTime.MinValue;
        private static int fetchWindowCount;
        //
        private static readonly HttpClient httpClient = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromSeconds(5) };
        //
        //====================================================================================================
        /// <summary>
        /// The certificate at the url, from the cache or fetched. Returns null if it cannot be fetched or parsed. Never throws.
        /// </summary>
        public static X509Certificate2 get(string url) => get(url, fetch);
        //
        //====================================================================================================
        /// <summary>
        /// The certificate at the url, using the given fetcher (tests). Returns null if it cannot be fetched or parsed. Never throws.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="fetcher">returns the response body, or null on failure</param>
        /// <returns></returns>
        public static X509Certificate2 get(string url, Func<string, byte[]> fetcher) {
            string key = getCacheKey(url);
            try {
                if (string.IsNullOrEmpty(key) || fetcher == null) { return null; }
                DateTime now = DateTime.Now;
                if (cache.TryGetValue(key, out CacheEntry entry) && entry.expires > now) {
                    return entry.certificate;
                }
                if (!tryClaimFetch(now)) {
                    logger.Warn($"SnsCertificateCache.get, fetch limit reached, certificate not fetched [{key}]");
                    return null;
                }
                X509Certificate2 certificate = null;
                try {
                    byte[] pem = fetcher(key);
                    if (pem != null) { certificate = parsePem(Encoding.ASCII.GetString(pem)); }
                } catch (Exception ex) {
                    logger.Warn(ex, $"SnsCertificateCache.get, could not load the SNS signing certificate [{key}]");
                    certificate = null;
                }
                store(key, new CacheEntry { certificate = certificate, expires = now.Add((certificate == null) ? failureCacheDuration : cacheDuration) }, now);
                return certificate;
            } catch (Exception ex) {
                logger.Warn(ex, $"SnsCertificateCache.get, unexpected error [{key}]");
                return null;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// The cache key and fetch url: scheme, host and path only (lower-case scheme and host). Empty if the url is not absolute.
        /// </summary>
        public static string getCacheKey(string url) {
            if (string.IsNullOrEmpty(url)) { return ""; }
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri)) { return ""; }
            return uri.GetLeftPart(UriPartial.Path);
        }
        //
        //====================================================================================================
        /// <summary>
        /// Empty the cache and reset the fetch limit (tests).
        /// </summary>
        public static void clear() {
            cache.Clear();
            lock (fetchLimitLock) {
                fetchWindowStart = DateTime.MinValue;
                fetchWindowCount = 0;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// true if a fetch is allowed in the current one-minute window
        /// </summary>
        private static bool tryClaimFetch(DateTime now) {
            lock (fetchLimitLock) {
                if (now - fetchWindowStart >= TimeSpan.FromMinutes(1)) {
                    fetchWindowStart = now;
                    fetchWindowCount = 0;
                }
                if (fetchWindowCount >= maxFetchesPerMinute) { return false; }
                fetchWindowCount++;
                return true;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// store an entry, evicting expired entries, then the soonest-expiring, when full
        /// </summary>
        private static void store(string key, CacheEntry entry, DateTime now) {
            if (cache.Count >= maxCacheEntries) {
                foreach (var expired in cache.Where(kvp => kvp.Value.expires <= now).Select(kvp => kvp.Key).ToList()) {
                    cache.TryRemove(expired, out _);
                }
                while (cache.Count >= maxCacheEntries) {
                    var soonest = cache.OrderBy(kvp => kvp.Value.expires).FirstOrDefault();
                    if (soonest.Key == null || !cache.TryRemove(soonest.Key, out _)) { break; }
                }
            }
            cache[key] = entry;
        }
        //
        //====================================================================================================
        /// <summary>
        /// GET the url, return the body or null if not 200 or larger than the cap
        /// </summary>
        private static byte[] fetch(string url) {
            using (HttpResponseMessage response = httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).waitSynchronously()) {
                if (!response.IsSuccessStatusCode) { return null; }
                long? contentLength = response.Content.Headers.ContentLength;
                if (contentLength.HasValue && contentLength.Value > maxCertificateBytes) { return null; }
                using (Stream stream = response.Content.ReadAsStreamAsync().waitSynchronously())
                using (var buffer = new MemoryStream()) {
                    byte[] chunk = new byte[4096];
                    int read;
                    while ((read = stream.Read(chunk, 0, chunk.Length)) > 0) {
                        buffer.Write(chunk, 0, read);
                        if (buffer.Length > maxCertificateBytes) { return null; }
                    }
                    return buffer.ToArray();
                }
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Parse the first certificate in a PEM document. Returns null if there is none.
        /// </summary>
        /// <param name="pem"></param>
        /// <returns></returns>
        public static X509Certificate2 parsePem(string pem) {
            if (string.IsNullOrEmpty(pem)) { return null; }
            const string beginMarker = "-----BEGIN CERTIFICATE-----";
            const string endMarker = "-----END CERTIFICATE-----";
            int begin = pem.IndexOf(beginMarker, StringComparison.Ordinal);
            if (begin < 0) { return null; }
            begin += beginMarker.Length;
            int end = pem.IndexOf(endMarker, begin, StringComparison.Ordinal);
            if (end < 0) { return null; }
            string base64 = pem.Substring(begin, end - begin).Replace("\r", "").Replace("\n", "").Replace(" ", "").Trim();
            byte[] der = Convert.FromBase64String(base64);
#if NET9_0_OR_GREATER
            return X509CertificateLoader.LoadCertificate(der);
#else
            return new X509Certificate2(der);
#endif
        }
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    }
}
