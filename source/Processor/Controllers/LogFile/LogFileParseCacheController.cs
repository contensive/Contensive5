using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Contensive.Processor.Models.Domain;
//
namespace Contensive.Processor.Controllers {
    //
    //====================================================================================================
    /// <summary>
    /// A short-lived, process-wide cache of parsed log files, so paging, searching, sorting and filtering the log file tool does not re-read and re-parse the file on every ajax call.
    /// Key is path (ignore case), file length, last write time (utc ticks) and the parse byte limit, so a file that has grown is always parsed again.
    /// At most maxCachedFiles results are kept (least recently used is evicted), each lives at most cacheLifeMinutes.
    /// Cached results are shared and must be treated as read-only. No dependency on CPClass or CoreController.
    /// </summary>
    public static class LogFileParseCacheController {
        //
        public const int maxCachedFiles = 2;
        public const int cacheLifeMinutes = 2;
        //
        /// <summary>
        /// The clock, utc. Settable so tests can move time.
        /// </summary>
        public static Func<DateTime> utcNow { get; set; } = () => DateTime.UtcNow;
        //
        private sealed class CacheEntry {
            public string key;
            public string path;
            public Lazy<LogFileParseResultModel> result;
            public DateTime createdUtc;
            public long lastAccess;
        }
        //
        private static readonly object cacheLock = new object();
        private static readonly Dictionary<string, CacheEntry> cache = new Dictionary<string, CacheEntry>(StringComparer.Ordinal);
        private static long accessCounter;
        //
        //====================================================================================================
        /// <summary>
        /// Return the parse of the file, from cache when the same unchanged file was parsed within the last 2 minutes. 
        /// Concurrent calls for the same key parse once. A parse that throws is not cached, and the exception is thrown to every waiting caller.
        /// </summary>
        /// <param name="absPathFilename"></param>
        /// <param name="maxParseBytes"></param>
        /// <param name="parseDelegate">Optional, for tests. Default is LogFileParserController.parseFile</param>
        /// <returns>The shared result. Do not change it or its entries list.</returns>
        public static LogFileParseResultModel getOrParse(string absPathFilename, long maxParseBytes, Func<string, long, LogFileParseResultModel> parseDelegate = null) {
            Func<string, long, LogFileParseResultModel> parse = parseDelegate ?? LogFileParserController.parseFile;
            string path = Path.GetFullPath(absPathFilename);
            var fileInfo = new FileInfo(path);
            string key = $"{path.ToLowerInvariant()}|{fileInfo.Length}|{fileInfo.LastWriteTimeUtc.Ticks}|{maxParseBytes}";
            CacheEntry entry;
            lock (cacheLock) {
                DateTime now = utcNow();
                removeExpired(now);
                if (!cache.TryGetValue(key, out entry)) {
                    //
                    // -- an older version of this same file (it has grown) can never be hit again, drop it now
                    foreach (string oldKey in cache.Values.Where(x => string.Equals(x.path, path, StringComparison.OrdinalIgnoreCase)).Select(x => x.key).ToList()) { cache.Remove(oldKey); }
                    entry = new CacheEntry {
                        key = key,
                        path = path,
                        createdUtc = now,
                        result = new Lazy<LogFileParseResultModel>(() => parse(path, maxParseBytes), LazyThreadSafetyMode.ExecutionAndPublication)
                    };
                    cache[key] = entry;
                }
                entry.lastAccess = ++accessCounter;
                //
                // -- evict least recently used. The entry just added or touched has the highest access value so it is never the one evicted
                while (cache.Count > maxCachedFiles) {
                    CacheEntry lru = cache.Values.OrderBy(x => x.lastAccess).First();
                    cache.Remove(lru.key);
                }
            }
            try {
                return entry.result.Value;
            } catch {
                //
                // -- never cache a failure. Remove only this entry, a newer one under the same key is left alone
                lock (cacheLock) {
                    if (cache.TryGetValue(key, out CacheEntry current) && ReferenceEquals(current, entry)) { cache.Remove(key); }
                }
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// The number of cached files, expired ones removed first. For tests.
        /// </summary>
        public static int count() {
            lock (cacheLock) {
                removeExpired(utcNow());
                return cache.Count;
            }
        }
        //
        /// <summary>
        /// Empty the cache and restore the real clock. For tests.
        /// </summary>
        public static void reset() {
            lock (cacheLock) {
                cache.Clear();
                utcNow = () => DateTime.UtcNow;
            }
        }
        //
        // -- caller holds cacheLock
        private static void removeExpired(DateTime now) {
            TimeSpan life = TimeSpan.FromMinutes(cacheLifeMinutes);
            foreach (string expiredKey in cache.Values.Where(x => now - x.createdUtc >= life).Select(x => x.key).ToList()) { cache.Remove(expiredKey); }
        }
    }
}
