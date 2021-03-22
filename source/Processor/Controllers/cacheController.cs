
using Contensive.Models.Db;
using Contensive.Processor.Exceptions;
using Contensive.Processor.Models.Domain;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Caching;
using System.Runtime.Serialization.Formatters.Binary;
using static Contensive.Processor.Constants;
using static Contensive.Processor.Controllers.GenericController;
using static Newtonsoft.Json.JsonConvert;
//
namespace Contensive.Processor.Controllers {
    //
    //====================================================================================================
    /// <summary>
    /// Interface to cache systems. Cache objects are saved to dotnet cache, remotecache, filecache. 
    /// 
    /// Cache Methods
    /// 
    ///   1) primary key -- cache key holding the object.
    ///         -- A key can be any string - a name for the cache
    ///         -- A key can be created in a standard format to represent a record in a database RecordKey()
    ///         -- A key can be created in a standard format to represent a dependency on Any record in a table - use
    ///         -- use createKey() to create a primary key
    ///         
    ///   2) dependent key -- an optional argument passed to a store() that invalidates the key if it is invalidated
    ///         -- it may hold content that the primary cache depends on. 
    ///                 -- for example a cache of Team may be saved with a dependency key list of people keys. if any people key is invalidated, the team invalidates
    ///         -- TableDependencyKey is invalidated each time a record is saved to the table. primary keys saved with the dependency are then also invalidated
    ///                 -- for example a cache of catalog items set to depend on the tableDependecyKey for items is invalidated if any item is updated.
    ///         -- use createKey() to create a dependency key if the primary key depends on the dependency
    ///         -- use create
    ///                 
    ///    3) pointer key -- a cache entry that holds a primary key (or another pointer key). 
    ///         -- When read, the cache returns the primary value. (primary="ccmembers/id/10", pointer="ccmembers/ccguid/{1234}"
    ///         -- pointer keys are read-only, as the content is saved in the primary key
    ///         -- set a pointer key with storePtr()
    ///         -- use storeRecord() to store the content to the key and all the ptrs
    /// 
    /// Cache Data Types
    /// 
    ///    1) object cache -- cache of any unstructured data
    ///         -- use createKey() to generate the key
    ///         -- use store() to store the object to the key
    ///         -- use get() to retrieve the object from the key
    ///         -- use invalidate() to invalidate the key
    ///         -- is invalidated by invalidateAll()
    ///         
    ///    2) record cache -- cache holding one record in a table
    ///         -- use createRecordKey() to generate the key
    ///         -- use createRecordGuidPtrKey() to generate a Ptr Key for the record that will retrieve the record based on the Guid
    ///         -- use createRecordNamePtrKey() to generate a Ptr Key for the record that will retrieve the record based on the Unique Name
    ///         -- use store() to store the object to the key
    ///         -- use get() to rerieve the object from the key
    ///         -- use invalidate() to invalidate the RecordKey, the RecordGuidPtr, or the RecordNamePtr
    ///         -- use invalidateRecordKey() to invalidate the key from the record/table/datasource names
    ///         
    /// Table Dependency Key
    ///         -- any cache object set to depend on this key will invalidate if the save date-time is newer than the table dependency key
    ///         -- saves the date-time when it was invalidated.
    ///         -- the /admin site invalidates this key on changes
    ///         -- models invalidate this key on change
    ///         -- database updates in the application should invalidate the key when a record is changed in a table
    ///         
    /// </summary>
    public class CacheController : IDisposable {
        //
        // ====================================================================================================
        // ----- objects passed in constructor, do not dispose
        /// <summary>
        /// local storage. Do not dispose
        /// </summary>
        private readonly CoreController core;
        //
        // ====================================================================================================
        // ----- objects constructed that must be disposed
        /// <summary>
        /// AWS client. Dispose on close
        /// </summary>
        //private Enyim.Caching.MemcachedClient cacheClientMemCacheD = null;
        private readonly ConnectionMultiplexer redisConnectionGroup;
        //
        private readonly IDatabase redisDb;
        //
        // ====================================================================================================
        // ----- private instance storage
        /// <summary>
        /// true if cacheClient initialized correctly
        /// </summary>
        private readonly bool remoteCacheInitialized;
        ///// <summary>
        ///// 
        ///// </summary>
        //private static Lazy<ConnectionMultiplexer> lazyConnection = new Lazy<ConnectionMultiplexer>(() => {
        //    return ConnectionMultiplexer.Connect("whatever-redis.bd1zu2.0009.usw2.cache.amazonaws.com:6379");
        //});
        ////
        ////====================================================================================================
        ////
        //public static ConnectionMultiplexer connection {
        //    get {
        //        ConnectionMultiplexer lazyConnection = ConnectionMultiplexer.Connect("whatever-redis.bd1zu2.0009.usw2.cache.amazonaws.com:6379");
        //        return lazyConnection;
        //    }
        //}
        //
        //====================================================================================================
        /// <summary>
        /// Initialize cache client
        /// </summary>
        /// <remarks></remarks>
        public CacheController(CoreController core) {
            try {
                this.core = core;
                //
                _globalInvalidationDate = null;
                remoteCacheInitialized = false;
                if (core.serverConfig.enableRemoteCache) {
                    //
                    // -- Redis implementation
                    string cacheEndpoint = core.serverConfig.awsElastiCacheConfigurationEndpoint;
                    if (!string.IsNullOrEmpty(cacheEndpoint)) {
                        redisConnectionGroup = ConnectionMultiplexer.Connect(cacheEndpoint);
                        if (redisConnectionGroup != null) {
                            redisDb = redisConnectionGroup.GetDatabase();
                            remoteCacheInitialized = true;
                        }
                    }
                }
            } catch (RedisConnectionException ex) {
                //
                // -- could not connect
                logger.Error(ex, LogController.processLogMessage(core, "Exception initializing Redis connection, will continue with cache disabled.", true));
            } catch (Exception ex) {
                //
                // -- mystery except, buyt cannot let a connection error take down the application
                logger.Error(ex, LogController.processLogMessage(core, "Exception initializing remote cache, will continue with cache disabled.", true));
            }
        }
        //
        //========================================================================
        /// <summary>
        /// get an object of type TData from cache. If the cache misses or is invalidated, null object is returned
        /// </summary>
        /// <typeparam name="TData"></typeparam>
        /// <param name="key">The text name for the cache entry</param>
        /// <returns></returns>
        public TData getObject<TData>(string key) {
            if (string.IsNullOrEmpty(key)) { return default; }
            CacheKeyHashClass keyHash = createKeyHash(key);
            return getObject<TData>(keyHash);
        }
        //
        //========================================================================
        /// <summary>
        /// get an object of type TData from cache. If the cache misses or is invalidated, null object is returned
        /// </summary>
        /// <typeparam name="TData"></typeparam>
        /// <param name="keyHash">The hashed key. Turn a Key (text name) to a has with createKeyHash().</param>
        /// <returns></returns>
        public TData getObject<TData>(CacheKeyHashClass keyHash) {
            try {
                //
                // -- read cacheDocument (the object that holds the data object plus control fields)
                CacheDocumentClass cacheDocument = getCacheDocument(keyHash);
                if (cacheDocument == null) { return default; }
                //
                // -- test for global invalidation
                int dateCompare = globalInvalidationDate.CompareTo(cacheDocument.saveDate);
                if (dateCompare >= 0) {
                    //
                    // -- global invalidation
                    logger.Trace(LogController.processLogMessage(core, "keyHash [" + keyHash + "], invalidated because cacheObject saveDate [" + cacheDocument.saveDate + "] is before the globalInvalidationDate [" + globalInvalidationDate + "]", false));
                    return default;
                }
                //
                // -- test all dependent objects for invalidation (if they have changed since this object changed, it is invalid)
                bool cacheMiss = false;
                foreach (CacheKeyHashClass dependentKeyHash in cacheDocument.dependentKeyHashList) {
                    CacheDocumentClass dependantCacheDocument = getCacheDocument(dependentKeyHash);
                    if (dependantCacheDocument == null) {
                        // create dummy cache to validate future cache requests, fake saveDate as last globalinvalidationdate
                        storeCacheDocument(dependentKeyHash, new CacheDocumentClass(core.dateTimeNowMockable) {
                            keyPtrHash = null,
                            content = "",
                            saveDate = globalInvalidationDate
                        });
                    } else {
                        dateCompare = dependantCacheDocument.saveDate.CompareTo(cacheDocument.saveDate);
                        if (dateCompare >= 0) {
                            //
                            // -- invalidate because a dependent document was changed after the cacheDocument was saved
                            cacheMiss = true;
                            logger.Trace(LogController.processLogMessage(core, "keyHash [" + keyHash + "], invalidated because the dependentKeyHash [" + dependentKeyHash + "] was modified [" + dependantCacheDocument.saveDate + "] after the cacheDocument's saveDate [" + cacheDocument.saveDate + "]", false));
                            break;
                        }
                    }
                }
                TData result = default;
                if (!cacheMiss) {
                    if ((cacheDocument.keyPtrHash != null) && !string.IsNullOrEmpty(cacheDocument.keyPtrHash.hash)) {
                        //
                        // -- this is a pointer key, load the primary
                        result = getObject<TData>(cacheDocument.keyPtrHash);
                    } else if (cacheDocument.content is Newtonsoft.Json.Linq.JObject dataJObject) {
                        //
                        // -- newtonsoft types
                        result = dataJObject.ToObject<TData>();
                    } else if (cacheDocument.content is Newtonsoft.Json.Linq.JArray dataJArray) {
                        //
                        // -- newtonsoft types
                        result = dataJArray.ToObject<TData>();
                    } else if (cacheDocument.content == null) {
                        //
                        // -- if cache data was left as a string (might be empty), and return object is not string, there was an error
                        result = default;
                    } else {
                        //
                        // -- all worked, but if the class is unavailable let it return default like a miss
                        try {
                            result = (TData)cacheDocument.content;
                        } catch (Exception ex) {
                            //
                            // -- object value did not match. return as miss
                            logger.Warn(LogController.processLogMessage(core, "cache getObject failed to cast value as type, keyHash [" + keyHash + "], type requested [" + typeof(TData).FullName + "], ex [" + ex + "]",true));
                            result = default;
                        }
                    }
                }
                return result;
            } catch (Exception ex) {
                logger.Error(ex, LogController.processLogMessage(core, "exception", true ));
                return default;
            }
        }
        //
        //========================================================================
        /// <summary>
        /// get an object from cache. If the cache misses or is invalidated, return ""
        /// </summary>
        /// <param name="key">The text name for the cache entry.</param>
        /// <returns></returns>
        public string getText(string key) => getObject<string>(key) ?? "";
        //
        //========================================================================
        /// <summary>
        /// get an object from cache. If the cache misses or is invalidated, return 0
        /// </summary>
        /// <param name="key">The text name for the cache entry.</param>
        /// <returns></returns>
        public int getInteger(string key) => getObject<int>(key);
        //
        //========================================================================
        /// <summary>
        /// get an object from cache. If the cache misses or is invalidated, return 0.0
        /// </summary>
        /// <param name="key">The text name for the cache entry.</param>
        /// <returns></returns>
        public double getNumber(string key) => getObject<double>(key);
        //
        //========================================================================
        /// <summary>
        /// get an object from cache. If the cache misses or is invalidated, return minDate
        /// </summary>
        /// <param name="key">The text name for the cache entry.</param>
        /// <returns></returns>
        public DateTime getDate(string key) => getObject<DateTime>(key);
        //
        //========================================================================
        /// <summary>
        /// get an object from cache. If the cache misses or is invalidated, return false
        /// </summary>
        /// <param name="key">The text name for the cache entry.</param>
        /// <returns></returns>
        public bool getBoolean(string key) => getObject<bool>(key);
        //
        //====================================================================================================
        /// <summary>
        /// get a cache object from the cache. returns the cacheObject that wraps the object
        /// </summary>
        /// <param name="keyHash">The hashed key. Turn a Key (text name) to a has with createKeyHash().</param>
        /// <returns></returns>
        private CacheDocumentClass getCacheDocument(CacheKeyHashClass keyHash) {
            CacheDocumentClass result = null;
            try {
                //string serverKey = createServerKeyHash(keyHash);
                string typeMessage = "";
                if (remoteCacheInitialized) {
                    //
                    // -- use remote cache
                    typeMessage = "remote";
                    try {
                        RedisKey redisKey = new RedisKey(keyHash.hash);
                        RedisValue redisValue = redisDb.StringGet(redisKey);
                        if (!redisValue.IsNull) {
                            result = Newtonsoft.Json.JsonConvert.DeserializeObject<CacheDocumentClass>(redisValue);
                        }
                        //result = cacheClientMemCacheD.Get<CacheDocumentClass>(keyHash.hash);
                    } catch (Exception ex) {
                        logger.Error(ex, LogController.processLogMessage(core, "exception", true ));
                        throw;
                    }
                }
                if ((result == null) && core.serverConfig.enableLocalMemoryCache) {
                    //
                    // -- local memory cache
                    typeMessage = "local-memory";
                    result = (CacheDocumentClass)MemoryCache.Default[keyHash.hash];
                }
                if ((result == null) && core.serverConfig.enableLocalFileCache) {
                    //
                    // -- local file cache
                    typeMessage = "local-file";
                    string serializedDataObject = null;
                    using (System.Threading.Mutex mutex = new System.Threading.Mutex(false, keyHash.hash)) {
                        mutex.WaitOne();
                        serializedDataObject = core.privateFiles.readFileText("appCache\\" + FileController.encodeDosFilename(keyHash.hash + ".txt"));
                        mutex.ReleaseMutex();
                    }
                    if (!string.IsNullOrEmpty(serializedDataObject)) {
                        result = DeserializeObject<CacheDocumentClass>(serializedDataObject);
                        storeCacheDocument_MemoryCache(keyHash, result);
                    }
                }
                string returnContentSegment = SerializeObject(result);
                returnContentSegment = (returnContentSegment.Length > 50) ? returnContentSegment.Substring(0, 50) : returnContentSegment;
                //
                // -- log result
                if (result == null) {
                    logger.Trace(LogController.processLogMessage(core, "miss, cacheType [" + typeMessage + "], key [" + keyHash.key + "]", false));
                } else {
                    if (result.content == null) {
                        logger.Trace(LogController.processLogMessage(core, "hit, cacheType [" + typeMessage + "], key [" + keyHash.key + "], saveDate [" + result.saveDate + "], content [null]", false));
                    } else {
                        string content = result.content.ToString();
                        content = (content.Length > 50) ? (content.left(50) + "...") : content;
                        logger.Trace(LogController.processLogMessage(core, "hit, cacheType [" + typeMessage + "], key [" + keyHash.key + "], saveDate [" + result.saveDate + "], content [" + content + "]", false));
                    }
                }
                //
                // if dependentKeyList is null, return an empty list, not null
                if (result != null) {
                    //
                    // -- empty objects return nothing, empty lists return count=0
                    if (result.dependentKeyHashList == null) {
                        result.dependentKeyHashList = new List<CacheKeyHashClass>();
                    }
                }

            } catch (Exception ex) {
                logger.Error(ex, LogController.processLogMessage(core, "exception", true ));
                throw;
            }
            return result;
        }
        //
        //====================================================================================================
        /// <summary>
        /// save an object to cache, with invalidation date and dependency list
        /// </summary>
        /// <param name="key">The text name for the cache entry.</param>
        /// <param name="content">The object to be saved in the cache</param>
        /// <param name="invalidationDate">The dateTime when the cache should automatically invalidate</param>
        /// <param name="dependentKeyHashList">A list of hashed keys (text keys that have been hashed with createhashKey). If any of these keys are invalidated or updated, this object will be automatically invalidated.</param>
        /// <remarks></remarks>
        public void storeObject(string key, object content, DateTime invalidationDate, List<CacheKeyHashClass> dependentKeyHashList) {
            CacheKeyHashClass keyHash = createKeyHash(key);
            storeObject(keyHash, content, invalidationDate, dependentKeyHashList);
        }
        //
        //====================================================================================================
        /// <summary>
        /// save an object to cache, with invalidation date and dependentKeyList
        /// </summary>
        /// <param name="key">The text name for the cache entry.</param>
        /// <param name="content">The object to be saved in the cache</param>
        /// <param name="invalidationDate">The dateTime when the cache should automatically invalidate</param>
        /// <param name="dependentKeyList">A list of text keys.  
        /// If any of these keys are invalidated or updated, this object will be automatically invalidated. 
        /// In the AdminSite, each time a record is updated, the system invalidates an object named for the record's table, using createTableDependencyKey()
        /// To make this object invalidate when any record in a table is updated in the admin site, add a tableDependencyKey for that table
        /// </param>
        public void storeObject(string key, object content, DateTime invalidationDate, List<string> dependentKeyList) {
            CacheKeyHashClass keyHash = createKeyHash(key);
            List<CacheKeyHashClass> dependentKeyHashList = createKeyHashList(dependentKeyList);
            storeObject(keyHash, content, invalidationDate, dependentKeyHashList);
        }
        //
        //====================================================================================================
        /// <summary>
        /// save an object to cache, with invalidation date and dependentKeyList
        /// </summary>
        /// <param name="keyHash">The hashed name for the cache entry. Use createKeyHash() to create a keyhash from a text key.</param>
        /// <param name="content">The object to be saved in the cache</param>
        /// <param name="invalidationDate">The dateTime when the cache should automatically invalidate</param>
        /// <param name="dependentKeyHashList">A list of hashed keys.  
        /// If any of these keys are invalidated or updated, this object will be automatically invalidated. 
        /// In the AdminSite, each time a record is updated, the system invalidates an object named for the record's table, using createTableDependencyKey()
        /// To make this object invalidate when any record in a table is updated in the admin site, add a tableDependencyKey for that table
        /// </param>
        public void storeObject(CacheKeyHashClass keyHash, object content, DateTime invalidationDate, List<CacheKeyHashClass> dependentKeyHashList) {
            try {
                var cacheDocument = new CacheDocumentClass(core.dateTimeNowMockable) {
                    content = content,
                    saveDate = core.dateTimeNowMockable,
                    invalidationDate = invalidationDate,
                    dependentKeyHashList = dependentKeyHashList
                };
                storeCacheDocument(keyHash, cacheDocument);
            } catch (Exception ex) {
                logger.Error(ex, LogController.processLogMessage(core, "exception", true ));
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// create a list of keyHash from a list of key
        /// </summary>
        /// <param name="keyList">A list of keys that need to be converted to a list of KeyHash</param>
        /// <returns></returns>
        public List<CacheKeyHashClass> createKeyHashList(List<string> keyList) {
            var result = new List<CacheKeyHashClass>();
            foreach (var key in keyList) {
                result.Add(createKeyHash(key));
            }
            return result;
        }
        //
        //====================================================================================================
        /// <summary>
        /// save a cache value, compatible with legacy method signature.
        /// </summary>
        /// <param name="key">The text name for the cache entry.</param>
        /// <param name="content"></param>
        /// <param name="dependentKeyHashList">List of dependent keyHash (created with createKeyHash().</param>
        /// <remarks>If a dependent key is invalidated, it's parent key is also invalid. 
        /// ex - org/id/10 has primary contact person/id/99. if org/id/10 object includes person/id/99 object, then org/id/10 depends on person/id/99,
        /// and "person/id/99" is a dependent key for "org/id/10". When "org/id/10" is read, it checks all its dependent keys (person/id/99) and
        /// invalidates if any dependent key is invalid.</remarks>
        public void storeObject(string key, object content, List<CacheKeyHashClass> dependentKeyHashList) {
            storeObject(key, content, core.dateTimeNowMockable.AddDays(invalidationDaysDefault), dependentKeyHashList);
        }
        //
        //====================================================================================================
        /// <summary>
        /// save a cache value, compatible with legacy method signature.
        /// </summary>
        /// <param name="key">The text name for the cache entry.</param>
        /// <param name="content"></param>
        /// <param name="dependentKeyList">List of dependent keys.</param>
        /// <remarks>If a dependent key is invalidated, it's parent key is also invalid. 
        /// ex - org/id/10 has primary contact person/id/99. if org/id/10 object includes person/id/99 object, then org/id/10 depends on person/id/99,
        /// and "person/id/99" is a dependent key for "org/id/10". When "org/id/10" is read, it checks all its dependent keys (person/id/99) and
        /// invalidates if any dependent key is invalid.</remarks>
        public void storeObject(string key, object content, List<string> dependentKeyList) {
            storeObject(key, content, core.dateTimeNowMockable.AddDays(invalidationDaysDefault), dependentKeyList);
        }
        //
        //====================================================================================================
        /// <summary>
        /// save a cache value, compatible with legacy method signature.
        /// </summary>
        /// <param name="key">The text name for the cache entry.</param>
        /// <param name="content"></param>
        /// <param name="dependantKey">a dependent key.</param>
        /// <remarks>If a dependent key is invalidated, it's parent key is also invalid. 
        /// ex - org/id/10 has primary contact person/id/99. if org/id/10 object includes person/id/99 object, then org/id/10 depends on person/id/99,
        /// and "person/id/99" is a dependent key for "org/id/10". When "org/id/10" is read, it checks all its dependent keys (person/id/99) and
        /// invalidates if any dependent key is invalid.</remarks>
        public void storeObject(string key, object content, string dependantKey) {
            storeObject(key, content, core.dateTimeNowMockable.AddDays(invalidationDaysDefault), new List<CacheKeyHashClass> { createKeyHash(dependantKey) });
        }
        //
        //====================================================================================================
        /// <summary>
        /// save an object to cache, with invalidation date
        /// </summary>
        /// <param name="key">The text name for the cache entry.</param>
        /// <param name="content"></param>
        /// <param name="invalidationDate"></param>
        /// <remarks></remarks>
        public void storeObject(string key, object content, DateTime invalidationDate) {
            storeObject(key, content, invalidationDate, new List<CacheKeyHashClass> { });
        }
        //
        //====================================================================================================
        /// <summary>
        /// save an object to cache, for compatibility with existing site. Always use a key generated from createKey methods
        /// </summary>
        /// <param name="key">A text key</param>
        /// <param name="content"></param>
        public void storeObject(string key, object content) {
            storeObject(key, content, core.dateTimeNowMockable.AddDays(invalidationDaysDefault), new List<CacheKeyHashClass> { });
        }
        //
        //====================================================================================================
        /// <summary>
        /// save an object to cache, for compatibility with existing site. Always use a key generated from createKey methods
        /// </summary>
        /// <param name="key">The text name for the cache entry.</param>
        /// <param name="content"></param>
        public void storeObject(CacheKeyHashClass keyHash, object content) {
            storeObject(keyHash, content, core.dateTimeNowMockable.AddDays(invalidationDaysDefault), new List<CacheKeyHashClass> { });
        }
        //
        //====================================================================================================
        /// <summary>
        /// save a Db Model cache.
        /// </summary>
        /// <param name="guid"></param>
        /// <param name="recordId"></param>
        /// <param name="tableName"></param>
        /// <param name="datasourceName"></param>
        /// <param name="content"></param>
        public void storeRecord(string guid, int recordId, string tableName, string datasourceName, object content) {
            CacheKeyHashClass keyHash = createRecordKeyHash(recordId, tableName, datasourceName);
            storeObject(keyHash, content);
            CacheKeyHashClass keyPtrHash = createRecordGuidPtrKeyHash(guid, tableName, datasourceName);
            storePtr(keyPtrHash, keyHash);
        }
        //
        //====================================================================================================
        /// <summary>
        /// future method. To support a cpbase implementation, but wait until BaseModel is exposed
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="guid"></param>
        /// <param name="recordId"></param>
        /// <param name="content"></param>
        public void storeRecord<T>(string guid, int recordId, object content) where T : DbBaseModel {
            Type derivedType = this.GetType();
            FieldInfo fieldInfoTable = derivedType.GetField("tableNameLower");
            if (fieldInfoTable == null) {
                throw new GenericException("Class [" + derivedType.Name + "] must declare constant [contentTableName].");
            } else {
                string tableName = fieldInfoTable.GetRawConstantValue().ToString();
                FieldInfo fieldInfoDatasource = derivedType.GetField("contentDataSource");
                if (fieldInfoDatasource == null) {
                    throw new GenericException("Class [" + derivedType.Name + "] must declare public constant [contentDataSource].");
                } else {
                    string datasourceName = fieldInfoDatasource.GetRawConstantValue().ToString();
                    CacheKeyHashClass keyHash = createRecordKeyHash(recordId, tableName, datasourceName);
                    storeObject(keyHash, content);
                    CacheKeyHashClass keyPtrHash = createRecordGuidPtrKeyHash(guid, tableName, datasourceName);
                    storePtr(keyPtrHash, keyHash);
                }
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// set a key ptr. A ptr points to a normal key, creating an altername way to get/invalidate a cache.
        /// ex - image with id=10, guid={999}. The normal key="image/id/10", the alias Key="image/ccguid/{9999}"
        /// </summary>
        /// <param name="CP"></param>
        /// <param name="keyPtr"></param>
        /// <param name="data"></param>
        /// <remarks></remarks>
        public void storePtr(CacheKeyHashClass keyPtrHash, CacheKeyHashClass keyHash) {
            try {
                CacheDocumentClass cacheDocument = new CacheDocumentClass(core.dateTimeNowMockable) {
                    saveDate = core.dateTimeNowMockable,
                    invalidationDate = core.dateTimeNowMockable.AddDays(invalidationDaysDefault),
                    keyPtrHash = keyHash
                };
                storeCacheDocument(keyPtrHash, cacheDocument);
            } catch (Exception ex) {
                logger.Error(ex, LogController.processLogMessage(core, "exception", true ));
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// set a key ptr. A ptr points to a normal key, creating an altername way to get/invalidate a cache.
        /// ex - image with id=10, guid={999}. The normal key="image/id/10", the alias Key="image/ccguid/{9999}"
        /// </summary>
        /// <param name="keyPtr"></param>
        /// <param name="key">The text name for the cache entry.</param>
        public void storePtr(string keyPtr, string key) {
            CacheKeyHashClass keyPtrHash = createKeyHash(keyPtr);
            CacheKeyHashClass keyHash = createKeyHash(key);
            storePtr(keyPtrHash, keyHash);
        }
        //
        //====================================================================================================
        /// <summary>
        /// invalidates the entire cache (except those entires written with saveRaw)
        /// </summary>
        /// <remarks></remarks>
        public void invalidateAll() {
            try {
                CacheKeyHashClass keyHash = createKeyHash(cacheNameGlobalInvalidationDate);
                storeCacheDocument(keyHash, new CacheDocumentClass(core.dateTimeNowMockable) { saveDate = core.dateTimeNowMockable });
                _globalInvalidationDate = null;
            } catch (Exception ex) {
                logger.Error(ex, LogController.processLogMessage(core, "exception", true ));
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// invalidate a key
        /// </summary>
        /// <param name="key">The text name for the cache entry.</param>
        public void invalidate(string key) {
            invalidate(createKeyHash(key), 5);
        }
        //
        //====================================================================================================
        /// <summary>
        /// invalidates a tag
        /// </summary>
        /// <param name="keyHash"></param>
        /// <param name="recursionLimit"></param>
        public void invalidate(CacheKeyHashClass keyHash, int recursionLimit = 5) {
            try {
                logger.Trace(LogController.processLogMessage(core, "invalidate, keyHash [key:" + keyHash.key + "], recursionLimit [" + recursionLimit + "]", false));
                if ((recursionLimit > 0) && (keyHash != null)) {
                    //CacheKeyHashClass keyHash = createKeyHash(key);
                    //
                    // if key is a ptr, we need to invalidate the real key
                    CacheDocumentClass cacheDocument = getCacheDocument(keyHash);
                    if (cacheDocument == null) {
                        //
                        // no cache for this key, if this is a dependency for another key, save invalidated
                        storeCacheDocument(keyHash, new CacheDocumentClass(core.dateTimeNowMockable) { saveDate = core.dateTimeNowMockable });
                    } else {
                        if (cacheDocument.keyPtrHash != null) {
                            //
                            // this key is an alias, invalidate it's parent key
                            invalidate(cacheDocument.keyPtrHash, --recursionLimit);
                        } else {
                            //
                            // key is a valid cache, invalidate it
                            storeCacheDocument(keyHash, new CacheDocumentClass(core.dateTimeNowMockable) { saveDate = core.dateTimeNowMockable });
                        }
                    }
                }
            } catch (Exception ex) {
                logger.Error(ex, LogController.processLogMessage(core, "exception", true ));
                throw;
            }
        }
        //
        //========================================================================
        /// <summary>
        /// invalidate a cache for a single database record. If you know the table is in a content model, call the model's invalidateRecord. Use this when the content is a variable.
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="recordId"></param>
        public void invalidateRecordKey(int recordId, string tableName, string dataSourceName = "default") {
            invalidate(createRecordKeyHash(recordId, tableName, dataSourceName));
        }
        //
        //====================================================================================================
        /// <summary>
        /// invalidates a list of keys 
        /// </summary>
        /// <param name="keyList"></param>
        /// <remarks></remarks>
        public void invalidate(List<string> keyList) {
            try {
                foreach (var key in keyList) {
                    invalidate(key);
                }
            } catch (Exception ex) {
                logger.Error(ex, LogController.processLogMessage(core, "exception", true ));
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// return the standard key for records. Store for this key should only be the object model for this id. Get for this key should return null or an object of the model.
        /// </summary>
        /// <param name="recordId"></param>
        /// <param name="tableName"></param>
        /// <param name="dataSourceName"></param>
        /// <returns></returns>
        public string createRecordKey(int recordId, string tableName, string dataSourceName) {
            string key = (String.IsNullOrWhiteSpace(dataSourceName)) ? "dbtable/default/" : "dbtable/" + dataSourceName.Trim().ToLowerInvariant() + "/";
            key += tableName.Trim().ToLowerInvariant() + "/id/" + recordId + "/";
            return key;
        }
        //
        //====================================================================================================
        public CacheKeyHashClass createRecordKeyHash(int recordId, string tableName, string dataSourceName) {
            return createKeyHash(createRecordKey(recordId, tableName, dataSourceName));
        }
        //
        //====================================================================================================
        /// <summary>
        /// return the standard key for records. Store for this key should only be the object model for this id. Get for this key should return null or an object of the model.
        /// </summary>
        /// <param name="recordId"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public string createRecordKey(int recordId, string tableName) {
            return createRecordKey(recordId, tableName, "default");
        }
        //
        //====================================================================================================
        public CacheKeyHashClass createRecordKeyHash(int recordId, string tableName) {
            return createRecordKeyHash(recordId, tableName, "default");
        }
        //
        //====================================================================================================
        /// <summary>
        /// return pointer key used in storePtr() to associate a guid to a record cache
        /// </summary>
        /// <param name="guid"></param>
        /// <param name="tableName"></param>
        /// <param name="dataSourceName"></param>
        /// <returns></returns>
        public string createRecordGuidPtrKey(string guid, string tableName, string dataSourceName) {
            return "dbptr/" + dataSourceName + "/" + tableName + "/ccguid/" + guid + "/";
        }
        //
        //====================================================================================================
        /// <summary>
        /// return pointer key used in storePtr() to associate a guid to a record cache
        /// </summary>
        /// <param name="recordGuid"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public string createRecordGuidPtrKey(string recordGuid, string tableName) {
            return createRecordGuidPtrKey(recordGuid, tableName, "default");
        }
        //
        //====================================================================================================
        /// <summary>
        /// return pointer key used in storePtr() to associate a guid to a record cache
        /// </summary>
        /// <param name="guid"></param>
        /// <param name="tableName"></param>
        /// <param name="dataSourceName"></param>
        /// <returns></returns>
        public CacheKeyHashClass createRecordGuidPtrKeyHash(string guid, string tableName, string dataSourceName) {
            string key = "dbptr/" + dataSourceName + "/" + tableName + "/ccguid/" + guid + "/";
            return createKeyHash(key);
        }
        //
        //====================================================================================================
        /// <summary>
        /// return pointer key used in storePtr() to associate a guid to a record cache
        /// </summary>
        /// <param name="recordGuid"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public CacheKeyHashClass createRecordGuidPtrKeyHash(string recordGuid, string tableName) {
            return createRecordGuidPtrKeyHash(recordGuid, tableName, "default");
        }
        //
        //====================================================================================================
        /// <summary>
        /// return pointer key used in storePtr() to associate a name to a record cache
        /// </summary>
        /// <param name="recordName"></param>
        /// <param name="tableName"></param>
        /// <param name="datasourceName"></param>
        /// <returns></returns>
        public string createRecordNamePtrKey(string recordName, string tableName, string datasourceName) {
            return "dbptr/" + datasourceName + "/" + tableName + "/name/" + recordName + "/";
        }
        //
        //====================================================================================================
        /// <summary>
        /// return pointer key used in storePtr() to associate a name to a record cache
        /// </summary>
        /// <param name="recordName"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public string createRecordNamePtrKey(string recordName, string tableName) {
            return createRecordNamePtrKey(recordName, tableName, "default");
        }
        //
        //====================================================================================================
        //
        public CacheKeyHashClass createRecordNamePtrKeyHash(string recordName, string tableName, string datasourceName) {
            string key = createRecordNamePtrKey(recordName, tableName, datasourceName);
            return createKeyHash(key);
        }
        //
        //====================================================================================================
        //
        public CacheKeyHashClass createRecordNamePtrKeyHash(string recordName, string tableName) {
            return createRecordNamePtrKeyHash(recordName, tableName, "default");
        }
        //
        //====================================================================================================
        /// <summary>
        /// create a key to store unmanaged content with store(). Managed content like database model should use createRecordKey and createTableDependencyKey.
        /// </summary>
        /// <param name="objectUniqueName">The unique key that describes the object. Ex. catalogitemList, or metadata-134</param>
        /// <returns></returns>
        public CacheKeyHashClass createKeyHash(string objectUniqueName) {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(createKey(objectUniqueName));
            return new CacheKeyHashClass() {
                key = objectUniqueName,
                hash = Convert.ToBase64String(plainTextBytes)
            };
        }
        //
        //====================================================================================================
        /// <summary>
        /// convert a unique name to a cache key (not hashed)
        /// </summary>
        /// <param name="objectUniqueName"></param>
        /// <returns></returns>
        public string createKey(string objectUniqueName) {
            return core.appConfig.name + "-" + objectUniqueName;
        }
        //
        //====================================================================================================
        /// <summary>
        /// returns the system globalInvalidationDate. This is the date/time when the entire cache was last cleared. Every cache object saved before this date is considered invalid.
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        private DateTime globalInvalidationDate {
            get {
                bool setDefault = false;
                CacheKeyHashClass globalInvalidationDateKeyHash = createKeyHash(cacheNameGlobalInvalidationDate);
                if (_globalInvalidationDate == null) {
                    CacheDocumentClass dataObject = getCacheDocument(globalInvalidationDateKeyHash);
                    if (dataObject != null) {
                        _globalInvalidationDate = dataObject.saveDate;
                    }
                    if (_globalInvalidationDate == null) {
                        setDefault = true;
                    }
                }
                if (!_globalInvalidationDate.HasValue) {
                    setDefault = true;
                } else {
                    if ((encodeDate(_globalInvalidationDate)).CompareTo(new DateTime(1990, 8, 7)) < 0) {
                        setDefault = true;
                    }
                }
                if (setDefault) {
                    _globalInvalidationDate = new DateTime(1990, 8, 7);
                    storeCacheDocument(globalInvalidationDateKeyHash, new CacheDocumentClass(core.dateTimeNowMockable) { saveDate = encodeDate(_globalInvalidationDate) });
                }
                return encodeDate(_globalInvalidationDate);
            }
        }
        private DateTime? _globalInvalidationDate;
        //
        //====================================================================================================
        /// <summary>
        /// save object directly to cache.
        /// </summary>
        /// <param name="keyHash">The hashed key. Turn a Key (text name) to a has with createKeyHash().</param>
        /// <param name="cacheDocument">Either a string, a date, or a serializable object</param>
        /// <param name="invalidationDate"></param>
        /// <remarks></remarks>
        private void storeCacheDocument(CacheKeyHashClass keyHash, CacheDocumentClass cacheDocument) {
            try {
                //
                if (keyHash == null) {
                    throw new ArgumentException("cache key cannot be blank");
                }
                string typeMessage = "";
                //string keyHash = createServerKeyHash(keyHash);
                if (core.serverConfig.enableLocalMemoryCache) {
                    //
                    // -- save local memory cache
                    typeMessage = "local-memory";
                    storeCacheDocument_MemoryCache(keyHash, cacheDocument);
                }
                if (core.serverConfig.enableLocalFileCache) {
                    //
                    // -- save local file cache
                    typeMessage = "local-file";
                    string serializedData = SerializeObject(cacheDocument);
                    using (System.Threading.Mutex mutex = new System.Threading.Mutex(false, keyHash.hash)) {
                        mutex.WaitOne();
                        core.privateFiles.saveFile("appCache\\" + FileController.encodeDosFilename(keyHash + ".txt"), serializedData);
                        mutex.ReleaseMutex();
                    }
                }
                if (core.serverConfig.enableRemoteCache) {
                    typeMessage = "remote";
                    if (remoteCacheInitialized) {
                        //
                        // -- save remote cache
                        var redisKey = new RedisKey(keyHash.hash);
                        string jsonCacheDocument = Newtonsoft.Json.JsonConvert.SerializeObject(cacheDocument);
                        var redisValue = new RedisValue(jsonCacheDocument);
                        TimeSpan? redisTimeSpan = cacheDocument.invalidationDate.Subtract(DateTime.Now);
                        redisDb.StringSet(redisKey, redisValue, redisTimeSpan);
                    }
                }
                //
                logger.Trace(LogController.processLogMessage(core, "cacheType [" + typeMessage + "], key [" + keyHash.key + "], expires [" + cacheDocument.invalidationDate + "], depends on [" + string.Join(",", cacheDocument.dependentKeyHashList) + "], points to [" + string.Join(",", cacheDocument.keyPtrHash) + "]", false));
                //
            } catch (Exception ex) {
                logger.Error(ex, LogController.processLogMessage(core, "exception", true ));
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// save cacheDocument to memory cache
        /// </summary>
        /// <param name="keyHash">key converted to serverKey with app name and code version</param>
        /// <param name="cacheDocument"></param>
        private void storeCacheDocument_MemoryCache(CacheKeyHashClass keyHash, CacheDocumentClass cacheDocument) {
            ObjectCache cache = MemoryCache.Default;
            CacheItemPolicy policy = new CacheItemPolicy {
                AbsoluteExpiration = cacheDocument.invalidationDate
            };
            cache.Set(keyHash.hash, cacheDocument, policy);
        }
        //
        //====================================================================================================
        /// <summary>
        /// invalidate the Table Dependency Key. This will invalidate any key set to be dependent on this key
        /// </summary>
        /// <param name="core"></param>
        public void invalidateTableDependencyKey(string tableName) {
            storeObject(createTableDependencyKeyHash(tableName), core.dateTimeNowMockable);
        }
        //
        //====================================================================================================
        /// <summary>
        /// create a table dependency key, used in store() to invalidate the content if any record in the table is modified
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="dataSourceName"></param>
        /// <returns></returns>
        public CacheKeyHashClass createTableDependencyKeyHash(string tableName, string dataSourceName) {
            //string key = "tabledependency/" + ((String.IsNullOrWhiteSpace(dataSourceName)) ? "default/" + tableName.Trim().ToLowerInvariant() + "/" : dataSourceName.Trim().ToLowerInvariant() + "/" + tableName.Trim().ToLowerInvariant() + "/");
            return createKeyHash(createTableDependencyKey(tableName, dataSourceName));
        }
        //
        public string createTableDependencyKey(string tableName, string dataSourceName) {
            return "tabledependency/" + ((String.IsNullOrWhiteSpace(dataSourceName)) ? "default/" + tableName.Trim().ToLowerInvariant() + "/" : dataSourceName.Trim().ToLowerInvariant() + "/" + tableName.Trim().ToLowerInvariant() + "/");
        }
        //
        //====================================================================================================
        /// <summary>
        /// create a table dependency key, used in store() to invalidate the content if any record in the table is modified
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public CacheKeyHashClass createTableDependencyKeyHash(string tableName) {
            return createTableDependencyKeyHash(tableName, "default");
        }
        public string createTableDependencyKey(string tableName) {
            return createTableDependencyKey(tableName, "default");
        }
        //
        //====================================================================================================
        //
        private static TData DeserializeFromString<TData>(string settings) {
            byte[] b = Convert.FromBase64String(settings);
            using (var stream = new MemoryStream(b)) {
                var formatter = new BinaryFormatter();
                stream.Seek(0, SeekOrigin.Begin);
                return (TData)formatter.Deserialize(stream);
            }
        }
        //
        //====================================================================================================
        //
        private static string SerializeToString<TData>(TData settings) {
            using (var stream = new MemoryStream()) {
                var formatter = new BinaryFormatter();
                formatter.Serialize(stream, settings);
                stream.Flush();
                stream.Position = 0;
                return Convert.ToBase64String(stream.ToArray());
            }
        }
        //
        //====================================================================================================
        //
        #region  IDisposable Support 
        //
        // this class must implement System.IDisposable
        // never throw an exception in dispose
        // Do not change or add Overridable to these methods.
        // Put cleanup code in Dispose(ByVal disposing As Boolean).
        //====================================================================================================
        //
        protected bool disposed;
        //
        public void Dispose() {
            // do not add code here. Use the Dispose(disposing) overload
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        //
        ~CacheController() {
            // do not add code here. Use the Dispose(disposing) overload
            Dispose(false);
        }
        //
        //====================================================================================================
        /// <summary>
        /// dispose.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing) {
            if (!this.disposed) {
                this.disposed = true;
                if (disposing) {
                    if (redisConnectionGroup != null) {
                        redisConnectionGroup.Close();
                        redisConnectionGroup.Dispose();
                    }
                    //if (cacheClientMemCacheD != null) {
                    //    cacheClientMemCacheD.Dispose();
                    //}
                    //
                    // cleanup managed objects
                }
                //
                // cleanup non-managed objects
            }
        }
        #endregion
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    }
}