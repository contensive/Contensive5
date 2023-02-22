using Contensive.Models.Db;
using Contensive.Processor.Models.Domain;
using System.Collections.Generic;

namespace Contensive.Processor.Controllers {
    //
    /// <summary>
    /// object to organize all the in-memory non-persistent cache in one place
    /// </summary>
    public class CacheStoreController {
        // 
        public CacheStoreController(CoreController core) {
            this.core = core;
        }
        private readonly CoreController core;
        //
        //===================================================================================================
        /// <summary>
        /// List of datasources. The default datasourse is the first entry, and is populated from the initialization configuration. Additional datasources come from the datasources content in the primary datasourse.
        /// </summary>
        public Dictionary<string, DataSourceModel> dataSourceDictionary {
            get {
                if (_dataSources == null) {
                    _dataSources = DataSourceModel.getNameDict(core.cpParent);
                }
                return _dataSources;
            }
        }
        private Dictionary<string, DataSourceModel> _dataSources;
        //
        //===================================================================================================
        /// <summary>
        /// provide an addon cache object lazy populated from the Domain.addonCacheModel. This object provides an
        /// interface to lookup read addon data and common lists
        /// </summary>
        public AddonCacheModel addonCache {
            get {
                if (_addonCache == null) {
                    _addonCache = core.cache.getObject<AddonCacheModel>(cacheName_addonCache);
                    if (_addonCache == null || _addonCache.isEmpty) {
                        _addonCache = new AddonCacheModel(core);
                        core.cache.storeObject(cacheName_addonCache, _addonCache);
                    }
                }
                return _addonCache;
            }
        }
        private AddonCacheModel _addonCache;
        internal const string cacheName_addonCache = "addonCache";
        //
        /// <summary>
        /// method to clear the core instance of routeMap. Explained in routeMap.
        /// </summary>
        public void addonCacheClear() {
            core.cache.invalidate(cacheName_addonCache);
            _addonCache = null;
        }
        //
        //===================================================================================================
        /// <summary>
        /// Dictionary of cdef, index by name
        /// </summary>
        internal Dictionary<string, Models.Domain.ContentMetadataModel> metaDataDictionary { get; set; }
        //
        //===================================================================================================
        /// <summary>
        /// Dictionary of tableschema, index by name
        /// </summary>
        internal Dictionary<string, Models.Domain.TableSchemaModel> tableSchemaDictionary { get; set; }
        //
        //===================================================================================================
        /// <summary>
        /// lookup contentId by contentName
        /// </summary>
        internal Dictionary<string, int> contentNameIdDictionary {
            get {
                if (_contentNameIdDictionary == null) {
                    _contentNameIdDictionary = new Dictionary<string, int>();
                }
                return _contentNameIdDictionary;
            }
        }
        internal Dictionary<string, int> _contentNameIdDictionary;
        //
        //====================================================================================================
        /// <summary>
        /// Clear all data from the metaData current instance. Next request will load from cache.
        /// </summary>
        public void clearMetaData() {
            if (metaDataDictionary != null) {
                metaDataDictionary.Clear();
            }
            tableSchemaDictionary.Clear();
            contentNameIdDictionaryClear();
        }
        //
        //====================================================================================================
        //
        internal void contentNameIdDictionaryClear() {
            _contentNameIdDictionary = null;
        }
        //
        //===================================================================================================
        // todo move to class
        /// <summary>
        /// A dictionary of addon collection.namespace.class and the file assembly where it was found. Built during execution, stored in cache
        /// </summary>
        public Dictionary<string, AssemblyFileDetails> assemblyList_AddonsFound {
            get {
                if (_assemblyFileDict != null) { return _assemblyFileDict; }
                //
                // -- if remote-mode collections.xml file is updated, invalidate cache
                if (!core.privateFiles.localFileStale(AddonController.getPrivateFilesAddonPath() + "Collections.xml")) {
                    _assemblyFileDict = core.cache.getObject<Dictionary<string, AssemblyFileDetails>>(AssemblyFileDictCacheName);
                }
                if (_assemblyFileDict == null) {
                    _assemblyFileDict = new Dictionary<string, AssemblyFileDetails>();
                }
                return _assemblyFileDict;
            }
        }
        //
        //===================================================================================================
        /// <summary>
        /// list of assemblies found to be addons. used to speed up execution
        /// </summary>
        public void assemblyList_AddonsFound_save() {
            var dependentKeyList = new List<CacheKeyHashClass> {
                core.cache.createTableDependencyKeyHash(AddonModel.tableMetadata.tableNameLower),
                core.cache.createTableDependencyKeyHash(AddonCollectionModel.tableMetadata.tableNameLower)
            };
            core.cache.storeObject(AssemblyFileDictCacheName, _assemblyFileDict, dependentKeyList);
        }
        private Dictionary<string, AssemblyFileDetails> _assemblyFileDict;
        private const string AssemblyFileDictCacheName = "assemblyFileDict";
        //
        //===================================================================================================
        /// <summary>
        /// domains configured for this app. keys are lowercase
        /// </summary>
        public Dictionary<string, DomainModel> domainDictionary { get; set; }
        //
        public class LayoutDict {
            public Dictionary<int, LayoutModel> layoutIdDict { get; set; } = new();
            public Dictionary<string, LayoutModel> layoutGuidDict { get; set; } = new();
            public Dictionary<string, LayoutModel> layoutNameDict { get; set; } = new();
        }
        //
        //====================================================================================================
        //
        /// <summary>
        /// load and return the layout dictionaries
        /// </summary>
        /// <returns></returns>
        private LayoutDict getLayoutDict() {
            string cacheKey = "layoutDict";
            LayoutDict layoutDict = core.cache.getObject<LayoutDict>(cacheKey);
            if (layoutDict != null) { return layoutDict; }
            //
            // -- load from db
            layoutDict = new LayoutDict();
            foreach (LayoutModel linkAlias in DbBaseModel.createList<LayoutModel>(core.cpParent)) {
                if (!layoutDict.layoutIdDict.ContainsKey(linkAlias.id)) {
                    layoutDict.layoutIdDict.Add(linkAlias.id, linkAlias);
                }
                if (!string.IsNullOrEmpty(linkAlias.ccguid) && !layoutDict.layoutGuidDict.ContainsKey(linkAlias.ccguid)) {
                    layoutDict.layoutGuidDict.Add(linkAlias.ccguid, linkAlias);
                }
                if (!string.IsNullOrEmpty(linkAlias.name) && !layoutDict.layoutNameDict.ContainsKey(linkAlias.name)) {
                    layoutDict.layoutNameDict.Add(linkAlias.name, linkAlias);
                }
            }
            //
            // -- update cache
            var dependentKeyHastList = new List<CacheKeyHashClass>() { core.cache.createTableDependencyKeyHash(LayoutModel.tableMetadata.tableNameLower) };
            core.cache.storeObject(cacheKey, layoutDict, dependentKeyHastList);
            return layoutDict;
        }
        //
        //====================================================================================================
        //
        /// <summary>
        /// cache dictionary of layout by id
        /// </summary>
        public Dictionary<int, LayoutModel> layoutIdDict {
            get {
                if (layoutIdDict_Local != null) { return layoutIdDict_Local; }
                LayoutDict layout = getLayoutDict();
                layoutIdDict_Local = layout.layoutIdDict;
                layoutGuidDict_Local = layout.layoutGuidDict;
                layoutNameDict_Local = layout.layoutNameDict;
                return layoutIdDict_Local;
            }
        }
        private Dictionary<int, LayoutModel> layoutIdDict_Local;
        //
        //====================================================================================================
        //
        /// <summary>
        /// cache dictionary of layout by name
        /// </summary>
        public Dictionary<string, LayoutModel> layoutNameDict {
            get {
                if (layoutNameDict_Local != null) { return layoutNameDict_Local; }
                LayoutDict layout = getLayoutDict();
                layoutIdDict_Local = layout.layoutIdDict;
                layoutGuidDict_Local = layout.layoutGuidDict;
                layoutNameDict_Local = layout.layoutNameDict;
                return layoutNameDict_Local;
            }
        }
        private Dictionary<string, LayoutModel> layoutNameDict_Local;
        //
        //====================================================================================================
        //
        /// <summary>
        /// cache dictionary of layout by id
        /// </summary>
        public Dictionary<string, LayoutModel> layoutGuidDict {
            get {
                if (layoutGuidDict_Local != null) { return layoutGuidDict_Local; }
                LayoutDict layout = getLayoutDict();
                layoutIdDict_Local = layout.layoutIdDict;
                layoutGuidDict_Local = layout.layoutGuidDict;
                layoutNameDict_Local = layout.layoutNameDict;
                return layoutGuidDict_Local;
            }
        }
        private Dictionary<string, LayoutModel> layoutGuidDict_Local;
        ////
        ////====================================================================================================
        ////
        ///// <summary>
        ///// Link alias cache dictionary. loads when initializing routemap, available for getPageLink() methods
        ///// </summary>
        //public Dictionary<string, LinkAliasModel> linkAliasPageDict {
        //    get {
        //        if (linkAliasPageDict_Local != null) { return linkAliasPageDict_Local; }
        //        //
        //        string cacheKey = "linkAliasPageDict";
        //        linkAliasPageDict_Local = core.cache.getObject<Dictionary<string, LinkAliasModel>>(cacheKey);
        //        if (linkAliasPageDict_Local != null) { return linkAliasPageDict_Local; }
        //        //
        //        // -- order by "pageid,queryStringSuffix,id desc" for routemap
        //        linkAliasPageDict_Local = new Dictionary<string, LinkAliasModel>();
        //        foreach (LinkAliasModel linkAlias in DbBaseModel.createList<LinkAliasModel>(core.cpParent, "name Is Not null", "pageid,queryStringSuffix,id desc")) {
        //            string key = $"{linkAlias.pageId}.{linkAlias.queryStringSuffix}";
        //            if (linkAliasPageDict_Local.ContainsKey(key)) { continue; }
        //            linkAliasPageDict_Local.Add(key, linkAlias);
        //        }
        //        var dependentKeyHastList = new List<CacheKeyHashClass>() { core.cache.createTableDependencyKeyHash(LinkAliasModel.tableMetadata.tableNameLower) };
        //        core.cache.storeObject(cacheKey, linkAliasPageDict_Local, dependentKeyHastList);
        //        return linkAliasPageDict_Local;
        //    }
        //}
        //private Dictionary<string, LinkAliasModel> linkAliasPageDict_Local;
        //
        //
        //
        //
        //
        //
        //

