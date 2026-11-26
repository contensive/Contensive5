
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
//
namespace Contensive.Processor.Controllers {
    //
    //====================================================================================================
    /// <summary>
    /// Render profiler. One instance per web request (only when serverConfig.enableRenderProfiler is true and the core serves a web request).
    /// Collects plain counters for sql, cache tiers, dependency checks, redis connect time and per-addon timing.
    /// When the profiler is off, core.profiler is null and every hook costs one null check.
    /// </summary>
    /// <remarks>
    /// Timing uses Stopwatch.GetTimestamp ticks. Counters are updated with Interlocked because executeNonQueryAsync can run on another thread.
    /// Addon exclusive time assumes addonEnter and addonExit for one addon are called on the same thread. If an addon starts work on another thread
    /// that executes nested addons with the same cp, exclusive times can be wrong.
    /// Records that arrive after writeLog (for example a late async sql) are ignored.
    /// Never put sql text or cache keys in profile output, they can contain user data.
    /// </remarks>
    public sealed class RenderProfilerController {
        //
        /// <summary>
        /// The cache tiers that are timed
        /// </summary>
        public enum CacheTier { L1 = 0, Remote = 1, LocalMemory = 2, LocalFile = 3 }
        //
        /// <summary>
        /// Per-addon statistics
        /// </summary>
        public class AddonStat {
            public int id { get; set; }
            public string name { get; set; }
            public int count { get; set; }
            public long inclusiveTicks { get; set; }
            public long exclusiveTicks { get; set; }
        }
        //
        private sealed class AddonFrame {
            public int addonId;
            public long startTicks;
            public long childTicks;
        }
        //
        // -- counters (updated with Interlocked)
        private long _sqlCount;
        private long _sqlTicks;
        private long _sqlMaxTicks;
        private readonly long[] _cacheHits = new long[4];
        private readonly long[] _cacheMisses = new long[4];
        private readonly long[] _cacheTicks = new long[4];
        private long _cacheWrites;
        private long _cacheWriteTicks;
        private long _dependencyChecks;
        private long _globalInvalidationReads;
        private long _redisConnectTicks;
        private long _l1Entries;
        private long _l1Clears;
        private long _l1Skips;
        private long _l1Negative;
        private long _renderTicks;
        private long _totalTicks;
        private int _closed;
        private readonly long _startTimestamp = Stopwatch.GetTimestamp();
        //
        // -- guarded by _lock
        private readonly object _lock = new();
        private readonly List<string> _rebuilds = [];
        private readonly Dictionary<int, AddonStat> _addons = [];
        private readonly Stack<AddonFrame> _addonStack = new();
        //
        // ====================================================================================================
        // -- read access (tests and output)
        public long sqlCount => Interlocked.Read(ref _sqlCount);
        public long sqlTicks => Interlocked.Read(ref _sqlTicks);
        public long sqlMaxTicks => Interlocked.Read(ref _sqlMaxTicks);
        public long cacheHits(CacheTier tier) => Interlocked.Read(ref _cacheHits[(int)tier]);
        public long cacheMisses(CacheTier tier) => Interlocked.Read(ref _cacheMisses[(int)tier]);
        public long cacheTicks(CacheTier tier) => Interlocked.Read(ref _cacheTicks[(int)tier]);
        public long cacheWrites => Interlocked.Read(ref _cacheWrites);
        public long cacheWriteTicks => Interlocked.Read(ref _cacheWriteTicks);
        public long dependencyChecks => Interlocked.Read(ref _dependencyChecks);
        public long globalInvalidationReads => Interlocked.Read(ref _globalInvalidationReads);
        public long redisConnectTicks => Interlocked.Read(ref _redisConnectTicks);
        public long l1Entries { get => Interlocked.Read(ref _l1Entries); set => Interlocked.Exchange(ref _l1Entries, value); }
        public long l1Clears => Interlocked.Read(ref _l1Clears);
        public long l1Skips => Interlocked.Read(ref _l1Skips);
        /// <summary>
        /// L1 negative entries read (Redis said null earlier in this request). Not counted as L1 hits or misses.
        /// </summary>
        public long l1Negative => Interlocked.Read(ref _l1Negative);
        public long renderTicks => Interlocked.Read(ref _renderTicks);
        public long totalTicks => Interlocked.Read(ref _totalTicks);
        public bool closed => Interlocked.CompareExchange(ref _closed, 0, 0) != 0;
        /// <summary>
        /// true if any cache structure was rebuilt during this request
        /// </summary>
        public bool cold { get { lock (_lock) { return _rebuilds.Count > 0; } } }
        /// <summary>
        /// the names of the structures rebuilt, in order
        /// </summary>
        public List<string> rebuilds { get { lock (_lock) { return [.. _rebuilds]; } } }
        /// <summary>
        /// a snapshot of the addon statistics
        /// </summary>
        public List<AddonStat> addonStats {
            get {
                lock (_lock) {
                    return _addons.Values.Select(a => new AddonStat { id = a.id, name = a.name, count = a.count, inclusiveTicks = a.inclusiveTicks, exclusiveTicks = a.exclusiveTicks }).ToList();
                }
            }
        }
        //
        // ====================================================================================================
        /// <summary>
        /// convert stopwatch ticks to milliseconds
        /// </summary>
        public static double ticksToMs(long ticks) { return ticks * 1000.0 / Stopwatch.Frequency; }
        //
        // ====================================================================================================
        // -- record methods
        public void recordSql(long ticks) {
            if (closed) { return; }
            Interlocked.Increment(ref _sqlCount);
            Interlocked.Add(ref _sqlTicks, ticks);
            long current;
            while (ticks > (current = Interlocked.Read(ref _sqlMaxTicks))) {
                if (Interlocked.CompareExchange(ref _sqlMaxTicks, ticks, current) == current) { break; }
            }
        }
        //
        public void recordCacheRead(CacheTier tier, bool hit, long ticks) {
            if (closed) { return; }
            int i = (int)tier;
            if (hit) { Interlocked.Increment(ref _cacheHits[i]); } else { Interlocked.Increment(ref _cacheMisses[i]); }
            Interlocked.Add(ref _cacheTicks[i], ticks);
        }
        //
        public void recordCacheWrite(long ticks) {
            if (closed) { return; }
            Interlocked.Increment(ref _cacheWrites);
            Interlocked.Add(ref _cacheWriteTicks, ticks);
        }
        //
        public void recordDependencyCheck() { if (!closed) { Interlocked.Increment(ref _dependencyChecks); } }
        public void recordGlobalInvalidationRead() { if (!closed) { Interlocked.Increment(ref _globalInvalidationReads); } }
        public void recordRedisConnect(long ticks) { if (!closed) { Interlocked.Add(ref _redisConnectTicks, ticks); } }
        public void recordL1Clear() { if (!closed) { Interlocked.Increment(ref _l1Clears); } }
        public void recordL1Skip() { if (!closed) { Interlocked.Increment(ref _l1Skips); } }
        //
        /// <summary>
        /// record an L1 negative entry read. Kept apart from l1.hit/l1.miss so the hit count only shows documents served from L1. The time is added to the L1 tier.
        /// </summary>
        public void recordL1Negative(long ticks) {
            if (closed) { return; }
            Interlocked.Increment(ref _l1Negative);
            Interlocked.Add(ref _cacheTicks[(int)CacheTier.L1], ticks);
        }
        //
        /// <summary>
        /// record that a cache structure was rebuilt (cold). Repeated names are kept, and shown as "name xN"
        /// </summary>
        public void markRebuild(string name) {
            if (closed) { return; }
            lock (_lock) { _rebuilds.Add(name); }
        }
        //
        public void addonEnter(int id, string name) {
            if (closed) { return; }
            long now = Stopwatch.GetTimestamp();
            lock (_lock) {
                _addonStack.Push(new AddonFrame { addonId = id, startTicks = now, childTicks = 0 });
                if (!_addons.TryGetValue(id, out AddonStat stat)) {
                    stat = new AddonStat { id = id, name = name ?? "" };
                    _addons.Add(id, stat);
                }
                stat.count++;
            }
        }
        //
        public void addonExit(int id) {
            if (closed) { return; }
            long now = Stopwatch.GetTimestamp();
            lock (_lock) {
                if (_addonStack.Count == 0) { return; }
                AddonFrame frame = _addonStack.Pop();
                long inclusive = now - frame.startTicks;
                long exclusive = inclusive - frame.childTicks;
                if (_addons.TryGetValue(frame.addonId, out AddonStat stat)) {
                    stat.inclusiveTicks += inclusive;
                    stat.exclusiveTicks += exclusive;
                }
                if (_addonStack.Count > 0) { _addonStack.Peek().childTicks += inclusive; }
            }
        }
        //
        // ====================================================================================================
        /// <summary>
        /// the cache mode, for output
        /// </summary>
        private static string getCacheMode(CoreController core) {
            if (core?.serverConfig == null) { return "none"; }
            bool redis = core.serverConfig.enableRemoteCache;
            bool memory = core.serverConfig.enableLocalMemoryCache;
            bool file = core.serverConfig.enableLocalFileCache;
            if (redis && memory) { return "redis+memory"; }
            if (redis) { return "redis"; }
            if (memory) { return "memory"; }
            if (file) { return "file"; }
            return "none";
        }
        //
        private string getRebuildsText() {
            List<string> list = rebuilds;
            return string.Join(",", list.GroupBy(x => x).Select(g => g.Count() > 1 ? $"{g.Key} x{g.Count()}" : g.Key));
        }
        //
        private static string tierText(CacheTier tier, RenderProfilerController p) {
            return $"{p.cacheHits(tier)}/{p.cacheMisses(tier)}/{ticksToMs(p.cacheTicks(tier)):0.0}";
        }
        //
        private static readonly Regex unsafeCommentChars = new("[^A-Za-z0-9 ._()\\-]", RegexOptions.Compiled);
        private static readonly Regex hyphenRun = new("-{2,}", RegexOptions.Compiled);
        //
        /// <summary>
        /// Make text safe inside an HTML comment. Allow-list: ASCII letters, digits, space and ._-() ; every other character becomes "_".
        /// Runs of hyphens become one hyphen, so "--" (and with it "-->" and "--!>") can never appear. The result is at most 100 characters.
        /// </summary>
        public static string getSafeCommentText(string text) {
            if (string.IsNullOrEmpty(text)) { return ""; }
            string result = hyphenRun.Replace(unsafeCommentChars.Replace(text, "_"), "-");
            return result.Length > 100 ? result.Substring(0, 100) : result;
        }
        //
        /// <summary>
        /// logCommonMessage that never throws, for use in profiler output and its error handling
        /// </summary>
        public static string getSafeLogCommonMessage(CoreController core) {
            try {
                return core?.logCommonMessage ?? "";
            } catch (Exception) {
                return "";
            }
        }
        //
        /// <summary>
        /// Build the admin-only HTML comment. The caller must check the user is an admin. Ends with "-->".
        /// </summary>
        public string getHtmlComment(CoreController core) {
            long now = Stopwatch.GetTimestamp();
            long render = core?.doc?.appStopWatch != null ? (long)(core.doc.appStopWatch.Elapsed.TotalSeconds * Stopwatch.Frequency) : now - _startTimestamp;
            Interlocked.Exchange(ref _renderTicks, render);
            var sb = new StringBuilder();
            string coldText = cold ? $"true({getRebuildsText()})" : "false";
            sb.Append($"<!-- renderProfile renderMs={ticksToMs(render):0.0} sql={sqlCount}/{ticksToMs(sqlTicks):0.0}");
            sb.Append($" cache l1={tierText(CacheTier.L1, this)} remote={tierText(CacheTier.Remote, this)} memory={tierText(CacheTier.LocalMemory, this)} file={tierText(CacheTier.LocalFile, this)}");
            sb.Append($" l1neg={l1Negative} deps={dependencyChecks} redisConnectMs={ticksToMs(redisConnectTicks):0.0} cold={coldText}");
            foreach (AddonStat a in addonStats.OrderByDescending(x => x.exclusiveTicks).Take(15)) {
                string safeName = getSafeCommentText(a.name);
                sb.Append($"\n addon {a.id} {safeName} n={a.count} incl={ticksToMs(a.inclusiveTicks):0.0}ms excl={ticksToMs(a.exclusiveTicks):0.0}ms");
            }
            sb.Append("\n-->");
            return sb.ToString();
        }
        //
        // ====================================================================================================
        private class TierDto {
            public long hit { get; set; }
            public long miss { get; set; }
            public double ms { get; set; }
        }
        private class AddonDto {
            public int id { get; set; }
            public string name { get; set; }
            public int n { get; set; }
            public double inclMs { get; set; }
            public double exclMs { get; set; }
        }
        private class SqlDto {
            public long count { get; set; }
            public double ms { get; set; }
            public double maxMs { get; set; }
        }
        private class CacheDto {
            public TierDto l1 { get; set; }
            public TierDto remote { get; set; }
            public TierDto memory { get; set; }
            public TierDto file { get; set; }
            public long writes { get; set; }
            public double writeMs { get; set; }
            public long depChecks { get; set; }
            public long globalReads { get; set; }
            public double redisConnectMs { get; set; }
            public long l1Entries { get; set; }
            public long l1Clears { get; set; }
            public long l1Skips { get; set; }
            public long l1Negative { get; set; }
        }
        private class LogDto {
            public int v { get; set; } = 1;
            public string path { get; set; }
            public int pageId { get; set; }
            public bool anon { get; set; }
            public string cacheMode { get; set; }
            public bool cold { get; set; }
            public List<string> rebuilds { get; set; }
            public double totalMs { get; set; }
            public double renderMs { get; set; }
            public SqlDto sql { get; set; }
            public CacheDto cache { get; set; }
            public List<AddonDto> addons { get; set; }
        }
        private TierDto tierDto(CacheTier t) { return new TierDto { hit = cacheHits(t), miss = cacheMisses(t), ms = Math.Round(ticksToMs(cacheTicks(t)), 2) }; }
        //
        /// <summary>
        /// Build the json for the log line. Does not close the profiler. Null-safe for a core without session, visit, doc or webServer.
        /// </summary>
        public string getLogJson(CoreController core) {
            long total = Interlocked.Read(ref _totalTicks);
            if (total == 0) { total = Stopwatch.GetTimestamp() - _startTimestamp; }
            var dto = new LogDto {
                path = core?.webServer?.requestPathPage ?? "",
                pageId = core?.doc?.pageController?.page?.id ?? 0,
                anon = !(core?.session?.visit?.visitAuthenticated ?? false),
                cacheMode = getCacheMode(core),
                cold = cold,
                rebuilds = rebuilds.GroupBy(x => x).Select(g => g.Count() > 1 ? $"{g.Key} x{g.Count()}" : g.Key).ToList(),
                totalMs = Math.Round(ticksToMs(total), 2),
                renderMs = Math.Round(ticksToMs(renderTicks), 2),
                sql = new SqlDto { count = sqlCount, ms = Math.Round(ticksToMs(sqlTicks), 2), maxMs = Math.Round(ticksToMs(sqlMaxTicks), 2) },
                cache = new CacheDto {
                    l1 = tierDto(CacheTier.L1),
                    remote = tierDto(CacheTier.Remote),
                    memory = tierDto(CacheTier.LocalMemory),
                    file = tierDto(CacheTier.LocalFile),
                    writes = cacheWrites,
                    writeMs = Math.Round(ticksToMs(cacheWriteTicks), 2),
                    depChecks = dependencyChecks,
                    globalReads = globalInvalidationReads,
                    redisConnectMs = Math.Round(ticksToMs(redisConnectTicks), 2),
                    l1Entries = l1Entries,
                    l1Clears = l1Clears,
                    l1Skips = l1Skips,
                    l1Negative = l1Negative
                },
                addons = addonStats.OrderByDescending(x => x.exclusiveTicks).Take(25).Select(a => new AddonDto {
                    id = a.id, name = a.name, n = a.count,
                    inclMs = Math.Round(ticksToMs(a.inclusiveTicks), 2), exclMs = Math.Round(ticksToMs(a.exclusiveTicks), 2)
                }).ToList()
            };
            return JsonConvert.SerializeObject(dto);
        }
        //
        private static readonly NLog.Logger profileLogger = NLog.LogManager.GetLogger("Contensive.RenderProfile");
        //
        /// <summary>
        /// Write one Info line to the Contensive.RenderProfile logger, then close the profiler so late records are ignored.
        /// Callers must still guard this call (try/catch): profiler output must never break the request or Dispose.
        /// </summary>
        public void writeLog(CoreController core) {
            if (closed) { return; }
            string json;
            try {
                Interlocked.Exchange(ref _totalTicks, Stopwatch.GetTimestamp() - _startTimestamp);
                json = getLogJson(core);
            } finally {
                //
                // -- close even if the json fails, so a later call does not try again
                Interlocked.Exchange(ref _closed, 1);
            }
            profileLogger.Info($"{getSafeLogCommonMessage(core)},renderProfile,{json}");
        }
    }
}
