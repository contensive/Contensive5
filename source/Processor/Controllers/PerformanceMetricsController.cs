
using Contensive.BaseClasses;
using Contensive.BaseModels;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
//
namespace Contensive.Processor.Controllers {
    //
    //===================================================================================================
    /// <summary>
    /// Static, thread-safe, per-app performance metrics collector.
    /// Records request response times and provides all-time and 5-minute rolling averages.
    /// </summary>
    public static class PerformanceMetricsController {
        //
        //===================================================================================================
        /// <summary>
        /// Snapshot of metrics for a single app
        /// </summary>
        public class MetricsSnapshot {
            public long AvgResponseTimeMs { get; set; }
            public long AvgResponseTime5MinMs { get; set; }
            public long HitCount { get; set; }
            public long HitCount5Min { get; set; }
            public long UptimeMinutes { get; set; }
        }
        //
        //===================================================================================================
        /// <summary>
        /// Per-app metrics state
        /// </summary>
        private class AppMetrics {
            public long TotalCount;
            public long TotalSumMs;
            public readonly ConcurrentQueue<(long ticksUtc, long elapsedMs)> RecentQueue = new ConcurrentQueue<(long, long)>();
            public readonly object PruneLock = new object();
        }
        //
        private static readonly ConcurrentDictionary<string, AppMetrics> metricsByApp = new ConcurrentDictionary<string, AppMetrics>(StringComparer.OrdinalIgnoreCase);
        //
        private static readonly Stopwatch processUptime = Stopwatch.StartNew();
        //
        private const long fiveMinuteTicks = 5L * 60 * TimeSpan.TicksPerSecond;
        //
        //===================================================================================================
        /// <summary>
        /// Record a request's response time for the given app
        /// </summary>
        public static void Record(string appName, long elapsedMs) {
            if (string.IsNullOrEmpty(appName)) { return; }
            var metrics = metricsByApp.GetOrAdd(appName, _ => new AppMetrics());
            //
            // -- all-time totals (lock-free)
            Interlocked.Increment(ref metrics.TotalCount);
            Interlocked.Add(ref metrics.TotalSumMs, elapsedMs);
            //
            // -- add to 5-minute rolling window and prune old entries
            long nowTicks = DateTime.UtcNow.Ticks;
            metrics.RecentQueue.Enqueue((nowTicks, elapsedMs));
            PruneQueue(metrics, nowTicks);
        }
        //
        //===================================================================================================
        /// <summary>
        /// Get a snapshot of metrics for the given app
        /// </summary>
        public static MetricsSnapshot GetMetrics(string appName) {
            var snapshot = new MetricsSnapshot {
                UptimeMinutes = (long)processUptime.Elapsed.TotalMinutes
            };
            if (string.IsNullOrEmpty(appName) || !metricsByApp.TryGetValue(appName, out AppMetrics metrics)) {
                return snapshot;
            }
            //
            // -- all-time average
            long count = Interlocked.Read(ref metrics.TotalCount);
            long sum = Interlocked.Read(ref metrics.TotalSumMs);
            snapshot.HitCount = count;
            if (count > 0) {
                snapshot.AvgResponseTimeMs = sum / count;
            }
            //
            // -- 5-minute window: prune then calculate
            long nowTicks = DateTime.UtcNow.Ticks;
            PruneQueue(metrics, nowTicks);
            long recentCount = 0;
            long recentSum = 0;
            foreach (var entry in metrics.RecentQueue) {
                recentCount++;
                recentSum += entry.elapsedMs;
            }
            snapshot.HitCount5Min = recentCount;
            if (recentCount > 0) {
                snapshot.AvgResponseTime5MinMs = recentSum / recentCount;
            }
            return snapshot;
        }
        //
        //===================================================================================================
        /// <summary>
        /// Remove entries older than 5 minutes from the queue
        /// </summary>
        private static void PruneQueue(AppMetrics metrics, long nowTicks) {
            long cutoff = nowTicks - fiveMinuteTicks;
            lock (metrics.PruneLock) {
                while (metrics.RecentQueue.TryPeek(out var oldest) && oldest.ticksUtc < cutoff) {
                    metrics.RecentQueue.TryDequeue(out _);
                }
            }
        }
        //
        //===================================================================================================
        /// <summary>
        /// Per-app last-persist timestamp, used to throttle site property writes to once per minute.
        /// </summary>
        private static readonly ConcurrentDictionary<string, long> lastPersistTicksByApp = new ConcurrentDictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        //
        private const long persistIntervalTicks = 60L * TimeSpan.TicksPerSecond;
        //
        //===================================================================================================
        /// <summary>
        /// Persist the current metrics snapshot to a site property so that external addons (aoStatus)
        /// can read it. Throttled to once per minute to avoid excessive writes.
        /// </summary>
        public static void PersistMetrics(CPBaseClass cp, string appName) {
            if (string.IsNullOrEmpty(appName)) { return; }
            try {
                long nowTicks = DateTime.UtcNow.Ticks;
                long lastPersist = lastPersistTicksByApp.GetOrAdd(appName, 0L);
                if ((nowTicks - lastPersist) < persistIntervalTicks) { return; }
                lastPersistTicksByApp[appName] = nowTicks;
                //
                var snapshot = GetMetrics(appName);
                var model = new StatusResponseModel.StatusMetricsModel {
                    avgResponseTimeMs = snapshot.AvgResponseTimeMs,
                    avgResponseTime5MinMs = snapshot.AvgResponseTime5MinMs,
                    hitCount = snapshot.HitCount,
                    hitCount5Min = snapshot.HitCount5Min,
                    uptimeMinutes = snapshot.UptimeMinutes
                };
                cp.Site.SetProperty("PerformanceMetricsStatus", JsonConvert.SerializeObject(model));
            } catch (Exception) {
                // -- metrics persistence is best-effort, never fail the request
            }
        }
    }
}
