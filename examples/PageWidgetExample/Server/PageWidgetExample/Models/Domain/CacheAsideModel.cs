using Contensive.BaseClasses;
using Contensive.Models.Db;
using System.Collections.Generic;
using System.Reflection;

namespace Contensive.PageWidgetExample.Models.Domain {
    // 
    // ====================================================================================================
    /// <summary>
    /// Implement cache aside read-through non-persistent cache for select models like person.
    /// The purpose is to prevent recreating objects if used 2+ times in a single document
    /// </summary>
    public class CacheAsideModel {
        // 
        /// <summary>
        /// store object by the string tablename.id
        /// </summary>
        private readonly Dictionary<string, object> recordIdDict = new Dictionary<string, object>();
        /// <summary>
        /// store id by guid
        /// </summary>
        private readonly Dictionary<string, int> recordGuidIdDict = new Dictionary<string, int>();
        // 
        // ====================================================================================================
        /// <summary>
        /// create the object from its Id
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cp"></param>
        /// <param name="recordId"></param>
        /// <returns></returns>
        public T create<T>(CPBaseClass cp, int recordId) where T : DbBaseModel {
            // 
            // -- argument check
            if (recordId <= 0)
                return null;
            // 
            // -- determine tablename so tablename is in the key and one dictionary can be used.
            var currentType = typeof(T);
            while (currentType is not null && currentType.BaseType is not null && !currentType.BaseType.Name.Equals("DbBaseModel"))
                currentType = currentType.BaseType;
            if (currentType is null)
                return DbBaseModel.create<T>(cp, recordId);
            // 
            // -- find the tableMetadata property of this class
            var propertyInfo = currentType.GetProperty("tableMetadata", BindingFlags.Static | BindingFlags.Public);
            if (propertyInfo is null)
                return DbBaseModel.create<T>(cp, recordId);
            // 
            // -- get the property value
            DbBaseTableMetadataModel tableMetadata = propertyInfo.GetValue(null, null) as DbBaseTableMetadataModel;
            if (tableMetadata is null)
                return DbBaseModel.create<T>(cp, recordId);
            // 
            // -- get the tablename from the tableMetadata property value
            string tablename = tableMetadata.tableNameLower;
            if (tablename is null)
                return DbBaseModel.create<T>(cp, recordId);
            // 
            cp.Utils.AppendLog("CacheAsideController.create, tablename [" + tablename + "], recordId [" + recordId + "]");
            // 
            // -- lookup model by id
            string keyId = tablename + "-" + recordId;
            if (recordIdDict.ContainsKey(keyId)) {
                // 
                cp.Utils.AppendLog("CacheAsideController.create, hit");
                // 
                return recordIdDict[keyId] as T;
            }
            // 
            cp.Utils.AppendLog("CacheAsideController.create, miss");
            // 
            // -- not found, get record from Db and populate id and guid dictionaries
            var result = DbBaseModel.create<T>(cp, recordId);
            if (result is null)
                return null;
            recordIdDict.Add(keyId, result);
            recordGuidIdDict.Add(result.ccguid, result.id);
            return result;
        }
        // 
        // ====================================================================================================
        /// <summary>
        /// create the object from its guid
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cp"></param>
        /// <param name="guid"></param>
        /// <returns></returns>
        public T create<T>(CPBaseClass cp, string guid) where T : DbBaseModel {
            // 
            // -- argument check
            if (string.IsNullOrEmpty(guid))
                return null;
            // 
            // -- determine tablename so tablename is in the key and one dictionary can be used.
            var currentType = typeof(T);
            var propertyInfo = currentType.GetProperty("tableMetadata", BindingFlags.Static | BindingFlags.Public);
            string tablename = null;
            if (propertyInfo is not null) {
                tablename = propertyInfo.GetValue(null, null) as string;
            }
            if (string.IsNullOrEmpty(tablename))
                return null;
            // 
            cp.Utils.AppendLog("CacheAsideController.create, tablename [" + tablename + "], guid [" + guid + "]");
            // 
            // -- lookup record Id by guid
            string keyGuid = tablename + "-" + guid;
            if (recordGuidIdDict.ContainsKey(keyGuid)) {
                // 
                cp.Utils.AppendLog("CacheAsideController.create, hit");
                // 
                // -- lookup model by id
                return create<T>(cp, cp.Utils.EncodeInteger(recordGuidIdDict[keyGuid]));
            }
            // 
            cp.Utils.AppendLog("CacheAsideController.create, miss");
            // 
            // -- not in guid-to-id, get model from Db and populate id and guid dictionaries
            var result = DbBaseModel.create<T>(cp, guid);
            if (result is null)
                return null;
            recordGuidIdDict.Add(keyGuid, result.id);
            string keyId = tablename + "-" + result.id;
            recordGuidIdDict.Add(keyId, result.id);
            return result;
        }
    }
}