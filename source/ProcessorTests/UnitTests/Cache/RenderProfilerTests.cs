
using Contensive.Models.Db;
using Contensive.Processor;
using Contensive.Processor.Controllers;
using Contensive.Processor.Models;
using Contensive.Processor.Models.Domain;
using Contensive.Processor.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using static Contensive.Processor.Tests.TestConstants;

namespace Contensive.Processor.Tests.UnitTests.Cache;
//
//====================================================================================================
/// <summary>
/// Job 20261126-contensive-render-speed, item 1. Render profiler tests.
/// Tests that render a page need the test app (sql server). profiler_counters_unit needs nothing.
/// </summary>
[TestClass()]
public class RenderProfilerTests {
    //
    // -- helpers
    private sealed class TestPageInfo {
        public string pageName;
        public string testString;
        public AddonModel testAddon;
        public PageContentModel testPage;
    }
    //
    /// <summary>
    /// create a page that runs the test addon. call with a core that has already been constructed. Caller removes the records with deleteTestPage()
    /// </summary>
    private static TestPageInfo createTestPage(CPClass cp, string pageName) {
        cp.Site.SetProperty("ALLOW HTML MINIFY", false);
        cp.core.siteProperties.anonymousUserResponseID = 0;
        AddonCollectionModel baseCollection = DbBaseModel.create<AddonCollectionModel>(cp, Contensive.Processor.Constants.baseCollectionGuid);
        AddonModel testAddon = DbBaseModel.addDefault<AddonModel>(cp);
        testAddon.name = $"profilerTestAddon{cp.Utils.GetRandomInteger()}";
        testAddon.dotNetClass = "Contensive.Processor.Addons.TestAddon";
        testAddon.remoteMethod = true;
        testAddon.collectionId = baseCollection.id;
        testAddon.htmlDocument = true;
        testAddon.save(cp);
        string testString = cp.Utils.GetRandomInteger().ToString();
        cp.Doc.SetProperty("test-in", testString);
        List<AddonListItemModel> addonList = [new AddonListItemModel() { designBlockTypeGuid = testAddon.ccguid, designBlockTypeName = "test addon" }];
        PageContentModel testPage = DbBaseModel.addDefault<PageContentModel>(cp);
        testPage.name = pageName;
        testPage.addonList = cp.JSON.Serialize(addonList);
        testPage.save(cp);
        LinkAliasModel linkAlias = DbBaseModel.addDefault<LinkAliasModel>(cp);
        linkAlias.name = testPage.name;
        linkAlias.pageId = testPage.id;
        linkAlias.save(cp);
        cp.core.routeMapRebuild();
        return new TestPageInfo { pageName = pageName, testString = testString, testAddon = testAddon, testPage = testPage };
    }
    //
    private static void deleteTestPage(CPClass cp, TestPageInfo info) {
        DbBaseModel.delete<AddonModel>(cp, info.testAddon.id);
        DbBaseModel.delete<PageContentModel>(cp, info.testPage.id);
    }
    //
    //====================================================================================================
    // -- 10.1-1
    [TestMethod]
    public void profiler_offByDefault() {
        string pageName = new Random().Next().ToString();
        using (CPClass cp = new(testAppName, TestHttpContext.createForRoute($"/{pageName}"))) {
            // a test machine may have the flag turned on in config.json. The assertion only holds with the default
            if (cp.core.serverConfig.enableRenderProfiler) { Assert.Inconclusive("enableRenderProfiler is true in config.json"); }
            TestPageInfo info = createTestPage(cp, pageName);
            // act
            string doc = cp.executeRoute($"/{pageName}");
            // assert
            Assert.IsNull(cp.core.profiler);
            Assert.IsFalse(doc.Contains("renderProfile"));
            deleteTestPage(cp, info);
        }
    }
    //
    //====================================================================================================
    // -- 10.1-2 pure unit test, no app needed
    [TestMethod]
    public void profiler_counters_unit() {
        var p = new RenderProfilerController();
        p.recordSql(100);
        p.recordSql(300);
        p.recordSql(200);
        Assert.AreEqual(3, p.sqlCount);
        Assert.AreEqual(600, p.sqlTicks);
        Assert.AreEqual(300, p.sqlMaxTicks);
        foreach (RenderProfilerController.CacheTier tier in Enum.GetValues(typeof(RenderProfilerController.CacheTier))) {
            p.recordCacheRead(tier, true, 10);
            p.recordCacheRead(tier, false, 5);
            p.recordCacheRead(tier, false, 5);
            Assert.AreEqual(1, p.cacheHits(tier));
            Assert.AreEqual(2, p.cacheMisses(tier));
            Assert.AreEqual(20, p.cacheTicks(tier));
        }
        p.recordCacheWrite(7);
        p.recordDependencyCheck();
        p.recordDependencyCheck();
        p.recordGlobalInvalidationRead();
        p.recordRedisConnect(50);
        Assert.AreEqual(1, p.cacheWrites);
        Assert.AreEqual(7, p.cacheWriteTicks);
        Assert.AreEqual(2, p.dependencyChecks);
        Assert.AreEqual(1, p.globalInvalidationReads);
        Assert.AreEqual(50, p.redisConnectTicks);
        Assert.IsFalse(p.cold);
        p.markRebuild("metadata");
        p.markRebuild("metadata");
        Assert.IsTrue(p.cold);
        Assert.AreEqual(2, p.rebuilds.Count);
        //
        // -- nested addons, A contains B. Use real elapsed time so inclusive and exclusive are measurable
        p.addonEnter(1, "A");
        Thread.Sleep(15);
        p.addonEnter(2, "B");
        Thread.Sleep(15);
        p.addonExit(2);
        Thread.Sleep(15);
        p.addonExit(1);
        var a = p.addonStats.Single(x => x.id == 1);
        var b = p.addonStats.Single(x => x.id == 2);
        Assert.AreEqual(1, a.count);
        Assert.AreEqual(1, b.count);
        Assert.IsTrue(a.exclusiveTicks < a.inclusiveTicks, "A exclusive must be less than A inclusive");
        Assert.AreEqual(b.inclusiveTicks, b.exclusiveTicks, "B has no children, exclusive == inclusive");
        Assert.AreEqual(a.inclusiveTicks, a.exclusiveTicks + b.inclusiveTicks, "A inclusive == A exclusive + B inclusive");
    }
    //
    [TestMethod]
    public void profiler_closed_ignoresLateRecords() {
        var p = new RenderProfilerController();
        p.recordSql(10);
        p.writeLog(null);
        Assert.IsTrue(p.closed);
        p.recordSql(10);
        Assert.AreEqual(1, p.sqlCount);
    }
    //
    [TestMethod]
    public void profiler_htmlComment_format_unit() {
        var p = new RenderProfilerController();
        p.recordSql(10);
        p.markRebuild("metadata");
        p.addonEnter(5, "bad--name");
        p.addonExit(5);
        string comment = p.getHtmlComment(null);
        Assert.IsTrue(comment.StartsWith("<!-- renderProfile"));
        Assert.IsTrue(comment.EndsWith("-->"));
        Assert.IsTrue(comment.Contains("sql=1/"));
        Assert.IsTrue(comment.Contains("cold=true(metadata)"));
        Assert.IsTrue(comment.Contains("bad- -name"), "double dash in addon name is escaped");
        // only one "-->" and it is the last
        Assert.AreEqual(comment.Length - 3, comment.IndexOf("-->"));
    }
    //
    //====================================================================================================
    // -- 10.1-3
    [TestMethod]
    public void profiler_anonymous_noComment() {
        string pageName = new Random().Next().ToString();
        using (CPClass cp = new(testAppName, TestHttpContext.createForRoute($"/{pageName}"))) {
            TestPageInfo info = createTestPage(cp, pageName);
            cp.core.profiler = new RenderProfilerController();
            // act
            string doc = cp.executeRoute($"/{pageName}");
            // assert
            Assert.IsFalse(cp.core.session.isAuthenticated);
            Assert.IsFalse(doc.Contains("<!-- renderProfile"));
            deleteTestPage(cp, info);
        }
    }
    //
    //====================================================================================================
    // -- 10.1-4
    [TestMethod]
    public void profiler_admin_comment() {
        string pageName = new Random().Next().ToString();
        using (CPClass cp = new(testAppName, TestHttpContext.createForRoute($"/{pageName}"))) {
            PersonModel testUser = null;
            TestPageInfo info = null;
            try {
                info = createTestPage(cp, pageName);
                testUser = DbBaseModel.addDefault<PersonModel>(cp);
                testUser.name = "RenderProfilerTestAdmin";
                testUser.admin = true;
                testUser.save(cp);
                Assert.IsTrue(cp.User.LoginByID(testUser.id));
                Assert.IsTrue(cp.User.IsAdmin);
                cp.core.profiler = new RenderProfilerController();
                // act
                string doc = cp.executeRoute($"/{pageName}");
                // assert
                int pos = doc.LastIndexOf("<!-- renderProfile");
                Assert.IsTrue(pos >= 0, "admin output has the profile comment");
                string block = doc.Substring(pos);
                Assert.IsTrue(block.TrimEnd().EndsWith("-->"), "comment is the last thing in the document");
                Assert.IsTrue(block.Contains("sql="));
                Assert.IsTrue(block.Contains("cold="));
            } finally {
                if (info != null) { deleteTestPage(cp, info); }
                if (testUser != null && testUser.id > 0) { DbBaseModel.delete<PersonModel>(cp, testUser.id); }
            }
        }
    }
    //
    //====================================================================================================
    // -- 10.1-5
    [TestMethod]
    public void profiler_sqlAndAddonCounted() {
        string pageName = new Random().Next().ToString();
        using (CPClass cp = new(testAppName, TestHttpContext.createForRoute($"/{pageName}"))) {
            TestPageInfo info = createTestPage(cp, pageName);
            var profiler = new RenderProfilerController();
            cp.core.profiler = profiler;
            // act
            cp.executeRoute($"/{pageName}");
            // assert
            Assert.IsTrue(profiler.sqlCount > 0);
            Assert.IsTrue(profiler.addonStats.Any(a => a.id == info.testAddon.id), "the test addon was timed");
            deleteTestPage(cp, info);
        }
    }
    //
    //====================================================================================================
    // -- 10.1-6
    [TestMethod]
    public void profiler_cold_then_warm() {
        string pageName = new Random().Next().ToString();
        string route = $"/{pageName}";
        TestPageInfo info;
        using (CPClass cp = new(testAppName, TestHttpContext.createForRoute(route))) {
            if (!cp.ServerConfig.enableLocalFileCache && !cp.ServerConfig.enableLocalMemoryCache && !cp.ServerConfig.enableRemoteCache) { return; }
            info = createTestPage(cp, pageName);
            cp.core.cache.invalidateAll();
            var cold = new RenderProfilerController();
            cp.core.profiler = cold;
            cp.executeRoute(route);
            Assert.IsTrue(cold.cold, "after invalidateAll something is rebuilt");
            Assert.IsTrue(cold.rebuilds.Contains("routeMap") || cold.rebuilds.Contains("addonCache"));
        }
        using (CPClass cp = new(testAppName, TestHttpContext.createForRoute(route))) {
            var warm = new RenderProfilerController();
            cp.core.profiler = warm;
            cp.executeRoute(route);
            Assert.IsFalse(warm.cold, $"second core is warm, rebuilds: {string.Join(",", warm.rebuilds)}");
            deleteTestPage(cp, info);
        }
    }
    //
    //====================================================================================================
    // -- 10.1-7
    [TestMethod]
    public void profiler_logLine() {
        string pageName = new Random().Next().ToString();
        var target = new NLog.Targets.MemoryTarget("renderProfileTest") { Layout = "${message}" };
        var config = NLog.LogManager.Configuration ?? new NLog.Config.LoggingConfiguration();
        config.AddTarget(target);
        var rule = new NLog.Config.LoggingRule("Contensive.RenderProfile", NLog.LogLevel.Info, target);
        config.LoggingRules.Insert(0, rule);
        NLog.LogManager.Configuration = config;
        try {
            using (CPClass cp = new(testAppName, TestHttpContext.createForRoute($"/{pageName}"))) {
                TestPageInfo info = createTestPage(cp, pageName);
                cp.core.profiler = new RenderProfilerController();
                cp.executeRoute($"/{pageName}");
                deleteTestPage(cp, info);
            } // dispose writes the line
            Assert.AreEqual(1, target.Logs.Count);
            string line = target.Logs[0];
            int pos = line.IndexOf("renderProfile,");
            Assert.IsTrue(pos >= 0);
            string json = line.Substring(pos + "renderProfile,".Length);
            var parsed = Newtonsoft.Json.Linq.JObject.Parse(json);
            Assert.AreEqual(1, (int)parsed["v"]);
        } finally {
            config.LoggingRules.Remove(rule);
            NLog.LogManager.Configuration = config;
        }
    }
    //
    //====================================================================================================
    // -- 10.1-8
    [TestMethod]
    public void profiler_nonWebCore_none() {
        using (CPClass cp = new(testAppName)) {
            Assert.IsFalse(cp.core.isWebRequest);
            // if config.json has enableRenderProfiler=true, this asserts the web-only rule directly
            Assert.IsNull(cp.core.profiler);
        }
    }
}
