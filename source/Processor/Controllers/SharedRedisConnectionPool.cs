
using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
//
namespace Contensive.Processor.Controllers {
    //
    //====================================================================================================
    /// <summary>
    /// Process-wide Redis connections, one ConnectionMultiplexer per endpoint string, created lazily and never disposed by a CacheController.
    /// StackExchange.Redis is designed for one multiplexer per process; it reconnects on its own after a connection loss.
    /// Used by CacheController when serverConfig.enableSharedRedisConnection is true. Same code in the IIS process and the task service.
    /// Job 20261126-contensive-render-speed, amendment A1.
    /// This class has no dependency on CoreController so it can be unit tested on its own (ProcessorTests links this file).
    /// </summary>
    /// <remarks>
    /// - The ConnectionFailed/ConnectionRestored handlers are registered once per multiplexer, when it is created. They are static and use the static logger,
    ///   so they never hold a reference to a per-request core.
    /// - A failed connect is not cached: the failed Lazy is removed, and the endpoint is in back-off for retryBackoff. During back-off get() returns null
    ///   at once, so a broken endpoint does not add the connect time to every request. The next get() after the back-off tries again.
    /// - If the endpoint string in config.json changes, the new string gets its own multiplexer. The old one is left open in the dictionary until the process
    ///   ends (it may still be in use by requests that started before the change). That is one idle connection per old endpoint, which is acceptable.
    /// - The endpoint string can contain a password. Never log it.
    /// </remarks>
    internal sealed class SharedRedisConnectionPool {
        //
        /// <summary>
        /// The process-wide pool used by CacheController.
        /// </summary>
        public static readonly SharedRedisConnectionPool shared = new(TimeSpan.FromSeconds(10));
        //
        private readonly ConcurrentDictionary<string, Lazy<ConnectionMultiplexer>> connections = new(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<string, long> retryAfterTimestamp = new(StringComparer.Ordinal);
        private readonly long retryBackoffTicks;
        private readonly Func<string, ConnectionMultiplexer> connect;
        private int _handlerRegistrations;
        private int _connectAttempts;
        //
        // ====================================================================================================
        /// <summary>
        /// Create a pool. Use SharedRedisConnectionPool.shared in product code; tests create their own.
        /// </summary>
        /// <param name="retryBackoff">After a failed connect, get() returns null for this long before it tries again.</param>
        /// <param name="connect">Optional connect function (tests). Default: ConnectionMultiplexer.Connect(createOptions(endpoint)).</param>
        public SharedRedisConnectionPool(TimeSpan retryBackoff, Func<string, ConnectionMultiplexer> connect = null) {
            retryBackoffTicks = (long)(retryBackoff.TotalSeconds * Stopwatch.Frequency);
            this.connect = connect ?? (endpoint => ConnectionMultiplexer.Connect(createOptions(endpoint)));
        }
        //
        // ====================================================================================================
        /// <summary>
        /// The connect options used for every Redis connection (shared or per-instance): AbortOnConnectFail=false, ConnectTimeout=5000.
        /// </summary>
        public static ConfigurationOptions createOptions(string endpoint) {
            var options = ConfigurationOptions.Parse(endpoint);
            options.AbortOnConnectFail = false;
            options.ConnectTimeout = 5000;
            return options;
        }
        //
        // ====================================================================================================
        /// <summary>
        /// number of times connection handlers were registered (one per multiplexer created)
        /// </summary>
        public int handlerRegistrations => Volatile.Read(ref _handlerRegistrations);
        //
        /// <summary>
        /// number of connect attempts (successful or not)
        /// </summary>
        public int connectAttempts => Volatile.Read(ref _connectAttempts);
        //
        /// <summary>
        /// number of endpoints that currently have a multiplexer (or a connect in progress)
        /// </summary>
        public int count => connections.Count;
        //
        // ====================================================================================================
        /// <summary>
        /// Return the shared multiplexer for the endpoint, creating it on first use. Concurrent first calls wait for one connect.
        /// Returns null if the endpoint is empty, or if it is in back-off after a failed connect.
        /// Throws the connect exception on the call whose connect failed (the caller logs it, as before).
        /// </summary>
        public ConnectionMultiplexer get(string endpoint) {
            if (string.IsNullOrEmpty(endpoint)) { return null; }
            if (retryAfterTimestamp.TryGetValue(endpoint, out long retryAfter) && (Stopwatch.GetTimestamp() < retryAfter)) { return null; }
            Lazy<ConnectionMultiplexer> lazy = connections.GetOrAdd(endpoint, ep => new Lazy<ConnectionMultiplexer>(() => create(ep), LazyThreadSafetyMode.ExecutionAndPublication));
            try {
                ConnectionMultiplexer result = lazy.Value;
                if (!retryAfterTimestamp.IsEmpty) { retryAfterTimestamp.TryRemove(endpoint, out _); }
                return result;
            } catch {
                //
                // -- do not keep a failed Lazy (it would rethrow forever). Remove only this Lazy, in case another thread already added a new one.
                ((ICollection<KeyValuePair<string, Lazy<ConnectionMultiplexer>>>)connections).Remove(new KeyValuePair<string, Lazy<ConnectionMultiplexer>>(endpoint, lazy));
                retryAfterTimestamp[endpoint] = Stopwatch.GetTimestamp() + retryBackoffTicks;
                throw;
            }
        }
        //
        // ====================================================================================================
        //
        private ConnectionMultiplexer create(string endpoint) {
            Interlocked.Increment(ref _connectAttempts);
            ConnectionMultiplexer multiplexer = connect(endpoint) ?? throw new InvalidOperationException("Redis connect returned no connection");
            multiplexer.ConnectionFailed += onConnectionFailed;
            multiplexer.ConnectionRestored += onConnectionRestored;
            Interlocked.Increment(ref _handlerRegistrations);
            logger.Info($"Redis shared connection created, connected [{multiplexer.IsConnected}]");
            return multiplexer;
        }
        //
        private static void onConnectionFailed(object sender, ConnectionFailedEventArgs e) {
            logger.Warn($"Redis shared connection failed, endpoint [{e.EndPoint}], failureType [{e.FailureType}]: {e.Exception?.Message}");
        }
        //
        private static void onConnectionRestored(object sender, ConnectionFailedEventArgs e) {
            logger.Info($"Redis shared connection restored, endpoint [{e.EndPoint}]");
        }
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    }
}
