
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
    /// - The shared pool closes its multiplexers when the AppDomain unloads (net48 IIS: web.config touch or bin deploy without a process recycle)
    ///   and when the process exits. The handlers are registered once, in the static constructor. After closeAll, get() returns null.
    /// </remarks>
    internal sealed class SharedRedisConnectionPool {
        //
        /// <summary>
        /// The process-wide pool used by CacheController.
        /// </summary>
        public static readonly SharedRedisConnectionPool shared;
        //
        /// <summary>
        /// Create the shared pool and register, once, the handler that closes its multiplexers when the AppDomain unloads (net48 IIS domain restart without a process recycle).
        /// ProcessExit is deliberately not handled: the OS frees the sockets at process end, and closing early would make work still running at shutdown skip its Redis writes and invalidations.
        /// </summary>
        static SharedRedisConnectionPool() {
            shared = new(TimeSpan.FromSeconds(10));
            AppDomain.CurrentDomain.DomainUnload += onDomainShutdown;
        }
        //
        private static void onDomainShutdown(object sender, EventArgs e) {
            shared.closeAll();
        }
        //
        private readonly ConcurrentDictionary<string, Lazy<ConnectionMultiplexer>> connections = new(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<string, long> retryAfterTimestamp = new(StringComparer.Ordinal);
        private readonly long retryBackoffTicks;
        private readonly Func<string, ConnectionMultiplexer> connect;
        private int _handlerRegistrations;
        private int _connectAttempts;
        private int _closed;
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
        /// <summary>
        /// true after closeAll. get() then returns null.
        /// </summary>
        public bool closed => Volatile.Read(ref _closed) != 0;
        //
        // ====================================================================================================
        /// <summary>
        /// Return the shared multiplexer for the endpoint, creating it on first use. Concurrent first calls wait for one connect.
        /// Returns null if the endpoint is empty, or if it is in back-off after a failed connect.
        /// Throws the connect exception on the call whose connect failed (the caller logs it, as before).
        /// </summary>
        public ConnectionMultiplexer get(string endpoint) {
            if (string.IsNullOrEmpty(endpoint)) { return null; }
            if (closed) { return null; }
            if (retryAfterTimestamp.TryGetValue(endpoint, out long retryAfter) && (Stopwatch.GetTimestamp() < retryAfter)) { return null; }
            Lazy<ConnectionMultiplexer> lazy = connections.GetOrAdd(endpoint, ep => new Lazy<ConnectionMultiplexer>(() => create(ep), LazyThreadSafetyMode.ExecutionAndPublication));
            try {
                ConnectionMultiplexer result = lazy.Value;
                if (!retryAfterTimestamp.IsEmpty) { retryAfterTimestamp.TryRemove(endpoint, out _); }
                return result;
            } catch {
                //
                // -- do not keep a failed Lazy (it would rethrow forever). Set the back-off first, so a thread that arrives after the Lazy is removed
                //    sees the back-off and does not connect again at once. Remove only this Lazy, in case another thread already added a new one.
                retryAfterTimestamp[endpoint] = Stopwatch.GetTimestamp() + retryBackoffTicks;
                ((ICollection<KeyValuePair<string, Lazy<ConnectionMultiplexer>>>)connections).Remove(new KeyValuePair<string, Lazy<ConnectionMultiplexer>>(endpoint, lazy));
                throw;
            }
        }
        //
        // ====================================================================================================
        /// <summary>
        /// Close and dispose every multiplexer this pool created, and stop handing out connections (get returns null afterwards).
        /// Called for the shared pool on AppDomain unload and process exit. Safe to call more than once. Never throws.
        /// </summary>
        public void closeAll() {
            Interlocked.Exchange(ref _closed, 1);
            foreach (KeyValuePair<string, Lazy<ConnectionMultiplexer>> pair in connections) {
                try {
                    if (connections.TryRemove(pair.Key, out Lazy<ConnectionMultiplexer> lazy) && lazy.IsValueCreated) {
                        ConnectionMultiplexer multiplexer = lazy.Value;
                        multiplexer.ConnectionFailed -= onConnectionFailed;
                        multiplexer.ConnectionRestored -= onConnectionRestored;
                        multiplexer.Close(allowCommandsToComplete: false);
                        multiplexer.Dispose();
                    }
                } catch (Exception ex) {
                    //
                    // -- shutting down, log and continue with the next one
                    try { logger.Warn(ex, "Redis shared connection close failed"); } catch (Exception) { }
                }
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
