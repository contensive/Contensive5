
using Contensive.BaseClasses;
using System;
using System.Collections.Generic;
using System.Data;

namespace Contensive.Models.Db {
    //
    public class DataSourceModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("data sources", "ccdatasources", "default", true);
        //
        //====================================================================================================
        public string connString { get; set; }
        public string endpoint { get; set; }
        public string username { get; set; }
        public string password { get; set; }
        public int dbTypeId { get; set; }
        public bool secure { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// Special case for create. If id less than or equal to 0  return default datasource
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="recordId"></param>
        /// <returns></returns>
        public static DataSourceModel create(CPBaseClass cp, int recordId) {
            return (recordId > 0) ? create<DataSourceModel>(cp, recordId) : getDefaultDatasource(cp);
        }
        //
        //====================================================================================================
        /// <summary>
        /// Special case.  if id=0, return default datasource
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="recordId"></param>
        /// <param name="callersCacheNameList"></param>
        /// <returns></returns>
        public static DataSourceModel create(CPBaseClass cp, int recordId, ref List<string> callersCacheNameList) {
            return (recordId > 0) ? create<DataSourceModel>(cp, recordId, ref callersCacheNameList) : getDefaultDatasource(cp);
        }
        //
        //====================================================================================================
        /// <summary>
        /// Special case.  if name=default, return default datasource
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="recordName"></param>
        /// <returns></returns>
        public static DataSourceModel createByUniqueName(CPBaseClass cp, string recordName) {
            return (string.IsNullOrWhiteSpace(recordName) || (recordName.ToLowerInvariant() == "default")) ? getDefaultDatasource(cp) : createByUniqueName<DataSourceModel>(cp, recordName);
        }
        //
        //====================================================================================================
        /// <summary>
        /// Special case.  if name=default, return default datasource
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="recordName"></param>
        /// <param name="callersCacheNameList"></param>
        /// <returns></returns>
        public static DataSourceModel createByUniqueName(CPBaseClass cp, string recordName, ref List<string> callersCacheNameList) {
            return (string.IsNullOrWhiteSpace(recordName) || (recordName.ToLowerInvariant() == "default")) ? getDefaultDatasource(cp) : createByUniqueName<DataSourceModel>(cp, recordName, ref callersCacheNameList);
        }
        //
        //====================================================================================================
        /// <summary>
        /// Special case.  if name=default, return default datasource
        /// </summary>
        /// <returns></returns>
        public static bool isDataSourceDefault(string datasourceName) {
            return (string.IsNullOrWhiteSpace(datasourceName) || (datasourceName.ToLower() == "default"));
        }
        //
        //====================================================================================================
        //
        public static Dictionary<string, DataSourceModel> getNameDict(CPBaseClass cp) {
            Dictionary<string, DataSourceModel> result = new();
            try {
                List<string> ignoreCacheNames = new();
                using ( DataTable dt = cp.Db.ExecuteQuery("select id from ccdatasources where active>0")) {
                    foreach ( DataRow row in dt.Rows) {
                        DataSourceModel instance = create<DataSourceModel>(cp, cp.Utils.EncodeInteger( row["id"] ));
                        if (instance != null) {
                            result.Add(instance.name.ToLowerInvariant(), instance);
                        }
                    }
                }
                if (!result.ContainsKey("default")) {
                    result.Add("default", getDefaultDatasource(cp));
                }
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                throw;
            }
            return result;
        }
        //
        //====================================================================================================
        /// <summary>
        /// return the default datasource. The default datasource is defined in the application configuration and is NOT a Db record in the ccdatasources table
        /// </summary>
        /// <param name="cp"></param>
        public static DataSourceModel getDefaultDatasource(CPBaseClass cp) {
            DataSourceModel result = null;
            try {
                result = new DataSourceModel {
                    active = true,
                    ccguid = "",
                    connString = "",
                    contentControlId = 0,
                    createdBy = 0,
                    createKey = 0,
                    dateAdded = DateTime.MinValue,
                    dbTypeId = 2,
                    endpoint = cp.Secrets.DefaultDataSourceAddress,
                    name = "default"
                };
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                throw;
            }
            return result;
        }
        //
        //====================================================================================================
        /// <summary>
        /// convert a datasource name into the key value used by the datasourcedictionary cache
        /// </summary>
        /// <param name="DataSourceName"></param>
        /// <returns></returns>
        public static string normalizeDataSourceName(string DataSourceName) {
            if (!string.IsNullOrEmpty(DataSourceName)) {
                return DataSourceName.Trim().ToLowerInvariant();
            }
            return string.Empty;
        }
    }
}
