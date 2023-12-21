
using System;
using System.Linq;
using System.Collections.Generic;
using Contensive.Processor.Controllers;
using Contensive.Processor.Models.Domain;

namespace Contensive.Processor {
    public class CPCacheClass : BaseClasses.CPCacheBaseClass {
        //
        private readonly CPClass cp;
        //
        //====================================================================================================
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="cp"></param>
        /// <remarks></remarks>
        public CPCacheClass(CPClass cp) {
            this.cp = cp;
        }
        //
        //====================================================================================================
        /// <summary>
        /// Invalidate a list of cache keys. These keys will return empty the next time they are read.
        /// </summary>
        /// <param name="keyList"></param>
        public override void Clear(List<string> keyList) => cp.core.cache.invalidate(keyList);
        //
        //====================================================================================================
        /// <summary>
        /// Retrieve a cache object and return it as an object
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public override object GetObject(string key) => cp.core.cache.getObject<object>(key);
        //
        //====================================================================================================
        /// <summary>
        /// Retrieve a cache object and return it as a string
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public override string GetText(string key) => cp.core.cache.getText(key);
        //
        //====================================================================================================
        /// <summary>
        /// Retrieve a cache object and return it as an integer
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public override int GetInteger(string key) => cp.core.cache.getInteger(key);
        //
        //====================================================================================================
        /// <summary>
        /// Retrieve a cache object and return it as a double number
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public override double GetNumber(string key) => cp.core.cache.getNumber(key);
        //
        //====================================================================================================
        /// <summary>
        /// Retrieve a cache object and return it as a date
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public override DateTime GetDate(string key) => cp.core.cache.getDate(key);
        //
        //====================================================================================================
        /// <summary>
        /// Retrieve a cache object and return it as a boolean
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public override bool GetBoolean(string key) => cp.core.cache.getBoolean(key);
        //
        //====================================================================================================
        /// <summary>
        /// invalidate a single cache key. The next time this cache is retrieved, it will return empty
        /// </summary>
        /// <param name="key"></param>
        public override void Invalidate(string key) => cp.core.cache.invalidate(key);
        //
        //====================================================================================================
        public override string CreateTableDependencyKey(string tableName) {
            return cp.core.cache.createTableDependencyKey(tableName);
        }
        //
        //====================================================================================================
        //
        public override string CreateTableDependencyKey(string tableName, string dataSourceName) {
            return cp.core.cache.createTableDependencyKey(tableName, dataSourceName);
        }
        //
        //====================================================================================================
        //
        public override void invalidateTableDependencyKey(string tableName) => cp.core.cache.invalidateTableDependencyKey(tableName);
        //
        //====================================================================================================
        //
        public override string CreateRecordKey(int recordId, string tableName, string dataSourceName) {
            return cp.core.cache.createRecordKey(recordId, tableName, dataSourceName);
        }
        //
        //====================================================================================================
        //
        public override string CreateRecordKey(int recordId, string tableName) {
            return cp.core.cache.createRecordKey(recordId, tableName);
        }
        //
        //====================================================================================================
        //
        public override string CreateKey(string objectName) {
            return cp.core.cache.createKey(objectName);
        }
        //
        //====================================================================================================
        //
        public override string CreateKey(string objectName, string objectUniqueIdentifier = "") {
            return cp.core.cache.createKey(objectName + "-" + objectUniqueIdentifier);
        }
        //
        //====================================================================================================
        //
        public override string CreatePtrKeyforDbRecordGuid(string guid, string tableName, string dataSourceName) {
            return cp.core.cache.createRecordGuidPtrKey(guid, tableName, dataSourceName);
        }
        //
        public override string CreatePtrKeyforDbRecordGuid(string guid, string tableName) {
            return cp.core.cache.createRecordGuidPtrKey(guid, tableName);
        }
        //
        //====================================================================================================
        //
        public override string CreatePtrKeyforDbRecordUniqueName(string name, string tableName, string dataSourceName) {
            return cp.core.cache.createRecordNamePtrKey(name, tableName, dataSourceName);
        }
        //
        //====================================================================================================
        //
        public override string CreatePtrKeyforDbRecordUniqueName(string name, string tableName) {
            return cp.core.cache.createRecordNamePtrKey(name, tableName);
        }
        //
        //====================================================================================================
        //
        public override T GetObject<T>(string key) => cp.core.cache.getObject<T>(key);
        //
        //====================================================================================================
        //
        public override void Store(string key, object value) => cp.core.cache.storeObject(key, value);
        //
        //====================================================================================================
        //
        public override void Store(string key, object value, DateTime invalidationDate) => cp.core.cache.storeObject(key, value, invalidationDate);
        //
        //====================================================================================================
        //
        public override void Store(string key, object value, List<string> dependentKeyList) {
            cp.core.cache.storeObject(key, value, cp.core.cache.createKeyHashList(dependentKeyList));
        }
        //
        //====================================================================================================
        //
        public override void Store(string key, object value, DateTime invalidationDate, List<string> dependentKeyList) {
            cp.core.cache.storeObject(key, value, invalidationDate, cp.core.cache.createKeyHashList(dependentKeyList));
        }
        //
        //====================================================================================================
        //
        public override void Store(string key, object value, string dependentKeyCommaList) {
            var dependentKeyHasList = cp.core.cache.createKeyHashList(dependentKeyCommaList.Split(',').ToList());
            cp.core.cache.storeObject(key, value, dependentKeyHasList);
        }
        //
        //====================================================================================================
        //
        public override void Store(string key, object value, DateTime invalidationDate, string dependentKeyCommaList) {
            var dependentKeyHasList = cp.core.cache.createKeyHashList(dependentKeyCommaList.Split(',').ToList());
            cp.core.cache.storeObject(key, value, invalidationDate, dependentKeyHasList);
        }
        //
        //====================================================================================================
        //
        public override void StorePtr(string keyPtr, string key) {
            cp.core.cache.storePtr(keyPtr, key);
        }
        //
        //====================================================================================================
        /// <summary>
        /// Clear all cache values
        /// </summary>
        /// <remarks></remarks>
        public override void ClearAll() => cp.core.cache.invalidateAll();
        //
        //====================================================================================================
        /// <summary>
        /// clear all cache entries related to all tables used by a comma delimited list of content.
        /// </summary>
        /// <param name="ContentNameList"></param>
        /// <remarks></remarks>
        ///
        [Obsolete("Use Clear(dependentKeyList)", false)]
        public override void Clear(string ContentNameList) {
            if (!string.IsNullOrEmpty(ContentNameList)) {
                List<string> tableNameList = new List<string>();
                foreach (var contentName in new List<string>(ContentNameList.ToLowerInvariant().Split(','))) {
                    string tableName = MetadataController.getContentTablename(cp.core, contentName).ToLowerInvariant();
                    if (!tableNameList.Contains(tableName)) {
                        tableNameList.Add(tableName);
                        cp.core.cache.invalidateTableDependencyKey(tableName);
                    }
                }
            }
        }
        //
        //====================================================================================================
        //
        public override void InvalidateAll() {
            cp.core.cache.invalidateAll();
        }
        //
        //====================================================================================================
        //
        public override void InvalidateTagList(List<string> tagList) {
            cp.core.cache.invalidate(tagList);
        }
        //
        //====================================================================================================
        //
        public override void InvalidateContentRecord(string contentName, int recordId) {
            cp.core.cache.invalidateRecordKey(recordId, MetadataController.getContentTablename(cp.core, contentName));
        }
        //
        //====================================================================================================
        //
        public override void InvalidateTableRecord(string tableName, int recordId) {
            cp.core.cache.invalidateRecordKey(recordId, tableName);
        }
        //
        //====================================================================================================
        //
        public override void InvalidateTable(string tableName) {
            cp.core.cache.invalidateTableDependencyKey(tableName);
        }
        //
        //====================================================================================================
        // deprecated
        //
        [Obsolete("deprecated", true)]
        public override void UpdateLastModified(string tableName) => cp.core.cache.invalidateTableDependencyKey(tableName);
        //
        [Obsolete("deprecated", true)]
        public override string CreateDependencyKeyInvalidateOnChange(string tableName, string dataSourceName) {
            return CreateTableDependencyKey(tableName, dataSourceName);
        }
        //
        [Obsolete("deprecated", true)]
        public override string CreateDependencyKeyInvalidateOnChange(string tableName) {
            return CreateTableDependencyKey(tableName);
        }
        //
        [Obsolete("deprecated", true)]
        public override void InvalidateTag(string tag) {
            cp.core.cache.invalidate(tag);
        }
        //
        [Obsolete("deprecated", true)]
        public override void SetKey(string key, object value) {
            cp.core.cache.storeObject(key, value);
        }
        //
        [Obsolete("deprecated", true)]
        public override void SetKey(string key, object value, DateTime invalidationDate) {
            cp.core.cache.storeObject(key, value, invalidationDate, new List<CacheKeyHashClass> { });
        }
        //
        [Obsolete("deprecated", true)]
        public override void SetKey(string key, object value, List<string> tagList) {
            cp.core.cache.storeObject(key, value, cp.core.cache.createKeyHashList(tagList));
        }
        //
        [Obsolete("deprecated", true)]
        public override void SetKey(string key, object value, DateTime invalidationDate, List<string> tagList) {
            cp.core.cache.storeObject(key, value, invalidationDate, cp.core.cache.createKeyHashList(tagList));
        }
        //
        [Obsolete("deprecated", true)]
        public override void SetKey(string key, object value, string tag) {
            cp.core.cache.storeObject(key, value, tag);
        }
        //
        [Obsolete("deprecated", true)]
        public override void SetKey(string key, object Value, DateTime invalidationDate, string tag) {
            List<string> depKeyList = (string.IsNullOrWhiteSpace(tag) ? new List<string> { } : tag.Split(',').ToList());
            cp.core.cache.storeObject(key, Value, invalidationDate, cp.core.cache.createKeyHashList(depKeyList));
        }
        //
        [Obsolete("Use GetText()", false)]
        public override string Read(string Name) => cp.core.cache.getText(Name);
        /// 
        [Obsolete("Use StoreObject()", false)]
        public override void Save(string key, string Value) => Store(key, Value);
        //
        [Obsolete("Use StoreObject()", false)]
        public override void Save(string key, string Value, string invalidationTagCommaList) => Save(key, Value, invalidationTagCommaList, DateTime.MinValue);
        //
        [Obsolete("Use StoreObject()", false)]
        public override void Save(string key, string Value, string invalidationKeyCommaList, DateTime invalidationDate) {
            try {
                List<string> invalidationKeyList = new List<string>();
                if (!string.IsNullOrEmpty(invalidationKeyCommaList.Trim())) {
                    invalidationKeyList.AddRange(invalidationKeyCommaList.Split(','));
                }
                var invalidatationKeyHashList = cp.core.cache.createKeyHashList(invalidationKeyList);
                if (invalidationDate.isOld()) {
                    cp.core.cache.storeObject(key, Value, invalidatationKeyHashList);
                } else {
                    cp.core.cache.storeObject(key, Value, invalidationDate, invalidatationKeyHashList);
                }
            } catch (Exception ex) {
                LogControllerX.logError(cp.core, ex);
                throw;
            }
        }
        //
        //====================================================================================================
        //
        [Obsolete("Use CreateRecordKey instead", true)]
        public override string CreateKeyForDbRecord(int recordId, string tableName, string dataSourceName) {
            return cp.core.cache.createRecordKey(recordId, tableName, dataSourceName);
        }
        //
        [Obsolete("Use CreateRecordKey instead", true)]
        public override string CreateKeyForDbRecord(int recordId, string tableName) {
            return cp.core.cache.createRecordKey(recordId, tableName);
        }
    }
}