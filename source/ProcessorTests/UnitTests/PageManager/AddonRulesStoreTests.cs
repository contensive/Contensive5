
using Contensive.Models.Db;
using Contensive.Processor;
using Contensive.Processor.Controllers;
using Contensive.Processor.Models;
using Contensive.Processor.Models.Domain;
using Contensive.Processor.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using static Contensive.Processor.Tests.TestConstants;

namespace Contensive.Processor.Tests.UnitTests.PageManager;
//
//====================================================================================================
/// <summary>
/// Job 20261126-contensive-render-speed, item 4. CacheStore_AddonRulesModel parity with the original page/template dependency queries.
/// Each test that reads the store uses a new CPClass (a new core), like a new request. These tests need the test app (sql server) and the cache enabled.
/// </summary>
[TestClass()]
public class AddonRulesStoreTests {
    //
    private static bool cacheEnabled(CPClass cp) => cp.ServerConfig.enableLocalFileCache || cp.ServerConfig.enableLocalMemoryCache || cp.ServerConfig.enableRemoteCache;
    //
    private static AddonModel createAddon(CPClass cp, string namePrefix, bool testAddon = false) {
        AddonModel addon = DbBaseModel.addDefault<AddonModel>(cp);
        addon.name = $"{namePrefix}{cp.Utils.GetRandomInteger()}";
        if (testAddon) {
            addon.dotNetClass = "Contensive.Processor.Addons.TestAddon";
            addon.collectionId = DbBaseModel.create<AddonCollectionModel>(cp, Contensive.Processor.Constants.baseCollectionGuid).id;
        }
        addon.save(cp);
        return addon;
    }
    //
    private static void addTemplateRule(CPClass cp, int templateId, int addonId, bool active = true) {
        AddonTemplateRuleModel rule = DbBaseModel.addDefault<AddonTemplateRuleModel>(cp);
        rule.templateId = templateId;
        rule.addonId = addonId;
        rule.active = active;
        rule.save(cp);
    }
    //
    private static void addPageRule(CPClass cp, int pageId, int addonId, bool active = true) {
        AddonPageRuleModel rule = DbBaseModel.addDefault<AddonPageRuleModel>(cp);
        rule.pageId = pageId;
        rule.addonId = addonId;
        rule.active = active;
        rule.save(cp);
    }
    //
    private static List<int> ids(IEnumerable<AddonModel> list) => list.Select(a => a.id).ToList();
    //
    private sealed class Data {
        public PageTemplateModel template;
        public PageContentModel page;
        public AddonModel x, y, z;
    }
    //
    /// <summary>
    /// a template, a page and three addons (ids x less than y less than z). Rules are created for z, x, y, and a duplicate for x, on both the template and the page.
    /// </summary>
    private static Data createData(CPClass cp) {
        var data = new Data {
            template = DbBaseModel.addDefault<PageTemplateModel>(cp),
            page = DbBaseModel.addDefault<PageContentModel>(cp)
        };
        data.template.name = $"rulesTemplate{cp.Utils.GetRandomInteger()}";
        data.template.save(cp);
        data.page.name = $"rulesPage{cp.Utils.GetRandomInteger()}";
        data.page.save(cp);
        data.x = createAddon(cp, "rulesX");
        data.y = createAddon(cp, "rulesY");
        data.z = createAddon(cp, "rulesZ");
        foreach (AddonModel addon in new[] { data.z, data.x, data.y, data.x }) {
            addTemplateRule(cp, data.template.id, addon.id);
            addPageRule(cp, data.page.id, addon.id);
        }
        return data;
    }
    //
    private static void cleanup(CPClass cp, Data data) {
        DbBaseModel.deleteRows<AddonTemplateRuleModel>(cp, $"templateId={data.template.id}");
        DbBaseModel.deleteRows<AddonPageRuleModel>(cp, $"pageId={data.page.id}");
        DbBaseModel.delete<AddonModel>(cp, data.x.id);
        DbBaseModel.delete<AddonModel>(cp, data.y.id);
        DbBaseModel.delete<AddonModel>(cp, data.z.id);
        DbBaseModel.delete<PageTemplateModel>(cp, data.template.id);
        DbBaseModel.delete<PageContentModel>(cp, data.page.id);
    }
    //
    //====================================================================================================
    // -- 10.4-1
    [TestMethod]
    public void rules_template_parity() {
        using (CPClass cp = new(testAppName)) {
            if (!cacheEnabled(cp)) { Assert.Inconclusive("cache not enabled"); }
            Data data = createData(cp);
            try {
                using (CPClass cp2 = new(testAppName)) {
                    List<int> fromStore = ids(cp2.core.cacheRuntime.addonRulesCache.getTemplateAddonList(cp2.core, data.template.id));
                    List<int> fromSql = ids(AddonModel.createList_templateDependencies(cp2, data.template.id));
                    CollectionAssert.AreEqual(new List<int> { data.x.id, data.y.id, data.z.id }, fromSql, "the original query");
                    CollectionAssert.AreEqual(fromSql, fromStore);
                }
            } finally { cleanup(cp, data); }
        }
    }
    //
    // -- 10.4-2
    [TestMethod]
    public void rules_page_parity() {
        using (CPClass cp = new(testAppName)) {
            if (!cacheEnabled(cp)) { Assert.Inconclusive("cache not enabled"); }
            Data data = createData(cp);
            try {
                using (CPClass cp2 = new(testAppName)) {
                    List<int> fromStore = ids(cp2.core.cacheRuntime.addonRulesCache.getPageAddonList(cp2.core, data.page.id));
                    List<int> fromSql = ids(AddonModel.createList_pageDependencies(cp2, data.page.id));
                    CollectionAssert.AreEqual(new List<int> { data.x.id, data.y.id, data.z.id }, fromSql, "the original query");
                    CollectionAssert.AreEqual(fromSql, fromStore);
                }
            } finally { cleanup(cp, data); }
        }
    }
    //
    // -- 10.4-3
    [TestMethod]
    public void rules_inactiveAddonExcluded() {
        using (CPClass cp = new(testAppName)) {
            if (!cacheEnabled(cp)) { Assert.Inconclusive("cache not enabled"); }
            Data data = createData(cp);
            try {
                data.y.active = false;
                data.y.save(cp);
                using (CPClass cp2 = new(testAppName)) {
                    List<int> expected = [data.x.id, data.z.id];
                    CollectionAssert.AreEqual(expected, ids(AddonModel.createList_templateDependencies(cp2, data.template.id)));
                    CollectionAssert.AreEqual(expected, ids(cp2.core.cacheRuntime.addonRulesCache.getTemplateAddonList(cp2.core, data.template.id)));
                    CollectionAssert.AreEqual(expected, ids(AddonModel.createList_pageDependencies(cp2, data.page.id)));
                    CollectionAssert.AreEqual(expected, ids(cp2.core.cacheRuntime.addonRulesCache.getPageAddonList(cp2.core, data.page.id)));
                }
            } finally { cleanup(cp, data); }
        }
    }
    //
    // -- 10.4-4 the rule row's own active flag is not tested by the original query, and not by the store
    [TestMethod]
    public void rules_inactiveRuleIncluded() {
        using (CPClass cp = new(testAppName)) {
            if (!cacheEnabled(cp)) { Assert.Inconclusive("cache not enabled"); }
            Data data = createData(cp);
            try {
                AddonModel w = createAddon(cp, "rulesW");
                addTemplateRule(cp, data.template.id, w.id, false);
                addPageRule(cp, data.page.id, w.id, false);
                using (CPClass cp2 = new(testAppName)) {
                    Assert.IsTrue(ids(AddonModel.createList_templateDependencies(cp2, data.template.id)).Contains(w.id), "original query includes it");
                    Assert.IsTrue(ids(cp2.core.cacheRuntime.addonRulesCache.getTemplateAddonList(cp2.core, data.template.id)).Contains(w.id));
                    Assert.IsTrue(ids(AddonModel.createList_pageDependencies(cp2, data.page.id)).Contains(w.id));
                    Assert.IsTrue(ids(cp2.core.cacheRuntime.addonRulesCache.getPageAddonList(cp2.core, data.page.id)).Contains(w.id));
                }
                DbBaseModel.delete<AddonModel>(cp, w.id);
            } finally { cleanup(cp, data); }
        }
    }
    //
    // -- 10.4-5
    [TestMethod]
    public void rules_invalidate_onModelSave() {
        using (CPClass cp = new(testAppName)) {
            if (!cacheEnabled(cp)) { Assert.Inconclusive("cache not enabled"); }
            Data data = createData(cp);
            try {
                // -- core 1 builds and caches the store
                using (CPClass cp1 = new(testAppName)) {
                    Assert.AreEqual(3, cp1.core.cacheRuntime.addonRulesCache.getPageAddonList(cp1.core, data.page.id).Count);
                }
                // -- add a rule through the model
                AddonModel w = createAddon(cp, "rulesW");
                addPageRule(cp, data.page.id, w.id);
                // -- core 2 sees it
                using (CPClass cp2 = new(testAppName)) {
                    Assert.IsTrue(ids(cp2.core.cacheRuntime.addonRulesCache.getPageAddonList(cp2.core, data.page.id)).Contains(w.id));
                }
                DbBaseModel.delete<AddonModel>(cp, w.id);
            } finally { cleanup(cp, data); }
        }
    }
    //
    // -- 10.4-6
    [TestMethod]
    public void rules_invalidate_onDelete() {
        using (CPClass cp = new(testAppName)) {
            if (!cacheEnabled(cp)) { Assert.Inconclusive("cache not enabled"); }
            Data data = createData(cp);
            try {
                AddonModel w = createAddon(cp, "rulesW");
                AddonPageRuleModel rule = DbBaseModel.addDefault<AddonPageRuleModel>(cp);
                rule.pageId = data.page.id;
                rule.addonId = w.id;
                rule.save(cp);
                using (CPClass cp1 = new(testAppName)) {
                    Assert.IsTrue(ids(cp1.core.cacheRuntime.addonRulesCache.getPageAddonList(cp1.core, data.page.id)).Contains(w.id));
                }
                DbBaseModel.delete<AddonPageRuleModel>(cp, rule.id);
                using (CPClass cp2 = new(testAppName)) {
                    Assert.IsFalse(ids(cp2.core.cacheRuntime.addonRulesCache.getPageAddonList(cp2.core, data.page.id)).Contains(w.id));
                }
                DbBaseModel.delete<AddonModel>(cp, w.id);
            } finally { cleanup(cp, data); }
        }
    }
    //
    // -- 10.4-7 the admin many-to-many checklist (page field includedaddons -> Add-on Page Rules)
    [TestMethod]
    public void rules_invalidate_onCheckList() {
        using (CPClass cp = new(testAppName)) {
            if (!cacheEnabled(cp)) { Assert.Inconclusive("cache not enabled"); }
            Data data = createData(cp);
            try {
                AddonModel w = createAddon(cp, "rulesW");
                using (CPClass cp1 = new(testAppName)) {
                    Assert.IsFalse(ids(cp1.core.cacheRuntime.addonRulesCache.getPageAddonList(cp1.core, data.page.id)).Contains(w.id));
                }
                // -- request properties as the admin edit form posts them: one row, addon w checked
                const string tag = "includedaddons";
                cp.Doc.SetProperty($"{tag}.RowCount", 1);
                cp.Doc.SetProperty($"{tag}.0.ID", w.id);
                cp.Doc.SetProperty($"{tag}.0", true);
                cp.core.html.processCheckList(tag, PageContentModel.tableMetadata.contentName, data.page.id, "Add-ons", "Add-on Page Rules", "pageid", "addonid");
                using (CPClass cp2 = new(testAppName)) {
                    Assert.IsTrue(ids(cp2.core.cacheRuntime.addonRulesCache.getPageAddonList(cp2.core, data.page.id)).Contains(w.id));
                }
                DbBaseModel.deleteRows<AddonPageRuleModel>(cp, $"addonId={w.id}");
                DbBaseModel.delete<AddonModel>(cp, w.id);
            } finally { cleanup(cp, data); }
        }
    }
    //
    // -- 10.4-8 a page with no rules still gets a cached store: core 2 does not rebuild it
    [TestMethod]
    public void rules_emptyStore_cached() {
        using (CPClass cp = new(testAppName)) {
            if (!cacheEnabled(cp)) { Assert.Inconclusive("cache not enabled"); }
            using (CPClass cp1 = new(testAppName)) {
                Assert.AreEqual(0, cp1.core.cacheRuntime.addonRulesCache.getPageAddonList(cp1.core, -12345).Count);
            }
            using (CPClass cp2 = new(testAppName)) {
                var profiler = new RenderProfilerController();
                cp2.core.profiler = profiler;
                Assert.AreEqual(0, cp2.core.cacheRuntime.addonRulesCache.getPageAddonList(cp2.core, -12345).Count);
                Assert.IsFalse(profiler.rebuilds.Contains("addonRules"), "second core takes the store from cache");
            }
        }
    }
    //
    // -- 10.4-9 and 10.4-10 render a page with a page dependency, with the store (default) and with the kill switch
    [TestMethod]
    public void rules_killSwitch_and_render_pageDependency_executes() {
        string pageName = new Random().Next().ToString();
        string route = $"/{pageName}";
        AddonModel dependencyAddon = null;
        PageContentModel testPage = null;
        string marker;
        using (CPClass cp = new(testAppName, TestHttpContext.createForRoute(route))) {
            if (!cacheEnabled(cp)) { Assert.Inconclusive("cache not enabled"); }
            cp.Site.SetProperty("ALLOW HTML MINIFY", false);
            cp.core.siteProperties.anonymousUserResponseID = 0;
            marker = cp.Utils.GetRandomInteger().ToString();
            dependencyAddon = createAddon(cp, "rulesDependency", true);
            testPage = DbBaseModel.addDefault<PageContentModel>(cp);
            testPage.name = pageName;
            testPage.save(cp);
            LinkAliasModel linkAlias = DbBaseModel.addDefault<LinkAliasModel>(cp);
            linkAlias.name = pageName;
            linkAlias.pageId = testPage.id;
            linkAlias.save(cp);
            addPageRule(cp, testPage.id, dependencyAddon.id);
            cp.core.routeMapRebuild();
        }
        try {
            long sqlWithStore;
            long sqlWithSwitch;
            // -- warm up the caches, then measure with the store (default)
            using (CPClass cp = new(testAppName, TestHttpContext.createForRoute(route))) {
                cp.Doc.SetProperty("test-in", marker);
                cp.executeRoute(route);
            }
            using (CPClass cp = new(testAppName, TestHttpContext.createForRoute(route))) {
                Assert.IsFalse(cp.core.serverConfig.disableAddonRulesCache, "default is the store on");
                var profiler = new RenderProfilerController();
                cp.core.profiler = profiler;
                cp.Doc.SetProperty("test-in", marker);
                string doc = cp.executeRoute(route);
                Assert.IsTrue(doc.Contains(marker), "10.4-10 the page dependency executes");
                sqlWithStore = profiler.sqlCount;
            }
            // -- kill switch, the original sql path
            using (CPClass cp = new(testAppName, TestHttpContext.createForRoute(route))) {
                cp.core.serverConfig.disableAddonRulesCache = true;
                var profiler = new RenderProfilerController();
                cp.core.profiler = profiler;
                cp.Doc.SetProperty("test-in", marker);
                string doc = cp.executeRoute(route);
                Assert.IsTrue(doc.Contains(marker), "same output with the kill switch");
                sqlWithSwitch = profiler.sqlCount;
            }
            Assert.AreEqual(sqlWithStore + 2, sqlWithSwitch, "the kill switch runs two more sql statements per render");
        } finally {
            using (CPClass cp = new(testAppName)) {
                DbBaseModel.deleteRows<AddonPageRuleModel>(cp, $"pageId={testPage.id}");
                DbBaseModel.delete<AddonModel>(cp, dependencyAddon.id);
                DbBaseModel.delete<PageContentModel>(cp, testPage.id);
            }
        }
    }
}
