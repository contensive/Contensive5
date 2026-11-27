
using Contensive.Processor;
using Contensive.Processor.Controllers;
using Contensive.Processor.Models.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using static Contensive.Processor.Tests.TestConstants;

namespace Contensive.Processor.Tests.UnitTests.Cache;
//
//====================================================================================================
/// <summary>
/// Job 20261126-contensive-render-speed, amendment A1. Shared Redis connection per process.
/// Tests named pool_*_unit use the SharedRedisConnectionPool class on its own (linked source) and need no database.
/// They connect with AbortOnConnectFail=false, which returns a (possibly disconnected) multiplexer even when nothing listens,
/// so they do not need Redis either; pool_connectionUsable_unit needs a Redis at REDIS_TEST_ENDPOINT (default 127.0.0.1:6379) and is inconclusive without one.
/// The shared_* tests need the test app with enableRemoteCache and a Redis endpoint, else they are inconclusive.
/// </summary>
[TestClass()]
public class SharedRedisConnectionTests {
    //
    private static string redisTestEndpoint => Environment.GetEnvironmentVariable("REDIS_TEST_ENDPOINT") is string ep && !string.IsNullOrEmpty(ep) ? ep : "127.0.0.1:6379";
    //
    /// <summary>
    /// fast connect for unit tests (the product uses ConnectTimeout 5000)
    /// </summary>
    private static ConnectionMultiplexer fastConnect(string endpoint) {
        ConfigurationOptions options = SharedRedisConnectionPool.createOptions(endpoint);
        options.ConnectTimeout = 500;
        return ConnectionMultiplexer.Connect(options);
    }
    //
    //====================================================================================================
    //
    [TestMethod]
    public void pool_createOptions_keepsConnectSettings_unit() {
        ConfigurationOptions options = SharedRedisConnectionPool.createOptions("127.0.0.1:6379,password=x");
        Assert.IsFalse(options.AbortOnConnectFail);
        Assert.AreEqual(5000, options.ConnectTimeout);
    }
    //
    //====================================================================================================
    //
    [TestMethod]
    public void pool_sameEndpoint_sameInstance_handlersOnce_unit() {
        var pool = new SharedRedisConnectionPool(TimeSpan.FromSeconds(10), fastConnect);
        ConnectionMultiplexer a = pool.get(redisTestEndpoint);
        ConnectionMultiplexer b = pool.get(redisTestEndpoint);
        ConnectionMultiplexer c = pool.get(redisTestEndpoint);
        try {
            Assert.IsNotNull(a);
            Assert.AreSame(a, b);
            Assert.AreSame(a, c);
            Assert.AreEqual(1, pool.connectAttempts);
            Assert.AreEqual(1, pool.handlerRegistrations);
            Assert.AreEqual(1, pool.count);
        } finally {
            a?.Dispose();
        }
    }
    //
    //====================================================================================================
    //
    [TestMethod]
    public void pool_differentEndpoint_ownInstance_unit() {
        var pool = new SharedRedisConnectionPool(TimeSpan.FromSeconds(10), fastConnect);
        ConnectionMultiplexer a = pool.get("127.0.0.1:6379");
        ConnectionMultiplexer b = pool.get("127.0.0.1:6379,connectRetry=2");
        try {
            Assert.AreNotSame(a, b);
            Assert.AreEqual(2, pool.count);
            Assert.AreEqual(2, pool.handlerRegistrations);
            // the old endpoint's multiplexer is kept (documented), a repeat get still returns it
            Assert.AreSame(a, pool.get("127.0.0.1:6379"));
        } finally {
            a?.Dispose();
            b?.Dispose();
        }
    }
    //
    //====================================================================================================
    //
    [TestMethod]
    public void pool_emptyEndpoint_null_unit() {
        var pool = new SharedRedisConnectionPool(TimeSpan.FromSeconds(10), fastConnect);
        Assert.IsNull(pool.get(""));
        Assert.IsNull(pool.get(null));
        Assert.AreEqual(0, pool.connectAttempts);
    }
    //
    //====================================================================================================
    // -- a failed connect is not cached forever: back-off returns null without connecting, then a later call retries
    [TestMethod]
    public void pool_failedConnect_backoff_thenRetry_unit() {
        int calls = 0;
        bool fail = true;
        var pool = new SharedRedisConnectionPool(TimeSpan.FromMilliseconds(300), ep => {
            Interlocked.Increment(ref calls);
            if (fail) { throw new RedisConnectionException(ConnectionFailureType.UnableToConnect, "test failure"); }
            return fastConnect(ep);
        });
        Assert.ThrowsException<RedisConnectionException>(() => pool.get(redisTestEndpoint));
        Assert.AreEqual(1, calls);
        Assert.AreEqual(0, pool.count, "failed Lazy must be removed");
        // in back-off: no connect attempt, null returned at once
        Assert.IsNull(pool.get(redisTestEndpoint));
        Assert.AreEqual(1, calls);
        // after the back-off a new attempt is made
        Thread.Sleep(400);
        fail = false;
        ConnectionMultiplexer m = pool.get(redisTestEndpoint);
        try {
            Assert.IsNotNull(m);
            Assert.AreEqual(2, calls);
            Assert.AreSame(m, pool.get(redisTestEndpoint));
            Assert.AreEqual(1, pool.handlerRegistrations);
        } finally {
            m?.Dispose();
        }
    }
    //
    //====================================================================================================
    // -- review m5: closeAll closes every multiplexer the pool created and get() returns null afterwards
    [TestMethod]
    public void pool_closeAll_closesAndStops_unit() {
        var pool = new SharedRedisConnectionPool(TimeSpan.FromSeconds(10), fastConnect);
        ConnectionMultiplexer a = pool.get("127.0.0.1:6379");
        ConnectionMultiplexer b = pool.get("127.0.0.1:6379,connectRetry=2");
        try {
            Assert.IsNotNull(a);
            Assert.IsNotNull(b);
            Assert.AreEqual(2, pool.count);
            Assert.IsFalse(pool.closed);
            pool.closeAll();
            Assert.IsTrue(pool.closed);
            Assert.AreEqual(0, pool.count);
            Assert.IsFalse(a.IsConnected);
            Assert.IsFalse(b.IsConnected);
            Assert.IsNull(pool.get("127.0.0.1:6379"), "no new connection after closeAll");
            Assert.AreEqual(2, pool.connectAttempts);
            // a second call is harmless
            pool.closeAll();
        } finally {
            a?.Dispose();
            b?.Dispose();
        }
    }
    //
    //====================================================================================================
    // -- concurrent first requests share one connect
    [TestMethod]
    public void pool_concurrentGet_singleConnect_unit() {
        var pool = new SharedRedisConnectionPool(TimeSpan.FromSeconds(10), fastConnect);
        var seen = new ConcurrentBag<ConnectionMultiplexer>();
        Parallel.For(0, 32, _ => seen.Add(pool.get(redisTestEndpoint)));
        ConnectionMultiplexer first = null;
        foreach (ConnectionMultiplexer m in seen) {
            first ??= m;
            Assert.AreSame(first, m);
        }
        Assert.AreEqual(1, pool.connectAttempts);
        Assert.AreEqual(1, pool.handlerRegistrations);
        first?.Dispose();
    }
    //
    //====================================================================================================
    //
    [TestMethod]
    public void pool_connectionUsable_unit() {
        var pool = new SharedRedisConnectionPool(TimeSpan.FromSeconds(10), fastConnect);
        ConnectionMultiplexer m = pool.get(redisTestEndpoint);
        try {
            if (!m.IsConnected) { Assert.Inconclusive($"no redis at {redisTestEndpoint}"); }
            IDatabase db = m.GetDatabase();
            string key = $"sharedRedisTest-{Guid.NewGuid()}";
            db.StringSet(key, "v1", TimeSpan.FromSeconds(30));
            Assert.AreEqual("v1", (string)pool.get(redisTestEndpoint).GetDatabase().StringGet(key));
            db.KeyDelete(key);
        } finally {
            m?.Dispose();
        }
    }
    //
    //====================================================================================================
    //
    [TestMethod]
    public void shared_defaultOn_unit() {
        Assert.IsTrue(new ServerConfigModel().enableSharedRedisConnection);
    }
    //
    //====================================================================================================
    // -- integration (test app + Redis)
    //
    private static void requireSharedRedis(CPClass cp) {
        if (!cp.core.cache.remoteCacheInitialized) { Assert.Inconclusive("redis not configured"); }
        if (!cp.core.cache.redisConnectionShared) { Assert.Inconclusive("enableSharedRedisConnection is false in config.json"); }
    }
    //
    [TestMethod]
    public void shared_twoCpClass_sameMultiplexer() {
        ConnectionMultiplexer first;
        using (CPClass cp = new(testAppName)) {
            requireSharedRedis(cp);
            first = cp.core.cache.redisConnection;
        }
        using (CPClass cp = new(testAppName)) {
            requireSharedRedis(cp);
            Assert.AreSame(first, cp.core.cache.redisConnection);
        }
    }
    //
    [TestMethod]
    public void shared_disposeLeavesConnected() {
        ConnectionMultiplexer shared;
        using (CPClass cp = new(testAppName)) {
            requireSharedRedis(cp);
            shared = cp.core.cache.redisConnection;
            Assert.IsTrue(shared.IsConnected, "redis configured but not connected");
        }
        // the core and its CacheController are disposed; the shared multiplexer is still connected and usable
        Assert.IsTrue(shared.IsConnected);
        string key = $"sharedRedisTest-{Guid.NewGuid()}";
        shared.GetDatabase().StringSet(key, "v1", TimeSpan.FromSeconds(30));
        using (CPClass cp = new(testAppName)) {
            Assert.AreSame(shared, cp.core.cache.redisConnection);
            Assert.AreEqual("v1", (string)cp.core.cache.redisConnection.GetDatabase().StringGet(key));
            // a cache round trip through the shared connection still works
            string cacheKey = $"sharedRedisTest-{Guid.NewGuid()}";
            cp.core.cache.storeObject(cacheKey, "value");
            Assert.AreEqual("value", cp.core.cache.getObject<string>(cacheKey));
        }
        shared.GetDatabase().KeyDelete(key);
    }
    //
    [TestMethod]
    public void shared_flagFalse_perInstanceConnection() {
        using CPClass cp = new(testAppName);
        requireSharedRedis(cp);
        ConnectionMultiplexer shared = cp.core.cache.redisConnection;
        // the flag is read when a CacheController is constructed, so build a second controller on the same core with the flag off
        cp.core.serverConfig.enableSharedRedisConnection = false;
        ConnectionMultiplexer own;
        using (var perInstance = new CacheController(cp.core)) {
            Assert.IsTrue(perInstance.remoteCacheInitialized);
            Assert.IsFalse(perInstance.redisConnectionShared);
            own = perInstance.redisConnection;
            Assert.IsNotNull(own);
            Assert.AreNotSame(shared, own);
        }
        // the per-instance connection is closed on dispose, the shared one is not
        Assert.IsFalse(own.IsConnected);
        Assert.IsTrue(shared.IsConnected);
    }
    //
    [TestMethod]
    public void shared_handlersRegisteredOncePerMultiplexer() {
        using (CPClass cp = new(testAppName)) { requireSharedRedis(cp); }
        int registrations = CacheController.sharedRedisHandlerRegistrations;
        for (int i = 0; i < 5; i++) {
            using CPClass cp = new(testAppName);
            Assert.IsTrue(cp.core.cache.redisConnectionShared);
        }
        Assert.AreEqual(registrations, CacheController.sharedRedisHandlerRegistrations, "new cores must not register handlers again");
        Assert.AreEqual(CacheController.sharedRedisConnectionCount, CacheController.sharedRedisHandlerRegistrations, "one registration per shared multiplexer");
    }
    //
    [TestMethod]
    public void shared_redisConnectMs_nearZeroAfterFirst() {
        using (CPClass cp = new(testAppName)) { requireSharedRedis(cp); }
        using CPClass cp2 = new(testAppName);
        cp2.core.profiler = new RenderProfilerController();
        // construct a fresh controller with the profiler attached to measure the connect hook
        using var cache = new CacheController(cp2.core);
        Assert.IsTrue(cache.redisConnectionShared);
        Assert.IsTrue(RenderProfilerController.ticksToMs(cp2.core.profiler.redisConnectTicks) < 5.0, $"redisConnectMs {RenderProfilerController.ticksToMs(cp2.core.profiler.redisConnectTicks)}");
    }
}
