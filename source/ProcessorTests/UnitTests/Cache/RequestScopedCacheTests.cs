
using Contensive.Processor;
using Contensive.Processor.Controllers;
using Contensive.Processor.Models.Domain;
using Contensive.Processor.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Contensive.Processor.Tests.TestConstants;

namespace Contensive.Processor.Tests.UnitTests.Cache;
//
//====================================================================================================
/// <summary>
/// Job 20261126-contensive-render-speed, item 2. Request-scoped L1 cache (design 10.2).
/// Tests named l1_*_unit use the RequestScopedCache class on its own (linked source) and need nothing.
/// The other tests need the test app and Redis (enableRemoteCache with a reachable endpoint); they are inconclusive without Redis.
/// </summary>
[TestClass()]
public class RequestScopedCacheTests {
    //
    public class L1TestClass {
        public string name { get; set; } = "a";
        public int number { get; set; } = 99;
    }
    //
    private static string jsonDocument(object content) {
        var doc = new CacheDocumentClass(DateTime.Now) { content = content };
        return Newtonsoft.Json.JsonConvert.SerializeObject(doc);
    }
    //
    //====================================================================================================
    // -- 10.2-1
    [TestMethod]
    public void l1_setGet_lazyJson_unit() {
        var l1 = new RequestScopedCache();
        l1.setFromWrite("h1", jsonDocument(new L1TestClass { name = "x", number = 5 }));
        Assert.IsTrue(l1.tryGet("h1", out CacheDocumentClass doc, out bool isMiss));
        Assert.IsFalse(isMiss);
        Assert.IsInstanceOfType(doc.content, typeof(JObject));
        L1TestClass a = ((JObject)doc.content).ToObject<L1TestClass>();
        Assert.IsTrue(l1.tryGet("h1", out CacheDocumentClass doc2, out _));
        L1TestClass b = ((JObject)doc2.content).ToObject<L1TestClass>();
        Assert.AreNotSame(a, b);
        Assert.AreEqual("x", b.name);
        Assert.AreEqual(5, b.number);
    }
    //
    //====================================================================================================
    // -- 10.2-2
    [TestMethod]
    public void l1_miss_entry_unit() {
        var l1 = new RequestScopedCache();
        Assert.IsFalse(l1.tryGet("h1", out _, out _));
        l1.setMiss("h1");
        Assert.IsTrue(l1.tryGet("h1", out CacheDocumentClass doc, out bool isMiss));
        Assert.IsTrue(isMiss);
        Assert.IsNull(doc);
    }
    //
    //====================================================================================================
    // -- 10.2-3
    [TestMethod]
    public void l1_cap_clears_unit() {
        var l1 = new RequestScopedCache();
        string json = jsonDocument("v");
        for (int i = 0; i < RequestScopedCache.maxEntries; i++) {
            Assert.AreEqual(RequestScopedCache.SetResult.Stored, l1.setFromWrite($"h{i}", json));
        }
        Assert.AreEqual(RequestScopedCache.maxEntries, l1.count);
        // overwriting an existing key at the cap does not clear
        Assert.AreEqual(RequestScopedCache.SetResult.Stored, l1.setFromWrite("h0", json));
        Assert.AreEqual(0, l1.capClears);
        Assert.AreEqual(RequestScopedCache.SetResult.StoredAfterClear, l1.setFromWrite("hNew", json));
        Assert.AreEqual(1, l1.count);
        Assert.AreEqual(1, l1.capClears);
        Assert.IsTrue(l1.tryGet("hNew", out _, out _));
        Assert.IsFalse(l1.tryGet("h1", out _, out _));
    }
    //
    //====================================================================================================
    // -- 10.2-4
    [TestMethod]
    public void l1_oversize_skipped_unit() {
        var l1 = new RequestScopedCache();
        string big = jsonDocument(new string('x', RequestScopedCache.maxEntryChars + 1));
        Assert.AreEqual(RequestScopedCache.SetResult.Skipped, l1.setFromWrite("h1", big));
        Assert.AreEqual(0, l1.count);
        Assert.IsFalse(l1.tryGet("h1", out _, out _));
    }
    //
    //====================================================================================================
    // -- an oversize write must remove the older entry, else a later read would return a value older than this core's own write
    [TestMethod]
    public void l1_oversizeWrite_removesOlderEntry_unit() {
        var l1 = new RequestScopedCache();
        l1.setFromWrite("h1", jsonDocument("small"));
        Assert.IsTrue(l1.tryGet("h1", out _, out _));
        string big = jsonDocument(new string('x', RequestScopedCache.maxEntryChars + 1));
        Assert.AreEqual(RequestScopedCache.SetResult.Skipped, l1.setFromWrite("h1", big));
        Assert.IsFalse(l1.tryGet("h1", out _, out _));
    }
    //
    //====================================================================================================
    // -- write-through: a write replaces a remote-read entry and a negative entry
    [TestMethod]
    public void l1_writeThrough_unit() {
        var l1 = new RequestScopedCache();
        string jsonV1 = jsonDocument("v1");
        var docV1 = Newtonsoft.Json.JsonConvert.DeserializeObject<CacheDocumentClass>(jsonV1);
        l1.setFromRemoteRead("h1", jsonV1, docV1);
        Assert.IsTrue(l1.tryGet("h1", out CacheDocumentClass read1, out _));
        Assert.AreSame(docV1, read1);
        Assert.AreEqual("v1", read1.content);
        //
        l1.setFromWrite("h1", jsonDocument("v2"));
        Assert.IsTrue(l1.tryGet("h1", out CacheDocumentClass read2, out bool isMiss2));
        Assert.IsFalse(isMiss2);
        Assert.AreEqual("v2", read2.content);
        //
        l1.setMiss("h2");
        l1.setFromWrite("h2", jsonDocument("v3"));
        Assert.IsTrue(l1.tryGet("h2", out CacheDocumentClass read3, out bool isMiss3));
        Assert.IsFalse(isMiss3);
        Assert.AreEqual("v3", read3.content);
    }
    //
    //====================================================================================================
    // -- review m2: total size budget. An add that would pass maxTotalChars clears all entries first
    [TestMethod]
    public void l1_sizeBudget_clears_unit() {
        var l1 = new RequestScopedCache();
        // documents just under the per-entry limit
        string json = jsonDocument(new string('x', RequestScopedCache.maxEntryChars - 200));
        int fit = (int)(RequestScopedCache.maxTotalChars / json.Length);
        Assert.IsTrue(fit > 1 && fit < RequestScopedCache.maxEntries, "the budget, not the entry cap, is what is tested");
        for (int i = 0; i < fit; i++) {
            Assert.AreEqual(RequestScopedCache.SetResult.Stored, l1.setFromWrite($"h{i}", json));
        }
        Assert.AreEqual((long)fit * json.Length, l1.totalChars);
        Assert.IsTrue(l1.totalChars <= RequestScopedCache.maxTotalChars);
        // overwriting a key replaces its size, it does not add to it
        Assert.AreEqual(RequestScopedCache.SetResult.Stored, l1.setFromWrite("h0", json));
        Assert.AreEqual((long)fit * json.Length, l1.totalChars);
        Assert.AreEqual(0, l1.capClears);
        // the next new key passes the budget
        Assert.AreEqual(RequestScopedCache.SetResult.StoredAfterClear, l1.setFromWrite("hNew", json));
        Assert.AreEqual(1, l1.count);
        Assert.AreEqual(json.Length, l1.totalChars);
        Assert.AreEqual(1, l1.capClears);
        Assert.IsTrue(l1.tryGet("hNew", out _, out _));
        Assert.IsFalse(l1.tryGet("h1", out _, out _));
    }
    //
    //====================================================================================================
    // -- review m2: totalChars follows remove, clear, negative entries and oversize skips
    [TestMethod]
    public void l1_sizeBudget_accounting_unit() {
        var l1 = new RequestScopedCache();
        string a = jsonDocument("aaaa");
        string b = jsonDocument("bbbbbbbbbbbb");
        l1.setFromWrite("a", a);
        l1.setFromRemoteRead("b", b, Newtonsoft.Json.JsonConvert.DeserializeObject<CacheDocumentClass>(b));
        l1.setMiss("m");
        Assert.AreEqual(a.Length + b.Length, l1.totalChars, "a negative entry has no size");
        l1.remove("a");
        Assert.AreEqual(b.Length, l1.totalChars);
        l1.setFromWrite("b", jsonDocument(new string('x', RequestScopedCache.maxEntryChars + 1)));
        Assert.AreEqual(0, l1.totalChars, "an oversize write removes the older entry and its size");
        l1.setFromWrite("a", a);
        l1.clear();
        Assert.AreEqual(0, l1.totalChars);
        Assert.AreEqual(0, l1.count);
    }
    //
    //====================================================================================================
    // -- review m2: an entry keeps one form only. A remote read keeps the document; a write keeps the JSON until the first read
    [TestMethod]
    public void l1_singleForm_unit() {
        var l1 = new RequestScopedCache();
        string jsonV1 = jsonDocument("v1");
        l1.setFromRemoteRead("r", jsonV1, Newtonsoft.Json.JsonConvert.DeserializeObject<CacheDocumentClass>(jsonV1));
        Assert.AreEqual(0, l1.jsonEntryCount, "a remote-read entry holds only the document");
        l1.setFromWrite("w", jsonDocument("v2"));
        Assert.AreEqual(1, l1.jsonEntryCount, "a written entry holds the json until it is read");
        Assert.IsTrue(l1.tryGet("w", out CacheDocumentClass first, out _));
        Assert.AreEqual(0, l1.jsonEntryCount, "after the first read only the document is kept");
        Assert.IsTrue(l1.tryGet("w", out CacheDocumentClass second, out _));
        Assert.AreSame(first, second);
        Assert.AreEqual("v2", second.content);
        // a remote read with no document is not stored and removes the older entry
        Assert.AreEqual(RequestScopedCache.SetResult.Skipped, l1.setFromRemoteRead("w", jsonV1, null));
        Assert.IsFalse(l1.tryGet("w", out _, out _));
    }
    //
    //====================================================================================================
    //
    [TestMethod]
    public void l1_clear_remove_unit() {
        var l1 = new RequestScopedCache();
        l1.setFromWrite("h1", jsonDocument("v1"));
        l1.setFromWrite("h2", jsonDocument("v2"));
        l1.remove("h1");
        Assert.IsFalse(l1.tryGet("h1", out _, out _));
        Assert.AreEqual(1, l1.count);
        l1.clear();
        Assert.AreEqual(0, l1.count);
        Assert.AreEqual(0, l1.capClears);
        // null keys are ignored
        Assert.AreEqual(RequestScopedCache.SetResult.Skipped, l1.setFromWrite(null, jsonDocument("v")));
        Assert.IsFalse(l1.tryGet(null, out _, out _));
    }
    //
    //====================================================================================================
    //
    [TestMethod]
    public void l1_threadSafety_unit() {
        var l1 = new RequestScopedCache();
        string json = jsonDocument(new L1TestClass());
        Parallel.For(0, 8, t => {
            for (int i = 0; i < 20000; i++) {
                string h = $"h{(i * 7 + t) % 6000}";
                switch (i % 5) {
                    case 0: l1.setFromWrite(h, json); break;
                    case 1: l1.setMiss(h); break;
                    case 2:
                        if (l1.tryGet(h, out CacheDocumentClass doc, out bool isMiss) && !isMiss) {
                            Assert.IsNotNull(doc);
                        }
                        break;
                    case 3: l1.remove(h); break;
                    default:
                        if (i % 5000 == 4) { l1.clear(); } else { l1.tryGet(h, out _, out _); }
                        break;
                }
            }
        });
        Assert.IsTrue(l1.count <= RequestScopedCache.maxEntries);
    }
    //
    //====================================================================================================
    // -- integration helpers (Redis)
    //
    private static CPClass createWebCp(bool l1On) {
        CPClass cp = new(testAppName, TestHttpContext.createForRoute($"/l1test{GenericController.getRandomInteger()}"));
        cp.core.serverConfig.enableRequestScopedCache = l1On;
        cp.core.profiler = new RenderProfilerController();
        return cp;
    }
    //
    private static void requireRedis(CPClass cp) {
        if (!cp.core.cache.remoteCacheInitialized) { Assert.Inconclusive("redis not configured"); }
    }
    //
    private static long l1Hits(CPClass cp) => cp.core.profiler.cacheHits(RenderProfilerController.CacheTier.L1);
    //
    private static void repeatRead(bool l1On) {
        using CPClass cp = createWebCp(l1On);
        requireRedis(cp);
        Assert.AreEqual(l1On, cp.core.cache.requestScopedCacheActive);
        string key = $"l1test-{GenericController.getRandomInteger()}";
        string value = $"value{GenericController.getRandomInteger()}";
        cp.core.cache.storeObject(key, value);
        Assert.AreEqual(value, cp.core.cache.getObject<string>(key));
        long hitsAfterFirst = l1Hits(cp);
        Assert.AreEqual(value, cp.core.cache.getObject<string>(key));
        if (l1On) {
            Assert.IsTrue(l1Hits(cp) > hitsAfterFirst, "second read should be an L1 hit");
        } else {
            Assert.AreEqual(0, l1Hits(cp));
        }
    }
    //
    private static void storeThenRead(bool l1On) {
        using CPClass cp = createWebCp(l1On);
        requireRedis(cp);
        string key = $"l1test-{GenericController.getRandomInteger()}";
        cp.core.cache.storeObject(key, "v1");
        Assert.AreEqual("v1", cp.core.cache.getObject<string>(key));
        cp.core.cache.storeObject(key, "v2");
        Assert.AreEqual("v2", cp.core.cache.getObject<string>(key));
        if (!l1On) { Assert.AreEqual(0, l1Hits(cp)); }
    }
    //
    private static void invalidateSameRequest(bool l1On) {
        using CPClass cp = createWebCp(l1On);
        requireRedis(cp);
        string key = $"l1test-{GenericController.getRandomInteger()}";
        cp.core.cache.storeObject(key, "v1");
        Assert.AreEqual("v1", cp.core.cache.getObject<string>(key));
        cp.core.cache.invalidate(key);
        Assert.IsNull(cp.core.cache.getObject<string>(key));
        if (!l1On) { Assert.AreEqual(0, l1Hits(cp)); }
    }
    //
    private static void tableDependencySameRequest(bool l1On) {
        using CPClass cp = createWebCp(l1On);
        requireRedis(cp);
        string table = $"l1testtable{GenericController.getRandomInteger()}";
        string key = $"l1test-{GenericController.getRandomInteger()}";
        cp.core.cache.storeObject(key, "v1", new List<string> { cp.core.cache.createTableDependencyKey(table) });
        Assert.AreEqual("v1", cp.core.cache.getObject<string>(key));
        // saveDate resolution: make sure the dependency is saved after the object
        System.Threading.Thread.Sleep(30);
        cp.core.cache.invalidateTableDependencyKey(table);
        Assert.IsNull(cp.core.cache.getObject<string>(key));
        if (!l1On) { Assert.AreEqual(0, l1Hits(cp)); }
    }
    //
    //====================================================================================================
    // -- 10.2-5
    [TestMethod] public void l1_repeatRead_servedFromL1() { repeatRead(true); }
    // -- 10.2-6
    [TestMethod] public void l1_storeThenRead_sameRequest() { storeThenRead(true); }
    // -- 10.2-7
    [TestMethod] public void l1_invalidate_sameRequest() { invalidateSameRequest(true); }
    // -- 10.2-8
    [TestMethod] public void l1_tableDependency_sameRequest() { tableDependencySameRequest(true); }
    //
    //====================================================================================================
    // -- 10.2-9
    [TestMethod]
    public void l1_invalidateAll_clears() {
        using CPClass cp = createWebCp(true);
        requireRedis(cp);
        string key = $"l1test-{GenericController.getRandomInteger()}";
        cp.core.cache.storeObject(key, "v1");
        Assert.AreEqual("v1", cp.core.cache.getObject<string>(key));
        Assert.IsTrue(cp.core.cache.requestScopedCacheCount > 0);
        System.Threading.Thread.Sleep(30);
        cp.core.cache.invalidateAll();
        // only the global invalidation date document may be in L1 now
        Assert.IsTrue(cp.core.cache.requestScopedCacheCount <= 1, $"count {cp.core.cache.requestScopedCacheCount}");
        Assert.IsNull(cp.core.cache.getObject<string>(key));
    }
    //
    //====================================================================================================
    // -- 10.2-10
    [TestMethod]
    public void l1_objectIsolation() {
        using CPClass cp = createWebCp(true);
        requireRedis(cp);
        string key = $"l1test-{GenericController.getRandomInteger()}";
        cp.core.cache.storeObject(key, new L1TestClass { name = "original", number = 1 });
        L1TestClass first = cp.core.cache.getObject<L1TestClass>(key);
        Assert.AreEqual("original", first.name);
        first.name = "changed";
        first.number = 2;
        L1TestClass second = cp.core.cache.getObject<L1TestClass>(key);
        Assert.AreNotSame(first, second);
        Assert.AreEqual("original", second.name);
        Assert.AreEqual(1, second.number);
    }
    //
    //====================================================================================================
    // -- 10.2-11
    [TestMethod]
    public void l1_nonWebCore_off() {
        using CPClass cp = new(testAppName);
        cp.core.serverConfig.enableRequestScopedCache = true;
        cp.core.profiler = new RenderProfilerController();
        requireRedis(cp);
        Assert.IsFalse(cp.core.cache.requestScopedCacheActive);
        string key = $"l1test-{GenericController.getRandomInteger()}";
        cp.core.cache.storeObject(key, "v1");
        for (int i = 0; i < 3; i++) { Assert.AreEqual("v1", cp.core.cache.getObject<string>(key)); }
        Assert.AreEqual(0, l1Hits(cp));
        Assert.AreEqual(0, cp.core.cache.requestScopedCacheCount);
    }
    //
    //====================================================================================================
    // -- 10.2-12, documents consistency rule 1: a key already in L1 does not see other cores' writes during the request
    [TestMethod]
    public void l1_crossCore_snapshot() {
        string key = $"l1test-{GenericController.getRandomInteger()}";
        using CPClass coreA = createWebCp(true);
        requireRedis(coreA);
        coreA.core.cache.storeObject(key, "v1");
        Assert.AreEqual("v1", coreA.core.cache.getObject<string>(key));
        System.Threading.Thread.Sleep(30);
        using (CPClass coreB = createWebCp(true)) {
            coreB.core.cache.storeObject(key, "v2");
        }
        Assert.AreEqual("v1", coreA.core.cache.getObject<string>(key));
        using CPClass coreC = createWebCp(true);
        Assert.AreEqual("v2", coreC.core.cache.getObject<string>(key));
    }
    //
    //====================================================================================================
    // -- 10.2-13: tests 5-8 with the flag off give the same values and no L1 hits
    [TestMethod]
    public void l1_flagOff_unchanged() {
        repeatRead(false);
        storeThenRead(false);
        invalidateSameRequest(false);
        tableDependencySameRequest(false);
    }
    //
    //====================================================================================================
    // -- the default is on (Jay, 2026-11-26), and memory-only mode never activates L1
    [TestMethod]
    public void l1_defaultOn_unit() {
        Assert.IsTrue(new ServerConfigModel().enableRequestScopedCache);
    }
    //
    [TestMethod]
    public void l1_memoryOnly_off() {
        using CPClass cp = new(testAppName, TestHttpContext.createForRoute("/l1test"));
        if (cp.core.cache.remoteCacheInitialized) { Assert.Inconclusive("redis is configured, this test needs memory-only mode"); }
        cp.core.serverConfig.enableRequestScopedCache = true;
        Assert.IsFalse(cp.core.cache.requestScopedCacheActive);
    }
}
