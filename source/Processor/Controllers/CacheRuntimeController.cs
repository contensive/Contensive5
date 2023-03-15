using Contensive.Models.Db;
using Contensive.Processor.Models.Domain;
using System;
using System.Collections.Generic;

namespace Contensive.Processor.Controllers {
    //
    /// <summary>
    /// object to organize all the in-memory non-persistent cache in one place
    /// </summary>
    public class CacheRuntimeController {
        // 
        public CacheRuntimeController(CoreController core) {
            this.core = core;
        }
        private readonly CoreController core;
        //
        //====================================================================================================
        /// <summary>
        /// clear the runtime cache for this table
        /// </summary>
        /// <param name="tableName"></param>
        public void clearTable(string tableName) {
            switch (tableName.ToLowerInvariant()) {
                case "ccaggregatefunctions": {
                        clearAddon();
                        break;
                    }
                case "cclinkaliases": {
                        clearLinkAlias();
                        break;
                    }
                case "cclayouts": {
                        clearLayout();
                        break;
                    }
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Clear all data from the metaData current instance. Next request will load from cache.
        /// </summary>
        public void clear() {
            clearLinkAlias();
            clearLayout();
            clearAddon();
            //
            if (_dataSourceDictionary != null) { _dataSourceDictionary = null; }
            metaDataDictionary.Clear();
            tableSchemaDictionary.Clear();
            contentNameIdDictionary_Clear();
            assemblyFileDict.Clear();
            domainDictionary = new();
            content_Clear();
            assemblyFileDict_Clear();
        }
        //
        //===================================================================================================
        /// <summary>
        /// List of datasources. The default datasourse is the first entry, and is populated from the initialization configuration. Additional datasources come from the datasources content in the primary datasourse.
        /// </summary>
        public Dictionary<string, DataSourceModel> dataSourceDictionary {
            get {
                if (_dataSourceDictionary == null) {
                    _dataSourceDictionary = DataSourceModel.getNameDict(core.cpParent);
                }
                return _dataSourceDictionary;
            }
        }
        private Dictionary<string, DataSourceModel> _dataSourceDictionary;
        //
        //===================================================================================================
        /// <summary>
        /// provide an addon cache object lazy populated from the Domain.addonCacheModel. This object provides an
        /// interface to lookup read addon data and common lists
        /// </summary>
        public CacheStore_AddonModel addonCache {
            get {
                //CacheKeyHashClass keyHash = core.cache.createKeyHash(cacheName_addonCache);
                //if (core.cache.tryGetCacheDocument<CacheStore_AddonModel>(keyHash, out CacheDocumentClass cacheDocument)) {
                //    // -- cache miss
                //    _addonCache = new CacheStore_AddonModel(core);
                //    List<CacheKeyHashClass> dependencyList = new List<CacheKeyHashClass> {
                //        core.cache.createTableDependencyKeyHash(AddonModel.tableMetadata.tableNameLower),
                //        core.cache.createTableDependencyKeyHash(AddonIncludeRuleModel.tableMetadata.tableNameLower)
                //    };
                //    core.cache.storeObject(cacheName_addonCache, _addonCache, dependencyList);
                //    return _addonCache;
                //}
                if (_addonCache != null) { return _addonCache; }
                //
                // -- populate local version from cache
                _addonCache = core.cache.getObject<CacheStore_AddonModel>(cacheName_addonCache);
                if (_addonCache == null || _addonCache.isEmpty) {
                    // -- cache empty (should not be possible since cache miss was covered)
                    _addonCache = new CacheStore_AddonModel(core);
                    List<CacheKeyHashClass> dependencyList = new List<CacheKeyHashClass> {
                            core.cache.createTableDependencyKeyHash(AddonModel.tableMetadata.tableNameLower),
                            core.cache.createTableDependencyKeyHash(AddonIncludeRuleModel.tableMetadata.tableNameLower)
                        };
                    core.cache.storeObject(cacheName_addonCache, _addonCache, dependencyList);
                }
                return _addonCache;
            }
        }
        private CacheStore_AddonModel _addonCache;
        internal const string cacheName_addonCache = "addonCache";
        //
        /// <summary>
        /// method to clear the core instance of routeMap. Explained in routeMap.
        /// </summary>
        public void clearAddon() {
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
        private void contentNameIdDictionary_Clear() {
            _contentNameIdDictionary = null;
        }
        //
        //===================================================================================================
        /// <summary>
        /// A dictionary of addon collection.namespace.class and the file assembly where it was found. Built during execution, stored in cache
        /// </summary>
        public Dictionary<string, AssemblyFileDetails> assemblyFileDict {
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
        private Dictionary<string, AssemblyFileDetails> _assemblyFileDict;
        private const string AssemblyFileDictCacheName = "assemblyFileDict";
        //
        private void assemblyFileDict_Clear() {
            _assemblyFileDict = null;
            core.cache.invalidate(AssemblyFileDictCacheName);
        }
        //
        /// <summary>
        /// update cache for assemblyList_AddonsFound
        /// </summary>
        public void assemblyFileDict_save() {
            var dependentKeyList = new List<CacheKeyHashClass> {
                core.cache.createTableDependencyKeyHash(AddonModel.tableMetadata.tableNameLower),
                core.cache.createTableDependencyKeyHash(AddonCollectionModel.tableMetadata.tableNameLower)
            };
            core.cache.storeObject(AssemblyFileDictCacheName, _assemblyFileDict, dependentKeyList);
        }
        //
        //===================================================================================================
        /// <summary>
        /// domains configured for this app. keys are lowercase
        /// </summary>
        public Dictionary<string, DomainModel> domainDictionary { get; set; }
        //
        //
        //===================================================================================================
        /// <summary>
        /// layoutsfor this app. keys are lowercase
        /// </summary>
        public class LayoutDict {
            public Dictionary<int, LayoutModel> layoutIdDict { get; set; } = new();
            public Dictionary<string, LayoutModel> layoutGuidDict { get; set; } = new(StringComparer.InvariantCultureIgnoreCase);
            public Dictionary<string, LayoutModel> layoutNameDict { get; set; } = new(StringComparer.InvariantCultureIgnoreCase);
        }
        //
        /// <summary>
        /// clear layouts
        /// </summary>
        public void clearLayout() {
            layoutIdDict_Local = null;
            layoutGuidDict_Local = null;
            layoutNameDict_Local = null;
        }
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
            foreach (LayoutModel record in DbBaseModel.createList<LayoutModel>(core.cpParent)) {
                if (!layoutDict.layoutIdDict.ContainsKey(record.id)) {
                    layoutDict.layoutIdDict.Add(record.id, record);
                }
                if (!string.IsNullOrEmpty(record.ccguid) && !layoutDict.layoutGuidDict.ContainsKey(record.ccguid)) {
                    layoutDict.layoutGuidDict.Add(record.ccguid, record);
                }
                if (!string.IsNullOrEmpty(record.name) && !layoutDict.layoutNameDict.ContainsKey(record.name)) {
                    layoutDict.layoutNameDict.Add(record.name, record);
                }
            }
            //
            // -- update cache
            var dependentKeyHastList = new List<CacheKeyHashClass>() { core.cache.createTableDependencyKeyHash(LayoutModel.tableMetadata.tableNameLower) };
            core.cache.storeObject(cacheKey, layoutDict, dependentKeyHastList);
            return layoutDict;
        }
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
        //
        //===================================================================================================
        //
        public class LinkAliasStoreModel {
            public Dictionary<int, LinkAliasModel> linkAliasIdDict { get; set; } = new();
            public Dictionary<string, LinkAliasModel> linkAliasGuidDict { get; set; } = new(StringComparer.InvariantCultureIgnoreCase);
            public Dictionary<string, LinkAliasModel> linkAliasNameDict { get; set; } = new(StringComparer.InvariantCultureIgnoreCase);
            public Dictionary<string, LinkAliasModel> linkAliasKeyDict { get; set; } = new(StringComparer.InvariantCultureIgnoreCase);
        }
        //
        /// <summary>
        /// clear store
        /// </summary>
        public void clearLinkAlias() {
            linkAliasIdDict_Local = null;
            linkAliasGuidDict_Local = null;
            linkAliasNameDict_Local = null;
            linkAliasKeyDict_Local = null;
        }
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
        //
        //===================================================================================================
        //
        public class ContentStoreModel {
            public Dictionary<int, ContentModel> ContentIdDict { get; set; } = new();
            public Dictionary<string, ContentModel> ContentGuidDict { get; set; } = new(StringComparer.InvariantCultureIgnoreCase);
            public Dictionary<string, ContentModel> ContentNameDict { get; set; } = new(StringComparer.InvariantCultureIgnoreCase);
            public Dictionary<string, ContentModel> ContentKeyDict { get; set; } = new(StringComparer.InvariantCultureIgnoreCase);
        }
        //
        /// <summary>
        /// clear store
        /// </summary>
        private void content_Clear() {
            ContentIdDict_Local = null;
            ContentGuidDict_Local = null;
            ContentNameDict_Local = null;
        }
        //
        /// <summary>
        /// load and return the Content dictionaries
        /// </summary>
        /// <returns></returns>
        private ContentStoreModel getContentStore() {
            string cacheKey = "ContentStore";
            ContentStoreModel ContentStore = core.cache.getObject<ContentStoreModel>(cacheKey);
            if (ContentStore != null) { return ContentStore; }
            //
            // -- load from db
            ContentStore = new ContentStoreModel();
            foreach (ContentModel Content in DbBaseModel.createList<ContentModel>(core.cpParent)) {
                if (!ContentStore.ContentIdDict.ContainsKey(Content.id)) {
                    ContentStore.ContentIdDict.Add(Content.id, Content);
                }
                string key = Content.ccguid.ToLowerInvariant();
                if (!string.IsNullOrEmpty(key) && !ContentStore.ContentGuidDict.ContainsKey(key)) {
                    ContentStore.ContentGuidDict.Add(key, Content);
                }
                key = Content.name.ToLowerInvariant();
                if (!string.IsNullOrEmpty(key) && !ContentStore.ContentNameDict.ContainsKey(key)) {
                    ContentStore.ContentNameDict.Add(key, Content);
                }
            }
            //
            // -- update cache
            var dependentKeyHastList = new List<CacheKeyHashClass>() { core.cache.createTableDependencyKeyHash(ContentModel.tableMetadata.tableNameLower) };
            core.cache.storeObject(cacheKey, ContentStore, dependentKeyHastList);
            return ContentStore;
        }
        //
        /// <summary>
        /// cache dictionary of Content by id
        /// </summary>
        public Dictionary<int, ContentModel> ContentIdDict {
            get {
                //if (core.cache.tryGetCacheDocument<ContentStoreModel>(core.cache.createKeyHash("ContentStoreModel"), out CacheDocumentClass _)) {
                //    if (ContentIdDict_Local != null) { return ContentIdDict_Local; }
                //}
                if (ContentIdDict_Local != null) { return ContentIdDict_Local; }
                //
                ContentStoreModel Content = getContentStore();
                ContentIdDict_Local = Content.ContentIdDict;
                ContentGuidDict_Local = Content.ContentGuidDict;
                ContentNameDict_Local = Content.ContentNameDict;
                return ContentIdDict_Local;
            }
        }
        private Dictionary<int, ContentModel> ContentIdDict_Local;
        //
        /// <summary>
        /// cache dictionary of Content by name
        /// </summary>
        public Dictionary<string, ContentModel> ContentNameDict {
            get {
                if (ContentNameDict_Local != null) { return ContentNameDict_Local; }
                ContentStoreModel Content = getContentStore();
                ContentIdDict_Local = Content.ContentIdDict;
                ContentGuidDict_Local = Content.ContentGuidDict;
                ContentNameDict_Local = Content.ContentNameDict;
                return ContentNameDict_Local;
            }
        }
        private Dictionary<string, ContentModel> ContentNameDict_Local;
        //
        /// <summary>
        /// cache dictionary of Content by id
        /// </summary>
        public Dictionary<string, ContentModel> ContentGuidDict {
            get {
                if (ContentGuidDict_Local != null) { return ContentGuidDict_Local; }
                ContentStoreModel Content = getContentStore();
                ContentIdDict_Local = Content.ContentIdDict;
                ContentGuidDict_Local = Content.ContentGuidDict;
                ContentNameDict_Local = Content.ContentNameDict;
                return ContentGuidDict_Local;
            }
        }
        private Dictionary<string, ContentModel> ContentGuidDict_Local;
    }
}