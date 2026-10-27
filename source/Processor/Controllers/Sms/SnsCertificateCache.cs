using Contensive.Processor.Extensions;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Contensive.Processor.Controllers {
    //
    //====================================================================================================
    /// <summary>
    /// Fetch and cache the SNS signing certificates (PEM) by url. Process-wide, 24 hour expiry.
    /// Only call with a url that passed SnsMessageVerifier.isValidSnsUrl. Redirects are not followed, the fetch times out after 5 seconds and the response is capped at 20 KB.
    /// </summary>
    public static class SnsCertificateCache {
        //
        private const int maxCertificateBytes = 20 * 1024;
        private const int maxCacheEntries = 100;
        private static readonly TimeSpan cacheDuration = TimeSpan.FromHours(24);
        //
        private sealed class CacheEntry {
            public X509Certificate2 certificate { get; set; }
            public DateTime expires { get; set; }
        }
        //
        private static readonly ConcurrentDictionary<string, CacheEntry> cache = new ConcurrentDictionary<string, CacheEntry>(StringComparer.Ordinal);
        //
        private static readonly HttpClient httpClient = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromSeconds(5) };
        //
        //====================================================================================================
        /// <summary>
        /// The certificate at the url, from the cache or fetched. Returns null if it cannot be fetched or parsed. Never throws.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static X509Certificate2 get(string url) {
            try {
                if (string.IsNullOrEmpty(url)) { return null; }
                if (cache.TryGetValue(url, out CacheEntry entry) && entry.expires > DateTime.Now) {
                    return entry.certificate;
                }
                byte[] pem = fetch(url);
                if (pem == null) { return null; }
                X509Certificate2 certificate = parsePem(Encoding.ASCII.GetString(pem));
                if (certificate == null) { return null; }
                if (cache.Count >= maxCacheEntries) { cache.Clear(); }
                cache[url] = new CacheEntry { certificate = certificate, expires = DateTime.Now.Add(cacheDuration) };
                return certificate;
            } catch (Exception ex) {
                logger.Warn(ex, $"SnsCertificateCache.get, could not load the SNS signing certificate [{url}]");
                return null;
            }
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