        //
        //===================================================================================================
        //
        public class LinkAliasStoreModel {
            public Dictionary<int, LinkAliasModel> linkAliasIdDict { get; set; } = new();
            public Dictionary<string, LinkAliasModel> linkAliasGuidDict { get; set; } = new();
            public Dictionary<string, LinkAliasModel> linkAliasNameDict { get; set; } = new();
            public Dictionary<string, LinkAliasModel> linkAliasKeyDict { get; set; } = new();
        }
        //
        //====================================================================================================
        //
        /// <summary>
        /// load and return the LinkAlias dictionaries
        /// </summary>
        /// <returns></returns>
        private LinkAliasStoreModel getLinkAliasStore() {
            string cacheKey = "LinkAliasStore";
            LinkAliasStoreModel linkAliasStore = core.cache.getObject<LinkAliasStoreModel>(cacheKey);
            if (linkAliasStore != null) { return linkAliasStore; }
            //
            // -- load from db
            linkAliasStore = new LinkAliasStoreModel();
            foreach (LinkAliasModel linkAlias in DbBaseModel.createList<LinkAliasModel>(core.cpParent)) {
                if (!linkAliasStore.linkAliasIdDict.ContainsKey(linkAlias.id)) {
                    linkAliasStore.linkAliasIdDict.Add(linkAlias.id, linkAlias);
                }
                if (!string.IsNullOrEmpty(linkAlias.ccguid) && !linkAliasStore.linkAliasGuidDict.ContainsKey(linkAlias.ccguid)) {
                    linkAliasStore.linkAliasGuidDict.Add(linkAlias.ccguid, linkAlias);
                }
                if (!string.IsNullOrEmpty(linkAlias.name) && !linkAliasStore.linkAliasNameDict.ContainsKey(linkAlias.name)) {
                    linkAliasStore.linkAliasNameDict.Add(linkAlias.name, linkAlias);
                }
                string linkAliasKey = $"{linkAlias.pageId}.{linkAlias.queryStringSuffix}";
                if (!string.IsNullOrEmpty(linkAliasKey) && !linkAliasStore.linkAliasKeyDict.ContainsKey(linkAliasKey)) {
                    linkAliasStore.linkAliasKeyDict.Add(linkAliasKey, linkAlias);
                }
            }
            //
            // -- update cache
            var dependentKeyHastList = new List<CacheKeyHashClass>() { core.cache.createTableDependencyKeyHash(LinkAliasModel.tableMetadata.tableNameLower) };
            core.cache.storeObject(cacheKey, linkAliasStore, dependentKeyHastList);
            return linkAliasStore;
        }
        //
        //====================================================================================================
        //
        /// <summary>
        /// cache dictionary of LinkAlias by id
        /// </summary>
        public Dictionary<int, LinkAliasModel> linkAliasIdDict {
            get {
                if (linkAliasIdDict_Local != null) { return linkAliasIdDict_Local; }
                LinkAliasStoreModel LinkAlias = getLinkAliasStore();
                linkAliasIdDict_Local = LinkAlias.linkAliasIdDict;
                linkAliasGuidDict_Local = LinkAlias.linkAliasGuidDict;
                linkAliasNameDict_Local = LinkAlias.linkAliasNameDict;
                return linkAliasIdDict_Local;
            }
        }
        private Dictionary<int, LinkAliasModel> linkAliasIdDict_Local;
        //
        //====================================================================================================
        //
        /// <summary>
        /// cache dictionary of LinkAlias by name
        /// </summary>
        public Dictionary<string, LinkAliasModel> linkAliasNameDict {
            get {
                if (linkAliasNameDict_Local != null) { return linkAliasNameDict_Local; }
                LinkAliasStoreModel LinkAlias = getLinkAliasStore();
                linkAliasIdDict_Local = LinkAlias.linkAliasIdDict;
                linkAliasGuidDict_Local = LinkAlias.linkAliasGuidDict;
                linkAliasNameDict_Local = LinkAlias.linkAliasNameDict;
                return linkAliasNameDict_Local;
            }
        }
        private Dictionary<string, LinkAliasModel> linkAliasNameDict_Local;
        //
        //====================================================================================================
        //
        /// <summary>
        /// cache dictionary of LinkAlias by id
        /// </summary>
        public Dictionary<string, LinkAliasModel> linkAliasGuidDict {
            get {
                if (linkAliasGuidDict_Local != null) { return linkAliasGuidDict_Local; }
                LinkAliasStoreModel LinkAlias = getLinkAliasStore();
                linkAliasIdDict_Local = LinkAlias.linkAliasIdDict;
                linkAliasGuidDict_Local = LinkAlias.linkAliasGuidDict;
                linkAliasNameDict_Local = LinkAlias.linkAliasNameDict;
                return linkAliasGuidDict_Local;
            }
        }
        private Dictionary<string, LinkAliasModel> linkAliasGuidDict_Local;
        //
        //====================================================================================================
        //
        /// <summary>
        /// cache dictionary of LinkAlias by key ($"{pageId}.{queryStringSuffix}")
        /// </summary>
        public Dictionary<string, LinkAliasModel> linkAliasKeyDict {
            get {
                if (linkAliasKeyDict_Local != null) { return linkAliasKeyDict_Local; }
                LinkAliasStoreModel LinkAlias = getLinkAliasStore();
                linkAliasIdDict_Local = LinkAlias.linkAliasIdDict;
                linkAliasKeyDict_Local = LinkAlias.linkAliasKeyDict;
                linkAliasNameDict_Local = LinkAlias.linkAliasNameDict;
                return linkAliasKeyDict_Local;
            }
        }
        private Dictionary<string, LinkAliasModel> linkAliasKeyDict_Local;
    }
}