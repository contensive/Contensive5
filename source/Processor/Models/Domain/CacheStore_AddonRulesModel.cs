
using Contensive.Models.Db;
using Contensive.Processor.Controllers;
using System;
using System.Collections.Generic;
using System.Data;
//
namespace Contensive.Processor.Models.Domain {
    //
    //====================================================================================================
    /// <summary>
    /// Page and template addon dependencies (the Add-on Page Rules and Add-on Template Rules tables) in one cached object.
    /// Replaces two uncached sql subselects, run on every page render, with a dictionary lookup.
    /// Job 20261126-contensive-render-speed, item 4.
    ///
    /// The lists hold addon ids in ascending order, each addon once. They are resolved through core.cacheRuntime.addonCache, which returns
    /// null for an inactive or deleted addon. That matches the original query: active addons, each once (IN), ordered by id.
    /// The rule rows themselves are not filtered by their own active flag, as in the original query.
    /// The cache entry depends on both rule tables, and the addon cache it resolves through depends on the addon tables.
    /// </summary>
    public class CacheStore_AddonRulesModel {
        /// <summary>
        /// templateId to the ascending, distinct list of addon ids that are dependencies of the template
        /// </summary>
        public Dictionary<int, List<int>> templateAddonIdDict { get; set; } = [];
        /// <summary>
        /// pageId to the ascending, distinct list of addon ids that are dependencies of the page
        /// </summary>
        public Dictionary<int, List<int>> pageAddonIdDict { get; set; } = [];
        //
        /// <summary>
        /// above this many rule rows a warning is logged. The store is still built.
        /// </summary>
        private const int warnRowCount = 20000;
        //
        //====================================================================================================
        /// <summary>
        /// return an new empty object. Required for cache read
        /// </summary>
        public CacheStore_AddonRulesModel() { }
        //
        //====================================================================================================
        /// <summary>
        /// construct an instance of the class, reading both rule tables
        /// </summary>
        /// <param name="core"></param>
        public CacheStore_AddonRulesModel(CoreController core) {
            int rowCount = 0;
            rowCount += load(core, $"select templateId as k, addonId as a from {AddonTemplateRuleModel.tableMetadata.tableNameLower} where (templateId is not null) and (addonId is not null) order by templateId, addonId", templateAddonIdDict);
            rowCount += load(core, $"select pageId as k, addonId as a from {AddonPageRuleModel.tableMetadata.tableNameLower} where (pageId is not null) and (addonId is not null) order by pageId, addonId", pageAddonIdDict);
            if (rowCount > warnRowCount) {
                logger.Warn($"{core.logCommonMessage},CacheStore_AddonRulesModel loaded [{rowCount}] rule rows, more than [{warnRowCount}]. The cached object is large.");
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// run a constant (no user input) query and add each distinct addon id to the list for its key. Because of the order by, lists end up ascending.
        /// </summary>
        private static int load(CoreController core, string sql, Dictionary<int, List<int>> dict) {
            int rowCount = 0;
            using (DataTable dt = core.db.executeQuery(sql)) {
                if (dt?.Rows == null) { return 0; }
                foreach (DataRow row in dt.Rows) {
                    rowCount++;
                    int key = GenericController.getInteger(row["k"]);
                    int addonId = GenericController.getInteger(row["a"]);
                    if (!dict.TryGetValue(key, out List<int> list)) {
                        list = [];
                        dict.Add(key, list);
                    }
                    if (list.Count == 0 || list[list.Count - 1] != addonId) { list.Add(addonId); }
                }
            }
            return rowCount;
        }
        //
        //====================================================================================================
        /// <summary>
        /// the active addons that are dependencies of the template, ordered by id. A new list is returned on each call.
        /// </summary>
        public List<AddonModel> getTemplateAddonList(CoreController core, int templateId) => resolve(core, templateAddonIdDict, templateId);
        //
        /// <summary>
        /// the active addons that are dependencies of the page, ordered by id. A new list is returned on each call.
        /// </summary>
        public List<AddonModel> getPageAddonList(CoreController core, int pageId) => resolve(core, pageAddonIdDict, pageId);
        //
        private static List<AddonModel> resolve(CoreController core, Dictionary<int, List<int>> dict, int key) {
            var result = new List<AddonModel>();
            if (!dict.TryGetValue(key, out List<int> addonIdList)) { return result; }
            foreach (int addonId in addonIdList) {
                //
                // -- null if the addon is inactive or deleted
                AddonModel addon = core.cacheRuntime.addonCache.create(addonId);
                if (addon != null) { result.Add(addon); }
            }
            return result;
        }
        //
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    }
}
