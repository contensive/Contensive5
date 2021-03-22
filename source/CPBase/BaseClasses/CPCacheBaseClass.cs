
using System;
using System.Collections.Generic;

namespace Contensive.BaseClasses {
    /// <summary>
    /// CP.Cache - local and remote caching
    /// </summary>
    /// <remarks></remarks>
    public abstract class CPCacheBaseClass {
        //
        //====================================================================================================
        /// <summary>
        /// Invalidate one or more cache objects by key
        /// </summary>
        /// <param name="keyList"></param>
        public abstract void Clear(List<string> keyList);
        //
        //====================================================================================================
        /// <summary>
        /// Return the value of a cache. If empty or invalid, returns null.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public abstract object GetObject(string key);
        //
        //====================================================================================================
        /// <summary>
        /// Return a string from cache. If empty or invalid, returns empty string.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public abstract string GetText(string key);
        //
        //====================================================================================================
        /// <summary>
        /// Return an integer from cache. If empty or invalid, returns 0.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public abstract int GetInteger(string key);
        //
        //====================================================================================================
        /// <summary>
        /// Return a double from cache. If empty or invalid, returns 0.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public abstract double GetNumber(string key);
        //
        //====================================================================================================
        /// <summary>
        /// Return a date from cache. If empty or invalid, returns Date.MinValue.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public abstract DateTime GetDate(string key);
        //
        //====================================================================================================
        /// <summary>
        /// Return a boolean from cache. If empty or invalid, returns false.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public abstract bool GetBoolean(string key);
        //
        //====================================================================================================
        /// <summary>
        /// Invalidate all system cache.
        /// </summary>
        /// <remarks></remarks>
        public abstract void ClearAll();
        //
        //====================================================================================================
        /// <summary>
        /// Invalidate a cache.
        /// </summary>
        /// <param name="key"></param>
        public abstract void Invalidate(string key);
        //
        //====================================================================================================
        /// <summary>
        /// Invalidate all system cache.
        /// </summary>
        public abstract void InvalidateAll();
        //
        //====================================================================================================
        /// <summary>
        /// Invalidate a list of cache keys.
        /// </summary>
        /// <param name="keyList"></param>
        public abstract void InvalidateTagList(List<string> keyList);
        //
        //====================================================================================================
        /// <summary>
        /// Invalidate a key based on the content name  and recordId
        /// </summary>
        /// <param name="contentName"></param>
        /// <param name="recordId"></param>
        public abstract void InvalidateContentRecord(string contentName, int recordId);
        //
        //====================================================================================================
        /// <summary>
        /// Invalidate a key based on the table and recordId
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="recordId"></param>
        public abstract void InvalidateTableRecord(string tableName, int recordId);
        //
        //====================================================================================================
        /// <summary>
        /// Invalidate all cache entries dependent on this tableKey
        /// </summary>
        /// <param name="tableName"></param>
        public abstract void InvalidateTable(string tableName);
        //
        //====================================================================================================
        /// <summary>
        /// Create a key used only as a dependency. If any record in the table is added/deleted/modified, this key will be updated, invalidating the parent object.
        /// Uses UpdateLastModified
        /// Uses the 
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="dataSourceName"></param>
        /// <returns></returns>
        public abstract string CreateTableDependencyKey(string tableName, string dataSourceName);
        //
        //====================================================================================================
        /// <summary>
        /// Create a key for a database table. Use this key to invalidate all keys based on thsi table using dependent keys.
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public abstract string CreateTableDependencyKey(string tableName);
        //
        //====================================================================================================
        /// <summary>
        /// Updates the cache dependency key for this table that a record in this table has been modified
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public abstract void invalidateTableDependencyKey(string tableName);
        //
        //====================================================================================================
        /// <summary>
        /// Create a cache key for a database model object
        /// </summary>
        /// <param name="recordId"></param>
        /// <param name="tableName"></param>
        /// <param name="dataSourceName"></param>
        /// <returns></returns>        
        public abstract string CreateRecordKey(int recordId, string tableName, string dataSourceName);
        //
        //====================================================================================================
        /// <summary>
        /// Create a key for a database record in the default datasource
        /// </summary>
        /// <param name="recordId"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public abstract string CreateRecordKey(int recordId, string tableName);
        //
        //====================================================================================================
        /// <summary>
        /// Create a cache key for a domain model. ObjectName can be generic domain model name. The unique Identifier must be unique, for example the id of a core record in the model.
        /// </summary>
        /// <param name="objectName"></param>
        /// <param name="objectUniqueIdentifier"></param>
        /// <returns></returns>
        public abstract string CreateKey(string objectName, string objectUniqueIdentifier);
        //
        //====================================================================================================
        /// <summary>
        /// Create a cache key for a domain model.
        /// </summary>
        /// <param name="objectName"></param>
        /// <returns></returns>
        public abstract string CreateKey(string objectName);
        //
        //====================================================================================================
        /// <summary>
        /// Create a key pointer for a db model object.
        /// A Ptr key doesnt contain the object, but points to a key for an object.
        /// When you get a cache object from a Ptr Key, the object it points to is returned.
        /// For example, data may be stored in a cache named for the id of a record, then a pointer created for the guid of the record.
        /// </summary>
        /// <param name="guid"></param>
        /// <param name="tableName"></param>
        /// <param name="dataSourceName"></param>
        /// <returns></returns>
        public abstract string CreatePtrKeyforDbRecordGuid(string guid, string tableName, string dataSourceName);
        //
        //====================================================================================================
        /// <summary>
        /// Create a key pointer for a db model object.
        /// </summary>
        /// <param name="guid"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public abstract string CreatePtrKeyforDbRecordGuid(string guid, string tableName);
        //
        //====================================================================================================
        /// <summary>
        /// Create a Ptr key for a db model object based on the record name. Only for tables where the name is unique
        /// </summary>
        /// <param name="name"></param>
        /// <param name="tableName"></param>
        /// <param name="dataSourceName"></param>
        /// <returns></returns>
        public abstract string CreatePtrKeyforDbRecordUniqueName(string name, string tableName, string dataSourceName);
        //
        //====================================================================================================
        /// <summary>
        /// Create a Ptr key for a db model object based on the record name.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public abstract string CreatePtrKeyforDbRecordUniqueName(string name, string tableName);
        //
        //====================================================================================================
        /// <summary>
        /// Get an object of type T from cache. If empty or invalid type, returns Null.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public abstract T GetObject<T>(string key);
        //
        //====================================================================================================
        /// <summary>
        /// Store an object to a key.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public abstract void Store(string key, object value);
        //
        //====================================================================================================
        /// <summary>
        /// Store an object to a key. Invalidate at the date and time specified.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="invalidationDate"></param>
        public abstract void Store(string key, object value, DateTime invalidationDate);
        //
        //====================================================================================================
        /// <summary>
        /// Store an object to a key.
        ///  Invalidate the object if a dependentKey is updated after this object is stored.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="dependentKeyList"></param>
        public abstract void Store(string key, object value, List<string> dependentKeyList);
        //
        //====================================================================================================
        /// <summary>
        /// Store an object to a key. Invalidate at the date and time specified.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="invalidationDate"></param>
        /// <param name="dependentKeyList"></param>
        public abstract void Store(string key, object value, DateTime invalidationDate, List<string> dependentKeyList);
        //
        //====================================================================================================
        /// <summary>
        /// Store an object to a key. 
        /// Invalidate the object if a dependentKey is updated after this object is stored.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="dependentKey"></param>
        public abstract void Store(string key, object value, string dependentKey);
        //
        //====================================================================================================
        /// <summary>
        /// Store an object to a key.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="invalidationDate"></param>
        /// <param name="dependentKey"></param>
        public abstract void Store(string key, object value, DateTime invalidationDate, string dependentKey);
        //
        //====================================================================================================
        /// <summary>
        /// Store a ptr to a cache entry. For example you may store an object by its recordId, but need to reference it by its guid. Create the cache key with the id, and a cache ptr which refers the guid to the cacheKey and a Get of the ptr will return the cache entry for the id.
        /// </summary>
        /// <param name="keyPtr"></param>
        /// <param name="key"></param>
        public abstract void StorePtr(string keyPtr, string key);
        //
        //====================================================================================================
        //
        // Deprecated methods
        //
        /// <summary>
        /// Use InvalidateTableDependencyKey() instead
        /// </summary>
        /// <param name="tableName"></param>
        [Obsolete("Use InvalidateTableDependencyKey() instead", false)] public abstract void UpdateLastModified(string tableName);
        /// <summary>
        /// use CreateRecordKey() instead
        /// </summary>
        /// <param name="recordId"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        [Obsolete("use CreateRecordKey() instead", false)] public abstract string CreateKeyForDbRecord(int recordId, string tableName);
        /// <summary>
        /// use CreateRecordKey() instead
        /// </summary>
        /// <param name="recordId"></param>
        /// <param name="tableName"></param>
        /// <param name="dataSourceName"></param>
        /// <returns></returns>
        [Obsolete("use CreateRecordKey() instead", false)] public abstract string CreateKeyForDbRecord(int recordId, string tableName, string dataSourceName);
        /// <summary>
        /// use CreateTabledependencyKey() instead
        /// </summary>
        /// <param name="tablename"></param>
        /// <returns></returns>
        [Obsolete("use CreateTabledependencyKey() instead", false)] public abstract string CreateDependencyKeyInvalidateOnChange(string tablename);
        /// <summary>
        /// use CreateTabledependencyKey() instead
        /// </summary>
        /// <param name="tablename"></param>
        /// <param name="datasource"></param>
        /// <returns></returns>
        [Obsolete("use CreateTabledependencyKey() instead", false)] public abstract string CreateDependencyKeyInvalidateOnChange(string tablename, string datasource);
        /// <summary>
        /// use Store() instead
        /// </summary>
        /// <param name="key"></param>
        /// <param name="Value"></param>
        [Obsolete("use Store() instead", false)] public abstract void SetKey(string key, object Value);
        /// <summary>
        /// use Store() instead
        /// </summary>
        /// <param name="key"></param>
        /// <param name="Value"></param>
        /// <param name="invalidationDate"></param>
        [Obsolete("use Store() instead", false)] public abstract void SetKey(string key, object Value, DateTime invalidationDate);
        /// <summary>
        /// use Store() instead
        /// </summary>
        /// <param name="key"></param>
        /// <param name="Value"></param>
        /// <param name="tagList"></param>
        [Obsolete("use Store() instead", false)] public abstract void SetKey(string key, object Value, List<string> tagList);
        /// <summary>
        /// use Store() instead
        /// </summary>
        /// <param name="key"></param>
        /// <param name="Value"></param>
        /// <param name="invalidationDate"></param>
        /// <param name="tagList"></param>
        [Obsolete("use Store() instead", false)] public abstract void SetKey(string key, object Value, DateTime invalidationDate, List<string> tagList);
        /// <summary>
        /// use Store() instead
        /// </summary>
        /// <param name="key"></param>
        /// <param name="Value"></param>
        /// <param name="tag"></param>
        [Obsolete("use Store() instead", false)] public abstract void SetKey(string key, object Value, string tag);
        /// <summary>
        /// use Store() instead
        /// </summary>
        /// <param name="key"></param>
        /// <param name="Value"></param>
        /// <param name="invalidationDate"></param>
        /// <param name="tag"></param>
        [Obsolete("use Store() instead", false)] public abstract void SetKey(string key, object Value, DateTime invalidationDate, string tag);
        /// <summary>
        /// Use Clear(dependentKeyList)
        /// </summary>
        /// <param name="ContentNameList"></param>
        [Obsolete("Use Clear(dependentKeyList)", false)] public abstract void Clear(string ContentNameList);
        /// <summary>
        /// Use GetText(key) instead
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        [Obsolete("Use GetText(key) instead", false)] public abstract string Read(string key);
        /// <summary>
        /// Use Invalidate(key)
        /// </summary>
        /// <param name="tag"></param>
        [Obsolete("Use Invalidate(key)", false)] public abstract void InvalidateTag(string tag);
        /// <summary>
        /// Use Store()
        /// </summary>
        /// <param name="key"></param>
        /// <param name="Value"></param>
        [Obsolete("Use Store()", false)] public abstract void Save(string key, string Value);
        /// <summary>
        /// Use Store()
        /// </summary>
        /// <param name="key"></param>
        /// <param name="Value"></param>
        /// <param name="tagCommaList"></param>
        [Obsolete("Use Store()", false)] public abstract void Save(string key, string Value, string tagCommaList);
        /// <summary>
        /// Use Store()
        /// </summary>
        /// <param name="key"></param>
        /// <param name="Value"></param>
        /// <param name="tagCommaList"></param>
        /// <param name="ClearOnDate"></param>
        [Obsolete("Use Store()", false)] public abstract void Save(string key, string Value, string tagCommaList, DateTime ClearOnDate);
    }

}

